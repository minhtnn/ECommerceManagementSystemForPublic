using System.Text.Json;
using AutoMapper;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.Orders;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Paginate;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using ECommerceManagementSystem.Coffee.Infrastructure.Utils;
using Mediator;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace ECommerceManagementSystem.Coffee.Application.Features.Orders.Query.GetCustomerOrders;

public class GetCustomerOrdersQueryHandler : IRequestHandler<GetCustomerOrdersQuery, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;
    private readonly IRedisService _redisService;
    private readonly IClaimService _claimService;
    private readonly IMapper _mapper;

    public GetCustomerOrdersQueryHandler(
        IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ILogger logger,
        IRedisService redisService,
        IClaimService claimService,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _redisService = redisService;
        _claimService = claimService;
        _mapper = mapper;
    }

    public async ValueTask<ApiResponse> Handle(GetCustomerOrdersQuery request, CancellationToken cancellationToken)
    {

        #region Get paginate

        // 1. Get current user info
        var role = _claimService.GetCurrentRoleEnum();
        var customerId = _claimService.GetCurrentReferenceId();
        
        // 2. Authorization - Only EndCustomer can access their orders
        if (role == null || role != ERole.EndCustomer || customerId == null || customerId == Guid.Empty)
        {
            return new ApiResponse()
            {
                Status = StatusCodes.Status401Unauthorized,
                Message = "Bạn không có quyền này!"
            };
        }
        
        // 3. Build cache key
        var cacheKey = BuildCacheKey(request, customerId.ToString());
        
        // 4. Try get from cache
        try
        {
            var cachedData = await _redisService.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                _logger.Debug($"Cache HIT: {cacheKey}");
        
                var cachedResult = JsonSerializer.Deserialize<Paginate<GetCustomerOrdersResponse>>(cachedData);
                if (cachedResult != null && cachedResult.Items.Any())
                {
                    foreach (var x in cachedResult.Items)
                    {
                        x.CreatedDate = TimeUtil.ConvertFromUtc(x.CreatedDate, request.TimeZone);
                    }
                }
                return new ApiResponse()
                {
                    Status = StatusCodes.Status200OK,
                    Message = "Lấy danh sách đơn hàng thành công",
                    Data = cachedResult
                };
            }
        
            _logger.Debug($"Cache MISS: {cacheKey}");
        }
        catch (RedisException e)
        {
            _logger.Warning("Redis cache read failed, falling back to database: {Error}", e.Message);
        }
        
        // 5. Query orders - ONLY for current customer
        var orders = await _unitOfWork.GetRepository<Domain.Entities.Orders>()
            .GetPagingListAsync<GetCustomerOrdersResponse>(
                predicate: x => 
                    x.CustomerId == customerId &&
                    (request.OrderStatus == null || x.OrderStatus == request.OrderStatus) &&
                    (request.PaymentStatus == null || x.PaymentStatus == request.PaymentStatus) &&
                    (string.IsNullOrEmpty(request.SearchKeyword) || x.Code.Contains(request.SearchKeyword)),
                include: i => i.Include(x => x.OrderDetails),
                page: request.Page,
                size: request.Size,
                sortBy: request.SortBy ?? "CreatedDate",
                isAsc: request.IsAsc
            );
        
        // 6. Cache the result
        try
        {
            var serializedData = JsonSerializer.Serialize(orders);
            await _redisService.SetStringAsync(
                cacheKey,
                serializedData,
                TimeSpan.FromMinutes(5) // Short TTL for order data
            );
            _logger.Information($"Cached my orders with key: {cacheKey}");
        }
        catch (RedisException redisEx)
        {
            _logger.Warning("Failed to cache my orders: {Error}", redisEx.Message);
        }

        if (orders != null && orders.Items.Any())
        {
            foreach (var x in orders.Items)
            {
                x.CreatedDate = TimeUtil.ConvertFromUtc(x.CreatedDate, request.TimeZone);
            }
        }
        
        return new ApiResponse()
        {
            Status = StatusCodes.Status200OK,
            Message = "Lấy danh sách đơn hàng thành công",
            Data = orders
        };

        #endregion
    }

    private string BuildCacheKey(GetCustomerOrdersQuery request, string customerId)
    {
        var orderStatus = request.OrderStatus?.ToString() ?? "all";
        var paymentStatus = request.PaymentStatus?.ToString() ?? "all";
        var searchKeyword = string.IsNullOrEmpty(request.SearchKeyword) ? "all" : request.SearchKeyword;

        return
            $"{CacheConfig.EntityListCachePrefix($"{nameof(Orders)}:{ERole.EndCustomer}:{customerId}")}" +
            $":page:{request.Page}:size:{request.Size}:searchKeyword:{searchKeyword}" +
            $":orderStatus:{orderStatus}:paymentStatus:{paymentStatus}" +
            $":sortBy:{request.SortBy}:isAsc:{request.IsAsc}";
    }
}
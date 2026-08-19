using System.Text.Json;
using AutoMapper;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Entities;
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

namespace ECommerceManagementSystem.Coffee.Application.Features.Orders.Query.GetBrandOrders;

public class GetBrandOrdersQueryHandler : IRequestHandler<GetBrandOrdersQuery, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;
    private readonly IRedisService _redisService;
    private readonly IClaimService _claimService;
    private readonly IMapper _mapper;

    public GetBrandOrdersQueryHandler(
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

    public async ValueTask<ApiResponse> Handle(GetBrandOrdersQuery request, CancellationToken cancellationToken)
    {
        // 1. Get current brand admin info
        var role = _claimService.GetCurrentRoleEnum();
        var brandId = _claimService.GetCurrentReferenceId();

        // 2. Authorization - Only BrandAdmin can access
        if (role == null || role != ERole.BrandAdmin || brandId == null || brandId == Guid.Empty)
        {
            return new ApiResponse()
            {
                Status = StatusCodes.Status401Unauthorized,
                Message = "Bạn không có quyền này!"
            };
        }
        request.FromDate = TimeUtil.ConvertToUtc(request.FromDate, request.TimeZone);
        request.ToDate = TimeUtil.ConvertToUtc(request.ToDate, request.TimeZone);
        // 3. Build cache key
        var cacheKey = BuildCacheKey(request, brandId.ToString());

        // 4. Try get from cache
        try
        {
            var cachedData = await _redisService.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                _logger.Debug($"Cache HIT: {cacheKey}");

                var cachedResult = JsonSerializer.Deserialize<Paginate<GetBrandOrdersResponse>>(cachedData);
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

        // 5. Query orders - ONLY for current brand's customers
        var orders = await _unitOfWork.GetRepository<Domain.Entities.Orders>()
            .GetPagingListAsync<GetBrandOrdersResponse>(
                predicate: x =>
                    x.Customer.BrandId == brandId && (x.Customer.Brand.Status == EBrandStatus.Active) &&
                    (request.OrderStatus == null || x.OrderStatus == request.OrderStatus) &&
                    (request.PaymentStatus == null || x.PaymentStatus == request.PaymentStatus) &&
                    (string.IsNullOrEmpty(request.SearchKeyword) ||
                     x.Code.Contains(request.SearchKeyword) ||
                     x.Customer.FullName.Contains(request.SearchKeyword)) &&
                    (request.FromDate == null || x.CreatedDate >= request.FromDate) &&
                    (request.ToDate == null || x.CreatedDate <= request.ToDate),
                include: i => i
                    .Include(x => x.OrderDetails)
                    .Include(x => x.Customer).ThenInclude(x => x.Brand)
                    .Include(x => x.Payments),
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
                TimeSpan.FromMinutes(5)
            );
            _logger.Information($"Cached brand orders with key: {cacheKey}");
        }
        catch (RedisException redisEx)
        {
            _logger.Warning("Failed to cache brand orders: {Error}", redisEx.Message);
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
    }

    private string BuildCacheKey(GetBrandOrdersQuery request, string brandId)
    {
        var orderStatus = request.OrderStatus?.ToString() ?? "all";
        var paymentStatus = request.PaymentStatus?.ToString() ?? "all";
        var searchKeyword = string.IsNullOrEmpty(request.SearchKeyword) ? "all" : request.SearchKeyword;
        var fromDate = request.FromDate?.ToString("yyyyMMdd") ?? "all";
        var toDate = request.ToDate?.ToString("yyyyMMdd") ?? "all";
        var sortBy = request.SortBy ?? "CreatedDate";

        return
            $"{CacheConfig.EntityListCachePrefix($"{nameof(Orders)}:{ERole.BrandAdmin}:{brandId}")}:{orderStatus}:{paymentStatus}:{searchKeyword}:{fromDate}:{toDate}:{request.Page}:{request.Size}:{sortBy}:{request.IsAsc}";
    }
}
using System.Text.Json;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.Customer;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Paginate;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Mediator;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace ECommerceManagementSystem.Coffee.Application.Features.Customers.query.GetCustomers;

public class GetCustomersQueryHandler : IRequestHandler<GetCustomersQuery, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly IRedisService _redisService;
    private readonly ILogger _logger;
    private readonly IClaimService _claimService;


    public GetCustomersQueryHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        IRedisService redisService, ILogger logger, IClaimService claimService)
    {
        _unitOfWork = unitOfWork;
        _redisService = redisService;
        _logger = logger;
        _claimService = claimService;
    }

    public async ValueTask<ApiResponse> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
    {
        var role = _claimService.GetCurrentRoleEnum();
        var brandId = _claimService.GetCurrentReferenceId();

        if (role == null || role != ERole.BrandAdmin || brandId == null || brandId == Guid.Empty)
        {
            return new ApiResponse()
            {
                Status = StatusCodes.Status401Unauthorized,
                Message = "Bạn không có quyền này!"
            };
        }

        var cacheKey =
            CacheConfig.EntityListCachePrefix(
                $"{nameof(Domain.Entities.Customers)}:{ERole.BrandAdmin}:{brandId.ToString()}");
        try
        {
            var cachedData = await _redisService.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                _logger.Debug($"Cache HIT: {cacheKey}");

                var cachedResult = JsonSerializer.Deserialize<Paginate<GetCustomersResponse>>(cachedData);

                return new ApiResponse()
                {
                    Status = StatusCodes.Status200OK,
                    Message = "Lấy danh sách khách hàng thành công",
                    Data = cachedResult
                };
            }

            _logger.Debug($"Cache MISS: {cacheKey}");
        }
        catch (RedisException e)
        {
            _logger.Warning("Redis cache read failed, falling back to database: {Error}", e.Message);
        }

        var customers = await _unitOfWork.GetRepository<Domain.Entities.Customers>()
            .GetPagingListAsync<GetCustomersResponse>(
                predicate: x => (string.IsNullOrWhiteSpace(request.Name) || x.FullName.Contains(request.Name))
                                && x.BrandId == brandId
                                && (!request.Status.HasValue ||
                                    x.CustomerAccounts.Any(ca => ca.Account.Status == request.Status)),
                include: x => x.Include(c => c.CustomerAccounts)
                    .ThenInclude(ca => ca.Account),
                page: request.Page,
                size: request.Size,
                sortBy: request.SortBy ?? "CreatedDate",
                isAsc: request.IsAsc
            );
        try
        {
            var serializedData = JsonSerializer.Serialize(customers);
            await _redisService.SetStringAsync(
                cacheKey,
                serializedData,
                CacheConfig.CustomersCacheTTL
            );
            _logger.Information(
                $"Cached customers list with key: {cacheKey}, TTL: {CacheConfig.CustomersCacheTTL}");
        }
        catch (RedisException redisEx)
        {
            _logger.Warning("Failed to cache customers list: {Error}", redisEx.Message);
        }

        return new ApiResponse()
        {
            Status = StatusCodes.Status200OK,
            Message = "Lấy danh sách khách hàng thành công",
            Data = customers
        };
    }
}
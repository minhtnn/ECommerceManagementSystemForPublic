using System.Text.Json;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.Customer;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Mediator;
using StackExchange.Redis;

namespace ECommerceManagementSystem.Coffee.Application.Features.CustomerAddresses.Query.GetCustomerAddresses;

public class GetCustomerAddressesQueryHandler : IRequestHandler<GetCustomerAddressesQuery, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;
    private readonly IRedisService _redisService;
    private readonly IClaimService _claimService;

    public GetCustomerAddressesQueryHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ILogger logger, IRedisService redisService, IClaimService claimService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _redisService = redisService;
        _claimService = claimService;
    }

    public async ValueTask<ApiResponse> Handle(GetCustomerAddressesQuery request, CancellationToken cancellationToken)
    {
        var role = _claimService.GetCurrentRoleEnum();
        var customerId = _claimService.GetCurrentReferenceId();

        if (role == null || role != ERole.EndCustomer || customerId == null || customerId == Guid.Empty)
        {
            return new ApiResponse()
            {
                Status = StatusCodes.Status401Unauthorized,
                Message = "Bạn không có quyền này!"
            };
        }

        var cacheKey = CacheConfig.EntityListCachePrefix($"{nameof(Domain.Entities.CustomerAddresses)}:{ERole.EndCustomer}:{customerId.ToString()}");
        try
        {
            var cachedData = await _redisService.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                _logger.Debug($"Cache HIT: {cacheKey}");

                var cachedResult =
                    JsonSerializer.Deserialize<ICollection<GetCustomerAddressesResponse>>(cachedData);

                return new ApiResponse()
                {
                    Status = StatusCodes.Status200OK,
                    Message = "Lấy danh sách địa chỉ thành công",
                    Data = cachedResult
                };
            }

            _logger.Debug($"Cache MISS: {cacheKey}");
        }
        catch (RedisException e)
        {
            _logger.Warning("Redis cache read failed, falling back to database: {Error}", e.Message);
        }

        var customerAddress = await _unitOfWork.GetRepository<Domain.Entities.CustomerAddresses>()
            .GetListAsync<GetCustomerAddressesResponse>(
                predicate: x => x.CustomerId == customerId
            );
        try
        {
            var serializedData = JsonSerializer.Serialize(customerAddress);
            await _redisService.SetStringAsync(
                cacheKey,
                serializedData,
                CacheConfig.CustomerAddressesCacheTTL
            );
            _logger.Information(
                $"Cached customer addresses list with key: {cacheKey}, TTL: {CacheConfig.CustomerAddressesCacheTTL}");
        }
        catch (RedisException redisEx)
        {
            _logger.Warning("Failed to cache customer addresses list: {Error}", redisEx.Message);
        }

        return new ApiResponse()
        {
            Status = StatusCodes.Status200OK,
            Message = "Lấy danh sách địa chỉ thành công",
            Data = customerAddress
        };
    }
}
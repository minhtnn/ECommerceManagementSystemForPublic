using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.Customer;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using ECommerceManagementSystem.Coffee.Infrastructure.Utils;
using Mediator;
using StackExchange.Redis;

namespace ECommerceManagementSystem.Coffee.Application.Features.CustomerAddresses.Query.GetCustomerAddressById;

public class GetCustomerAddressByIdQueryHandler : IRequestHandler<GetCustomerAddressByIdQuery, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;
    private readonly ICacheInvalidationService _cacheService;
    private readonly IClaimService _claimService;

    public GetCustomerAddressByIdQueryHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ILogger logger, ICacheInvalidationService cacheService, IClaimService claimService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cacheService = cacheService;
        _claimService = claimService;
    }

    public async ValueTask<ApiResponse> Handle(GetCustomerAddressByIdQuery request, CancellationToken cancellationToken)
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

        try
        {
            var cachedCustomerAddress = await _cacheService.GetDetailFromCacheAsync<GetCustomerAddressByIdResponse>(
                $"{CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.CustomerAddresses), request.Id.ToString())}:{ERole.EndCustomer}:{customerId.ToString()}"
            );
            if (cachedCustomerAddress != null)
            {
                _logger.Debug($"Cache HIT for customer address:{request.Id}");
                cachedCustomerAddress.CreatedDate =
                    TimeUtil.ConvertFromUtc(utcDateTime: cachedCustomerAddress.CreatedDate,
                        ianaTimeZone: request.TimeZone);
                cachedCustomerAddress.LastModifiedDate =
                    TimeUtil.ConvertFromUtc(
                        utcDateTime: cachedCustomerAddress.LastModifiedDate ?? cachedCustomerAddress.CreatedDate,
                        ianaTimeZone: request.TimeZone);
                return new ApiResponse
                {
                    Status = StatusCodes.Status200OK,
                    Message = "Lấy địa chỉ thành công",
                    Data = cachedCustomerAddress
                };
            }

            _logger.Debug($"Cache MISS for customer address:{request.Id}");
        }
        catch (RedisException e)
        {
            _logger.Warning("Redis cache read failed, falling back to database: {Error}", e.Message);
        }

        var customerAddress = await _unitOfWork.GetRepository<Domain.Entities.CustomerAddresses>()
            .SingleOrDefaultAsync<GetCustomerAddressByIdResponse>(
                predicate: (x => x.Id == request.Id && x.CustomerId == customerId)
            );
        if (customerAddress == null)
        {
            throw new BadHttpRequestException("Không tìm thấy địa chỉ với ID đã cho");
        }

        try
        {
            await _cacheService.SetDetailToCacheAsync(
                $"{CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.CustomerAddresses), request.Id.ToString())}:{ERole.EndCustomer}:{customerId.ToString()}",
                customerAddress,
                CacheConfig.CustomerAddressesCacheTTL
            );
        }
        catch (RedisException e)
        {
            _logger.Warning("Failed to cache customer address {ProductId}: {Error}", customerAddress.Id, e.Message);
        }
        customerAddress.CreatedDate =
            TimeUtil.ConvertFromUtc(utcDateTime: customerAddress.CreatedDate,
                ianaTimeZone: request.TimeZone);
        customerAddress.LastModifiedDate =
            TimeUtil.ConvertFromUtc(
                utcDateTime: customerAddress.LastModifiedDate ?? customerAddress.CreatedDate,
                ianaTimeZone: request.TimeZone);
        return new ApiResponse()
        {
            Status = StatusCodes.Status200OK,
            Message = "Lấy thông tin địa chỉ thành công",
            Data = customerAddress
        };
    }
}
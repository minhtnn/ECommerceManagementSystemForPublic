using AutoMapper;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemEnum;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.CustomerAddresses.Command.UpdateCustomerAddress;

public class UpdateCustomerAddressCommandHandler : IRequestHandler<UpdateCustomerAddressCommand, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly IClaimService _claimService;
    private readonly ICacheInvalidationService _cacheInvalidation;
    private readonly ILogger _logger;

    public UpdateCustomerAddressCommandHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        IClaimService claimService, ICacheInvalidationService cacheInvalidation, ILogger logger)
    {
        _unitOfWork = unitOfWork;
        _claimService = claimService;
        _cacheInvalidation = cacheInvalidation;
        _logger = logger;
    }

    public async ValueTask<ApiResponse> Handle(UpdateCustomerAddressCommand request,
        CancellationToken cancellationToken)
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

        var transactionResult = await _unitOfWork.BeginTransactionAsync();
        if (!transactionResult.IsSuccess)
        {
            _logger.Error("Không thể bắt đầu transaction: {Message}", transactionResult.Message);
            return new ApiResponse()
            {
                Status = StatusCodes.Status500InternalServerError,
                Message = "Không thể bắt đầu transaction"
            };
        }

        var existingCustomerAddress = await _unitOfWork.GetRepository<Domain.Entities.CustomerAddresses>()
            .SingleOrDefaultAsync(
                predicate: x => x.Id == request.Id
            );
        if (existingCustomerAddress == null)
        {
            throw new BadHttpRequestException("Không tìm thấy địa chỉ với Id đã cho!");
        }

        existingCustomerAddress.Receiver = request.Receiver;
        existingCustomerAddress.Address = request.Address;
        existingCustomerAddress.ShippingContact = request.ShippingContact;
        existingCustomerAddress.Latitude = request.Latitude;
        existingCustomerAddress.Longitude = request.Longitude;
        if (request.IsPrimary)
        {
            var isPrimaryCustomerAddress = await _unitOfWork.GetRepository<Domain.Entities.CustomerAddresses>()
                .SingleOrDefaultAsync(
                    predicate: x => x.CustomerId == customerId && x.IsPrimary == request.IsPrimary
                );
            if (isPrimaryCustomerAddress != null)
            {
                isPrimaryCustomerAddress.IsPrimary = false;
                _unitOfWork.GetRepository<Domain.Entities.CustomerAddresses>().UpdateAsync(isPrimaryCustomerAddress);
            }
        }

        existingCustomerAddress.IsPrimary = request.IsPrimary;
        existingCustomerAddress.LastModifiedDate = DateTime.UtcNow;
        _unitOfWork.GetRepository<Domain.Entities.CustomerAddresses>().UpdateAsync(existingCustomerAddress);
        var commitResult = await _unitOfWork.CommitTransactionAsync();

        if (!commitResult.IsSuccess)
        {
            _logger.Error(
                "Transaction commit failed: {Message}. Exception: {Exception}",
                commitResult.Message,
                commitResult.Exception?.Message
            );
            throw new Exception($"Không thể cập nhật địa chỉ khách hàng: {commitResult.Message}");
        }

        // Invalidate cache (sau khi commit thành công)
        // Thương hiệu sau khi cập nhật thành công nếu critical
        // cần xóa tất cả trong list brand ở redis trước đó, bao gồm detail.
        var cacheListResult = await _cacheInvalidation.InvalidateEntityCacheAsync(
            lockKey: CacheConfig.EntityInvalidationLock(
                CacheConfig.EntityListCachePrefix($"{nameof(Domain.Entities.CustomerAddresses)}:{ERole.EndCustomer}:{customerId.ToString()}")
            ),
            operation: EOperationBeforeCache.BulkUpdate,
            counterKey: CacheConfig.EntityInvalidationCounter(
                CacheConfig.EntityListCachePrefix($"{nameof(Domain.Entities.CustomerAddresses)}:{ERole.EndCustomer}:{customerId.ToString()}")
            ),
            entityCachePrefix:
            CacheConfig.EntityListCachePrefix($"{nameof(Domain.Entities.CustomerAddresses)}:{ERole.EndCustomer}:{customerId.ToString()}")
        );

        var cacheByIdResult = await _cacheInvalidation.InvalidateEntityCacheAsync(
            lockKey: CacheConfig.EntityInvalidationLock(
                $"{CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.CustomerAddresses), existingCustomerAddress.Id.ToString())}:{ERole.EndCustomer}:{customerId.ToString()}"
            ),
            operation: EOperationBeforeCache.BulkUpdate,
            counterKey: CacheConfig.EntityInvalidationCounter(
                $"{CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.CustomerAddresses), existingCustomerAddress.Id.ToString())}:{ERole.EndCustomer}:{customerId.ToString()}"
            ),
            entityCachePrefix:
            $"{CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.CustomerAddresses), existingCustomerAddress.Id.ToString())}:{ERole.EndCustomer}:{customerId.ToString()}"
        );

        if (cacheListResult.Success && cacheByIdResult.Success)
        {
            _logger.Information(
                "Updated customer address (ID: {Id}). Cache: {CacheListMessage}, {CacheDetailMessage}",
                existingCustomerAddress.Id,
                cacheListResult.Message,
                cacheByIdResult.Message
            );
        }
        else
        {
            _logger.Warning(
                "Updated customer address '{Id}' but cache invalidation failed: {CacheListMessage}, {CacheDetailMessage}",
                existingCustomerAddress.Id,
                cacheListResult.Message,
                cacheByIdResult.Message
            );
        }

        return new ApiResponse()
        {
            Status = StatusCodes.Status202Accepted,
            Message = "Cập nhật địa chỉ thành công!",
            Data = existingCustomerAddress.Id
        };
    }
}
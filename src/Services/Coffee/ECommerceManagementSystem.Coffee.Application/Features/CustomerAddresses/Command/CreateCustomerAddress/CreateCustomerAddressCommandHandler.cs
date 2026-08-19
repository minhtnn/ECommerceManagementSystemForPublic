using AutoMapper;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemEnum;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.CustomerAddresses.Command.CreateCustomerAddress;

public class CreateCustomerAddressCommandHandler : IRequestHandler<CreateCustomerAddressCommand, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly IClaimService _claimService;
    private readonly ICacheInvalidationService _cacheInvalidation;
    private readonly ILogger _logger;
    private readonly IMapper _mapper;

    public CreateCustomerAddressCommandHandler(IClaimService claimService, IMapper mapper,
        IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork, ILogger logger,
        ICacheInvalidationService cacheInvalidation)
    {
        _claimService = claimService;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cacheInvalidation = cacheInvalidation;
    }

    public async ValueTask<ApiResponse> Handle(CreateCustomerAddressCommand request,
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

        var customerAddress = _mapper.Map<Domain.Entities.CustomerAddresses>(request);
        customerAddress.Id = Guid.CreateVersion7();
        customerAddress.CustomerId = customerId;
        customerAddress.CreatedDate = DateTime.UtcNow;

        if (request.IsPrimary)
        {
            var existingCustomerAddress = await _unitOfWork.GetRepository<Domain.Entities.CustomerAddresses>()
                .SingleOrDefaultAsync(
                    predicate: x => x.CustomerId == customerId && x.IsPrimary == request.IsPrimary
                );
            if (existingCustomerAddress != null)
            {
                existingCustomerAddress.IsPrimary = !request.IsPrimary;
                _unitOfWork.GetRepository<Domain.Entities.CustomerAddresses>().UpdateAsync(existingCustomerAddress);
            }
        }

        await _unitOfWork.GetRepository<Domain.Entities.CustomerAddresses>().InsertAsync(customerAddress);
        var commitResult = await _unitOfWork.CommitTransactionAsync();

        if (!commitResult.IsSuccess)
        {
            _logger.Error(
                "Transaction commit failed: {Message}. Exception: {Exception}",
                commitResult.Message,
                commitResult.Exception?.Message
            );
            throw new Exception($"Không thể tạo địa chỉ khách hàng: {commitResult.Message}");
        }

        // Invalidate cache (sau khi commit thành công)
        // Địa chỉ khách hàng sau khi tạo thành công cần xóa tất cả trong
        // list address của khách hàng đó ở redis trước đó, bao gồm detail.
        var cacheResult = await _cacheInvalidation.InvalidateEntityCacheAsync(
            lockKey: CacheConfig.EntityInvalidationLock(
                CacheConfig.EntityListCachePrefix($"{nameof(Domain.Entities.CustomerAddresses)}:{ERole.EndCustomer}:{customerId.ToString()}")
            ),
            operation: EOperationBeforeCache.BulkCreate,
            counterKey: CacheConfig.EntityInvalidationCounter(
                CacheConfig.EntityListCachePrefix($"{nameof(Domain.Entities.CustomerAddresses)}:{ERole.EndCustomer}:{customerId.ToString()}")
            ),
            entityCachePrefix:
            CacheConfig.EntityCachePrefix($"{nameof(Domain.Entities.CustomerAddresses)}:{ERole.EndCustomer}:{customerId.ToString()}")
        );

        if (cacheResult.Success)
        {
            _logger.Information(
                "Created customer address (ID: {Id}). Cache: {CacheMessage}",
                customerAddress.Id,
                cacheResult.Message
            );
        }
        else
        {
            _logger.Warning(
                "Created customer address '{Id}' but cache invalidation failed: {CacheMessage}",
                customerAddress.Id,
                cacheResult.Message
            );
        }

        return new ApiResponse()
        {
            Status = StatusCodes.Status201Created,
            Message = "Tạo địa chỉ thành công!",
            Data = customerAddress.Id
        };
    }
}
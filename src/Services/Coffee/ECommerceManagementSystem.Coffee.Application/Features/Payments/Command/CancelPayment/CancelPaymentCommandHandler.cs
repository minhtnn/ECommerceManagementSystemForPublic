using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemEnum;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ECommerceManagementSystem.Coffee.Application.Features.Payments.Command.CancelPayment;

public class CancelPaymentCommandHandler : IRequestHandler<CancelPaymentCommand, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;
    private readonly IClaimService _claimService;
    private readonly ICacheInvalidationService _cacheInvalidation;

    public CancelPaymentCommandHandler(
        IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ILogger logger,
        IClaimService claimService,
        ICacheInvalidationService cacheInvalidation)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _claimService = claimService;
        _cacheInvalidation = cacheInvalidation;
    }

    public async ValueTask<ApiResponse> Handle(CancelPaymentCommand request, CancellationToken cancellationToken)
    {
        // 1. Authorization
        var role = _claimService.GetCurrentRoleEnum();
        var referenceId = _claimService.GetCurrentReferenceId();

        if (role == null || role != ERole.EndCustomer || referenceId == null || referenceId == Guid.Empty)
        {
            return new ApiResponse
            {
                Status = StatusCodes.Status401Unauthorized,
                Message = "Bạn không có quyền này!"
            };
        }

        // 2. Begin transaction
        var transactionResult = await _unitOfWork.BeginTransactionAsync();
        if (!transactionResult.IsSuccess)
        {
            _logger.Error("Cannot begin transaction: {Message}", transactionResult.Message);
            return new ApiResponse
            {
                Status = StatusCodes.Status500InternalServerError,
                Message = "Không thể bắt đầu transaction"
            };
        }

        try
        {
            // 3. Get order with details
            var order = await _unitOfWork.GetRepository<Domain.Entities.Orders>()
                .SingleOrDefaultAsync(
                    predicate: x => x.Id == request.OrderId,
                    include: i => i
                        .Include(x => x.Payments)
                        .Include(x => x.OrderDetails)
                );

            if (order == null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new ApiResponse
                {
                    Status = StatusCodes.Status404NotFound,
                    Message = "Không tìm thấy đơn hàng!"
                };
            }

            // 4. Verify ownership
            if (order.CustomerId != referenceId)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.Warning(
                    "Unauthorized cancel attempt: OrderId={OrderId}, CustomerId={CustomerId}",
                    request.OrderId, referenceId);

                return new ApiResponse
                {
                    Status = StatusCodes.Status403Forbidden,
                    Message = "Bạn không có quyền hủy đơn hàng này!"
                };
            }

            // 5. Check if order can be cancelled
            if (order.OrderStatus != EOrderStatus.WaitingPayment)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new ApiResponse
                {
                    Status = StatusCodes.Status400BadRequest,
                    Message = "Chỉ có thể hủy đơn hàng đang chờ thanh toán!"
                };
            }

            var payment = order.Payments?.FirstOrDefault();
            if (payment == null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new ApiResponse
                {
                    Status = StatusCodes.Status404NotFound,
                    Message = "Không tìm thấy thông tin thanh toán!"
                };
            }

            if (payment.PaymentStatus == EPaymentStatus.Completed)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new ApiResponse
                {
                    Status = StatusCodes.Status400BadRequest,
                    Message = "Không thể hủy đơn hàng đã thanh toán!"
                };
            }

            // 6. Update order status
            order.OrderStatus = EOrderStatus.Cancelled;
            order.PaymentStatus = EPaymentStatus.Failed;
            order.LastModifiedDate = DateTime.UtcNow;
            _unitOfWork.GetRepository<Domain.Entities.Orders>().UpdateAsync(order);

            // 7. Update payment status
            payment.PaymentStatus = EPaymentStatus.Failed;
            payment.FailedAt = DateTime.UtcNow;
            payment.FailedReason = request.CancelReason ?? "Khách hàng hủy thanh toán";
            payment.LastModifiedDate = DateTime.UtcNow;
            _unitOfWork.GetRepository<Domain.Entities.Payments>().UpdateAsync(payment);

            // 8. Restore product stock — chỉ non-gift items
            // Gift items (IsGiftItem=true) có UnitPrice=0 và không trừ stock khi tạo đơn
            // nên cũng không cần restore stock khi huỷ
            var nonGiftDetails = order.OrderDetails
                .Where(d => !d.IsGiftItem)
                .ToList();

            foreach (var detail in nonGiftDetails)
            {
                var product = await _unitOfWork.GetRepository<Domain.Entities.Products>()
                    .SingleOrDefaultAsync(predicate: x => x.Id == detail.ProductId);

                if (product != null)
                {
                    product.StockQuantity += detail.Quantity;
                    _unitOfWork.GetRepository<Domain.Entities.Products>().UpdateAsync(product);
                }
            }

            _logger.Information(
                "Restored stock for {Count} non-gift items (skipped {GiftCount} gift items) on cancel OrderId={OrderId}",
                nonGiftDetails.Count,
                order.OrderDetails.Count - nonGiftDetails.Count,
                request.OrderId);

            // 9. Create order history
            var orderHistory = new OrderHistoryStatus
            {
                Id = Guid.CreateVersion7(),
                OrderId = order.Id,
                FromStatus = EOrderStatus.WaitingPayment,
                ToStatus = EOrderStatus.Cancelled,
                Note = request.CancelReason ?? "Khách hàng hủy thanh toán",
                LastModifiedDate = DateTime.UtcNow
            };
            await _unitOfWork.GetRepository<OrderHistoryStatus>().InsertAsync(orderHistory);

            // 10. Commit transaction
            var commitResult = await _unitOfWork.CommitTransactionAsync();
            if (!commitResult.IsSuccess)
            {
                _logger.Error("Transaction commit failed: {Message}", commitResult.Message);
                throw new Exception($"Không thể hủy đơn hàng: {commitResult.Message}");
            }

            _logger.Information("Payment cancelled: OrderId={OrderId}", request.OrderId);

            // 11. Invalidate cache
            try
            {
                var cacheListResult = await _cacheInvalidation.InvalidateEntityCacheAsync(
                    lockKey: CacheConfig.EntityInvalidationLock(
                        CacheConfig.EntityListCachePrefix(
                            $"{nameof(Domain.Entities.Orders)}:{role}:{referenceId}")
                    ),
                    operation: EOperationBeforeCache.BulkUpdate,
                    counterKey: CacheConfig.EntityInvalidationCounter(
                        CacheConfig.EntityListCachePrefix(
                            $"{nameof(Domain.Entities.Orders)}:{role}:{referenceId}")
                    ),
                    entityCachePrefix: CacheConfig.EntityListCachePrefix(
                        $"{nameof(Domain.Entities.Orders)}:{role}:{referenceId}")
                );

                var cacheByIdResult = await _cacheInvalidation.InvalidateEntityCacheAsync(
                    lockKey: CacheConfig.EntityInvalidationLock(
                        $"{CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.Orders), order.Id.ToString())}:{role}:{referenceId}"
                    ),
                    operation: EOperationBeforeCache.BulkUpdate,
                    counterKey: CacheConfig.EntityInvalidationCounter(
                        $"{CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.Orders), order.Id.ToString())}:{role}:{referenceId}"
                    ),
                    entityCachePrefix:
                    $"{CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.Orders), order.Id.ToString())}:{role}:{referenceId}"
                );

                if (cacheListResult.Success && cacheByIdResult.Success)
                    _logger.Information(
                        "Cache invalidated after payment cancel: OrderId={OrderId}", request.OrderId);
                else
                    _logger.Warning(
                        "Cache invalidation issue after payment cancel: {ListMsg}, {ByIdMsg}",
                        cacheListResult.Message, cacheByIdResult.Message);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to invalidate cache after payment cancel");
                // Không fail request vì business logic đã thành công
            }

            return new ApiResponse
            {
                Status = StatusCodes.Status200OK,
                Message = "Hủy thanh toán thành công!"
            };
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.Error(ex, "Error cancelling payment");
            throw;
        }
    }
}
using System.Security.Authentication;
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

namespace ECommerceManagementSystem.Coffee.Application.Features.Orders.Command.UpdateOrder;

public class UpdateOrderCommandHandler : IRequestHandler<UpdateOrderCommand, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;
    private readonly IClaimService _claimService;
    private readonly ICacheInvalidationService _cacheInvalidation;
    private readonly IRefundService _refundService;

    public UpdateOrderCommandHandler(
        IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ILogger logger,
        IClaimService claimService,
        IRefundService refundService, ICacheInvalidationService cacheInvalidation)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _claimService = claimService;
        _refundService = refundService;
        _cacheInvalidation = cacheInvalidation;
    }

    public async ValueTask<ApiResponse> Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
    {
        #region 1. Get current user info

        var role = _claimService.GetCurrentRoleEnum();
        var userId = _claimService.GetCurrentAccountId();
        var referenceId = _claimService.GetCurrentReferenceId();

        if (role == null || userId == Guid.Empty)
        {
            throw new AuthenticationException("Bạn chưa đăng nhập!");
        }

        #endregion

        #region 2. Get order with all necessary includes

        var order = await _unitOfWork.GetRepository<Domain.Entities.Orders>()
            .SingleOrDefaultAsync(
                predicate: x => x.Id == request.OrderId,
                include: i => i.Include(x => x.OrderDetails)
                    .Include(x => x.Customer)
                    .Include(x => x.RefundRequest)
            );

        if (order == null)
        {
            throw new BadHttpRequestException("Không tìm thấy đơn hàng!");
        }

        #endregion

        #region 3. Authorization check

        if (role == ERole.EndCustomer)
        {
            if (order.CustomerId != referenceId)
            {
                _logger.Warning(
                    "Customer {CustomerId} attempted to update order {OrderId} owned by {OwnerId}",
                    referenceId, order.Id, order.CustomerId);
                throw new AuthenticationException("Bạn không có quyền cập nhật đơn hàng này!");
            }
        }
        else if (role == ERole.BrandAdmin)
        {
            if (order.Customer?.BrandId != referenceId)
            {
                _logger.Warning(
                    "BrandAdmin {BrandId} attempted to update order {OrderId} from brand {OrderBrandId}",
                    referenceId, order.Id, order.Customer?.BrandId);

                throw new AuthenticationException("Bạn không có quyền cập nhật đơn hàng này!");
            }
        }

        #endregion

        #region 4. Validate and process based on role

        ApiResponse result;
        if (role == ERole.EndCustomer)
        {
            result = await ProcessCustomerUpdateAsync(order, request, userId, cancellationToken);
        }
        else if (role == ERole.BrandAdmin)
        {
            result = await ProcessAdminUpdateAsync(order, request, userId, cancellationToken);
        }
        else
        {
            return new ApiResponse
            {
                Status = StatusCodes.Status403Forbidden,
                Message = "Bạn không có quyền cập nhật đơn hàng!"
            };
        }

        #endregion

        // 5. Invalidate cache if successful
        if (result.Status == StatusCodes.Status200OK)
        {
            try
            {
                var isStatusChanged = order.OrderStatus != request.NewOrderStatus;

                // Invalidate cache (sau khi commit thành công)
                // Đơn hàng của khách hàng sau khi cập nhật thành công nếu critical
                // cần xóa tất cả trong list brand ở redis trước đó, bao gồm detail.
                var cacheListResult = await _cacheInvalidation.InvalidateEntityCacheAsync(
                    lockKey: CacheConfig.EntityInvalidationLock(
                        CacheConfig.EntityListCachePrefix($"{nameof(Domain.Entities.Orders)}:{role}:{referenceId}")
                    ),
                    operation: isStatusChanged
                        ? EOperationBeforeCache.BulkUpdate
                        : EOperationBeforeCache.NormalUpdate,
                    counterKey: CacheConfig.EntityInvalidationCounter(
                        CacheConfig.EntityListCachePrefix($"{nameof(Domain.Entities.Orders)}:{role}:{referenceId}")
                    ),
                    entityCachePrefix:
                    CacheConfig.EntityListCachePrefix($"{nameof(Domain.Entities.Orders)}:{role}:{referenceId}")
                );

                var cacheByIdResult = await _cacheInvalidation.InvalidateEntityCacheAsync(
                    lockKey: CacheConfig.EntityInvalidationLock(
                        $"{CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.Orders), order.Id.ToString())}:{role}:{referenceId}"
                    ),
                    operation: isStatusChanged
                        ? EOperationBeforeCache.BulkUpdate
                        : EOperationBeforeCache.NormalUpdate,
                    counterKey: CacheConfig.EntityInvalidationCounter(
                        $"{CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.Orders), order.Id.ToString())}:{role}:{referenceId}"
                    ),
                    entityCachePrefix:
                    $"{CacheConfig.EntityByIdCachePrefix(nameof(Domain.Entities.Orders), order.Id.ToString())}:{role}:{referenceId}"
                );
                if (cacheListResult.Success && cacheByIdResult.Success)
                {
                    _logger.Information(
                        $"Updated orders '{order.Code}' (ID: {order.Id}). Cache: {cacheListResult.Message}, {cacheByIdResult.Message}."
                    );
                }
                else
                {
                    _logger.Warning(
                        $"Updated brand '{order.Code}' but cache invalidation failed: {cacheListResult.Message}, {cacheByIdResult.Message}."
                    );
                }


                _logger.Debug("Cache invalidated for order {OrderCode}", order.Code);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to invalidate cache for order {OrderCode}", order.Code);
            }
        }


        return result;
    }

    #region Customer Update

    private async Task<ApiResponse> ProcessCustomerUpdateAsync(
        Domain.Entities.Orders order,
        UpdateOrderCommand request,
        Guid userId,
        CancellationToken cancellationToken)
    {
        // Customer can only update when order is Pending
        if (order.OrderStatus != EOrderStatus.Pending)
        {
            return new ApiResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Message = "Chỉ có thể cập nhật đơn hàng khi đang ở trạng thái Chờ xử lý!"
            };
        }

        // Check if customer is trying to cancel
        if (request.NewOrderStatus == EOrderStatus.Cancelled)
        {
            return await CancelOrderAsync(order, request, userId, ERole.EndCustomer, cancellationToken);
        }

        // Check if customer is trying to change status (not allowed except cancel)
        if (request.NewOrderStatus != null)
        {
            return new ApiResponse
            {
                Status = StatusCodes.Status403Forbidden,
                Message = "Bạn chỉ có quyền hủy đơn hàng, không thể thay đổi trạng thái khác!"
            };
        }

        // Update shipping info only
        return await UpdateShippingInfoAsync(order, request);
    }

    private async Task<ApiResponse> UpdateShippingInfoAsync(
        Domain.Entities.Orders order,
        UpdateOrderCommand request)
    {
        var transactionResult = await _unitOfWork.BeginTransactionAsync();
        if (!transactionResult.IsSuccess)
        {
            return new ApiResponse
            {
                Status = StatusCodes.Status500InternalServerError,
                Message = "Không thể bắt đầu transaction!"
            };
        }

        try
        {
            var hasChanges = false;
            var changes = new List<string>();

            // Update shipping address
            if (!string.IsNullOrWhiteSpace(request.ShippingAddress) &&
                request.ShippingAddress != order.ShippingAddress)
            {
                var oldAddress = order.ShippingAddress;
                order.ShippingAddress = request.ShippingAddress;
                hasChanges = true;
                changes.Add($"Địa chỉ giao hàng: '{oldAddress}' → '{request.ShippingAddress}'");
            }

            // Update shipping contact
            if (!string.IsNullOrWhiteSpace(request.ShippingContact) &&
                request.ShippingContact != order.ShippingContact)
            {
                var oldContact = order.ShippingContact;
                order.ShippingContact = request.ShippingContact;
                hasChanges = true;
                changes.Add($"Số điện thoại: '{oldContact}' → '{request.ShippingContact}'");
            }

            // Update customer note
            if (request.CustomerNote != null && request.CustomerNote != order.CustomerNote)
            {
                order.CustomerNote = request.CustomerNote;
                hasChanges = true;
                changes.Add("Ghi chú đơn hàng đã được cập nhật");
            }

            if (!hasChanges)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new ApiResponse
                {
                    Status = StatusCodes.Status400BadRequest,
                    Message = "Không có thay đổi nào được thực hiện!"
                };
            }

            order.LastModifiedDate = DateTime.UtcNow;
            _unitOfWork.GetRepository<Domain.Entities.Orders>().UpdateAsync(order);

            // Create order history
            var history = new OrderHistoryStatus
            {
                Id = Guid.CreateVersion7(),
                OrderId = order.Id,
                FromStatus = order.OrderStatus,
                ToStatus = order.OrderStatus, // Same status
                Note = $"Khách hàng cập nhật thông tin: {string.Join(", ", changes)}",
                LastModifiedDate = DateTime.UtcNow
            };
            await _unitOfWork.GetRepository<OrderHistoryStatus>().InsertAsync(history);

            var commitResult = await _unitOfWork.CommitTransactionAsync();
            if (!commitResult.IsSuccess)
            {
                _logger.Error("Failed to commit shipping info update: {Message}", commitResult.Message);
                throw new Exception($"Không thể cập nhật thông tin giao hàng: {commitResult.Message}");
            }

            _logger.Information(
                "Customer updated shipping info - Order: {OrderCode}, Changes: {Changes}",
                order.Code, string.Join(", ", changes));

            return new ApiResponse
            {
                Status = StatusCodes.Status200OK,
                Message = "Cập nhật thông tin giao hàng thành công!",
                Data = order.Id,
            };
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.Error(ex, "Error updating shipping info for order {OrderCode}", order.Code);
            throw;
        }
    }

    #endregion

    #region Admin Update

    private async Task<ApiResponse> ProcessAdminUpdateAsync(
        Domain.Entities.Orders order,
        UpdateOrderCommand request,
        Guid userId,
        CancellationToken cancellationToken)
    {
        // Admin cannot update shipping info
        if (!string.IsNullOrEmpty(request.ShippingAddress) ||
            !string.IsNullOrEmpty(request.ShippingContact) ||
            !string.IsNullOrEmpty(request.CustomerNote))
        {
            throw new BadHttpRequestException("Admin không được phép sửa thông tin giao hàng!");
        }

        // Admin must provide new status
        if (request.NewOrderStatus == null)
        {
            throw new BadHttpRequestException("Vui lòng chọn trạng thái mới cho đơn hàng!");
        }

        // Check if cancelling
        if (request.NewOrderStatus == EOrderStatus.Cancelled)
        {
            return await CancelOrderAsync(order, request, userId, ERole.BrandAdmin, cancellationToken);
        }

        // Validate status transition (forward only)
        var validationResult = ValidateStatusTransition(order.OrderStatus, request.NewOrderStatus.Value);
        if (validationResult.Status != StatusCodes.Status200OK)
        {
            return validationResult;
        }

        return await UpdateOrderStatusAsync(order, request.NewOrderStatus.Value);
    }

    private ApiResponse ValidateStatusTransition(EOrderStatus currentStatus, EOrderStatus newStatus)
    {
        // Define status order (progression)
        var statusOrder = new Dictionary<EOrderStatus, int>
        {
            { EOrderStatus.WaitingPayment, 0 },
            { EOrderStatus.Pending, 1 },
            { EOrderStatus.Processing, 2 },
            { EOrderStatus.Shipped, 3 },
            { EOrderStatus.Delivered, 4 },
            { EOrderStatus.Cancelled, 99 }
        };

        // Cannot change from final states
        if (currentStatus == EOrderStatus.Delivered)
        {
            throw new Exception("Không thể thay đổi trạng thái đơn hàng đã giao thành công!");
        }

        if (currentStatus == EOrderStatus.Cancelled)
        {
            throw new Exception("Không thể thay đổi trạng thái đơn hàng đã hủy!");
        }

        // Must move forward
        if (statusOrder[newStatus] <= statusOrder[currentStatus])
        {
            throw new Exception("Không thể chuyển trạng thái đơn hàng lùi về!");
        }

        return new ApiResponse { Status = StatusCodes.Status200OK };
    }

    private async Task<ApiResponse> UpdateOrderStatusAsync(
        Domain.Entities.Orders order,
        EOrderStatus newStatus)
    {
        var transactionResult = await _unitOfWork.BeginTransactionAsync();
        if (!transactionResult.IsSuccess)
        {
            return new ApiResponse
            {
                Status = StatusCodes.Status500InternalServerError,
                Message = "Không thể bắt đầu transaction!"
            };
        }

        try
        {
            var oldStatus = order.OrderStatus;
            order.OrderStatus = newStatus;
            order.LastModifiedDate = DateTime.UtcNow;

            _unitOfWork.GetRepository<Domain.Entities.Orders>().UpdateAsync(order);

            // Create order history
            var history = new OrderHistoryStatus
            {
                Id = Guid.CreateVersion7(),
                OrderId = order.Id,
                FromStatus = oldStatus,
                ToStatus = newStatus,
                Note =
                    $"Admin cập nhật trạng thái: {GetStatusDisplayName(oldStatus)} → {GetStatusDisplayName(newStatus)}",
                LastModifiedDate = DateTime.UtcNow
            };
            await _unitOfWork.GetRepository<OrderHistoryStatus>().InsertAsync(history);

            var commitResult = await _unitOfWork.CommitTransactionAsync();
            if (!commitResult.IsSuccess)
            {
                _logger.Error("Failed to commit status update: {Message}", commitResult.Message);
                throw new Exception($"Không thể cập nhật trạng thái: {commitResult.Message}");
            }

            _logger.Information(
                "Admin updated order status - Order: {OrderCode}, From: {OldStatus} → To: {NewStatus}",
                order.Code, oldStatus, newStatus);

            return new ApiResponse
            {
                Status = StatusCodes.Status200OK,
                Message = "Cập nhật trạng thái đơn hàng thành công!",
                Data = order.Id,
            };
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.Error(ex, "Error updating order status for {OrderCode}", order.Code);
            throw new Exception(ex.Message);
        }
    }

    #endregion

    #region Cancel Order

    private async Task<ApiResponse> CancelOrderAsync(
        Domain.Entities.Orders order,
        UpdateOrderCommand request,
        Guid userId,
        ERole role,
        CancellationToken cancellationToken)
    {
        // Validate cancel rules based on role
        if (role == ERole.EndCustomer)
        {
            // Customer can only cancel when Pending
            if (order.OrderStatus != EOrderStatus.Pending)
            {
                throw new Exception("Chỉ có thể hủy đơn hàng khi đang ở trạng thái Chờ xử lý!");
            }
        }
        else if (role == ERole.BrandAdmin)
        {
            // Admin can cancel Processing or Shipped
            if (order.OrderStatus != EOrderStatus.Processing &&
                order.OrderStatus != EOrderStatus.Shipped)
            {
                throw new Exception("Chỉ có thể hủy đơn hàng khi đang ở trạng thái Đang xử lý hoặc Đang giao hàng!");
            }
        }

        // Validate cancel reason
        if (string.IsNullOrWhiteSpace(request.CancelReason))
        {
            throw new BadHttpRequestException("Vui lòng nhập lý do hủy đơn hàng!");
        }

        var transactionResult = await _unitOfWork.BeginTransactionAsync();
        if (!transactionResult.IsSuccess)
        {
            throw new Exception("Không thể bắt đầu transaction!");
        }

        try
        {
            var oldStatus = order.OrderStatus;

            // Update order
            order.OrderStatus = EOrderStatus.Cancelled;
            order.CancelledBy = userId;
            order.CancelledByRole = role;
            order.CancelReason = request.CancelReason;
            order.CancelledAt = DateTime.UtcNow;
            order.LastModifiedDate = DateTime.UtcNow;

            _unitOfWork.GetRepository<Domain.Entities.Orders>().UpdateAsync(order);

            // Restore stock — chỉ hoàn lại stock cho non-gift items
            foreach (var detail in order.OrderDetails.Where(d => !d.IsGiftItem))
            {
                var product = await _unitOfWork.GetRepository<Domain.Entities.Products>()
                    .SingleOrDefaultAsync(predicate: x => x.Id == detail.ProductId);

                if (product != null)
                {
                    product.StockQuantity += detail.Quantity;
                    _unitOfWork.GetRepository<Domain.Entities.Products>().UpdateAsync(product);

                    _logger.Information(
                        "Restored stock - Product: {ProductId}, Quantity: +{Quantity}",
                        product.Id, detail.Quantity);
                }
            }

            // Create order history
            var history = new OrderHistoryStatus
            {
                Id = Guid.CreateVersion7(),
                OrderId = order.Id,
                FromStatus = oldStatus,
                ToStatus = EOrderStatus.Cancelled,
                Note = $"{(role == ERole.EndCustomer ? "Khách hàng" : "Admin")} hủy đơn hàng: {request.CancelReason}",
                LastModifiedDate = DateTime.UtcNow
            };
            await _unitOfWork.GetRepository<OrderHistoryStatus>().InsertAsync(history);

            // Create refund request if order was paid
            RefundRequests? refundRequest = null;
            if (order.PaymentStatus == EPaymentStatus.Completed)
            {
                refundRequest = await _refundService.CreateRefundRequestAsync(
                    order,
                    userId,
                    role,
                    request.CancelReason,
                    cancellationToken
                );

                _logger.Information(
                    "Created refund request - RefundId: {RefundId}, Mode: {Mode}",
                    refundRequest.Id, refundRequest.Mode);
            }

            var commitResult = await _unitOfWork.CommitTransactionAsync();
            if (!commitResult.IsSuccess)
            {
                _logger.Error("Failed to commit cancel order: {Message}", commitResult.Message);
                throw new Exception($"Không thể hủy đơn hàng: {commitResult.Message}");
            }

            _logger.Information(
                "Order cancelled - Order: {OrderCode}, By: {Role}, Reason: {Reason}",
                order.Code, role, request.CancelReason);

            var message = order.PaymentStatus == EPaymentStatus.Completed
                ? "Đơn hàng đã được hủy. Yêu cầu hoàn tiền đang được xử lý."
                : "Đơn hàng đã được hủy thành công!";

            return new ApiResponse
            {
                Status = StatusCodes.Status200OK,
                Message = message,
                Data = order.Id,
            };
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.Error(ex, "Error cancelling order {OrderCode}", order.Code);
            throw new Exception(ex.Message);
        }
    }

    #endregion

    #region Helper Methods

    private string GetStatusDisplayName(EOrderStatus status)
    {
        return status switch
        {
            EOrderStatus.WaitingPayment => "Chờ thanh toán",
            EOrderStatus.Pending => "Chờ xử lý",
            EOrderStatus.Processing => "Đang xử lý",
            EOrderStatus.Shipped => "Đang giao hàng",
            EOrderStatus.Delivered => "Đã giao hàng",
            EOrderStatus.Cancelled => "Đã hủy",
            _ => status.ToString()
        };
    }

    #endregion
}
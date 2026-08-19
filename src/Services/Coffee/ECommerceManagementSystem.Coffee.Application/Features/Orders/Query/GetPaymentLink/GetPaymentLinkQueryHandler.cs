using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ECommerceManagementSystem.Coffee.Application.Features.Orders.Query.GetPaymentLink;

public class GetPaymentLinkQueryHandler : IRequestHandler<GetPaymentLinkQuery, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly IPaymentGatewayFactory _paymentGatewayFactory;
    private readonly IClaimService _claimService;
    private readonly ILogger _logger;

    public GetPaymentLinkQueryHandler(
        IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        IPaymentGatewayFactory paymentGatewayFactory,
        IClaimService claimService,
        ILogger logger)
    {
        _unitOfWork = unitOfWork;
        _paymentGatewayFactory = paymentGatewayFactory;
        _claimService = claimService;
        _logger = logger;
    }

    public async ValueTask<ApiResponse> Handle(GetPaymentLinkQuery request, CancellationToken cancellationToken)
    {
        var role = _claimService.GetCurrentRoleEnum();
        var userId = _claimService.GetCurrentAccountId();
        var referenceId = _claimService.GetCurrentReferenceId();

        if (role != ERole.EndCustomer || userId == Guid.Empty)
        {
            return new ApiResponse
            {
                Status = StatusCodes.Status401Unauthorized,
                Message = "Bạn chưa đăng nhập!"
            };
        }

        // Get order with necessary includes
        var order = await _unitOfWork.GetRepository<Domain.Entities.Orders>()
            .SingleOrDefaultAsync(
                predicate: x => x.Id == request.OrderId,
                include: i => i.Include(x => x.OrderDetails)
                    .Include(x => x.Customer)
            );

        if (order == null)
        {
            return new ApiResponse
            {
                Status = StatusCodes.Status404NotFound,
                Message = "Không tìm thấy đơn hàng!"
            };
        }

        // Check ownership
        if (order.CustomerId != referenceId)
        {
            return new ApiResponse
            {
                Status = StatusCodes.Status403Forbidden,
                Message = "Bạn không có quyền truy cập đơn hàng này!"
            };
        }

        // Check order status
        if (order.OrderStatus != EOrderStatus.WaitingPayment)
        {
            return new ApiResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Message = "Đơn hàng không ở trạng thái chờ thanh toán!"
            };
        }

        // Check payment status
        if (order.PaymentStatus == EPaymentStatus.Completed)
        {
            return new ApiResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Message = "Đơn hàng đã được thanh toán!"
            };
        }

        // Get payment info
        var payment = await _unitOfWork.GetRepository<Domain.Entities.Payments>()
            .SingleOrDefaultAsync(predicate: x => x.OrderId == order.Id);

        if (payment == null)
        {
            return new ApiResponse
            {
                Status = StatusCodes.Status404NotFound,
                Message = "Không tìm thấy thông tin thanh toán!"
            };
        }

        // Get payment method configuration
        var brandPaymentMethod = await _unitOfWork.GetRepository<BrandPaymentMethods>()
            .SingleOrDefaultAsync(
                predicate: x => x.PaymentMethodId == payment.PaymentMethodId,
                include: i => i.Include(x => x.PaymentMethods)
            );

        if (brandPaymentMethod == null)
        {
            return new ApiResponse
            {
                Status = StatusCodes.Status404NotFound,
                Message = "Không tìm thấy cấu hình phương thức thanh toán!"
            };
        }

        // Check if payment method is COD (no payment link needed)
        if (brandPaymentMethod.PaymentMethods.Code.Equals("PayInCash", StringComparison.OrdinalIgnoreCase))
        {
            return new ApiResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Message = "Đơn hàng thanh toán khi nhận hàng không cần link thanh toán!"
            };
        }

        // If payment URL exists and QR code exists, return existing link
        // (PayOS links are valid for 15 minutes from creation)
        if (!string.IsNullOrEmpty(order.PaymentUrl) && !string.IsNullOrEmpty(order.QrCode))
        {
            _logger.Information(
                "Returning existing payment link for order {OrderCode}",
                order.Code);

            return new ApiResponse
            {
                Status = StatusCodes.Status200OK,
                Message = "Lấy link thanh toán thành công!",
                Data = new
                {
                    OrderId = order.Id,
                    OrderCode = order.Code,
                    PaymentUrl = order.PaymentUrl,
                    QrCode = order.QrCode,
                    TotalAmount = order.TotalAmount
                }
            };
        }

        // Payment link expired or doesn't exist - create new one
        try
        {
            var gateway = _paymentGatewayFactory.GetGatewayByBrandPaymentMethod(brandPaymentMethod);

            var paymentResult = await gateway.CreatePaymentAsync(
                order,
                payment,
                brandPaymentMethod.Configuration ?? "{}",
                cancellationToken);

            if (!paymentResult.Success)
            {
                _logger.Error(
                    "Failed to create payment link for order {OrderCode}: {Error}",
                    order.Code, paymentResult.ErrorMessage);

                return new ApiResponse
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Message = $"Không thể tạo link thanh toán: {paymentResult.ErrorMessage}"
                };
            }

            // Update order with new payment link
            order.PaymentUrl = paymentResult.PaymentUrl;
            order.QrCode = paymentResult.QrCode;
            order.LastModifiedDate = DateTime.UtcNow;

            // Update payment transaction ID if returned
            if (!string.IsNullOrEmpty(paymentResult.TransactionId))
            {
                payment.TransactionId = paymentResult.TransactionId;
                payment.LastModifiedDate = DateTime.UtcNow;
                _unitOfWork.GetRepository<Domain.Entities.Payments>().UpdateAsync(payment);
            }

            _unitOfWork.GetRepository<Domain.Entities.Orders>().UpdateAsync(order);
            await _unitOfWork.CommitAsync();

            _logger.Information(
                "Created new payment link for order {OrderCode}",
                order.Code);

            return new ApiResponse
            {
                Status = StatusCodes.Status200OK,
                Message = "Tạo link thanh toán mới thành công!",
                Data = new
                {
                    OrderId = order.Id,
                    OrderCode = order.Code,
                    PaymentUrl = paymentResult.PaymentUrl,
                    QrCode = paymentResult.QrCode,
                    TotalAmount = order.TotalAmount
                }
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error creating payment link for order {OrderCode}", order.Code);
            return new ApiResponse
            {
                Status = StatusCodes.Status500InternalServerError,
                Message = "Đã xảy ra lỗi khi tạo link thanh toán!"
            };
        }
    }
}
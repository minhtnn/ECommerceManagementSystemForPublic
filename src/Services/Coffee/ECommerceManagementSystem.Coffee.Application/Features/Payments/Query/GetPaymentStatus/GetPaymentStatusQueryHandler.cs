using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.Payment;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using ECommerceManagementSystem.Coffee.Infrastructure.Utils;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ECommerceManagementSystem.Coffee.Application.Features.Payments.Query.GetPaymentStatus;

public class GetPaymentStatusQueryHandler : IRequestHandler<GetPaymentStatusQuery, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;
    private readonly IClaimService _claimService;

    public GetPaymentStatusQueryHandler(
        IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ILogger logger,
        IClaimService claimService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _claimService = claimService;
    }

    public async ValueTask<ApiResponse> Handle(GetPaymentStatusQuery request, CancellationToken cancellationToken)
    {
        // 1. Get current user info
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

        // 2. Get order with payment
        var order = await _unitOfWork.GetRepository<Domain.Entities.Orders>()
            .SingleOrDefaultAsync(
                predicate: x => x.Id == request.OrderId,
                include: i => i.Include(x => x.Payments)
            );

        if (order == null)
        {
            return new ApiResponse()
            {
                Status = StatusCodes.Status404NotFound,
                Message = "Không tìm thấy đơn hàng!"
            };
        }

        // 3. Verify ownership
        if (order.CustomerId != customerId)
        {
            _logger.Warning(
                "Unauthorized payment status check: OrderId={OrderId}, CustomerId={CustomerId}",
                request.OrderId, customerId);

            return new ApiResponse()
            {
                Status = StatusCodes.Status403Forbidden,
                Message = "Bạn không có quyền truy cập đơn hàng này!"
            };
        }

        // 4. Get payment info
        var payment = order.Payments?.FirstOrDefault();
        
        if (payment == null)
        {
            return new ApiResponse()
            {
                Status = StatusCodes.Status404NotFound,
                Message = "Không tìm thấy thông tin thanh toán!"
            };
        }

        // 5. Build response
        var response = new GetPaymentStatusResponse()
        {
            OrderId = order.Id,
            OrderCode = order.Code,
            OrderStatus = order.OrderStatus,
            PaymentStatus = payment.PaymentStatus,
            Amount = payment.Amount,
            TransactionId = payment.TransactionId,
            PaidAt = payment.PaidAt.HasValue? TimeUtil.ConvertFromUtc(payment.PaidAt.Value, request.TimeZone):null,
            CreatedDate = TimeUtil.ConvertFromUtc(payment.CreatedDate, request.TimeZone)
        };

        _logger.Information(
            "Payment status checked: OrderId={OrderId}, Status={Status}",
            request.OrderId, payment.PaymentStatus);

        return new ApiResponse()
        {
            Status = StatusCodes.Status200OK,
            Message = "Lấy trạng thái thanh toán thành công!",
            Data = response
        };
    }
}
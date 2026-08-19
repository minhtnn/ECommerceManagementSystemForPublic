using FluentValidation;
using ECommerceManagementSystem.Coffee.Domain.Enums;

namespace ECommerceManagementSystem.Coffee.Application.Features.Orders.Command.UpdateOrder;

public class UpdateOrderCommandValidator : AbstractValidator<UpdateOrderCommand>
{
    public UpdateOrderCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("OrderId không được để trống!");
        
        // When cancelling, reason is required
        When(x => x.NewOrderStatus == EOrderStatus.Cancelled, () =>
        {
            RuleFor(x => x.CancelReason)
                .NotEmpty()
                .WithMessage("Vui lòng nhập lý do hủy đơn hàng!")
                .MaximumLength(500)
                .WithMessage("Lý do hủy không được quá 500 ký tự!");
        });
        
        // Shipping address validation
        When(x => !string.IsNullOrEmpty(x.ShippingAddress), () =>
        {
            RuleFor(x => x.ShippingAddress)
                .MaximumLength(500)
                .WithMessage("Địa chỉ giao hàng không được quá 500 ký tự!");
        });
        
        // Shipping contact validation
        When(x => !string.IsNullOrEmpty(x.ShippingContact), () =>
        {
            RuleFor(x => x.ShippingContact)
                .NotEmpty()
                .WithMessage("Số điện thoại không được để trống!")
                .MaximumLength(20)
                .WithMessage("Số điện thoại không được quá 20 ký tự!")
                .Matches(@"^[0-9+\-\s()]+$")
                .WithMessage("Số điện thoại không hợp lệ!");
        });
        
        // Customer note validation
        When(x => !string.IsNullOrEmpty(x.CustomerNote), () =>
        {
            RuleFor(x => x.CustomerNote)
                .MaximumLength(500)
                .WithMessage("Ghi chú không được quá 500 ký tự!");
        });
    }
}
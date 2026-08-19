using FluentValidation;

namespace ECommerceManagementSystem.Coffee.Application.Features.Orders.Command.CreateOrder;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {

        RuleFor(x => x.BrandPaymentMethodId)
            .NotEmpty()
            .WithMessage("BrandPaymentMethodId không được để trống!");

        RuleFor(x => x.ShippingAddress)
            .NotEmpty()
            .WithMessage("Địa chỉ giao hàng không được để trống!")
            .MaximumLength(500)
            .WithMessage("Địa chỉ giao hàng không được quá 500 ký tự!");

        RuleFor(x => x.ShippingContact)
            .NotEmpty()
            .WithMessage("Số điện thoại liên hệ không được để trống!")
            .MaximumLength(20)
            .WithMessage("Số điện thoại không được quá 20 ký tự!");

        RuleFor(x => x.CustomerNote)
            .MaximumLength(500)
            .WithMessage("Ghi chú không được quá 500 ký tự!");
    }
}
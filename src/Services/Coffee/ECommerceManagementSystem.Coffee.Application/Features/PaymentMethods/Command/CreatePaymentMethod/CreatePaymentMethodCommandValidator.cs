using FluentValidation;

namespace ECommerceManagementSystem.Coffee.Application.Features.PaymentMethods.Command.CreatePaymentMethod;

public class CreatePaymentMethodCommandValidator : AbstractValidator<CreatePaymentMethodCommand>
{
    public CreatePaymentMethodCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Mã phương thức thanh toán không được để trống!");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Tên phương thức thanh toán không được để trống!");
        // RuleFor(x => x.Status).NotEmpty().WithMessage("Trạng thái phương thức thanh toán không được để trống!");
    }
}
using ECommerceManagementSystem.Coffee.Application.Features.PaymentMethods.Command.CreatePaymentMethod;
using FluentValidation;

namespace ECommerceManagementSystem.Coffee.Application.Features.PaymentMethods.Command.UpdatePaymentMethod;

public class UpdatePaymentMethodCommandValidator : AbstractValidator<UpdatePaymentMethodCommand>
{
    public UpdatePaymentMethodCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Tên phương thức thanh toán không được để trống!");
        // RuleFor(x => x.Status).NotEmpty().WithMessage("Trạng thái phương thức thanh toán không được để trống!");
    }
}
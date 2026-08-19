using FluentValidation;

namespace ECommerceManagementSystem.Coffee.Application.Features.PaymentMethods.Command.UpdateBrandPaymentMethod;

public class UpdateBrandPaymentMethodCommandValidator : AbstractValidator<UpdateBrandPaymentMethodCommand>
{
    public UpdateBrandPaymentMethodCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("ID không được để trống!");

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Thứ tự hiển thị phải >= 0!");

        RuleFor(x => x.Configuration)
            .MaximumLength(4000)
            .WithMessage("Configuration quá dài (tối đa 4000 ký tự)!");
    }
}
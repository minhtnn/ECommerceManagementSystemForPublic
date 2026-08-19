using FluentValidation;

namespace ECommerceManagementSystem.Coffee.Application.Features.PaymentMethods.Command.CreateBrandPaymentMethod;

public class CreateBrandPaymentMethodCommandValidator : AbstractValidator<CreateBrandPaymentMethodCommand>
{
    public CreateBrandPaymentMethodCommandValidator()
    {
        RuleFor(x => x.PaymentMethodId)
            .NotEmpty()
            .WithMessage("ID phương thức thanh toán không được để trống!");

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Thứ tự hiển thị phải >= 0!")
            .LessThanOrEqualTo(1000)
            .WithMessage("Thứ tự hiển thị không được vượt quá 1000!");

        RuleFor(x => x.Configuration)
            .MaximumLength(4000)
            .WithMessage("Configuration quá dài (tối đa 4000 ký tự)!")
            .Must(BeValidJsonOrNull)
            .WithMessage("Configuration phải là JSON hợp lệ hoặc để trống!");
    }

    // Helper method để validate JSON
    private bool BeValidJsonOrNull(string? configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration))
        {
            return true;
        }

        try
        {
            System.Text.Json.JsonDocument.Parse(configuration);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
using FluentValidation;

namespace ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.ForgotPassword;

public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống!")
            .EmailAddress().WithMessage("Email không hợp lệ!")
            .MaximumLength(100).WithMessage("Email không được vượt quá 100 ký tự!");

        RuleFor(x => x.BrandCode)
            .NotEmpty().WithMessage("Mã thương hiệu không được để trống!")
            .MaximumLength(50).WithMessage("Mã thương hiệu không được vượt quá 50 ký tự!");
    }
}
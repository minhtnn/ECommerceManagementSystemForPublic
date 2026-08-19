using FluentValidation;

namespace ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.ResendOTP.Email;

public class ResendEmailOTPCommandValidator : AbstractValidator<ResendEmailOTPCommand>
{
    public ResendEmailOTPCommandValidator()
    {
        RuleFor(command => command.BrandCode)
            .NotEmpty().WithMessage("BrandCode không được bỏ trống!");
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage("Email không được bỏ trống!")
            .EmailAddress().WithMessage("Email không hợp lệ!");
        ;
    }
}
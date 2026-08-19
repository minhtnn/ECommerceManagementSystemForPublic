using FluentValidation;

namespace ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.Login;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.BrandCode)
            .NotEmpty().WithMessage("BrandCode không được để trống");
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Username) || !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Vui lòng nhập tên đăng nhập hoặc email");
        When(x => !string.IsNullOrWhiteSpace(x.Username), () =>
        {
            RuleFor(x => x.Username)
                .MinimumLength(3).WithMessage("Username phải có ít nhất 3 ký tự")
                .MaximumLength(50).WithMessage("Username không được vượt quá 50 ký tự");
        });
        When(x => !string.IsNullOrWhiteSpace(x.Email), () =>
        {
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Email không đúng định dạng")
                .MaximumLength(100).WithMessage("Email không được vượt quá 100 ký tự");
        });
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống");
    }
}
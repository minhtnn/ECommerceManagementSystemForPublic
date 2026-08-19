using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using FluentValidation;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.CreateAccount;

public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được bỏ trống")
            .EmailAddress()
            .MaximumLength(100).WithMessage("Email không được quá 100 kí tự");
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Tên đăng nhập không được bỏ trống")
            .MinimumLength(2).WithMessage("Tên đăng nhập không được ít hơn 2 kí tự")
            .MaximumLength(20).WithMessage("Tên đăng nhập không được quá 20 kí tự");
        RuleFor(x => x.PasswordString)
            .NotEmpty().WithMessage("Mật khẩu không được bỏ trống")
            .MinimumLength(2).WithMessage("Mật khẩu không được ít hơn 2 kí tự")
            .MaximumLength(100).WithMessage("Mật khẩu không được quá 100 kí tự");
    }
}
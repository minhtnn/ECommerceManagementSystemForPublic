using FluentValidation;

namespace ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.ValidateResetToken;

public class ValidateResetTokenCommandValidator : AbstractValidator<ValidateResetTokenCommand>
{
    public ValidateResetTokenCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống!")
            .EmailAddress().WithMessage("Email không hợp lệ!");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token không được để trống!")
            .MinimumLength(32).WithMessage("Token không hợp lệ!");
        
        RuleFor(x => x.BrandCode)          // thêm mới
            .NotEmpty().WithMessage("Mã thương hiệu không được để trống!")
            .MaximumLength(50).WithMessage("Mã thương hiệu không được vượt quá 50 ký tự!");
    }
}
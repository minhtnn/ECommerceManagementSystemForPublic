using FluentValidation;

namespace ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.ResetPassword;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống!")
            .EmailAddress().WithMessage("Email không hợp lệ!");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token không được để trống!")
            .MinimumLength(32).WithMessage("Token không hợp lệ!");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Mật khẩu mới không được để trống!")
            .MinimumLength(8).WithMessage("Mật khẩu phải có ít nhất 8 ký tự!")
            .Matches(@"[A-Z]").WithMessage("Mật khẩu phải chứa ít nhất 1 chữ hoa!")
            .Matches(@"[a-z]").WithMessage("Mật khẩu phải chứa ít nhất 1 chữ thường!")
            .Matches(@"[0-9]").WithMessage("Mật khẩu phải chứa ít nhất 1 số!")
            .Matches(@"[!@#$%^&*(),.?""':{}|<>]").WithMessage("Mật khẩu phải chứa ít nhất 1 ký tự đặc biệt!");

        RuleFor(x => x.ConfirmNewPassword)
            .NotEmpty().WithMessage("Xác nhận mật khẩu không được để trống!")
            .Equal(x => x.NewPassword).WithMessage("Mật khẩu xác nhận không khớp!");
        RuleFor(x => x.BrandCode)          // thêm mới
            .NotEmpty().WithMessage("Mã thương hiệu không được để trống!")
            .MaximumLength(50).WithMessage("Mã thương hiệu không được vượt quá 50 ký tự!");
    }
}
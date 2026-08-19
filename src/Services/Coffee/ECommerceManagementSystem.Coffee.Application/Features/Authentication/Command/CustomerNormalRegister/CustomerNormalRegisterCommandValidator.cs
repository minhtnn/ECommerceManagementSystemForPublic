using ECommerceManagementSystem.Coffee.Application.Common.Utils;
using FluentValidation;

namespace ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.CustomerNormalRegister;

public class CustomerNormalRegisterCommandValidator : AbstractValidator<CustomerNormalRegisterCommand>
{
    public CustomerNormalRegisterCommandValidator()
    {
        RuleFor(x => x.BrandCode)
            .NotEmpty().WithMessage("BrandCode không được bỏ trống");
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được bỏ trống")
            .EmailAddress().WithMessage("Email không đúng định dạng");
        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).WithMessage("Số điện thoại không vượt quá 20 kí tự");
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Tên đăng nhập không được bỏ trống")
            .MaximumLength(20).WithMessage("Tên đăng nhập không vượt quá 20 ký tự")
            .MinimumLength(2).WithMessage("Tên đăng nhập không ít hơn 2 kí tự");
        RuleFor(x => x.PasswordString)
            .NotEmpty().WithMessage("Mật khẩu không được bỏ trống")
            .MinimumLength(2).WithMessage("Mật khẩu không ít hơn 2 kí tự");
        RuleFor(x => x.Avatar)
            .Must(file => ImageUtil.IsValidImageFile(file))
            .WithMessage("Chỉ cho phép ảnh jpg, jpeg, png, webp với kích thước 5MB");

    }
}
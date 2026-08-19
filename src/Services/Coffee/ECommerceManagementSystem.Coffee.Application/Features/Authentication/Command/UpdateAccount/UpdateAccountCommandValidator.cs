using FluentValidation;

namespace ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.UpdateAccount;

public class UpdateAccountCommandValidator : AbstractValidator<UpdateAccountCommand>
{
    public UpdateAccountCommandValidator()
    {
        RuleFor(x => x.BrandCode)
            .NotEmpty().WithMessage("Mã thương hiệu không được để trống!")
            .MaximumLength(100).WithMessage("Mã thương hiệu không được dài quá 100 ký tự!")
            .MinimumLength(2).WithMessage("Mã thương hiệu phải có ít nhất 2 ký tự!");

        RuleFor(x => x.Name)
            .MaximumLength(200).WithMessage("Tên không được dài quá 200 ký tự!")
            .MinimumLength(2).WithMessage("Tên phải có ít nhất 2 ký tự!")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.FullName)
            .MaximumLength(200).WithMessage("Họ và tên không được dài quá 200 ký tự!")
            .MinimumLength(2).WithMessage("Họ và tên phải có ít nhất 2 ký tự!")
            .When(x => !string.IsNullOrEmpty(x.FullName));

        // RuleFor(x => x.Username)
        //     .MaximumLength(100).WithMessage("Tên đăng nhập không được dài quá 100 ký tự!")
        //     .MinimumLength(3).WithMessage("Tên đăng nhập phải có ít nhất 3 ký tự!")
        //     .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Tên đăng nhập chỉ được chứa chữ cái, số và dấu gạch dưới!")
        //     .When(x => !string.IsNullOrEmpty(x.Username));

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(15).WithMessage("Số điện thoại không được dài quá 15 ký tự!")
            .Matches(@"^[0-9+\-\s]+$").WithMessage("Số điện thoại không hợp lệ!")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Email không hợp lệ!")
            .MaximumLength(256).WithMessage("Email không được dài quá 256 ký tự!")
            .When(x => !string.IsNullOrEmpty(x.Email));
        
        RuleFor(x => x.Address)
            .MaximumLength(200).WithMessage("Địa chỉ của thương hiệu không vượt quá 200 ký tự")
            .When(x => !string.IsNullOrEmpty(x.Address));
    }
}
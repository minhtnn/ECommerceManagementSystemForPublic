using FluentValidation;

namespace ECommerceManagementSystem.Coffee.Application.Features.Customers.Command.SendCustomerConsult;

public class SendCustomerConsultCommandValidator : AbstractValidator<SendCustomerConsultCommand>
{
    public SendCustomerConsultCommandValidator()
    {
        RuleFor(x => x.CustomerFullName)
            .NotEmpty().WithMessage("Họ và tên không được để trống!")
            .MaximumLength(100).WithMessage("Họ và tên không được dài quá 100 ký tự!")
            .MinimumLength(2).WithMessage("Họ và tên không được ít hơn 2 ký tự!");

        RuleFor(x => x.CustomerEmail)
            .NotEmpty().WithMessage("Email không được để trống!")
            .MaximumLength(200).WithMessage("Email không được dài quá 200 ký tự!")
            .EmailAddress().WithMessage("Email không đúng định dạng!");

        RuleFor(x => x.CustomerPhone)
            .NotEmpty().WithMessage("Số điện thoại không được để trống!")
            .MaximumLength(15).WithMessage("Số điện thoại không được dài quá 15 ký tự!")
            .Matches(@"^(\+84|0)[3|5|7|8|9][0-9]{8}$")
            .WithMessage("Số điện thoại không đúng định dạng Việt Nam!");

        RuleFor(x => x.CustomerMessage)
            .NotEmpty().WithMessage("Nội dung yêu cầu không được để trống!")
            .MaximumLength(2000).WithMessage("Nội dung yêu cầu không được dài quá 2000 ký tự!")
            .MinimumLength(10).WithMessage("Nội dung yêu cầu không được ít hơn 10 ký tự!");

        RuleFor(x => x.BrandCode)
            .NotEmpty().WithMessage("Mã thương hiệu không được để trống!")
            .MaximumLength(100).WithMessage("Mã thương hiệu không được dài quá 100 ký tự!");
    }
}
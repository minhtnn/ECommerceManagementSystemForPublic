// UpdateCustomerAddressCommandValidator.cs
using FluentValidation;

namespace ECommerceManagementSystem.Coffee.Application.Features.CustomerAddresses.Command.UpdateCustomerAddress;

public class UpdateCustomerAddressCommandValidator : AbstractValidator<UpdateCustomerAddressCommand>
{
    public UpdateCustomerAddressCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id địa chỉ không được để trống!");

        RuleFor(x => x.Receiver)
            .NotEmpty().WithMessage("Tên người nhận không được để trống!")
            .MinimumLength(2).WithMessage("Tên người nhận phải có ít nhất 2 ký tự!")
            .MaximumLength(100).WithMessage("Tên người nhận không được dài quá 100 ký tự!");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Địa chỉ không được để trống!")
            .MinimumLength(5).WithMessage("Địa chỉ phải có ít nhất 5 ký tự!")
            .MaximumLength(500).WithMessage("Địa chỉ không được dài quá 500 ký tự!");

        RuleFor(x => x.ShippingContact)
            .NotEmpty().WithMessage("Số điện thoại giao hàng không được để trống!")
            .Matches(@"^(0|\+84)[3|5|7|8|9][0-9]{8}$")
            .WithMessage("Số điện thoại giao hàng không hợp lệ!");
        //
        // RuleFor(x => x.Latitude)
        //     .InclusiveBetween(-90, 90).WithMessage("Vĩ độ phải nằm trong khoảng từ -90 đến 90!");
        //
        // RuleFor(x => x.Longitude)
        //     .InclusiveBetween(-180, 180).WithMessage("Kinh độ phải nằm trong khoảng từ -180 đến 180!");
    }
}
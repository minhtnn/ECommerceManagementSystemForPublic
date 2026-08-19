using FluentValidation;

namespace ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.CustomerGoogleLoginAndRegister;

public class CustomerGoogleLoginAndRegisterCommandValidator : AbstractValidator<CustomerGoogleLoginAndRegisterCommand>
{
    public CustomerGoogleLoginAndRegisterCommandValidator()
    {
        RuleFor(x => x.BrandCode)
            .NotEmpty().WithMessage("Mã thương hiệu không được để trống!");

        RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage("Google ID Token không được để trống!");
    }
}
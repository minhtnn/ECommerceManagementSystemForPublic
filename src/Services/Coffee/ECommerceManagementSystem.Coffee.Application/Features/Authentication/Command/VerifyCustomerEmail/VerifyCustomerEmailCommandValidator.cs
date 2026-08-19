using FluentValidation;

namespace ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.VerifyCustomerEmail;

public class VerifyCustomerEmailCommandValidator : AbstractValidator<VerifyCustomerEmailCommand>
{
    public VerifyCustomerEmailCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage("Email không được để trống!")
            .EmailAddress().WithMessage("Email không hợp lệ!");
    }
}
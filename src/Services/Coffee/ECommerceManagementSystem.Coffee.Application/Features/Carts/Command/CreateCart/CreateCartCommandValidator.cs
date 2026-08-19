using FluentValidation;

namespace ECommerceManagementSystem.Coffee.Application.Features.Carts.Command.CreateCart;

public class CreateCartCommandValidator : AbstractValidator<CreateCartCommand>
{
    public CreateCartCommandValidator()
    {
        // Validate CartId nếu có (optional)
        RuleFor(x => x.CartId)
            .NotEqual(Guid.Empty)
            .WithMessage("CartId không hợp lệ")
            .When(x => x.CartId.HasValue);

        // Validate CartName nếu có
        RuleFor(x => x.CartName)
            .MaximumLength(100)
            .WithMessage("Tên giỏ hàng không quá 100 ký tự")
            .When(x => !string.IsNullOrEmpty(x.CartName));

        // Validate CartName không chứa ký tự đặc biệt
        RuleFor(x => x.CartName)
            .Matches(@"^[a-zA-Z0-9àáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđĐ\s]+$")
            .WithMessage("Tên giỏ hàng không được chứa ký tự đặc biệt")
            .When(x => !string.IsNullOrEmpty(x.CartName));
    }
}
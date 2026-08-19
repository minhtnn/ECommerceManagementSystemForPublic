using FluentValidation;

namespace ECommerceManagementSystem.Coffee.Application.Features.SystemConfigs.Command.UpdateSystemConfig;

public class UpdateSystemConfigCommandValidator : AbstractValidator<UpdateSystemConfigCommand>
{
    public UpdateSystemConfigCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id không được bỏ trống");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Tiêu đề không được bỏ trống")
            .MaximumLength(255).WithMessage("Tiêu đề không vượt quá 255 ký tự");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Mô tả không vượt quá 500 ký tự");

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Thứ tự hiển thị không được âm");

        // Value và ClearValue không được dùng đồng thời
        RuleFor(x => x)
            .Must(x => !(x.ClearValue && !string.IsNullOrWhiteSpace(x.Value)))
            .WithMessage("Không thể vừa set Value vừa ClearValue = true");

        RuleForEach(x => x.Dependencies)
            .ChildRules(dep =>
            {
                dep.RuleFor(x => x.TriggerKeyId)
                    .NotEmpty().WithMessage("TriggerKeyId không được bỏ trống");
                dep.RuleFor(x => x.TriggerValue)
                    .NotEmpty().WithMessage("TriggerValue không được bỏ trống")
                    .MaximumLength(100).WithMessage("TriggerValue không vượt quá 100 ký tự");
            })
            .When(x => x.Dependencies != null && x.Dependencies.Any());
    }
}
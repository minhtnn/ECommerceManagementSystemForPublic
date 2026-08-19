using ECommerceManagementSystem.Coffee.Domain.Enums;
using FluentValidation;

namespace ECommerceManagementSystem.Coffee.Application.Features.SystemConfigs.Command.CreateSystemConfig;

public class CreateSystemConfigCommandValidator : AbstractValidator<CreateSystemConfigCommand>
{
    public CreateSystemConfigCommandValidator()
    {
        RuleFor(x => x.Key)
            .NotEmpty().WithMessage("Key không được bỏ trống")
            .MaximumLength(100).WithMessage("Key không vượt quá 100 ký tự")
            .Matches("^[A-Za-z][A-Za-z0-9_]*$")
            .WithMessage("Key chỉ được chứa chữ cái, số và dấu gạch dưới, không bắt đầu bằng số");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Tiêu đề không được bỏ trống")
            .MaximumLength(255).WithMessage("Tiêu đề không vượt quá 255 ký tự");

        RuleFor(x => x.DataType)
            .IsInEnum().WithMessage("Kiểu dữ liệu không hợp lệ");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Mô tả không vượt quá 500 ký tự");

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Thứ tự hiển thị không được âm");

        RuleFor(x => x.DefaultValue)
            .Must((cmd, val) => BeValidValueForDataType(val, cmd.DataType))
            .When(x => !string.IsNullOrWhiteSpace(x.DefaultValue))
            .WithMessage(x => $"DefaultValue không hợp lệ với kiểu dữ liệu {x.DataType}");

        // Nếu IsRequired = true thì Value hoặc DefaultValue phải có
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Value) || !string.IsNullOrWhiteSpace(x.DefaultValue))
            .When(x => x.IsRequired)
            .WithMessage("Key bắt buộc phải có Value hoặc DefaultValue");

        RuleFor(x => x.Value)
            .Must((cmd, val) => BeValidValueForDataType(val, cmd.DataType))
            .When(x => !string.IsNullOrWhiteSpace(x.Value))
            .WithMessage(x => $"Value không hợp lệ với kiểu dữ liệu {x.DataType}");

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

    private static bool BeValidValueForDataType(string? value, EConfigDataType dataType)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        return dataType switch
        {
            EConfigDataType.Boolean => bool.TryParse(value, out _),
            EConfigDataType.Number  => decimal.TryParse(value, out _),
            EConfigDataType.Json    => IsValidJson(value),
            EConfigDataType.String  => true,
            _                       => false
        };
    }

    private static bool IsValidJson(string value)
    {
        try { System.Text.Json.JsonDocument.Parse(value); return true; }
        catch { return false; }
    }
}
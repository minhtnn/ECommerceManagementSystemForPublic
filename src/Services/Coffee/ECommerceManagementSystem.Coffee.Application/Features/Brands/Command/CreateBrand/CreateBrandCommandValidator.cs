using System.Text.Json;
using ECommerceManagementSystem.Coffee.Domain.Models.Configurations;
using ECommerceManagementSystem.Coffee.Domain.Models.Settings;
using FluentValidation;

namespace ECommerceManagementSystem.Coffee.Application.Features.Brands.Command.CreateBrand;

public class CreateBrandCommandValidator : AbstractValidator<CreateBrandCommand>
{
    private static readonly HashSet<string> _validKeys = typeof(BrandSetting)
        .GetProperties()
        .Select(p => p.Name)
        .ToHashSet();

    public CreateBrandCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Mã thương hiệu không được bỏ trống")
            .MaximumLength(20).WithMessage("Mã thương hiệu không vượt quá 20 ký tự")
            .MinimumLength(2).WithMessage("Mã thương hiệu không ít hơn 2 kí tự");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên thương hiệu không được bỏ trống")
            .MaximumLength(50).WithMessage("Tên thương hiệu không vượt quá 50 ký tự")
            .MinimumLength(2).WithMessage("Tên thương hiệu không ít hơn 2 kí tự");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được bỏ trống")
            .EmailAddress().WithMessage("Email không đúng định dạng");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Địa chỉ không được bỏ trống")
            .MaximumLength(200).WithMessage("Địa chỉ của thương hiệu không vượt quá 200 ký tự");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).WithMessage("Số điện thoại không vượt quá 20 kí tự");

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Tên đăng nhập không được bỏ trống")
            .MaximumLength(20).WithMessage("Tên đăng nhập không vượt quá 20 ký tự")
            .MinimumLength(2).WithMessage("Tên đăng nhập không ít hơn 2 kí tự");

        RuleFor(x => x.PasswordString)
            .NotEmpty().WithMessage("Mật khẩu không được bỏ trống")
            .MinimumLength(2).WithMessage("Mật khẩu không ít hơn 2 kí tự");

        RuleFor(x => x.Configuration)
            .Must(BeValidConfiguration)
            .When(x => !string.IsNullOrWhiteSpace(x.Configuration))
            .WithMessage(x => GetConfigurationError(x.Configuration));
    }

    private static bool BeValidConfiguration(string? configJson)
    {
        if (string.IsNullOrWhiteSpace(configJson)) return true;

        try
        {
            var config = JsonSerializer.Deserialize<BrandConfiguration>(configJson, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (config?.Attributes == null) return false;

            // Kiểm tra tất cả key có hợp lệ không
            return config.Attributes.Keys.All(k => _validKeys.Contains(k));
        }
        catch
        {
            return false;
        }
    }

    private static string GetConfigurationError(string? configJson)
    {
        if (string.IsNullOrWhiteSpace(configJson))
            return "Configuration không hợp lệ";

        try
        {
            var config = JsonSerializer.Deserialize<BrandConfiguration>(configJson, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (config?.Attributes == null)
                return "Configuration không đúng định dạng JSON";

            var invalidKeys = config.Attributes.Keys
                .Where(k => !_validKeys.Contains(k))
                .ToList();

            if (invalidKeys.Any())
                return $"Các key không hợp lệ: [{string.Join(", ", invalidKeys)}]. " +
                       $"Các key hợp lệ: [{string.Join(", ", _validKeys)}]";
        }
        catch
        {
            return "Configuration không đúng định dạng JSON";
        }

        return "Configuration không hợp lệ";
    }
}
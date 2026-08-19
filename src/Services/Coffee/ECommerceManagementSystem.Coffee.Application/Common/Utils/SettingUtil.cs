using System.Reflection;
using System.Text.Json;

namespace ECommerceManagementSystem.Coffee.Application.Common.Utils;

public static class SettingUtil
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Deserialize flat JSON dictionary string thành typed setting object.
    /// Key có trong JSON nhưng không có trong TSetting → bỏ qua.
    /// Property có trong TSetting nhưng không có trong JSON → giữ default (null/false/0).
    /// </summary>
    public static TSetting Parse<TSetting>(string? configurationJson)
        where TSetting : class, new()
    {
        var setting = new TSetting();

        if (string.IsNullOrWhiteSpace(configurationJson))
            return setting;

        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(
                configurationJson, _options
            );

            if (dict == null) return setting;

            var properties = typeof(TSetting).GetProperties(
                BindingFlags.Public | BindingFlags.Instance
            );

            foreach (var prop in properties)
            {
                if (!prop.CanWrite) continue;
                if (!dict.TryGetValue(prop.Name, out var rawValue)) continue;

                var converted = ConvertValue(rawValue, prop.PropertyType);
                if (converted is not null)
                    prop.SetValue(setting, converted);
            }
        }
        catch
        {
            // Configuration malformed → trả về default setting
        }

        return setting;
    }

    /// <summary>
    /// Serialize typed setting object thành flat JSON dictionary string.
    /// Chỉ serialize các property có value khác null.
    /// </summary>
    public static string Serialize<TSetting>(TSetting setting)
        where TSetting : class
    {
        var dict = new Dictionary<string, string>();

        var properties = typeof(TSetting).GetProperties(
            BindingFlags.Public | BindingFlags.Instance
        );

        foreach (var prop in properties)
        {
            if (!prop.CanRead) continue;

            var value = prop.GetValue(setting);
            if (value is null) continue;

            dict[prop.Name] = value.ToString()!;
        }

        return JsonSerializer.Serialize(dict, _options);
    }

    private static object? ConvertValue(string raw, Type targetType)
    {
        try
        {
            var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (underlying == typeof(bool))
                return bool.TryParse(raw, out var b) ? b : null;

            if (underlying == typeof(int))
                return int.TryParse(raw, out var i) ? i : null;

            if (underlying == typeof(long))
                return long.TryParse(raw, out var l) ? l : null;

            if (underlying == typeof(double))
                return double.TryParse(raw, out var d) ? d : null;

            if (underlying == typeof(decimal))
                return decimal.TryParse(raw, out var m) ? m : null;

            if (underlying == typeof(Guid))
                return Guid.TryParse(raw, out var g) ? g : null;

            if (underlying == typeof(string))
                return raw;

            return null;
        }
        catch
        {
            return null;
        }
    }
}
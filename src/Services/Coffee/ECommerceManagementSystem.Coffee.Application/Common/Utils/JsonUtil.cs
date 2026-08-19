using System.Text.Json;

namespace ECommerceManagementSystem.Coffee.Application.Common.Utils;

public static class JsonUtil
{
    private static readonly JsonSerializerOptions _snakeCaseOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static IResult Json(object data)
    {
        return Results.Json(data, _snakeCaseOptions, statusCode:200);
    }

    public static string JsonString(object data)
    {
        return JsonSerializer.Serialize(data, _snakeCaseOptions);
    }
}
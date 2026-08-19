using Microsoft.AspNetCore.Builder;
using Scalar.AspNetCore;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Configurations;

public static class ScalarConfig
{
    public static void UseScalar(this WebApplication app)
    {

        app.MapOpenApi();
        app.MapScalarApiReference(options =>
            {
                options.EndpointPathPrefix = "/api/{documentName}";
                options.Theme = ScalarTheme.DeepSpace;
                options.Favicon = "/assets/images/logo.png";
            })
            .RequireAuthorization(options =>
            {
                options.RequireAssertion(context =>
                {
                    return true;
                });
            });
    }
}
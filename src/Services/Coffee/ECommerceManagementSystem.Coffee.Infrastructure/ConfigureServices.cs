using System.Security.Claims;
using ECommerceManagementSystem.Coffee.Domain.Constants;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Extensions;

namespace ECommerceManagementSystem.Coffee.Infrastructure;

public static class ConfigureServices
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ECommerceManagementSystemCoffeeContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnectionString"),
                builder => builder.MigrationsAssembly(typeof(ECommerceManagementSystemCoffeeContext).Assembly
                    .FullName));
        });
        services
            .AddScoped<IUnitOfWork<ECommerceManagementSystemCoffeeContext>,
                UnitOfWork<ECommerceManagementSystemCoffeeContext>>();
        services.AddScoped<ECommerceManagementSystemCoffeeContextSeed>();
        services.AddJWT(configuration);
        services.AddOpenApiConfig();
        services.AddHttpContextAccessor();
        services.AddAuthorization(options =>
            {
                options.AddPolicy(EPolicy.BrandPolicy.GetDisplayName(), policy =>
                    policy.RequireAuthenticatedUser().RequireRole(ClaimTypes.Role, ERole.BrandAdmin.GetDisplayName()));
                options.AddPolicy(EPolicy.SystemPolicy.GetDisplayName(), policy =>
                    policy.RequireAuthenticatedUser().RequireRole(ClaimTypes.Role, ERole.SystemAdmin.GetDisplayName()));
                options.AddPolicy(EPolicy.SystemOrBrandPolicy.GetDisplayName(), policy =>
                    policy.RequireAuthenticatedUser().RequireRole(ClaimTypes.Role, ERole.SystemAdmin.GetDisplayName(),
                        ERole.BrandAdmin.GetDisplayName()));
                options.AddPolicy(EPolicy.EndCustomerPolicy.GetDisplayName(), policy =>
                    policy.RequireAuthenticatedUser().RequireRole(ClaimTypes.Role, ERole.EndCustomer.GetDisplayName()));
                options.AddPolicy(EPolicy.BrandOrEndCustomerPolicy.GetDisplayName(), policy =>
                    policy.RequireAuthenticatedUser().RequireRole(ClaimTypes.Role, ERole.BrandAdmin.GetDisplayName(),
                        ERole.EndCustomer.GetDisplayName()));
            }
        );
        services.AddEndpointsApiExplorer();
        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicy.AllowFrontend, policy =>
            {
                policy.WithOrigins("http://localhost:3000", "https://ecommerce.reso.vn",
                        "https://ecommerce-resouni-web.web.app", "https://unicoffeeroastery.vn")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .SetIsOriginAllowed(origin => true);
            });
            options.AddPolicy(CorsPolicy.AllowPublic, policy =>
            {
                policy.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });
        
        services.AddFirebase(configuration);
        return services;
    }
    
    private static IServiceCollection AddFirebase(this IServiceCollection services, IConfiguration configuration)
    {
        if (FirebaseApp.DefaultInstance == null)
        {
            // Lấy toàn bộ section "Firebase:ServiceAccountKey" rồi serialize lại thành JSON string
            var section = configuration.GetSection("Firebase:ServiceAccountKey");
        
            if (!section.Exists())
                throw new InvalidOperationException("Firebase:ServiceAccountKey is missing!");

            // Convert IConfigurationSection → JSON string
            var firebaseCredentialJson = System.Text.Json.JsonSerializer.Serialize(
                section.Get<Dictionary<string, object>>()
            );

            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromJson(firebaseCredentialJson)
            });

        }
        return services;
    }
}
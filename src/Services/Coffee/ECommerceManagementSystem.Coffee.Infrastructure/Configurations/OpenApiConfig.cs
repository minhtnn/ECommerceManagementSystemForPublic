using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Configurations;

public static class OpenApiConfig
    {
        public static void AddOpenApiConfig(this IServiceCollection services)
        {
            services.AddOpenApi(opt =>
            {
                opt.AddDocumentTransformer((document, context, cancellationToken) =>
                {
                    document.Info.Title = "Brands";
                    document.Info.Contact = new OpenApiContact
                    {
                        Email = "nhatminh@reso.com",
                        Name = "CoffeeMachine",
                    };

                    return Task.CompletedTask;
                });
                opt.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            });
        }
        
        internal sealed class BearerSecuritySchemeTransformer(
            IAuthenticationSchemeProvider authenticationSchemeProvider
        ) : IOpenApiDocumentTransformer
        {
            public async Task TransformAsync(
                OpenApiDocument document,
                OpenApiDocumentTransformerContext context,
                CancellationToken cancellationToken
            )
            {
                var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
                if (authenticationSchemes.Any(authScheme => authScheme.Name == "Bearer"))
                {
                    var requirements = new Dictionary<string, OpenApiSecurityScheme>
                    {
                        ["Bearer"] = new OpenApiSecurityScheme
                        {
                            In = ParameterLocation.Header,
                            Description = "Please enter a valid token using the Bearer scheme (\"bearer {token}\")",
                            Name = "Authorization",
                            Type = SecuritySchemeType.Http,
                            BearerFormat = "Security",
                            Scheme = "Bearer"
                        },
                    };
                    document.Components ??= new OpenApiComponents();
                    document.Components.SecuritySchemes = requirements;
                    foreach (var operation in document.Paths.Values.SelectMany(path => path.Operations))
                    {
                        operation.Value.Security.Add(new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecurityScheme { Reference = new OpenApiReference { Id = "Bearer", Type = ReferenceType.SecurityScheme } }] = Array.Empty<string>()
                        });
                    }
                }
            }
        }
    }
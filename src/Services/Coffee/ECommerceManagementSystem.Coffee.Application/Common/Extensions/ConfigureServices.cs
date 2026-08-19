using ECommerceManagementSystem.Coffee.Application.Common.Behaviours;
using ECommerceManagementSystem.Coffee.Application.Common.Utils;
using ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.ChangePassword;
using ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.CreateAccount;
using ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.CustomerGoogleLoginAndRegister;
using ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.CustomerNormalRegister;
using ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.ForgotPassword;
using ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.Login;
using ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.ResendOTP.Email;
using ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.ResetPassword;
using ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.UpdateAccount;
using ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.ValidateResetToken;
using ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.VerifyCustomerEmail;
using ECommerceManagementSystem.Coffee.Application.Features.Brands.Command.CreateBrand;
using ECommerceManagementSystem.Coffee.Application.Features.Brands.Command.UpdateBrand;
using ECommerceManagementSystem.Coffee.Application.Features.Carts.Command.CreateCart;
using ECommerceManagementSystem.Coffee.Application.Features.Carts.Command.UpdateCart;
using ECommerceManagementSystem.Coffee.Application.Features.CustomerAddresses.Command.CreateCustomerAddress;
using ECommerceManagementSystem.Coffee.Application.Features.CustomerAddresses.Command.UpdateCustomerAddress;
using ECommerceManagementSystem.Coffee.Application.Features.Customers.Command.SendCustomerConsult;
using ECommerceManagementSystem.Coffee.Application.Features.Orders.Command.CreateOrder;
using ECommerceManagementSystem.Coffee.Application.Features.Orders.Command.UpdateOrder;
using ECommerceManagementSystem.Coffee.Application.Features.PaymentMethods.Command.CreateBrandPaymentMethod;
using ECommerceManagementSystem.Coffee.Application.Features.PaymentMethods.Command.CreatePaymentMethod;
using ECommerceManagementSystem.Coffee.Application.Features.PaymentMethods.Command.UpdateBrandPaymentMethod;
using ECommerceManagementSystem.Coffee.Application.Features.PaymentMethods.Command.UpdatePaymentMethod;
using ECommerceManagementSystem.Coffee.Application.Features.Posts.Command.CreateBrandPost;
using ECommerceManagementSystem.Coffee.Application.Features.Posts.Command.UpdateBrandPost;
using ECommerceManagementSystem.Coffee.Application.Features.ProductCategories.Command.CreateProductCategory;
using ECommerceManagementSystem.Coffee.Application.Features.ProductCategories.Command.UpdateProductCategory;
using ECommerceManagementSystem.Coffee.Application.Features.Products.Command.CreateProduct;
using ECommerceManagementSystem.Coffee.Application.Features.Products.Command.UpdateProduct;
using ECommerceManagementSystem.Coffee.Application.Features.PromotionRules.Command.CreateBrandPromotionRule;
using ECommerceManagementSystem.Coffee.Application.Features.PromotionRules.Command.UpdateBrandPromotionRule;
using ECommerceManagementSystem.Coffee.Application.Features.SystemConfigs.Command.CreateSystemConfig;
using ECommerceManagementSystem.Coffee.Application.Features.SystemConfigs.Command.UpdateSystemConfig;
using ECommerceManagementSystem.Coffee.Application.Jobs;
using ECommerceManagementSystem.Coffee.Application.Jobs.CustomerAccountJobs;
using ECommerceManagementSystem.Coffee.Application.Services;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Application.Services.PaymentGateways;
using ECommerceManagementSystem.Coffee.Domain.Models.Settings;
using FluentValidation;
using Hangfire;
using Hangfire.SqlServer;
using Mediator;
using StackExchange.Redis;

namespace ECommerceManagementSystem.Coffee.Application.Common.Extensions;

public static class ConfigureServices
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        // services.AddAntiforgery();
        services.AddMediator(options =>
            {
                options.Namespace = "CoffeeMachineNewBE.Dolores.Application.Endpoints";
                options.ServiceLifetime = ServiceLifetime.Scoped;
            })
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehaviour<,>))
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        services.AddScoped(typeof(ValidationUtil<>));
        services.AddAutoMapper(typeof(Program));
        services.AddRedis(configuration: configuration);
        services.AddScoped<IMediaService, MediaService>();
        services.Configure<FirebaseStorageSetting>(configuration.GetSection("FirebaseStorageSettings"));
        services.Configure<RefundSettings>(configuration.GetSection("RefundSettings"));
        services.Configure<EmailSetting>(configuration.GetSection("EmailSettings"));

        services.AddScoped<IValidator<CustomerNormalRegisterCommand>, CustomerNormalRegisterCommandValidator>();
        services.AddScoped<IValidator<VerifyCustomerEmailCommand>, VerifyCustomerEmailCommandValidator>();
        services.AddScoped<IValidator<ResendEmailOTPCommand>, ResendEmailOTPCommandValidator>();
        services.AddScoped<IValidator<ChangePasswordCommand>, ChangePasswordCommandValidator>();
        
        services.AddScoped<IValidator<ForgotPasswordCommand>, ForgotPasswordCommandValidator>();
        services.AddScoped<IValidator<ValidateResetTokenCommand>, ValidateResetTokenCommandValidator>();
        services.AddScoped<IValidator<ResetPasswordCommand>, ResetPasswordCommandValidator>();

        services.AddScoped<IValidator<CreateAccountCommand>, CreateAccountCommandValidator>();
        services.AddScoped<IValidator<UpdateAccountCommand>, UpdateAccountCommandValidator>();
        services.AddScoped<IValidator<LoginCommand>, LoginCommandValidator>();

        services.AddScoped<IValidator<CreateBrandCommand>, CreateBrandCommandValidator>();
        services.AddScoped<IValidator<UpdateBrandCommand>, UpdateBrandCommandValidator>();

        services.AddScoped<IValidator<CreateProductCategoryCommand>, CreateProductCategoryCommandValidator>();
        services.AddScoped<IValidator<UpdateProductCategoryCommand>, UpdateProductCategoryCommandValidator>();

        services.AddScoped<IValidator<CreateProductCommand>, CreateProductCommandValidator>();
        services.AddScoped<IValidator<UpdateProductCommand>, UpdateProductCommandValidator>();

        services.AddScoped<IValidator<CreateCartCommand>, CreateCartCommandValidator>();
        services.AddScoped<IValidator<UpdateCartCommand>, UpdateCartCommandValidator>();

        services.AddScoped<IValidator<CreatePaymentMethodCommand>, CreatePaymentMethodCommandValidator>();
        services.AddScoped<IValidator<UpdatePaymentMethodCommand>, UpdatePaymentMethodCommandValidator>();

        services.AddScoped<IValidator<CreateBrandPaymentMethodCommand>, CreateBrandPaymentMethodCommandValidator>();
        services.AddScoped<IValidator<UpdateBrandPaymentMethodCommand>, UpdateBrandPaymentMethodCommandValidator>();

        services.AddScoped<IValidator<CreateCustomerAddressCommand>, CreateCustomerAddressCommandValidator>();
        services.AddScoped<IValidator<UpdateCustomerAddressCommand>, UpdateCustomerAddressCommandValidator>();

        services.AddScoped<IValidator<CreateOrderCommand>, CreateOrderCommandValidator>();
        services.AddScoped<IValidator<UpdateOrderCommand>, UpdateOrderCommandValidator>();
        
        services.AddScoped<IValidator<CreateBrandPromotionRuleCommand>, CreateBrandPromotionRuleCommandValidator>();
        services.AddScoped<IValidator<UpdateBrandPromotionRuleCommand>, UpdateBrandPromotionRuleCommandValidator>();
        
        services.AddScoped<IValidator<CreateBrandPostCommand>, CreateBrandPostCommandValidator>();
        services.AddScoped<IValidator<UpdateBrandPostCommand>, UpdateBrandPostCommandValidator>();
        
        // services.AddScoped<IValidator<CustomerGoogleRegisterCommand>, CustomerGoogleRegisterCommandValidator>();
        services.AddScoped<IValidator<CustomerGoogleLoginAndRegisterCommand>, CustomerGoogleLoginAndRegisterCommandValidator>();
        
        services.AddScoped<IValidator<CreateSystemConfigCommand>, CreateSystemConfigCommandValidator>();
        services.AddScoped<IValidator<UpdateSystemConfigCommand>, UpdateSystemConfigCommandValidator>();
        
        services.AddScoped<IValidator<SendCustomerConsultCommand>, SendCustomerConsultCommandValidator>();

        services.AddScoped<IClaimService, ClaimService>();
        services.AddScoped<IRedisService, RedisService>();
        services.AddScoped<IMediaService, MediaService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IRefundService, RefundService>();
        services.AddScoped<ICacheInvalidationService, CacheInvalidationService>();
        services.AddScoped<IPaymentGatewayService, PayOSService>();
        services.AddScoped<IPaymentGatewayService, PayInCashService>();

        services.AddScoped<IPaymentGatewayFactory, PaymentGatewayFactory>();

        services.AddScoped<IAuthenticationService, AuthenticationService>();
        
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(
                configuration.GetConnectionString("HangfireConnection"),
                new SqlServerStorageOptions()
                {
                    CommandBatchMaxTimeout     = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval          = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks         = true,
                }
            )
        );
        
        services.AddHangfireServer(options =>
        {
            options.WorkerCount = 2;
            options.Queues      = new[] { "default" };
        });
        
        services.AddScoped<PromotionStatusSyncJob>();
        services.AddScoped<RefreshTokenSyncJob>();
        services.AddScoped<CustomerAccountDeleteJob>();

        services.Configure<RouteHandlerOptions>(options => { options.ThrowOnBadRequest = true; });
        services.AddHealthChecks();
        return services;
    }

    public static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (redisConnectionString != null)
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var options = ConfigurationOptions.Parse(redisConnectionString);
                options.ConnectTimeout = 4000;
                options.SyncTimeout = 3000;
                options.AsyncTimeout = 3000;

                options.ConnectRetry = 3;
                options.AbortOnConnectFail = false;
                return ConnectionMultiplexer.Connect(options);
            });
        return services;
    }
}
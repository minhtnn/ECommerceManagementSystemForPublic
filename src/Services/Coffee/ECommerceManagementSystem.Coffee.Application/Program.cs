using Carter;
using Common.Logging;
using ECommerceManagementSystem.Coffee.Application.Common.Extensions;
using ECommerceManagementSystem.Coffee.Application.Common.Filters;
using ECommerceManagementSystem.Coffee.Application.Common.Middlewares;
using ECommerceManagementSystem.Coffee.Application.Jobs;
using ECommerceManagementSystem.Coffee.Application.Jobs.CustomerAccountJobs;
using ECommerceManagementSystem.Coffee.Application.Jobs.DailySalesAggregateJobs;
using ECommerceManagementSystem.Coffee.Domain.Constants;
using ECommerceManagementSystem.Coffee.Infrastructure;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.ServiceDefaults;
using Hangfire;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Host.UseSerilog(SeriLogger.Configure);
Log.Information("Starting Ecommerce Coffee API up");

try
{
    builder.Services.AddInfrastructureServices(builder.Configuration);
    builder.Services.AddApplicationServices(builder.Configuration);
    builder.Services.AddCarter(new DependencyContextAssemblyCatalog([typeof(Program).Assembly]));

    var app = builder.Build();

    if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
    {
        app.UseScalar();
    }

    app.UseHealthChecks("/health");

    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var brandContextSeed =
                scope.ServiceProvider.GetRequiredService<ECommerceManagementSystemCoffeeContextSeed>();
            await brandContextSeed.InitializeAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "An error occurred while seeding the database.");
            throw;
        }
    }

    if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
    {
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            // Chặn truy cập từ bên ngoài khi dev/staging:
            // chỉ cho phép request từ localhost
            Authorization = new[]
            {
                new LocalRequestsOnlyAuthorizationFilter()
            }
        });
    }

    // RecurringJob.AddOrUpdate<PromotionStatusSyncJob>(
    //     recurringJobId: "promotion-status-sync",
    //     methodCall: job => job.ExecuteAsync(),
    //     cronExpression: "* * * * *",
    //     timeZone: TimeZoneInfo.Utc
    // );
    // RecurringJob.AddOrUpdate<RefreshTokenSyncJob>(
    //     recurringJobId: "refresh-token-cleanup",
    //     methodCall: job => job.ExecuteAsync(),
    //     cronExpression: "0 2 * * *",
    //     timeZone: TimeZoneInfo.Utc
    // );

    var jobManager = app.Services.GetRequiredService<IRecurringJobManager>();

    jobManager.AddOrUpdate<PromotionStatusSyncJob>(
        recurringJobId: "promotion-status-sync",
        methodCall: job => job.ExecuteAsync(),
        cronExpression: "* * * * *",
        timeZone: TimeZoneInfo.Utc
    );
    jobManager.AddOrUpdate<RefreshTokenSyncJob>(
        recurringJobId: "refresh-token-cleanup",
        methodCall: job => job.ExecuteAsync(),
        cronExpression: "0 2 * * *",
        timeZone: TimeZoneInfo.Utc
    );
    jobManager.AddOrUpdate<CustomerAccountDeleteJob>(
        recurringJobId: "customer-account-cleanup",
        methodCall: job => job.ExecuteAsync(),
        cronExpression: "*/15 * * * *",
        timeZone: TimeZoneInfo.Utc
    );
    jobManager.AddOrUpdate<DailySalesAggregateJob>(
        recurringJobId: "daily-sales-aggregate",
        methodCall: job => job.ExecuteAsync(null),
        cronExpression: "0 2 * * *",
        timeZone: TimeZoneInfo.Utc
    );

// Aggregate doanh thu tổng hợp theo brand
    jobManager.AddOrUpdate<DailyBrandSummaryJob>(
        recurringJobId: "daily-brand-summary",
        methodCall: job => job.ExecuteAsync(null),
        cronExpression: "0 2 * * *",
        timeZone: TimeZoneInfo.Utc
    );
    app.MapGet("/robots.txt", () => Results.Content(
        """
        User-agent: facebookexternalhit
        Allow: /

        User-agent: Twitterbot
        Allow: /

        User-agent: *
        Allow: /
        """,
        "text/plain"
    )).AllowAnonymous().RequireCors(CorsPolicy.AllowPublic);
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseCors(CorsPolicy.AllowFrontend);
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<GlobalException>();
    app.MapCarter();
    app.Run();
}
catch (Exception ex)
{
    string type = ex.GetType().Name;
    Log.Fatal(ex, $"Unhandled: {ex.Message}");
    Console.WriteLine(ex);
    if (type.Equals("StopTheHostException", StringComparison.Ordinal))
    {
        throw;
    }
}
finally
{
    Log.Information("Shut down Brands API complete");
    Log.CloseAndFlush();
}
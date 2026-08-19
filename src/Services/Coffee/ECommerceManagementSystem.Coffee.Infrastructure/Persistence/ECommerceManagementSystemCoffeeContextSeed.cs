using Microsoft.EntityFrameworkCore;
using ILogger = Serilog.ILogger;
namespace ECommerceManagementSystem.Coffee.Infrastructure.Persistence;

public class ECommerceManagementSystemCoffeeContextSeed
{
    private readonly ILogger _logger;
    private readonly ECommerceManagementSystemCoffeeContext _context;

    public ECommerceManagementSystemCoffeeContextSeed(ILogger logger, ECommerceManagementSystemCoffeeContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task InitializeAsync()
    {
        try
        {
            if (_context.Database.IsSqlServer() && Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                // await _context.Database.MigrateAsync();
            }
        }
        catch (Exception e)
        {
            _logger.Error(e, "An error occurred while migrating the database");
            throw;
        }
    }
}
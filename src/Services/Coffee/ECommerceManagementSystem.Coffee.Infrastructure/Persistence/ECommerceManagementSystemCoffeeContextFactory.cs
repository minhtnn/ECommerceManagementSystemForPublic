using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Persistence;

public class ECommerceManagementSystemCoffeeContextFactory: IDesignTimeDbContextFactory<ECommerceManagementSystemCoffeeContext>
{
    public ECommerceManagementSystemCoffeeContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();
        var optionsBuilder = new DbContextOptionsBuilder<ECommerceManagementSystemCoffeeContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnectionString");

        optionsBuilder.UseSqlServer(connectionString);
        
        return new ECommerceManagementSystemCoffeeContext(optionsBuilder.Options);
    }
}
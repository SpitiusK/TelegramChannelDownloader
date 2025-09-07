using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TelegramChannelDownloader.DataBase;

/// <summary>
/// Design-time factory for creating TelegramDbContext instances during migrations
/// </summary>
public class TelegramDbContextFactory : IDesignTimeDbContextFactory<TelegramDbContext>
{
    public TelegramDbContext CreateDbContext(string[] args)
    {
        // Build configuration from appsettings.json in Desktop project
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "TelegramChannelDownloader.Desktop");
        
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<TelegramDbContext>();

        // Get connection string from configuration
        var connectionString = configuration.GetConnectionString("TelegramDatabase") ??
            throw new InvalidOperationException("TelegramDatabase connection string not found");

        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.CommandTimeout(300); // 5 minutes
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);
        });

        // Enable detailed errors for design-time operations
        optionsBuilder.EnableDetailedErrors();
        optionsBuilder.EnableSensitiveDataLogging();

        return new TelegramDbContext(optionsBuilder.Options);
    }
}
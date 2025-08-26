using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TelegramChannelDownloader.Core.Services;

namespace TelegramChannelDownloader.Core.Extensions;

/// <summary>
/// Extension methods for registering Core layer services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Core layer services with the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddTelegramChannelDownloaderCore(this IServiceCollection services)
    {
        // Register validation service as singleton (stateless)
        services.AddSingleton<IValidationService, ValidationService>();

        // Register export service as scoped (might have state during operations)
        services.AddScoped<IExportService, ExportService>();

        // Register download service as scoped (maintains state during downloads)
        services.AddScoped<IDownloadService, DownloadService>();

        return services;
    }

    /// <summary>
    /// Registers Core layer services with custom configurations
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Optional configuration action</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddTelegramChannelDownloaderCore(
        this IServiceCollection services, 
        Action<CoreServiceOptions>? configureOptions = null)
    {
        var options = new CoreServiceOptions();
        configureOptions?.Invoke(options);

        // Register configuration options
        services.AddSingleton(options);

        // Register services based on configuration
        if (options.UseMemoryCaching)
        {
            services.AddMemoryCache();
        }

        // Register logging if not already registered
        if (!services.Any(x => x.ServiceType == typeof(ILogger<>)))
        {
            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(options.LogLevel);
                if (options.EnableConsoleLogging)
                {
                    builder.AddConsole();
                }
            });
        }

        return services.AddTelegramChannelDownloaderCore();
    }
}

/// <summary>
/// Configuration options for Core layer services
/// </summary>
public class CoreServiceOptions
{
    /// <summary>
    /// Minimum log level for Core services
    /// </summary>
    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Whether to enable console logging
    /// </summary>
    public bool EnableConsoleLogging { get; set; } = true;

    /// <summary>
    /// Whether to use memory caching for validation results
    /// </summary>
    public bool UseMemoryCaching { get; set; } = true;

    /// <summary>
    /// Cache duration for validation results (in minutes)
    /// </summary>
    public int CacheExpirationMinutes { get; set; } = 10;

    /// <summary>
    /// Maximum number of concurrent downloads
    /// </summary>
    public int MaxConcurrentDownloads { get; set; } = 3;

    /// <summary>
    /// Default batch size for message processing
    /// </summary>
    public int DefaultBatchSize { get; set; } = 100;

    /// <summary>
    /// Whether to enable detailed performance logging
    /// </summary>
    public bool EnablePerformanceLogging { get; set; } = false;

    /// <summary>
    /// Directory for temporary files during processing
    /// </summary>
    public string? TempDirectory { get; set; }

    /// <summary>
    /// Maximum file size for exports (in bytes, 0 = no limit)
    /// </summary>
    public long MaxExportFileSize { get; set; } = 0;

    /// <summary>
    /// Default timeout for operations (in seconds)
    /// </summary>
    public int DefaultTimeoutSeconds { get; set; } = 300; // 5 minutes
}
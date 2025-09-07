using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TelegramChannelDownloader.DataBase.Repositories;
using TelegramChannelDownloader.DataBase.Services;

namespace TelegramChannelDownloader.DataBase.Extensions;

/// <summary>
/// Extension methods for registering DataBase layer services with PostgreSQL database support
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all DataBase layer services with the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Configuration for database connection strings</param>
    /// <param name="configureOptions">Optional configuration action</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddTelegramDatabase(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<DatabaseServiceOptions>? configureOptions = null)
    {
        var options = new DatabaseServiceOptions();
        configureOptions?.Invoke(options);

        // Register configuration options
        services.AddSingleton(options);

        // Configure database context
        services.AddPostgreSqlDatabase(configuration, options);

        // Register repository services
        services.AddScoped<IMessageRepository, MessageRepository>();

        // Register data lifecycle service as hosted service if enabled
        if (options.DataLifecycleOptions.Enabled)
        {
            services.Configure<DataLifecycleOptions>(opt =>
            {
                opt.CleanupInterval = options.DataLifecycleOptions.CleanupInterval;
                opt.DataRetentionPeriod = options.DataLifecycleOptions.DataRetentionPeriod;
                opt.LogDatabaseStatistics = options.DataLifecycleOptions.LogDatabaseStatistics;
                opt.Enabled = options.DataLifecycleOptions.Enabled;
            });

            services.AddHostedService<DataLifecycleService>();
        }

        return services;
    }

    /// <summary>
    /// Add PostgreSQL database context with optimized configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Configuration containing connection strings</param>
    /// <param name="options">Database service options for database tuning</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddPostgreSqlDatabase(
        this IServiceCollection services,
        IConfiguration configuration,
        DatabaseServiceOptions? options = null)
    {
        var databaseOptions = options?.DatabaseOptions ?? new DatabaseOptions();
        
        // Configure DbContext - use pool if enabled, otherwise regular registration
        if (databaseOptions.UseDbContextPooling)
        {
            services.AddDbContextPool<TelegramDbContext>(contextOptions =>
            {
                var connectionString = configuration.GetConnectionString("TelegramDatabase")
                    ?? databaseOptions.DefaultConnectionString;

                ConfigureDbContextOptions(contextOptions, connectionString, databaseOptions);
            }, poolSize: databaseOptions.DbContextPoolSize);
        }
        else
        {
            services.AddDbContext<TelegramDbContext>(contextOptions =>
            {
                var connectionString = configuration.GetConnectionString("TelegramDatabase") 
                    ?? databaseOptions.DefaultConnectionString;

                ConfigureDbContextOptions(contextOptions, connectionString, databaseOptions);
            });
        }

        return services;
    }

    /// <summary>
    /// Add only database services without full database layer registration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="connectionString">PostgreSQL connection string</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddTelegramDatabaseOnly(
        this IServiceCollection services, 
        string connectionString)
    {
        services.AddDbContext<TelegramDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.CommandTimeout(300); // 5 minutes
                npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
            });
        });

        services.AddScoped<IMessageRepository, MessageRepository>();

        return services;
    }

    /// <summary>
    /// Common DbContext configuration for both regular and pooled contexts
    /// </summary>
    private static void ConfigureDbContextOptions(
        DbContextOptionsBuilder contextOptions, 
        string connectionString, 
        DatabaseOptions databaseOptions)
    {
        contextOptions.UseNpgsql(connectionString, npgsqlOptions =>
        {
            // Connection configuration
            npgsqlOptions.CommandTimeout(databaseOptions.CommandTimeoutSeconds);
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: databaseOptions.MaxRetryCount,
                maxRetryDelay: TimeSpan.FromSeconds(databaseOptions.MaxRetryDelaySeconds),
                errorCodesToAdd: null);

            // Performance optimizations
            npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        });

        // Development vs Production settings
        if (databaseOptions.EnableSensitiveDataLogging)
        {
            contextOptions.EnableSensitiveDataLogging();
        }

        if (databaseOptions.EnableDetailedErrors)
        {
            contextOptions.EnableDetailedErrors();
        }

        // Connection pooling
        contextOptions.EnableServiceProviderCaching();
        
        // Configure logging
        contextOptions.LogTo(message => System.Diagnostics.Debug.WriteLine(message), 
            databaseOptions.LogLevel);
    }
}

/// <summary>
/// Configuration options for DataBase layer services
/// </summary>
public class DatabaseServiceOptions
{
    /// <summary>
    /// Database configuration options
    /// </summary>
    public DatabaseOptions DatabaseOptions { get; set; } = new();

    /// <summary>
    /// Data lifecycle management options
    /// </summary>
    public DataLifecycleOptions DataLifecycleOptions { get; set; } = new();
}

/// <summary>
/// Database-specific configuration options
/// </summary>
public class DatabaseOptions
{
    /// <summary>
    /// Default connection string if not provided in configuration
    /// </summary>
    public string DefaultConnectionString { get; set; } = 
        "Host=localhost;Database=telegram_downloads;Username=telegram_user;Password=secure_password_123;Maximum Pool Size=20;Connection Idle Lifetime=300;Connection Pruning Interval=10";

    /// <summary>
    /// Command timeout in seconds for database operations
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Maximum retry count for failed database operations
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// Maximum delay between retries in seconds
    /// </summary>
    public int MaxRetryDelaySeconds { get; set; } = 30;

    /// <summary>
    /// Whether to enable sensitive data logging (disable in production)
    /// </summary>
    public bool EnableSensitiveDataLogging { get; set; } = false;

    /// <summary>
    /// Whether to enable detailed error messages (disable in production)
    /// </summary>
    public bool EnableDetailedErrors { get; set; } = false;

    /// <summary>
    /// Log level for Entity Framework logging
    /// </summary>
    public LogLevel LogLevel { get; set; } = LogLevel.Warning;

    /// <summary>
    /// Whether to use DbContext pooling for better performance
    /// </summary>
    public bool UseDbContextPooling { get; set; } = true;

    /// <summary>
    /// Size of the DbContext pool
    /// </summary>
    public int DbContextPoolSize { get; set; } = 20;

    /// <summary>
    /// Batch size for bulk insert operations
    /// </summary>
    public int BulkInsertBatchSize { get; set; } = 1000;

    /// <summary>
    /// Whether to use streaming for large result sets
    /// </summary>
    public bool EnableStreaming { get; set; } = true;

    /// <summary>
    /// Query timeout for bulk operations in seconds
    /// </summary>
    public int BulkOperationTimeoutSeconds { get; set; } = 600; // 10 minutes
}

/// <summary>
/// Data lifecycle management options
/// </summary>
public class DataLifecycleOptions
{
    /// <summary>
    /// Whether data lifecycle management is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// How often to run cleanup operations
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(6);

    /// <summary>
    /// How long to retain data before cleanup
    /// </summary>
    public TimeSpan DataRetentionPeriod { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// Whether to log database statistics during cleanup
    /// </summary>
    public bool LogDatabaseStatistics { get; set; } = true;
}

/// <summary>
/// Background service for data lifecycle management
/// </summary>
public class DataLifecycleService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DataLifecycleService> _logger;
    private readonly DataLifecycleOptions _options;

    public DataLifecycleService(
        IServiceScopeFactory scopeFactory,
        ILogger<DataLifecycleService> logger,
        Microsoft.Extensions.Options.IOptions<DataLifecycleOptions> options)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Data lifecycle service started with cleanup interval: {Interval}, retention period: {Retention}",
            _options.CleanupInterval, _options.DataRetentionPeriod);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformCleanupAsync(stoppingToken);
                await Task.Delay(_options.CleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Data lifecycle service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during data lifecycle cleanup");
                // Wait a shorter time before retrying on error
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }
    }

    private async Task PerformCleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IMessageRepository>();

        var cutoffDate = DateTime.UtcNow.Subtract(_options.DataRetentionPeriod);
        
        _logger.LogInformation("Starting data cleanup for sessions older than {CutoffDate}", cutoffDate);

        if (_options.LogDatabaseStatistics)
        {
            var statsBefore = await repository.GetDatabaseStatisticsAsync(cancellationToken);
            _logger.LogInformation("Database statistics before cleanup: {TotalSessions} sessions, {TotalMessages} messages, {DatabaseSize} bytes",
                statsBefore.TotalSessions, statsBefore.TotalMessages, statsBefore.DatabaseSizeBytes);
        }

        var cleanedSessions = await repository.CleanupExpiredSessionsAsync(cutoffDate, cancellationToken);
        
        if (cleanedSessions > 0)
        {
            _logger.LogInformation("Cleaned up {SessionCount} expired sessions", cleanedSessions);

            if (_options.LogDatabaseStatistics)
            {
                var statsAfter = await repository.GetDatabaseStatisticsAsync(cancellationToken);
                _logger.LogInformation("Database statistics after cleanup: {TotalSessions} sessions, {TotalMessages} messages, {DatabaseSize} bytes",
                    statsAfter.TotalSessions, statsAfter.TotalMessages, statsAfter.DatabaseSizeBytes);
            }
        }
        else
        {
            _logger.LogDebug("No expired sessions found for cleanup");
        }
    }
}
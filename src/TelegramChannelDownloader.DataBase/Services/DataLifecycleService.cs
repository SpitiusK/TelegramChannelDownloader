using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TelegramChannelDownloader.DataBase.Repositories;

namespace TelegramChannelDownloader.DataBase.Services;

/// <summary>
/// Background service that manages data lifecycle, including automatic cleanup of expired download sessions
/// </summary>
public class DataLifecycleService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataLifecycleService> _logger;
    private readonly DataLifecycleOptions _options;

    public DataLifecycleService(
        IServiceProvider serviceProvider,
        ILogger<DataLifecycleService> logger,
        IOptions<DataLifecycleOptions> options)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Data Lifecycle Service started with cleanup interval: {CleanupInterval}", 
            _options.CleanupInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformCleanupAsync(stoppingToken);
                
                // Wait for the next cleanup cycle
                await Task.Delay(_options.CleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping the service
                _logger.LogInformation("Data Lifecycle Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during data lifecycle cleanup");
                
                // Wait a shorter interval before retrying after an error
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    /// <summary>
    /// Perform cleanup of expired data
    /// </summary>
    private async Task PerformCleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var messageRepository = scope.ServiceProvider.GetRequiredService<IMessageRepository>();

        try
        {
            var cutoffDate = DateTime.UtcNow.Subtract(_options.DataRetentionPeriod);
            
            _logger.LogInformation("Starting data cleanup for sessions older than {CutoffDate}", cutoffDate);

            // Get expired sessions for logging
            var expiredSessions = await messageRepository.GetExpiredSessionsAsync(cutoffDate, cancellationToken);
            
            if (expiredSessions.Count == 0)
            {
                _logger.LogInformation("No expired sessions found for cleanup");
                return;
            }

            _logger.LogInformation("Found {ExpiredCount} expired sessions to clean up", expiredSessions.Count);

            // Log details about what will be cleaned up
            foreach (var session in expiredSessions.Take(10)) // Log first 10 to avoid spam
            {
                _logger.LogDebug("Cleaning up session {SessionId} for channel '{ChannelTitle}' (expired: {ExpiryDate})",
                    session.Id, session.ChannelTitle, session.ExpiresAt);
            }

            if (expiredSessions.Count > 10)
            {
                _logger.LogDebug("... and {RemainingCount} more sessions", expiredSessions.Count - 10);
            }

            // Perform the actual cleanup
            var cleanedCount = await messageRepository.CleanupExpiredSessionsAsync(cutoffDate, cancellationToken);

            _logger.LogInformation("Successfully cleaned up {CleanedCount} expired sessions", cleanedCount);

            // Get database statistics after cleanup
            if (_options.LogDatabaseStatistics)
            {
                await LogDatabaseStatisticsAsync(messageRepository, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform data cleanup");
            throw;
        }
    }

    /// <summary>
    /// Log database statistics for monitoring
    /// </summary>
    private async Task LogDatabaseStatisticsAsync(IMessageRepository messageRepository, CancellationToken cancellationToken)
    {
        try
        {
            var stats = await messageRepository.GetDatabaseStatisticsAsync(cancellationToken);
            
            _logger.LogInformation("Database Statistics: " +
                "Total Sessions: {TotalSessions}, " +
                "Active Sessions: {ActiveSessions}, " +
                "Expired Sessions: {ExpiredSessions}, " +
                "Total Messages: {TotalMessages}, " +
                "Oldest Session Age: {OldestSessionAge}",
                stats.TotalSessions,
                stats.ActiveSessions,
                stats.ExpiredSessions,
                stats.TotalMessages,
                stats.OldestSessionAge);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve database statistics");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Data Lifecycle Service is stopping...");
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("Data Lifecycle Service stopped");
    }
}

/// <summary>
/// Configuration options for data lifecycle management
/// </summary>
public class DataLifecycleOptions
{
    public const string SectionName = "DataLifecycle";

    /// <summary>
    /// How often to run the cleanup process (default: every 24 hours)
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// How long to retain download session data (default: 30 days)
    /// </summary>
    public TimeSpan DataRetentionPeriod { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// Whether to log database statistics after cleanup (default: true)
    /// </summary>
    public bool LogDatabaseStatistics { get; set; } = true;

    /// <summary>
    /// Whether the data lifecycle service is enabled (default: true)
    /// </summary>
    public bool Enabled { get; set; } = true;
}
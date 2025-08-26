using TelegramChannelDownloader.TelegramApi.Messages.Models;

namespace TelegramChannelDownloader.Core.Models;

/// <summary>
/// Generic progress information for operations
/// </summary>
public class ProgressInfo
{
    /// <summary>
    /// Total number of items to process
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Number of items completed so far
    /// </summary>
    public int CompletedItems { get; set; }

    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public int ProgressPercentage => TotalItems > 0 ? (CompletedItems * 100) / TotalItems : 0;

    /// <summary>
    /// Current processing speed in items per second
    /// </summary>
    public double ItemsPerSecond { get; set; }

    /// <summary>
    /// Estimated time remaining
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; set; }

    /// <summary>
    /// Current phase of the operation
    /// </summary>
    public string CurrentPhase { get; set; } = string.Empty;

    /// <summary>
    /// Status message
    /// </summary>
    public string StatusMessage { get; set; } = string.Empty;

    /// <summary>
    /// Error message if an error occurred
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Indicates if the operation can be cancelled
    /// </summary>
    public bool CanCancel { get; set; } = true;

    /// <summary>
    /// Indicates if the operation is complete
    /// </summary>
    public bool IsComplete => CompletedItems >= TotalItems;

    /// <summary>
    /// Indicates if an error occurred
    /// </summary>
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    /// <summary>
    /// Operation start time
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Current time
    /// </summary>
    public DateTime CurrentTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Elapsed time since operation started
    /// </summary>
    public TimeSpan ElapsedTime => CurrentTime - StartTime;
}

/// <summary>
/// Enhanced progress information for download operations
/// </summary>
public class DownloadProgressInfo
{
    /// <summary>
    /// Unique identifier for the download
    /// </summary>
    public string DownloadId { get; set; } = string.Empty;

    /// <summary>
    /// Current phase of the download
    /// </summary>
    public DownloadPhase Phase { get; set; }

    /// <summary>
    /// Total number of messages to download
    /// </summary>
    public int TotalMessages { get; set; }

    /// <summary>
    /// Number of messages downloaded so far
    /// </summary>
    public int DownloadedMessages { get; set; }

    /// <summary>
    /// Download progress percentage (0-100)
    /// </summary>
    public int ProgressPercentage => TotalMessages > 0 ? (DownloadedMessages * 100) / TotalMessages : 0;

    /// <summary>
    /// Current download speed in messages per second
    /// </summary>
    public double MessagesPerSecond { get; set; }

    /// <summary>
    /// Estimated time remaining
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; set; }

    /// <summary>
    /// Current message being processed
    /// </summary>
    public MessageData? CurrentMessage { get; set; }

    /// <summary>
    /// Error message if an error occurred
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Indicates if the download can be cancelled
    /// </summary>
    public bool CanCancel { get; set; } = true;

    /// <summary>
    /// Indicates if the download is complete
    /// </summary>
    public bool IsComplete => DownloadedMessages >= TotalMessages || Phase == DownloadPhase.Completed;

    /// <summary>
    /// Indicates if an error occurred
    /// </summary>
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    /// <summary>
    /// Download start time
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Current processing time
    /// </summary>
    public DateTime CurrentTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Elapsed time since download started
    /// </summary>
    public TimeSpan ElapsedTime => CurrentTime - StartTime;

    /// <summary>
    /// Current batch being processed (for batch downloads)
    /// </summary>
    public int CurrentBatch { get; set; }

    /// <summary>
    /// Total number of batches
    /// </summary>
    public int TotalBatches { get; set; }

    /// <summary>
    /// Additional status information
    /// </summary>
    public string StatusMessage { get; set; } = string.Empty;

    /// <summary>
    /// Memory usage in bytes (optional monitoring)
    /// </summary>
    public long? MemoryUsage { get; set; }

    /// <summary>
    /// Creates a progress info for a specific phase
    /// </summary>
    /// <param name="downloadId">Download identifier</param>
    /// <param name="phase">Current phase</param>
    /// <param name="message">Status message</param>
    /// <returns>Progress info instance</returns>
    public static DownloadProgressInfo ForPhase(string downloadId, DownloadPhase phase, string message = "")
    {
        return new DownloadProgressInfo
        {
            DownloadId = downloadId,
            Phase = phase,
            StatusMessage = message,
            CurrentTime = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Phases of a download operation
/// </summary>
public enum DownloadPhase
{
    /// <summary>
    /// Download is initializing
    /// </summary>
    Initializing,

    /// <summary>
    /// Validating channel and permissions
    /// </summary>
    Validating,

    /// <summary>
    /// Counting messages
    /// </summary>
    Counting,

    /// <summary>
    /// Downloading messages
    /// </summary>
    Downloading,

    /// <summary>
    /// Processing downloaded messages
    /// </summary>
    Processing,

    /// <summary>
    /// Exporting to file
    /// </summary>
    Exporting,

    /// <summary>
    /// Finalizing and cleanup
    /// </summary>
    Finalizing,

    /// <summary>
    /// Download completed successfully
    /// </summary>
    Completed,

    /// <summary>
    /// Download failed
    /// </summary>
    Failed,

    /// <summary>
    /// Download was cancelled
    /// </summary>
    Cancelled
}

/// <summary>
/// Extension methods for DownloadPhase
/// </summary>
public static class DownloadPhaseExtensions
{
    /// <summary>
    /// Gets a human-readable description of the download phase
    /// </summary>
    /// <param name="phase">Download phase</param>
    /// <returns>Description</returns>
    public static string GetDescription(this DownloadPhase phase)
    {
        return phase switch
        {
            DownloadPhase.Initializing => "Initializing download",
            DownloadPhase.Validating => "Validating channel",
            DownloadPhase.Counting => "Counting messages",
            DownloadPhase.Downloading => "Downloading messages",
            DownloadPhase.Processing => "Processing messages",
            DownloadPhase.Exporting => "Exporting to file",
            DownloadPhase.Finalizing => "Finalizing",
            DownloadPhase.Completed => "Completed",
            DownloadPhase.Failed => "Failed",
            DownloadPhase.Cancelled => "Cancelled",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Indicates if the phase represents a terminal state
    /// </summary>
    /// <param name="phase">Download phase</param>
    /// <returns>True if the phase is terminal</returns>
    public static bool IsTerminal(this DownloadPhase phase)
    {
        return phase is DownloadPhase.Completed or DownloadPhase.Failed or DownloadPhase.Cancelled;
    }
}
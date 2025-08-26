using TelegramChannelDownloader.TelegramApi.Channels.Models;

namespace TelegramChannelDownloader.Core.Models;

/// <summary>
/// Current status of a download operation
/// </summary>
public class DownloadStatus
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
    /// Progress percentage (0-100)
    /// </summary>
    public int ProgressPercentage => TotalMessages > 0 ? (DownloadedMessages * 100) / TotalMessages : 0;

    /// <summary>
    /// Number of messages downloaded
    /// </summary>
    public int DownloadedMessages { get; set; }

    /// <summary>
    /// Total number of messages to download
    /// </summary>
    public int TotalMessages { get; set; }

    /// <summary>
    /// Current download speed in messages per second
    /// </summary>
    public double MessagesPerSecond { get; set; }

    /// <summary>
    /// Estimated time remaining
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; set; }

    /// <summary>
    /// Download start time
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Completion time (if completed)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Last update time
    /// </summary>
    public DateTime LastUpdateTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Error message if download failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Channel information for the download
    /// </summary>
    public ChannelInfo? ChannelInfo { get; set; }

    /// <summary>
    /// Indicates if the download can be cancelled
    /// </summary>
    public bool CanCancel { get; set; } = true;

    /// <summary>
    /// Indicates if the download is in a terminal state
    /// </summary>
    public bool IsTerminal => Phase.IsTerminal();

    /// <summary>
    /// Indicates if the download has encountered an error
    /// </summary>
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    /// <summary>
    /// User-friendly status message
    /// </summary>
    public string StatusMessage
    {
        get
        {
            if (HasError)
                return ErrorMessage ?? "An error occurred";

            return Phase switch
            {
                DownloadPhase.Initializing => "Initializing download",
                DownloadPhase.Validating => "Validating channel",
                DownloadPhase.Counting => "Counting messages",
                DownloadPhase.Downloading => $"Downloaded {DownloadedMessages:N0}/{TotalMessages:N0} messages",
                DownloadPhase.Processing => "Processing messages",
                DownloadPhase.Exporting => "Exporting to file",
                DownloadPhase.Finalizing => "Finalizing",
                DownloadPhase.Completed => $"Completed - Downloaded {DownloadedMessages:N0} messages",
                DownloadPhase.Failed => "Download failed",
                DownloadPhase.Cancelled => "Download cancelled",
                _ => "Unknown status"
            };
        }
    }
}


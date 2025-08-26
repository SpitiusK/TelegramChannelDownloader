namespace TelegramChannelDownloader.Core.Models;

/// <summary>
/// Result of a download operation
/// </summary>
public class DownloadResult
{
    /// <summary>
    /// Indicates if the download was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Unique identifier for this download
    /// </summary>
    public string DownloadId { get; set; } = string.Empty;

    /// <summary>
    /// Number of messages successfully downloaded
    /// </summary>
    public int MessagesDownloaded { get; set; }

    /// <summary>
    /// Total duration of the download operation
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Output file path
    /// </summary>
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>
    /// File size of the output file
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Error message if download failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Additional details about the error
    /// </summary>
    public string? ErrorDetails { get; set; }

    /// <summary>
    /// Exception type that caused the failure (for debugging)
    /// </summary>
    public string? ExceptionType { get; set; }

    /// <summary>
    /// Channel information that was downloaded
    /// </summary>
    public ChannelSummary? ChannelSummary { get; set; }

    /// <summary>
    /// Download statistics
    /// </summary>
    public DownloadStatistics Statistics { get; set; } = new();
}

/// <summary>
/// Summary information about a channel
/// </summary>
public class ChannelSummary
{
    /// <summary>
    /// Channel display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Channel username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Channel type (Channel, Group, etc.)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Total number of members
    /// </summary>
    public int MemberCount { get; set; }

    /// <summary>
    /// Total number of messages in the channel
    /// </summary>
    public int MessageCount { get; set; }
}

/// <summary>
/// Statistics about a download operation
/// </summary>
public class DownloadStatistics
{
    /// <summary>
    /// Average download speed in messages per second
    /// </summary>
    public double AverageSpeed { get; set; }

    /// <summary>
    /// Peak download speed in messages per second
    /// </summary>
    public double PeakSpeed { get; set; }

    /// <summary>
    /// Number of messages with text content
    /// </summary>
    public int TextMessages { get; set; }

    /// <summary>
    /// Number of messages with media content
    /// </summary>
    public int MediaMessages { get; set; }

    /// <summary>
    /// Number of messages that were forwarded
    /// </summary>
    public int ForwardedMessages { get; set; }

    /// <summary>
    /// Number of errors encountered during download
    /// </summary>
    public int Errors { get; set; }

    /// <summary>
    /// Start time of the download
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// End time of the download
    /// </summary>
    public DateTime EndTime { get; set; }
}
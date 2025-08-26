namespace TelegramChannelDownloader.Core.Models;

/// <summary>
/// Result of an export operation
/// </summary>
public class ExportResult
{
    /// <summary>
    /// Indicates if the export was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Unique identifier for this export
    /// </summary>
    public string ExportId { get; set; } = string.Empty;

    /// <summary>
    /// Path to the exported file
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Size of the exported file in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Number of messages exported
    /// </summary>
    public int MessagesExported { get; set; }

    /// <summary>
    /// Duration of the export operation
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Error message if export failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Additional details about the error
    /// </summary>
    public string? ErrorDetails { get; set; }

    /// <summary>
    /// Export format that was used
    /// </summary>
    public ExportFormat Format { get; set; }

    /// <summary>
    /// Checksum of the exported file (for integrity verification)
    /// </summary>
    public string? FileChecksum { get; set; }

    /// <summary>
    /// Export statistics
    /// </summary>
    public ExportStatistics Statistics { get; set; } = new();
}

/// <summary>
/// Statistics about an export operation
/// </summary>
public class ExportStatistics
{
    /// <summary>
    /// Number of text-only messages
    /// </summary>
    public int TextMessages { get; set; }

    /// <summary>
    /// Number of messages with media
    /// </summary>
    public int MediaMessages { get; set; }

    /// <summary>
    /// Number of forwarded messages
    /// </summary>
    public int ForwardedMessages { get; set; }

    /// <summary>
    /// Number of reply messages
    /// </summary>
    public int ReplyMessages { get; set; }

    /// <summary>
    /// Number of edited messages
    /// </summary>
    public int EditedMessages { get; set; }

    /// <summary>
    /// Total number of characters exported
    /// </summary>
    public long TotalCharacters { get; set; }

    /// <summary>
    /// Average message length
    /// </summary>
    public double AverageMessageLength { get; set; }

    /// <summary>
    /// Date range of exported messages
    /// </summary>
    public DateRange? DateRange { get; set; }
}

/// <summary>
/// Represents a date range
/// </summary>
public class DateRange
{
    /// <summary>
    /// Start date of the range
    /// </summary>
    public DateTime Start { get; set; }

    /// <summary>
    /// End date of the range
    /// </summary>
    public DateTime End { get; set; }

    /// <summary>
    /// Duration of the range
    /// </summary>
    public TimeSpan Duration => End - Start;
}
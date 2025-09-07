using System.ComponentModel.DataAnnotations;

namespace TelegramChannelDownloader.DataBase.Entities;

/// <summary>
/// Represents a download session for a Telegram channel
/// </summary>
public class DownloadSession
{
    /// <summary>
    /// Unique identifier for the download session
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Channel username (without @)
    /// </summary>
    [Required]
    [StringLength(200)]
    public string ChannelUsername { get; set; } = string.Empty;

    /// <summary>
    /// Display title of the channel
    /// </summary>
    [Required]
    [StringLength(500)]
    public string ChannelTitle { get; set; } = string.Empty;

    /// <summary>
    /// Telegram channel ID
    /// </summary>
    public long ChannelId { get; set; }

    /// <summary>
    /// When the download session started
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the download session completed (null if in progress or failed)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Total number of messages downloaded in this session
    /// </summary>
    public int TotalMessages { get; set; }

    /// <summary>
    /// Total number of messages processed (including skipped/failed)
    /// </summary>
    public int ProcessedMessages { get; set; }

    /// <summary>
    /// Current status of the download session
    /// </summary>
    public DownloadSessionStatus Status { get; set; } = DownloadSessionStatus.InProgress;

    /// <summary>
    /// Error message if the session failed
    /// </summary>
    [StringLength(2000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When this record was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this session expires and can be cleaned up (30 days default)
    /// </summary>
    public DateTime? ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(30);

    /// <summary>
    /// Export format used for this session
    /// </summary>
    [StringLength(50)]
    public string? ExportFormat { get; set; }

    /// <summary>
    /// Export file path if exported
    /// </summary>
    [StringLength(1000)]
    public string? ExportPath { get; set; }

    /// <summary>
    /// Navigation property for all messages in this session
    /// </summary>
    public virtual ICollection<TelegramMessage> Messages { get; set; } = new List<TelegramMessage>();
}

/// <summary>
/// Status of a download session
/// </summary>
public enum DownloadSessionStatus
{
    /// <summary>
    /// Session is currently downloading messages
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// Session completed successfully
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Session failed with an error
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Session was cancelled by user
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// Session is paused
    /// </summary>
    Paused = 5
}
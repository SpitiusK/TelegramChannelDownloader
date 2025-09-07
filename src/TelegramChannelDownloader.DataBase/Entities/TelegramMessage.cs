using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TelegramChannelDownloader.DataBase.Entities;

/// <summary>
/// Represents a Telegram message stored in the database
/// </summary>
public class TelegramMessage
{
    /// <summary>
    /// Telegram message ID (unique within a channel)
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// Foreign key to the download session
    /// </summary>
    public Guid DownloadSessionId { get; set; }

    /// <summary>
    /// ID of the user/channel who sent the message
    /// </summary>
    public long FromId { get; set; }

    /// <summary>
    /// Username of the sender (nullable for channels)
    /// </summary>
    [StringLength(200)]
    public string? FromUsername { get; set; }

    /// <summary>
    /// Display name of the sender
    /// </summary>
    [StringLength(500)]
    public string? FromDisplayName { get; set; }

    /// <summary>
    /// Message text content
    /// </summary>
    [Column(TypeName = "text")]
    public string? Content { get; set; }

    /// <summary>
    /// When the message was sent
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Type of the message
    /// </summary>
    public MessageType MessageType { get; set; } = MessageType.Text;

    /// <summary>
    /// Whether this message has media attachments
    /// </summary>
    public bool HasMedia { get; set; }

    /// <summary>
    /// Type of media if HasMedia is true
    /// </summary>
    [StringLength(100)]
    public string? MediaType { get; set; }

    /// <summary>
    /// Media file name if downloaded
    /// </summary>
    [StringLength(500)]
    public string? MediaFileName { get; set; }

    /// <summary>
    /// Media file size in bytes
    /// </summary>
    public long? MediaFileSize { get; set; }

    /// <summary>
    /// Media MIME type
    /// </summary>
    [StringLength(200)]
    public string? MediaMimeType { get; set; }

    /// <summary>
    /// ID of the message this is replying to
    /// </summary>
    public long? ReplyToMessageId { get; set; }

    /// <summary>
    /// Number of views (for channel messages)
    /// </summary>
    public int Views { get; set; }

    /// <summary>
    /// Number of forwards
    /// </summary>
    public int Forwards { get; set; }

    /// <summary>
    /// Message reactions as JSON
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? Reactions { get; set; }

    /// <summary>
    /// Whether the message is forwarded from another source
    /// </summary>
    public bool IsForwarded { get; set; }

    /// <summary>
    /// Original channel/user ID if forwarded
    /// </summary>
    public long? ForwardedFromId { get; set; }

    /// <summary>
    /// Original message ID if forwarded
    /// </summary>
    public long? ForwardedFromMessageId { get; set; }

    /// <summary>
    /// Whether the message was edited
    /// </summary>
    public bool IsEdited { get; set; }

    /// <summary>
    /// When the message was last edited
    /// </summary>
    public DateTime? EditedAt { get; set; }

    /// <summary>
    /// Whether the message is pinned
    /// </summary>
    public bool IsPinned { get; set; }

    /// <summary>
    /// Raw message data as JSON for debugging/future use
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? RawData { get; set; }

    /// <summary>
    /// When this record was created in the database
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the download session
    /// </summary>
    [ForeignKey(nameof(DownloadSessionId))]
    public virtual DownloadSession DownloadSession { get; set; } = null!;
}

/// <summary>
/// Type of Telegram message
/// </summary>
public enum MessageType
{
    /// <summary>
    /// Plain text message
    /// </summary>
    Text = 1,

    /// <summary>
    /// Photo message
    /// </summary>
    Photo = 2,

    /// <summary>
    /// Video message
    /// </summary>
    Video = 3,

    /// <summary>
    /// Audio message
    /// </summary>
    Audio = 4,

    /// <summary>
    /// Voice message
    /// </summary>
    Voice = 5,

    /// <summary>
    /// Document/file message
    /// </summary>
    Document = 6,

    /// <summary>
    /// Sticker message
    /// </summary>
    Sticker = 7,

    /// <summary>
    /// Animation/GIF message
    /// </summary>
    Animation = 8,

    /// <summary>
    /// Video note (round video)
    /// </summary>
    VideoNote = 9,

    /// <summary>
    /// Location message
    /// </summary>
    Location = 10,

    /// <summary>
    /// Contact message
    /// </summary>
    Contact = 11,

    /// <summary>
    /// Poll message
    /// </summary>
    Poll = 12,

    /// <summary>
    /// Service message (user joined, etc.)
    /// </summary>
    Service = 13
}
using System.Text.RegularExpressions;

namespace TelegramChannelDownloader.TelegramApi.Messages.Models;

/// <summary>
/// Represents a downloaded Telegram message with structured data
/// </summary>
public class MessageData
{
    /// <summary>
    /// Unique message identifier
    /// </summary>
    public int MessageId { get; set; }

    /// <summary>
    /// Message timestamp (UTC)
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Username of the message sender (without @ symbol), null for channels
    /// </summary>
    public string? SenderUsername { get; set; }

    /// <summary>
    /// Display name of the sender
    /// </summary>
    public string? SenderDisplayName { get; set; }

    /// <summary>
    /// User ID of the message sender
    /// </summary>
    public long? SenderId { get; set; }

    /// <summary>
    /// Raw message content/text
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// List of URLs found in the message
    /// </summary>
    public List<string> Links { get; set; } = new();

    /// <summary>
    /// List of username mentions found in the message (without @ symbol)
    /// </summary>
    public List<string> Mentions { get; set; } = new();

    /// <summary>
    /// List of hashtags found in the message (without # symbol)
    /// </summary>
    public List<string> Hashtags { get; set; } = new();

    /// <summary>
    /// Type of message (text, photo, video, document, etc.)
    /// </summary>
    public MessageType MessageType { get; set; } = MessageType.Text;

    /// <summary>
    /// Information about media attachments, if any
    /// </summary>
    public MediaInfo? Media { get; set; }

    /// <summary>
    /// Number of views for this message (if available)
    /// </summary>
    public int? Views { get; set; }

    /// <summary>
    /// Information about forwarded message, if applicable
    /// </summary>
    public ForwardInfo? ForwardInfo { get; set; }

    /// <summary>
    /// Information about replied-to message, if applicable
    /// </summary>
    public ReplyInfo? ReplyInfo { get; set; }

    /// <summary>
    /// Indicates if the message has been edited
    /// </summary>
    public bool IsEdited { get; set; }

    /// <summary>
    /// Timestamp of when the message was edited (UTC), null if not edited
    /// </summary>
    public DateTime? EditedTimestamp { get; set; }

    /// <summary>
    /// Channel ID where this message was posted
    /// </summary>
    public long ChannelId { get; set; }

    /// <summary>
    /// Channel title/name where this message was posted
    /// </summary>
    public string ChannelTitle { get; set; } = string.Empty;

    /// <summary>
    /// Formatted message for markdown export
    /// </summary>
    public string FormattedContent
    {
        get
        {
            var content = Content;
            if (string.IsNullOrWhiteSpace(content))
            {
                content = GetMediaDescription();
            }

            // Preserve @mentions and links in the content
            return content;
        }
    }

    /// <summary>
    /// Sender display text for exports
    /// </summary>
    public string SenderDisplay
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(SenderDisplayName))
            {
                return SenderDisplayName;
            }

            if (!string.IsNullOrWhiteSpace(SenderUsername))
            {
                return $"@{SenderUsername}";
            }

            return "Channel";
        }
    }

    /// <summary>
    /// Gets a description of media content for display purposes
    /// </summary>
    /// <returns>Media description string</returns>
    private string GetMediaDescription()
    {
        if (Media == null)
            return string.Empty;

        return MessageType switch
        {
            MessageType.Photo => $"📷 Photo{(Media.HasCaption ? $": {Media.Caption}" : "")}",
            MessageType.Video => $"🎥 Video{(Media.HasCaption ? $": {Media.Caption}" : "")}",
            MessageType.Audio => $"🎵 Audio{(Media.HasCaption ? $": {Media.Caption}" : "")}",
            MessageType.Document => $"📄 Document: {Media.FileName ?? "Unknown"}{(Media.HasCaption ? $" - {Media.Caption}" : "")}",
            MessageType.Animation => $"🎬 Animation{(Media.HasCaption ? $": {Media.Caption}" : "")}",
            MessageType.Voice => "🎤 Voice message",
            MessageType.VideoNote => "🎥 Video note",
            MessageType.Sticker => "🎭 Sticker",
            MessageType.Location => "📍 Location",
            MessageType.Contact => "👤 Contact",
            MessageType.Poll => "📊 Poll",
            _ => "📎 Media"
        };
    }

    /// <summary>
    /// Extracts structured data from message content
    /// </summary>
    public void ProcessContent()
    {
        if (string.IsNullOrWhiteSpace(Content))
            return;

        // Extract URLs
        var urlPattern = @"https?://[^\s]+";
        var urlMatches = Regex.Matches(Content, urlPattern, RegexOptions.IgnoreCase);
        Links = urlMatches.Cast<Match>().Select(m => m.Value).Distinct().ToList();

        // Extract mentions (@username)
        var mentionPattern = @"@(\w+)";
        var mentionMatches = Regex.Matches(Content, mentionPattern, RegexOptions.IgnoreCase);
        Mentions = mentionMatches.Cast<Match>().Select(m => m.Groups[1].Value).Distinct().ToList();

        // Extract hashtags (#hashtag)
        var hashtagPattern = @"#(\w+)";
        var hashtagMatches = Regex.Matches(Content, hashtagPattern, RegexOptions.IgnoreCase);
        Hashtags = hashtagMatches.Cast<Match>().Select(m => m.Groups[1].Value).Distinct().ToList();
    }
}

/// <summary>
/// Types of Telegram messages
/// </summary>
public enum MessageType
{
    Text,
    Photo,
    Video,
    Audio,
    Document,
    Animation,
    Voice,
    VideoNote,
    Sticker,
    Location,
    Contact,
    Poll,
    Service,
    Unknown
}

/// <summary>
/// Information about media attachments
/// </summary>
public class MediaInfo
{
    /// <summary>
    /// Original filename (for documents)
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long? FileSize { get; set; }

    /// <summary>
    /// MIME type of the file
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// Media caption/description
    /// </summary>
    public string? Caption { get; set; }

    /// <summary>
    /// Width for photos/videos
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// Height for photos/videos
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// Duration for audio/video in seconds
    /// </summary>
    public int? Duration { get; set; }

    /// <summary>
    /// Indicates if the media has a caption
    /// </summary>
    public bool HasCaption => !string.IsNullOrWhiteSpace(Caption);

    /// <summary>
    /// File ID for potential future downloads
    /// </summary>
    public string? FileId { get; set; }
}

/// <summary>
/// Information about forwarded messages
/// </summary>
public class ForwardInfo
{
    /// <summary>
    /// Original sender username
    /// </summary>
    public string? OriginalSender { get; set; }

    /// <summary>
    /// Original channel title
    /// </summary>
    public string? OriginalChannel { get; set; }

    /// <summary>
    /// Original message date
    /// </summary>
    public DateTime OriginalDate { get; set; }

    /// <summary>
    /// Original message ID
    /// </summary>
    public int? OriginalMessageId { get; set; }
}

/// <summary>
/// Information about replied messages
/// </summary>
public class ReplyInfo
{
    /// <summary>
    /// ID of the message being replied to
    /// </summary>
    public int ReplyToMessageId { get; set; }

    /// <summary>
    /// Username of the original message sender
    /// </summary>
    public string? OriginalSender { get; set; }

    /// <summary>
    /// Preview of the original message content
    /// </summary>
    public string? OriginalMessagePreview { get; set; }
}

/// <summary>
/// Progress information for message downloading
/// </summary>
public class DownloadProgressInfo
{
    /// <summary>
    /// Total number of messages to download
    /// </summary>
    public int TotalMessages { get; set; }

    /// <summary>
    /// Number of messages downloaded so far
    /// </summary>
    public int DownloadedMessages { get; set; }

    /// <summary>
    /// Current message being processed
    /// </summary>
    public MessageData? CurrentMessage { get; set; }

    /// <summary>
    /// Download progress percentage (0-100)
    /// </summary>
    public int ProgressPercentage => TotalMessages > 0 ? (DownloadedMessages * 100) / TotalMessages : 0;

    /// <summary>
    /// Estimated time remaining
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; set; }

    /// <summary>
    /// Download speed (messages per second)
    /// </summary>
    public double MessagesPerSecond { get; set; }

    /// <summary>
    /// Any error that occurred during download
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Indicates if the download is complete
    /// </summary>
    public bool IsComplete => DownloadedMessages >= TotalMessages;

    /// <summary>
    /// Indicates if an error occurred
    /// </summary>
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
}
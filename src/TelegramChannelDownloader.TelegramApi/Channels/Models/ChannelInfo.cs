namespace TelegramChannelDownloader.TelegramApi.Channels.Models;

/// <summary>
/// Represents the type of a Telegram channel or chat
/// </summary>
public enum ChannelType
{
    /// <summary>
    /// Unknown channel type
    /// </summary>
    Unknown,

    /// <summary>
    /// Public channel
    /// </summary>
    Channel,

    /// <summary>
    /// Public supergroup
    /// </summary>
    Supergroup,

    /// <summary>
    /// Private channel
    /// </summary>
    PrivateChannel,

    /// <summary>
    /// Private supergroup
    /// </summary>
    PrivateSupergroup,

    /// <summary>
    /// Regular group chat
    /// </summary>
    Group
}

/// <summary>
/// Contains comprehensive information about a Telegram channel
/// </summary>
public class ChannelInfo
{
    /// <summary>
    /// Channel ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Channel username (without @ symbol), null for private channels
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Channel display title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Channel description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Type of the channel (public/private, channel/supergroup)
    /// </summary>
    public ChannelType Type { get; set; } = ChannelType.Unknown;

    /// <summary>
    /// Number of members/subscribers in the channel
    /// </summary>
    public int MemberCount { get; set; }

    /// <summary>
    /// Total number of messages in the channel
    /// </summary>
    public int MessageCount { get; set; }

    /// <summary>
    /// Date when the channel was created
    /// </summary>
    public DateTime? CreatedDate { get; set; }

    /// <summary>
    /// Date of the last message or activity
    /// </summary>
    public DateTime? LastActivityDate { get; set; }

    /// <summary>
    /// Indicates if the channel is public (has a username)
    /// </summary>
    public bool IsPublic => !string.IsNullOrWhiteSpace(Username);

    /// <summary>
    /// Indicates if the channel is a broadcast channel (not a group)
    /// </summary>
    public bool IsChannel => Type == ChannelType.Channel || Type == ChannelType.PrivateChannel;

    /// <summary>
    /// Indicates if the channel is a supergroup
    /// </summary>
    public bool IsSupergroup => Type == ChannelType.Supergroup || Type == ChannelType.PrivateSupergroup;

    /// <summary>
    /// Indicates if the channel is accessible to the current user
    /// </summary>
    public bool IsAccessible { get; set; } = true;

    /// <summary>
    /// Indicates if the channel is restricted or has download limitations
    /// </summary>
    public bool IsRestricted { get; set; } = false;

    /// <summary>
    /// Indicates if the channel has been verified by Telegram
    /// </summary>
    public bool IsVerified { get; set; } = false;

    /// <summary>
    /// Indicates if the channel is marked as scam by Telegram
    /// </summary>
    public bool IsScam { get; set; } = false;

    /// <summary>
    /// Indicates if the channel has protected content (no forwarding/downloading)
    /// </summary>
    public bool HasProtectedContent { get; set; } = false;

    /// <summary>
    /// Photo file ID for the channel avatar, if available
    /// </summary>
    public string? PhotoFileId { get; set; }

    /// <summary>
    /// Access hash for API operations (internal use)
    /// </summary>
    public long? AccessHash { get; set; }

    /// <summary>
    /// Error message if channel information could not be retrieved
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Full channel URL for public channels
    /// </summary>
    public string? ChannelUrl => IsPublic ? $"https://t.me/{Username}" : null;

    /// <summary>
    /// Display name for the channel (title with username if public)
    /// </summary>
    public string DisplayName
    {
        get
        {
            if (IsPublic && !string.IsNullOrWhiteSpace(Username))
            {
                return $"{Title} (@{Username})";
            }
            return Title;
        }
    }

    /// <summary>
    /// Summary information about the channel
    /// </summary>
    public string Summary
    {
        get
        {
            var summary = $"{DisplayName} - {Type}";
            if (MemberCount > 0)
            {
                summary += $", {MemberCount:N0} members";
            }
            if (MessageCount > 0)
            {
                summary += $", {MessageCount:N0} messages";
            }
            return summary;
        }
    }

    /// <summary>
    /// Indicates if the channel can be downloaded from (not restricted, accessible, has messages)
    /// </summary>
    public bool CanDownload => IsAccessible && !IsRestricted && !HasProtectedContent && MessageCount > 0;

    /// <summary>
    /// Validation message explaining why the channel cannot be downloaded, if applicable
    /// </summary>
    public string? ValidationMessage
    {
        get
        {
            if (!IsAccessible)
                return "Channel is not accessible. You may not have permission to view this channel.";
            
            if (IsRestricted)
                return "Channel is restricted and cannot be downloaded.";
            
            if (HasProtectedContent)
                return "Channel has protected content that cannot be downloaded.";
            
            if (MessageCount == 0)
                return "Channel has no messages to download.";
            
            if (IsScam)
                return "Channel is marked as scam by Telegram.";
            
            return null;
        }
    }
}

/// <summary>
/// Result of channel URL validation and processing
/// </summary>
public class ChannelValidationResult
{
    /// <summary>
    /// Indicates if the channel URL is valid and accessible
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Parsed and cleaned channel username
    /// </summary>
    public string? ChannelUsername { get; set; }

    /// <summary>
    /// Channel information if successfully retrieved
    /// </summary>
    public ChannelInfo? ChannelInfo { get; set; }

    /// <summary>
    /// Error message if validation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Indicates if the validation was successful and channel can be downloaded
    /// </summary>
    public bool CanProceed => IsValid && ChannelInfo?.CanDownload == true;

    /// <summary>
    /// User-friendly message about the validation result
    /// </summary>
    public string Message
    {
        get
        {
            if (!IsValid)
                return ErrorMessage ?? "Channel URL is invalid";
            
            if (ChannelInfo == null)
                return "Channel information could not be retrieved";
            
            if (!ChannelInfo.CanDownload)
                return ChannelInfo.ValidationMessage ?? "Channel cannot be downloaded";
            
            return $"Channel is ready for download: {ChannelInfo.Summary}";
        }
    }
}
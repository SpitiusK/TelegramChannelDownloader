using TelegramChannelDownloader.TelegramApi.Channels.Models;

namespace TelegramChannelDownloader.TelegramApi.Channels;

/// <summary>
/// Handles Telegram channel operations
/// </summary>
public interface IChannelService
{
    /// <summary>
    /// Gets comprehensive information about a Telegram channel by username or URL
    /// </summary>
    /// <param name="channelUrl">Channel URL, username, or identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Channel information or null if not found</returns>
    Task<ChannelInfo?> GetChannelInfoAsync(string channelUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a channel URL format and parses it to extract the username
    /// </summary>
    /// <param name="channelUrl">Channel URL to validate</param>
    /// <returns>Channel URL validation result with parsed information</returns>
    ChannelValidationResult ValidateChannelUrl(string channelUrl);

    /// <summary>
    /// Checks if a channel exists and is accessible to the current user
    /// </summary>
    /// <param name="channelUsername">Clean channel username (without @ or URL parts)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if channel is accessible</returns>
    Task<bool> CheckChannelAccessAsync(string channelUsername, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total message count for a channel
    /// </summary>
    /// <param name="channelUsername">Clean channel username (without @ or URL parts)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Message count</returns>
    Task<int> GetChannelMessageCountAsync(string channelUsername, CancellationToken cancellationToken = default);
}
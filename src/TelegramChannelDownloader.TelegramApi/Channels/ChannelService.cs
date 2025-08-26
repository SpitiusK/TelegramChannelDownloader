using Microsoft.Extensions.Logging;
using TelegramChannelDownloader.TelegramApi.Channels.Models;
using TelegramChannelDownloader.TelegramApi.Utils;
using WTelegram;
using TL;

namespace TelegramChannelDownloader.TelegramApi.Channels;

/// <summary>
/// Implementation of channel operations using WTelegramClient
/// </summary>
public class ChannelService : IChannelService, IDisposable
{
    private readonly ILogger<ChannelService> _logger;
    private readonly Client _client;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of ChannelService
    /// </summary>
    public ChannelService(ILogger<ChannelService> logger, Client client)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    /// <summary>
    /// Gets comprehensive information about a Telegram channel by username or URL
    /// </summary>
    /// <param name="channelUrl">Channel URL, username, or identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Channel information or null if not found</returns>
    public async Task<ChannelInfo?> GetChannelInfoAsync(string channelUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting channel information for URL: {ChannelUrl}", channelUrl);

            // First validate and parse the channel URL
            var validation = ValidateChannelUrl(channelUrl);
            if (!validation.IsValid || validation.ChannelInfo == null)
            {
                var errorInfo = new ChannelInfo
                {
                    ErrorMessage = validation.ErrorMessage ?? "Invalid channel URL",
                    IsAccessible = false
                };
                return errorInfo;
            }

            var username = validation.ChannelInfo.Username;
            if (string.IsNullOrWhiteSpace(username))
            {
                return new ChannelInfo
                {
                    ErrorMessage = "Unable to extract username from channel URL",
                    IsAccessible = false
                };
            }

            _logger.LogDebug("Resolving channel username: {Username}", username);

            // Resolve channel by username
            var resolved = await _client.Contacts_ResolveUsername(username);
            var chat = resolved.chats?.Values.FirstOrDefault();

            if (chat == null)
            {
                return new ChannelInfo
                {
                    Username = username,
                    ErrorMessage = "Channel not found or not accessible",
                    IsAccessible = false
                };
            }

            _logger.LogDebug("Successfully resolved channel: {Title} (ID: {Id})", chat.Title, chat.ID);

            // Convert TL chat object to our ChannelInfo model
            var channelInfo = ConvertToChannelInfo(chat, username);
            
            // Get additional information if it's a channel/supergroup
            if (chat is Channel channel)
            {
                try
                {
                    // Get full channel info for additional details
                    var fullChannel = await _client.Channels_GetFullChannel(channel);
                    UpdateChannelInfoFromFullChannel(channelInfo, fullChannel.full_chat as ChannelFull);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get full channel information for: {Username}", username);
                    // Continue with basic info
                }
            }

            return channelInfo;
        }
        catch (WTelegram.WTException ex) when (
            ex.Message.Contains("USERNAME_NOT_OCCUPIED") ||
            ex.Message.Contains("USERNAME_INVALID") ||
            ex.Message.Contains("CHANNEL_INVALID") ||
            ex.Message.Contains("CHAT_INVALID"))
        {
            _logger.LogWarning("Channel not found or invalid: {ChannelUrl} - {Error}", channelUrl, ex.Message);
            return new ChannelInfo
            {
                ErrorMessage = "Channel not found or not accessible",
                IsAccessible = false
            };
        }
        catch (WTelegram.WTException ex) when (ex.Message.Contains("CHANNEL_PRIVATE"))
        {
            _logger.LogWarning("Channel is private: {ChannelUrl}", channelUrl);
            return new ChannelInfo
            {
                ErrorMessage = "Channel is private and cannot be accessed",
                IsAccessible = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get channel information for: {ChannelUrl}", channelUrl);
            return new ChannelInfo
            {
                ErrorMessage = $"Error retrieving channel information: {ex.Message}",
                IsAccessible = false
            };
        }
    }

    /// <summary>
    /// Validates a channel URL format and parses it to extract the username
    /// </summary>
    /// <param name="channelUrl">Channel URL to validate</param>
    /// <returns>Channel URL validation result with parsed information</returns>
    public ChannelValidationResult ValidateChannelUrl(string channelUrl)
    {
        try
        {
            _logger.LogTrace("Validating channel URL: {ChannelUrl}", channelUrl);

            var parseResult = ChannelUrlParser.ParseChannelUrl(channelUrl);
            var result = new ChannelValidationResult
            {
                IsValid = parseResult.IsValid,
                ChannelUsername = parseResult.CleanUsername,
                ErrorMessage = parseResult.ErrorMessage
            };

            if (parseResult.IsValid && !string.IsNullOrWhiteSpace(parseResult.CleanUsername))
            {
                // Create a basic ChannelInfo with the parsed username
                result.ChannelInfo = new ChannelInfo
                {
                    Username = parseResult.CleanUsername,
                    Title = parseResult.CleanUsername, // Will be updated when actual data is fetched
                    Type = ChannelType.Unknown // Will be determined during API call
                };
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating channel URL: {ChannelUrl}", channelUrl);
            return new ChannelValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Validation error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Checks if a channel exists and is accessible to the current user
    /// </summary>
    /// <param name="channelUsername">Clean channel username (without @ or URL parts)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if channel is accessible</returns>
    public async Task<bool> CheckChannelAccessAsync(string channelUsername, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(channelUsername))
            {
                return false;
            }

            _logger.LogDebug("Checking channel access for: {Username}", channelUsername);

            // Try to resolve the channel
            var resolved = await _client.Contacts_ResolveUsername(channelUsername);
            return resolved.chats?.Count > 0;
        }
        catch (WTelegram.WTException ex) when (
            ex.Message.Contains("USERNAME_NOT_OCCUPIED") ||
            ex.Message.Contains("USERNAME_INVALID") ||
            ex.Message.Contains("CHANNEL_INVALID") ||
            ex.Message.Contains("CHAT_INVALID") ||
            ex.Message.Contains("CHANNEL_PRIVATE"))
        {
            _logger.LogDebug("Channel not accessible: {Username} - {Error}", channelUsername, ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking channel access for: {Username}", channelUsername);
            return false;
        }
    }

    /// <summary>
    /// Gets the total message count for a channel
    /// </summary>
    /// <param name="channelUsername">Clean channel username (without @ or URL parts)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Message count</returns>
    public async Task<int> GetChannelMessageCountAsync(string channelUsername, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(channelUsername))
            {
                return 0;
            }

            _logger.LogDebug("Getting message count for channel: {Username}", channelUsername);

            // First resolve the channel
            var resolved = await _client.Contacts_ResolveUsername(channelUsername);
            var channel = resolved.chats?.Values.FirstOrDefault();

            if (channel == null)
            {
                _logger.LogWarning("Channel not found when getting message count: {Username}", channelUsername);
                return 0;
            }

            // For channels and supergroups, try to get the message count from channel info
            if (channel is Channel ch)
            {
                try
                {
                    var fullChannel = await _client.Channels_GetFullChannel(ch);
                    var fullInfo = fullChannel.full_chat as ChannelFull;
                    
                    if (fullInfo != null)
                    {
                        // Use pts (message sequence number) as an approximation of message count
                        var messageCount = fullInfo.pts;
                        _logger.LogDebug("Channel message count for {Username}: {Count}", channelUsername, messageCount);
                        return messageCount;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get full channel info for message count: {Username}", channelUsername);
                }

                // Fallback: try to get history to estimate count
                try
                {
                    var peer = new InputPeerChannel(ch.id, ch.access_hash);
                    var history = await _client.Messages_GetHistory(peer, limit: 1);
                    var count = history switch
                    {
                        Messages_MessagesSlice slice => slice.count,
                        Messages_ChannelMessages channelMessages => channelMessages.count,
                        Messages_Messages messages => messages.messages?.Length ?? 0,
                        _ => 0
                    };
                    _logger.LogDebug("Channel message count (from history) for {Username}: {Count}", channelUsername, count);
                    return count;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get message history count: {Username}", channelUsername);
                }
            }

            _logger.LogWarning("Unable to determine message count for channel: {Username}", channelUsername);
            return 0;
        }
        catch (WTelegram.WTException ex) when (
            ex.Message.Contains("USERNAME_NOT_OCCUPIED") ||
            ex.Message.Contains("USERNAME_INVALID") ||
            ex.Message.Contains("CHANNEL_INVALID") ||
            ex.Message.Contains("CHAT_INVALID"))
        {
            _logger.LogWarning("Channel not found for message count: {Username} - {Error}", channelUsername, ex.Message);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting message count for channel: {Username}", channelUsername);
            throw;
        }
    }

    /// <summary>
    /// Converts a TL Chat object to our ChannelInfo model
    /// </summary>
    private static ChannelInfo ConvertToChannelInfo(ChatBase chat, string username)
    {
        var channelInfo = new ChannelInfo
        {
            Id = chat.ID,
            Username = username,
            Title = chat.Title ?? username,
            IsAccessible = true
        };

        switch (chat)
        {
            case Channel channel:
                channelInfo.Type = channel.flags.HasFlag(Channel.Flags.broadcast) 
                    ? (!string.IsNullOrWhiteSpace(channel.username) ? ChannelType.Channel : ChannelType.PrivateChannel)
                    : (!string.IsNullOrWhiteSpace(channel.username) ? ChannelType.Supergroup : ChannelType.PrivateSupergroup);
                
                channelInfo.IsVerified = channel.flags.HasFlag(Channel.Flags.verified);
                channelInfo.IsScam = channel.flags.HasFlag(Channel.Flags.scam);
                channelInfo.IsRestricted = channel.flags.HasFlag(Channel.Flags.restricted);
                channelInfo.HasProtectedContent = channel.flags.HasFlag(Channel.Flags.noforwards);
                // channelInfo.CreatedDate = channel.date == 0 ? null : DateTimeOffset.FromUnixTimeSeconds(channel.date).DateTime;
                
                if (channel.participants_count > 0)
                {
                    channelInfo.MemberCount = channel.participants_count;
                }
                break;

            case Chat regularChat:
                channelInfo.Type = ChannelType.Group;
                // channelInfo.CreatedDate = regularChat.date == 0 ? null : DateTimeOffset.FromUnixTimeSeconds(regularChat.date).DateTime;
                channelInfo.MemberCount = regularChat.participants_count;
                break;
        }

        return channelInfo;
    }

    /// <summary>
    /// Updates channel info with additional data from ChannelFull
    /// </summary>
    private static void UpdateChannelInfoFromFullChannel(ChannelInfo channelInfo, ChannelFull? fullChannel)
    {
        if (fullChannel == null) return;

        channelInfo.Description = fullChannel.about;
        
        if (fullChannel.participants_count > 0)
        {
            channelInfo.MemberCount = fullChannel.participants_count;
        }

        // Update message count estimate
        if (fullChannel.pts > 0)
        {
            channelInfo.MessageCount = fullChannel.pts;
        }

        // Set photo file ID if available
        if (fullChannel.chat_photo is Photo photo && photo.id != 0)
        {
            channelInfo.PhotoFileId = photo.id.ToString();
        }
    }

    /// <summary>
    /// Disposes of the channel service
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            // Note: We don't dispose the client here as it's injected and managed externally
            _disposed = true;
        }
    }
}
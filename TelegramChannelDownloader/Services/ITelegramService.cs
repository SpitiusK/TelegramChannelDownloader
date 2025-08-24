using TelegramChannelDownloader.Models;
using TelegramChannelDownloader.Utils;

namespace TelegramChannelDownloader.Services;

/// <summary>
/// Service interface for Telegram API operations using WTelegramClient
/// </summary>
public interface ITelegramService
{
    /// <summary>
    /// Event triggered when authentication status changes
    /// </summary>
    event EventHandler<AuthenticationStatus>? AuthenticationStatusChanged;

    /// <summary>
    /// Current authentication status
    /// </summary>
    AuthenticationStatus Status { get; }

    /// <summary>
    /// Indicates if the client is currently connected and authenticated
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Current authenticated user information (null if not authenticated)
    /// </summary>
    TelegramUser? CurrentUser { get; }

    /// <summary>
    /// Initializes the Telegram client with API credentials
    /// </summary>
    /// <param name="credentials">API credentials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the initialization operation</returns>
    Task InitializeAsync(TelegramCredentials credentials, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts the authentication process with phone number
    /// </summary>
    /// <param name="phoneNumber">Phone number including country code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the phone authentication operation</returns>
    Task AuthenticateWithPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes authentication with verification code received via SMS/app
    /// </summary>
    /// <param name="verificationCode">Verification code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the code verification operation</returns>
    Task VerifyCodeAsync(string verificationCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes authentication with two-factor authentication password
    /// </summary>
    /// <param name="password">Two-factor authentication password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the two-factor authentication operation</returns>
    Task VerifyTwoFactorAuthAsync(string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from Telegram and clears authentication state
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the disconnect operation</returns>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current session data for persistence
    /// </summary>
    /// <returns>Session data as string, or null if not available</returns>
    string? GetSessionData();

    /// <summary>
    /// Restores session from previously saved session data
    /// </summary>
    /// <param name="sessionData">Previously saved session data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the session restoration operation</returns>
    Task RestoreSessionAsync(string sessionData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the connection to Telegram servers
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the connection test operation</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets comprehensive information about a Telegram channel by username or URL
    /// </summary>
    /// <param name="channelUrl">Channel URL, username, or identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the channel information retrieval operation</returns>
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
    /// <returns>Task representing the channel existence check operation</returns>
    Task<bool> CheckChannelAccessAsync(string channelUsername, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total message count for a channel
    /// </summary>
    /// <param name="channelUsername">Clean channel username (without @ or URL parts)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the message count retrieval operation</returns>
    Task<int> GetChannelMessageCountAsync(string channelUsername, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads all messages from a specified channel with progress reporting
    /// </summary>
    /// <param name="channelInfo">Channel information obtained from GetChannelInfoAsync</param>
    /// <param name="progress">Progress reporter for download status updates</param>
    /// <param name="cancellationToken">Cancellation token to cancel the download operation</param>
    /// <returns>Task representing the download operation, returning list of downloaded messages</returns>
    Task<List<MessageData>> DownloadChannelMessagesAsync(ChannelInfo channelInfo, IProgress<DownloadProgressInfo>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads messages from a channel in batches for memory efficiency
    /// </summary>
    /// <param name="channelInfo">Channel information obtained from GetChannelInfoAsync</param>
    /// <param name="batchSize">Number of messages to download in each batch (default: 100)</param>
    /// <param name="progress">Progress reporter for download status updates</param>
    /// <param name="cancellationToken">Cancellation token to cancel the download operation</param>
    /// <returns>Async enumerable of message batches</returns>
    IAsyncEnumerable<List<MessageData>> DownloadChannelMessagesBatchAsync(ChannelInfo channelInfo, int batchSize = 100, IProgress<DownloadProgressInfo>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports downloaded messages to markdown format
    /// </summary>
    /// <param name="messages">List of messages to export</param>
    /// <param name="channelInfo">Channel information for header</param>
    /// <param name="outputPath">Full path where the markdown file should be saved</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the export operation</returns>
    Task ExportMessagesToMarkdownAsync(List<MessageData> messages, ChannelInfo channelInfo, string outputPath, CancellationToken cancellationToken = default);
}
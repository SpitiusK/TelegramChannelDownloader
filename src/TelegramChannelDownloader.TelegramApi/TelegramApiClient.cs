using Microsoft.Extensions.Logging;
using System.Text.Json;
using TelegramChannelDownloader.TelegramApi.Authentication;
using TelegramChannelDownloader.TelegramApi.Authentication.Models;
using TelegramChannelDownloader.TelegramApi.Channels;
using TelegramChannelDownloader.TelegramApi.Channels.Models;
using TelegramChannelDownloader.TelegramApi.Configuration;
using TelegramChannelDownloader.TelegramApi.Messages;
using TelegramChannelDownloader.TelegramApi.Messages.Models;
using TelegramChannelDownloader.TelegramApi.Session;
using WTelegram;

namespace TelegramChannelDownloader.TelegramApi;

/// <summary>
/// Main implementation of the Telegram API client using specialized service handlers
/// </summary>
public class TelegramApiClient : ITelegramApiClient, IDisposable
{
    private readonly ILogger<TelegramApiClient> _logger;
    private readonly ISessionManager _sessionManager;
    private IAuthenticationHandler? _authenticationHandler;
    private IChannelService? _channelService;
    private IMessageService? _messageService;
    private WTelegram.Client? _client;
    private bool _disposed;

    /// <summary>
    /// Event triggered when authentication status changes
    /// </summary>
    public event EventHandler<AuthStatusChangedEventArgs>? AuthenticationStatusChanged;

    /// <summary>
    /// Current authentication status
    /// </summary>
    public AuthResult CurrentAuthStatus => _authenticationHandler?.CurrentStatus ?? new AuthResult
    {
        State = AuthenticationState.Disconnected,
        Message = "Not initialized",
        IsSuccess = false
    };

    /// <summary>
    /// Indicates if the client is currently connected and authenticated
    /// </summary>
    public bool IsConnected => CurrentAuthStatus.IsConnected;

    /// <summary>
    /// Current authenticated user information (null if not authenticated)
    /// </summary>
    public TelegramUserInfo? CurrentUser => CurrentAuthStatus.User;

    /// <summary>
    /// Initializes a new instance of TelegramApiClient
    /// </summary>
    public TelegramApiClient(
        ILogger<TelegramApiClient> logger,
        ISessionManager sessionManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
    }

    /// <summary>
    /// Initializes the Telegram client with API credentials
    /// </summary>
    /// <param name="config">API configuration including credentials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication result</returns>
    public async Task<AuthResult> InitializeAsync(TelegramApiConfig config, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Initializing Telegram API client");

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var validationErrors = config.Validate();
            if (validationErrors.Any())
            {
                var errorMessage = string.Join(", ", validationErrors);
                _logger.LogError("Configuration validation failed: {Errors}", errorMessage);
                return AuthResult.Failure(AuthenticationState.ConnectionError, "Invalid configuration", errorMessage);
            }

            // Initialize WTelegram client with only API credentials
            _client = new WTelegram.Client(what => what switch
            {
                "api_id" => config.ApiId.ToString(),
                "api_hash" => config.ApiHash,
                "session_pathname" => _sessionManager.SessionPath,
                _ => null
            });

            // Create service instances with the initialized client
            var authLogger = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Debug))
                .CreateLogger<AuthenticationHandler>();
            _authenticationHandler = new AuthenticationHandler(authLogger);

            var channelLogger = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Debug))
                .CreateLogger<ChannelService>();
            _channelService = new ChannelService(channelLogger, _client);

            var messageLogger = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Debug))
                .CreateLogger<MessageService>();
            _messageService = new MessageService(messageLogger, _client);

            var result = await _authenticationHandler.InitializeAsync(config, cancellationToken);
            _logger.LogDebug("Telegram API client initialization completed with state: {State}", result.State);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Telegram API client");
            return AuthResult.Failure(AuthenticationState.ConnectionError, "Initialization failed", ex.Message, ex);
        }
    }

    /// <summary>
    /// Starts the authentication process with phone number
    /// </summary>
    /// <param name="phoneNumber">Phone number including country code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication result</returns>
    public async Task<AuthResult> AuthenticatePhoneAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Starting phone number authentication");
            
            if (_authenticationHandler == null)
            {
                return AuthResult.Failure(AuthenticationState.ConnectionError, "Client not initialized", "Call InitializeAsync first");
            }
            
            return await _authenticationHandler.AuthenticatePhoneAsync(phoneNumber, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Phone number authentication failed");
            return AuthResult.Failure(AuthenticationState.AuthenticationFailed, "Phone authentication failed", ex.Message, ex);
        }
    }

    /// <summary>
    /// Completes authentication with verification code received via SMS/app
    /// </summary>
    /// <param name="verificationCode">Verification code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication result</returns>
    public async Task<AuthResult> VerifyCodeAsync(string verificationCode, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Verifying authentication code");
            
            if (_authenticationHandler == null)
            {
                return AuthResult.Failure(AuthenticationState.ConnectionError, "Client not initialized", "Call InitializeAsync first");
            }
            
            return await _authenticationHandler.VerifyCodeAsync(verificationCode, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Code verification failed");
            return AuthResult.Failure(AuthenticationState.AuthenticationFailed, "Code verification failed", ex.Message, ex);
        }
    }

    /// <summary>
    /// Completes authentication with two-factor authentication password
    /// </summary>
    /// <param name="password">Two-factor authentication password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication result</returns>
    public async Task<AuthResult> VerifyTwoFactorAuthAsync(string password, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Verifying two-factor authentication");
            
            if (_authenticationHandler == null)
            {
                return AuthResult.Failure(AuthenticationState.ConnectionError, "Client not initialized", "Call InitializeAsync first");
            }
            
            return await _authenticationHandler.VerifyTwoFactorAsync(password, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Two-factor authentication failed");
            return AuthResult.Failure(AuthenticationState.AuthenticationFailed, "Two-factor authentication failed", ex.Message, ex);
        }
    }

    /// <summary>
    /// Disconnects from Telegram and clears authentication state
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the disconnect operation</returns>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Disconnecting from Telegram");
            if (_authenticationHandler != null)
            {
                await _authenticationHandler.DisconnectAsync(cancellationToken);
            }
            _logger.LogInformation("Successfully disconnected from Telegram");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during disconnect");
            throw;
        }
    }

    /// <summary>
    /// Gets current session data for persistence
    /// </summary>
    /// <returns>Session data as string, or null if not available</returns>
    public string? GetSessionData()
    {
        return _authenticationHandler?.GetSessionData();
    }

    /// <summary>
    /// Restores session from previously saved session data
    /// </summary>
    /// <param name="sessionData">Previously saved session data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication result</returns>
    public async Task<AuthResult> RestoreSessionAsync(string sessionData, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Restoring Telegram session");
            
            if (_authenticationHandler == null)
            {
                return AuthResult.Failure(AuthenticationState.ConnectionError, "Client not initialized", "Call InitializeAsync first");
            }
            
            return await _authenticationHandler.RestoreSessionAsync(sessionData, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Session restoration failed");
            return AuthResult.Failure(AuthenticationState.AuthenticationFailed, "Session restoration failed", ex.Message, ex);
        }
    }

    /// <summary>
    /// Tests the connection to Telegram servers
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connection is successful</returns>
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Testing connection to Telegram servers");
            
            if (_authenticationHandler == null)
            {
                return false;
            }
            
            return await _authenticationHandler.TestConnectionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed");
            return false;
        }
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
            _logger.LogDebug("Getting channel information for: {ChannelUrl}", channelUrl);
            
            if (_channelService == null)
            {
                throw new InvalidOperationException("Client not initialized. Call InitializeAsync first.");
            }
            
            return await _channelService.GetChannelInfoAsync(channelUrl, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get channel information for: {ChannelUrl}", channelUrl);
            throw;
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
            _logger.LogDebug("Validating channel URL: {ChannelUrl}", channelUrl);
            if (_channelService == null)
            {
                return new ChannelValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Client not initialized. Call InitializeAsync first."
                };
            }
            
            return _channelService.ValidateChannelUrl(channelUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Channel URL validation failed for: {ChannelUrl}", channelUrl);
            return new ChannelValidationResult
            {
                IsValid = false,
                ErrorMessage = ex.Message
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
            _logger.LogDebug("Checking channel access for: {ChannelUsername}", channelUsername);
            if (_channelService == null)
            {
                return false;
            }
            
            return await _channelService.CheckChannelAccessAsync(channelUsername, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Channel access check failed for: {ChannelUsername}", channelUsername);
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
            _logger.LogDebug("Getting message count for channel: {ChannelUsername}", channelUsername);
            if (_channelService == null)
            {
                throw new InvalidOperationException("Client not initialized. Call InitializeAsync first.");
            }
            
            return await _channelService.GetChannelMessageCountAsync(channelUsername, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get message count for channel: {ChannelUsername}", channelUsername);
            throw;
        }
    }

    /// <summary>
    /// Downloads all messages from a specified channel with progress reporting
    /// </summary>
    /// <param name="channelInfo">Channel information obtained from GetChannelInfoAsync</param>
    /// <param name="progress">Progress reporter for download status updates</param>
    /// <param name="cancellationToken">Cancellation token to cancel the download operation</param>
    /// <returns>List of downloaded messages</returns>
    public async Task<List<MessageData>> DownloadChannelMessagesAsync(ChannelInfo channelInfo, IProgress<DownloadProgressInfo>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Downloading messages from channel: {ChannelTitle} (ID: {ChannelId})", channelInfo.Title, channelInfo.Id);
            if (_messageService == null)
            {
                throw new InvalidOperationException("Client not initialized. Call InitializeAsync first.");
            }
            
            return await _messageService.DownloadChannelMessagesAsync(channelInfo, progress, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download messages from channel: {ChannelTitle} (ID: {ChannelId})", channelInfo.Title, channelInfo.Id);
            throw;
        }
    }

    /// <summary>
    /// Downloads messages from a channel in batches for memory efficiency
    /// </summary>
    /// <param name="channelInfo">Channel information obtained from GetChannelInfoAsync</param>
    /// <param name="batchSize">Number of messages to download in each batch (default: 100)</param>
    /// <param name="progress">Progress reporter for download status updates</param>
    /// <param name="cancellationToken">Cancellation token to cancel the download operation</param>
    /// <returns>Async enumerable of message batches</returns>
    public IAsyncEnumerable<List<MessageData>> DownloadChannelMessagesBatchAsync(ChannelInfo channelInfo, int batchSize = 100, IProgress<DownloadProgressInfo>? progress = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting batch download from channel: {ChannelTitle} (ID: {ChannelId}), batch size: {BatchSize}", channelInfo.Title, channelInfo.Id, batchSize);
        if (_messageService == null)
        {
            throw new InvalidOperationException("Client not initialized. Call InitializeAsync first.");
        }
        
        return _messageService.DownloadChannelMessagesBatchAsync(channelInfo, batchSize, progress, cancellationToken);
    }

    /// <summary>
    /// Exports downloaded messages to markdown format
    /// </summary>
    /// <param name="messages">List of messages to export</param>
    /// <param name="channelInfo">Channel information for header</param>
    /// <param name="outputPath">Full path where the markdown file should be saved</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the export operation</returns>
    public async Task ExportMessagesToMarkdownAsync(List<MessageData> messages, ChannelInfo channelInfo, string outputPath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Exporting {MessageCount} messages to markdown: {OutputPath}", messages.Count, outputPath);
            if (_messageService == null)
            {
                throw new InvalidOperationException("Client not initialized. Call InitializeAsync first.");
            }
            
            await _messageService.ExportMessagesToMarkdownAsync(messages, channelInfo, outputPath, cancellationToken);
            _logger.LogInformation("Successfully exported {MessageCount} messages to: {OutputPath}", messages.Count, outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export messages to markdown: {OutputPath}", outputPath);
            throw;
        }
    }

    /// <summary>
    /// Handles authentication status changes from the authentication handler
    /// </summary>
    private void OnAuthenticationStatusChanged(object? sender, AuthStatusChangedEventArgs e)
    {
        _logger.LogDebug("Authentication status changed: {PreviousState} -> {CurrentState}", e.PreviousStatus?.State, e.CurrentStatus.State);
        AuthenticationStatusChanged?.Invoke(this, e);
    }

    /// <summary>
    /// Disposes of the Telegram API client
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            if (_authenticationHandler != null)
            {
                _authenticationHandler.StatusChanged -= OnAuthenticationStatusChanged;
            }

            if (_authenticationHandler is IDisposable authDisposable)
                authDisposable.Dispose();

            if (_channelService is IDisposable channelDisposable)
                channelDisposable.Dispose();

            if (_messageService is IDisposable messageDisposable)
                messageDisposable.Dispose();

            if (_sessionManager is IDisposable sessionDisposable)
                sessionDisposable.Dispose();

            _disposed = true;
        }
    }
}
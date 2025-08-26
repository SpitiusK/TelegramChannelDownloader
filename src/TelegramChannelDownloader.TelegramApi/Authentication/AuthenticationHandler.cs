using Microsoft.Extensions.Logging;
using TelegramChannelDownloader.TelegramApi.Authentication.Models;
using TelegramChannelDownloader.TelegramApi.Configuration;
using WTelegram;
using TL;

namespace TelegramChannelDownloader.TelegramApi.Authentication;

/// <summary>
/// Handles Telegram authentication operations using WTelegramClient
/// </summary>
public class AuthenticationHandler : IAuthenticationHandler, IDisposable
{
    private readonly ILogger<AuthenticationHandler> _logger;
    private Client? _client;
    private TelegramCredentials? _credentials;
    private TelegramApiConfig? _config;
    private AuthResult _currentStatus;
    private bool _disposed;

    /// <summary>
    /// Event triggered when authentication status changes
    /// </summary>
    public event EventHandler<AuthStatusChangedEventArgs>? StatusChanged;

    /// <summary>
    /// Current authentication status
    /// </summary>
    public AuthResult CurrentStatus 
    { 
        get => _currentStatus; 
        private set 
        {
            var previousStatus = _currentStatus;
            _currentStatus = value;
            StatusChanged?.Invoke(this, new AuthStatusChangedEventArgs(_currentStatus, previousStatus));
        }
    }

    /// <summary>
    /// Initializes a new instance of AuthenticationHandler
    /// </summary>
    public AuthenticationHandler(ILogger<AuthenticationHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _currentStatus = AuthResult.Failure(AuthenticationState.Disconnected, "Not connected");
    }

    /// <summary>
    /// Initializes the authentication handler with configuration
    /// </summary>
    /// <param name="config">Telegram API configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication result</returns>
    public async Task<AuthResult> InitializeAsync(TelegramApiConfig config, CancellationToken cancellationToken = default)
    {
        try
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (!config.IsValid)
            {
                var errors = string.Join(", ", config.Validate());
                return CurrentStatus = AuthResult.Failure(AuthenticationState.ConnectionError, "Invalid configuration", errors);
            }

            // Dispose existing client if any
            _client?.Dispose();

            _config = config;
            _credentials = new TelegramCredentials
            {
                ApiId = config.ApiId,
                ApiHash = config.ApiHash
            };

            // Update status to connecting
            CurrentStatus = AuthResult.Success(AuthenticationState.Connecting, "Initializing connection...");

            _logger.LogDebug("Creating WTelegram client with API ID: {ApiId}", config.ApiId);

            // Create WTelegram client with configuration
            _client = new Client(Config);

            // Test basic connectivity
            await _client.ConnectAsync();

            _logger.LogInformation("Successfully connected to Telegram servers");

            return CurrentStatus = AuthResult.Success(AuthenticationState.WaitingForPhoneNumber, "Please provide your phone number");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Telegram client");
            return CurrentStatus = AuthResult.Failure(AuthenticationState.ConnectionError, "Connection failed", ex.Message, ex);
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
            if (_client == null || _credentials == null)
            {
                return CurrentStatus = AuthResult.Failure(AuthenticationState.ConnectionError, "Client not initialized. Call InitializeAsync first.");
            }

            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return CurrentStatus = AuthResult.Failure(AuthenticationState.AuthenticationFailed, "Phone number cannot be empty");
            }

            CurrentStatus = AuthResult.Success(AuthenticationState.Connecting, "Sending verification code...");

            // Store phone number in credentials
            _credentials.PhoneNumber = phoneNumber;

            _logger.LogDebug("Starting authentication process for phone: {Phone}", phoneNumber.Substring(0, Math.Min(phoneNumber.Length, 5)) + "***");

            try
            {
                // Start the authentication process
                var user = await _client.LoginUserIfNeeded();
                if (user != null)
                {
                    return await HandleSuccessfulAuthenticationAsync(user);
                }
                else
                {
                    // If no user returned but no exception, likely waiting for verification code
                    return CurrentStatus = AuthResult.Success(AuthenticationState.WaitingForVerificationCode, "Please enter the verification code sent to your phone");
                }
            }
            catch (WTelegram.WTException ex) when (ex.Message.Contains("phone_code") || ex.Message.Contains("PHONE_CODE"))
            {
                // Verification code is needed
                _logger.LogDebug("Verification code requested");
                return CurrentStatus = AuthResult.Success(AuthenticationState.WaitingForVerificationCode, "Please enter the verification code sent to your phone");
            }
            catch (WTelegram.WTException ex) when (ex.Message.Contains("SESSION_PASSWORD_NEEDED"))
            {
                // Two-factor authentication is needed
                _logger.LogDebug("Two-factor authentication required");
                return CurrentStatus = AuthResult.Success(AuthenticationState.WaitingForTwoFactorAuth, "Please enter your two-factor authentication password");
            }
            catch (WTelegram.WTException ex)
            {
                // Log the actual exception message for debugging
                _logger.LogWarning("WTelegram exception during phone auth: {Message}", ex.Message);
                
                // Check if it might be asking for verification code
                if (ex.Message.ToUpper().Contains("CODE") || ex.Message.ToUpper().Contains("VERIFICATION"))
                {
                    return CurrentStatus = AuthResult.Success(AuthenticationState.WaitingForVerificationCode, "Please enter the verification code sent to your phone");
                }
                else
                {
                    throw; // Re-throw if it's not a code-related exception
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to authenticate with phone number");
            return CurrentStatus = AuthResult.Failure(AuthenticationState.AuthenticationFailed, "Authentication failed", ex.Message, ex);
        }
    }

    /// <summary>
    /// Completes authentication with verification code
    /// </summary>
    /// <param name="verificationCode">Verification code from SMS/app</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication result</returns>
    public async Task<AuthResult> VerifyCodeAsync(string verificationCode, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_client == null || _credentials == null)
            {
                return CurrentStatus = AuthResult.Failure(AuthenticationState.ConnectionError, "Client not initialized. Use AuthenticatePhoneAsync first.");
            }

            if (string.IsNullOrWhiteSpace(verificationCode))
            {
                return CurrentStatus = AuthResult.Failure(AuthenticationState.AuthenticationFailed, "Verification code cannot be empty");
            }

            CurrentStatus = AuthResult.Success(AuthenticationState.Connecting, "Verifying code...");

            _credentials.VerificationCode = verificationCode;

            _logger.LogDebug("Verifying authentication code");

            try
            {
                // Continue the authentication process with the verification code
                var user = await _client.LoginUserIfNeeded();
                if (user != null)
                {
                    return await HandleSuccessfulAuthenticationAsync(user);
                }
                else
                {
                    return CurrentStatus = AuthResult.Failure(AuthenticationState.AuthenticationFailed, "Authentication failed - no user returned");
                }
            }
            catch (WTelegram.WTException ex) when (ex.Message.Contains("SESSION_PASSWORD_NEEDED"))
            {
                // Two-factor authentication is needed
                _logger.LogDebug("Two-factor authentication required after code verification");
                return CurrentStatus = AuthResult.Success(AuthenticationState.WaitingForTwoFactorAuth, "Please enter your two-factor authentication password");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify code");
            return CurrentStatus = AuthResult.Failure(AuthenticationState.AuthenticationFailed, "Code verification failed", ex.Message, ex);
        }
    }

    /// <summary>
    /// Completes authentication with two-factor password
    /// </summary>
    /// <param name="password">Two-factor authentication password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication result</returns>
    public async Task<AuthResult> VerifyTwoFactorAsync(string password, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_client == null || _credentials == null)
            {
                return CurrentStatus = AuthResult.Failure(AuthenticationState.ConnectionError, "Client not initialized. Use AuthenticatePhoneAsync first.");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                return CurrentStatus = AuthResult.Failure(AuthenticationState.AuthenticationFailed, "Password cannot be empty");
            }

            CurrentStatus = AuthResult.Success(AuthenticationState.Connecting, "Verifying two-factor authentication...");

            _credentials.TwoFactorPassword = password;

            _logger.LogDebug("Verifying two-factor authentication");

            // Complete the authentication process with the 2FA password
            var user = await _client.LoginUserIfNeeded();
            if (user != null)
            {
                return await HandleSuccessfulAuthenticationAsync(user);
            }
            else
            {
                return CurrentStatus = AuthResult.Failure(AuthenticationState.AuthenticationFailed, "Authentication failed - no user returned");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify two-factor authentication");
            return CurrentStatus = AuthResult.Failure(AuthenticationState.AuthenticationFailed, "Two-factor authentication failed", ex.Message, ex);
        }
    }

    /// <summary>
    /// Disconnects and clears authentication state
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the disconnect operation</returns>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_client != null)
            {
                _logger.LogDebug("Logging out from Telegram");
                await _client.Auth_LogOut();
                _client.Dispose();
                _client = null;
            }

            _credentials = null;
            _config = null;

            CurrentStatus = AuthResult.Success(AuthenticationState.Disconnected, "Disconnected");
            
            _logger.LogInformation("Successfully disconnected from Telegram");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during disconnect");
            CurrentStatus = AuthResult.Failure(AuthenticationState.ConnectionError, "Disconnect failed", ex.Message, ex);
            throw;
        }
    }

    /// <summary>
    /// Gets current session data for persistence
    /// </summary>
    /// <returns>Session data as string, or null if not available</returns>
    public string? GetSessionData()
    {
        try
        {
            if (_client == null)
            {
                return null;
            }

            // WTelegramClient handles session data internally
            // We can return the phone number as a simple session indicator
            return _credentials?.PhoneNumber;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get session data");
            return null;
        }
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
            if (string.IsNullOrWhiteSpace(sessionData))
            {
                return CurrentStatus = AuthResult.Failure(AuthenticationState.AuthenticationFailed, "Session data cannot be empty");
            }

            if (_client == null || _credentials == null)
            {
                return CurrentStatus = AuthResult.Failure(AuthenticationState.ConnectionError, "Client not initialized. Call InitializeAsync first.");
            }

            CurrentStatus = AuthResult.Success(AuthenticationState.Connecting, "Restoring session...");

            _logger.LogDebug("Attempting to restore session");

            // Set the session data (phone number in this simple case)
            _credentials.PhoneNumber = sessionData;

            // Try to get current user info to validate session
            var me = await _client.LoginUserIfNeeded();
            if (me != null)
            {
                return await HandleSuccessfulAuthenticationAsync(me);
            }
            else
            {
                return CurrentStatus = AuthResult.Failure(AuthenticationState.AuthenticationFailed, "Session restoration failed - unable to verify user");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore session");
            return CurrentStatus = AuthResult.Failure(AuthenticationState.AuthenticationFailed, "Session restoration failed", ex.Message, ex);
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
            if (_client == null)
            {
                return false;
            }

            // Simple connectivity test - just check if we can make a basic call
            await _client.Help_GetConfig();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed");
            return false;
        }
    }

    /// <summary>
    /// Handles successful authentication and updates status
    /// </summary>
    private Task<AuthResult> HandleSuccessfulAuthenticationAsync(User user)
    {
        try
        {
            var userInfo = new TelegramUserInfo
            {
                Id = user.id,
                Username = user.username,
                FirstName = user.first_name ?? string.Empty,
                LastName = user.last_name,
                PhoneNumber = user.phone,
                IsBot = user.flags.HasFlag(User.Flags.bot),
                IsVerified = user.flags.HasFlag(User.Flags.verified)
            };

            // Store session data in credentials for future use
            if (_credentials != null)
            {
                _credentials.SessionData = GetSessionData();
            }

            _logger.LogInformation("Successfully authenticated as: {DisplayName} (ID: {UserId})", userInfo.DisplayName, userInfo.Id);

            return Task.FromResult(CurrentStatus = AuthResult.Success(AuthenticationState.Authenticated, $"Connected as {userInfo.DisplayName}", userInfo));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling successful authentication");
            return Task.FromResult(CurrentStatus = AuthResult.Failure(AuthenticationState.AuthenticationFailed, "Authentication processing failed", ex.Message, ex));
        }
    }

    /// <summary>
    /// Configuration callback for WTelegramClient
    /// </summary>
    private string? Config(string what)
    {
        if (_credentials == null)
        {
            return null;
        }

        var result = what switch
        {
            "api_id" => _credentials.ApiId.ToString(),
            "api_hash" => _credentials.ApiHash,
            "phone_number" => _credentials.PhoneNumber,
            "verification_code" => HandleVerificationCodeRequest(),
            "password" => HandleTwoFactorPasswordRequest(),
            "session_pathname" => _config?.SessionPath ?? "session.dat",
            _ => null
        };

        _logger.LogTrace("Config requested: {ConfigKey} = {ConfigValue}", what, result ?? "null");
        return result;
    }

    /// <summary>
    /// Handles verification code requests from WTelegramClient
    /// </summary>
    private string? HandleVerificationCodeRequest()
    {
        _logger.LogDebug("Verification code requested by WTelegram client");
        return _credentials?.VerificationCode;
    }

    /// <summary>
    /// Handles two-factor password requests from WTelegramClient
    /// </summary>
    private string? HandleTwoFactorPasswordRequest()
    {
        _logger.LogDebug("Two-factor password requested by WTelegram client");
        return _credentials?.TwoFactorPassword;
    }

    /// <summary>
    /// Disposes of the authentication handler
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _client?.Dispose();
            _disposed = true;
        }
    }
}
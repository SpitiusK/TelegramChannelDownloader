using Microsoft.Extensions.Logging;
using System.Net.Http;
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

            // Dispose existing client if any and ensure file handles are released
            await DisposePreviousClientAsync();

            _config = config;
            _credentials = new TelegramCredentials
            {
                ApiId = config.ApiId,
                ApiHash = config.ApiHash
            };

            // Update status to connecting
            CurrentStatus = AuthResult.Success(AuthenticationState.Connecting, "Initializing connection...");

            _logger.LogDebug("Creating WTelegram client with API ID: {ApiId}, API Hash length: {ApiHashLength}", 
                config.ApiId, config.ApiHash?.Length ?? 0);
            
            // Additional validation before connection
            if (config.ApiId <= 0)
            {
                throw new ArgumentException($"Invalid API ID: {config.ApiId}. Must be a positive integer.");
            }
            
            if (string.IsNullOrWhiteSpace(config.ApiHash) || config.ApiHash.Length != 32)
            {
                throw new ArgumentException($"Invalid API Hash: must be exactly 32 characters, got {config.ApiHash?.Length ?? 0} characters.");
            }

            // Create WTelegram client with configuration and retry logic for file conflicts
            _client = await CreateClientWithRetryAsync();
            
            _logger.LogDebug("WTelegram client created, attempting to connect...");

            try
            {
                // Test basic connectivity
                _logger.LogDebug("Attempting to connect to Telegram servers...");
                
                // Try basic network connectivity test first
                try
                {
                    using var httpClient = new HttpClient();
                    httpClient.Timeout = TimeSpan.FromSeconds(10);
                    var response = await httpClient.GetAsync("https://api.telegram.org/", cancellationToken);
                    _logger.LogDebug("Basic HTTP connectivity to Telegram API: {StatusCode}", response.StatusCode);
                }
                catch (Exception httpEx)
                {
                    _logger.LogWarning("HTTP connectivity test failed: {Error}", httpEx.Message);
                }
                
                await _client.ConnectAsync();
                _logger.LogInformation("Successfully connected to Telegram servers");
            }
            catch (Exception connectEx)
            {
                _logger.LogError(connectEx, "Failed to connect to Telegram servers during ConnectAsync. Error: {ErrorType} - {ErrorMessage}", 
                    connectEx.GetType().Name, connectEx.Message);
                
                // Log additional details if available
                if (connectEx.InnerException != null)
                {
                    _logger.LogError("Inner exception: {InnerExceptionType} - {InnerExceptionMessage}", 
                        connectEx.InnerException.GetType().Name, connectEx.InnerException.Message);
                }
                
                // Add more specific error context
                var errorMessage = connectEx.Message.ToLower();
                if (errorMessage.Contains("timeout") || errorMessage.Contains("network"))
                {
                    _logger.LogError("Network connectivity issue detected. This might be due to:");
                    _logger.LogError("1. Firewall blocking Telegram connections");
                    _logger.LogError("2. ISP blocking Telegram in your region");
                    _logger.LogError("3. Proxy configuration needed");
                    _logger.LogError("4. Temporary Telegram server issues");
                }
                else if (errorMessage.Contains("unauthorized") || errorMessage.Contains("invalid"))
                {
                    _logger.LogError("Authentication issue detected. Please verify:");
                    _logger.LogError("1. API ID and Hash are correct from my.telegram.org");
                    _logger.LogError("2. API credentials haven't been revoked");
                }
                
                throw;
            }

            // Connection successful - ready for phone number input
            // Session restoration will be handled in AuthenticatePhoneAsync
            return CurrentStatus = AuthResult.Success(AuthenticationState.WaitingForPhoneNumber, "Connected to Telegram. Please provide your phone number");
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

            CurrentStatus = AuthResult.Success(AuthenticationState.Connecting, "Starting authentication...");

            // Store phone number in credentials
            _credentials.PhoneNumber = phoneNumber;

            _logger.LogDebug("Starting authentication process for phone: {Phone}", phoneNumber.Substring(0, Math.Min(phoneNumber.Length, 5)) + "***");

            // Use WTelegram's non-blocking Login method instead of LoginUserIfNeeded
            // This prevents console blocking and allows UI-driven authentication
            try
            {
                _logger.LogDebug("Calling WTelegram Login with phone number");
                var loginResult = await _client.Login(phoneNumber);
                
                _logger.LogDebug("WTelegram Login result: {LoginResult}", loginResult ?? "null");
                
                // Handle different login stages
                return await HandleLoginResultAsync(loginResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login method failed with phone number");
                return CurrentStatus = AuthResult.Failure(AuthenticationState.AuthenticationFailed, "Authentication failed", ex.Message, ex);
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

            if (CurrentStatus.State != AuthenticationState.WaitingForVerificationCode)
            {
                return CurrentStatus = AuthResult.Failure(AuthenticationState.AuthenticationFailed, "Invalid state for verification code. Use AuthenticatePhoneAsync first.");
            }

            CurrentStatus = AuthResult.Success(AuthenticationState.Connecting, "Verifying code...");

            _credentials.VerificationCode = verificationCode;

            _logger.LogDebug("Verifying authentication code: {CodeLength} digits", verificationCode.Length);

            try
            {
                // Use WTelegram's non-blocking Login method with verification code
                _logger.LogDebug("Calling WTelegram Login with verification code");
                var loginResult = await _client.Login(verificationCode);
                
                _logger.LogDebug("WTelegram Login result after code: {LoginResult}", loginResult ?? "null");
                
                // Handle the result of code verification
                return await HandleLoginResultAsync(loginResult);
            }
            catch (WTelegram.WTException ex) when (ex.Message.Contains("PHONE_CODE_INVALID"))
            {
                _logger.LogWarning("Invalid verification code provided");
                // Return to waiting for verification code state
                _credentials.VerificationCode = null; // Clear invalid code
                return CurrentStatus = AuthResult.Failure(AuthenticationState.WaitingForVerificationCode, "Invalid verification code. Please try again.", ex.Message);
            }
            catch (WTelegram.WTException ex) when (ex.Message.Contains("PHONE_CODE_EXPIRED"))
            {
                _logger.LogWarning("Verification code expired");
                return CurrentStatus = AuthResult.Failure(AuthenticationState.AuthenticationFailed, "Verification code expired. Please restart authentication.", ex.Message);
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

            try
            {
                // Use WTelegram's non-blocking Login method with 2FA password
                _logger.LogDebug("Calling WTelegram Login with 2FA password");
                var loginResult = await _client.Login(password);
                
                _logger.LogDebug("WTelegram Login result after 2FA: {LoginResult}", loginResult ?? "null");
                
                // Handle the result of 2FA verification
                return await HandleLoginResultAsync(loginResult);
            }
            catch (WTelegram.WTException ex) when (ex.Message.Contains("PASSWORD_HASH_INVALID"))
            {
                _logger.LogWarning("Invalid 2FA password provided");
                return CurrentStatus = AuthResult.Failure(AuthenticationState.WaitingForTwoFactorAuth, "Invalid password. Please try again.", ex.Message);
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

            // Check if user is already authenticated (session restored automatically)
            if (_client.User != null)
            {
                _logger.LogDebug("Session restored automatically, user already authenticated");
                return await HandleSuccessfulAuthenticationAsync(_client.User);
            }
            
            // Try to authenticate with the session - use non-blocking Login method
            try
            {
                var loginResult = await _client.Login(sessionData);
                return await HandleLoginResultAsync(loginResult);
            }
            catch (Exception loginEx)
            {
                _logger.LogDebug("Session restoration with Login method failed: {Message}", loginEx.Message);
                return CurrentStatus = AuthResult.Failure(AuthenticationState.AuthenticationFailed, "Session restoration failed - please authenticate again", loginEx.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore session");
            return CurrentStatus = AuthResult.Failure(AuthenticationState.AuthenticationFailed, "Session restoration failed", ex.Message, ex);
        }
    }

    /// <summary>
    /// Gets the underlying WTelegram client for shared use by other services
    /// </summary>
    /// <returns>WTelegram client instance, or null if not initialized</returns>
    public Client? GetClient()
    {
        return _client;
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
    /// Handles the result from WTelegram's Login method and determines next authentication step
    /// </summary>
    /// <param name="loginResult">Result from WTelegram Login call</param>
    /// <returns>Authentication result with appropriate state</returns>
    private async Task<AuthResult> HandleLoginResultAsync(string? loginResult)
    {
        try
        {
            _logger.LogDebug("Processing WTelegram login result: {LoginResult}", loginResult ?? "null");
            
            if (string.IsNullOrEmpty(loginResult))
            {
                // Login successful - user is now authenticated
                if (_client?.User != null)
                {
                    _logger.LogDebug("Login successful, user authenticated");
                    return await HandleSuccessfulAuthenticationAsync(_client.User);
                }
                else
                {
                    _logger.LogWarning("Login result is null but no user found");
                    return CurrentStatus = AuthResult.Failure(AuthenticationState.AuthenticationFailed, "Authentication completed but no user information available");
                }
            }
            
            // Handle different stages of authentication process
            switch (loginResult)
            {
                case "verification_code":
                    _logger.LogDebug("WTelegram requesting verification code");
                    return CurrentStatus = AuthResult.Success(AuthenticationState.WaitingForVerificationCode, "Verification code sent. Please enter the code from your phone.");
                
                case "password":
                    _logger.LogDebug("WTelegram requesting 2FA password");
                    return CurrentStatus = AuthResult.Success(AuthenticationState.WaitingForTwoFactorAuth, "Please enter your two-factor authentication password");
                
                case "first_name":
                    _logger.LogWarning("New user signup required - not supported in this implementation");
                    return CurrentStatus = AuthResult.Failure(AuthenticationState.AuthenticationFailed, "New user signup is not supported. Please create your Telegram account first.");
                
                default:
                    _logger.LogWarning("Unknown login result from WTelegram: {LoginResult}", loginResult);
                    return CurrentStatus = AuthResult.Failure(AuthenticationState.AuthenticationFailed, $"Unexpected authentication stage: {loginResult}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing login result");
            return CurrentStatus = AuthResult.Failure(AuthenticationState.AuthenticationFailed, "Failed to process authentication result", ex.Message, ex);
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
    /// NOTE: With the new Login() method approach, this callback should rarely be used
    /// but we keep it safe to prevent any console blocking issues
    /// </summary>
    private string? Config(string what)
    {
        _logger.LogDebug("WTelegramClient Config callback requested: {ConfigKey}", what);
        
        if (_credentials == null)
        {
            _logger.LogWarning("Config callback called but _credentials is null for key: {ConfigKey}", what);
            return null;
        }

        var result = what switch
        {
            "api_id" => _credentials.ApiId.ToString(),
            "api_hash" => _credentials.ApiHash,
            "phone_number" => _credentials.PhoneNumber,
            // CRITICAL: Never wait for interactive input in Config callback
            // These should return immediately with available data or null
            "verification_code" => _credentials.VerificationCode, // Return immediately, no waiting
            "password" => _credentials.TwoFactorPassword, // Return immediately, no waiting  
            "session_pathname" => _config?.SessionPath ?? "session.dat",
            "server_address" => "149.154.167.50:443", // Try alternative server
            "device_model" => "Desktop",
            "system_version" => "Windows 10",
            "app_version" => "1.0.0",
            "lang_code" => "en",
            _ => null
        };

        _logger.LogDebug("Config callback returning for {ConfigKey}: {ConfigValue}", 
            what, 
            what == "api_hash" || what == "password" ? "[REDACTED]" : result ?? "null");
        return result;
    }


    /// <summary>
    /// Properly disposes of the previous client and ensures file handles are released
    /// </summary>
    private async Task DisposePreviousClientAsync()
    {
        if (_client != null)
        {
            try
            {
                _logger.LogDebug("Disposing existing WTelegram client");
                
                // First try to properly log out to release server-side session
                try
                {
                    await _client.Auth_LogOut();
                }
                catch (Exception logoutEx)
                {
                    _logger.LogDebug("Logout failed during disposal (may be expected): {Message}", logoutEx.Message);
                }
                
                // Dispose the client
                _client.Dispose();
                
                // Wait a brief moment to ensure file handles are released
                await Task.Delay(100);
                
                // Force garbage collection to release any lingering file handles
                GC.Collect();
                GC.WaitForPendingFinalizers();
                
                _client = null;
                _logger.LogDebug("Previous WTelegram client disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during client disposal, proceeding anyway");
                _client = null;
            }
        }
    }

    /// <summary>
    /// Creates WTelegram client with retry logic to handle file access conflicts
    /// </summary>
    private async Task<Client> CreateClientWithRetryAsync()
    {
        const int maxRetries = 3;
        const int delayMs = 500;
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogDebug("Attempting to create WTelegram client (attempt {Attempt}/{MaxRetries})", attempt, maxRetries);
                
                // Try to create the client
                var client = new Client(Config);
                _logger.LogDebug("WTelegram client created successfully on attempt {Attempt}", attempt);
                return client;
            }
            catch (IOException ioEx) when (ioEx.Message.Contains("being used by another process"))
            {
                _logger.LogWarning("Session file locked on attempt {Attempt}: {Message}", attempt, ioEx.Message);
                
                if (attempt == maxRetries)
                {
                    _logger.LogError("Failed to create client after {MaxRetries} attempts. Trying alternative session file.", maxRetries);
                    
                    // As last resort, try with a temporary session file
                    var tempSessionPath = Path.GetTempFileName() + ".session";
                    _logger.LogWarning("Using temporary session file: {TempSessionPath}", tempSessionPath);
                    
                    // Update config to use temp session
                    var originalSessionPath = _config?.SessionPath;
                    if (_config != null)
                    {
                        _config.SessionPath = tempSessionPath;
                    }
                    
                    try
                    {
                        return new Client(Config);
                    }
                    finally
                    {
                        // Restore original session path
                        if (_config != null && originalSessionPath != null)
                        {
                            _config.SessionPath = originalSessionPath;
                        }
                    }
                }
                
                // Wait before retrying
                await Task.Delay(delayMs * attempt);
                
                // Try to force release any lingering handles
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
        
        // This should never be reached due to the throw in the catch block
        throw new InvalidOperationException("Failed to create WTelegram client after all retry attempts");
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
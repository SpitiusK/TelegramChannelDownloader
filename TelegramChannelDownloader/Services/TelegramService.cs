using System.Text.Json;
using TelegramChannelDownloader.Models;
using TelegramChannelDownloader.Utils;
using WTelegram;
using TL;

namespace TelegramChannelDownloader.Services;

/// <summary>
/// Implementation of ITelegramService using WTelegramClient
/// </summary>
public class TelegramService : ITelegramService, IDisposable
{
    private Client? _client;
    private TelegramCredentials? _credentials;
    private AuthenticationStatus _status;
    private TelegramUser? _currentUser;
    private bool _disposed;

    /// <summary>
    /// Event triggered when authentication status changes
    /// </summary>
    public event EventHandler<AuthenticationStatus>? AuthenticationStatusChanged;

    /// <summary>
    /// Current authentication status
    /// </summary>
    public AuthenticationStatus Status 
    { 
        get => _status; 
        private set 
        {
            _status = value;
            AuthenticationStatusChanged?.Invoke(this, _status);
        }
    }

    /// <summary>
    /// Indicates if the client is currently connected and authenticated
    /// </summary>
    public bool IsConnected => Status.IsConnected;

    /// <summary>
    /// Current authenticated user information (null if not authenticated)
    /// </summary>
    public TelegramUser? CurrentUser => _currentUser;

    /// <summary>
    /// Initializes a new instance of TelegramService
    /// </summary>
    public TelegramService()
    {
        _status = new AuthenticationStatus
        {
            State = AuthenticationState.Disconnected,
            Message = "Not connected"
        };
    }

    /// <summary>
    /// Initializes the Telegram client with API credentials
    /// </summary>
    /// <param name="credentials">API credentials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the initialization operation</returns>
    public async Task InitializeAsync(TelegramCredentials credentials, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!credentials.IsValid)
            {
                throw new ArgumentException("Invalid credentials provided", nameof(credentials));
            }

            // Dispose existing client if any
            _client?.Dispose();

            _credentials = credentials;

            // Update status to connecting
            Status = new AuthenticationStatus
            {
                State = AuthenticationState.Connecting,
                Message = "Initializing connection..."
            };

            // Create WTelegram client with configuration
            _client = new Client(Config);

            // Test basic connectivity
            await _client.ConnectAsync();

            // Check if we have session data to restore
            if (credentials.HasSessionData)
            {
                try
                {
                    await RestoreSessionFromCredentialsAsync(cancellationToken);
                }
                catch
                {
                    // If session restoration fails, continue with normal auth flow
                    Status = new AuthenticationStatus
                    {
                        State = AuthenticationState.WaitingForPhoneNumber,
                        Message = "Please provide your phone number"
                    };
                }
            }
            else
            {
                Status = new AuthenticationStatus
                {
                    State = AuthenticationState.WaitingForPhoneNumber,
                    Message = "Please provide your phone number"
                };
            }
        }
        catch (Exception ex)
        {
            Status = new AuthenticationStatus
            {
                State = AuthenticationState.ConnectionError,
                Message = "Connection failed",
                ErrorMessage = ex.Message
            };
            throw new InvalidOperationException($"Failed to initialize Telegram client: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Starts the authentication process with phone number
    /// </summary>
    /// <param name="phoneNumber">Phone number including country code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the phone authentication operation</returns>
    public async Task AuthenticateWithPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_client == null)
            {
                throw new InvalidOperationException("Client not initialized. Call InitializeAsync first.");
            }

            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                throw new ArgumentException("Phone number cannot be empty", nameof(phoneNumber));
            }

            Status = new AuthenticationStatus
            {
                State = AuthenticationState.Connecting,
                Message = "Sending verification code..."
            };

            // Store phone number in credentials
            if (_credentials != null)
            {
                _credentials.PhoneNumber = phoneNumber;
            }

            // Start the authentication process
            // This will trigger the Config callback which will provide the phone number
            // WTelegramClient will then request verification code
            try
            {
                var user = await _client.LoginUserIfNeeded();
                if (user != null)
                {
                    await HandleSuccessfulAuthenticationAsync(user, cancellationToken);
                }
                else
                {
                    // If no user returned but no exception, likely waiting for verification code
                    Status = new AuthenticationStatus
                    {
                        State = AuthenticationState.WaitingForVerificationCode,
                        Message = "Please enter the verification code sent to your phone"
                    };
                }
            }
            catch (WTelegram.WTException ex) when (ex.Message.Contains("phone_code") || ex.Message.Contains("PHONE_CODE"))
            {
                // Verification code is needed
                Status = new AuthenticationStatus
                {
                    State = AuthenticationState.WaitingForVerificationCode,
                    Message = "Please enter the verification code sent to your phone"
                };
            }
            catch (WTelegram.WTException ex) when (ex.Message.Contains("SESSION_PASSWORD_NEEDED"))
            {
                // Two-factor authentication is needed
                Status = new AuthenticationStatus
                {
                    State = AuthenticationState.WaitingForTwoFactorAuth,
                    Message = "Please enter your two-factor authentication password"
                };
            }
            catch (WTelegram.WTException ex)
            {
                // Log the actual exception message for debugging
                System.Diagnostics.Debug.WriteLine($"WTelegram exception: {ex.Message}");
                
                // Check if it might be asking for verification code
                if (ex.Message.ToUpper().Contains("CODE") || ex.Message.ToUpper().Contains("VERIFICATION"))
                {
                    Status = new AuthenticationStatus
                    {
                        State = AuthenticationState.WaitingForVerificationCode,
                        Message = "Please enter the verification code sent to your phone"
                    };
                }
                else
                {
                    throw; // Re-throw if it's not a code-related exception
                }
            }
        }
        catch (Exception ex)
        {
            Status = new AuthenticationStatus
            {
                State = AuthenticationState.AuthenticationFailed,
                Message = "Authentication failed",
                ErrorMessage = ex.Message
            };
            throw new InvalidOperationException($"Failed to authenticate: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Completes authentication with verification code received via SMS/app
    /// </summary>
    /// <param name="verificationCode">Verification code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the code verification operation</returns>
    public async Task VerifyCodeAsync(string verificationCode, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_client == null || _credentials == null)
            {
                throw new InvalidOperationException("Client not initialized. Use AuthenticateWithPhoneAsync instead.");
            }

            if (string.IsNullOrWhiteSpace(verificationCode))
            {
                throw new ArgumentException("Verification code cannot be empty", nameof(verificationCode));
            }

            Status = new AuthenticationStatus
            {
                State = AuthenticationState.Connecting,
                Message = "Verifying code..."
            };

            _credentials.VerificationCode = verificationCode;

            // Continue the authentication process with the verification code
            try
            {
                var user = await _client.LoginUserIfNeeded();
                if (user != null)
                {
                    await HandleSuccessfulAuthenticationAsync(user, cancellationToken);
                }
            }
            catch (WTelegram.WTException ex) when (ex.Message.Contains("SESSION_PASSWORD_NEEDED"))
            {
                // Two-factor authentication is needed
                Status = new AuthenticationStatus
                {
                    State = AuthenticationState.WaitingForTwoFactorAuth,
                    Message = "Please enter your two-factor authentication password"
                };
            }
        }
        catch (Exception ex)
        {
            Status = new AuthenticationStatus
            {
                State = AuthenticationState.AuthenticationFailed,
                Message = "Code verification failed",
                ErrorMessage = ex.Message
            };
            throw new InvalidOperationException($"Failed to verify code: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Completes authentication with two-factor authentication password
    /// </summary>
    /// <param name="password">Two-factor authentication password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the two-factor authentication operation</returns>
    public async Task VerifyTwoFactorAuthAsync(string password, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_client == null || _credentials == null)
            {
                throw new InvalidOperationException("Client not initialized. Use AuthenticateWithPhoneAsync instead.");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password cannot be empty", nameof(password));
            }

            Status = new AuthenticationStatus
            {
                State = AuthenticationState.Connecting,
                Message = "Verifying two-factor authentication..."
            };

            _credentials.TwoFactorPassword = password;

            // Complete the authentication process with the 2FA password
            var user = await _client.LoginUserIfNeeded();
            if (user != null)
            {
                await HandleSuccessfulAuthenticationAsync(user, cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("Authentication failed - no user returned");
            }
        }
        catch (Exception ex)
        {
            Status = new AuthenticationStatus
            {
                State = AuthenticationState.AuthenticationFailed,
                Message = "Two-factor authentication failed",
                ErrorMessage = ex.Message
            };
            throw new InvalidOperationException($"Failed to verify two-factor authentication: {ex.Message}", ex);
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
            if (_client != null)
            {
                await _client.Auth_LogOut();
                _client.Dispose();
                _client = null;
            }

            _currentUser = null;
            _credentials = null;

            Status = new AuthenticationStatus
            {
                State = AuthenticationState.Disconnected,
                Message = "Disconnected"
            };
        }
        catch (Exception ex)
        {
            Status = new AuthenticationStatus
            {
                State = AuthenticationState.Disconnected,
                Message = "Disconnected with errors",
                ErrorMessage = ex.Message
            };
            // Don't throw here as disconnect should always succeed
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
            if (_client == null || !IsConnected)
            {
                return null;
            }

            // WTelegramClient handles session automatically via config callback
            // For now, return a placeholder - session persistence will be improved later
            return "session_placeholder";
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Restores session from previously saved session data
    /// </summary>
    /// <param name="sessionData">Previously saved session data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the session restoration operation</returns>
    public async Task RestoreSessionAsync(string sessionData, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sessionData))
            {
                throw new ArgumentException("Session data cannot be empty", nameof(sessionData));
            }

            if (_client == null)
            {
                throw new InvalidOperationException("Client not initialized. Call InitializeAsync first.");
            }

            Status = new AuthenticationStatus
            {
                State = AuthenticationState.Connecting,
                Message = "Restoring session..."
            };

            // For now, session restoration will be handled by WTelegramClient's built-in mechanism
            // This is a placeholder for future implementation
            var me = await _client.LoginUserIfNeeded();
            if (me != null)
            {
                await HandleSuccessfulAuthenticationAsync(me, cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("Session restoration failed - unable to verify user");
            }
        }
        catch (Exception ex)
        {
            Status = new AuthenticationStatus
            {
                State = AuthenticationState.AuthenticationFailed,
                Message = "Session restoration failed",
                ErrorMessage = ex.Message
            };
            throw new InvalidOperationException($"Failed to restore session: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Tests the connection to Telegram servers
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the connection test operation</returns>
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_client == null)
            {
                return false;
            }

            // Test connection by calling a simple API method
            await _client.Help_GetConfig();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets comprehensive information about a Telegram channel by username or URL
    /// </summary>
    /// <param name="channelUrl">Channel URL, username, or identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the channel information retrieval operation</returns>
    public async Task<ChannelInfo?> GetChannelInfoAsync(string channelUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_client == null || !IsConnected)
            {
                return new ChannelInfo
                {
                    ErrorMessage = "Client not connected. Please authenticate first.",
                    IsAccessible = false
                };
            }

            // First validate and parse the channel URL
            var validation = ValidateChannelUrl(channelUrl);
            if (!validation.IsValid || validation.ChannelInfo == null)
            {
                return validation.ChannelInfo ?? new ChannelInfo
                {
                    ErrorMessage = validation.ErrorMessage ?? "Invalid channel URL",
                    IsAccessible = false
                };
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

            // Resolve channel by username
            var resolved = await _client.Contacts_ResolveUsername(username);
            var channel = resolved.chats?.Values.FirstOrDefault();

            if (channel == null)
            {
                return new ChannelInfo
                {
                    Username = username,
                    ErrorMessage = "Channel not found or not accessible",
                    IsAccessible = false
                };
            }

            // Convert TL chat object to ChannelInfo
            var channelInfo = await ConvertToChannelInfoAsync(channel, cancellationToken);
            return channelInfo;
        }
        catch (WTelegram.WTException ex) when (ex.Message.Contains("USERNAME_NOT_OCCUPIED"))
        {
            return new ChannelInfo
            {
                ErrorMessage = "Channel username does not exist",
                IsAccessible = false
            };
        }
        catch (WTelegram.WTException ex) when (ex.Message.Contains("USERNAME_INVALID"))
        {
            return new ChannelInfo
            {
                ErrorMessage = "Invalid channel username format",
                IsAccessible = false
            };
        }
        catch (WTelegram.WTException ex) when (ex.Message.Contains("CHAT_FORBIDDEN"))
        {
            return new ChannelInfo
            {
                ErrorMessage = "Access to this channel is forbidden",
                IsAccessible = false
            };
        }
        catch (Exception ex)
        {
            return new ChannelInfo
            {
                ErrorMessage = $"Failed to get channel information: {ex.Message}",
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
            var parseResult = ValidationHelper.ParseChannelUrl(channelUrl);
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
            return new ChannelValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Error validating channel URL: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Checks if a channel exists and is accessible to the current user
    /// </summary>
    /// <param name="channelUsername">Clean channel username (without @ or URL parts)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the channel existence check operation</returns>
    public async Task<bool> CheckChannelAccessAsync(string channelUsername, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_client == null || !IsConnected)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(channelUsername))
            {
                return false;
            }

            // Try to resolve the channel
            var resolved = await _client.Contacts_ResolveUsername(channelUsername);
            return resolved.chats?.Count > 0;
        }
        catch (WTelegram.WTException ex) when (
            ex.Message.Contains("USERNAME_NOT_OCCUPIED") ||
            ex.Message.Contains("USERNAME_INVALID") ||
            ex.Message.Contains("CHAT_FORBIDDEN"))
        {
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the total message count for a channel
    /// </summary>
    /// <param name="channelUsername">Clean channel username (without @ or URL parts)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the message count retrieval operation</returns>
    public async Task<int> GetChannelMessageCountAsync(string channelUsername, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_client == null || !IsConnected)
            {
                return 0;
            }

            if (string.IsNullOrWhiteSpace(channelUsername))
            {
                return 0;
            }

            // First resolve the channel
            var resolved = await _client.Contacts_ResolveUsername(channelUsername);
            var channel = resolved.chats?.Values.FirstOrDefault();

            if (channel == null)
            {
                return 0;
            }

            // Get channel messages to determine count
            var history = await _client.Messages_GetHistory(channel, limit: 1);
            return history.Count;
        }
        catch (Exception)
        {
            return 0;
        }
    }

    /// <summary>
    /// Downloads all messages from a specified channel with progress reporting
    /// </summary>
    /// <param name="channelInfo">Channel information obtained from GetChannelInfoAsync</param>
    /// <param name="progress">Progress reporter for download status updates</param>
    /// <param name="cancellationToken">Cancellation token to cancel the download operation</param>
    /// <returns>Task representing the download operation, returning list of downloaded messages</returns>
    public async Task<List<MessageData>> DownloadChannelMessagesAsync(ChannelInfo channelInfo, IProgress<DownloadProgressInfo>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_client == null || !IsConnected)
            {
                throw new InvalidOperationException("Client not connected. Please authenticate first.");
            }

            if (channelInfo == null || !channelInfo.IsAccessible)
            {
                throw new ArgumentException("Invalid or inaccessible channel information", nameof(channelInfo));
            }

            var allMessages = new List<MessageData>();
            var startTime = DateTime.UtcNow;

            // Resolve channel by ID
            var peer = new InputPeerChannel(channelInfo.Id, channelInfo.AccessHash ?? 0);
            
            // Get initial message count estimate
            var initialHistory = await _client.Messages_GetHistory(peer, limit: 1);
            var totalEstimate = initialHistory.Count;

            var progressInfo = new DownloadProgressInfo
            {
                TotalMessages = totalEstimate,
                DownloadedMessages = 0,
                MessagesPerSecond = 0
            };

            progress?.Report(progressInfo);

            const int batchSize = 100; // Optimal batch size for Telegram API
            int offsetId = 0;
            int processedCount = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    // Add delay to respect rate limits
                    if (processedCount > 0)
                    {
                        await Task.Delay(200, cancellationToken); // 200ms delay between batches
                    }

                    var history = await _client.Messages_GetHistory(
                        peer: peer,
                        offset_id: offsetId,
                        limit: batchSize
                    );

                    if (history.Messages == null || history.Messages.Length == 0)
                    {
                        break; // No more messages
                    }

                    // Process messages in the current batch
                    var batchMessages = new List<MessageData>();
                    foreach (var msg in history.Messages)
                    {
                        if (msg is TL.Message telegramMessage)
                        {
                            var messageData = ConvertTelegramMessageToMessageData(telegramMessage, channelInfo);
                            if (messageData != null)
                            {
                                batchMessages.Add(messageData);
                            }
                        }
                    }

                    allMessages.AddRange(batchMessages);
                    processedCount += batchMessages.Count;

                    // Update offset for next batch
                    offsetId = history.Messages.Last().ID;

                    // Calculate performance metrics
                    var elapsed = DateTime.UtcNow - startTime;
                    var messagesPerSecond = elapsed.TotalSeconds > 0 ? processedCount / elapsed.TotalSeconds : 0;

                    // Update progress every 50 messages or at end of batch
                    if (processedCount % 50 == 0 || batchMessages.Count < batchSize)
                    {
                        progressInfo = new DownloadProgressInfo
                        {
                            TotalMessages = Math.Max(totalEstimate, processedCount),
                            DownloadedMessages = processedCount,
                            MessagesPerSecond = messagesPerSecond,
                            CurrentMessage = batchMessages.LastOrDefault(),
                            EstimatedTimeRemaining = messagesPerSecond > 0 && totalEstimate > processedCount 
                                ? TimeSpan.FromSeconds((totalEstimate - processedCount) / messagesPerSecond) 
                                : null
                        };

                        progress?.Report(progressInfo);
                    }

                    // Break if we got fewer messages than requested (end of channel)
                    if (history.Messages.Length < batchSize)
                    {
                        break;
                    }
                }
                catch (WTelegram.WTException ex) when (ex.Message.Contains("FLOOD_WAIT"))
                {
                    // Handle rate limiting
                    var waitTime = ExtractFloodWaitTime(ex.Message);
                    progressInfo.ErrorMessage = $"Rate limited. Waiting {waitTime} seconds...";
                    progress?.Report(progressInfo);
                    
                    await Task.Delay(TimeSpan.FromSeconds(waitTime), cancellationToken);
                    progressInfo.ErrorMessage = null;
                    continue;
                }
            }

            // Final progress update
            var finalElapsed = DateTime.UtcNow - startTime;
            var finalMessagesPerSecond = finalElapsed.TotalSeconds > 0 ? processedCount / finalElapsed.TotalSeconds : 0;

            progressInfo = new DownloadProgressInfo
            {
                TotalMessages = processedCount,
                DownloadedMessages = processedCount,
                MessagesPerSecond = finalMessagesPerSecond,
                EstimatedTimeRemaining = TimeSpan.Zero
            };

            progress?.Report(progressInfo);

            // Sort messages by timestamp (oldest first)
            allMessages.Sort((x, y) => x.Timestamp.CompareTo(y.Timestamp));

            return allMessages;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to download channel messages: {ex.Message}", ex);
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
    public async IAsyncEnumerable<List<MessageData>> DownloadChannelMessagesBatchAsync(
        ChannelInfo channelInfo, 
        int batchSize = 100, 
        IProgress<DownloadProgressInfo>? progress = null, 
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_client == null || !IsConnected)
        {
            throw new InvalidOperationException("Client not connected. Please authenticate first.");
        }

        if (channelInfo == null || !channelInfo.IsAccessible)
        {
            throw new ArgumentException("Invalid or inaccessible channel information", nameof(channelInfo));
        }

        if (batchSize <= 0 || batchSize > 100)
        {
            batchSize = 100; // Clamp to Telegram API limits
        }

        var startTime = DateTime.UtcNow;
        var peer = new InputPeerChannel(channelInfo.Id, channelInfo.AccessHash ?? 0);
        
        // Get initial count estimate
        var initialHistory = await _client.Messages_GetHistory(peer, limit: 1);
        var totalEstimate = initialHistory.Count;

        int offsetId = 0;
        int totalProcessed = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Add delay to respect rate limits
            if (totalProcessed > 0)
            {
                await Task.Delay(200, cancellationToken);
            }

            TL.Messages_MessagesBase history;
            try
            {
                history = await _client.Messages_GetHistory(
                    peer: peer,
                    offset_id: offsetId,
                    limit: batchSize
                );
            }
            catch (WTelegram.WTException ex) when (ex.Message.Contains("FLOOD_WAIT"))
            {
                // Handle rate limiting
                var waitTime = ExtractFloodWaitTime(ex.Message);
                var progressInfo = new DownloadProgressInfo
                {
                    TotalMessages = totalEstimate,
                    DownloadedMessages = totalProcessed,
                    ErrorMessage = $"Rate limited. Waiting {waitTime} seconds..."
                };
                progress?.Report(progressInfo);
                
                await Task.Delay(TimeSpan.FromSeconds(waitTime), cancellationToken);
                continue;
            }

            if (history.Messages == null || history.Messages.Length == 0)
            {
                break; // No more messages
            }

            // Convert messages to MessageData
            var batchMessages = new List<MessageData>();
            foreach (var msg in history.Messages)
            {
                if (msg is TL.Message telegramMessage)
                {
                    var messageData = ConvertTelegramMessageToMessageData(telegramMessage, channelInfo);
                    if (messageData != null)
                    {
                        batchMessages.Add(messageData);
                    }
                }
            }

            totalProcessed += batchMessages.Count;
            offsetId = history.Messages.Last().ID;

            // Calculate performance metrics
            var elapsed = DateTime.UtcNow - startTime;
            var messagesPerSecond = elapsed.TotalSeconds > 0 ? totalProcessed / elapsed.TotalSeconds : 0;

            // Report progress
            var progressInfo2 = new DownloadProgressInfo
            {
                TotalMessages = Math.Max(totalEstimate, totalProcessed),
                DownloadedMessages = totalProcessed,
                MessagesPerSecond = messagesPerSecond,
                CurrentMessage = batchMessages.LastOrDefault(),
                EstimatedTimeRemaining = messagesPerSecond > 0 && totalEstimate > totalProcessed 
                    ? TimeSpan.FromSeconds((totalEstimate - totalProcessed) / messagesPerSecond) 
                    : null
            };

            progress?.Report(progressInfo2);

            // Yield the batch
            yield return batchMessages;

            // Break if we got fewer messages than requested
            if (history.Messages.Length < batchSize)
            {
                break;
            }
        }

        // Final progress update
        var finalElapsed = DateTime.UtcNow - startTime;
        var finalMessagesPerSecond = finalElapsed.TotalSeconds > 0 ? totalProcessed / finalElapsed.TotalSeconds : 0;

        var finalProgressInfo = new DownloadProgressInfo
        {
            TotalMessages = totalProcessed,
            DownloadedMessages = totalProcessed,
            MessagesPerSecond = finalMessagesPerSecond,
            EstimatedTimeRemaining = TimeSpan.Zero
        };

        progress?.Report(finalProgressInfo);
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
            if (messages == null || messages.Count == 0)
            {
                throw new ArgumentException("No messages to export", nameof(messages));
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException("Output path cannot be empty", nameof(outputPath));
            }

            // Ensure directory exists
            var directory = System.IO.Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(directory) && !System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            using var writer = new System.IO.StreamWriter(outputPath, false, System.Text.Encoding.UTF8);

            // Write header
            await writer.WriteLineAsync($"# {channelInfo?.Title ?? "Telegram Channel Export"}");
            await writer.WriteLineAsync();
            
            if (channelInfo != null)
            {
                await writer.WriteLineAsync($"**Channel:** {channelInfo.Title}");
                if (!string.IsNullOrWhiteSpace(channelInfo.Username))
                {
                    await writer.WriteLineAsync($"**Username:** @{channelInfo.Username}");
                }
                if (!string.IsNullOrWhiteSpace(channelInfo.Description))
                {
                    await writer.WriteLineAsync($"**Description:** {channelInfo.Description}");
                }
                await writer.WriteLineAsync($"**Export Date:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                await writer.WriteLineAsync($"**Total Messages:** {messages.Count:N0}");
                await writer.WriteLineAsync();
            }

            await writer.WriteLineAsync("---");
            await writer.WriteLineAsync();

            // Sort messages by timestamp (oldest first)
            var sortedMessages = messages.OrderBy(m => m.Timestamp).ToList();

            // Export messages in the specified format: **timestamp** | **sender** \n message content
            foreach (var message in sortedMessages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Format timestamp
                var timestampStr = message.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                
                // Format sender
                var senderStr = message.SenderDisplay;

                // Write message header
                await writer.WriteLineAsync($"**{timestampStr}** | **{senderStr}**");
                
                // Write message content
                var content = message.FormattedContent;
                if (!string.IsNullOrWhiteSpace(content))
                {
                    // Escape markdown characters in content to prevent formatting issues
                    content = EscapeMarkdown(content);
                    await writer.WriteLineAsync(content);
                }

                // Add forward info if present
                if (message.ForwardInfo != null)
                {
                    var forwardSource = !string.IsNullOrWhiteSpace(message.ForwardInfo.OriginalChannel) 
                        ? message.ForwardInfo.OriginalChannel
                        : message.ForwardInfo.OriginalSender ?? "Unknown";
                    await writer.WriteLineAsync($"*Forwarded from: {forwardSource}*");
                }

                // Add reply info if present
                if (message.ReplyInfo != null && !string.IsNullOrWhiteSpace(message.ReplyInfo.OriginalMessagePreview))
                {
                    await writer.WriteLineAsync($"*In reply to: {EscapeMarkdown(message.ReplyInfo.OriginalMessagePreview)}*");
                }

                // Add links, mentions, hashtags if present
                if (message.Links.Any())
                {
                    await writer.WriteLineAsync($"*Links: {string.Join(", ", message.Links.Take(5))}*");
                }

                if (message.Mentions.Any())
                {
                    await writer.WriteLineAsync($"*Mentions: {string.Join(", ", message.Mentions.Take(10).Select(m => "@" + m))}*");
                }

                if (message.Hashtags.Any())
                {
                    await writer.WriteLineAsync($"*Hashtags: {string.Join(", ", message.Hashtags.Take(10).Select(h => "#" + h))}*");
                }

                // Add views if available
                if (message.Views.HasValue && message.Views.Value > 0)
                {
                    await writer.WriteLineAsync($"*Views: {message.Views.Value:N0}*");
                }

                // Add media info if present
                if (message.Media != null)
                {
                    await writer.WriteLineAsync($"*Media: {GetMediaTypeDescription(message.MessageType)}*");
                    if (message.Media.FileSize.HasValue)
                    {
                        await writer.WriteLineAsync($"*File Size: {FormatFileSize(message.Media.FileSize.Value)}*");
                    }
                }

                await writer.WriteLineAsync(); // Empty line between messages
            }

            // Write footer
            await writer.WriteLineAsync("---");
            await writer.WriteLineAsync($"*Export completed at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC*");
        }
        catch (OperationCanceledException)
        {
            // Clean up partial file if operation was cancelled
            try
            {
                if (System.IO.File.Exists(outputPath))
                {
                    System.IO.File.Delete(outputPath);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to export messages to markdown: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Converts a TL chat object to ChannelInfo with comprehensive metadata
    /// </summary>
    /// <param name="chat">TL chat object from Telegram API</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the conversion operation</returns>
    private async Task<ChannelInfo> ConvertToChannelInfoAsync(ChatBase chat, CancellationToken cancellationToken = default)
    {
        var channelInfo = new ChannelInfo();

        try
        {
            channelInfo.Id = chat.ID;
            channelInfo.IsAccessible = true;

            switch (chat)
            {
                case Channel channel:
                    channelInfo.Title = channel.title ?? string.Empty;
                    channelInfo.Username = channel.username;
                    channelInfo.AccessHash = channel.access_hash;
                    channelInfo.IsVerified = channel.flags.HasFlag(Channel.Flags.verified);
                    channelInfo.IsScam = channel.flags.HasFlag(Channel.Flags.scam);
                    channelInfo.IsRestricted = channel.flags.HasFlag(Channel.Flags.restricted);
                    channelInfo.HasProtectedContent = channel.flags.HasFlag(Channel.Flags.noforwards);

                    // Determine channel type
                    if (channel.flags.HasFlag(Channel.Flags.broadcast))
                    {
                        channelInfo.Type = string.IsNullOrWhiteSpace(channel.username) ? 
                            ChannelType.PrivateChannel : ChannelType.Channel;
                    }
                    else
                    {
                        channelInfo.Type = string.IsNullOrWhiteSpace(channel.username) ? 
                            ChannelType.PrivateSupergroup : ChannelType.Supergroup;
                    }

                    // Get additional channel information
                    try
                    {
                        var fullChannel = await _client.Channels_GetFullChannel(channel);
                        if (fullChannel.full_chat is ChannelFull fullChannelInfo)
                        {
                            channelInfo.Description = fullChannelInfo.about;
                            channelInfo.MemberCount = fullChannelInfo.participants_count;
                            
                            // Get message count from recent messages
                            var history = await _client.Messages_GetHistory(channel, limit: 1);
                            channelInfo.MessageCount = history.Count;

                            // Set creation date if available
                            try
                            {
                                channelInfo.CreatedDate = channel.date;
                            }
                            catch
                            {
                                // If date conversion fails, set to null
                                channelInfo.CreatedDate = null;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // If we can't get full info, continue with basic info
                        // Try to get message count directly
                        try
                        {
                            var history = await _client.Messages_GetHistory(channel, limit: 1);
                            channelInfo.MessageCount = history.Count;
                        }
                        catch (Exception)
                        {
                            channelInfo.MessageCount = 0;
                        }
                    }
                    break;

                case Chat group:
                    channelInfo.Title = group.title ?? string.Empty;
                    channelInfo.Type = ChannelType.Group;
                    channelInfo.MemberCount = group.participants_count;
                    try
                    {
                        channelInfo.CreatedDate = group.date;
                    }
                    catch
                    {
                        channelInfo.CreatedDate = null;
                    }
                    channelInfo.IsRestricted = group.flags.HasFlag(Chat.Flags.deactivated);

                    // Get message count for group
                    try
                    {
                        var history = await _client.Messages_GetHistory(group, limit: 1);
                        channelInfo.MessageCount = history.Count;
                    }
                    catch (Exception)
                    {
                        channelInfo.MessageCount = 0;
                    }
                    break;

                default:
                    channelInfo.Title = "Unknown Chat Type";
                    channelInfo.Type = ChannelType.Unknown;
                    break;
            }

            // Set last activity date to current time if we successfully got the info
            channelInfo.LastActivityDate = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            channelInfo.ErrorMessage = $"Error retrieving channel details: {ex.Message}";
            channelInfo.IsAccessible = false;
        }

        return channelInfo;
    }

    /// <summary>
    /// Configuration callback for WTelegramClient
    /// </summary>
    /// <param name="what">Configuration parameter name</param>
    /// <returns>Configuration value</returns>
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
            "session_pathname" => "session.dat", // Let WTelegramClient handle session file
            _ => null
        };

        System.Diagnostics.Debug.WriteLine($"Config requested: {what} = {result ?? "null"}");
        return result;
    }

    private string? HandleVerificationCodeRequest()
    {
        // If verification code is requested but we don't have one, update the UI state
        if (string.IsNullOrWhiteSpace(_credentials?.VerificationCode))
        {
            Status = new AuthenticationStatus
            {
                State = AuthenticationState.WaitingForVerificationCode,
                Message = "Please enter the verification code sent to your phone"
            };
            return null; // Return null so WTelegramClient waits
        }
        
        return _credentials.VerificationCode;
    }

    private string? HandleTwoFactorPasswordRequest()
    {
        // If 2FA password is requested but we don't have one, update the UI state
        if (string.IsNullOrWhiteSpace(_credentials?.TwoFactorPassword))
        {
            Status = new AuthenticationStatus
            {
                State = AuthenticationState.WaitingForTwoFactorAuth,
                Message = "Please enter your two-factor authentication password"
            };
            return null; // Return null so WTelegramClient waits
        }
        
        return _credentials.TwoFactorPassword;
    }

    /// <summary>
    /// Handles successful authentication by updating status and user information
    /// </summary>
    /// <param name="user">Authenticated user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    private Task HandleSuccessfulAuthenticationAsync(User user, CancellationToken cancellationToken = default)
    {
        try
        {
            _currentUser = new TelegramUser
            {
                Id = user.id,
                Username = user.username,
                FirstName = user.first_name ?? string.Empty,
                LastName = user.last_name,
                PhoneNumber = user.phone
            };

            // Store session data in credentials for future use
            if (_credentials != null)
            {
                _credentials.SessionData = GetSessionData();
            }

            Status = new AuthenticationStatus
            {
                State = AuthenticationState.Authenticated,
                Message = $"Connected as {_currentUser.DisplayName}",
                User = _currentUser
            };

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Status = new AuthenticationStatus
            {
                State = AuthenticationState.AuthenticationFailed,
                Message = "Authentication completed but failed to get user info",
                ErrorMessage = ex.Message
            };
            throw;
        }
    }

    /// <summary>
    /// Attempts to restore session from credentials
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    private async Task RestoreSessionFromCredentialsAsync(CancellationToken cancellationToken = default)
    {
        if (_credentials?.SessionData != null)
        {
            await RestoreSessionAsync(_credentials.SessionData, cancellationToken);
        }
    }

    /// <summary>
    /// Converts a Telegram Message object to MessageData
    /// </summary>
    /// <param name="telegramMessage">Telegram message from WTelegramClient</param>
    /// <param name="channelInfo">Channel information</param>
    /// <returns>Converted MessageData object</returns>
    private MessageData? ConvertTelegramMessageToMessageData(TL.Message telegramMessage, ChannelInfo channelInfo)
    {
        try
        {
            var messageData = new MessageData
            {
                MessageId = telegramMessage.id,
                Timestamp = telegramMessage.date,
                Content = telegramMessage.message ?? string.Empty,
                ChannelId = channelInfo.Id,
                ChannelTitle = channelInfo.Title ?? string.Empty,
                Views = telegramMessage.views,
                IsEdited = telegramMessage.edit_date != default,
                EditedTimestamp = telegramMessage.edit_date
            };

            // Set sender information
            if (telegramMessage.from_id != null)
            {
                messageData.SenderId = telegramMessage.from_id.ID;
                // Note: To get sender display name and username, we'd need to resolve the peer
                // For now, we'll use the ID and set display name generically
                messageData.SenderDisplayName = $"User {telegramMessage.from_id.ID}";
            }

            // Process message type and media
            ProcessMessageTypeAndMedia(telegramMessage, messageData);

            // Process forward information
            if (telegramMessage.fwd_from != null)
            {
                messageData.ForwardInfo = new ForwardInfo
                {
                    OriginalDate = telegramMessage.fwd_from.date,
                    OriginalSender = telegramMessage.fwd_from.from_name,
                    OriginalMessageId = telegramMessage.fwd_from.channel_post
                };

                if (telegramMessage.fwd_from.from_id != null)
                {
                    messageData.ForwardInfo.OriginalSender = $"User {telegramMessage.fwd_from.from_id.ID}";
                }
            }

            // Process reply information
            if (telegramMessage.reply_to is MessageReplyHeader replyHeader && replyHeader.reply_to_msg_id != 0)
            {
                messageData.ReplyInfo = new ReplyInfo
                {
                    ReplyToMessageId = replyHeader.reply_to_msg_id,
                    OriginalMessagePreview = "Reply message" // Would need additional API call to get full content
                };
            }

            // Process entities (mentions, links, hashtags)
            if (telegramMessage.entities != null && !string.IsNullOrWhiteSpace(messageData.Content))
            {
                ProcessMessageEntities(telegramMessage.entities, messageData);
            }

            // Process content to extract additional structured data
            messageData.ProcessContent();

            return messageData;
        }
        catch (Exception)
        {
            // If conversion fails, skip this message
            return null;
        }
    }

    /// <summary>
    /// Processes message type and media information
    /// </summary>
    /// <param name="telegramMessage">Telegram message</param>
    /// <param name="messageData">MessageData to populate</param>
    private void ProcessMessageTypeAndMedia(TL.Message telegramMessage, MessageData messageData)
    {
        if (telegramMessage.media == null)
        {
            messageData.MessageType = MessageType.Text;
            return;
        }

        var mediaInfo = new MediaInfo();

        switch (telegramMessage.media)
        {
            case MessageMediaPhoto photo:
                messageData.MessageType = MessageType.Photo;
                if (photo.photo is Photo photoObj)
                {
                    // Get the largest photo size
                    var largestSize = photoObj.sizes?.OfType<PhotoSize>().OrderByDescending(s => s.w * s.h).FirstOrDefault();
                    if (largestSize != null)
                    {
                        mediaInfo.Width = largestSize.w;
                        mediaInfo.Height = largestSize.h;
                    }
                }
                break;

            case MessageMediaDocument doc:
                if (doc.document is Document document)
                {
                    mediaInfo.FileName = GetDocumentFileName(document);
                    mediaInfo.FileSize = document.size;
                    mediaInfo.MimeType = document.mime_type;
                    mediaInfo.FileId = document.id.ToString();

                    // Determine message type based on MIME type
                    messageData.MessageType = GetMessageTypeFromMimeType(document.mime_type);

                    // Get additional attributes
                    if (document.attributes != null)
                    {
                        foreach (var attr in document.attributes)
                        {
                            switch (attr)
                            {
                                case DocumentAttributeVideo video:
                                    mediaInfo.Duration = (int?)video.duration;
                                    mediaInfo.Width = video.w;
                                    mediaInfo.Height = video.h;
                                    break;
                                case DocumentAttributeAudio audio:
                                    mediaInfo.Duration = (int?)audio.duration;
                                    break;
                            }
                        }
                    }
                }
                break;

            case MessageMediaGeo _:
                messageData.MessageType = MessageType.Location;
                break;

            case MessageMediaContact _:
                messageData.MessageType = MessageType.Contact;
                break;

            case MessageMediaPoll _:
                messageData.MessageType = MessageType.Poll;
                break;

            default:
                messageData.MessageType = MessageType.Unknown;
                break;
        }

        if (mediaInfo.FileName != null || mediaInfo.FileSize.HasValue || mediaInfo.MimeType != null)
        {
            messageData.Media = mediaInfo;
        }
    }

    /// <summary>
    /// Gets the document filename from various attributes
    /// </summary>
    /// <param name="document">Document object</param>
    /// <returns>Filename or null if not found</returns>
    private string? GetDocumentFileName(Document document)
    {
        if (document.attributes == null) return null;

        foreach (var attr in document.attributes)
        {
            if (attr is DocumentAttributeFilename filename)
            {
                return filename.file_name;
            }
        }

        return null;
    }

    /// <summary>
    /// Determines message type from MIME type
    /// </summary>
    /// <param name="mimeType">MIME type string</param>
    /// <returns>MessageType enum value</returns>
    private MessageType GetMessageTypeFromMimeType(string? mimeType)
    {
        if (string.IsNullOrWhiteSpace(mimeType)) return MessageType.Document;

        return mimeType.ToLowerInvariant() switch
        {
            var mime when mime.StartsWith("image/") => MessageType.Photo,
            var mime when mime.StartsWith("video/") => MessageType.Video,
            var mime when mime.StartsWith("audio/") => MessageType.Audio,
            "image/gif" => MessageType.Animation,
            "video/mp4" when mimeType.Contains("gif") => MessageType.Animation,
            _ => MessageType.Document
        };
    }

    /// <summary>
    /// Processes message entities to extract structured data
    /// </summary>
    /// <param name="entities">Message entities from Telegram</param>
    /// <param name="messageData">MessageData to populate</param>
    private void ProcessMessageEntities(MessageEntity[] entities, MessageData messageData)
    {
        var content = messageData.Content;
        if (string.IsNullOrWhiteSpace(content)) return;

        foreach (var entity in entities)
        {
            try
            {
                var entityText = content.Substring(entity.offset, Math.Min(entity.length, content.Length - entity.offset));

                switch (entity)
                {
                    case MessageEntityUrl _:
                        if (!messageData.Links.Contains(entityText))
                        {
                            messageData.Links.Add(entityText);
                        }
                        break;

                    case MessageEntityTextUrl textUrl:
                        if (!string.IsNullOrWhiteSpace(textUrl.url) && !messageData.Links.Contains(textUrl.url))
                        {
                            messageData.Links.Add(textUrl.url);
                        }
                        break;

                    case MessageEntityMention _:
                        var mention = entityText.TrimStart('@');
                        if (!messageData.Mentions.Contains(mention))
                        {
                            messageData.Mentions.Add(mention);
                        }
                        break;

                    case MessageEntityHashtag _:
                        var hashtag = entityText.TrimStart('#');
                        if (!messageData.Hashtags.Contains(hashtag))
                        {
                            messageData.Hashtags.Add(hashtag);
                        }
                        break;
                }
            }
            catch
            {
                // Skip malformed entities
                continue;
            }
        }
    }

    /// <summary>
    /// Extracts wait time from FLOOD_WAIT error message
    /// </summary>
    /// <param name="errorMessage">Error message from Telegram</param>
    /// <returns>Wait time in seconds</returns>
    private int ExtractFloodWaitTime(string errorMessage)
    {
        try
        {
            var match = System.Text.RegularExpressions.Regex.Match(errorMessage, @"FLOOD_WAIT_(\d+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out var waitTime))
            {
                return waitTime;
            }
        }
        catch
        {
            // Fallback to default wait time
        }

        return 60; // Default 60 seconds
    }

    /// <summary>
    /// Escapes markdown characters in text to prevent formatting issues
    /// </summary>
    /// <param name="text">Text to escape</param>
    /// <returns>Escaped text</returns>
    private string EscapeMarkdown(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;

        return text
            .Replace("\\", "\\\\")
            .Replace("*", "\\*")
            .Replace("_", "\\_")
            .Replace("`", "\\`")
            .Replace("[", "\\[")
            .Replace("]", "\\]")
            .Replace("(", "\\(")
            .Replace(")", "\\)")
            .Replace("#", "\\#")
            .Replace("+", "\\+")
            .Replace("-", "\\-")
            .Replace(".", "\\.")
            .Replace("!", "\\!");
    }

    /// <summary>
    /// Gets description for media type
    /// </summary>
    /// <param name="messageType">Message type</param>
    /// <returns>Human-readable description</returns>
    private string GetMediaTypeDescription(MessageType messageType)
    {
        return messageType switch
        {
            MessageType.Photo => "Photo",
            MessageType.Video => "Video",
            MessageType.Audio => "Audio",
            MessageType.Document => "Document",
            MessageType.Animation => "Animation/GIF",
            MessageType.Voice => "Voice Message",
            MessageType.VideoNote => "Video Note",
            MessageType.Sticker => "Sticker",
            MessageType.Location => "Location",
            MessageType.Contact => "Contact",
            MessageType.Poll => "Poll",
            _ => "Unknown Media"
        };
    }

    /// <summary>
    /// Formats file size in human-readable format
    /// </summary>
    /// <param name="fileSize">File size in bytes</param>
    /// <returns>Formatted file size string</returns>
    private string FormatFileSize(long fileSize)
    {
        const long KB = 1024;
        const long MB = KB * 1024;
        const long GB = MB * 1024;

        return fileSize switch
        {
            >= GB => $"{fileSize / (double)GB:F1} GB",
            >= MB => $"{fileSize / (double)MB:F1} MB",
            >= KB => $"{fileSize / (double)KB:F1} KB",
            _ => $"{fileSize} bytes"
        };
    }

    /// <summary>
    /// Disposes the service and cleans up resources
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _client?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
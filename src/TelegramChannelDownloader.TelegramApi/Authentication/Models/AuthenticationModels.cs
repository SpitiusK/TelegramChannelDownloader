using TelegramChannelDownloader.TelegramApi.Configuration;

namespace TelegramChannelDownloader.TelegramApi.Authentication.Models;

/// <summary>
/// Represents the current authentication state of the Telegram client
/// </summary>
public enum AuthenticationState
{
    /// <summary>
    /// Client is not initialized or disconnected
    /// </summary>
    Disconnected,

    /// <summary>
    /// Client is initializing connection
    /// </summary>
    Connecting,

    /// <summary>
    /// Waiting for phone number input
    /// </summary>
    WaitingForPhoneNumber,

    /// <summary>
    /// Waiting for verification code from SMS/app
    /// </summary>
    WaitingForVerificationCode,

    /// <summary>
    /// Waiting for two-factor authentication password
    /// </summary>
    WaitingForTwoFactorAuth,

    /// <summary>
    /// Successfully authenticated and connected
    /// </summary>
    Authenticated,

    /// <summary>
    /// Authentication failed due to invalid credentials
    /// </summary>
    AuthenticationFailed,

    /// <summary>
    /// Connection error occurred
    /// </summary>
    ConnectionError
}

/// <summary>
/// Result of an authentication operation
/// </summary>
public class AuthResult
{
    /// <summary>
    /// Current authentication state
    /// </summary>
    public AuthenticationState State { get; set; } = AuthenticationState.Disconnected;

    /// <summary>
    /// Indicates if the operation was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Status message for display purposes
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Authenticated user information (null if not authenticated)
    /// </summary>
    public TelegramUserInfo? User { get; set; }

    /// <summary>
    /// Error message if authentication failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Exception details if available
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Indicates if the client is currently connected
    /// </summary>
    public bool IsConnected => State == AuthenticationState.Authenticated;

    /// <summary>
    /// Indicates if authentication is in progress (actively connecting, not waiting for user input)
    /// </summary>
    public bool IsAuthenticating => State == AuthenticationState.Connecting;

    /// <summary>
    /// Indicates if user input is required to continue
    /// </summary>
    public bool RequiresUserInput => State is AuthenticationState.WaitingForPhoneNumber 
                                            or AuthenticationState.WaitingForVerificationCode 
                                            or AuthenticationState.WaitingForTwoFactorAuth;

    /// <summary>
    /// Creates a successful authentication result
    /// </summary>
    public static AuthResult Success(AuthenticationState state, string message, TelegramUserInfo? user = null)
    {
        return new AuthResult
        {
            IsSuccess = true,
            State = state,
            Message = message,
            User = user
        };
    }

    /// <summary>
    /// Creates a failed authentication result
    /// </summary>
    public static AuthResult Failure(AuthenticationState state, string message, string? errorMessage = null, Exception? exception = null)
    {
        return new AuthResult
        {
            IsSuccess = false,
            State = state,
            Message = message,
            ErrorMessage = errorMessage,
            Exception = exception
        };
    }
}

/// <summary>
/// Event arguments for authentication status changes
/// </summary>
public class AuthStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// Previous authentication result
    /// </summary>
    public AuthResult? PreviousStatus { get; set; }

    /// <summary>
    /// Current authentication result
    /// </summary>
    public AuthResult CurrentStatus { get; set; }

    public AuthStatusChangedEventArgs(AuthResult currentStatus, AuthResult? previousStatus = null)
    {
        CurrentStatus = currentStatus;
        PreviousStatus = previousStatus;
    }
}

/// <summary>
/// Represents basic user information from Telegram
/// </summary>
public class TelegramUserInfo
{
    /// <summary>
    /// User ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Username (without @ symbol)
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// First name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Last name (optional)
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Phone number
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Indicates if this is a bot account
    /// </summary>
    public bool IsBot { get; set; }

    /// <summary>
    /// Indicates if the user is verified by Telegram
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// Full display name
    /// </summary>
    public string DisplayName
    {
        get
        {
            var name = FirstName;
            if (!string.IsNullOrWhiteSpace(LastName))
            {
                name += $" {LastName}";
            }
            if (!string.IsNullOrWhiteSpace(Username))
            {
                name += $" (@{Username})";
            }
            return name;
        }
    }
}
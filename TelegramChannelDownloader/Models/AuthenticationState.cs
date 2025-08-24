namespace TelegramChannelDownloader.Models;

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
/// Contains authentication status information
/// </summary>
public class AuthenticationStatus
{
    /// <summary>
    /// Current authentication state
    /// </summary>
    public AuthenticationState State { get; set; } = AuthenticationState.Disconnected;

    /// <summary>
    /// Status message for display purposes
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Authenticated user information (null if not authenticated)
    /// </summary>
    public TelegramUser? User { get; set; }

    /// <summary>
    /// Error message if authentication failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Indicates if the client is currently connected
    /// </summary>
    public bool IsConnected => State == AuthenticationState.Authenticated;

    /// <summary>
    /// Indicates if authentication is in progress (actively connecting, not waiting for user input)
    /// </summary>
    public bool IsAuthenticating => State == AuthenticationState.Connecting;
}

/// <summary>
/// Represents basic user information from Telegram
/// </summary>
public class TelegramUser
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
using TelegramChannelDownloader.TelegramApi.Authentication.Models;
using TelegramChannelDownloader.TelegramApi.Configuration;

namespace TelegramChannelDownloader.TelegramApi.Authentication;

/// <summary>
/// Handles Telegram authentication operations
/// </summary>
public interface IAuthenticationHandler
{
    /// <summary>
    /// Event triggered when authentication status changes
    /// </summary>
    event EventHandler<AuthStatusChangedEventArgs>? StatusChanged;

    /// <summary>
    /// Current authentication status
    /// </summary>
    AuthResult CurrentStatus { get; }

    /// <summary>
    /// Initializes the authentication handler with configuration
    /// </summary>
    /// <param name="config">Telegram API configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication result</returns>
    Task<AuthResult> InitializeAsync(TelegramApiConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts the authentication process with phone number
    /// </summary>
    /// <param name="phoneNumber">Phone number including country code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication result</returns>
    Task<AuthResult> AuthenticatePhoneAsync(string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes authentication with verification code
    /// </summary>
    /// <param name="verificationCode">Verification code from SMS/app</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication result</returns>
    Task<AuthResult> VerifyCodeAsync(string verificationCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes authentication with two-factor password
    /// </summary>
    /// <param name="password">Two-factor authentication password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication result</returns>
    Task<AuthResult> VerifyTwoFactorAsync(string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects and clears authentication state
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
    /// <returns>Authentication result</returns>
    Task<AuthResult> RestoreSessionAsync(string sessionData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the connection to Telegram servers
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connection is successful</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}
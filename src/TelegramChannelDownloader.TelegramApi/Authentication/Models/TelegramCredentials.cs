namespace TelegramChannelDownloader.TelegramApi.Authentication.Models;

/// <summary>
/// Represents Telegram API credentials and authentication data
/// </summary>
public class TelegramCredentials
{
    /// <summary>
    /// Telegram API ID obtained from my.telegram.org
    /// </summary>
    public int ApiId { get; set; }

    /// <summary>
    /// Telegram API Hash obtained from my.telegram.org
    /// </summary>
    public string ApiHash { get; set; } = string.Empty;

    /// <summary>
    /// Phone number for authentication (including country code)
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Two-factor authentication password (if enabled)
    /// </summary>
    public string? TwoFactorPassword { get; set; }

    /// <summary>
    /// Verification code received via SMS/app
    /// </summary>
    public string? VerificationCode { get; set; }

    /// <summary>
    /// Session data for maintaining authentication
    /// </summary>
    public string? SessionData { get; set; }

    /// <summary>
    /// Indicates if the credentials are valid for authentication
    /// </summary>
    public bool IsValid => ApiId > 0 && !string.IsNullOrWhiteSpace(ApiHash);

    /// <summary>
    /// Indicates if phone number is provided
    /// </summary>
    public bool HasPhoneNumber => !string.IsNullOrWhiteSpace(PhoneNumber);

    /// <summary>
    /// Indicates if verification code is provided
    /// </summary>
    public bool HasVerificationCode => !string.IsNullOrWhiteSpace(VerificationCode);

    /// <summary>
    /// Indicates if two-factor password is provided
    /// </summary>
    public bool HasTwoFactorPassword => !string.IsNullOrWhiteSpace(TwoFactorPassword);

    /// <summary>
    /// Indicates if session data is available
    /// </summary>
    public bool HasSessionData => !string.IsNullOrWhiteSpace(SessionData);
}
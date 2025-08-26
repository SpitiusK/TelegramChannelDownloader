namespace TelegramChannelDownloader.Core.Models;

/// <summary>
/// API credentials for Telegram authentication
/// </summary>
public class ApiCredentials
{
    /// <summary>
    /// Telegram API ID
    /// </summary>
    public int ApiId { get; set; }

    /// <summary>
    /// Telegram API Hash
    /// </summary>
    public string ApiHash { get; set; } = string.Empty;

    /// <summary>
    /// Phone number for authentication
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Verification code (when required)
    /// </summary>
    public string? VerificationCode { get; set; }

    /// <summary>
    /// Two-factor authentication password (when required)
    /// </summary>
    public string? TwoFactorPassword { get; set; }

    /// <summary>
    /// Session data for persistence
    /// </summary>
    public string? SessionData { get; set; }

    /// <summary>
    /// Session file path
    /// </summary>
    public string? SessionFilePath { get; set; }

    /// <summary>
    /// Validates the basic credentials (API ID and Hash)
    /// </summary>
    /// <returns>True if basic credentials are valid</returns>
    public bool IsValid()
    {
        return ApiId > 0 && !string.IsNullOrWhiteSpace(ApiHash) && ApiHash.Length == 32;
    }

    /// <summary>
    /// Validates if phone number is provided
    /// </summary>
    /// <returns>True if phone number is provided</returns>
    public bool HasPhoneNumber()
    {
        return !string.IsNullOrWhiteSpace(PhoneNumber);
    }

    /// <summary>
    /// Validates if verification code is provided
    /// </summary>
    /// <returns>True if verification code is provided</returns>
    public bool HasVerificationCode()
    {
        return !string.IsNullOrWhiteSpace(VerificationCode);
    }

    /// <summary>
    /// Validates if two-factor password is provided
    /// </summary>
    /// <returns>True if two-factor password is provided</returns>
    public bool HasTwoFactorPassword()
    {
        return !string.IsNullOrWhiteSpace(TwoFactorPassword);
    }

    /// <summary>
    /// Validates if session data is available
    /// </summary>
    /// <returns>True if session data is available</returns>
    public bool HasSessionData()
    {
        return !string.IsNullOrWhiteSpace(SessionData);
    }
}
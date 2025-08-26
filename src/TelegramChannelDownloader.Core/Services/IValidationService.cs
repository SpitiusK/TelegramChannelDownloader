using TelegramChannelDownloader.Core.Models;
using TelegramChannelDownloader.TelegramApi.Channels.Models;

namespace TelegramChannelDownloader.Core.Services;

/// <summary>
/// Service for handling all validation operations
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// Validates API ID
    /// </summary>
    /// <param name="apiId">API ID to validate</param>
    /// <returns>Validation result</returns>
    ValidationResult ValidateApiId(string apiId);

    /// <summary>
    /// Validates API Hash
    /// </summary>
    /// <param name="apiHash">API Hash to validate</param>
    /// <returns>Validation result</returns>
    ValidationResult ValidateApiHash(string apiHash);

    /// <summary>
    /// Validates output directory path
    /// </summary>
    /// <param name="directoryPath">Directory path to validate</param>
    /// <returns>Validation result</returns>
    ValidationResult ValidateOutputDirectory(string directoryPath);

    /// <summary>
    /// Validates API credentials
    /// </summary>
    /// <param name="credentials">API credentials to validate</param>
    /// <returns>Validation result</returns>
    ValidationResult ValidateApiCredentials(ApiCredentials credentials);

    /// <summary>
    /// Validates a phone number format
    /// </summary>
    /// <param name="phoneNumber">Phone number to validate</param>
    /// <returns>Validation result</returns>
    ValidationResult ValidatePhoneNumber(string phoneNumber);

    /// <summary>
    /// Validates a verification code format
    /// </summary>
    /// <param name="code">Verification code to validate</param>
    /// <returns>Validation result</returns>
    ValidationResult ValidateVerificationCode(string code);

    /// <summary>
    /// Validates a two-factor authentication code format
    /// </summary>
    /// <param name="code">2FA code to validate</param>
    /// <returns>Validation result</returns>
    ValidationResult ValidateTwoFactorCode(string code);

    /// <summary>
    /// Validates a channel URL format
    /// </summary>
    /// <param name="channelUrl">Channel URL to validate</param>
    /// <returns>Validation result</returns>
    ValidationResult ValidateChannelUrl(string channelUrl);

    /// <summary>
    /// Validates a channel URL by connecting to Telegram API
    /// </summary>
    /// <param name="channelUrl">Channel URL to validate</param>
    /// <returns>Validation result with channel information</returns>
    Task<ValidationResult<ChannelInfo>> ValidateChannelAsync(string channelUrl);

    /// <summary>
    /// Validates a directory path
    /// </summary>
    /// <param name="directoryPath">Directory path to validate</param>
    /// <param name="checkWriteAccess">Whether to check write access</param>
    /// <param name="createIfNotExists">Whether to create directory if it doesn't exist</param>
    /// <returns>Validation result</returns>
    ValidationResult ValidateDirectoryPath(string directoryPath, bool checkWriteAccess = true, bool createIfNotExists = false);

    /// <summary>
    /// Validates directory path with disk space requirements
    /// </summary>
    /// <param name="directoryPath">Directory path to validate</param>
    /// <param name="requiredSpaceBytes">Required space in bytes</param>
    /// <returns>Validation result</returns>
    ValidationResult ValidateDirectorySpace(string directoryPath, long requiredSpaceBytes);

    /// <summary>
    /// Validates a complete download request
    /// </summary>
    /// <param name="request">Download request to validate</param>
    /// <returns>Validation result</returns>
    Task<ValidationResult> ValidateDownloadRequestAsync(DownloadRequest request);

    /// <summary>
    /// Validates a complete export request
    /// </summary>
    /// <param name="request">Export request to validate</param>
    /// <returns>Validation result</returns>
    ValidationResult ValidateExportRequest(ExportRequest request);

    /// <summary>
    /// Gets a user-friendly error message for validation failures
    /// </summary>
    /// <param name="fieldName">Name of the field that failed validation</param>
    /// <param name="value">Value that failed validation</param>
    /// <param name="additionalContext">Additional context for the error</param>
    /// <returns>User-friendly error message</returns>
    string GetValidationErrorMessage(string fieldName, string value, string? additionalContext = null);

    /// <summary>
    /// Parses a channel URL and extracts username and format information
    /// </summary>
    /// <param name="channelUrl">Channel URL to parse</param>
    /// <returns>Parsed channel URL information</returns>
    ChannelUrlParseResult ParseChannelUrl(string channelUrl);
}

/// <summary>
/// Enhanced validation result that can contain typed data
/// </summary>
/// <typeparam name="T">Type of data contained in the result</typeparam>
public class ValidationResult<T> : ValidationResult
{
    /// <summary>
    /// Data associated with the validation result
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Creates a successful validation result with data
    /// </summary>
    /// <param name="data">Data to include</param>
    /// <returns>Successful validation result</returns>
    public static ValidationResult<T> Success(T data)
    {
        return new ValidationResult<T>
        {
            IsValid = true,
            Data = data
        };
    }

    /// <summary>
    /// Creates a failed validation result with error message
    /// </summary>
    /// <param name="errorMessage">Error message</param>
    /// <param name="errorCode">Optional error code</param>
    /// <returns>Failed validation result</returns>
    public static new ValidationResult<T> Failure(string errorMessage, string? errorCode = null)
    {
        return new ValidationResult<T>
        {
            IsValid = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }
}

/// <summary>
/// Result of channel URL parsing and validation
/// </summary>
public class ChannelUrlParseResult
{
    /// <summary>
    /// Indicates if the URL is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Clean username extracted from the URL
    /// </summary>
    public string? CleanUsername { get; set; }

    /// <summary>
    /// Canonical URL for the channel
    /// </summary>
    public string? CanonicalUrl { get; set; }

    /// <summary>
    /// Format of the input URL
    /// </summary>
    public ChannelUrlFormat InputFormat { get; set; }

    /// <summary>
    /// Error message if parsing failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Represents the format of the input channel URL
/// </summary>
public enum ChannelUrlFormat
{
    /// <summary>
    /// Username with @ prefix (@username)
    /// </summary>
    Username,

    /// <summary>
    /// Full HTTPS URL (https://t.me/username)
    /// </summary>
    HttpsUrl,

    /// <summary>
    /// HTTP URL (http://t.me/username)
    /// </summary>
    HttpUrl,

    /// <summary>
    /// Short URL (t.me/username)
    /// </summary>
    ShortUrl,

    /// <summary>
    /// Plain username without any prefix
    /// </summary>
    PlainUsername
}
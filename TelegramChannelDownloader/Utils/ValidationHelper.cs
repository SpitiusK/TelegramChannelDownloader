using System.Text.RegularExpressions;
using System.IO;

namespace TelegramChannelDownloader.Utils;

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
/// Helper class for input validation
/// </summary>
public static class ValidationHelper
{
    /// <summary>
    /// Validates if a string represents a valid API ID (numeric)
    /// </summary>
    /// <param name="apiId">The API ID to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValidApiId(string apiId)
    {
        if (string.IsNullOrWhiteSpace(apiId))
            return false;
            
        return int.TryParse(apiId, out var id) && id > 0;
    }
    
    /// <summary>
    /// Validates if a string represents a valid API Hash
    /// </summary>
    /// <param name="apiHash">The API Hash to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValidApiHash(string apiHash)
    {
        if (string.IsNullOrWhiteSpace(apiHash))
            return false;
            
        // API Hash should be a 32-character hexadecimal string
        return apiHash.Length == 32 && Regex.IsMatch(apiHash, @"^[a-fA-F0-9]{32}$");
    }
    
    /// <summary>
    /// Validates if a string represents a valid phone number
    /// </summary>
    /// <param name="phoneNumber">The phone number to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValidPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;
            
        // Remove common formatting characters
        var cleaned = phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "");
        
        // Should be between 10-15 digits
        return cleaned.Length >= 10 && cleaned.Length <= 15 && Regex.IsMatch(cleaned, @"^\d+$");
    }
    
    /// <summary>
    /// Validates if a string represents a valid verification code
    /// </summary>
    /// <param name="code">The verification code to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValidVerificationCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;
            
        // Telegram verification codes are typically 5 digits
        return code.Length == 5 && Regex.IsMatch(code, @"^\d{5}$");
    }
    
    /// <summary>
    /// Validates if a string represents a valid two-factor authentication code
    /// </summary>
    /// <param name="code">The 2FA code to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValidTwoFactorCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;
            
        // 2FA codes can vary in length but are typically 6-8 characters
        return code.Length >= 6 && code.Length <= 8;
    }
    
    /// <summary>
    /// Validates if a string represents a valid Telegram channel URL
    /// </summary>
    /// <param name="channelUrl">The channel URL to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValidChannelUrl(string channelUrl)
    {
        var parsed = ParseChannelUrl(channelUrl);
        return parsed.IsValid;
    }

    /// <summary>
    /// Parses and validates a Telegram channel URL, extracting the clean username
    /// </summary>
    /// <param name="channelUrl">The channel URL to parse</param>
    /// <returns>Parsed channel information with validation result</returns>
    public static ChannelUrlParseResult ParseChannelUrl(string channelUrl)
    {
        var result = new ChannelUrlParseResult();

        if (string.IsNullOrWhiteSpace(channelUrl))
        {
            result.ErrorMessage = "Channel URL cannot be empty";
            return result;
        }

        var input = channelUrl.Trim();
        string username;

        try
        {
            // Handle different URL formats
            if (input.StartsWith("@"))
            {
                // Direct username format: @username
                username = input.Substring(1);
                result.InputFormat = ChannelUrlFormat.Username;
            }
            else if (input.StartsWith("https://t.me/", StringComparison.OrdinalIgnoreCase))
            {
                // Full HTTPS URL: https://t.me/username
                username = input.Substring(13);
                result.InputFormat = ChannelUrlFormat.HttpsUrl;
            }
            else if (input.StartsWith("http://t.me/", StringComparison.OrdinalIgnoreCase))
            {
                // HTTP URL: http://t.me/username
                username = input.Substring(12);
                result.InputFormat = ChannelUrlFormat.HttpUrl;
            }
            else if (input.StartsWith("t.me/", StringComparison.OrdinalIgnoreCase))
            {
                // Short URL: t.me/username
                username = input.Substring(5);
                result.InputFormat = ChannelUrlFormat.ShortUrl;
            }
            else
            {
                // Assume it's a direct username without @ prefix
                username = input;
                result.InputFormat = ChannelUrlFormat.PlainUsername;
            }

            // Remove any additional path parameters (e.g., t.me/username/123)
            var slashIndex = username.IndexOf('/');
            if (slashIndex > 0)
            {
                username = username.Substring(0, slashIndex);
            }

            // Remove query parameters and fragments
            var queryIndex = username.IndexOf('?');
            if (queryIndex > 0)
            {
                username = username.Substring(0, queryIndex);
            }

            var hashIndex = username.IndexOf('#');
            if (hashIndex > 0)
            {
                username = username.Substring(0, hashIndex);
            }

            username = username.Trim();

            // Validate username format
            if (string.IsNullOrEmpty(username))
            {
                result.ErrorMessage = "Username cannot be empty after parsing URL";
                return result;
            }

            if (username.Length < 5)
            {
                result.ErrorMessage = "Username must be at least 5 characters long";
                return result;
            }

            if (username.Length > 32)
            {
                result.ErrorMessage = "Username cannot be longer than 32 characters";
                return result;
            }

            // Check username character rules:
            // - Can contain letters, digits, and underscores
            // - Cannot start or end with underscore
            // - Cannot have consecutive underscores
            if (!Regex.IsMatch(username, @"^[a-zA-Z0-9][a-zA-Z0-9_]*[a-zA-Z0-9]$") && 
                !Regex.IsMatch(username, @"^[a-zA-Z0-9]{5,32}$"))
            {
                result.ErrorMessage = "Username can only contain letters, digits, and underscores. Cannot start or end with underscore.";
                return result;
            }

            if (username.Contains("__"))
            {
                result.ErrorMessage = "Username cannot contain consecutive underscores";
                return result;
            }

            // Check for reserved usernames or invalid patterns
            if (IsReservedUsername(username))
            {
                result.ErrorMessage = "This username is reserved and cannot be used";
                return result;
            }

            result.IsValid = true;
            result.CleanUsername = username;
            result.CanonicalUrl = $"https://t.me/{username}";
            result.ErrorMessage = null;

            return result;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Error parsing channel URL: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// Checks if a username is reserved and cannot be used for channels
    /// </summary>
    /// <param name="username">Username to check</param>
    /// <returns>True if username is reserved</returns>
    private static bool IsReservedUsername(string username)
    {
        var reserved = new[]
        {
            "telegram", "admin", "api", "support", "help", "news", "settings", 
            "privacy", "contact", "about", "username", "channel", "group",
            "bot", "bots", "proxy", "socks", "http", "https", "ftp", "www",
            "joinchat", "addstickers", "login", "auth", "oauth", "iv"
        };

        return reserved.Contains(username.ToLowerInvariant());
    }
    
    /// <summary>
    /// Validates if a string represents a valid directory path
    /// </summary>
    /// <param name="directoryPath">The directory path to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValidDirectoryPath(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            return false;
            
        try
        {
            // Check if the path is valid and not just whitespace
            var fullPath = Path.GetFullPath(directoryPath);
            return !string.IsNullOrWhiteSpace(fullPath);
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Gets a user-friendly error message for validation failures
    /// </summary>
    /// <param name="fieldName">The name of the field being validated</param>
    /// <param name="value">The value that failed validation</param>
    /// <returns>A user-friendly error message</returns>
    public static string GetValidationErrorMessage(string fieldName, string value)
    {
        return fieldName.ToLower() switch
        {
            "apiid" => "API ID must be a positive number",
            "apihash" => "API Hash must be a 32-character hexadecimal string",
            "phonenumber" => "Phone number must be 10-15 digits",
            "verificationcode" => "Verification code must be 5 digits",
            "twofactorcode" => "Two-factor code must be 6-8 characters",
            "channelurl" => "Channel URL must be a valid Telegram username (5-32 characters)",
            "outputdirectory" => "Output directory path is invalid",
            _ => $"{fieldName} is invalid"
        };
    }
}
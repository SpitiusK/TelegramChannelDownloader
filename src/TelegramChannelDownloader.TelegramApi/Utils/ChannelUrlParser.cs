using System.Text.RegularExpressions;

namespace TelegramChannelDownloader.TelegramApi.Utils;

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
/// Utility class for parsing and validating Telegram channel URLs
/// </summary>
public static class ChannelUrlParser
{
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
}
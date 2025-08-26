namespace TelegramChannelDownloader.TelegramApi.Configuration;

/// <summary>
/// Configuration settings for the Telegram API client
/// </summary>
public class TelegramApiConfig
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
    /// Path where session data will be stored
    /// </summary>
    public string SessionPath { get; set; } = "session.dat";

    /// <summary>
    /// Request timeout in milliseconds
    /// </summary>
    public int RequestTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Maximum number of retry attempts for failed operations
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts in milliseconds
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Maximum number of messages to download in a single batch
    /// </summary>
    public int MaxBatchSize { get; set; } = 100;

    /// <summary>
    /// Directory where downloaded files will be stored
    /// </summary>
    public string? DownloadDirectory { get; set; }

    /// <summary>
    /// Indicates if media files should be downloaded along with messages
    /// </summary>
    public bool DownloadMedia { get; set; } = false;

    /// <summary>
    /// Maximum file size to download (in bytes). 0 means no limit.
    /// </summary>
    public long MaxFileSize { get; set; } = 0;

    /// <summary>
    /// Indicates if the configuration is valid for API operations
    /// </summary>
    public bool IsValid => ApiId > 0 && !string.IsNullOrWhiteSpace(ApiHash);

    /// <summary>
    /// Validates the configuration and returns any validation errors
    /// </summary>
    /// <returns>List of validation error messages</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (ApiId <= 0)
            errors.Add("API ID must be a positive integer");

        if (string.IsNullOrWhiteSpace(ApiHash))
            errors.Add("API Hash is required");
        else if (ApiHash.Length != 32)
            errors.Add("API Hash must be exactly 32 characters long");

        if (RequestTimeoutMs <= 0)
            errors.Add("Request timeout must be greater than 0");

        if (MaxRetryAttempts < 0)
            errors.Add("Max retry attempts cannot be negative");

        if (RetryDelayMs < 0)
            errors.Add("Retry delay cannot be negative");

        if (MaxBatchSize <= 0)
            errors.Add("Max batch size must be greater than 0");

        return errors;
    }
}
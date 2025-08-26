namespace TelegramChannelDownloader.Core.Models;

/// <summary>
/// Request model for download operations
/// </summary>
public class DownloadRequest
{
    /// <summary>
    /// Channel URL or username to download from
    /// </summary>
    public string ChannelUrl { get; set; } = string.Empty;

    /// <summary>
    /// Output directory where files will be saved
    /// </summary>
    public string OutputDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Export format for the downloaded messages
    /// </summary>
    public ExportFormat ExportFormat { get; set; } = ExportFormat.Markdown;

    /// <summary>
    /// Additional download options
    /// </summary>
    public DownloadOptions Options { get; set; } = new();

    /// <summary>
    /// Unique identifier for this download request
    /// </summary>
    public string DownloadId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// API credentials for authentication
    /// </summary>
    public ApiCredentials? Credentials { get; set; }
}

/// <summary>
/// Additional options for download operations
/// </summary>
public class DownloadOptions
{
    /// <summary>
    /// Maximum number of messages to download (0 = all)
    /// </summary>
    public int MaxMessages { get; set; } = 0;

    /// <summary>
    /// Start date for message filtering (null = from beginning)
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date for message filtering (null = until now)
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Whether to include media files in download
    /// </summary>
    public bool IncludeMedia { get; set; } = false;

    /// <summary>
    /// Batch size for processing messages
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Whether to overwrite existing files
    /// </summary>
    public bool OverwriteExisting { get; set; } = false;

    /// <summary>
    /// Custom filename format
    /// </summary>
    public string? CustomFilename { get; set; }
}
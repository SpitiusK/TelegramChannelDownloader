namespace TelegramChannelDownloader.Core.Models;

/// <summary>
/// Application configuration settings
/// </summary>
public class AppConfig
{
    /// <summary>
    /// Default output directory for downloads
    /// </summary>
    public string DefaultOutputDirectory { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
        "TelegramDownloads");

    /// <summary>
    /// Default export format for downloads
    /// </summary>
    public ExportFormat DefaultExportFormat { get; set; } = ExportFormat.Markdown;

    /// <summary>
    /// Whether to automatically open files after download
    /// </summary>
    public bool AutoOpenAfterDownload { get; set; } = false;

    /// <summary>
    /// Whether to automatically open the output directory after download
    /// </summary>
    public bool AutoOpenOutputDirectory { get; set; } = false;

    /// <summary>
    /// Whether to automatically open the exported file after download
    /// </summary>
    public bool AutoOpenExportedFile { get; set; } = false;

    /// <summary>
    /// Maximum number of concurrent downloads
    /// </summary>
    public int MaxConcurrentDownloads { get; set; } = 1;

    /// <summary>
    /// Default batch size for message processing
    /// </summary>
    public int DefaultBatchSize { get; set; } = 100;

    /// <summary>
    /// Session file path for authentication persistence
    /// </summary>
    public string SessionFilePath { get; set; } = "session.dat";

    /// <summary>
    /// Maximum number of log entries to keep in memory
    /// </summary>
    public int MaxLogEntries { get; set; } = 100;

    /// <summary>
    /// Log level for application logging
    /// </summary>
    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Enable detailed performance metrics
    /// </summary>
    public bool EnablePerformanceMetrics { get; set; } = false;

    /// <summary>
    /// Timeout for network operations in seconds
    /// </summary>
    public int NetworkTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum retry attempts for failed operations
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts in milliseconds
    /// </summary>
    public int RetryDelayMilliseconds { get; set; } = 1000;

    /// <summary>
    /// Maximum file size for exports in MB (0 = no limit)
    /// </summary>
    public int MaxExportFileSizeMB { get; set; } = 0;

    /// <summary>
    /// Enable automatic backup of export files
    /// </summary>
    public bool EnableAutoBackup { get; set; } = false;

    /// <summary>
    /// Number of backup files to keep
    /// </summary>
    public int BackupFileCount { get; set; } = 5;

    /// <summary>
    /// UI theme preference
    /// </summary>
    public string Theme { get; set; } = "Auto";

    /// <summary>
    /// UI language/culture
    /// </summary>
    public string Culture { get; set; } = "en-US";

    /// <summary>
    /// Custom format strings for different export types
    /// </summary>
    public ExportFormatSettings ExportFormats { get; set; } = new();

    /// <summary>
    /// Validates the configuration settings
    /// </summary>
    /// <returns>Validation result</returns>
    public ValidationResult Validate()
    {
        var result = new ValidationResult { IsValid = true };

        // Validate output directory
        if (string.IsNullOrWhiteSpace(DefaultOutputDirectory))
        {
            result.AddError("Default output directory cannot be empty");
        }
        else
        {
            try
            {
                var fullPath = Path.GetFullPath(DefaultOutputDirectory);
                // Try to create directory if it doesn't exist
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }
            }
            catch (Exception ex)
            {
                result.AddError($"Invalid default output directory: {ex.Message}");
            }
        }

        // Validate numeric settings
        if (MaxConcurrentDownloads < 1 || MaxConcurrentDownloads > 10)
        {
            result.AddError("Max concurrent downloads must be between 1 and 10");
        }

        if (DefaultBatchSize < 1 || DefaultBatchSize > 1000)
        {
            result.AddError("Default batch size must be between 1 and 1000");
        }

        if (NetworkTimeoutSeconds < 5 || NetworkTimeoutSeconds > 300)
        {
            result.AddError("Network timeout must be between 5 and 300 seconds");
        }

        if (MaxRetryAttempts < 0 || MaxRetryAttempts > 10)
        {
            result.AddError("Max retry attempts must be between 0 and 10");
        }

        if (MaxLogEntries < 10 || MaxLogEntries > 10000)
        {
            result.AddError("Max log entries must be between 10 and 10000");
        }

        return result;
    }
}

/// <summary>
/// Settings for different export formats
/// </summary>
public class ExportFormatSettings
{
    /// <summary>
    /// Settings for Markdown export
    /// </summary>
    public MarkdownExportSettings Markdown { get; set; } = new();

    /// <summary>
    /// Settings for JSON export
    /// </summary>
    public JsonExportSettings Json { get; set; } = new();

    /// <summary>
    /// Settings for CSV export
    /// </summary>
    public CsvExportSettings Csv { get; set; } = new();

    /// <summary>
    /// Settings for HTML export
    /// </summary>
    public HtmlExportSettings Html { get; set; } = new();
}

/// <summary>
/// Settings for Markdown export
/// </summary>
public class MarkdownExportSettings
{
    /// <summary>
    /// Whether to include table of contents
    /// </summary>
    public bool IncludeTableOfContents { get; set; } = true;

    /// <summary>
    /// Heading level for channel name
    /// </summary>
    public int ChannelHeadingLevel { get; set; } = 1;

    /// <summary>
    /// Heading level for date sections
    /// </summary>
    public int DateHeadingLevel { get; set; } = 2;
}

/// <summary>
/// Settings for JSON export
/// </summary>
public class JsonExportSettings
{
    /// <summary>
    /// Whether to format JSON with indentation
    /// </summary>
    public bool PrettyFormat { get; set; } = true;

    /// <summary>
    /// Whether to include null values
    /// </summary>
    public bool IncludeNullValues { get; set; } = false;
}

/// <summary>
/// Settings for CSV export
/// </summary>
public class CsvExportSettings
{
    /// <summary>
    /// CSV delimiter character
    /// </summary>
    public char Delimiter { get; set; } = ',';

    /// <summary>
    /// Whether to include header row
    /// </summary>
    public bool IncludeHeader { get; set; } = true;

    /// <summary>
    /// Text encoding
    /// </summary>
    public string Encoding { get; set; } = "UTF-8";
}

/// <summary>
/// Settings for HTML export
/// </summary>
public class HtmlExportSettings
{
    /// <summary>
    /// Whether to include CSS styles
    /// </summary>
    public bool IncludeCss { get; set; } = true;

    /// <summary>
    /// Whether to make responsive design
    /// </summary>
    public bool ResponsiveDesign { get; set; } = true;

    /// <summary>
    /// Custom CSS file path (optional)
    /// </summary>
    public string? CustomCssPath { get; set; }
}
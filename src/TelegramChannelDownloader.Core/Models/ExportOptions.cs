namespace TelegramChannelDownloader.Core.Models;

/// <summary>
/// Options for export operations
/// </summary>
public class ExportOptions
{
    /// <summary>
    /// Whether to overwrite existing files
    /// </summary>
    public bool OverwriteExisting { get; set; } = false;

    /// <summary>
    /// Whether to include metadata in the export
    /// </summary>
    public bool IncludeMetadata { get; set; } = true;

    /// <summary>
    /// Whether to include statistics in the export
    /// </summary>
    public bool IncludeStatistics { get; set; } = true;

    /// <summary>
    /// Whether to include media file information
    /// </summary>
    public bool IncludeMediaInfo { get; set; } = true;

    /// <summary>
    /// Whether to include message reactions
    /// </summary>
    public bool IncludeReactions { get; set; } = true;

    /// <summary>
    /// Whether to include forwarded message information
    /// </summary>
    public bool IncludeForwardedInfo { get; set; } = true;

    /// <summary>
    /// Whether to include reply information
    /// </summary>
    public bool IncludeReplyInfo { get; set; } = true;

    /// <summary>
    /// Date format for timestamps in exports
    /// </summary>
    public string DateFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";

    /// <summary>
    /// Whether to use UTC or local time for timestamps
    /// </summary>
    public bool UseUtcTime { get; set; } = true;

    /// <summary>
    /// Maximum length for message text in exports (0 = no limit)
    /// </summary>
    public int MaxMessageLength { get; set; } = 0;

    /// <summary>
    /// Whether to escape HTML entities in text
    /// </summary>
    public bool EscapeHtml { get; set; } = true;

    /// <summary>
    /// Custom template for export formatting (format-specific)
    /// </summary>
    public string? CustomTemplate { get; set; }

    /// <summary>
    /// Additional custom properties for specific export formats
    /// </summary>
    public Dictionary<string, object> CustomProperties { get; set; } = new();
}
namespace TelegramChannelDownloader.Core.Models;

/// <summary>
/// Available export formats for messages
/// </summary>
public enum ExportFormat
{
    /// <summary>
    /// Markdown format (.md)
    /// </summary>
    Markdown,

    /// <summary>
    /// JSON format (.json)
    /// </summary>
    Json,

    /// <summary>
    /// CSV format (.csv)
    /// </summary>
    Csv,

    /// <summary>
    /// HTML format (.html)
    /// </summary>
    Html,

    /// <summary>
    /// Plain text format (.txt)
    /// </summary>
    Text
}

/// <summary>
/// Extension methods for ExportFormat
/// </summary>
public static class ExportFormatExtensions
{
    /// <summary>
    /// Gets the file extension for the export format
    /// </summary>
    /// <param name="format">Export format</param>
    /// <returns>File extension including the dot</returns>
    public static string GetFileExtension(this ExportFormat format)
    {
        return format switch
        {
            ExportFormat.Markdown => ".md",
            ExportFormat.Json => ".json",
            ExportFormat.Csv => ".csv",
            ExportFormat.Html => ".html",
            ExportFormat.Text => ".txt",
            _ => ".txt"
        };
    }

    /// <summary>
    /// Gets the MIME type for the export format
    /// </summary>
    /// <param name="format">Export format</param>
    /// <returns>MIME type</returns>
    public static string GetMimeType(this ExportFormat format)
    {
        return format switch
        {
            ExportFormat.Markdown => "text/markdown",
            ExportFormat.Json => "application/json",
            ExportFormat.Csv => "text/csv",
            ExportFormat.Html => "text/html",
            ExportFormat.Text => "text/plain",
            _ => "text/plain"
        };
    }

    /// <summary>
    /// Gets a human-readable description of the export format
    /// </summary>
    /// <param name="format">Export format</param>
    /// <returns>Description</returns>
    public static string GetDescription(this ExportFormat format)
    {
        return format switch
        {
            ExportFormat.Markdown => "Markdown Document",
            ExportFormat.Json => "JSON Data",
            ExportFormat.Csv => "Comma-Separated Values",
            ExportFormat.Html => "HTML Document",
            ExportFormat.Text => "Plain Text",
            _ => "Unknown Format"
        };
    }
}
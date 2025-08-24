namespace TelegramChannelDownloader.Models;

/// <summary>
/// Represents the severity level of a log entry
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Informational messages (gray color)
    /// </summary>
    Info,
    
    /// <summary>
    /// Warning messages (orange color)
    /// </summary>
    Warning,
    
    /// <summary>
    /// Error messages (red color)
    /// </summary>
    Error
}
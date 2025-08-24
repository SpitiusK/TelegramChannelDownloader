using System;

namespace TelegramChannelDownloader.Models;

/// <summary>
/// Represents a single log entry with timestamp, level, and message
/// </summary>
public class LogEntry
{
    /// <summary>
    /// Gets or sets the timestamp when the log entry was created
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Gets or sets the severity level of the log entry
    /// </summary>
    public LogLevel Level { get; set; }
    
    /// <summary>
    /// Gets or sets the log message content
    /// </summary>
    public string Message { get; set; }
    
    /// <summary>
    /// Gets the formatted log entry string for display
    /// </summary>
    public string FormattedMessage => $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level.ToString().ToUpper()}] {Message}";
    
    /// <summary>
    /// Initializes a new instance of the LogEntry class
    /// </summary>
    /// <param name="level">The log level</param>
    /// <param name="message">The log message</param>
    public LogEntry(LogLevel level, string message)
    {
        Timestamp = DateTime.Now;
        Level = level;
        Message = message ?? string.Empty;
    }
    
    /// <summary>
    /// Initializes a new instance of the LogEntry class with a specific timestamp
    /// </summary>
    /// <param name="timestamp">The timestamp</param>
    /// <param name="level">The log level</param>
    /// <param name="message">The log message</param>
    public LogEntry(DateTime timestamp, LogLevel level, string message)
    {
        Timestamp = timestamp;
        Level = level;
        Message = message ?? string.Empty;
    }
}
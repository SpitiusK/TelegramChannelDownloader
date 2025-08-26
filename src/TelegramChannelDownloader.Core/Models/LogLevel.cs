namespace TelegramChannelDownloader.Core.Models;

/// <summary>
/// Logging levels for the application
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Trace level logging (most detailed)
    /// </summary>
    Trace = 0,

    /// <summary>
    /// Debug level logging
    /// </summary>
    Debug = 1,

    /// <summary>
    /// Information level logging
    /// </summary>
    Information = 2,

    /// <summary>
    /// Warning level logging
    /// </summary>
    Warning = 3,

    /// <summary>
    /// Error level logging
    /// </summary>
    Error = 4,

    /// <summary>
    /// Critical level logging (least detailed)
    /// </summary>
    Critical = 5,

    /// <summary>
    /// No logging
    /// </summary>
    None = 6
}

/// <summary>
/// Extension methods for LogLevel
/// </summary>
public static class LogLevelExtensions
{
    /// <summary>
    /// Gets a short name for the log level
    /// </summary>
    /// <param name="logLevel">Log level</param>
    /// <returns>Short name</returns>
    public static string ToShortName(this LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "TRCE",
            LogLevel.Debug => "DBUG",
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "FAIL",
            LogLevel.Critical => "CRIT",
            LogLevel.None => "NONE",
            _ => "UNKN"
        };
    }

    /// <summary>
    /// Determines if the log level is enabled based on minimum level
    /// </summary>
    /// <param name="logLevel">Log level to check</param>
    /// <param name="minimumLevel">Minimum enabled level</param>
    /// <returns>True if enabled</returns>
    public static bool IsEnabled(this LogLevel logLevel, LogLevel minimumLevel)
    {
        return logLevel >= minimumLevel && minimumLevel != LogLevel.None;
    }
}
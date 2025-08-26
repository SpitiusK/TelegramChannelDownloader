using TelegramChannelDownloader.Core.Models;

namespace TelegramChannelDownloader.Core.Exceptions;

/// <summary>
/// Exception thrown when export operations fail
/// </summary>
public class ExportException : TelegramCoreException
{
    /// <summary>
    /// Export ID that failed
    /// </summary>
    public string? ExportId { get; protected set; }

    /// <summary>
    /// Export format that was being used
    /// </summary>
    public ExportFormat? Format { get; protected set; }

    /// <summary>
    /// Output path where export was attempted
    /// </summary>
    public string? OutputPath { get; protected set; }

    /// <summary>
    /// Number of messages that were successfully exported before failure
    /// </summary>
    public int MessagesExported { get; protected set; }

    /// <summary>
    /// Creates a new export exception
    /// </summary>
    /// <param name="message">Error message</param>
    public ExportException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Creates a new export exception with inner exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public ExportException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Creates a new export exception with context
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="exportId">Export ID that failed</param>
    /// <param name="format">Export format</param>
    /// <param name="outputPath">Output path</param>
    /// <param name="messagesExported">Messages exported before failure</param>
    public ExportException(string message, string exportId, ExportFormat format, string outputPath, int messagesExported = 0)
        : base(message)
    {
        ExportId = exportId;
        Format = format;
        OutputPath = outputPath;
        MessagesExported = messagesExported;
        AddContext("ExportId", exportId);
        AddContext("Format", format);
        AddContext("OutputPath", outputPath);
        AddContext("MessagesExported", messagesExported);
    }

    /// <summary>
    /// Creates a new export exception with full context
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    /// <param name="exportId">Export ID that failed</param>
    /// <param name="format">Export format</param>
    /// <param name="outputPath">Output path</param>
    /// <param name="messagesExported">Messages exported before failure</param>
    public ExportException(string message, Exception innerException, string exportId, ExportFormat format, string outputPath, int messagesExported = 0)
        : base(message, innerException)
    {
        ExportId = exportId;
        Format = format;
        OutputPath = outputPath;
        MessagesExported = messagesExported;
        AddContext("ExportId", exportId);
        AddContext("Format", format);
        AddContext("OutputPath", outputPath);
        AddContext("MessagesExported", messagesExported);
    }
}

/// <summary>
/// Exception thrown when export file cannot be written
/// </summary>
public class ExportFileException : ExportException
{
    /// <summary>
    /// Creates a new export file exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="outputPath">File path that failed</param>
    /// <param name="innerException">Inner exception</param>
    public ExportFileException(string message, string outputPath, Exception? innerException = null)
        : base(message, innerException ?? new IOException(message))
    {
        OutputPath = outputPath;
        AddContext("OutputPath", outputPath);
    }
}

/// <summary>
/// Exception thrown when export format is not supported
/// </summary>
public class UnsupportedExportFormatException : ExportException
{
    /// <summary>
    /// Creates a new unsupported export format exception
    /// </summary>
    /// <param name="format">Unsupported format</param>
    public UnsupportedExportFormatException(ExportFormat format)
        : base($"Export format '{format}' is not supported")
    {
        Format = format;
        AddContext("Format", format);
    }
}

/// <summary>
/// Exception thrown when export file size exceeds limits
/// </summary>
public class ExportFileSizeException : ExportException
{
    /// <summary>
    /// Maximum allowed size in bytes
    /// </summary>
    public long MaxSizeBytes { get; }

    /// <summary>
    /// Actual size in bytes
    /// </summary>
    public long ActualSizeBytes { get; }

    /// <summary>
    /// Creates a new export file size exception
    /// </summary>
    /// <param name="maxSizeBytes">Maximum allowed size</param>
    /// <param name="actualSizeBytes">Actual size</param>
    /// <param name="exportId">Export ID</param>
    public ExportFileSizeException(long maxSizeBytes, long actualSizeBytes, string exportId)
        : base($"Export file size ({actualSizeBytes:N0} bytes) exceeds maximum allowed size ({maxSizeBytes:N0} bytes)", exportId, ExportFormat.Markdown, "", 0)
    {
        MaxSizeBytes = maxSizeBytes;
        ActualSizeBytes = actualSizeBytes;
        AddContext("MaxSizeBytes", maxSizeBytes);
        AddContext("ActualSizeBytes", actualSizeBytes);
    }
}
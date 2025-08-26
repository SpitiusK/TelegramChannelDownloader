using TelegramChannelDownloader.Core.Models;

namespace TelegramChannelDownloader.Core.Exceptions;

/// <summary>
/// Exception thrown when download operations fail
/// </summary>
public class DownloadException : TelegramCoreException
{
    /// <summary>
    /// Download ID that failed
    /// </summary>
    public string? DownloadId { get; }

    /// <summary>
    /// Phase during which the download failed
    /// </summary>
    public DownloadPhase? FailedPhase { get; }

    /// <summary>
    /// Number of messages that were successfully downloaded before failure
    /// </summary>
    public int MessagesDownloaded { get; }

    /// <summary>
    /// Creates a new download exception
    /// </summary>
    /// <param name="message">Error message</param>
    public DownloadException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Creates a new download exception with inner exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public DownloadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Creates a new download exception with context
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="downloadId">Download ID that failed</param>
    /// <param name="failedPhase">Phase during which download failed</param>
    /// <param name="messagesDownloaded">Messages downloaded before failure</param>
    public DownloadException(string message, string downloadId, DownloadPhase failedPhase, int messagesDownloaded = 0)
        : base(message)
    {
        DownloadId = downloadId;
        FailedPhase = failedPhase;
        MessagesDownloaded = messagesDownloaded;
        AddContext("DownloadId", downloadId);
        AddContext("FailedPhase", failedPhase);
        AddContext("MessagesDownloaded", messagesDownloaded);
    }

    /// <summary>
    /// Creates a new download exception with full context
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    /// <param name="downloadId">Download ID that failed</param>
    /// <param name="failedPhase">Phase during which download failed</param>
    /// <param name="messagesDownloaded">Messages downloaded before failure</param>
    public DownloadException(string message, Exception innerException, string downloadId, DownloadPhase failedPhase, int messagesDownloaded = 0)
        : base(message, innerException)
    {
        DownloadId = downloadId;
        FailedPhase = failedPhase;
        MessagesDownloaded = messagesDownloaded;
        AddContext("DownloadId", downloadId);
        AddContext("FailedPhase", failedPhase);
        AddContext("MessagesDownloaded", messagesDownloaded);
    }
}

/// <summary>
/// Exception thrown when download is cancelled
/// </summary>
public class DownloadCancelledException : DownloadException
{
    /// <summary>
    /// Creates a new download cancelled exception
    /// </summary>
    /// <param name="downloadId">Download ID that was cancelled</param>
    /// <param name="messagesDownloaded">Messages downloaded before cancellation</param>
    public DownloadCancelledException(string downloadId, int messagesDownloaded = 0)
        : base("Download was cancelled by user", downloadId, DownloadPhase.Cancelled, messagesDownloaded)
    {
    }
}

/// <summary>
/// Exception thrown when download times out
/// </summary>
public class DownloadTimeoutException : DownloadException
{
    /// <summary>
    /// Timeout duration
    /// </summary>
    public TimeSpan Timeout { get; }

    /// <summary>
    /// Creates a new download timeout exception
    /// </summary>
    /// <param name="timeout">Timeout duration</param>
    /// <param name="downloadId">Download ID that timed out</param>
    /// <param name="failedPhase">Phase during which timeout occurred</param>
    public DownloadTimeoutException(TimeSpan timeout, string downloadId, DownloadPhase failedPhase)
        : base($"Download timed out after {timeout.TotalSeconds:F1} seconds", downloadId, failedPhase)
    {
        Timeout = timeout;
        AddContext("Timeout", timeout);
    }
}

/// <summary>
/// Exception thrown when download quota is exceeded
/// </summary>
public class DownloadQuotaExceededException : DownloadException
{
    /// <summary>
    /// Maximum allowed messages
    /// </summary>
    public int MaxMessages { get; }

    /// <summary>
    /// Creates a new download quota exceeded exception
    /// </summary>
    /// <param name="maxMessages">Maximum allowed messages</param>
    /// <param name="downloadId">Download ID</param>
    public DownloadQuotaExceededException(int maxMessages, string downloadId)
        : base($"Download quota exceeded. Maximum {maxMessages:N0} messages allowed", downloadId, DownloadPhase.Validating)
    {
        MaxMessages = maxMessages;
        AddContext("MaxMessages", maxMessages);
    }
}
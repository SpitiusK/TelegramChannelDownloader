using TelegramChannelDownloader.Core.Models;
using TelegramChannelDownloader.TelegramApi.Channels.Models;

namespace TelegramChannelDownloader.Core.Services;

/// <summary>
/// Service for orchestrating download operations
/// </summary>
public interface IDownloadService
{
    /// <summary>
    /// Downloads messages from a Telegram channel
    /// </summary>
    /// <param name="request">Download request parameters</param>
    /// <param name="progress">Progress reporting callback</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Download result</returns>
    Task<DownloadResult> DownloadChannelAsync(
        DownloadRequest request, 
        IProgress<DownloadProgressInfo>? progress = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a download request before execution
    /// </summary>
    /// <param name="request">Download request to validate</param>
    /// <returns>Validation result</returns>
    Task<ValidationResult> ValidateDownloadRequestAsync(DownloadRequest request);

    /// <summary>
    /// Validates a channel URL and retrieves channel information
    /// </summary>
    /// <param name="channelUrl">Channel URL to validate</param>
    /// <returns>Validation result with channel information</returns>
    Task<ValidationResult<ChannelInfo>> ValidateChannelAsync(string channelUrl);

    /// <summary>
    /// Cancels an ongoing download operation
    /// </summary>
    /// <param name="downloadId">ID of the download to cancel</param>
    /// <returns>Task representing the cancellation operation</returns>
    Task CancelDownloadAsync(string downloadId);

    /// <summary>
    /// Gets the current status of a download operation
    /// </summary>
    /// <param name="downloadId">ID of the download</param>
    /// <returns>Download status</returns>
    Task<DownloadStatus> GetDownloadStatusAsync(string downloadId);

    /// <summary>
    /// Estimates the size of a download operation
    /// </summary>
    /// <param name="request">Download request</param>
    /// <returns>Estimated size in bytes</returns>
    Task<long> EstimateDownloadSizeAsync(DownloadRequest request);

    /// <summary>
    /// Event raised when download status changes
    /// </summary>
    event EventHandler<DownloadStatusChangedEventArgs> DownloadStatusChanged;
}

/// <summary>
/// Event arguments for download status changes
/// </summary>
public class DownloadStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// Download ID
    /// </summary>
    public string DownloadId { get; set; } = string.Empty;

    /// <summary>
    /// Previous download status
    /// </summary>
    public DownloadStatus PreviousStatus { get; set; } = default!;

    /// <summary>
    /// Current download status
    /// </summary>
    public DownloadStatus CurrentStatus { get; set; } = default!;

    /// <summary>
    /// Timestamp of the status change
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
using TelegramChannelDownloader.TelegramApi.Channels.Models;
using TelegramChannelDownloader.TelegramApi.Messages.Models;

namespace TelegramChannelDownloader.TelegramApi.Messages;

/// <summary>
/// Handles Telegram message operations
/// </summary>
public interface IMessageService
{
    /// <summary>
    /// Downloads all messages from a specified channel with progress reporting
    /// </summary>
    /// <param name="channelInfo">Channel information obtained from GetChannelInfoAsync</param>
    /// <param name="progress">Progress reporter for download status updates</param>
    /// <param name="cancellationToken">Cancellation token to cancel the download operation</param>
    /// <returns>List of downloaded messages</returns>
    Task<List<MessageData>> DownloadChannelMessagesAsync(ChannelInfo channelInfo, IProgress<DownloadProgressInfo>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads messages from a channel in batches for memory efficiency
    /// </summary>
    /// <param name="channelInfo">Channel information obtained from GetChannelInfoAsync</param>
    /// <param name="batchSize">Number of messages to download in each batch (default: 100)</param>
    /// <param name="progress">Progress reporter for download status updates</param>
    /// <param name="cancellationToken">Cancellation token to cancel the download operation</param>
    /// <returns>Async enumerable of message batches</returns>
    IAsyncEnumerable<List<MessageData>> DownloadChannelMessagesBatchAsync(ChannelInfo channelInfo, int batchSize = 100, IProgress<DownloadProgressInfo>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports downloaded messages to markdown format
    /// </summary>
    /// <param name="messages">List of messages to export</param>
    /// <param name="channelInfo">Channel information for header</param>
    /// <param name="outputPath">Full path where the markdown file should be saved</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the export operation</returns>
    Task ExportMessagesToMarkdownAsync(List<MessageData> messages, ChannelInfo channelInfo, string outputPath, CancellationToken cancellationToken = default);
}
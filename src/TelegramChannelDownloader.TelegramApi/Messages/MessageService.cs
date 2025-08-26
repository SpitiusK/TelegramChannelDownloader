using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Text;
using TelegramChannelDownloader.TelegramApi.Channels.Models;
using TelegramChannelDownloader.TelegramApi.Messages.Models;
using WTelegram;
using TL;

namespace TelegramChannelDownloader.TelegramApi.Messages;

/// <summary>
/// Implementation of message operations using WTelegramClient
/// </summary>
public class MessageService : IMessageService, IDisposable
{
    private readonly ILogger<MessageService> _logger;
    private readonly Client _client;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of MessageService
    /// </summary>
    public MessageService(ILogger<MessageService> logger, Client client)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    /// <summary>
    /// Downloads all messages from a specified channel with progress reporting
    /// </summary>
    /// <param name="channelInfo">Channel information obtained from GetChannelInfoAsync</param>
    /// <param name="progress">Progress reporter for download status updates</param>
    /// <param name="cancellationToken">Cancellation token to cancel the download operation</param>
    /// <returns>List of downloaded messages</returns>
    public async Task<List<MessageData>> DownloadChannelMessagesAsync(ChannelInfo channelInfo, IProgress<DownloadProgressInfo>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (channelInfo == null || !channelInfo.IsAccessible)
            {
                throw new ArgumentException("Invalid or inaccessible channel information", nameof(channelInfo));
            }

            _logger.LogInformation("Starting download of all messages from channel: {ChannelTitle} (ID: {ChannelId})", channelInfo.Title, channelInfo.Id);

            var allMessages = new List<MessageData>();
            var startTime = DateTime.UtcNow;

            // Use batch download to collect all messages
            await foreach (var batch in DownloadChannelMessagesBatchAsync(channelInfo, 100, progress, cancellationToken))
            {
                allMessages.AddRange(batch);
                
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Download cancelled by user. Downloaded {Count} messages so far.", allMessages.Count);
                    break;
                }
            }

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("Completed downloading {Count} messages from channel {ChannelTitle} in {Duration}", 
                allMessages.Count, channelInfo.Title, duration);

            return allMessages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download messages from channel: {ChannelTitle} (ID: {ChannelId})", channelInfo.Title, channelInfo.Id);
            throw;
        }
    }

    /// <summary>
    /// Downloads messages from a channel in batches for memory efficiency
    /// </summary>
    /// <param name="channelInfo">Channel information obtained from GetChannelInfoAsync</param>
    /// <param name="batchSize">Number of messages to download in each batch (default: 100)</param>
    /// <param name="progress">Progress reporter for download status updates</param>
    /// <param name="cancellationToken">Cancellation token to cancel the download operation</param>
    /// <returns>Async enumerable of message batches</returns>
    public async IAsyncEnumerable<List<MessageData>> DownloadChannelMessagesBatchAsync(
        ChannelInfo channelInfo, 
        int batchSize = 100, 
        IProgress<DownloadProgressInfo>? progress = null, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (channelInfo == null || !channelInfo.IsAccessible)
        {
            throw new ArgumentException("Invalid or inaccessible channel information", nameof(channelInfo));
        }

        if (batchSize <= 0 || batchSize > 100)
        {
            batchSize = 100; // Clamp to Telegram API limits
        }

        _logger.LogInformation("Starting batch download from channel: {ChannelTitle} (ID: {ChannelId}), batch size: {BatchSize}", 
            channelInfo.Title, channelInfo.Id, batchSize);

        var startTime = DateTime.UtcNow;
        var peer = new InputPeerChannel(channelInfo.Id, channelInfo.AccessHash ?? 0);
        
        // Get initial count estimate
        var initialHistory = await _client.Messages_GetHistory(peer, limit: 1);
        var totalEstimate = initialHistory switch
        {
            Messages_MessagesSlice slice => slice.count,
            Messages_ChannelMessages channelMessages => channelMessages.count,
            Messages_Messages messages => messages.messages?.Length ?? 0,
            _ => 0
        };
        int offsetId = 0;
        int totalProcessed = 0;

        _logger.LogDebug("Estimated total messages: {TotalEstimate}", totalEstimate);

        while (!cancellationToken.IsCancellationRequested)
        {
            List<MessageData> batch;
            bool isLastBatch = false;
            
            try
            {
                // Get batch of messages
                var history = await _client.Messages_GetHistory(
                    peer: peer, 
                    limit: batchSize, 
                    offset_id: offsetId,
                    add_offset: 0,
                    max_id: 0,
                    min_id: 0,
                    hash: 0);

                var historyMessages = history switch
                {
                    Messages_MessagesSlice sliceMsg => sliceMsg.messages,
                    Messages_ChannelMessages channelMsgs => channelMsgs.messages,
                    Messages_Messages msgs => msgs.messages,
                    _ => null
                };
                
                if (historyMessages == null || historyMessages.Length == 0)
                {
                    _logger.LogDebug("No more messages to download, stopping batch download");
                    break;
                }

                batch = new List<MessageData>();
                
                // Process messages in this batch
                foreach (var message in historyMessages)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    
                    var messageData = ConvertTelegramMessage(message, channelInfo);
                    if (messageData != null)
                    {
                        batch.Add(messageData);
                    }
                    
                    // Update offset for next batch
                    if (message.ID < offsetId || offsetId == 0)
                    {
                        offsetId = message.ID;
                    }
                }

                totalProcessed += batch.Count;
                
                // Report progress
                if (progress != null)
                {
                    var elapsed = DateTime.UtcNow - startTime;
                    var progressInfo = new DownloadProgressInfo
                    {
                        TotalMessages = totalEstimate,
                        DownloadedMessages = totalProcessed,
                        MessagesPerSecond = elapsed.TotalSeconds > 0 ? totalProcessed / elapsed.TotalSeconds : 0,
                        EstimatedTimeRemaining = elapsed.TotalSeconds > 0 && totalProcessed > 0 
                            ? TimeSpan.FromSeconds((totalEstimate - totalProcessed) * elapsed.TotalSeconds / totalProcessed)
                            : null
                    };
                    progress.Report(progressInfo);
                }

                _logger.LogTrace("Downloaded batch of {BatchSize} messages (total: {TotalProcessed}/{TotalEstimate})", 
                    batch.Count, totalProcessed, totalEstimate);

                // If we got fewer messages than requested, we've reached the end
                isLastBatch = historyMessages.Length < batchSize;
                if (isLastBatch)
                {
                    _logger.LogDebug("Received fewer messages than requested, download complete");
                }
            }
            catch (WTelegram.WTException ex) when (ex.Message.Contains("FLOOD_WAIT"))
            {
                // Extract wait time from error message and wait
                var waitSeconds = ExtractFloodWaitTime(ex.Message);
                _logger.LogWarning("Rate limited, waiting {WaitSeconds} seconds before continuing", waitSeconds);
                await Task.Delay(TimeSpan.FromSeconds(waitSeconds), cancellationToken);
                continue; // Skip yielding this iteration
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during batch download from channel: {ChannelTitle}", channelInfo.Title);
                throw;
            }

            yield return batch;

            // Check if this was the last batch
            if (batch.Count < batchSize || isLastBatch)
            {
                break;
            }

            // Small delay to avoid hitting rate limits
            await Task.Delay(100, cancellationToken);
        }

        var totalDuration = DateTime.UtcNow - startTime;
        _logger.LogInformation("Batch download completed. Downloaded {TotalProcessed} messages in {Duration}", 
            totalProcessed, totalDuration);
    }

    /// <summary>
    /// Exports downloaded messages to markdown format
    /// </summary>
    /// <param name="messages">List of messages to export</param>
    /// <param name="channelInfo">Channel information for header</param>
    /// <param name="outputPath">Full path where the markdown file should be saved</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the export operation</returns>
    public async Task ExportMessagesToMarkdownAsync(List<MessageData> messages, ChannelInfo channelInfo, string outputPath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (messages == null || messages.Count == 0)
            {
                throw new ArgumentException("No messages to export", nameof(messages));
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException("Output path cannot be empty", nameof(outputPath));
            }

            _logger.LogInformation("Exporting {MessageCount} messages to markdown: {OutputPath}", messages.Count, outputPath);

            // Ensure directory exists
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var markdown = new StringBuilder();
            
            // Add header
            markdown.AppendLine($"# {channelInfo.Title}");
            markdown.AppendLine();
            
            if (!string.IsNullOrWhiteSpace(channelInfo.Description))
            {
                markdown.AppendLine($"**Description:** {channelInfo.Description}");
                markdown.AppendLine();
            }
            
            markdown.AppendLine($"**Channel:** {channelInfo.DisplayName}");
            if (channelInfo.ChannelUrl != null)
            {
                markdown.AppendLine($"**URL:** {channelInfo.ChannelUrl}");
            }
            markdown.AppendLine($"**Messages:** {messages.Count:N0}");
            markdown.AppendLine($"**Exported:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            markdown.AppendLine();
            markdown.AppendLine("---");
            markdown.AppendLine();

            // Sort messages by timestamp (oldest first)
            var sortedMessages = messages.OrderBy(m => m.Timestamp).ToList();

            // Add messages
            foreach (var message in sortedMessages)
            {
                if (cancellationToken.IsCancellationRequested) break;

                markdown.AppendLine($"## Message {message.MessageId}");
                markdown.AppendLine();
                markdown.AppendLine($"**Date:** {message.Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
                markdown.AppendLine($"**Sender:** {message.SenderDisplay}");
                
                if (message.Views.HasValue)
                {
                    markdown.AppendLine($"**Views:** {message.Views:N0}");
                }

                if (message.IsEdited)
                {
                    markdown.AppendLine($"**Edited:** {message.EditedTimestamp:yyyy-MM-dd HH:mm:ss} UTC");
                }

                markdown.AppendLine();

                // Add forward info if available
                if (message.ForwardInfo != null)
                {
                    markdown.AppendLine("**Forwarded from:**");
                    if (!string.IsNullOrWhiteSpace(message.ForwardInfo.OriginalSender))
                    {
                        markdown.AppendLine($"- Sender: {message.ForwardInfo.OriginalSender}");
                    }
                    if (!string.IsNullOrWhiteSpace(message.ForwardInfo.OriginalChannel))
                    {
                        markdown.AppendLine($"- Channel: {message.ForwardInfo.OriginalChannel}");
                    }
                    markdown.AppendLine($"- Date: {message.ForwardInfo.OriginalDate:yyyy-MM-dd HH:mm:ss} UTC");
                    markdown.AppendLine();
                }

                // Add reply info if available
                if (message.ReplyInfo != null)
                {
                    markdown.AppendLine($"**Reply to message {message.ReplyInfo.ReplyToMessageId}**");
                    if (!string.IsNullOrWhiteSpace(message.ReplyInfo.OriginalMessagePreview))
                    {
                        markdown.AppendLine($"> {message.ReplyInfo.OriginalMessagePreview}");
                    }
                    markdown.AppendLine();
                }

                // Add message content
                if (!string.IsNullOrWhiteSpace(message.FormattedContent))
                {
                    markdown.AppendLine(message.FormattedContent);
                }

                // Add media info
                if (message.Media != null)
                {
                    markdown.AppendLine();
                    markdown.AppendLine($"**Media Type:** {message.MessageType}");
                    if (!string.IsNullOrWhiteSpace(message.Media.FileName))
                    {
                        markdown.AppendLine($"**File:** {message.Media.FileName}");
                    }
                    if (message.Media.FileSize.HasValue)
                    {
                        markdown.AppendLine($"**Size:** {FormatFileSize(message.Media.FileSize.Value)}");
                    }
                    if (!string.IsNullOrWhiteSpace(message.Media.Caption))
                    {
                        markdown.AppendLine($"**Caption:** {message.Media.Caption}");
                    }
                }

                // Add links, mentions, and hashtags
                if (message.Links.Count > 0)
                {
                    markdown.AppendLine();
                    markdown.AppendLine("**Links:**");
                    foreach (var link in message.Links)
                    {
                        markdown.AppendLine($"- {link}");
                    }
                }

                if (message.Mentions.Count > 0)
                {
                    markdown.AppendLine();
                    markdown.AppendLine("**Mentions:**");
                    foreach (var mention in message.Mentions)
                    {
                        markdown.AppendLine($"- @{mention}");
                    }
                }

                if (message.Hashtags.Count > 0)
                {
                    markdown.AppendLine();
                    markdown.AppendLine("**Hashtags:**");
                    foreach (var hashtag in message.Hashtags)
                    {
                        markdown.AppendLine($"- #{hashtag}");
                    }
                }

                markdown.AppendLine();
                markdown.AppendLine("---");
                markdown.AppendLine();
            }

            // Write to file
            await File.WriteAllTextAsync(outputPath, markdown.ToString(), cancellationToken);
            
            _logger.LogInformation("Successfully exported {MessageCount} messages to: {OutputPath}", messages.Count, outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export messages to markdown: {OutputPath}", outputPath);
            throw;
        }
    }

    /// <summary>
    /// Converts a Telegram message to our MessageData model
    /// </summary>
    private MessageData? ConvertTelegramMessage(MessageBase messageBase, ChannelInfo channelInfo)
    {
        try
        {
            if (messageBase is not Message telegramMessage)
            {
                return null; // Skip service messages or other types
            }

            var messageData = new MessageData
            {
                MessageId = telegramMessage.id,
                Timestamp = DateTime.UtcNow, // TODO: Fix date conversion
                Content = telegramMessage.message ?? string.Empty,
                ChannelId = channelInfo.Id,
                ChannelTitle = channelInfo.Title,
                Views = telegramMessage.views,
                // IsEdited = telegramMessage.edit_date != 0,
                // EditedTimestamp = telegramMessage.edit_date != 0 
                //     ? DateTimeOffset.FromUnixTimeSeconds(telegramMessage.edit_date).UtcDateTime 
                //     : null
            };

            // Set sender information
            if (telegramMessage.from_id != null)
            {
                messageData.SenderId = telegramMessage.from_id.ID;
                messageData.SenderDisplayName = $"User {telegramMessage.from_id.ID}";
            }

            // Process message type and media
            ProcessMessageTypeAndMedia(telegramMessage, messageData);

            // Process forward information
            if (telegramMessage.fwd_from != null)
            {
                messageData.ForwardInfo = new ForwardInfo
                {
                    // OriginalDate = DateTimeOffset.FromUnixTimeSeconds(telegramMessage.fwd_from.date).UtcDateTime,
                    OriginalSender = telegramMessage.fwd_from.from_name,
                    OriginalMessageId = telegramMessage.fwd_from.channel_post
                };

                if (telegramMessage.fwd_from.from_id != null)
                {
                    messageData.ForwardInfo.OriginalSender = $"User {telegramMessage.fwd_from.from_id.ID}";
                }
            }

            // Process reply information
            if (telegramMessage.reply_to is MessageReplyHeader replyHeader && replyHeader.reply_to_msg_id != 0)
            {
                messageData.ReplyInfo = new ReplyInfo
                {
                    ReplyToMessageId = replyHeader.reply_to_msg_id,
                    OriginalMessagePreview = "Reply message" // Would need additional API call to get full content
                };
            }

            // Process entities (mentions, links, hashtags)
            if (telegramMessage.entities != null && !string.IsNullOrWhiteSpace(messageData.Content))
            {
                ProcessMessageEntities(telegramMessage.entities, messageData);
            }

            // Process content to extract additional structured data
            messageData.ProcessContent();

            return messageData;
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Failed to convert Telegram message {MessageId}", messageBase.ID);
            return null; // Skip messages that fail to convert
        }
    }

    /// <summary>
    /// Processes message type and media information
    /// </summary>
    private void ProcessMessageTypeAndMedia(Message telegramMessage, MessageData messageData)
    {
        if (telegramMessage.media == null)
        {
            messageData.MessageType = MessageType.Text;
            return;
        }

        var mediaInfo = new MediaInfo();

        switch (telegramMessage.media)
        {
            case MessageMediaPhoto photo:
                messageData.MessageType = MessageType.Photo;
                if (photo.photo is Photo photoObj)
                {
                    // Get the largest photo size
                    var largestSize = photoObj.sizes?.OfType<PhotoSize>().OrderByDescending(s => s.w * s.h).FirstOrDefault();
                    if (largestSize != null)
                    {
                        mediaInfo.Width = largestSize.w;
                        mediaInfo.Height = largestSize.h;
                    }
                }
                break;

            case MessageMediaDocument doc:
                if (doc.document is Document document)
                {
                    mediaInfo.FileName = GetDocumentFileName(document);
                    mediaInfo.FileSize = document.size;
                    mediaInfo.MimeType = document.mime_type;
                    mediaInfo.FileId = document.id.ToString();

                    // Determine message type based on MIME type
                    messageData.MessageType = GetMessageTypeFromMimeType(document.mime_type);

                    // Get additional attributes
                    if (document.attributes != null)
                    {
                        foreach (var attr in document.attributes)
                        {
                            switch (attr)
                            {
                                case DocumentAttributeVideo video:
                                    mediaInfo.Duration = (int?)video.duration;
                                    mediaInfo.Width = video.w;
                                    mediaInfo.Height = video.h;
                                    break;
                                case DocumentAttributeAudio audio:
                                    mediaInfo.Duration = (int?)audio.duration;
                                    break;
                            }
                        }
                    }
                }
                break;

            case MessageMediaGeo _:
                messageData.MessageType = MessageType.Location;
                break;

            case MessageMediaContact _:
                messageData.MessageType = MessageType.Contact;
                break;

            case MessageMediaPoll _:
                messageData.MessageType = MessageType.Poll;
                break;

            default:
                messageData.MessageType = MessageType.Unknown;
                break;
        }

        if (mediaInfo.FileName != null || mediaInfo.FileSize.HasValue || mediaInfo.MimeType != null)
        {
            messageData.Media = mediaInfo;
        }
    }

    /// <summary>
    /// Gets the document filename from various attributes
    /// </summary>
    private static string? GetDocumentFileName(Document document)
    {
        if (document.attributes == null) return null;

        foreach (var attr in document.attributes)
        {
            if (attr is DocumentAttributeFilename filename)
            {
                return filename.file_name;
            }
        }

        return null;
    }

    /// <summary>
    /// Determines message type from MIME type
    /// </summary>
    private static MessageType GetMessageTypeFromMimeType(string? mimeType)
    {
        if (string.IsNullOrWhiteSpace(mimeType)) return MessageType.Document;

        return mimeType.ToLowerInvariant() switch
        {
            var mime when mime.StartsWith("image/") => MessageType.Photo,
            var mime when mime.StartsWith("video/") => MessageType.Video,
            var mime when mime.StartsWith("audio/") => MessageType.Audio,
            "image/gif" => MessageType.Animation,
            var mime when mime == "video/mp4" && mimeType.Contains("gif") => MessageType.Animation,
            _ => MessageType.Document
        };
    }

    /// <summary>
    /// Processes message entities to extract structured data
    /// </summary>
    private void ProcessMessageEntities(MessageEntity[] entities, MessageData messageData)
    {
        var content = messageData.Content;
        if (string.IsNullOrWhiteSpace(content)) return;

        foreach (var entity in entities)
        {
            try
            {
                var entityText = content.Substring(entity.offset, Math.Min(entity.length, content.Length - entity.offset));

                switch (entity)
                {
                    case MessageEntityUrl _:
                        if (!messageData.Links.Contains(entityText))
                            messageData.Links.Add(entityText);
                        break;
                    case MessageEntityTextUrl textUrl:
                        if (!string.IsNullOrWhiteSpace(textUrl.url) && !messageData.Links.Contains(textUrl.url))
                            messageData.Links.Add(textUrl.url);
                        break;
                    case MessageEntityMention _:
                        var mention = entityText.TrimStart('@');
                        if (!messageData.Mentions.Contains(mention))
                            messageData.Mentions.Add(mention);
                        break;
                    case MessageEntityHashtag _:
                        var hashtag = entityText.TrimStart('#');
                        if (!messageData.Hashtags.Contains(hashtag))
                            messageData.Hashtags.Add(hashtag);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Error processing message entity");
                // Continue processing other entities
            }
        }
    }

    /// <summary>
    /// Extracts flood wait time from error message
    /// </summary>
    private static int ExtractFloodWaitTime(string errorMessage)
    {
        var match = System.Text.RegularExpressions.Regex.Match(errorMessage, @"FLOOD_WAIT_(\d+)");
        return match.Success && int.TryParse(match.Groups[1].Value, out var seconds) ? seconds : 30;
    }

    /// <summary>
    /// Formats file size in human readable format
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        double size = bytes;
        int suffixIndex = 0;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return $"{size:F1} {suffixes[suffixIndex]}";
    }

    /// <summary>
    /// Disposes of the message service
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            // Note: We don't dispose the client here as it's injected and managed externally
            _disposed = true;
        }
    }
}
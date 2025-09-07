using System.Text.Json;
using TelegramChannelDownloader.DataBase.Entities;
using TelegramChannelDownloader.TelegramApi.Messages.Models;
using TelegramChannelDownloader.TelegramApi.Channels.Models;
using Microsoft.Extensions.Logging;

namespace TelegramChannelDownloader.Core.Services;

/// <summary>
/// Service for mapping between TelegramApi models and database entities
/// </summary>
public class MessageMappingService : IMessageMappingService
{
    private readonly ILogger<MessageMappingService> _logger;

    public MessageMappingService(ILogger<MessageMappingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create a DownloadSession from channel information
    /// </summary>
    public DownloadSession CreateDownloadSession(ChannelInfo channelInfo, string? exportFormat = null)
    {
        return new DownloadSession
        {
            Id = Guid.NewGuid(),
            ChannelUsername = channelInfo.Username ?? string.Empty,
            ChannelTitle = channelInfo.DisplayName,
            ChannelId = channelInfo.Id,
            StartedAt = DateTime.UtcNow,
            Status = DownloadSessionStatus.InProgress,
            ExportFormat = exportFormat,
            ExpiresAt = DateTime.UtcNow.AddDays(30), // Default 30-day expiry
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Convert TelegramApi MessageData to database TelegramMessage entity
    /// </summary>
    public TelegramMessage ConvertToEntity(MessageData messageData, Guid sessionId)
    {
        try
        {
            var entity = new TelegramMessage
            {
                Id = messageData.MessageId,
                DownloadSessionId = sessionId,
                FromId = messageData.SenderId ?? 0,
                FromUsername = messageData.SenderUsername,
                FromDisplayName = messageData.SenderDisplayName,
                Content = messageData.Content,
                Date = messageData.Timestamp,
                MessageType = MapMessageType(messageData.MessageType),
                HasMedia = messageData.Media != null,
                Views = messageData.Views ?? 0,
                Forwards = 0, // Not available in current TelegramApi model
                IsForwarded = messageData.ForwardInfo != null,
                IsEdited = messageData.IsEdited,
                EditedAt = messageData.EditedTimestamp,
                IsPinned = false, // Not available in current TelegramApi model
                CreatedAt = DateTime.UtcNow
            };

            // Map media information if available
            if (messageData.Media != null)
            {
                entity.MediaType = messageData.MessageType.ToString().ToLowerInvariant();
                entity.MediaFileName = messageData.Media.FileName;
                entity.MediaFileSize = messageData.Media.FileSize;
                entity.MediaMimeType = messageData.Media.MimeType;
            }

            // Map reply information if available
            if (messageData.ReplyInfo != null)
            {
                entity.ReplyToMessageId = messageData.ReplyInfo.ReplyToMessageId;
            }

            // Map forward information if available
            if (messageData.ForwardInfo != null)
            {
                entity.ForwardedFromMessageId = messageData.ForwardInfo.OriginalMessageId;
                // Note: ForwardedFromId not available in current model structure
            }

            // Store complete message data as JSON for debugging/future use
            try
            {
                var rawData = new
                {
                    messageData.Links,
                    messageData.Mentions,
                    messageData.Hashtags,
                    ForwardInfo = messageData.ForwardInfo != null ? new
                    {
                        messageData.ForwardInfo.OriginalSender,
                        messageData.ForwardInfo.OriginalChannel,
                        messageData.ForwardInfo.OriginalDate,
                        messageData.ForwardInfo.OriginalMessageId
                    } : null,
                    ReplyInfo = messageData.ReplyInfo != null ? new
                    {
                        messageData.ReplyInfo.ReplyToMessageId,
                        messageData.ReplyInfo.OriginalSender,
                        messageData.ReplyInfo.OriginalMessagePreview
                    } : null,
                    Media = messageData.Media != null ? new
                    {
                        messageData.Media.FileName,
                        messageData.Media.FileSize,
                        messageData.Media.MimeType,
                        messageData.Media.Caption,
                        messageData.Media.Width,
                        messageData.Media.Height,
                        messageData.Media.Duration,
                        messageData.Media.FileId
                    } : null,
                    messageData.ChannelId,
                    messageData.ChannelTitle
                };

                entity.RawData = JsonSerializer.Serialize(rawData, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to serialize raw data for message {MessageId}", messageData.MessageId);
            }

            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert MessageData to TelegramMessage entity for message {MessageId}", messageData.MessageId);
            throw;
        }
    }

    /// <summary>
    /// Convert a batch of MessageData to TelegramMessage entities
    /// </summary>
    public List<TelegramMessage> ConvertToEntities(IEnumerable<MessageData> messages, Guid sessionId)
    {
        var entities = new List<TelegramMessage>();
        
        foreach (var message in messages)
        {
            try
            {
                entities.Add(ConvertToEntity(message, sessionId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Skipping message {MessageId} due to conversion error", message.MessageId);
                // Continue processing other messages rather than failing the entire batch
            }
        }

        return entities;
    }

    /// <summary>
    /// Convert database TelegramMessage back to MessageData (for export)
    /// </summary>
    public MessageData ConvertToMessageData(TelegramMessage entity)
    {
        try
        {
            var messageData = new MessageData
            {
                MessageId = (int)entity.Id,
                Timestamp = entity.Date,
                SenderUsername = entity.FromUsername,
                SenderDisplayName = entity.FromDisplayName,
                SenderId = entity.FromId,
                Content = entity.Content ?? string.Empty,
                Views = entity.Views,
                IsEdited = entity.IsEdited,
                EditedTimestamp = entity.EditedAt,
                MessageType = MapToTelegramApiMessageType(entity.MessageType),
                ChannelId = entity.DownloadSession?.ChannelId ?? 0,
                ChannelTitle = entity.DownloadSession?.ChannelTitle ?? string.Empty
            };

            // Reconstruct media information if available
            if (entity.HasMedia && !string.IsNullOrEmpty(entity.MediaType))
            {
                messageData.Media = new MediaInfo
                {
                    FileName = entity.MediaFileName,
                    FileSize = entity.MediaFileSize,
                    MimeType = entity.MediaMimeType
                };
            }

            // Reconstruct reply information
            if (entity.ReplyToMessageId.HasValue)
            {
                messageData.ReplyInfo = new ReplyInfo
                {
                    ReplyToMessageId = (int)entity.ReplyToMessageId.Value
                };
            }

            // Reconstruct forward information
            if (entity.IsForwarded && entity.ForwardedFromMessageId.HasValue)
            {
                messageData.ForwardInfo = new ForwardInfo
                {
                    OriginalMessageId = (int)entity.ForwardedFromMessageId.Value
                };
            }

            // Deserialize additional data from RawData if needed
            if (!string.IsNullOrEmpty(entity.RawData))
            {
                try
                {
                    using var document = JsonDocument.Parse(entity.RawData);
                    var root = document.RootElement;

                    if (root.TryGetProperty("links", out var linksElement) && linksElement.ValueKind == JsonValueKind.Array)
                    {
                        messageData.Links = linksElement.EnumerateArray()
                            .Where(x => x.ValueKind == JsonValueKind.String)
                            .Select(x => x.GetString()!)
                            .Where(x => !string.IsNullOrEmpty(x))
                            .ToList();
                    }

                    if (root.TryGetProperty("mentions", out var mentionsElement) && mentionsElement.ValueKind == JsonValueKind.Array)
                    {
                        messageData.Mentions = mentionsElement.EnumerateArray()
                            .Where(x => x.ValueKind == JsonValueKind.String)
                            .Select(x => x.GetString()!)
                            .Where(x => !string.IsNullOrEmpty(x))
                            .ToList();
                    }

                    if (root.TryGetProperty("hashtags", out var hashtagsElement) && hashtagsElement.ValueKind == JsonValueKind.Array)
                    {
                        messageData.Hashtags = hashtagsElement.EnumerateArray()
                            .Where(x => x.ValueKind == JsonValueKind.String)
                            .Select(x => x.GetString()!)
                            .Where(x => !string.IsNullOrEmpty(x))
                            .ToList();
                    }

                    // Reconstruct more complex objects from raw data if needed
                    if (root.TryGetProperty("forwardInfo", out var forwardElement) && forwardElement.ValueKind == JsonValueKind.Object)
                    {
                        if (messageData.ForwardInfo == null) messageData.ForwardInfo = new ForwardInfo();
                        
                        if (forwardElement.TryGetProperty("originalSender", out var senderElement) && senderElement.ValueKind == JsonValueKind.String)
                        {
                            messageData.ForwardInfo.OriginalSender = senderElement.GetString();
                        }
                        
                        if (forwardElement.TryGetProperty("originalChannel", out var channelElement) && channelElement.ValueKind == JsonValueKind.String)
                        {
                            messageData.ForwardInfo.OriginalChannel = channelElement.GetString();
                        }
                    }

                    if (root.TryGetProperty("replyInfo", out var replyElement) && replyElement.ValueKind == JsonValueKind.Object)
                    {
                        if (messageData.ReplyInfo == null) messageData.ReplyInfo = new ReplyInfo();
                        
                        if (replyElement.TryGetProperty("originalSender", out var origSenderElement) && origSenderElement.ValueKind == JsonValueKind.String)
                        {
                            messageData.ReplyInfo.OriginalSender = origSenderElement.GetString();
                        }
                        
                        if (replyElement.TryGetProperty("originalMessagePreview", out var previewElement) && previewElement.ValueKind == JsonValueKind.String)
                        {
                            messageData.ReplyInfo.OriginalMessagePreview = previewElement.GetString();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize raw data for message {MessageId}", entity.Id);
                }
            }

            return messageData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert TelegramMessage entity to MessageData for message {MessageId}", entity.Id);
            throw;
        }
    }

    /// <summary>
    /// Map TelegramApi MessageType to database MessageType
    /// </summary>
    private static DataBase.Entities.MessageType MapMessageType(TelegramApi.Messages.Models.MessageType telegramMessageType)
    {
        return telegramMessageType switch
        {
            TelegramApi.Messages.Models.MessageType.Text => DataBase.Entities.MessageType.Text,
            TelegramApi.Messages.Models.MessageType.Photo => DataBase.Entities.MessageType.Photo,
            TelegramApi.Messages.Models.MessageType.Video => DataBase.Entities.MessageType.Video,
            TelegramApi.Messages.Models.MessageType.Audio => DataBase.Entities.MessageType.Audio,
            TelegramApi.Messages.Models.MessageType.Voice => DataBase.Entities.MessageType.Voice,
            TelegramApi.Messages.Models.MessageType.Document => DataBase.Entities.MessageType.Document,
            TelegramApi.Messages.Models.MessageType.Sticker => DataBase.Entities.MessageType.Sticker,
            TelegramApi.Messages.Models.MessageType.Animation => DataBase.Entities.MessageType.Animation,
            TelegramApi.Messages.Models.MessageType.VideoNote => DataBase.Entities.MessageType.VideoNote,
            TelegramApi.Messages.Models.MessageType.Location => DataBase.Entities.MessageType.Location,
            TelegramApi.Messages.Models.MessageType.Contact => DataBase.Entities.MessageType.Contact,
            TelegramApi.Messages.Models.MessageType.Poll => DataBase.Entities.MessageType.Poll,
            TelegramApi.Messages.Models.MessageType.Service => DataBase.Entities.MessageType.Service,
            TelegramApi.Messages.Models.MessageType.Unknown => DataBase.Entities.MessageType.Text, // Default unknown to text
            _ => DataBase.Entities.MessageType.Text
        };
    }

    /// <summary>
    /// Map database MessageType back to TelegramApi MessageType
    /// </summary>
    private static TelegramApi.Messages.Models.MessageType MapToTelegramApiMessageType(DataBase.Entities.MessageType dbMessageType)
    {
        return dbMessageType switch
        {
            DataBase.Entities.MessageType.Text => TelegramApi.Messages.Models.MessageType.Text,
            DataBase.Entities.MessageType.Photo => TelegramApi.Messages.Models.MessageType.Photo,
            DataBase.Entities.MessageType.Video => TelegramApi.Messages.Models.MessageType.Video,
            DataBase.Entities.MessageType.Audio => TelegramApi.Messages.Models.MessageType.Audio,
            DataBase.Entities.MessageType.Voice => TelegramApi.Messages.Models.MessageType.Voice,
            DataBase.Entities.MessageType.Document => TelegramApi.Messages.Models.MessageType.Document,
            DataBase.Entities.MessageType.Sticker => TelegramApi.Messages.Models.MessageType.Sticker,
            DataBase.Entities.MessageType.Animation => TelegramApi.Messages.Models.MessageType.Animation,
            DataBase.Entities.MessageType.VideoNote => TelegramApi.Messages.Models.MessageType.VideoNote,
            DataBase.Entities.MessageType.Location => TelegramApi.Messages.Models.MessageType.Location,
            DataBase.Entities.MessageType.Contact => TelegramApi.Messages.Models.MessageType.Contact,
            DataBase.Entities.MessageType.Poll => TelegramApi.Messages.Models.MessageType.Poll,
            DataBase.Entities.MessageType.Service => TelegramApi.Messages.Models.MessageType.Service,
            _ => TelegramApi.Messages.Models.MessageType.Text
        };
    }
}

/// <summary>
/// Interface for message mapping service
/// </summary>
public interface IMessageMappingService
{
    /// <summary>
    /// Create a DownloadSession from channel information
    /// </summary>
    DownloadSession CreateDownloadSession(ChannelInfo channelInfo, string? exportFormat = null);

    /// <summary>
    /// Convert TelegramApi MessageData to database TelegramMessage entity
    /// </summary>
    TelegramMessage ConvertToEntity(MessageData messageData, Guid sessionId);

    /// <summary>
    /// Convert a batch of MessageData to TelegramMessage entities
    /// </summary>
    List<TelegramMessage> ConvertToEntities(IEnumerable<MessageData> messages, Guid sessionId);

    /// <summary>
    /// Convert database TelegramMessage back to MessageData
    /// </summary>
    MessageData ConvertToMessageData(TelegramMessage entity);
}
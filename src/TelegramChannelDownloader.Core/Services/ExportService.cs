using TelegramChannelDownloader.Core.Models;
using TelegramChannelDownloader.Core.Exceptions;
using TelegramChannelDownloader.TelegramApi.Messages.Models;
using TelegramChannelDownloader.TelegramApi.Channels.Models;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace TelegramChannelDownloader.Core.Services;

/// <summary>
/// Service for handling export operations in different formats
/// </summary>
public class ExportService : IExportService
{
    private readonly IValidationService _validationService;
    private readonly ILogger<ExportService> _logger;
    private static readonly List<ExportFormat> SupportedFormats = new()
    {
        ExportFormat.Markdown,
        ExportFormat.Json,
        ExportFormat.Csv
    };

    public ExportService(IValidationService validationService, ILogger<ExportService> logger)
    {
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ExportResult> ExportToMarkdownAsync(ExportRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var validation = _validationService.ValidateExportRequest(request);
        if (!validation.IsValid)
            throw new ExportException($"Invalid export request: {validation.ErrorMessage}");

        try
        {
            _logger.LogInformation("Starting Markdown export to {OutputPath}", request.OutputPath);

            var content = await GenerateMarkdownContentAsync(request, cancellationToken);
            
            // Write to file
            await File.WriteAllTextAsync(request.OutputPath, content, Encoding.UTF8, cancellationToken);

            var fileSize = new FileInfo(request.OutputPath).Length;
            
            _logger.LogInformation("Markdown export completed. File size: {FileSize} bytes", fileSize);

            return new ExportResult
            {
                IsSuccess = true,
                FilePath = request.OutputPath,
                Format = ExportFormat.Markdown,
                FileSize = fileSize,
                MessagesExported = request.Messages.Count()
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Markdown export cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Markdown export failed: {Error}", ex.Message);
            return new ExportResult
            {
                IsSuccess = false,
                Format = ExportFormat.Markdown,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<ExportResult> ExportToJsonAsync(ExportRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var validation = _validationService.ValidateExportRequest(request);
        if (!validation.IsValid)
            throw new ExportException($"Invalid export request: {validation.ErrorMessage}");

        try
        {
            _logger.LogInformation("Starting JSON export to {OutputPath}", request.OutputPath);

            var exportData = new
            {
                ExportInfo = new
                {
                    ExportedAt = DateTime.UtcNow,
                    Format = "JSON",
                    Version = "1.0",
                    MessageCount = request.Messages.Count()
                },
                Channel = request.ChannelInfo != null ? new
                {
                    request.ChannelInfo.Id,
                    request.ChannelInfo.Title,
                    request.ChannelInfo.Username,
                    request.ChannelInfo.Type,
                    request.ChannelInfo.MemberCount,
                    request.ChannelInfo.MessageCount,
                    request.ChannelInfo.Description
                } : null,
                Messages = request.Messages.Select(msg => new
                {
                    Id = msg.MessageId,
                    Date = msg.Timestamp,
                    Text = msg.Content,
                    AuthorName = msg.SenderDisplayName,
                    AuthorId = msg.SenderId,
                    IsForwarded = msg.ForwardInfo != null,
                    OriginalAuthor = msg.ForwardInfo?.OriginalSender,
                    ReplyToMessageId = msg.ReplyInfo?.ReplyToMessageId,
                    msg.Views,
                    MediaType = msg.MessageType.ToString(),
                    MediaPath = msg.Media?.FileName,
                    MediaSize = msg.Media?.FileSize,
                    Links = msg.Links,
                    Mentions = msg.Mentions,
                    Hashtags = msg.Hashtags
                }).ToArray()
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(exportData, options);
            await File.WriteAllTextAsync(request.OutputPath, json, Encoding.UTF8, cancellationToken);

            var fileSize = new FileInfo(request.OutputPath).Length;
            
            _logger.LogInformation("JSON export completed. File size: {FileSize} bytes", fileSize);

            return new ExportResult
            {
                IsSuccess = true,
                FilePath = request.OutputPath,
                Format = ExportFormat.Json,
                FileSize = fileSize,
                MessagesExported = request.Messages.Count()
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("JSON export cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JSON export failed: {Error}", ex.Message);
            return new ExportResult
            {
                IsSuccess = false,
                Format = ExportFormat.Json,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<ExportResult> ExportToCsvAsync(ExportRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var validation = _validationService.ValidateExportRequest(request);
        if (!validation.IsValid)
            throw new ExportException($"Invalid export request: {validation.ErrorMessage}");

        try
        {
            _logger.LogInformation("Starting CSV export to {OutputPath}", request.OutputPath);

            var csv = new StringBuilder();
            
            // CSV Header
            csv.AppendLine("Id,Date,AuthorName,AuthorId,Text,IsForwarded,OriginalAuthor,ReplyToMessageId,Views,MediaType,MediaPath,MediaSize");

            // CSV Data
            foreach (var message in request.Messages)
            {
                var fields = new[]
                {
                    EscapeCsvField(message.MessageId.ToString()),
                    EscapeCsvField(message.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")),
                    EscapeCsvField(message.SenderDisplayName ?? ""),
                    EscapeCsvField(message.SenderId?.ToString() ?? ""),
                    EscapeCsvField(message.Content ?? ""),
                    EscapeCsvField((message.ForwardInfo != null).ToString()),
                    EscapeCsvField(message.ForwardInfo?.OriginalSender ?? ""),
                    EscapeCsvField(message.ReplyInfo?.ReplyToMessageId.ToString() ?? ""),
                    EscapeCsvField(message.Views?.ToString() ?? ""),
                    EscapeCsvField(message.MessageType.ToString()),
                    EscapeCsvField(message.Media?.FileName ?? ""),
                    EscapeCsvField(message.Media?.FileSize?.ToString() ?? "")
                };

                csv.AppendLine(string.Join(",", fields));
            }

            await File.WriteAllTextAsync(request.OutputPath, csv.ToString(), Encoding.UTF8, cancellationToken);

            var fileSize = new FileInfo(request.OutputPath).Length;
            
            _logger.LogInformation("CSV export completed. File size: {FileSize} bytes", fileSize);

            return new ExportResult
            {
                IsSuccess = true,
                FilePath = request.OutputPath,
                Format = ExportFormat.Csv,
                FileSize = fileSize,
                MessagesExported = request.Messages.Count()
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("CSV export cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CSV export failed: {Error}", ex.Message);
            return new ExportResult
            {
                IsSuccess = false,
                Format = ExportFormat.Csv,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<List<ExportFormat>> GetSupportedFormatsAsync()
    {
        return await Task.FromResult(new List<ExportFormat>(SupportedFormats));
    }

    /// <inheritdoc />
    public async Task<long> EstimateExportSizeAsync(ExportRequest request)
    {
        if (request == null || !request.Messages.Any())
            return 0;

        try
        {
            var messageCount = request.Messages.Count();
            var averageMessageLength = request.Messages.Average(m => (m.Content?.Length ?? 0) + (m.SenderDisplayName?.Length ?? 0) + 50); // 50 for metadata

            var estimatedSize = request.Format switch
            {
                ExportFormat.Markdown => (long)(messageCount * (averageMessageLength * 1.2 + 100)), // Markdown formatting overhead
                ExportFormat.Json => (long)(messageCount * (averageMessageLength * 1.5 + 200)), // JSON structure overhead
                ExportFormat.Csv => (long)(messageCount * (averageMessageLength + 50)), // CSV is compact
                _ => (long)(messageCount * averageMessageLength)
            };

            return estimatedSize;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to estimate export size");
            return 0;
        }
    }

    /// <inheritdoc />
    public string GenerateSafeFileName(string channelName, ExportFormat exportFormat, bool includeTimestamp = true)
    {
        if (string.IsNullOrWhiteSpace(channelName))
            channelName = "telegram_export";

        // Remove invalid filename characters
        var invalidChars = Path.GetInvalidFileNameChars();
        var safeName = new StringBuilder();
        
        foreach (var c in channelName)
        {
            if (invalidChars.Contains(c))
                safeName.Append('_');
            else
                safeName.Append(c);
        }

        var fileName = safeName.ToString().Trim('_');
        
        // Limit filename length
        if (fileName.Length > 50)
        {
            fileName = fileName.Substring(0, 50);
        }

        // Add timestamp if requested
        if (includeTimestamp)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            fileName = $"{fileName}_{timestamp}";
        }

        // Add extension
        var extension = exportFormat switch
        {
            ExportFormat.Markdown => ".md",
            ExportFormat.Json => ".json",
            ExportFormat.Csv => ".csv",
            _ => ".txt"
        };

        return $"{fileName}{extension}";
    }

    /// <inheritdoc />
    public async Task<ValidationResult> ValidateExportRequestAsync(ExportRequest request)
    {
        return await Task.FromResult(_validationService.ValidateExportRequest(request));
    }

    private async Task<string> GenerateMarkdownContentAsync(ExportRequest request, CancellationToken cancellationToken)
    {
        var markdown = new StringBuilder();
        
        // Header
        markdown.AppendLine($"# {request.ChannelInfo?.Title ?? "Telegram Channel Export"}");
        markdown.AppendLine();
        
        if (request.ChannelInfo != null)
        {
            markdown.AppendLine("## Channel Information");
            markdown.AppendLine();
            markdown.AppendLine($"- **Name:** {request.ChannelInfo.Title}");
            if (!string.IsNullOrEmpty(request.ChannelInfo.Username))
                markdown.AppendLine($"- **Username:** @{request.ChannelInfo.Username}");
            markdown.AppendLine($"- **Type:** {request.ChannelInfo.Type}");
            if (request.ChannelInfo.MemberCount > 0)
                markdown.AppendLine($"- **Members:** {request.ChannelInfo.MemberCount:N0}");
            markdown.AppendLine($"- **Total Messages:** {request.ChannelInfo.MessageCount:N0}");
            if (!string.IsNullOrEmpty(request.ChannelInfo.Description))
            {
                markdown.AppendLine($"- **Description:** {request.ChannelInfo.Description}");
            }
            markdown.AppendLine();
        }

        // Export information
        if (request.Options?.IncludeMetadata == true)
        {
            markdown.AppendLine("## Export Information");
            markdown.AppendLine();
            markdown.AppendLine($"- **Export Date:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            markdown.AppendLine($"- **Messages Exported:** {request.Messages.Count():N0}");
            markdown.AppendLine($"- **Format:** Markdown");
            markdown.AppendLine();
        }

        // Statistics
        if (request.Options?.IncludeStatistics == true)
        {
            var stats = CalculateStatistics(request.Messages);
            markdown.AppendLine("## Statistics");
            markdown.AppendLine();
            markdown.AppendLine($"- **Date Range:** {stats.StartDate:yyyy-MM-dd} to {stats.EndDate:yyyy-MM-dd}");
            markdown.AppendLine($"- **Total Authors:** {stats.UniqueAuthors}");
            markdown.AppendLine($"- **Messages with Media:** {stats.MessagesWithMedia}");
            markdown.AppendLine($"- **Forwarded Messages:** {stats.ForwardedMessages}");
            markdown.AppendLine($"- **Total Views:** {stats.TotalViews:N0}");
            markdown.AppendLine();
        }

        // Messages
        markdown.AppendLine("## Messages");
        markdown.AppendLine();

        foreach (var message in request.Messages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            markdown.AppendLine("---");
            markdown.AppendLine();
            
            // Message header
            var header = new StringBuilder();
            header.Append($"**{message.Timestamp:yyyy-MM-dd HH:mm:ss}**");
            
            if (!string.IsNullOrEmpty(message.SenderDisplayName))
                header.Append($" - {EscapeMarkdown(message.SenderDisplayName)}");
            
            if (message.Views.HasValue)
                header.Append($" - 👁 {message.Views:N0}");

            markdown.AppendLine(header.ToString());
            markdown.AppendLine();

            // Forwarded message info
            if (message.ForwardInfo != null && !string.IsNullOrEmpty(message.ForwardInfo.OriginalSender))
            {
                markdown.AppendLine($"*Forwarded from: {EscapeMarkdown(message.ForwardInfo.OriginalSender)}*");
                markdown.AppendLine();
            }

            // Reply info
            if (message.ReplyInfo != null)
            {
                markdown.AppendLine($"*Replying to message #{message.ReplyInfo.ReplyToMessageId}*");
                markdown.AppendLine();
            }

            // Message text
            if (!string.IsNullOrEmpty(message.Content))
            {
                markdown.AppendLine(EscapeMarkdown(message.Content));
                markdown.AppendLine();
            }

            // Media info
            if (message.Media != null)
            {
                markdown.AppendLine($"📎 **Media:** {message.MessageType}");
                if (!string.IsNullOrEmpty(message.Media.FileName))
                    markdown.AppendLine($"📁 **File:** `{message.Media.FileName}`");
                if (message.Media.FileSize.HasValue)
                    markdown.AppendLine($"📊 **Size:** {FormatFileSize(message.Media.FileSize.Value)}");
                markdown.AppendLine();
            }

            // Links, mentions, hashtags
            if (message.Links.Any())
            {
                markdown.AppendLine($"🔗 **Links:** {string.Join(", ", message.Links)}");
            }
            if (message.Mentions.Any())
            {
                markdown.AppendLine($"👥 **Mentions:** {string.Join(", ", message.Mentions.Select(m => "@" + m))}");
            }
            if (message.Hashtags.Any())
            {
                markdown.AppendLine($"#️⃣ **Hashtags:** {string.Join(", ", message.Hashtags.Select(h => "#" + h))}");
            }
        }

        return markdown.ToString();
    }

    private MessageStatistics CalculateStatistics(IEnumerable<MessageData> messages)
    {
        var messageList = messages.ToList();
        
        return new MessageStatistics
        {
            StartDate = messageList.Min(m => m.Timestamp),
            EndDate = messageList.Max(m => m.Timestamp),
            UniqueAuthors = messageList.Where(m => !string.IsNullOrEmpty(m.SenderDisplayName)).Select(m => m.SenderDisplayName).Distinct().Count(),
            MessagesWithMedia = messageList.Count(m => m.Media != null),
            ForwardedMessages = messageList.Count(m => m.ForwardInfo != null),
            TotalViews = messageList.Where(m => m.Views.HasValue).Sum(m => m.Views.Value)
        };
    }

    private static string EscapeMarkdown(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text
            .Replace("\\", "\\\\")
            .Replace("*", "\\*")
            .Replace("_", "\\_")
            .Replace("[", "\\[")
            .Replace("]", "\\]")
            .Replace("(", "\\(")
            .Replace(")", "\\)")
            .Replace("`", "\\`")
            .Replace("#", "\\#")
            .Replace("+", "\\+")
            .Replace("-", "\\-")
            .Replace(".", "\\.")
            .Replace("!", "\\!");
    }

    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return "\"\"";

        if (field.Contains("\"") || field.Contains(",") || field.Contains("\n") || field.Contains("\r"))
        {
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        }

        return field;
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private class MessageStatistics
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int UniqueAuthors { get; set; }
        public int MessagesWithMedia { get; set; }
        public int ForwardedMessages { get; set; }
        public long TotalViews { get; set; }
    }
}
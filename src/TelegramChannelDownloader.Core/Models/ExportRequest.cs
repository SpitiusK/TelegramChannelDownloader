using TelegramChannelDownloader.TelegramApi.Messages.Models;
using TelegramChannelDownloader.TelegramApi.Channels.Models;

namespace TelegramChannelDownloader.Core.Models;

/// <summary>
/// Request model for export operations
/// </summary>
public class ExportRequest
{
    /// <summary>
    /// Messages to export
    /// </summary>
    public IEnumerable<MessageData> Messages { get; set; } = new List<MessageData>();

    /// <summary>
    /// Channel information for export header
    /// </summary>
    public ChannelInfo? ChannelInfo { get; set; }

    /// <summary>
    /// Output file path
    /// </summary>
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>
    /// Export format
    /// </summary>
    public ExportFormat Format { get; set; } = ExportFormat.Markdown;

    /// <summary>
    /// Additional export options
    /// </summary>
    public ExportOptions Options { get; set; } = new();

    /// <summary>
    /// Unique identifier for this export request
    /// </summary>
    public string ExportId { get; set; } = Guid.NewGuid().ToString();
}
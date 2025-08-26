using TelegramChannelDownloader.Core.Models;

namespace TelegramChannelDownloader.Core.Services;

/// <summary>
/// Service for handling export operations in different formats
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Exports messages to Markdown format
    /// </summary>
    /// <param name="request">Export request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Export result</returns>
    Task<ExportResult> ExportToMarkdownAsync(ExportRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports messages to JSON format
    /// </summary>
    /// <param name="request">Export request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Export result</returns>
    Task<ExportResult> ExportToJsonAsync(ExportRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports messages to CSV format
    /// </summary>
    /// <param name="request">Export request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Export result</returns>
    Task<ExportResult> ExportToCsvAsync(ExportRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of supported export formats
    /// </summary>
    /// <returns>List of supported formats</returns>
    Task<List<ExportFormat>> GetSupportedFormatsAsync();

    /// <summary>
    /// Estimates the size of an export operation
    /// </summary>
    /// <param name="request">Export request</param>
    /// <returns>Estimated size in bytes</returns>
    Task<long> EstimateExportSizeAsync(ExportRequest request);

    /// <summary>
    /// Generates a safe filename for the export
    /// </summary>
    /// <param name="channelName">Channel name or title</param>
    /// <param name="exportFormat">Export format</param>
    /// <param name="includeTimestamp">Whether to include timestamp in filename</param>
    /// <returns>Safe filename</returns>
    string GenerateSafeFileName(string channelName, ExportFormat exportFormat, bool includeTimestamp = true);

    /// <summary>
    /// Validates an export request before execution
    /// </summary>
    /// <param name="request">Export request to validate</param>
    /// <returns>Validation result</returns>
    Task<ValidationResult> ValidateExportRequestAsync(ExportRequest request);
}
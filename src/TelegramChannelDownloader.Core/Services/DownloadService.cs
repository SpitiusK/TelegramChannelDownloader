using TelegramChannelDownloader.Core.Models;
using TelegramChannelDownloader.Core.Exceptions;
using TelegramChannelDownloader.TelegramApi;
using TelegramChannelDownloader.TelegramApi.Authentication.Models;
using TelegramChannelDownloader.TelegramApi.Channels.Models;
using TelegramChannelDownloader.TelegramApi.Messages.Models;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace TelegramChannelDownloader.Core.Services;

/// <summary>
/// Service for orchestrating download operations
/// </summary>
public class DownloadService : IDownloadService
{
    private readonly ITelegramApiClient _telegramClient;
    private readonly IValidationService _validationService;
    private readonly IExportService _exportService;
    private readonly ILogger<DownloadService> _logger;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeDownloads = new();
    private readonly ConcurrentDictionary<string, DownloadStatus> _downloadStatuses = new();

    public event EventHandler<DownloadStatusChangedEventArgs>? DownloadStatusChanged;

    public DownloadService(
        ITelegramApiClient telegramClient,
        IValidationService validationService,
        IExportService exportService,
        ILogger<DownloadService> logger)
    {
        _telegramClient = telegramClient ?? throw new ArgumentNullException(nameof(telegramClient));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<DownloadResult> DownloadChannelAsync(
        DownloadRequest request, 
        IProgress<Core.Models.DownloadProgressInfo>? progress = null, 
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var downloadId = request.DownloadId;
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        try
        {
            // Register the download for tracking
            _activeDownloads[downloadId] = cts;
            
            // Initialize download status
            var status = new DownloadStatus
            {
                DownloadId = downloadId,
                Phase = DownloadPhase.Initializing,
                StartTime = DateTime.UtcNow,
                CanCancel = true
            };
            await UpdateDownloadStatusAsync(downloadId, status);

            _logger.LogInformation("Starting download {DownloadId} from channel: {ChannelUrl}", 
                downloadId, request.ChannelUrl);

            // Phase 1: Validate the request
            ReportProgress(progress, Core.Models.DownloadProgressInfo.ForPhase(downloadId, DownloadPhase.Validating, "Validating download request"));
            
            var validationResult = await ValidateDownloadRequestAsync(request);
            if (!validationResult.IsValid)
            {
                throw new ValidationException($"Download request validation failed: {validationResult.ErrorMessage}");
            }

            // Phase 2: Validate output directory and check disk space
            ReportProgress(progress, Core.Models.DownloadProgressInfo.ForPhase(downloadId, DownloadPhase.Validating, "Validating output directory"));
            
            var estimatedSize = await EstimateDownloadSizeAsync(request);
            var dirValidation = _validationService.ValidateDirectorySpace(request.OutputDirectory, estimatedSize);
            if (!dirValidation.IsValid)
            {
                throw new DownloadException($"Output directory validation failed: {dirValidation.ErrorMessage}");
            }

            // Ensure directory exists
            if (!Directory.Exists(request.OutputDirectory))
            {
                Directory.CreateDirectory(request.OutputDirectory);
                _logger.LogInformation("Created output directory: {Directory}", request.OutputDirectory);
            }

            // Phase 3: Pre-download authentication verification
            ReportProgress(progress, Core.Models.DownloadProgressInfo.ForPhase(downloadId, DownloadPhase.Validating, "Verifying authentication state"));
            
            // Verify authentication state before attempting channel operations
            if (!_telegramClient.IsConnected || _telegramClient.CurrentAuthStatus?.State != AuthenticationState.Authenticated)
            {
                _logger.LogError("Authentication lost before download attempt for {DownloadId}", downloadId);
                throw new DownloadException("Telegram authentication has expired. Please re-authenticate and try again.");
            }

            // Test connection stability
            var connectionTest = await _telegramClient.TestConnectionAsync();
            if (!connectionTest)
            {
                _logger.LogError("Connection test failed before download for {DownloadId}", downloadId);
                throw new DownloadException("Connection to Telegram servers failed. Please check your internet connection and authentication status.");
            }

            _logger.LogDebug("Authentication and connection verified for download {DownloadId}", downloadId);

            // Phase 4: Get channel information
            ReportProgress(progress, Core.Models.DownloadProgressInfo.ForPhase(downloadId, DownloadPhase.Validating, "Retrieving channel information"));
            
            var channelInfo = await _telegramClient.GetChannelInfoAsync(request.ChannelUrl);
            if (channelInfo == null || !channelInfo.CanDownload)
            {
                throw new DownloadException($"Cannot download from channel: {channelInfo?.ValidationMessage ?? "Unknown error"}");
            }

            // Phase 5: Count messages
            status.Phase = DownloadPhase.Counting;
            status.ChannelInfo = channelInfo;
            await UpdateDownloadStatusAsync(downloadId, status);

            var totalMessages = channelInfo.MessageCount;
            if (request.Options.MaxMessages > 0 && request.Options.MaxMessages < totalMessages)
            {
                totalMessages = request.Options.MaxMessages;
            }

            ReportProgress(progress, new Core.Models.DownloadProgressInfo
            {
                DownloadId = downloadId,
                Phase = DownloadPhase.Counting,
                TotalMessages = totalMessages,
                StatusMessage = $"Found {totalMessages:N0} messages to download",
                StartTime = status.StartTime
            });

            _logger.LogInformation("Found {MessageCount} messages in channel {ChannelName}", 
                totalMessages, channelInfo.DisplayName);

            // Phase 6: Download messages
            status.Phase = DownloadPhase.Downloading;
            status.TotalMessages = totalMessages;
            await UpdateDownloadStatusAsync(downloadId, status);

            ReportProgress(progress, new Core.Models.DownloadProgressInfo
            {
                DownloadId = downloadId,
                Phase = DownloadPhase.Downloading,
                TotalMessages = totalMessages,
                StatusMessage = "Starting message download",
                StartTime = status.StartTime,
                CanCancel = true
            });

            // Create progress reporter that transforms TelegramApi.DownloadProgressInfo to Core.DownloadProgressInfo
            var telegramApiProgress = CreateTelegramApiProgressReporter(downloadId, progress, status.StartTime);

            var messages = await _telegramClient.DownloadChannelMessagesAsync(
                channelInfo, telegramApiProgress, cts.Token);

            if (cts.Token.IsCancellationRequested)
            {
                status.Phase = DownloadPhase.Cancelled;
                await UpdateDownloadStatusAsync(downloadId, status);
                return new DownloadResult
                {
                    DownloadId = downloadId,
                    IsSuccess = false,
                    ErrorMessage = "Download was cancelled by user",
                    Statistics = new DownloadStatistics
                    {
                        EndTime = DateTime.UtcNow
                    }
                };
            }

            _logger.LogInformation("Downloaded {MessageCount} messages from {ChannelName}", 
                messages.Count, channelInfo.DisplayName);

            // Phase 7: Export to file
            status.Phase = DownloadPhase.Exporting;
            status.DownloadedMessages = messages.Count;
            await UpdateDownloadStatusAsync(downloadId, status);

            ReportProgress(progress, new Core.Models.DownloadProgressInfo
            {
                DownloadId = downloadId,
                Phase = DownloadPhase.Exporting,
                TotalMessages = totalMessages,
                DownloadedMessages = messages.Count,
                StatusMessage = "Exporting messages to file",
                StartTime = status.StartTime
            });

            var fileName = string.IsNullOrWhiteSpace(request.Options.CustomFilename)
                ? _exportService.GenerateSafeFileName(channelInfo.DisplayName, request.ExportFormat)
                : request.Options.CustomFilename;

            var outputPath = Path.Combine(request.OutputDirectory, fileName);

            var exportRequest = new ExportRequest
            {
                Messages = messages,
                ChannelInfo = channelInfo,
                OutputPath = outputPath,
                Format = request.ExportFormat,
                Options = new ExportOptions
                {
                    OverwriteExisting = request.Options.OverwriteExisting,
                    IncludeMetadata = true,
                    IncludeStatistics = true
                }
            };

            var exportResult = request.ExportFormat switch
            {
                ExportFormat.Markdown => await _exportService.ExportToMarkdownAsync(exportRequest, cts.Token),
                ExportFormat.Json => await _exportService.ExportToJsonAsync(exportRequest, cts.Token),
                ExportFormat.Csv => await _exportService.ExportToCsvAsync(exportRequest, cts.Token),
                _ => throw new ExportException($"Unsupported export format: {request.ExportFormat}")
            };

            if (!exportResult.IsSuccess)
            {
                throw new ExportException($"Export failed: {exportResult.ErrorMessage}");
            }

            // Phase 8: Finalize
            status.Phase = DownloadPhase.Completed;
            status.CompletedAt = DateTime.UtcNow;
            status.CanCancel = false;
            await UpdateDownloadStatusAsync(downloadId, status);

            var totalTime = DateTime.UtcNow - status.StartTime;
            
            ReportProgress(progress, new Core.Models.DownloadProgressInfo
            {
                DownloadId = downloadId,
                Phase = DownloadPhase.Completed,
                TotalMessages = totalMessages,
                DownloadedMessages = messages.Count,
                StatusMessage = $"Download completed in {totalTime:mm\\:ss}",
                StartTime = status.StartTime,
                CanCancel = false
            });

            _logger.LogInformation("Download {DownloadId} completed successfully in {Duration}. " +
                                   "Downloaded {MessageCount} messages to {OutputPath}", 
                downloadId, totalTime, messages.Count, outputPath);

            return new DownloadResult
            {
                DownloadId = downloadId,
                IsSuccess = true,
                ChannelSummary = new ChannelSummary
                {
                    DisplayName = channelInfo.DisplayName,
                    Username = channelInfo.Username ?? "",
                    Type = channelInfo.Type.ToString(),
                    MemberCount = channelInfo.MemberCount,
                    MessageCount = channelInfo.MessageCount
                },
                MessagesDownloaded = messages.Count,
                OutputPath = outputPath,
                Duration = totalTime,
                FileSize = new FileInfo(outputPath).Length,
                Statistics = new DownloadStatistics
                {
                    StartTime = status.StartTime,
                    EndTime = DateTime.UtcNow,
                    TextMessages = messages.Count(m => m.MessageType == MessageType.Text),
                    MediaMessages = messages.Count(m => m.Media != null),
                    ForwardedMessages = messages.Count(m => m.ForwardInfo != null)
                }
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Download {DownloadId} was cancelled", downloadId);
            
            var status = new DownloadStatus
            {
                DownloadId = downloadId,
                Phase = DownloadPhase.Cancelled,
                CompletedAt = DateTime.UtcNow,
                CanCancel = false
            };
            await UpdateDownloadStatusAsync(downloadId, status);

            return new DownloadResult
            {
                DownloadId = downloadId,
                IsSuccess = false,
                ErrorMessage = "Download was cancelled",
                Statistics = new DownloadStatistics
                {
                    EndTime = DateTime.UtcNow
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Download {DownloadId} failed: {Error}", downloadId, ex.Message);
            
            var status = new DownloadStatus
            {
                DownloadId = downloadId,
                Phase = DownloadPhase.Failed,
                ErrorMessage = ex.Message,
                CompletedAt = DateTime.UtcNow,
                CanCancel = false
            };
            await UpdateDownloadStatusAsync(downloadId, status);

            return new DownloadResult
            {
                DownloadId = downloadId,
                IsSuccess = false,
                ErrorMessage = ex.Message,
                ExceptionType = ex.GetType().Name,
                Statistics = new DownloadStatistics
                {
                    EndTime = DateTime.UtcNow
                }
            };
        }
        finally
        {
            // Clean up
            _activeDownloads.TryRemove(downloadId, out _);
            cts.Dispose();
        }
    }

    /// <inheritdoc />
    public async Task<ValidationResult> ValidateDownloadRequestAsync(DownloadRequest request)
    {
        if (request == null)
            return ValidationResult.Failure("Download request cannot be null");

        // Validate channel URL
        var channelValidation = _validationService.ValidateChannelUrl(request.ChannelUrl);
        if (!channelValidation.IsValid)
            return channelValidation;

        // Validate output directory
        var directoryValidation = _validationService.ValidateDirectoryPath(request.OutputDirectory, checkWriteAccess: true);
        if (!directoryValidation.IsValid)
            return directoryValidation;

        // Validate API credentials if provided
        if (request.Credentials != null)
        {
            var credentialsValidation = _validationService.ValidateApiCredentials(request.Credentials);
            if (!credentialsValidation.IsValid)
                return credentialsValidation;
        }

        // Validate export format
        var supportedFormats = await _exportService.GetSupportedFormatsAsync();
        if (!supportedFormats.Contains(request.ExportFormat))
        {
            return ValidationResult.Failure($"Export format {request.ExportFormat} is not supported");
        }

        return ValidationResult.Success();
    }

    /// <inheritdoc />
    public async Task<ValidationResult<ChannelInfo>> ValidateChannelAsync(string channelUrl)
    {
        if (string.IsNullOrWhiteSpace(channelUrl))
            return ValidationResult<ChannelInfo>.Failure("Channel URL cannot be empty", "EMPTY_CHANNEL_URL");

        try
        {
            // First validate the URL format
            var urlValidation = _validationService.ValidateChannelUrl(channelUrl);
            if (!urlValidation.IsValid)
                return ValidationResult<ChannelInfo>.Failure(urlValidation.ErrorMessage, urlValidation.ErrorCode);

            // Try to get channel information from Telegram API
            var channelInfo = await _telegramClient.GetChannelInfoAsync(channelUrl);
            if (channelInfo == null)
                return ValidationResult<ChannelInfo>.Failure("Channel not found or access denied", "CHANNEL_NOT_FOUND");

            _logger.LogInformation("Successfully validated channel {ChannelUrl} - {DisplayName}", 
                channelUrl, channelInfo.DisplayName);

            return ValidationResult<ChannelInfo>.Success(channelInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate channel {ChannelUrl}: {Error}", channelUrl, ex.Message);
            return ValidationResult<ChannelInfo>.Failure($"Channel validation failed: {ex.Message}", "VALIDATION_ERROR");
        }
    }

    /// <inheritdoc />
    public async Task CancelDownloadAsync(string downloadId)
    {
        if (string.IsNullOrWhiteSpace(downloadId))
            throw new ArgumentException("Download ID cannot be null or empty", nameof(downloadId));

        if (_activeDownloads.TryGetValue(downloadId, out var cts))
        {
            _logger.LogInformation("Cancelling download {DownloadId}", downloadId);
            cts.Cancel();
            
            // Update status
            if (_downloadStatuses.TryGetValue(downloadId, out var status))
            {
                status.Phase = DownloadPhase.Cancelled;
                status.CompletedAt = DateTime.UtcNow;
                status.CanCancel = false;
                await UpdateDownloadStatusAsync(downloadId, status);
            }
        }
        else
        {
            _logger.LogWarning("Attempted to cancel non-existent or completed download {DownloadId}", downloadId);
        }
    }

    /// <inheritdoc />
    public async Task<DownloadStatus> GetDownloadStatusAsync(string downloadId)
    {
        if (string.IsNullOrWhiteSpace(downloadId))
            throw new ArgumentException("Download ID cannot be null or empty", nameof(downloadId));

        if (_downloadStatuses.TryGetValue(downloadId, out var status))
        {
            return status;
        }

        // Return a default "not found" status
        return new DownloadStatus
        {
            DownloadId = downloadId,
            Phase = DownloadPhase.Failed,
            ErrorMessage = "Download not found"
        };
    }

    /// <inheritdoc />
    public async Task<long> EstimateDownloadSizeAsync(DownloadRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        try
        {
            // Get channel information to estimate size
            var channelInfo = await _telegramClient.GetChannelInfoAsync(request.ChannelUrl);
            if (channelInfo == null)
            {
                return 0;
            }

            var messageCount = channelInfo.MessageCount;
            if (request.Options.MaxMessages > 0 && request.Options.MaxMessages < messageCount)
            {
                messageCount = request.Options.MaxMessages;
            }

            // Rough estimate: average 200 bytes per message for text
            // This is conservative and doesn't include media files
            const long averageBytesPerMessage = 200;
            var estimatedSize = messageCount * averageBytesPerMessage;

            // Add overhead for export format
            var formatMultiplier = request.ExportFormat switch
            {
                ExportFormat.Json => 1.5, // JSON has more metadata
                ExportFormat.Markdown => 1.2, // Markdown has formatting
                ExportFormat.Csv => 0.8, // CSV is more compact
                _ => 1.0
            };

            return (long)(estimatedSize * formatMultiplier);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to estimate download size for channel {ChannelUrl}", request.ChannelUrl);
            return 0; // Return 0 if estimation fails
        }
    }

    private IProgress<TelegramApi.Messages.Models.DownloadProgressInfo> CreateTelegramApiProgressReporter(
        string downloadId, 
        IProgress<Core.Models.DownloadProgressInfo>? originalProgress,
        DateTime startTime)
    {        
        return new Progress<TelegramApi.Messages.Models.DownloadProgressInfo>(telegramApiProgress =>
        {
            var coreProgress = new Core.Models.DownloadProgressInfo
            {
                DownloadId = downloadId,
                Phase = DownloadPhase.Downloading,
                TotalMessages = telegramApiProgress.TotalMessages,
                DownloadedMessages = telegramApiProgress.DownloadedMessages,
                MessagesPerSecond = telegramApiProgress.MessagesPerSecond,
                EstimatedTimeRemaining = telegramApiProgress.EstimatedTimeRemaining,
                CurrentMessage = telegramApiProgress.CurrentMessage,
                StartTime = startTime,
                CurrentTime = DateTime.UtcNow,
                ErrorMessage = telegramApiProgress.ErrorMessage
            };

            originalProgress?.Report(coreProgress);
        });
    }

    private void ReportProgress(IProgress<Core.Models.DownloadProgressInfo>? progress, Core.Models.DownloadProgressInfo progressInfo)
    {
        progress?.Report(progressInfo);
    }

    private async Task UpdateDownloadStatusAsync(string downloadId, DownloadStatus newStatus)
    {
        var previousStatus = _downloadStatuses.GetValueOrDefault(downloadId);
        _downloadStatuses[downloadId] = newStatus;

        // Raise status changed event
        if (previousStatus != null)
        {
            DownloadStatusChanged?.Invoke(this, new DownloadStatusChangedEventArgs
            {
                DownloadId = downloadId,
                PreviousStatus = previousStatus,
                CurrentStatus = newStatus
            });
        }
    }
}
using TelegramChannelDownloader.Core.Models;
using TelegramChannelDownloader.Core.Exceptions;
using TelegramChannelDownloader.DataBase.Entities;
using TelegramChannelDownloader.DataBase.Repositories;
using TelegramChannelDownloader.TelegramApi;
using TelegramChannelDownloader.TelegramApi.Authentication.Models;
using TelegramChannelDownloader.TelegramApi.Channels.Models;
using TelegramChannelDownloader.TelegramApi.Messages.Models;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace TelegramChannelDownloader.Core.Services;

/// <summary>
/// Service for orchestrating download operations with database storage
/// Enhanced to store messages in PostgreSQL database for improved performance and data management
/// </summary>
public class DownloadService : IDownloadService
{
    private readonly ITelegramApiClient _telegramClient;
    private readonly IValidationService _validationService;
    private readonly IExportService _exportService;
    private readonly IMessageRepository _messageRepository;
    private readonly IMessageMappingService _mappingService;
    private readonly ILogger<DownloadService> _logger;
    
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeDownloads = new();
    private readonly ConcurrentDictionary<string, DownloadStatus> _downloadStatuses = new();

    // Batch processing configuration
    private const int BatchSize = 1000; // Process messages in batches of 1000
    private const int BulkInsertThreshold = 100; // Use bulk insert for batches larger than 100

    public event EventHandler<DownloadStatusChangedEventArgs>? DownloadStatusChanged;

    public DownloadService(
        ITelegramApiClient telegramClient,
        IValidationService validationService,
        IExportService exportService,
        IMessageRepository messageRepository,
        IMessageMappingService mappingService,
        ILogger<DownloadService> logger)
    {
        _telegramClient = telegramClient ?? throw new ArgumentNullException(nameof(telegramClient));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
        _mappingService = mappingService ?? throw new ArgumentNullException(nameof(mappingService));
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
        DownloadSession? session = null;
        
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

            _logger.LogInformation("Starting database-enabled download {DownloadId} from channel: {ChannelUrl}", 
                downloadId, request.ChannelUrl);

            // Phase 1: Validate the request
            ReportProgress(progress, Core.Models.DownloadProgressInfo.ForPhase(downloadId, DownloadPhase.Validating, "Validating download request"));
            
            var validationResult = await ValidateDownloadRequestAsync(request);
            if (!validationResult.IsValid)
            {
                throw new ValidationException($"Download request validation failed: {validationResult.ErrorMessage}");
            }

            // Phase 2: Validate output directory for export (still needed for export files)
            ReportProgress(progress, Core.Models.DownloadProgressInfo.ForPhase(downloadId, DownloadPhase.Validating, "Validating output directory"));
            
            var estimatedSize = await EstimateDownloadSizeAsync(request);
            var dirValidation = _validationService.ValidateDirectorySpace(request.OutputDirectory, estimatedSize);
            if (!dirValidation.IsValid)
            {
                throw new DownloadException($"Output directory validation failed: {dirValidation.ErrorMessage}");
            }

            // Ensure export directory exists
            if (!Directory.Exists(request.OutputDirectory))
            {
                Directory.CreateDirectory(request.OutputDirectory);
                _logger.LogInformation("Created output directory: {Directory}", request.OutputDirectory);
            }

            // Phase 3: Authentication verification
            ReportProgress(progress, Core.Models.DownloadProgressInfo.ForPhase(downloadId, DownloadPhase.Validating, "Verifying authentication state"));
            
            if (!_telegramClient.IsConnected || _telegramClient.CurrentAuthStatus?.State != AuthenticationState.Authenticated)
            {
                _logger.LogError("Authentication lost before download attempt for {DownloadId}", downloadId);
                throw new DownloadException("Telegram authentication has expired. Please re-authenticate and try again.");
            }

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

            // Phase 5: Create database session
            ReportProgress(progress, Core.Models.DownloadProgressInfo.ForPhase(downloadId, DownloadPhase.Initializing, "Creating download session"));
            
            session = _mappingService.CreateDownloadSession(channelInfo, request.ExportFormat.ToString());
            session = await _messageRepository.CreateSessionAsync(session, cancellationToken);
            
            _logger.LogInformation("Created database session {SessionId} for download {DownloadId}", 
                session.Id, downloadId);

            // Phase 6: Count messages
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

            // Phase 7: Download messages with database storage
            status.Phase = DownloadPhase.Downloading;
            status.TotalMessages = totalMessages;
            await UpdateDownloadStatusAsync(downloadId, status);

            ReportProgress(progress, new Core.Models.DownloadProgressInfo
            {
                DownloadId = downloadId,
                Phase = DownloadPhase.Downloading,
                TotalMessages = totalMessages,
                StatusMessage = "Starting message download and database storage",
                StartTime = status.StartTime,
                CanCancel = true
            });

            // Download messages in batches and store in database
            var downloadedMessageCount = await DownloadMessagesWithDatabaseStorageAsync(
                channelInfo, session.Id, totalMessages, downloadId, progress, status.StartTime, cts.Token);

            if (cts.Token.IsCancellationRequested)
            {
                await _messageRepository.UpdateSessionStatusAsync(session.Id, DownloadSessionStatus.Cancelled, 
                    downloadedMessageCount, downloadedMessageCount, "Download cancelled by user", cancellationToken);
                
                status.Phase = DownloadPhase.Cancelled;
                await UpdateDownloadStatusAsync(downloadId, status);
                
                return new DownloadResult
                {
                    DownloadId = downloadId,
                    IsSuccess = false,
                    ErrorMessage = "Download was cancelled by user",
                    Statistics = new DownloadStatistics { EndTime = DateTime.UtcNow }
                };
            }

            _logger.LogInformation("Downloaded and stored {MessageCount} messages from {ChannelName} in database", 
                downloadedMessageCount, channelInfo.DisplayName);

            // Phase 8: Export from database to file
            status.Phase = DownloadPhase.Exporting;
            status.DownloadedMessages = downloadedMessageCount;
            await UpdateDownloadStatusAsync(downloadId, status);

            ReportProgress(progress, new Core.Models.DownloadProgressInfo
            {
                DownloadId = downloadId,
                Phase = DownloadPhase.Exporting,
                TotalMessages = totalMessages,
                DownloadedMessages = downloadedMessageCount,
                StatusMessage = "Exporting messages from database to file",
                StartTime = status.StartTime
            });

            var exportPath = await ExportSessionFromDatabaseAsync(session, request, cts.Token);

            // Phase 9: Complete session
            await _messageRepository.CompleteSessionAsync(session.Id, downloadedMessageCount, 
                exportPath, request.ExportFormat.ToString(), cancellationToken);

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
                DownloadedMessages = downloadedMessageCount,
                StatusMessage = $"Download completed in {totalTime:mm\\:ss}",
                StartTime = status.StartTime,
                CanCancel = false
            });

            _logger.LogInformation("Download {DownloadId} completed successfully in {Duration}. " +
                                   "Downloaded {MessageCount} messages, stored in database session {SessionId}, exported to {ExportPath}", 
                downloadId, totalTime, downloadedMessageCount, session.Id, exportPath);

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
                MessagesDownloaded = downloadedMessageCount,
                OutputPath = exportPath,
                Duration = totalTime,
                FileSize = File.Exists(exportPath) ? new FileInfo(exportPath).Length : 0,
                Statistics = new DownloadStatistics
                {
                    StartTime = status.StartTime,
                    EndTime = DateTime.UtcNow,
                    // We'll calculate these from database in the future
                    TextMessages = downloadedMessageCount, // Simplified for now
                    MediaMessages = 0,
                    ForwardedMessages = 0
                }
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Download {DownloadId} was cancelled", downloadId);
            
            if (session != null)
            {
                await _messageRepository.UpdateSessionStatusAsync(session.Id, DownloadSessionStatus.Cancelled, 
                    cancellationToken: cancellationToken);
            }
            
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
                Statistics = new DownloadStatistics { EndTime = DateTime.UtcNow }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Download {DownloadId} failed: {Error}", downloadId, ex.Message);
            
            if (session != null)
            {
                await _messageRepository.UpdateSessionStatusAsync(session.Id, DownloadSessionStatus.Failed, 
                    errorMessage: ex.Message, cancellationToken: cancellationToken);
            }
            
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
                Statistics = new DownloadStatistics { EndTime = DateTime.UtcNow }
            };
        }
        finally
        {
            // Clean up
            _activeDownloads.TryRemove(downloadId, out _);
            cts.Dispose();
        }
    }

    /// <summary>
    /// Download messages in batches and store directly in database for optimal performance
    /// </summary>
    private async Task<int> DownloadMessagesWithDatabaseStorageAsync(
        ChannelInfo channelInfo,
        Guid sessionId,
        int totalMessages,
        string downloadId,
        IProgress<Core.Models.DownloadProgressInfo>? progress,
        DateTime startTime,
        CancellationToken cancellationToken)
    {
        var downloadedCount = 0;
        var batch = new List<TelegramApi.Messages.Models.MessageData>();

        try
        {
            // Create progress reporter that handles database storage
            var telegramApiProgress = new Progress<TelegramApi.Messages.Models.DownloadProgressInfo>(async telegramProgress =>
            {
                try
                {
                    // Store the current message in batch
                    if (telegramProgress.CurrentMessage != null)
                    {
                        batch.Add(telegramProgress.CurrentMessage);
                    }

                    // Process batch when it reaches the configured size or when download is complete
                    if (batch.Count >= BatchSize || 
                        (telegramProgress.DownloadedMessages >= totalMessages && batch.Count > 0))
                    {
                        await ProcessMessageBatchAsync(batch, sessionId, cancellationToken);
                        downloadedCount += batch.Count;
                        
                        _logger.LogDebug("Processed batch of {BatchSize} messages. Total processed: {TotalProcessed}",
                            batch.Count, downloadedCount);
                        
                        batch.Clear();
                    }

                    // Update progress with database context
                    var coreProgress = new Core.Models.DownloadProgressInfo
                    {
                        DownloadId = downloadId,
                        Phase = DownloadPhase.Downloading,
                        TotalMessages = telegramProgress.TotalMessages,
                        DownloadedMessages = downloadedCount,
                        MessagesPerSecond = telegramProgress.MessagesPerSecond,
                        EstimatedTimeRemaining = telegramProgress.EstimatedTimeRemaining,
                        StatusMessage = $"Downloaded {downloadedCount:N0} messages, storing in database...",
                        StartTime = startTime,
                        CurrentTime = DateTime.UtcNow
                    };

                    progress?.Report(coreProgress);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message batch during download {DownloadId}", downloadId);
                }
            });

            // Start the actual download from Telegram API
            await _telegramClient.DownloadChannelMessagesAsync(channelInfo, telegramApiProgress, cancellationToken);

            // Process any remaining messages in the final batch
            if (batch.Count > 0 && !cancellationToken.IsCancellationRequested)
            {
                await ProcessMessageBatchAsync(batch, sessionId, cancellationToken);
                downloadedCount += batch.Count;
                
                _logger.LogInformation("Processed final batch of {BatchSize} messages. Total processed: {TotalProcessed}",
                    batch.Count, downloadedCount);
            }

            return downloadedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download messages with database storage for session {SessionId}", sessionId);
            throw;
        }
    }

    /// <summary>
    /// Process a batch of messages by converting and storing them in the database
    /// </summary>
    private async Task ProcessMessageBatchAsync(List<TelegramApi.Messages.Models.MessageData> batch, 
        Guid sessionId, CancellationToken cancellationToken)
    {
        if (!batch.Any()) return;

        try
        {
            // Convert TelegramApi messages to database entities
            var entities = _mappingService.ConvertToEntities(batch, sessionId);
            
            if (!entities.Any())
            {
                _logger.LogWarning("No valid entities converted from batch of {BatchSize} messages", batch.Count);
                return;
            }

            // Use bulk insert for better performance with large batches
            if (entities.Count >= BulkInsertThreshold)
            {
                await _messageRepository.BulkInsertMessagesAsync(entities, cancellationToken);
                _logger.LogDebug("Bulk inserted {EntityCount} messages for session {SessionId}", 
                    entities.Count, sessionId);
            }
            else
            {
                await _messageRepository.AddMessagesBatchAsync(entities, cancellationToken);
                _logger.LogDebug("Batch inserted {EntityCount} messages for session {SessionId}", 
                    entities.Count, sessionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process message batch of {BatchSize} messages for session {SessionId}", 
                batch.Count, sessionId);
            throw;
        }
    }

    /// <summary>
    /// Export session data from database to file
    /// </summary>
    private async Task<string> ExportSessionFromDatabaseAsync(DownloadSession session, 
        DownloadRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Get messages from database in manageable chunks
            var allMessages = new List<TelegramApi.Messages.Models.MessageData>();
            var skip = 0;
            const int pageSize = 1000;
            
            while (true)
            {
                var entities = await _messageRepository.GetSessionMessagesAsync(session.Id, skip, pageSize, cancellationToken);
                if (!entities.Any()) break;

                // Convert database entities back to MessageData for export
                var messageDataBatch = entities.Select(_mappingService.ConvertToMessageData).ToList();
                allMessages.AddRange(messageDataBatch);
                
                skip += pageSize;
                
                _logger.LogDebug("Loaded {BatchSize} messages from database for export. Total loaded: {TotalLoaded}",
                    messageDataBatch.Count, allMessages.Count);
            }

            // Generate export filename
            var fileName = string.IsNullOrWhiteSpace(request.Options.CustomFilename)
                ? _exportService.GenerateSafeFileName(session.ChannelTitle, request.ExportFormat)
                : request.Options.CustomFilename;

            var outputPath = Path.Combine(request.OutputDirectory, fileName);

            // Create channel info for export
            var channelInfoForExport = new ChannelInfo
            {
                Id = session.ChannelId,
                Title = session.ChannelTitle,
                Username = session.ChannelUsername,
                MessageCount = allMessages.Count,
                Type = ChannelType.Channel // Simplified
            };

            // Create export request
            var exportRequest = new ExportRequest
            {
                Messages = allMessages,
                ChannelInfo = channelInfoForExport,
                OutputPath = outputPath,
                Format = request.ExportFormat,
                Options = new ExportOptions
                {
                    OverwriteExisting = request.Options.OverwriteExisting,
                    IncludeMetadata = true,
                    IncludeStatistics = true
                }
            };

            // Perform export
            var exportResult = request.ExportFormat switch
            {
                ExportFormat.Markdown => await _exportService.ExportToMarkdownAsync(exportRequest, cancellationToken),
                ExportFormat.Json => await _exportService.ExportToJsonAsync(exportRequest, cancellationToken),
                ExportFormat.Csv => await _exportService.ExportToCsvAsync(exportRequest, cancellationToken),
                _ => throw new ExportException($"Unsupported export format: {request.ExportFormat}")
            };

            if (!exportResult.IsSuccess)
            {
                throw new ExportException($"Export failed: {exportResult.ErrorMessage}");
            }

            _logger.LogInformation("Successfully exported {MessageCount} messages from session {SessionId} to {OutputPath}",
                allMessages.Count, session.Id, outputPath);

            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export session {SessionId} from database", session.Id);
            throw;
        }
    }

    #region Existing Interface Methods (unchanged)

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

            // Conservative estimate: 200 bytes per message for text + database overhead
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

    #endregion

    #region Private Helper Methods

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

    #endregion
}
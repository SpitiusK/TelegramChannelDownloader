using System.IO;
using System.Windows.Input;
using TelegramChannelDownloader.Core.Models;
using TelegramChannelDownloader.Core.Services;
using TelegramChannelDownloader.Desktop.Commands;
using TelegramChannelDownloader.Desktop.Services;
using TelegramChannelDownloader.Desktop.Utils;
using TelegramChannelDownloader.TelegramApi.Channels.Models;
using TelegramChannelDownloader.TelegramApi.Messages.Models;
using CoreProgressInfo = TelegramChannelDownloader.Core.Models.DownloadProgressInfo;

namespace TelegramChannelDownloader.Desktop.ViewModels;

public class DownloadViewModel : ObservableObject
{
    private readonly IDownloadService _downloadService;
    private readonly IValidationService _validation;
    private readonly IUIService _uiService;
    private readonly IExportService _exportService;

    private string _channelUrl = string.Empty;
    private ChannelInfo? _channelInfo;
    private bool _isValidatingChannel;
    private string _channelValidationMessage = string.Empty;
    private string _outputDirectory = string.Empty;
    private ExportFormat _exportFormat = ExportFormat.Markdown;
    private int _downloadProgress;
    private int _totalMessages;
    private int _downloadedMessages;
    private bool _isDownloading;
    private TimeSpan? _estimatedTimeRemaining;
    private double _downloadSpeed;
    private MessageData? _currentMessage;
    private bool _canCancelDownload;
    private string _downloadPhase = string.Empty;
    private bool _showCompletionNotification;
    private string _completionMessage = string.Empty;
    private CancellationTokenSource? _downloadCancellationTokenSource;
    private DateTime _downloadStartTime;

    public DownloadViewModel(IDownloadService downloadService, IValidationService validation, 
        IUIService uiService, IExportService exportService)
    {
        _downloadService = downloadService ?? throw new ArgumentNullException(nameof(downloadService));
        _validation = validation ?? throw new ArgumentNullException(nameof(validation));
        _uiService = uiService ?? throw new ArgumentNullException(nameof(uiService));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));

        // Initialize commands
        ValidateChannelCommand = new AsyncRelayCommand(ExecuteValidateChannelAsync, CanExecuteValidateChannel);
        StartDownloadCommand = new AsyncRelayCommand(ExecuteStartDownloadAsync, CanExecuteStartDownload);
        CancelDownloadCommand = new AsyncRelayCommand(ExecuteCancelDownloadAsync, CanExecuteCancelDownload);
        BrowseDirectoryCommand = new AsyncRelayCommand(ExecuteBrowseDirectoryAsync);
        OpenOutputDirectoryCommand = new AsyncRelayCommand(ExecuteOpenOutputDirectoryAsync, CanExecuteOpenOutputDirectory);
        DismissNotificationCommand = new RelayCommand(ExecuteDismissNotification);

        // Set default output directory
        OutputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TelegramDownloads");
    }

    #region Properties

    public string ChannelUrl
    {
        get => _channelUrl;
        set
        {
            if (SetProperty(ref _channelUrl, value))
            {
                OnPropertyChanged(nameof(IsChannelUrlValid));
                OnPropertyChanged(nameof(ChannelUrlValidationMessage));
                
                // Trigger validation when URL changes and is valid
                if (IsChannelUrlValid && !string.IsNullOrWhiteSpace(value))
                {
                    _ = ExecuteValidateChannelAsync();
                }
                else
                {
                    ChannelInfo = null;
                    ChannelValidationMessage = string.Empty;
                }
            }
        }
    }

    public bool IsChannelUrlValid
    {
        get
        {
            var result = _validation.ValidateChannelUrl(ChannelUrl);
            return result.IsValid;
        }
    }

    public string ChannelUrlValidationMessage
    {
        get
        {
            if (string.IsNullOrEmpty(ChannelUrl)) return string.Empty;
            var result = _validation.ValidateChannelUrl(ChannelUrl);
            return result.IsValid ? string.Empty : result.ErrorMessage;
        }
    }

    public ChannelInfo? ChannelInfo
    {
        get => _channelInfo;
        set
        {
            SetProperty(ref _channelInfo, value);
            OnPropertyChanged(nameof(HasValidChannel));
            OnPropertyChanged(nameof(ChannelSummary));
        }
    }

    public bool IsValidatingChannel
    {
        get => _isValidatingChannel;
        set => SetProperty(ref _isValidatingChannel, value);
    }

    public string ChannelValidationMessage
    {
        get => _channelValidationMessage;
        set => SetProperty(ref _channelValidationMessage, value);
    }

    public bool HasValidChannel => ChannelInfo?.CanDownload == true;

    public string ChannelSummary
    {
        get
        {
            if (ChannelInfo == null) return string.Empty;
            if (!string.IsNullOrWhiteSpace(ChannelInfo.ErrorMessage)) return ChannelInfo.ErrorMessage;
            return ChannelInfo.Summary;
        }
    }

    public string OutputDirectory
    {
        get => _outputDirectory;
        set
        {
            if (SetProperty(ref _outputDirectory, value))
            {
                OnPropertyChanged(nameof(IsOutputDirectoryValid));
                OnPropertyChanged(nameof(OutputDirectoryValidationMessage));
            }
        }
    }

    public bool IsOutputDirectoryValid
    {
        get
        {
            var result = _validation.ValidateOutputDirectory(OutputDirectory);
            return result.IsValid;
        }
    }

    public string OutputDirectoryValidationMessage
    {
        get
        {
            if (string.IsNullOrEmpty(OutputDirectory)) return string.Empty;
            var result = _validation.ValidateOutputDirectory(OutputDirectory);
            return result.IsValid ? string.Empty : result.ErrorMessage;
        }
    }

    public ExportFormat ExportFormat
    {
        get => _exportFormat;
        set => SetProperty(ref _exportFormat, value);
    }

    public IEnumerable<ExportFormat> AvailableExportFormats => 
        Enum.GetValues<ExportFormat>();

    public int DownloadProgress
    {
        get => _downloadProgress;
        set => SetProperty(ref _downloadProgress, value);
    }

    public int TotalMessages
    {
        get => _totalMessages;
        set => SetProperty(ref _totalMessages, value);
    }

    public int DownloadedMessages
    {
        get => _downloadedMessages;
        set
        {
            SetProperty(ref _downloadedMessages, value);
            OnPropertyChanged(nameof(ProgressText));
        }
    }

    public bool IsDownloading
    {
        get => _isDownloading;
        set => SetProperty(ref _isDownloading, value);
    }

    public TimeSpan? EstimatedTimeRemaining
    {
        get => _estimatedTimeRemaining;
        set => SetProperty(ref _estimatedTimeRemaining, value);
    }

    public double DownloadSpeed
    {
        get => _downloadSpeed;
        set => SetProperty(ref _downloadSpeed, value);
    }

    public MessageData? CurrentMessage
    {
        get => _currentMessage;
        set => SetProperty(ref _currentMessage, value);
    }

    public bool CanCancelDownload
    {
        get => _canCancelDownload;
        set => SetProperty(ref _canCancelDownload, value);
    }

    public string DownloadPhase
    {
        get => _downloadPhase;
        set
        {
            SetProperty(ref _downloadPhase, value);
            OnPropertyChanged(nameof(ProgressText));
        }
    }

    public bool ShowCompletionNotification
    {
        get => _showCompletionNotification;
        set => SetProperty(ref _showCompletionNotification, value);
    }

    public string CompletionMessage
    {
        get => _completionMessage;
        set => SetProperty(ref _completionMessage, value);
    }

    public string ProgressText
    {
        get
        {
            if (!IsDownloading || TotalMessages == 0)
                return "Ready to download";
            
            if (!string.IsNullOrWhiteSpace(DownloadPhase))
                return $"{DownloadPhase}: {DownloadedMessages:N0}/{TotalMessages:N0}";
            
            return $"Downloaded: {DownloadedMessages:N0}/{TotalMessages:N0}";
        }
    }

    #endregion

    #region Commands

    public ICommand ValidateChannelCommand { get; }
    public ICommand StartDownloadCommand { get; }
    public ICommand CancelDownloadCommand { get; }
    public ICommand BrowseDirectoryCommand { get; }
    public ICommand OpenOutputDirectoryCommand { get; }
    public ICommand DismissNotificationCommand { get; }

    #endregion

    #region Events

    public event EventHandler<string>? LogMessageRequested;

    #endregion

    #region Command Implementations

    private bool CanExecuteValidateChannel() => !IsValidatingChannel && IsChannelUrlValid;

    private async Task ExecuteValidateChannelAsync()
    {
        if (string.IsNullOrWhiteSpace(ChannelUrl)) return;

        try
        {
            IsValidatingChannel = true;
            ChannelValidationMessage = "Validating channel...";
            ChannelInfo = null;

            // Add a small delay to debounce rapid typing
            await Task.Delay(500);

            // Check if the URL has changed while we were waiting
            if (ChannelUrl != _channelUrl) return;

            var request = new DownloadRequest
            {
                ChannelUrl = ChannelUrl,
                OutputDirectory = OutputDirectory,
                ExportFormat = ExportFormat
            };

            var validationResult = await _downloadService.ValidateChannelAsync(ChannelUrl);
            
            if (!validationResult.IsValid || validationResult.Data == null)
            {
                ChannelValidationMessage = validationResult.ErrorMessage ?? "Unable to retrieve channel information";
                return;
            }

            var channelInfo = validationResult.Data;
            ChannelInfo = channelInfo;

            ChannelValidationMessage = $"Channel validated: {channelInfo.MessageCount:N0} messages available";
            LogMessageRequested?.Invoke(this, $"Channel validated: {channelInfo.DisplayName} - {channelInfo.MessageCount} messages");
        }
        catch (Exception ex)
        {
            ChannelValidationMessage = $"Validation error: {ex.Message}";
            LogMessageRequested?.Invoke(this, $"Channel validation failed: {ex.Message}");
            ChannelInfo = null;
        }
        finally
        {
            IsValidatingChannel = false;
        }
    }

    private bool CanExecuteStartDownload() => !IsDownloading && HasValidChannel && IsOutputDirectoryValid;

    private async Task ExecuteStartDownloadAsync()
    {
        try
        {
            IsDownloading = true;
            CanCancelDownload = true;
            ShowCompletionNotification = false;
            _downloadStartTime = DateTime.Now;
            _downloadCancellationTokenSource = new CancellationTokenSource();

            if (ChannelInfo == null)
            {
                LogMessageRequested?.Invoke(this, "No valid channel selected for download");
                return;
            }

            LogMessageRequested?.Invoke(this, $"Starting download from channel: {ChannelInfo.DisplayName}");
            LogMessageRequested?.Invoke(this, $"Channel type: {ChannelInfo.Type}, Members: {ChannelInfo.MemberCount:N0}");
            LogMessageRequested?.Invoke(this, $"Output directory: {OutputDirectory}");

            // Create download request
            var request = new DownloadRequest
            {
                ChannelUrl = ChannelUrl,
                OutputDirectory = OutputDirectory,
                ExportFormat = ExportFormat
            };

            // Setup progress reporting
            var progress = new Progress<CoreProgressInfo>(progressInfo =>
            {
                TotalMessages = progressInfo.TotalMessages;
                DownloadedMessages = progressInfo.DownloadedMessages;
                DownloadProgress = progressInfo.ProgressPercentage;
                
                if (progressInfo.EstimatedTimeRemaining.HasValue)
                    EstimatedTimeRemaining = progressInfo.EstimatedTimeRemaining;
                
                DownloadSpeed = progressInfo.MessagesPerSecond;
                DownloadPhase = progressInfo.Phase.GetDescription();

                if (progressInfo.DownloadedMessages > 0 && progressInfo.DownloadedMessages % 100 == 0)
                {
                    LogMessageRequested?.Invoke(this, 
                        $"Downloaded {progressInfo.DownloadedMessages:N0}/{progressInfo.TotalMessages:N0} messages " +
                        $"({progressInfo.MessagesPerSecond:F1} msg/sec)");
                }
            });

            // Start the download
            var result = await _downloadService.DownloadChannelAsync(request, progress, _downloadCancellationTokenSource.Token);

            if (_downloadCancellationTokenSource.Token.IsCancellationRequested)
            {
                LogMessageRequested?.Invoke(this, "Download was cancelled by user");
                CompletionMessage = "Download cancelled";
                ShowCompletionNotification = true;
                return;
            }

            if (result.IsSuccess)
            {
                var totalTime = DateTime.Now - _downloadStartTime;
                LogMessageRequested?.Invoke(this, $"Download completed successfully: {result.OutputPath}");
                LogMessageRequested?.Invoke(this, $"Downloaded {result.MessagesDownloaded} messages in {totalTime:mm\\:ss}");
                
                CompletionMessage = $"Successfully downloaded {result.MessagesDownloaded:N0} messages from {ChannelInfo.DisplayName}";
                ShowCompletionNotification = true;
                
                // Auto-dismiss notification after 10 seconds
                _ = Task.Delay(10000).ContinueWith(_ => ShowCompletionNotification = false);
            }
            else
            {
                LogMessageRequested?.Invoke(this, $"Download failed: {result.ErrorMessage}");
                CompletionMessage = "Download failed";
                ShowCompletionNotification = true;
                await _uiService.ShowErrorAsync("Download Failed", result.ErrorMessage ?? "An unknown error occurred during download.");
            }
        }
        catch (OperationCanceledException)
        {
            LogMessageRequested?.Invoke(this, "Download was cancelled");
            CompletionMessage = "Download cancelled";
            ShowCompletionNotification = true;
        }
        catch (Exception ex)
        {
            LogMessageRequested?.Invoke(this, $"Download failed - {ex.Message}");
            CompletionMessage = "Download failed";
            ShowCompletionNotification = true;
            await _uiService.ShowErrorAsync("Download Failed", $"An unexpected error occurred: {ex.Message}");
        }
        finally
        {
            IsDownloading = false;
            CanCancelDownload = false;
            CurrentMessage = null;
            DownloadPhase = string.Empty;
            EstimatedTimeRemaining = null;
            DownloadSpeed = 0;
            _downloadCancellationTokenSource?.Dispose();
            _downloadCancellationTokenSource = null;
        }
    }

    private bool CanExecuteCancelDownload() => IsDownloading && CanCancelDownload;

    private async Task ExecuteCancelDownloadAsync()
    {
        try
        {
            var confirmed = await _uiService.ShowConfirmationAsync("Cancel Download", 
                "Are you sure you want to cancel the download? Any progress will be lost.");
            
            if (confirmed)
            {
                LogMessageRequested?.Invoke(this, "Cancelling download...");
                _downloadCancellationTokenSource?.Cancel();
                CanCancelDownload = false;
            }
        }
        catch (Exception ex)
        {
            LogMessageRequested?.Invoke(this, $"Error cancelling download: {ex.Message}");
        }
    }

    private async Task ExecuteBrowseDirectoryAsync()
    {
        try
        {
            var selectedDirectory = await _uiService.SelectDirectoryAsync(OutputDirectory);
            if (!string.IsNullOrEmpty(selectedDirectory))
            {
                OutputDirectory = selectedDirectory;
                LogMessageRequested?.Invoke(this, $"Output directory changed to: {OutputDirectory}");
            }
        }
        catch (Exception ex)
        {
            LogMessageRequested?.Invoke(this, $"Failed to open directory browser - {ex.Message}");
        }
    }

    private bool CanExecuteOpenOutputDirectory() => Directory.Exists(OutputDirectory);

    private async Task ExecuteOpenOutputDirectoryAsync()
    {
        try
        {
            await _uiService.OpenFileOrDirectoryAsync(OutputDirectory);
        }
        catch (Exception ex)
        {
            LogMessageRequested?.Invoke(this, $"Failed to open output directory - {ex.Message}");
        }
    }

    private void ExecuteDismissNotification()
    {
        ShowCompletionNotification = false;
        CompletionMessage = string.Empty;
    }

    #endregion

    #region Public Methods

    public void UpdateAuthenticationStatus(bool isAuthenticated)
    {
        // Re-validate channel when authentication state changes
        if (isAuthenticated && !string.IsNullOrWhiteSpace(ChannelUrl))
        {
            _ = ExecuteValidateChannelAsync();
        }
        else if (!isAuthenticated)
        {
            ChannelInfo = null;
            ChannelValidationMessage = string.Empty;
        }
    }

    #endregion
}
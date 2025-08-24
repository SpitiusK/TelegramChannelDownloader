using TelegramChannelDownloader.Utils;
using TelegramChannelDownloader.Commands;
using TelegramChannelDownloader.Services;
using TelegramChannelDownloader.Models;
using System.Windows.Input;
using System.Text;
using System.Windows.Threading;
using System.Windows;
using System.IO;
using System.Collections.ObjectModel;
using System.Linq;

namespace TelegramChannelDownloader.ViewModels;

public class MainViewModel : ObservableObject
{
    private readonly ITelegramService _telegramService;
    private readonly Dispatcher _dispatcher;
    
    private string _apiId = string.Empty;
    private string _apiHash = string.Empty;
    private string _phoneNumber = string.Empty;
    private string _verificationCode = string.Empty;
    private string _twoFactorCode = string.Empty;
    private string _channelUrl = string.Empty;
    private ChannelInfo? _channelInfo;
    private bool _isValidatingChannel;
    private string _channelValidationMessage = string.Empty;
    private string _outputDirectory = string.Empty;
    private string _connectionStatus = "Not connected";
    private bool _isConnected;
    private bool _isConnecting;
    private bool _isTwoFactorRequired;
    private bool _isPhoneNumberRequired;
    private bool _isVerificationCodeRequired;
    private AuthenticationState _authenticationState = AuthenticationState.Disconnected;
    private int _downloadProgress;
    private int _totalMessages;
    private int _downloadedMessages;
    private bool _isDownloading;
    private string _logOutput = string.Empty;
    private readonly ObservableCollection<LogEntry> _logEntries = new();
    private const int MaxLogEntries = 100;
    private bool? _lastSubmitPhoneResult;
    
    // Enhanced progress tracking properties
    private TimeSpan? _estimatedTimeRemaining;
    private double _downloadSpeed;
    private MessageData? _currentMessage;
    private CancellationTokenSource? _downloadCancellationTokenSource;
    private bool _canCancelDownload;
    private DateTime _downloadStartTime;
    private string _downloadPhase = string.Empty;
    private bool _showCompletionNotification;
    private string _completionMessage = string.Empty;

    public MainViewModel(ITelegramService telegramService)
    {
        _telegramService = telegramService ?? throw new ArgumentNullException(nameof(telegramService));
        _dispatcher = System.Windows.Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        
        // Subscribe to authentication status changes
        _telegramService.AuthenticationStatusChanged += OnAuthenticationStatusChanged;
        
        ConnectCommand = new AsyncRelayCommand(ExecuteConnectAsync, CanExecuteConnect);
        SubmitPhoneCommand = new AsyncRelayCommand(ExecuteSubmitPhoneAsync, CanExecuteSubmitPhone);
        SubmitCodeCommand = new AsyncRelayCommand(ExecuteSubmitCodeAsync, CanExecuteSubmitCode);
        SubmitTwoFactorCommand = new AsyncRelayCommand(ExecuteSubmitTwoFactorAsync, CanExecuteSubmitTwoFactor);
        DownloadCommand = new AsyncRelayCommand(ExecuteDownloadAsync, CanExecuteDownload);
        CancelDownloadCommand = new AsyncRelayCommand(ExecuteCancelDownloadAsync, CanExecuteCancelDownload);
        BrowseDirectoryCommand = new RelayCommand(ExecuteBrowseDirectory);
        ClearLogCommand = new RelayCommand(ExecuteClearLog);
        DismissNotificationCommand = new RelayCommand(ExecuteDismissNotification);
        
        // Set default output directory
        OutputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TelegramDownloads");
        
        // Add initial log message
        AddLogMessage("Application started. Please enter your API credentials to connect to Telegram.", LogLevel.Info);
    }

    #region Properties

    public string ApiId
    {
        get => _apiId;
        set
        {
            SetProperty(ref _apiId, value);
            OnPropertyChanged(nameof(IsApiIdValid));
            OnPropertyChanged(nameof(ApiIdValidationMessage));
        }
    }

    public bool IsApiIdValid => ValidationHelper.IsValidApiId(ApiId);
    public string ApiIdValidationMessage => IsApiIdValid || string.IsNullOrEmpty(ApiId) ? 
        string.Empty : ValidationHelper.GetValidationErrorMessage("apiid", ApiId);

    public string ApiHash
    {
        get => _apiHash;
        set
        {
            SetProperty(ref _apiHash, value);
            OnPropertyChanged(nameof(IsApiHashValid));
            OnPropertyChanged(nameof(ApiHashValidationMessage));
        }
    }

    public bool IsApiHashValid => ValidationHelper.IsValidApiHash(ApiHash);
    public string ApiHashValidationMessage => IsApiHashValid || string.IsNullOrEmpty(ApiHash) ? 
        string.Empty : ValidationHelper.GetValidationErrorMessage("apihash", ApiHash);

    public string PhoneNumber
    {
        get => _phoneNumber;
        set
        {
            SetProperty(ref _phoneNumber, value);
            OnPropertyChanged(nameof(IsPhoneNumberValid));
            OnPropertyChanged(nameof(PhoneNumberValidationMessage));
        }
    }

    public bool IsPhoneNumberValid => ValidationHelper.IsValidPhoneNumber(PhoneNumber);
    public string PhoneNumberValidationMessage => IsPhoneNumberValid || string.IsNullOrEmpty(PhoneNumber) ? 
        string.Empty : ValidationHelper.GetValidationErrorMessage("phonenumber", PhoneNumber);

    public string VerificationCode
    {
        get => _verificationCode;
        set
        {
            SetProperty(ref _verificationCode, value);
            OnPropertyChanged(nameof(IsVerificationCodeValid));
            OnPropertyChanged(nameof(VerificationCodeValidationMessage));
        }
    }

    public bool IsVerificationCodeValid => ValidationHelper.IsValidVerificationCode(VerificationCode);
    public string VerificationCodeValidationMessage => IsVerificationCodeValid || string.IsNullOrEmpty(VerificationCode) ? 
        string.Empty : ValidationHelper.GetValidationErrorMessage("verificationcode", VerificationCode);

    public string TwoFactorCode
    {
        get => _twoFactorCode;
        set
        {
            SetProperty(ref _twoFactorCode, value);
            OnPropertyChanged(nameof(IsTwoFactorCodeValid));
            OnPropertyChanged(nameof(TwoFactorCodeValidationMessage));
        }
    }

    public bool IsTwoFactorCodeValid => ValidationHelper.IsValidTwoFactorCode(TwoFactorCode);
    public string TwoFactorCodeValidationMessage => IsTwoFactorCodeValid || string.IsNullOrEmpty(TwoFactorCode) ? 
        string.Empty : ValidationHelper.GetValidationErrorMessage("twofactorcode", TwoFactorCode);

    public string ChannelUrl
    {
        get => _channelUrl;
        set
        {
            if (SetProperty(ref _channelUrl, value))
            {
                OnPropertyChanged(nameof(IsChannelUrlValid));
                OnPropertyChanged(nameof(ChannelUrlValidationMessage));
                
                // Perform real-time validation if connected and URL is not empty
                if (IsConnected && !string.IsNullOrWhiteSpace(value))
                {
                    _ = ValidateChannelAsync(value);
                }
                else
                {
                    // Clear channel info if URL is empty or not connected
                    ChannelInfo = null;
                    ChannelValidationMessage = string.Empty;
                }
            }
        }
    }

    public bool IsChannelUrlValid => ValidationHelper.IsValidChannelUrl(ChannelUrl);
    public string ChannelUrlValidationMessage => IsChannelUrlValid || string.IsNullOrEmpty(ChannelUrl) ? 
        string.Empty : ValidationHelper.GetValidationErrorMessage("channelurl", ChannelUrl);

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
            if (ChannelInfo == null)
                return string.Empty;
            
            if (!string.IsNullOrWhiteSpace(ChannelInfo.ErrorMessage))
                return ChannelInfo.ErrorMessage;
            
            return ChannelInfo.Summary;
        }
    }

    public string OutputDirectory
    {
        get => _outputDirectory;
        set
        {
            SetProperty(ref _outputDirectory, value);
            OnPropertyChanged(nameof(IsOutputDirectoryValid));
            OnPropertyChanged(nameof(OutputDirectoryValidationMessage));
        }
    }

    public bool IsOutputDirectoryValid => ValidationHelper.IsValidDirectoryPath(OutputDirectory);
    public string OutputDirectoryValidationMessage => IsOutputDirectoryValid || string.IsNullOrEmpty(OutputDirectory) ? 
        string.Empty : ValidationHelper.GetValidationErrorMessage("outputdirectory", OutputDirectory);

    public string ConnectionStatus
    {
        get => _connectionStatus;
        set => SetProperty(ref _connectionStatus, value);
    }

    public bool IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    public bool IsConnecting
    {
        get => _isConnecting;
        set => SetProperty(ref _isConnecting, value);
    }

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
        set => SetProperty(ref _downloadedMessages, value);
    }

    public bool IsDownloading
    {
        get => _isDownloading;
        set => SetProperty(ref _isDownloading, value);
    }

    public bool IsTwoFactorRequired
    {
        get => _isTwoFactorRequired;
        set => SetProperty(ref _isTwoFactorRequired, value);
    }

    public bool IsPhoneNumberRequired
    {
        get => _isPhoneNumberRequired;
        set => SetProperty(ref _isPhoneNumberRequired, value);
    }

    public bool IsVerificationCodeRequired
    {
        get => _isVerificationCodeRequired;
        set => SetProperty(ref _isVerificationCodeRequired, value);
    }

    public AuthenticationState AuthenticationState
    {
        get => _authenticationState;
        set => SetProperty(ref _authenticationState, value);
    }

    public string LogOutput
    {
        get => _logOutput;
        set => SetProperty(ref _logOutput, value);
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
        set => SetProperty(ref _downloadPhase, value);
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

    public ObservableCollection<LogEntry> LogEntries => _logEntries;

    #endregion

    #region Commands

    public ICommand ConnectCommand { get; }
    public ICommand SubmitPhoneCommand { get; }
    public ICommand SubmitCodeCommand { get; }
    public ICommand SubmitTwoFactorCommand { get; }
    public ICommand DownloadCommand { get; }
    public ICommand CancelDownloadCommand { get; }
    public ICommand BrowseDirectoryCommand { get; }
    public ICommand ClearLogCommand { get; }
    public ICommand DismissNotificationCommand { get; }

    #endregion

    #region Command Implementations

    private bool CanExecuteConnect() => !IsConnecting && !string.IsNullOrWhiteSpace(ApiId) && !string.IsNullOrWhiteSpace(ApiHash);

    private async Task ExecuteConnectAsync()
    {
        try
        {
            IsConnecting = true;
            ConnectionStatus = "Connecting...";
            AddLogMessage("Initializing connection to Telegram...", LogLevel.Info);

            if (!int.TryParse(ApiId, out var apiIdInt))
            {
                AddLogMessage("Invalid API ID format. Please enter a valid numeric API ID.", LogLevel.Error);
                return;
            }

            var credentials = new TelegramCredentials
            {
                ApiId = apiIdInt,
                ApiHash = ApiHash
            };

            await _telegramService.InitializeAsync(credentials);
            AddLogMessage("Connection initialized successfully.", LogLevel.Info);
            AddLogMessage("Ready for authentication. Please enter your phone number.", LogLevel.Info);
        }
        catch (Exception ex)
        {
            AddLogMessage($"Failed to connect - {ex.Message}", LogLevel.Error);
            ConnectionStatus = "Connection failed";
            AuthenticationState = AuthenticationState.ConnectionError;
        }
        finally
        {
            IsConnecting = false;
        }
    }

    private bool CanExecuteDownload() => IsConnected && !IsDownloading && HasValidChannel && !string.IsNullOrWhiteSpace(OutputDirectory);

    private async Task ExecuteDownloadAsync()
    {
        try
        {
            IsDownloading = true;
            CanCancelDownload = true;
            ShowCompletionNotification = false;
            _downloadStartTime = DateTime.Now;
            
            if (ChannelInfo == null)
            {
                AddLogMessage("No valid channel selected for download", LogLevel.Error);
                return;
            }

            // Create cancellation token source for this download
            _downloadCancellationTokenSource = new CancellationTokenSource();

            AddLogMessage($"Starting download from channel: {ChannelInfo.DisplayName}", LogLevel.Info);
            AddLogMessage($"Channel type: {ChannelInfo.Type}, Members: {ChannelInfo.MemberCount:N0}", LogLevel.Info);
            AddLogMessage($"Output directory: {OutputDirectory}", LogLevel.Info);
            
            // Validate output directory and check disk space
            DownloadPhase = "Validating output directory";
            var estimatedSize = EstimateDownloadSize(ChannelInfo);
            if (!ValidateOutputDirectory(OutputDirectory, estimatedSize))
            {
                AddLogMessage("Output directory validation failed. Download cancelled.", LogLevel.Error);
                return;
            }
            
            // Use actual message count from channel info
            TotalMessages = ChannelInfo.MessageCount;
            DownloadedMessages = 0;
            DownloadProgress = 0;
            DownloadPhase = "Initializing";
            AddLogMessage($"Found {TotalMessages:N0} messages to download", LogLevel.Info);
            
            // Create enhanced progress reporter
            var progress = new Progress<DownloadProgressInfo>(progressInfo =>
            {
                _dispatcher.BeginInvoke(() =>
                {
                    TotalMessages = progressInfo.TotalMessages;
                    DownloadedMessages = progressInfo.DownloadedMessages;
                    DownloadProgress = progressInfo.ProgressPercentage;
                    EstimatedTimeRemaining = progressInfo.EstimatedTimeRemaining;
                    DownloadSpeed = progressInfo.MessagesPerSecond;
                    CurrentMessage = progressInfo.CurrentMessage;
                    DownloadPhase = "Downloading messages";
                    
                    OnPropertyChanged(nameof(ProgressText));
                    
                    if (progressInfo.HasError)
                    {
                        AddLogMessage($"Download error: {progressInfo.ErrorMessage}", LogLevel.Error);
                    }
                    else if (progressInfo.DownloadedMessages > 0 && progressInfo.DownloadedMessages % 100 == 0)
                    {
                        AddLogMessage($"Downloaded {progressInfo.DownloadedMessages:N0}/{progressInfo.TotalMessages:N0} messages " +
                                    $"({progressInfo.MessagesPerSecond:F1} msg/sec)", LogLevel.Info);
                    }
                });
            });

            DownloadPhase = "Downloading messages";
            
            // Start the actual download
            var downloadedMessages = await _telegramService.DownloadChannelMessagesAsync(
                ChannelInfo, progress, _downloadCancellationTokenSource.Token);

            if (_downloadCancellationTokenSource.Token.IsCancellationRequested)
            {
                AddLogMessage("Download was cancelled by user", LogLevel.Warning);
                CompletionMessage = "Download cancelled";
                ShowCompletionNotification = true;
                return;
            }

            AddLogMessage($"Downloaded {downloadedMessages.Count:N0} messages successfully", LogLevel.Info);
            DownloadPhase = "Exporting to file";

            // Generate safe filename for export
            var fileName = GenerateSafeFileName(ChannelInfo);
            var outputPath = Path.Combine(OutputDirectory, fileName);

            AddLogMessage($"Exporting to markdown: {fileName}", LogLevel.Info);

            // Export to markdown
            await _telegramService.ExportMessagesToMarkdownAsync(
                downloadedMessages, ChannelInfo, outputPath, _downloadCancellationTokenSource.Token);

            var totalTime = DateTime.Now - _downloadStartTime;
            AddLogMessage($"Export completed successfully: {outputPath}", LogLevel.Info);
            AddLogMessage($"Download and export completed in {totalTime:mm\\:ss}!", LogLevel.Info);
            
            // Show completion notification
            CompletionMessage = $"Successfully downloaded {downloadedMessages.Count:N0} messages from {ChannelInfo.DisplayName}";
            ShowCompletionNotification = true;
            
            // Auto-dismiss notification after 10 seconds
            _ = Task.Delay(10000).ContinueWith(_ => 
            {
                _dispatcher.BeginInvoke(() => ShowCompletionNotification = false);
            });
        }
        catch (OperationCanceledException)
        {
            AddLogMessage("Download was cancelled", LogLevel.Warning);
            CompletionMessage = "Download cancelled";
            ShowCompletionNotification = true;
        }
        catch (UnauthorizedAccessException ex)
        {
            AddLogMessage($"Access denied to output directory - {ex.Message}", LogLevel.Error);
            CompletionMessage = "Access denied. Please check folder permissions.";
            ShowCompletionNotification = true;
            ShowErrorDialog("Access Denied", 
                "Cannot write to the selected output directory. Please check folder permissions or select a different directory.");
        }
        catch (DirectoryNotFoundException ex)
        {
            AddLogMessage($"Output directory not found - {ex.Message}", LogLevel.Error);
            CompletionMessage = "Output directory not found";
            ShowCompletionNotification = true;
            ShowErrorDialog("Directory Not Found", 
                "The specified output directory does not exist. Please select a valid directory.");
        }
        catch (DriveNotFoundException ex)
        {
            AddLogMessage($"Drive not available - {ex.Message}", LogLevel.Error);
            CompletionMessage = "Drive not available";
            ShowCompletionNotification = true;
            ShowErrorDialog("Drive Not Available", 
                "The drive containing the output directory is not available. Please check the drive and try again.");
        }
        catch (IOException ex)
        {
            AddLogMessage($"File system error - {ex.Message}", LogLevel.Error);
            CompletionMessage = "File system error occurred";
            ShowCompletionNotification = true;
            ShowErrorDialog("File System Error", 
                $"An error occurred while writing files: {ex.Message}\n\nPlease ensure you have enough disk space and proper permissions.");
        }
        catch (ArgumentException ex) when (ex.Message.Contains("path"))
        {
            AddLogMessage($"Invalid file path - {ex.Message}", LogLevel.Error);
            CompletionMessage = "Invalid file path";
            ShowCompletionNotification = true;
            ShowErrorDialog("Invalid Path", 
                "The file path contains invalid characters. Please select a different output directory.");
        }
        catch (Exception ex)
        {
            AddLogMessage($"Download failed - {ex.Message}", LogLevel.Error);
            CompletionMessage = "Download failed";
            ShowCompletionNotification = true;
            
            // Show detailed error for unexpected exceptions
            ShowErrorDialog("Download Failed", 
                $"An unexpected error occurred during download:\n\n{ex.Message}\n\nPlease try again or contact support if the issue persists.");
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

    /// <summary>
    /// Validates a channel URL asynchronously and retrieves channel information
    /// </summary>
    /// <param name="channelUrl">Channel URL to validate</param>
    /// <returns>Task representing the validation operation</returns>
    private async Task ValidateChannelAsync(string channelUrl)
    {
        try
        {
            IsValidatingChannel = true;
            ChannelValidationMessage = "Validating channel...";
            ChannelInfo = null;

            // Add a small delay to debounce rapid typing
            await Task.Delay(500);

            // Check if the URL has changed while we were waiting
            if (channelUrl != ChannelUrl)
            {
                return; // URL changed, abort this validation
            }

            // First do basic URL validation
            var basicValidation = _telegramService.ValidateChannelUrl(channelUrl);
            if (!basicValidation.IsValid)
            {
                ChannelValidationMessage = basicValidation.ErrorMessage ?? "Invalid channel URL format";
                return;
            }

            ChannelValidationMessage = "Checking channel accessibility...";

            // Get full channel information from Telegram API
            var channelInfo = await _telegramService.GetChannelInfoAsync(channelUrl);
            
            if (channelInfo == null)
            {
                ChannelValidationMessage = "Unable to retrieve channel information";
                return;
            }

            ChannelInfo = channelInfo;

            if (channelInfo.CanDownload)
            {
                ChannelValidationMessage = $"Channel validated: {channelInfo.MessageCount:N0} messages available";
                AddLogMessage($"Channel validated: {channelInfo.Summary}", LogLevel.Info);
            }
            else
            {
                ChannelValidationMessage = channelInfo.ValidationMessage ?? "Channel cannot be downloaded";
                AddLogMessage($"Channel validation warning: {ChannelValidationMessage}", LogLevel.Warning);
            }
        }
        catch (Exception ex)
        {
            ChannelValidationMessage = $"Validation error: {ex.Message}";
            AddLogMessage($"Channel validation failed: {ex.Message}", LogLevel.Error);
            ChannelInfo = null;
        }
        finally
        {
            IsValidatingChannel = false;
        }
    }

    private bool CanExecuteSubmitPhone() 
    { 
        var isNotConnecting = !IsConnecting;
        var hasPhoneNumber = !string.IsNullOrWhiteSpace(PhoneNumber);
        var isWaitingForPhone = AuthenticationState == AuthenticationState.WaitingForPhoneNumber;
        var result = isNotConnecting && hasPhoneNumber && isWaitingForPhone;
        
        // Debug logging (only when phone number is entered and result changes)
        if (!string.IsNullOrWhiteSpace(PhoneNumber) && (!_lastSubmitPhoneResult.HasValue || _lastSubmitPhoneResult != result))
        {
            AddLogMessage($"DEBUG Submit Button: NotConnecting={isNotConnecting}, HasPhone={hasPhoneNumber}, WaitingForPhone={isWaitingForPhone}, RESULT={result}", LogLevel.Warning);
            _lastSubmitPhoneResult = result;
        }
        
        return result;
    }

    private async Task ExecuteSubmitPhoneAsync()
    {
        try
        {
            IsConnecting = true;
            AddLogMessage($"Authenticating with phone number: {PhoneNumber}", LogLevel.Info);
            
            await _telegramService.AuthenticateWithPhoneAsync(PhoneNumber);
            AddLogMessage("Phone number submitted. Please check your phone for verification code.", LogLevel.Info);
        }
        catch (Exception ex)
        {
            AddLogMessage($"Failed to submit phone number - {ex.Message}", LogLevel.Error);
        }
        finally
        {
            IsConnecting = false;
        }
    }

    private bool CanExecuteSubmitCode() => !IsConnecting && !string.IsNullOrWhiteSpace(VerificationCode) && 
                                          AuthenticationState == AuthenticationState.WaitingForVerificationCode;

    private async Task ExecuteSubmitCodeAsync()
    {
        try
        {
            IsConnecting = true;
            AddLogMessage($"Verifying code: {VerificationCode}", LogLevel.Info);
            
            await _telegramService.VerifyCodeAsync(VerificationCode);
            AddLogMessage("Verification code submitted successfully.", LogLevel.Info);
        }
        catch (Exception ex)
        {
            AddLogMessage($"Failed to verify code - {ex.Message}", LogLevel.Error);
        }
        finally
        {
            IsConnecting = false;
        }
    }

    private bool CanExecuteSubmitTwoFactor() => !IsConnecting && !string.IsNullOrWhiteSpace(TwoFactorCode) && 
                                               AuthenticationState == AuthenticationState.WaitingForTwoFactorAuth;

    private async Task ExecuteSubmitTwoFactorAsync()
    {
        try
        {
            IsConnecting = true;
            AddLogMessage("Verifying two-factor authentication code...", LogLevel.Info);
            
            await _telegramService.VerifyTwoFactorAuthAsync(TwoFactorCode);
            AddLogMessage("Two-factor authentication completed successfully.", LogLevel.Info);
        }
        catch (Exception ex)
        {
            AddLogMessage($"Failed to verify two-factor code - {ex.Message}", LogLevel.Error);
        }
        finally
        {
            IsConnecting = false;
        }
    }

    private void ExecuteBrowseDirectory()
    {
        try
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "Select Output Directory";
            dialog.SelectedPath = OutputDirectory;
            dialog.ShowNewFolderButton = true;
            
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                OutputDirectory = dialog.SelectedPath;
                AddLogMessage($"Output directory changed to: {OutputDirectory}", LogLevel.Info);
            }
        }
        catch (Exception ex)
        {
            AddLogMessage($"Failed to open directory browser - {ex.Message}", LogLevel.Error);
        }
    }

    private bool CanExecuteCancelDownload() => IsDownloading && CanCancelDownload;
    
    private async Task ExecuteCancelDownloadAsync()
    {
        try
        {
            var result = System.Windows.MessageBox.Show(
                "Are you sure you want to cancel the download? Any progress will be lost.",
                "Cancel Download",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                AddLogMessage("Cancelling download...", LogLevel.Warning);
                _downloadCancellationTokenSource?.Cancel();
                CanCancelDownload = false;
            }
        }
        catch (Exception ex)
        {
            AddLogMessage($"Error cancelling download: {ex.Message}", LogLevel.Error);
        }
        
        await Task.CompletedTask;
    }
    
    private void ExecuteDismissNotification()
    {
        ShowCompletionNotification = false;
        CompletionMessage = string.Empty;
    }

    private void ExecuteClearLog()
    {
        _logEntries.Clear();
        LogOutput = string.Empty;
        AddLogMessage("Log cleared.", LogLevel.Info);
    }

    #endregion
    
    #region Private Methods

    /// <summary>
    /// Shows an error dialog to the user with detailed information
    /// </summary>
    /// <param name="title">Dialog title</param>
    /// <param name="message">Error message</param>
    private void ShowErrorDialog(string title, string message)
    {
        _dispatcher.BeginInvoke(() =>
        {
            System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        });
    }

    /// <summary>
    /// Validates that the output directory is accessible and has sufficient space
    /// </summary>
    /// <param name="path">Directory path to validate</param>
    /// <param name="estimatedSizeBytes">Estimated size needed in bytes</param>
    /// <returns>True if directory is valid and accessible</returns>
    private bool ValidateOutputDirectory(string path, long estimatedSizeBytes = 0)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                AddLogMessage("Output directory path is empty", LogLevel.Error);
                return false;
            }

            // Check if directory exists
            if (!Directory.Exists(path))
            {
                AddLogMessage($"Output directory does not exist: {path}", LogLevel.Warning);
                
                try
                {
                    Directory.CreateDirectory(path);
                    AddLogMessage($"Created output directory: {path}", LogLevel.Info);
                }
                catch (Exception ex)
                {
                    AddLogMessage($"Failed to create output directory: {ex.Message}", LogLevel.Error);
                    return false;
                }
            }

            // Check if directory is writable
            var testFileName = Path.Combine(path, $"test_{Guid.NewGuid()}.tmp");
            try
            {
                File.WriteAllText(testFileName, "test");
                File.Delete(testFileName);
                AddLogMessage("Output directory write access confirmed", LogLevel.Info);
            }
            catch (Exception ex)
            {
                AddLogMessage($"Output directory is not writable: {ex.Message}", LogLevel.Error);
                return false;
            }

            // Check available disk space if estimate provided
            if (estimatedSizeBytes > 0)
            {
                try
                {
                    var drive = new DriveInfo(Path.GetPathRoot(path) ?? path);
                    if (drive.AvailableFreeSpace < estimatedSizeBytes)
                    {
                        var availableGB = drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                        var neededGB = estimatedSizeBytes / (1024.0 * 1024.0 * 1024.0);
                        AddLogMessage($"Insufficient disk space. Available: {availableGB:F1} GB, Needed: {neededGB:F1} GB", LogLevel.Warning);
                        
                        var result = System.Windows.MessageBox.Show(
                            $"Warning: Low disk space detected.\n\nAvailable: {availableGB:F1} GB\nEstimated needed: {neededGB:F1} GB\n\nContinue anyway?",
                            "Low Disk Space",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);
                            
                        return result == MessageBoxResult.Yes;
                    }
                }
                catch (Exception ex)
                {
                    AddLogMessage($"Could not check disk space: {ex.Message}", LogLevel.Warning);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            AddLogMessage($"Directory validation failed: {ex.Message}", LogLevel.Error);
            return false;
        }
    }

    /// <summary>
    /// Estimates the size of a download based on channel information
    /// </summary>
    /// <param name="channelInfo">Channel information</param>
    /// <returns>Estimated size in bytes</returns>
    private long EstimateDownloadSize(ChannelInfo channelInfo)
    {
        if (channelInfo == null)
            return 0;

        // Rough estimate: average 200 bytes per message for text
        // This is conservative and doesn't include media files
        const long averageBytesPerMessage = 200;
        return channelInfo.MessageCount * averageBytesPerMessage;
    }

    /// <summary>
    /// Generates a safe filename for the channel export
    /// </summary>
    /// <param name="channelInfo">Channel information</param>
    /// <returns>Safe filename with timestamp</returns>
    private string GenerateSafeFileName(ChannelInfo channelInfo)
    {
        if (channelInfo == null)
            return $"telegram_export_{DateTime.Now:yyyyMMdd_HHmmss}.md";

        var channelName = channelInfo.Username ?? channelInfo.Title ?? "unknown";
        
        // Remove invalid filename characters
        var invalidChars = Path.GetInvalidFileNameChars();
        foreach (var c in invalidChars)
        {
            channelName = channelName.Replace(c, '_');
        }
        
        // Limit filename length
        if (channelName.Length > 50)
        {
            channelName = channelName.Substring(0, 50);
        }
        
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        return $"{channelName}_{timestamp}.md";
    }
    
    private void OnAuthenticationStatusChanged(object? sender, AuthenticationStatus status)
    {
        // Ensure UI updates happen on the UI thread
        _dispatcher.BeginInvoke(() =>
        {
            // Update authentication state
            AuthenticationState = status.State;
            IsConnected = status.IsConnected;
            IsConnecting = status.IsAuthenticating;
            ConnectionStatus = status.Message;
            
            if (status.User != null)
            {
                ConnectionStatus = $"Connected as {status.User.DisplayName}";
            }

            // If we just connected and have a channel URL, validate it
            if (status.IsConnected && !string.IsNullOrWhiteSpace(ChannelUrl))
            {
                _ = ValidateChannelAsync(ChannelUrl);
            }
            
            // Update UI field visibility based on authentication state
            IsPhoneNumberRequired = status.State == AuthenticationState.WaitingForPhoneNumber;
            IsVerificationCodeRequired = status.State == AuthenticationState.WaitingForVerificationCode;
            IsTwoFactorRequired = status.State == AuthenticationState.WaitingForTwoFactorAuth;
            
            // Clear input fields when appropriate
            if (status.State == AuthenticationState.WaitingForPhoneNumber)
            {
                VerificationCode = string.Empty;
                TwoFactorCode = string.Empty;
            }
            else if (status.State == AuthenticationState.WaitingForVerificationCode)
            {
                TwoFactorCode = string.Empty;
            }
            
            // Log status changes
            AddLogMessage($"Authentication status: {status.State} - {status.Message}", LogLevel.Info);
            
            if (!string.IsNullOrEmpty(status.ErrorMessage))
            {
                AddLogMessage(status.ErrorMessage, LogLevel.Error);
            }
            
            // Update command can execute states
            ((AsyncRelayCommand)SubmitPhoneCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)SubmitCodeCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)SubmitTwoFactorCommand).RaiseCanExecuteChanged();
        });
    }
    
    private void AddLogMessage(string message, LogLevel level = LogLevel.Info)
    {
        var logEntry = new LogEntry(level, message);
        
        // Add to structured log collection
        _dispatcher.BeginInvoke(() =>
        {
            _logEntries.Add(logEntry);
            
            // Maintain maximum log entries limit
            while (_logEntries.Count > MaxLogEntries)
            {
                _logEntries.RemoveAt(0);
            }
            
            // Update the text-based log output for backward compatibility
            if (!string.IsNullOrEmpty(LogOutput))
            {
                LogOutput += Environment.NewLine;
            }
            LogOutput += logEntry.FormattedMessage;
            
            // Trim text log if it gets too long (keep last 50 entries in text format)
            var lines = LogOutput.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > 50)
            {
                LogOutput = string.Join(Environment.NewLine, lines.Skip(lines.Length - 50));
            }
        });
    }
    
    protected override void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        
        // Update command can execute states when relevant properties change
        if (propertyName == nameof(ApiId) || propertyName == nameof(ApiHash) || propertyName == nameof(IsConnecting))
        {
            ((AsyncRelayCommand)ConnectCommand).RaiseCanExecuteChanged();
        }
        
        if (propertyName == nameof(PhoneNumber) || propertyName == nameof(IsConnecting) || propertyName == nameof(AuthenticationState))
        {
            ((AsyncRelayCommand)SubmitPhoneCommand).RaiseCanExecuteChanged();
        }
        
        if (propertyName == nameof(VerificationCode) || propertyName == nameof(IsConnecting) || propertyName == nameof(AuthenticationState))
        {
            ((AsyncRelayCommand)SubmitCodeCommand).RaiseCanExecuteChanged();
        }
        
        if (propertyName == nameof(TwoFactorCode) || propertyName == nameof(IsConnecting) || propertyName == nameof(AuthenticationState))
        {
            ((AsyncRelayCommand)SubmitTwoFactorCommand).RaiseCanExecuteChanged();
        }
        
        if (propertyName == nameof(IsConnected) || propertyName == nameof(IsDownloading) || 
            propertyName == nameof(ChannelUrl) || propertyName == nameof(OutputDirectory) ||
            propertyName == nameof(HasValidChannel))
        {
            ((AsyncRelayCommand)DownloadCommand).RaiseCanExecuteChanged();
        }
        
        if (propertyName == nameof(IsDownloading) || propertyName == nameof(CanCancelDownload))
        {
            ((AsyncRelayCommand)CancelDownloadCommand).RaiseCanExecuteChanged();
        }
        
        if (propertyName == nameof(DownloadedMessages) || propertyName == nameof(TotalMessages))
        {
            OnPropertyChanged(nameof(ProgressText));
        }
    }
    
    #endregion
}
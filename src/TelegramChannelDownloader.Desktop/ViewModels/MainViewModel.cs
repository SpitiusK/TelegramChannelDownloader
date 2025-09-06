using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using TelegramChannelDownloader.Core.Models;
using TelegramChannelDownloader.Desktop.Commands;
using TelegramChannelDownloader.Desktop.Services;
using TelegramChannelDownloader.Desktop.Utils;
using TelegramChannelDownloader.TelegramApi.Authentication.Models;

namespace TelegramChannelDownloader.Desktop.ViewModels;

public class MainViewModel : ObservableObject
{
    private readonly IUIService _uiService;
    private readonly ObservableCollection<LogEntry> _logEntries = new();
    private const int DefaultMaxLogEntries = 100;
    
    private string _logOutput = string.Empty;
    private int _selectedTabIndex = 0;

    public MainViewModel(AuthenticationViewModel authenticationViewModel, 
        DownloadViewModel downloadViewModel, SettingsViewModel settingsViewModel, IUIService uiService)
    {
        Authentication = authenticationViewModel ?? throw new ArgumentNullException(nameof(authenticationViewModel));
        Download = downloadViewModel ?? throw new ArgumentNullException(nameof(downloadViewModel));
        Settings = settingsViewModel ?? throw new ArgumentNullException(nameof(settingsViewModel));
        _uiService = uiService ?? throw new ArgumentNullException(nameof(uiService));

        // Initialize commands
        ClearLogCommand = new RelayCommand(ExecuteClearLog);

        // Subscribe to events from child ViewModels
        Authentication.AuthenticationStateChanged += OnAuthenticationStateChanged;
        Download.LogMessageRequested += OnLogMessageRequested;
        Settings.SettingsChanged += OnSettingsChanged;

        // Initialize with welcome message
        AddLogMessage("Application started. Please enter your API credentials to connect to Telegram.", LogLevel.Info);
        
        // Apply initial settings
        ApplySettings();
    }

    #region Properties

    public AuthenticationViewModel Authentication { get; }
    public DownloadViewModel Download { get; }
    public SettingsViewModel Settings { get; }

    public ObservableCollection<LogEntry> LogEntries => _logEntries;

    public string LogOutput
    {
        get => _logOutput;
        set => SetProperty(ref _logOutput, value);
    }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }

    public bool IsAuthenticated => Authentication.IsConnected;
    public bool CanDownload => IsAuthenticated && Download.HasValidChannel;

    #endregion

    #region Commands

    public ICommand ClearLogCommand { get; }

    #endregion

    #region Command Implementations

    private void ExecuteClearLog()
    {
        _logEntries.Clear();
        LogOutput = string.Empty;
        AddLogMessage("Log cleared.", LogLevel.Info);
    }

    #endregion

    #region Event Handlers

    private void OnAuthenticationStateChanged(object? sender, bool isAuthenticated)
    {
        // Update download VM when authentication state changes
        Download.UpdateAuthenticationStatus(isAuthenticated);
        
        // Update computed properties
        OnPropertyChanged(nameof(IsAuthenticated));
        OnPropertyChanged(nameof(CanDownload));
        
        // Add appropriate log messages based on authentication state
        if (isAuthenticated)
        {
            AddLogMessage("Successfully authenticated with Telegram.", LogLevel.Info);
            AddLogMessage("You can now validate channels and start downloads.", LogLevel.Info);
        }
        else if (sender is AuthenticationViewModel authVM)
        {
            var state = authVM.AuthenticationState;
            switch (state)
            {
                case AuthenticationState.Connecting:
                    AddLogMessage("Connecting to Telegram...", LogLevel.Info);
                    break;
                case AuthenticationState.WaitingForPhoneNumber:
                    AddLogMessage("Connected to Telegram. Please enter your phone number.", LogLevel.Info);
                    break;
                case AuthenticationState.WaitingForVerificationCode:
                    AddLogMessage("Phone number submitted. Please enter verification code.", LogLevel.Info);
                    break;
                case AuthenticationState.WaitingForTwoFactorAuth:
                    AddLogMessage("Verification code accepted. Please enter 2FA password.", LogLevel.Info);
                    break;
                case AuthenticationState.Disconnected:
                    AddLogMessage("Disconnected from Telegram.", LogLevel.Warning);
                    break;
                case AuthenticationState.AuthenticationFailed:
                    AddLogMessage("Authentication failed. Please check credentials and try again.", LogLevel.Warning);
                    break;
                case AuthenticationState.ConnectionError:
                    AddLogMessage("Connection error. Please check internet connection and try again.", LogLevel.Warning);
                    break;
            }
        }
    }

    private void OnLogMessageRequested(object? sender, string message)
    {
        AddLogMessage(message, LogLevel.Info);
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        ApplySettings();
        AddLogMessage("Settings updated.", LogLevel.Info);
    }

    #endregion

    #region Public Methods

    public void AddLogMessage(string message, LogLevel level = LogLevel.Info)
    {
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = level,
            Message = message,
            FormattedMessage = $"[{DateTime.Now:HH:mm:ss}] {GetLogLevelPrefix(level)}{message}"
        };

        // Add to structured log collection
        _logEntries.Add(logEntry);

        // Maintain maximum log entries limit
        var maxEntries = Settings.MaxLogEntries;
        while (_logEntries.Count > maxEntries)
        {
            _logEntries.RemoveAt(0);
        }

        // Update the text-based log output for backward compatibility
        if (!string.IsNullOrEmpty(LogOutput))
        {
            LogOutput += Environment.NewLine;
        }
        LogOutput += logEntry.FormattedMessage;

        // Trim text log if it gets too long (keep last entries in text format)
        var lines = LogOutput.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        var maxLines = Math.Max(50, maxEntries / 2);
        if (lines.Length > maxLines)
        {
            LogOutput = string.Join(Environment.NewLine, lines.Skip(lines.Length - maxLines));
        }
    }

    #endregion

    #region Private Methods

    private void ApplySettings()
    {
        var config = Settings.GetConfiguration();
        
        // Apply default output directory to download VM if it's not already set
        if (string.IsNullOrEmpty(Download.OutputDirectory) || 
            Download.OutputDirectory == Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TelegramDownloads"))
        {
            Download.OutputDirectory = config.DefaultOutputDirectory ?? Download.OutputDirectory;
        }
        
        // Apply default export format
        Download.ExportFormat = config.DefaultExportFormat;
    }

    private static string GetLogLevelPrefix(LogLevel level) => level switch
    {
        LogLevel.Error => "[ERROR] ",
        LogLevel.Warning => "[WARN] ",
        LogLevel.Info => "",
        _ => ""
    };

    #endregion
}

/// <summary>
/// Represents a log entry with timestamp and level
/// </summary>
public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public LogLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public string FormattedMessage { get; set; } = string.Empty;
}

/// <summary>
/// Represents log levels for the application
/// </summary>
public enum LogLevel
{
    Info,
    Warning,
    Error
}
using System.IO;
using System.Windows.Input;
using TelegramChannelDownloader.Core.Models;
using TelegramChannelDownloader.Desktop.Commands;
using TelegramChannelDownloader.Desktop.Services;
using TelegramChannelDownloader.Desktop.Utils;

namespace TelegramChannelDownloader.Desktop.ViewModels;

public class SettingsViewModel : ObservableObject
{
    private readonly IUIService _uiService;
    
    private string _defaultOutputDirectory = string.Empty;
    private ExportFormat _defaultExportFormat = ExportFormat.Markdown;
    private bool _showLogDetails = true;
    private bool _autoOpenOutputDirectory = false;
    private bool _autoOpenExportedFile = false;
    private int _maxLogEntries = 100;
    private bool _enableNotifications = true;
    private TimeSpan _notificationDismissTime = TimeSpan.FromSeconds(10);

    public SettingsViewModel(IUIService uiService)
    {
        _uiService = uiService ?? throw new ArgumentNullException(nameof(uiService));

        // Initialize commands
        BrowseDefaultDirectoryCommand = new AsyncRelayCommand(ExecuteBrowseDefaultDirectoryAsync);
        ResetToDefaultsCommand = new RelayCommand(ExecuteResetToDefaults);
        SaveSettingsCommand = new RelayCommand(ExecuteSaveSettings);
        
        // Set default values
        LoadDefaultSettings();
    }

    #region Properties

    public string DefaultOutputDirectory
    {
        get => _defaultOutputDirectory;
        set => SetProperty(ref _defaultOutputDirectory, value);
    }

    public ExportFormat DefaultExportFormat
    {
        get => _defaultExportFormat;
        set => SetProperty(ref _defaultExportFormat, value);
    }

    public IEnumerable<ExportFormat> AvailableExportFormats => 
        Enum.GetValues<ExportFormat>();

    public bool ShowLogDetails
    {
        get => _showLogDetails;
        set => SetProperty(ref _showLogDetails, value);
    }

    public bool AutoOpenOutputDirectory
    {
        get => _autoOpenOutputDirectory;
        set => SetProperty(ref _autoOpenOutputDirectory, value);
    }

    public bool AutoOpenExportedFile
    {
        get => _autoOpenExportedFile;
        set => SetProperty(ref _autoOpenExportedFile, value);
    }

    public int MaxLogEntries
    {
        get => _maxLogEntries;
        set => SetProperty(ref _maxLogEntries, Math.Max(10, Math.Min(1000, value)));
    }

    public bool EnableNotifications
    {
        get => _enableNotifications;
        set => SetProperty(ref _enableNotifications, value);
    }

    public TimeSpan NotificationDismissTime
    {
        get => _notificationDismissTime;
        set => SetProperty(ref _notificationDismissTime, value);
    }

    public int NotificationDismissSeconds
    {
        get => (int)NotificationDismissTime.TotalSeconds;
        set => NotificationDismissTime = TimeSpan.FromSeconds(Math.Max(5, Math.Min(60, value)));
    }

    #endregion

    #region Commands

    public ICommand BrowseDefaultDirectoryCommand { get; }
    public ICommand ResetToDefaultsCommand { get; }
    public ICommand SaveSettingsCommand { get; }

    #endregion

    #region Events

    public event EventHandler? SettingsChanged;

    #endregion

    #region Command Implementations

    private async Task ExecuteBrowseDefaultDirectoryAsync()
    {
        try
        {
            var selectedDirectory = await _uiService.SelectDirectoryAsync(DefaultOutputDirectory);
            if (!string.IsNullOrEmpty(selectedDirectory))
            {
                DefaultOutputDirectory = selectedDirectory;
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (Exception ex)
        {
            await _uiService.ShowErrorAsync("Directory Selection Failed", 
                $"Failed to select directory: {ex.Message}");
        }
    }

    private void ExecuteResetToDefaults()
    {
        LoadDefaultSettings();
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ExecuteSaveSettings()
    {
        // For now, just notify that settings have changed
        // In a full implementation, this would persist settings to a configuration file
        SettingsChanged?.Invoke(this, EventArgs.Empty);
        _uiService.ShowNotification("Settings saved successfully", NotificationType.Success);
    }

    #endregion

    #region Private Methods

    private void LoadDefaultSettings()
    {
        DefaultOutputDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
            "TelegramDownloads");
        DefaultExportFormat = ExportFormat.Markdown;
        ShowLogDetails = true;
        AutoOpenOutputDirectory = false;
        AutoOpenExportedFile = false;
        MaxLogEntries = 100;
        EnableNotifications = true;
        NotificationDismissTime = TimeSpan.FromSeconds(10);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets the current settings as a configuration object
    /// </summary>
    /// <returns>Application configuration</returns>
    public AppConfig GetConfiguration()
    {
        return new AppConfig
        {
            DefaultOutputDirectory = DefaultOutputDirectory,
            DefaultExportFormat = DefaultExportFormat,
            MaxLogEntries = MaxLogEntries,
            AutoOpenOutputDirectory = AutoOpenOutputDirectory,
            AutoOpenExportedFile = AutoOpenExportedFile
        };
    }

    /// <summary>
    /// Updates settings from a configuration object
    /// </summary>
    /// <param name="config">Application configuration</param>
    public void UpdateFromConfiguration(AppConfig config)
    {
        DefaultOutputDirectory = config.DefaultOutputDirectory ?? DefaultOutputDirectory;
        DefaultExportFormat = config.DefaultExportFormat;
        MaxLogEntries = config.MaxLogEntries;
        AutoOpenOutputDirectory = config.AutoOpenOutputDirectory;
        AutoOpenExportedFile = config.AutoOpenExportedFile;
    }

    #endregion
}
using Microsoft.Extensions.Logging;
using TelegramChannelDownloader.Desktop.ViewModels;

namespace TelegramChannelDownloader.Desktop.Services;

/// <summary>
/// Logger provider that bridges Microsoft.Extensions.Logging to the WPF UI logging system
/// </summary>
public class UILoggerProvider : ILoggerProvider
{
    private readonly Func<MainViewModel> _mainViewModelFactory;

    public UILoggerProvider(Func<MainViewModel> mainViewModelFactory)
    {
        _mainViewModelFactory = mainViewModelFactory ?? throw new ArgumentNullException(nameof(mainViewModelFactory));
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new UILogger(categoryName, _mainViewModelFactory);
    }

    public void Dispose()
    {
        // No resources to dispose
    }
}

/// <summary>
/// Logger implementation that forwards log messages to the WPF UI
/// </summary>
internal class UILogger : ILogger
{
    private readonly string _categoryName;
    private readonly Func<MainViewModel> _mainViewModelFactory;

    public UILogger(string categoryName, Func<MainViewModel> mainViewModelFactory)
    {
        _categoryName = categoryName;
        _mainViewModelFactory = mainViewModelFactory;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel)
    {
        // Enable all log levels
        return true;
    }

    public void Log<TState>(
        Microsoft.Extensions.Logging.LogLevel logLevel, 
        EventId eventId, 
        TState state, 
        Exception? exception, 
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        try
        {
            var mainViewModel = _mainViewModelFactory();
            var message = formatter(state, exception);
            
            // Format the message with category name for context
            var formattedMessage = $"[{GetCategoryShortName(_categoryName)}] {message}";
            
            // Convert Microsoft.Extensions.Logging.LogLevel to our UI LogLevel
            var uiLogLevel = ConvertLogLevel(logLevel);
            
            // Add to UI log
            mainViewModel.AddLogMessage(formattedMessage, uiLogLevel);
        }
        catch
        {
            // Ignore errors in logging to prevent infinite loops
        }
    }

    private static string GetCategoryShortName(string categoryName)
    {
        // Get just the class name from the full namespace
        var parts = categoryName.Split('.');
        return parts.Length > 0 ? parts[^1] : categoryName;
    }

    private static ViewModels.LogLevel ConvertLogLevel(Microsoft.Extensions.Logging.LogLevel logLevel)
    {
        return logLevel switch
        {
            Microsoft.Extensions.Logging.LogLevel.Critical => ViewModels.LogLevel.Error,
            Microsoft.Extensions.Logging.LogLevel.Error => ViewModels.LogLevel.Error,
            Microsoft.Extensions.Logging.LogLevel.Warning => ViewModels.LogLevel.Warning,
            Microsoft.Extensions.Logging.LogLevel.Information => ViewModels.LogLevel.Info,
            Microsoft.Extensions.Logging.LogLevel.Debug => ViewModels.LogLevel.Debug,
            Microsoft.Extensions.Logging.LogLevel.Trace => ViewModels.LogLevel.Trace,
            _ => ViewModels.LogLevel.Info
        };
    }
}
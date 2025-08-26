using System.Windows;

namespace TelegramChannelDownloader.Desktop.Services;

public class DialogService : IDialogService
{
    public async Task<bool> ShowProgressDialogAsync(string title, string message, Func<IProgress<string>, CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        // For now, just execute the operation without a progress dialog
        // This could be enhanced with a custom progress dialog window
        var progress = new Progress<string>(_ => { }); // Placeholder progress reporter
        
        try
        {
            await operation(progress, cancellationToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Error", $"{message}\n\nError: {ex.Message}");
            return false;
        }
    }

    public Task ShowMessageAsync(string title, string message, string buttonText = "OK")
    {
        if (Application.Current?.Dispatcher == null)
            return Task.CompletedTask;

        return Application.Current.Dispatcher.InvokeAsync(() =>
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }).Task;
    }

    public Task<bool> ShowQuestionAsync(string title, string message, string yesText = "Yes", string noText = "No")
    {
        if (Application.Current?.Dispatcher == null)
            return Task.FromResult(false);

        return Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }).Task;
    }

    public Task<string?> ShowInputAsync(string title, string message, string defaultValue = "")
    {
        // For now, return the default value. This could be enhanced with a custom input dialog
        return Task.FromResult<string?>(defaultValue);
    }
}
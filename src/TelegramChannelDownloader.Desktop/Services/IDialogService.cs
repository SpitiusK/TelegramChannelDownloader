namespace TelegramChannelDownloader.Desktop.Services;

public interface IDialogService
{
    Task<bool> ShowProgressDialogAsync(string title, string message, Func<IProgress<string>, CancellationToken, Task> operation, CancellationToken cancellationToken = default);
    Task ShowMessageAsync(string title, string message, string buttonText = "OK");
    Task<bool> ShowQuestionAsync(string title, string message, string yesText = "Yes", string noText = "No");
    Task<string?> ShowInputAsync(string title, string message, string defaultValue = "");
}
namespace TelegramChannelDownloader.Desktop.Services;

public enum NotificationType
{
    Info,
    Warning,
    Error,
    Success
}

public interface IUIService
{
    Task ShowErrorAsync(string title, string message);
    Task<bool> ShowConfirmationAsync(string title, string message);
    Task<string?> SelectDirectoryAsync(string currentPath = "");
    Task<string?> SelectFileAsync(string filter, string defaultPath = "");
    void ShowNotification(string message, NotificationType type = NotificationType.Info);
    Task OpenFileOrDirectoryAsync(string path);
}
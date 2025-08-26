using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace TelegramChannelDownloader.Desktop.Services;

public class UIService : IUIService
{
    public Task ShowErrorAsync(string title, string message)
    {
        return Application.Current?.Dispatcher?.InvokeAsync(() =>
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }).Task ?? Task.CompletedTask;
    }

    public Task<bool> ShowConfirmationAsync(string title, string message)
    {
        if (Application.Current?.Dispatcher == null)
            return Task.FromResult(false);

        return Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }).Task;
    }

    public Task<string?> SelectDirectoryAsync(string currentPath = "")
    {
        return Task.FromResult<string?>(SelectDirectory(currentPath));
    }

    public Task<string?> SelectFileAsync(string filter, string defaultPath = "")
    {
        return Task.FromResult<string?>(SelectFile(filter, defaultPath));
    }

    public void ShowNotification(string message, NotificationType type = NotificationType.Info)
    {
        // For now, use a simple message box. This could be enhanced with toast notifications
        var icon = type switch
        {
            NotificationType.Error => MessageBoxImage.Error,
            NotificationType.Warning => MessageBoxImage.Warning,
            NotificationType.Success => MessageBoxImage.Information,
            _ => MessageBoxImage.Information
        };

        Application.Current?.Dispatcher?.BeginInvoke(() =>
        {
            MessageBox.Show(message, $"Notification - {type}", MessageBoxButton.OK, icon);
        });
    }

    public Task OpenFileOrDirectoryAsync(string path)
    {
        return Task.Run(() =>
        {
            try
            {
                if (File.Exists(path))
                {
                    // Open file with default application
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    });
                }
                else if (Directory.Exists(path))
                {
                    // Open directory in explorer
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"\"{path}\"",
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                Application.Current?.Dispatcher?.BeginInvoke(() =>
                {
                    MessageBox.Show($"Failed to open {path}: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        });
    }

    private string? SelectDirectory(string currentPath)
    {
        try
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Directory",
                InitialDirectory = currentPath
            };

            return dialog.ShowDialog() == true ? dialog.FolderName : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private string? SelectFile(string filter, string defaultPath)
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Filter = filter,
                InitialDirectory = defaultPath
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
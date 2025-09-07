using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TelegramChannelDownloader.Core.Extensions;
using TelegramChannelDownloader.Desktop.Services;
using TelegramChannelDownloader.Desktop.ViewModels;
using TelegramChannelDownloader.TelegramApi;
using TelegramChannelDownloader.TelegramApi.Extensions;

namespace TelegramChannelDownloader.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Create and configure the host
        _host = Host.CreateDefaultBuilder(e.Args)
            .ConfigureServices((context, services) =>
            {
                // Register TelegramApi layer
                services.AddTelegramApi();
                
                // Register Core layer with database configuration
                services.AddTelegramChannelDownloaderCore(context.Configuration);
                
                // Register Desktop layer services
                services.AddScoped<IUIService, UIService>();
                services.AddScoped<IDialogService, DialogService>();
                
                // Register ViewModels
                services.AddScoped<AuthenticationViewModel>();
                services.AddScoped<DownloadViewModel>();
                services.AddScoped<SettingsViewModel>();
                services.AddScoped<MainViewModel>();
                
                // Register Views
                services.AddTransient<MainWindow>();
            })
            .ConfigureLogging((context, logging) =>
            {
                // Clear default providers
                logging.ClearProviders();
                
                // Set minimum log level to include Debug for development
                logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
                
                // Add console logging for development
                logging.AddConsole();
                
                // Add our custom UI logger provider
                logging.Services.AddSingleton<ILoggerProvider>(serviceProvider =>
                {
                    var mainViewModelFactory = new Func<MainViewModel>(() => 
                        serviceProvider.GetRequiredService<MainViewModel>());
                    return new UILoggerProvider(mainViewModelFactory);
                });
            })
            .Build();

        // Start the host
        _host.Start();

        // Show the main window
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        base.OnExit(e);
    }
}


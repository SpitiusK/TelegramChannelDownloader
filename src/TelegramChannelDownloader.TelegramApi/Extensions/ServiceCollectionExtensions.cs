using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TelegramChannelDownloader.TelegramApi.Authentication;
using TelegramChannelDownloader.TelegramApi.Channels;
using TelegramChannelDownloader.TelegramApi.Messages;
using TelegramChannelDownloader.TelegramApi.Session;
using WTelegram;

namespace TelegramChannelDownloader.TelegramApi.Extensions;

/// <summary>
/// Extension methods for registering TelegramApi services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all TelegramApi layer services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddTelegramApi(this IServiceCollection services)
    {
        // Register main API client first - it will manage WTelegramClient internally
        services.AddScoped<ITelegramApiClient, TelegramApiClient>();
        
        // For now, let's not register the services that depend on WTelegramClient
        // They will be created manually by TelegramApiClient when needed
        
        // Register session management with default session path
        services.AddScoped<ISessionManager>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<SessionManager>>();
            return new SessionManager("session.dat", logger);
        });
        
        return services;
    }
}
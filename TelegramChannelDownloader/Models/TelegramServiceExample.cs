using TelegramChannelDownloader.Services;
using TelegramChannelDownloader.Models;

namespace TelegramChannelDownloader.Models;

/// <summary>
/// Example integration class showing how to use ITelegramService
/// This demonstrates the proper usage patterns for the UI architect
/// </summary>
public class TelegramServiceExample
{
    private readonly ITelegramService _telegramService;

    public TelegramServiceExample(ITelegramService telegramService)
    {
        _telegramService = telegramService;
        
        // Subscribe to authentication status changes
        _telegramService.AuthenticationStatusChanged += OnAuthenticationStatusChanged;
    }

    /// <summary>
    /// Example of how to initialize and authenticate with Telegram
    /// </summary>
    /// <param name="apiId">API ID from my.telegram.org</param>
    /// <param name="apiHash">API Hash from my.telegram.org</param>
    /// <param name="phoneNumber">Phone number including country code</param>
    /// <returns>Task representing the operation</returns>
    public async Task<bool> ConnectToTelegramAsync(int apiId, string apiHash, string phoneNumber)
    {
        try
        {
            // Step 1: Initialize with credentials
            var credentials = new TelegramCredentials
            {
                ApiId = apiId,
                ApiHash = apiHash,
                PhoneNumber = phoneNumber
            };

            await _telegramService.InitializeAsync(credentials);

            // Step 2: Start authentication process
            await _telegramService.AuthenticateWithPhoneAsync(phoneNumber);

            // At this point, the user will need to provide verification code
            // The UI should listen to AuthenticationStatusChanged event and show appropriate prompts

            return true;
        }
        catch (Exception ex)
        {
            // Handle error in UI
            Console.WriteLine($"Connection failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Example of how to handle verification code input
    /// </summary>
    /// <param name="verificationCode">Code received via SMS or app</param>
    /// <returns>Task representing the operation</returns>
    public async Task<bool> ProvideVerificationCodeAsync(string verificationCode)
    {
        try
        {
            await _telegramService.VerifyCodeAsync(verificationCode);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Code verification failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Example of how to handle two-factor authentication
    /// </summary>
    /// <param name="password">Two-factor authentication password</param>
    /// <returns>Task representing the operation</returns>
    public async Task<bool> ProvideTwoFactorPasswordAsync(string password)
    {
        try
        {
            await _telegramService.VerifyTwoFactorAuthAsync(password);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Two-factor authentication failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Event handler for authentication status changes
    /// This shows how the UI should respond to different states
    /// </summary>
    /// <param name="sender">Event sender</param>
    /// <param name="status">Authentication status</param>
    private void OnAuthenticationStatusChanged(object? sender, AuthenticationStatus status)
    {
        switch (status.State)
        {
            case AuthenticationState.Disconnected:
                // Show connect button, disable other controls
                break;

            case AuthenticationState.Connecting:
                // Show loading indicator, disable controls
                break;

            case AuthenticationState.WaitingForPhoneNumber:
                // Show phone number input
                break;

            case AuthenticationState.WaitingForVerificationCode:
                // Show verification code input
                break;

            case AuthenticationState.WaitingForTwoFactorAuth:
                // Show two-factor password input
                break;

            case AuthenticationState.Authenticated:
                // Enable main functionality, show user info
                var user = status.User;
                Console.WriteLine($"Connected as: {user?.DisplayName}");
                break;

            case AuthenticationState.AuthenticationFailed:
                // Show error message, reset to initial state
                Console.WriteLine($"Authentication failed: {status.ErrorMessage}");
                break;

            case AuthenticationState.ConnectionError:
                // Show connection error, allow retry
                Console.WriteLine($"Connection error: {status.ErrorMessage}");
                break;
        }

        // Update UI status display
        Console.WriteLine($"Status: {status.Message}");
    }

    /// <summary>
    /// Example of how to disconnect
    /// </summary>
    /// <returns>Task representing the operation</returns>
    public async Task DisconnectAsync()
    {
        await _telegramService.DisconnectAsync();
    }

    /// <summary>
    /// Example of how to check if connected
    /// </summary>
    /// <returns>True if connected and authenticated</returns>
    public bool IsConnected()
    {
        return _telegramService.IsConnected;
    }

    /// <summary>
    /// Example of how to get current user information
    /// </summary>
    /// <returns>Current user or null if not authenticated</returns>
    public TelegramUser? GetCurrentUser()
    {
        return _telegramService.CurrentUser;
    }
}
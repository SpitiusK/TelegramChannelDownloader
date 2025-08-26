namespace TelegramChannelDownloader.TelegramApi.Session;

/// <summary>
/// Manages Telegram session persistence and storage
/// </summary>
public interface ISessionManager
{
    /// <summary>
    /// Gets the current session data if available
    /// </summary>
    /// <returns>Session data as string, or null if no session exists</returns>
    Task<string?> GetSessionDataAsync();

    /// <summary>
    /// Saves session data for future use
    /// </summary>
    /// <param name="sessionData">Session data to save</param>
    /// <returns>Task representing the save operation</returns>
    Task SaveSessionDataAsync(string sessionData);

    /// <summary>
    /// Clears all stored session data
    /// </summary>
    /// <returns>Task representing the clear operation</returns>
    Task ClearSessionAsync();

    /// <summary>
    /// Checks if a valid session exists
    /// </summary>
    /// <returns>True if a valid session is available</returns>
    bool HasValidSession();

    /// <summary>
    /// Gets the path where session data is stored
    /// </summary>
    string SessionPath { get; }

    /// <summary>
    /// Event triggered when session data changes
    /// </summary>
    event EventHandler<SessionEventArgs>? SessionChanged;
}

/// <summary>
/// Event arguments for session changes
/// </summary>
public class SessionEventArgs : EventArgs
{
    /// <summary>
    /// Type of session event
    /// </summary>
    public SessionEventType EventType { get; set; }

    /// <summary>
    /// Optional message describing the session change
    /// </summary>
    public string? Message { get; set; }

    public SessionEventArgs(SessionEventType eventType, string? message = null)
    {
        EventType = eventType;
        Message = message;
    }
}

/// <summary>
/// Types of session events
/// </summary>
public enum SessionEventType
{
    /// <summary>
    /// Session data was saved
    /// </summary>
    SessionSaved,

    /// <summary>
    /// Session data was loaded
    /// </summary>
    SessionLoaded,

    /// <summary>
    /// Session data was cleared
    /// </summary>
    SessionCleared,

    /// <summary>
    /// Session validation failed
    /// </summary>
    SessionInvalid
}
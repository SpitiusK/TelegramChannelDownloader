using Microsoft.Extensions.Logging;

namespace TelegramChannelDownloader.TelegramApi.Session;

/// <summary>
/// File-based implementation of session management
/// </summary>
public class SessionManager : ISessionManager
{
    private readonly ILogger<SessionManager> _logger;
    private readonly string _sessionPath;
    
    /// <summary>
    /// Event triggered when session data changes
    /// </summary>
    public event EventHandler<SessionEventArgs>? SessionChanged;

    /// <summary>
    /// Gets the path where session data is stored
    /// </summary>
    public string SessionPath => _sessionPath;

    /// <summary>
    /// Initializes a new instance of SessionManager
    /// </summary>
    /// <param name="sessionPath">Path where session data will be stored</param>
    /// <param name="logger">Logger instance</param>
    public SessionManager(string sessionPath, ILogger<SessionManager> logger)
    {
        _sessionPath = sessionPath ?? throw new ArgumentNullException(nameof(sessionPath));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the current session data if available
    /// </summary>
    /// <returns>Session data as string, or null if no session exists</returns>
    public async Task<string?> GetSessionDataAsync()
    {
        try
        {
            if (!File.Exists(_sessionPath))
            {
                _logger.LogDebug("Session file does not exist at path: {SessionPath}", _sessionPath);
                return null;
            }

            var sessionData = await File.ReadAllTextAsync(_sessionPath);
            
            if (string.IsNullOrWhiteSpace(sessionData))
            {
                _logger.LogWarning("Session file exists but is empty: {SessionPath}", _sessionPath);
                return null;
            }

            _logger.LogDebug("Session data loaded successfully from: {SessionPath}", _sessionPath);
            SessionChanged?.Invoke(this, new SessionEventArgs(SessionEventType.SessionLoaded, "Session data loaded successfully"));
            
            return sessionData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read session data from: {SessionPath}", _sessionPath);
            SessionChanged?.Invoke(this, new SessionEventArgs(SessionEventType.SessionInvalid, $"Failed to load session: {ex.Message}"));
            return null;
        }
    }

    /// <summary>
    /// Saves session data for future use
    /// </summary>
    /// <param name="sessionData">Session data to save</param>
    /// <returns>Task representing the save operation</returns>
    public async Task SaveSessionDataAsync(string sessionData)
    {
        if (string.IsNullOrWhiteSpace(sessionData))
        {
            throw new ArgumentException("Session data cannot be null or empty", nameof(sessionData));
        }

        try
        {
            // Ensure the directory exists
            var directory = Path.GetDirectoryName(_sessionPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogDebug("Created session directory: {Directory}", directory);
            }

            // Save session data to file
            await File.WriteAllTextAsync(_sessionPath, sessionData);
            
            _logger.LogDebug("Session data saved successfully to: {SessionPath}", _sessionPath);
            SessionChanged?.Invoke(this, new SessionEventArgs(SessionEventType.SessionSaved, "Session data saved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save session data to: {SessionPath}", _sessionPath);
            throw new InvalidOperationException($"Failed to save session data: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Clears all stored session data
    /// </summary>
    /// <returns>Task representing the clear operation</returns>
    public async Task ClearSessionAsync()
    {
        try
        {
            if (File.Exists(_sessionPath))
            {
                File.Delete(_sessionPath);
                _logger.LogDebug("Session file deleted: {SessionPath}", _sessionPath);
            }
            
            SessionChanged?.Invoke(this, new SessionEventArgs(SessionEventType.SessionCleared, "Session data cleared successfully"));
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear session data at: {SessionPath}", _sessionPath);
            throw new InvalidOperationException($"Failed to clear session data: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Checks if a valid session exists
    /// </summary>
    /// <returns>True if a valid session is available</returns>
    public bool HasValidSession()
    {
        try
        {
            if (!File.Exists(_sessionPath))
            {
                return false;
            }

            var fileInfo = new FileInfo(_sessionPath);
            if (fileInfo.Length == 0)
            {
                return false;
            }

            // Basic validation - check if file was created recently (within last 30 days)
            // This is a simple heuristic, actual session validation should be done by the Telegram client
            var sessionAge = DateTime.Now - fileInfo.LastWriteTime;
            if (sessionAge.TotalDays > 30)
            {
                _logger.LogWarning("Session file is older than 30 days, may be expired: {SessionPath}", _sessionPath);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking session validity: {SessionPath}", _sessionPath);
            return false;
        }
    }
}
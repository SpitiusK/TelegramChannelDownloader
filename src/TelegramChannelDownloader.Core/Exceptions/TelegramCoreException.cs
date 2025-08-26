namespace TelegramChannelDownloader.Core.Exceptions;

/// <summary>
/// Base exception for all Telegram Channel Downloader Core exceptions
/// </summary>
public abstract class TelegramCoreException : Exception
{
    /// <summary>
    /// Additional context information about the exception
    /// </summary>
    public Dictionary<string, object> Context { get; } = new();

    /// <summary>
    /// Timestamp when the exception occurred
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;

    /// <summary>
    /// Unique identifier for this exception occurrence
    /// </summary>
    public string ExceptionId { get; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets the exception type name for logging
    /// </summary>
    public string ExceptionType => GetType().Name;

    /// <summary>
    /// Creates a new core exception
    /// </summary>
    /// <param name="message">Error message</param>
    protected TelegramCoreException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Creates a new core exception with inner exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    protected TelegramCoreException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Adds context information to the exception
    /// </summary>
    /// <param name="key">Context key</param>
    /// <param name="value">Context value</param>
    public void AddContext(string key, object value)
    {
        Context[key] = value;
    }

    /// <summary>
    /// Gets context value by key
    /// </summary>
    /// <typeparam name="T">Type to cast to</typeparam>
    /// <param name="key">Context key</param>
    /// <returns>Context value or default</returns>
    public T? GetContext<T>(string key)
    {
        if (Context.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }

    /// <summary>
    /// Gets context value by key with default
    /// </summary>
    /// <typeparam name="T">Type to cast to</typeparam>
    /// <param name="key">Context key</param>
    /// <param name="defaultValue">Default value if not found</param>
    /// <returns>Context value or default</returns>
    public T GetContext<T>(string key, T defaultValue)
    {
        if (Context.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }

    /// <summary>
    /// Gets all context information as a formatted string
    /// </summary>
    /// <returns>Formatted context string</returns>
    public string GetFormattedContext()
    {
        if (!Context.Any())
            return string.Empty;

        var contextItems = Context.Select(kvp => $"{kvp.Key}: {kvp.Value}");
        return string.Join(", ", contextItems);
    }

    /// <summary>
    /// Gets detailed exception information for logging
    /// </summary>
    /// <returns>Detailed exception information</returns>
    public virtual string GetDetailedInfo()
    {
        var details = new List<string>
        {
            $"Exception ID: {ExceptionId}",
            $"Type: {ExceptionType}",
            $"Timestamp: {Timestamp:yyyy-MM-dd HH:mm:ss.fff} UTC",
            $"Message: {Message}"
        };

        if (Context.Any())
        {
            details.Add($"Context: {GetFormattedContext()}");
        }

        if (InnerException != null)
        {
            details.Add($"Inner Exception: {InnerException.GetType().Name}: {InnerException.Message}");
        }

        return string.Join(Environment.NewLine, details);
    }

    /// <summary>
    /// Gets user-friendly error message (without technical details)
    /// </summary>
    /// <returns>User-friendly message</returns>
    public virtual string GetUserFriendlyMessage()
    {
        // Override in derived classes to provide more specific user-friendly messages
        return Message;
    }

    /// <summary>
    /// Determines if the exception represents a recoverable error
    /// </summary>
    /// <returns>True if the operation can be retried</returns>
    public virtual bool IsRecoverable()
    {
        // Override in derived classes to indicate if retry is possible
        return false;
    }

    /// <summary>
    /// Gets recommended action for the user
    /// </summary>
    /// <returns>Recommended action string</returns>
    public virtual string GetRecommendedAction()
    {
        // Override in derived classes to provide specific recommendations
        return "Please try again or contact support if the issue persists.";
    }
}
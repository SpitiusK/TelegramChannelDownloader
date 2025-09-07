using TelegramChannelDownloader.DataBase.Entities;

namespace TelegramChannelDownloader.DataBase.Repositories;

/// <summary>
/// Repository interface for managing download sessions and messages with high-performance batch operations
/// </summary>
public interface IMessageRepository
{
    #region Download Session Management

    /// <summary>
    /// Create a new download session
    /// </summary>
    /// <param name="session">The download session to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created session with generated ID</returns>
    Task<DownloadSession> CreateSessionAsync(DownloadSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a download session by ID
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The download session or null if not found</returns>
    Task<DownloadSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a download session with its messages
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The download session with messages or null if not found</returns>
    Task<DownloadSession?> GetSessionWithMessagesAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update download session status and metadata
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="status">New status</param>
    /// <param name="totalMessages">Total messages downloaded</param>
    /// <param name="processedMessages">Total messages processed</param>
    /// <param name="errorMessage">Error message if failed</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateSessionStatusAsync(Guid sessionId, DownloadSessionStatus status, int totalMessages = 0, int processedMessages = 0, string? errorMessage = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark session as completed
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="totalMessages">Total messages downloaded</param>
    /// <param name="exportPath">Export file path if exported</param>
    /// <param name="exportFormat">Export format used</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CompleteSessionAsync(Guid sessionId, int totalMessages, string? exportPath = null, string? exportFormat = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active download sessions (not expired)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active sessions</returns>
    Task<List<DownloadSession>> GetActiveSessionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get sessions by channel ID
    /// </summary>
    /// <param name="channelId">Channel ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of sessions for the channel</returns>
    Task<List<DownloadSession>> GetSessionsByChannelAsync(long channelId, CancellationToken cancellationToken = default);

    #endregion

    #region Message Management

    /// <summary>
    /// Add messages in batch for optimal performance (recommended batch size: 1000)
    /// </summary>
    /// <param name="messages">Messages to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddMessagesBatchAsync(IEnumerable<TelegramMessage> messages, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add messages using bulk insert for maximum performance
    /// </summary>
    /// <param name="messages">Messages to bulk insert</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task BulkInsertMessagesAsync(IEnumerable<TelegramMessage> messages, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get messages for a session with pagination
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="skip">Number of messages to skip</param>
    /// <param name="take">Number of messages to take</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated messages</returns>
    Task<List<TelegramMessage>> GetSessionMessagesAsync(Guid sessionId, int skip = 0, int take = 1000, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get message count for a session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total message count</returns>
    Task<int> GetSessionMessageCountAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a message exists in a session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="messageId">Telegram message ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if message exists</returns>
    Task<bool> MessageExistsAsync(Guid sessionId, long messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get messages by date range
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="fromDate">Start date</param>
    /// <param name="toDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Messages in date range</returns>
    Task<List<TelegramMessage>> GetMessagesByDateRangeAsync(Guid sessionId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search messages by content
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="searchTerm">Search term</param>
    /// <param name="skip">Number of results to skip</param>
    /// <param name="take">Number of results to take</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Messages matching search term</returns>
    Task<List<TelegramMessage>> SearchMessagesAsync(Guid sessionId, string searchTerm, int skip = 0, int take = 100, CancellationToken cancellationToken = default);

    #endregion

    #region Data Lifecycle Management

    /// <summary>
    /// Clean up expired download sessions and their messages
    /// </summary>
    /// <param name="cutoffDate">Delete sessions that expired before this date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of sessions cleaned up</returns>
    Task<int> CleanupExpiredSessionsAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get expired sessions for cleanup
    /// </summary>
    /// <param name="cutoffDate">Sessions that expired before this date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of expired sessions</returns>
    Task<List<DownloadSession>> GetExpiredSessionsAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update expiry date for sessions
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="newExpiryDate">New expiry date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateSessionExpiryAsync(Guid sessionId, DateTime? newExpiryDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get database statistics for monitoring
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Database usage statistics</returns>
    Task<DatabaseStatistics> GetDatabaseStatisticsAsync(CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Database usage statistics
/// </summary>
public class DatabaseStatistics
{
    public int TotalSessions { get; set; }
    public int ActiveSessions { get; set; }
    public int ExpiredSessions { get; set; }
    public long TotalMessages { get; set; }
    public long DatabaseSizeBytes { get; set; }
    public DateTime LastCleanupDate { get; set; }
    public TimeSpan OldestSessionAge { get; set; }
}
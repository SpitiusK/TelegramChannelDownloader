using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TelegramChannelDownloader.DataBase.Entities;

namespace TelegramChannelDownloader.DataBase.Repositories;

/// <summary>
/// High-performance repository implementation for managing download sessions and messages
/// Optimized for handling millions of messages with batch operations
/// </summary>
public class MessageRepository : IMessageRepository
{
    private readonly TelegramDbContext _context;
    private readonly ILogger<MessageRepository> _logger;

    public MessageRepository(TelegramDbContext context, ILogger<MessageRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Download Session Management

    public async Task<DownloadSession> CreateSessionAsync(DownloadSession session, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.DownloadSessions.Add(session);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created download session {SessionId} for channel {ChannelUsername}", 
                session.Id, session.ChannelUsername);

            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create download session for channel {ChannelUsername}", 
                session.ChannelUsername);
            throw;
        }
    }

    public async Task<DownloadSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.DownloadSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get download session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<DownloadSession?> GetSessionWithMessagesAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.DownloadSessions
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get download session {SessionId} with messages", sessionId);
            throw;
        }
    }

    public async Task UpdateSessionStatusAsync(Guid sessionId, DownloadSessionStatus status, 
        int totalMessages = 0, int processedMessages = 0, string? errorMessage = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await _context.DownloadSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

            if (session == null)
            {
                _logger.LogWarning("Download session {SessionId} not found for status update", sessionId);
                return;
            }

            session.Status = status;
            if (totalMessages > 0) session.TotalMessages = totalMessages;
            if (processedMessages > 0) session.ProcessedMessages = processedMessages;
            if (!string.IsNullOrEmpty(errorMessage)) session.ErrorMessage = errorMessage;

            if (status == DownloadSessionStatus.Completed)
            {
                session.CompletedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated session {SessionId} status to {Status}", sessionId, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update session {SessionId} status", sessionId);
            throw;
        }
    }

    public async Task CompleteSessionAsync(Guid sessionId, int totalMessages, string? exportPath = null, 
        string? exportFormat = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await _context.DownloadSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

            if (session == null)
            {
                _logger.LogWarning("Download session {SessionId} not found for completion", sessionId);
                return;
            }

            session.Status = DownloadSessionStatus.Completed;
            session.CompletedAt = DateTime.UtcNow;
            session.TotalMessages = totalMessages;
            session.ExportPath = exportPath;
            session.ExportFormat = exportFormat;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Completed session {SessionId} with {TotalMessages} messages", 
                sessionId, totalMessages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<List<DownloadSession>> GetActiveSessionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.GetActiveDownloadSessions()
                .OrderByDescending(s => s.StartedAt)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active sessions");
            throw;
        }
    }

    public async Task<List<DownloadSession>> GetSessionsByChannelAsync(long channelId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.DownloadSessions
                .Where(s => s.ChannelId == channelId)
                .OrderByDescending(s => s.StartedAt)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get sessions for channel {ChannelId}", channelId);
            throw;
        }
    }

    #endregion

    #region Message Management

    public async Task AddMessagesBatchAsync(IEnumerable<TelegramMessage> messages, CancellationToken cancellationToken = default)
    {
        try
        {
            var messageList = messages.ToList();
            if (!messageList.Any()) return;

            await _context.TelegramMessages.AddRangeAsync(messageList, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Added batch of {MessageCount} messages", messageList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add message batch");
            throw;
        }
    }

    public async Task BulkInsertMessagesAsync(IEnumerable<TelegramMessage> messages, CancellationToken cancellationToken = default)
    {
        try
        {
            // For now, use regular batch insert - can be optimized later with BulkExtensions
            await AddMessagesBatchAsync(messages, cancellationToken);
            _logger.LogInformation("Bulk inserted {MessageCount} messages using batch insert", messages.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bulk insert {MessageCount} messages", messages.Count());
            throw;
        }
    }

    public async Task<List<TelegramMessage>> GetSessionMessagesAsync(Guid sessionId, int skip = 0, int take = 1000, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.GetSessionMessages(sessionId)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get messages for session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<int> GetSessionMessageCountAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.TelegramMessages
                .Where(m => m.DownloadSessionId == sessionId)
                .CountAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get message count for session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<bool> MessageExistsAsync(Guid sessionId, long messageId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.TelegramMessages
                .AnyAsync(m => m.DownloadSessionId == sessionId && m.Id == messageId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if message {MessageId} exists in session {SessionId}", 
                messageId, sessionId);
            throw;
        }
    }

    public async Task<List<TelegramMessage>> GetMessagesByDateRangeAsync(Guid sessionId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.TelegramMessages
                .Where(m => m.DownloadSessionId == sessionId && m.Date >= fromDate && m.Date <= toDate)
                .OrderBy(m => m.Date)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get messages by date range for session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<List<TelegramMessage>> SearchMessagesAsync(Guid sessionId, string searchTerm, 
        int skip = 0, int take = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            // Use simple LIKE search for now - can be optimized with PostgreSQL full-text search later
            return await _context.TelegramMessages
                .Where(m => m.DownloadSessionId == sessionId && 
                           m.Content != null && 
                           m.Content.Contains(searchTerm))
                .OrderByDescending(m => m.Date)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search messages in session {SessionId} for term '{SearchTerm}'", 
                sessionId, searchTerm);
            throw;
        }
    }

    #endregion

    #region Data Lifecycle Management

    public async Task<int> CleanupExpiredSessionsAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var expiredSessions = await _context.DownloadSessions
                .Where(s => s.ExpiresAt != null && s.ExpiresAt <= cutoffDate)
                .ToListAsync(cancellationToken);

            if (!expiredSessions.Any())
            {
                _logger.LogInformation("No expired sessions found for cleanup");
                return 0;
            }

            var sessionIds = expiredSessions.Select(s => s.Id).ToList();
            
            // Delete messages first (cascading delete should handle this, but being explicit)
            var messagesToDelete = await _context.TelegramMessages
                .Where(m => sessionIds.Contains(m.DownloadSessionId))
                .ToListAsync(cancellationToken);

            if (messagesToDelete.Any())
            {
                _context.TelegramMessages.RemoveRange(messagesToDelete);
            }

            // Delete sessions
            _context.DownloadSessions.RemoveRange(expiredSessions);
            
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Cleaned up {SessionCount} expired sessions and {MessageCount} messages", 
                expiredSessions.Count, messagesToDelete.Count);

            return expiredSessions.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired sessions");
            throw;
        }
    }

    public async Task<List<DownloadSession>> GetExpiredSessionsAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.GetExpiredDownloadSessions()
                .Where(s => s.ExpiresAt <= cutoffDate)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get expired sessions");
            throw;
        }
    }

    public async Task UpdateSessionExpiryAsync(Guid sessionId, DateTime? newExpiryDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await _context.DownloadSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

            if (session == null)
            {
                _logger.LogWarning("Download session {SessionId} not found for expiry update", sessionId);
                return;
            }

            session.ExpiresAt = newExpiryDate;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated session {SessionId} expiry to {ExpiryDate}", 
                sessionId, newExpiryDate?.ToString() ?? "never");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update session {SessionId} expiry", sessionId);
            throw;
        }
    }

    public async Task<DatabaseStatistics> GetDatabaseStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var totalSessions = await _context.DownloadSessions.CountAsync(cancellationToken);
            var activeSessions = await _context.GetActiveDownloadSessions().CountAsync(cancellationToken);
            var expiredSessions = await _context.GetExpiredDownloadSessions().CountAsync(cancellationToken);
            var totalMessages = await _context.TelegramMessages.LongCountAsync(cancellationToken);

            // Get the oldest session age
            var oldestSession = await _context.DownloadSessions
                .OrderBy(s => s.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            var oldestSessionAge = oldestSession?.CreatedAt != null 
                ? DateTime.UtcNow - oldestSession.CreatedAt 
                : TimeSpan.Zero;

            // Database size would require raw SQL query - simplified for now
            var databaseSizeBytes = 0L; // TODO: Implement with raw SQL query

            return new DatabaseStatistics
            {
                TotalSessions = totalSessions,
                ActiveSessions = activeSessions,
                ExpiredSessions = expiredSessions,
                TotalMessages = totalMessages,
                DatabaseSizeBytes = databaseSizeBytes,
                LastCleanupDate = DateTime.UtcNow, // TODO: Track this properly
                OldestSessionAge = oldestSessionAge
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get database statistics");
            throw;
        }
    }

    #endregion
}
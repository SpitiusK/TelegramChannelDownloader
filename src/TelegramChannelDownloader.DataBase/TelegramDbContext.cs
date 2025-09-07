using Microsoft.EntityFrameworkCore;
using TelegramChannelDownloader.DataBase.Entities;

namespace TelegramChannelDownloader.DataBase;

/// <summary>
/// Entity Framework DbContext for Telegram Channel Downloader database
/// Optimized for handling millions of messages with proper indexing and relationships
/// </summary>
public class TelegramDbContext : DbContext
{
    public TelegramDbContext(DbContextOptions<TelegramDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Download sessions table
    /// </summary>
    public DbSet<DownloadSession> DownloadSessions { get; set; } = null!;

    /// <summary>
    /// Telegram messages table
    /// </summary>
    public DbSet<TelegramMessage> TelegramMessages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureDownloadSession(modelBuilder);
        ConfigureTelegramMessage(modelBuilder);
    }

    /// <summary>
    /// Configure DownloadSession entity with optimizations
    /// </summary>
    private static void ConfigureDownloadSession(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DownloadSession>(entity =>
        {
            entity.ToTable("download_sessions");
            
            entity.HasKey(e => e.Id);
            
            // Indexes for efficient queries
            entity.HasIndex(e => e.ChannelId)
                  .HasDatabaseName("IX_download_sessions_channel_id");
            
            entity.HasIndex(e => e.Status)
                  .HasDatabaseName("IX_download_sessions_status");
            
            entity.HasIndex(e => e.StartedAt)
                  .HasDatabaseName("IX_download_sessions_started_at");
            
            entity.HasIndex(e => e.ExpiresAt)
                  .HasDatabaseName("IX_download_sessions_expires_at")
                  .HasFilter("expires_at IS NOT NULL");
            
            entity.HasIndex(e => new { e.ChannelId, e.Status })
                  .HasDatabaseName("IX_download_sessions_channel_status");

            // String length constraints
            entity.Property(e => e.ChannelUsername)
                  .IsRequired()
                  .HasMaxLength(200);
            
            entity.Property(e => e.ChannelTitle)
                  .IsRequired()
                  .HasMaxLength(500);
            
            entity.Property(e => e.ErrorMessage)
                  .HasMaxLength(2000);
            
            entity.Property(e => e.ExportFormat)
                  .HasMaxLength(50);
            
            entity.Property(e => e.ExportPath)
                  .HasMaxLength(1000);

            // Default values
            entity.Property(e => e.Status)
                  .HasDefaultValue(DownloadSessionStatus.InProgress);
            
            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("NOW()");
            
            entity.Property(e => e.StartedAt)
                  .HasDefaultValueSql("NOW()");
        });
    }

    /// <summary>
    /// Configure TelegramMessage entity with performance optimizations
    /// </summary>
    private static void ConfigureTelegramMessage(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TelegramMessage>(entity =>
        {
            entity.ToTable("telegram_messages");
            
            entity.HasKey(e => e.Id);
            
            // Critical indexes for performance with millions of records
            entity.HasIndex(e => e.DownloadSessionId)
                  .HasDatabaseName("IX_telegram_messages_session_id");
            
            entity.HasIndex(e => new { e.DownloadSessionId, e.Date })
                  .HasDatabaseName("IX_telegram_messages_session_date");
            
            entity.HasIndex(e => new { e.DownloadSessionId, e.MessageType })
                  .HasDatabaseName("IX_telegram_messages_session_type");
            
            entity.HasIndex(e => e.Date)
                  .HasDatabaseName("IX_telegram_messages_date");
            
            entity.HasIndex(e => e.FromId)
                  .HasDatabaseName("IX_telegram_messages_from_id");
            
            entity.HasIndex(e => e.ReplyToMessageId)
                  .HasDatabaseName("IX_telegram_messages_reply_to")
                  .HasFilter("reply_to_message_id IS NOT NULL");
            
            // GIN index for full-text search on content (PostgreSQL specific)
            // Note: Commented out until pg_trgm extension is available
            // entity.HasIndex(e => e.Content)
            //       .HasDatabaseName("IX_telegram_messages_content_gin")
            //       .HasMethod("gin")
            //       .HasOperators("gin_trgm_ops");

            // Composite index for common queries
            entity.HasIndex(e => new { e.DownloadSessionId, e.HasMedia })
                  .HasDatabaseName("IX_telegram_messages_session_media");

            // String length constraints
            entity.Property(e => e.FromUsername)
                  .HasMaxLength(200);
            
            entity.Property(e => e.FromDisplayName)
                  .HasMaxLength(500);
            
            entity.Property(e => e.MediaType)
                  .HasMaxLength(100);
            
            entity.Property(e => e.MediaFileName)
                  .HasMaxLength(500);
            
            entity.Property(e => e.MediaMimeType)
                  .HasMaxLength(200);

            // Use PostgreSQL-specific column types for better performance
            entity.Property(e => e.Content)
                  .HasColumnType("text");
            
            entity.Property(e => e.Reactions)
                  .HasColumnType("jsonb");
            
            entity.Property(e => e.RawData)
                  .HasColumnType("jsonb");

            // Default values
            entity.Property(e => e.MessageType)
                  .HasDefaultValue(MessageType.Text);
            
            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("NOW()");
            
            entity.Property(e => e.HasMedia)
                  .HasDefaultValue(false);
            
            entity.Property(e => e.IsForwarded)
                  .HasDefaultValue(false);
            
            entity.Property(e => e.IsEdited)
                  .HasDefaultValue(false);
            
            entity.Property(e => e.IsPinned)
                  .HasDefaultValue(false);

            // Foreign key relationship
            entity.HasOne(e => e.DownloadSession)
                  .WithMany(s => s.Messages)
                  .HasForeignKey(e => e.DownloadSessionId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("FK_telegram_messages_download_sessions");
        });
    }

    // Note: OnConfiguring method removed to support DbContext pooling
    // All configuration is now handled through dependency injection in ServiceCollectionExtensions.cs

    /// <summary>
    /// Bulk insert messages for optimal performance
    /// </summary>
    public async Task BulkInsertMessagesAsync(IEnumerable<TelegramMessage> messages, CancellationToken cancellationToken = default)
    {
        await TelegramMessages.AddRangeAsync(messages, cancellationToken);
        await SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Get active download sessions that haven't expired
    /// </summary>
    public IQueryable<DownloadSession> GetActiveDownloadSessions()
    {
        return DownloadSessions.Where(s => s.ExpiresAt == null || s.ExpiresAt > DateTime.UtcNow);
    }

    /// <summary>
    /// Get expired sessions for cleanup
    /// </summary>
    public IQueryable<DownloadSession> GetExpiredDownloadSessions()
    {
        return DownloadSessions.Where(s => s.ExpiresAt != null && s.ExpiresAt <= DateTime.UtcNow);
    }

    /// <summary>
    /// Get messages for a session with efficient pagination
    /// </summary>
    public IQueryable<TelegramMessage> GetSessionMessages(Guid sessionId)
    {
        return TelegramMessages
            .Where(m => m.DownloadSessionId == sessionId)
            .OrderBy(m => m.Date);
    }
}
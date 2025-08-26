# Claude.md - TelegramChannelDownloader.TelegramApi Layer

## Layer Overview

**Purpose**: The TelegramApi layer serves as the data access tier in the Telegram Channel Downloader's clean 3-layer architecture. It provides a clean abstraction over the WTelegramClient library, handling all direct interactions with Telegram's MTProto API, including authentication, channel operations, message retrieval, and session management.

**Architecture Role**: 
- **Position**: Bottom layer in the 3-layer architecture (Data Access Layer)
- **Dependencies**: Only external dependencies (WTelegramClient 3.7.1, .NET libraries)
- **Dependents**: Core layer consumes TelegramApi services through interfaces
- **Design Principle**: Pure API integration with no business logic or UI concerns

**Key Responsibilities**:
- Telegram MTProto API integration through WTelegramClient
- Multi-step authentication flow management with state tracking
- Channel discovery, validation, and information retrieval
- Message downloading with progress reporting and batching
- Session management and persistence for authenticated users
- Error handling and API-specific exception management
- Real-time export functionality for multiple formats
- URL parsing and validation for Telegram channels

## Technology Stack

**Framework**: .NET 8.0 Class Library
**Primary Dependency**: WTelegramClient 3.7.1 (C# wrapper for Telegram MTProto API)
**Additional Dependencies**:
- Microsoft.Extensions.Logging 8.0.0 (for structured logging)
- System.Text.Json (for serialization)

**API Integration**:
- **Protocol**: MTProto (Telegram's proprietary protocol)
- **Authentication**: Multi-step flow (phone → SMS/app code → optional 2FA)
- **Session Management**: Encrypted session persistence
- **Rate Limiting**: Built-in handling through WTelegramClient

## Project Structure

```
TelegramChannelDownloader.TelegramApi/
├── ITelegramApiClient.cs                 # Main API client interface
├── TelegramApiClient.cs                  # Primary API client facade implementation
├── Authentication/                       # Authentication management
│   ├── IAuthenticationHandler.cs         # Authentication interface
│   ├── AuthenticationHandler.cs          # Multi-step authentication logic
│   └── Models/                          # Authentication data models
│       ├── AuthenticationModels.cs       # Auth state, results, user info
│       └── TelegramCredentials.cs        # API credentials container
├── Channels/                            # Channel operations
│   ├── IChannelService.cs               # Channel service interface
│   ├── ChannelService.cs                # Channel info and validation
│   └── Models/                          # Channel-related models
│       └── ChannelInfo.cs               # Comprehensive channel information
├── Messages/                            # Message operations
│   ├── IMessageService.cs               # Message service interface
│   ├── MessageService.cs                # Download and export implementation
│   └── Models/                          # Message data structures
│       └── MessageData.cs               # Complete message representation
├── Session/                             # Session management
│   ├── ISessionManager.cs               # Session persistence interface
│   └── SessionManager.cs                # File-based session storage
├── Configuration/                       # API configuration
│   └── TelegramApiConfig.cs             # API settings and validation
├── Utils/                               # Utility classes
│   └── ChannelUrlParser.cs              # URL parsing and validation
├── Extensions/                          # Service registration
│   └── ServiceCollectionExtensions.cs   # DI container setup
└── TelegramChannelDownloader.TelegramApi.csproj  # Project configuration
```

## Core Components

### TelegramApiClient (Main Facade)

**Purpose**: Primary entry point that coordinates specialized service handlers and provides a unified API interface.

**Architecture Pattern**: Facade pattern - delegates operations to specialized service handlers:
- `AuthenticationHandler` for authentication operations
- `ChannelService` for channel-related operations  
- `MessageService` for message downloading and export
- `SessionManager` for session persistence

**Key Features**:
- **Centralized Error Handling**: All exceptions properly caught, logged, and wrapped
- **Service Coordination**: Manages lifecycles of specialized service handlers
- **Event Aggregation**: Forwards authentication events to upper layers
- **Resource Management**: Proper disposal of WTelegramClient and service instances

**Usage Pattern**:
```csharp
// Initialize with API credentials
var config = new TelegramApiConfig 
{ 
    ApiId = 12345, 
    ApiHash = "abcdef..." 
};
var result = await _telegramApi.InitializeAsync(config);

// Authenticate with phone number
await _telegramApi.AuthenticatePhoneAsync("+1234567890");

// Verify with SMS code
await _telegramApi.VerifyCodeAsync("12345");

// Download channel content
var channelInfo = await _telegramApi.GetChannelInfoAsync("@channelname");
var messages = await _telegramApi.DownloadChannelMessagesAsync(channelInfo);
```

### Authentication Components

#### IAuthenticationHandler / AuthenticationHandler
**Purpose**: Manages the complete Telegram authentication workflow with state tracking.

**Authentication Flow States**:
1. **Disconnected**: Initial state, not connected to Telegram
2. **Connecting**: Establishing connection with API credentials
3. **WaitingForPhoneNumber**: Ready to accept phone number input
4. **WaitingForVerificationCode**: Phone submitted, awaiting SMS/app code
5. **WaitingForTwoFactorAuth**: Code verified, awaiting 2FA password
6. **Authenticated**: Successfully authenticated and connected
7. **AuthenticationFailed**: Authentication failed, retry required
8. **ConnectionError**: Network or API connection issues

**State Management**:
```csharp
public class AuthResult
{
    public AuthenticationState State { get; set; }
    public bool IsSuccess { get; set; }
    public string Message { get; set; }
    public TelegramUserInfo? User { get; set; }
    public string? ErrorMessage { get; set; }
    public Exception? Exception { get; set; }
    
    // Computed properties for UI binding
    public bool IsConnected => State == AuthenticationState.Authenticated;
    public bool IsAuthenticating => State == AuthenticationState.Connecting;
    public bool RequiresUserInput => State is WaitingForPhoneNumber 
                                           or WaitingForVerificationCode 
                                           or WaitingForTwoFactorAuth;
}
```

**Event-Driven Updates**:
```csharp
public event EventHandler<AuthStatusChangedEventArgs>? StatusChanged;

// Usage in upper layers
_telegramApi.AuthenticationStatusChanged += OnAuthStatusChanged;
```

### Channel Components

#### IChannelService / ChannelService
**Purpose**: Handles all channel-related operations including discovery, validation, and information retrieval.

**Key Operations**:
- **Channel URL Validation**: Parse and validate t.me URLs and usernames
- **Channel Information Retrieval**: Get comprehensive channel metadata
- **Access Verification**: Check if channel is accessible to current user
- **Message Count Estimation**: Pre-download size estimation

**Channel Information Model**:
```csharp
public class ChannelInfo
{
    // Basic Information
    public long Id { get; set; }
    public string? Username { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public ChannelType Type { get; set; }
    
    // Statistics
    public int MemberCount { get; set; }
    public int MessageCount { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? LastActivityDate { get; set; }
    
    // Access and Restrictions
    public bool IsAccessible { get; set; }
    public bool IsRestricted { get; set; }
    public bool HasProtectedContent { get; set; }
    public bool IsVerified { get; set; }
    public bool IsScam { get; set; }
    
    // Computed Properties
    public bool IsPublic => !string.IsNullOrWhiteSpace(Username);
    public bool CanDownload => IsAccessible && !IsRestricted && !HasProtectedContent && MessageCount > 0;
    public string DisplayName => IsPublic ? $"{Title} (@{Username})" : Title;
    public string? ValidationMessage => /* user-friendly validation message */;
}
```

**Channel Types**:
```csharp
public enum ChannelType
{
    Unknown,
    Channel,           // Public broadcast channel
    Supergroup,        // Public supergroup with discussion
    PrivateChannel,    // Private broadcast channel
    PrivateSupergroup, // Private supergroup
    Group             // Regular group chat
}
```

### Message Components

#### IMessageService / MessageService
**Purpose**: Handles message downloading, processing, and export functionality.

**Message Data Structure**:
```csharp
public class MessageData
{
    // Basic Message Information
    public int MessageId { get; set; }
    public DateTime Timestamp { get; set; }
    public string Content { get; set; }
    
    // Sender Information
    public string? SenderUsername { get; set; }
    public string? SenderDisplayName { get; set; }
    public long? SenderId { get; set; }
    
    // Content Analysis (auto-extracted)
    public List<string> Links { get; set; }      // URLs found in content
    public List<string> Mentions { get; set; }   // @username mentions
    public List<string> Hashtags { get; set; }   // #hashtag references
    
    // Media and Attachments
    public MessageType MessageType { get; set; }
    public MediaInfo? Media { get; set; }
    
    // Message Context
    public ForwardInfo? ForwardInfo { get; set; }  // If forwarded
    public ReplyInfo? ReplyInfo { get; set; }      // If reply to another message
    public bool IsEdited { get; set; }
    public DateTime? EditedTimestamp { get; set; }
    public int? Views { get; set; }
    
    // Processing Methods
    public void ProcessContent();                   // Extract links, mentions, hashtags
    public string FormattedContent { get; }         // Formatted for export
    public string SenderDisplay { get; }            // Friendly sender name
}
```

**Message Types**:
```csharp
public enum MessageType
{
    Text, Photo, Video, Audio, Document, Animation,
    Voice, VideoNote, Sticker, Location, Contact, Poll, Service, Unknown
}
```

**Download Progress Reporting**:
```csharp
public class DownloadProgressInfo
{
    public int TotalMessages { get; set; }
    public int DownloadedMessages { get; set; }
    public MessageData? CurrentMessage { get; set; }
    public int ProgressPercentage => (DownloadedMessages * 100) / TotalMessages;
    public TimeSpan? EstimatedTimeRemaining { get; set; }
    public double MessagesPerSecond { get; set; }
    public string? ErrorMessage { get; set; }
    
    public bool IsComplete => DownloadedMessages >= TotalMessages;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
}
```

**Batch Processing**:
```csharp
// Memory-efficient batch processing
public IAsyncEnumerable<List<MessageData>> DownloadChannelMessagesBatchAsync(
    ChannelInfo channelInfo, 
    int batchSize = 100, 
    IProgress<DownloadProgressInfo>? progress = null, 
    CancellationToken cancellationToken = default)
{
    // Implementation yields batches of messages for processing
    // Prevents memory issues with large channels
}
```

### Session Management

#### ISessionManager / SessionManager
**Purpose**: Handles persistent storage of Telegram session data for authentication continuity.

**Key Features**:
- **File-based Storage**: Session data stored in encrypted format
- **Automatic Persistence**: Sessions saved after successful authentication
- **Session Restoration**: Seamless reconnection without re-authentication
- **Security**: WTelegramClient handles encryption automatically

**Usage Pattern**:
```csharp
public class SessionManager : ISessionManager
{
    public SessionManager(string sessionPath, ILogger<SessionManager> logger);
    
    public async Task<bool> HasValidSessionAsync();
    public async Task<string?> LoadSessionDataAsync();
    public async Task SaveSessionDataAsync(string sessionData);
    public async Task ClearSessionAsync();
    public string SessionPath { get; }
}
```

### Configuration Management

#### TelegramApiConfig
**Purpose**: Centralized configuration for all Telegram API operations with validation.

**Configuration Options**:
```csharp
public class TelegramApiConfig
{
    // Required API Credentials
    public int ApiId { get; set; }                    // From my.telegram.org
    public string ApiHash { get; set; }               // From my.telegram.org
    
    // Session and Storage
    public string SessionPath { get; set; } = "session.dat";
    public string? DownloadDirectory { get; set; }
    
    // Performance and Limits
    public int RequestTimeoutMs { get; set; } = 30000;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
    public int MaxBatchSize { get; set; } = 100;
    
    // Media Download Settings
    public bool DownloadMedia { get; set; } = false;
    public long MaxFileSize { get; set; } = 0;        // 0 = no limit
    
    // Validation
    public bool IsValid => ApiId > 0 && !string.IsNullOrWhiteSpace(ApiHash);
    public List<string> Validate();                   // Returns validation errors
}
```

**Configuration Validation**:
```csharp
public List<string> Validate()
{
    var errors = new List<string>();
    
    if (ApiId <= 0)
        errors.Add("API ID must be a positive integer");
        
    if (string.IsNullOrWhiteSpace(ApiHash))
        errors.Add("API Hash is required");
    else if (ApiHash.Length != 32)
        errors.Add("API Hash must be exactly 32 characters long");
        
    // Additional validations...
    return errors;
}
```

## Service Registration and DI Integration

### ServiceCollectionExtensions
**Purpose**: Provides clean dependency injection setup for the TelegramApi layer.

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTelegramApi(this IServiceCollection services)
    {
        // Register main API client as the primary interface
        services.AddScoped<ITelegramApiClient, TelegramApiClient>();
        
        // Register session management with configurable session path
        services.AddScoped<ISessionManager>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<SessionManager>>();
            return new SessionManager("session.dat", logger);
        });
        
        // Individual services are created internally by TelegramApiClient
        // This ensures proper WTelegramClient lifecycle management
        
        return services;
    }
}
```

**Service Lifetimes**:
- **ITelegramApiClient**: Scoped (one per application session)
- **ISessionManager**: Scoped (shared with API client)
- **Internal Services**: Created and managed by TelegramApiClient

## Error Handling and Logging

### Exception Management
The TelegramApi layer implements comprehensive error handling:

```csharp
try
{
    // API operation
    return await _channelService.GetChannelInfoAsync(channelUrl, cancellationToken);
}
catch (UnauthorizedAccessException ex)
{
    _logger.LogError(ex, "Access denied for channel: {ChannelUrl}", channelUrl);
    throw new TelegramApiException("Channel access denied. Please verify authentication.", ex);
}
catch (TimeoutException ex)
{
    _logger.LogWarning(ex, "Request timeout for channel: {ChannelUrl}", channelUrl);
    throw new TelegramApiException("Request timed out. Please try again.", ex);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error retrieving channel: {ChannelUrl}", channelUrl);
    throw new TelegramApiException($"Failed to retrieve channel information: {ex.Message}", ex);
}
```

### Structured Logging
All operations include comprehensive logging with structured data:

```csharp
_logger.LogDebug("Starting authentication for phone: {PhoneNumber}", 
    phoneNumber.Substring(0, 3) + "***"); // Partial phone for privacy

_logger.LogInformation("Successfully downloaded {MessageCount} messages from {ChannelTitle} in {Duration}ms", 
    messages.Count, channelInfo.Title, stopwatch.ElapsedMilliseconds);

_logger.LogError(ex, "Authentication failed for user {UserId} with state {AuthState}", 
    userId, authState);
```

## Integration Patterns

### With Core Layer
The TelegramApi layer is consumed by the Core layer through clean interfaces:

```csharp
// In Core layer DownloadService
public class DownloadService : IDownloadService
{
    private readonly ITelegramApiClient _telegramApi;
    
    public async Task<DownloadResult> DownloadChannelAsync(DownloadRequest request)
    {
        // 1. Validate channel through TelegramApi
        var channelInfo = await _telegramApi.GetChannelInfoAsync(request.ChannelUrl);
        
        // 2. Download messages with progress reporting
        var messages = await _telegramApi.DownloadChannelMessagesAsync(
            channelInfo, progress, cancellationToken);
            
        // 3. Core layer handles business logic and export coordination
        return ProcessDownloadedMessages(messages, request);
    }
}
```

### WTelegramClient Integration
The layer abstracts WTelegramClient complexity:

```csharp
// Internal WTelegramClient configuration
_client = new WTelegram.Client(what => what switch
{
    "api_id" => config.ApiId.ToString(),
    "api_hash" => config.ApiHash,
    "session_pathname" => _sessionManager.SessionPath,
    _ => null  // Let WTelegramClient handle other parameters
});

// Service creation with shared WTelegramClient instance
_authenticationHandler = new AuthenticationHandler(logger);
_channelService = new ChannelService(logger, _client);
_messageService = new MessageService(logger, _client);
```

## Performance and Scalability

### Memory Management
- **Streaming Downloads**: Large channels processed in configurable batches
- **Resource Disposal**: Proper cleanup of WTelegramClient and service instances
- **Event Unsubscription**: Prevents memory leaks through proper event handling

### Rate Limiting
- **Built-in Handling**: WTelegramClient handles Telegram rate limits automatically
- **Configurable Timeouts**: Request timeouts and retry strategies
- **Batch Size Control**: Configurable batch sizes for memory efficiency

### Progress Reporting
```csharp
public async Task<List<MessageData>> DownloadChannelMessagesAsync(
    ChannelInfo channelInfo, 
    IProgress<DownloadProgressInfo>? progress = null, 
    CancellationToken cancellationToken = default)
{
    var messages = new List<MessageData>();
    var totalMessages = channelInfo.MessageCount;
    var startTime = DateTime.UtcNow;
    
    await foreach (var batch in DownloadChannelMessagesBatchAsync(channelInfo, 100, null, cancellationToken))
    {
        messages.AddRange(batch);
        
        // Report progress
        var elapsed = DateTime.UtcNow - startTime;
        var progressInfo = new DownloadProgressInfo
        {
            TotalMessages = totalMessages,
            DownloadedMessages = messages.Count,
            MessagesPerSecond = messages.Count / elapsed.TotalSeconds,
            EstimatedTimeRemaining = CalculateETA(messages.Count, totalMessages, elapsed)
        };
        
        progress?.Report(progressInfo);
    }
    
    return messages;
}
```

## Security Considerations

### Credential Handling
- **No Persistence**: API credentials never stored permanently
- **Session Encryption**: WTelegramClient handles session encryption
- **Memory Safety**: Sensitive data cleared after operations
- **Logging Privacy**: Phone numbers and codes partially masked in logs

### Authentication Security
- **State Validation**: All authentication states properly validated
- **Timeout Handling**: Authentication timeouts handled gracefully
- **Error Sanitization**: Sensitive error details not exposed to upper layers

## Future Enhancements

### Planned Features
1. **Media Downloads**: Full media file downloading with progress tracking
2. **Advanced Filtering**: Message filtering by date, type, sender
3. **Batch Channel Operations**: Multiple channel processing
4. **Export Formats**: Additional export formats (HTML, CSV, XML)
5. **Offline Capabilities**: Channel metadata caching

### Architecture Improvements
1. **Repository Pattern**: Database integration for message storage
2. **Caching Layer**: Redis integration for session and metadata caching
3. **Background Processing**: Queue-based download processing
4. **Metrics Collection**: Detailed API usage and performance metrics
5. **Circuit Breaker**: Advanced resilience patterns

## Testing Strategy

### Unit Testing
- Mock WTelegramClient for isolated testing
- Test all authentication state transitions
- Validate error handling scenarios
- Test configuration validation logic

### Integration Testing
- Test actual Telegram API interactions (with test credentials)
- Verify session persistence and restoration
- Test channel discovery and validation
- Validate message download accuracy

### Performance Testing
- Large channel download performance
- Memory usage during batch processing
- Authentication flow performance
- Error recovery scenarios

## Development Guidelines

### API Design Principles
- **Interface Segregation**: Separate interfaces for different concerns
- **Clean Abstractions**: Hide WTelegramClient complexity
- **Event-Driven**: Use events for status updates and progress
- **Async/Await**: Full async support with cancellation tokens

### Error Handling Best Practices
- **Specific Exceptions**: Use typed exceptions for different error categories
- **Context Preservation**: Maintain error context through exception chaining
- **User-Friendly Messages**: Separate technical details from user messages
- **Comprehensive Logging**: Log all errors with structured data

### Resource Management
- **Disposal Patterns**: Implement IDisposable for resource cleanup
- **Event Cleanup**: Unsubscribe from events to prevent memory leaks
- **Client Lifecycle**: Properly manage WTelegramClient lifecycle
- **Session Management**: Handle session data securely

This TelegramApi layer documentation provides comprehensive guidance for AI assistants to understand and work effectively with the data access tier, enabling effective integration with Telegram's API while maintaining clean architecture principles.
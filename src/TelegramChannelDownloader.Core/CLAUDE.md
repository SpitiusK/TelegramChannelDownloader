# Claude.md - TelegramChannelDownloader.Core Layer

## Layer Overview

**Purpose**: The Core layer serves as the business logic tier in the Telegram Channel Downloader's clean 3-layer architecture. It orchestrates operations between the UI (Desktop layer) and data access (TelegramApi layer) while implementing all business rules, validation logic, export functionality, and download orchestration.

**Architecture Role**: 
- **Position**: Middle layer between Desktop and TelegramApi layers
- **Dependencies**: Only depends on TelegramApi layer through interfaces
- **Dependents**: Desktop layer consumes Core services through interfaces
- **Design Principle**: Contains no UI or external API concerns, purely business logic

**Key Responsibilities**:
- Download workflow orchestration and state management
- Business rule validation for all inputs and operations
- Export functionality for multiple output formats
- Progress tracking and reporting for download operations
- Error handling and recovery strategies
- Configuration management for business operations
- Integration between UI requests and API operations

## Technology Stack

**Framework**: .NET 8.0 Class Library
**Dependencies**:
- Microsoft.Extensions.DependencyInjection 8.0.0 (for service registration)
- Microsoft.Extensions.Logging 8.0.0 (for structured logging)
- Microsoft.Extensions.Caching.Memory (for caching validation results)
- TelegramChannelDownloader.TelegramApi (through interfaces only)

**Patterns Used**:
- Service Layer pattern for business logic encapsulation
- Repository pattern abstraction (future database integration)
- Command/Query separation for different operation types
- Event-driven architecture for status updates
- Options pattern for configuration management

## Project Structure

```
TelegramChannelDownloader.Core/
├── Services/                        # Core business services
│   ├── IDownloadService.cs          # Download orchestration interface
│   ├── DownloadService.cs           # Download workflow implementation
│   ├── IExportService.cs           # Export functionality interface
│   ├── ExportService.cs            # Multi-format export implementation
│   ├── IValidationService.cs       # Business validation interface
│   └── ValidationService.cs        # Comprehensive validation logic
├── Models/                         # Business domain models
│   ├── DownloadRequest.cs          # Download operation request
│   ├── DownloadResult.cs           # Download operation result
│   ├── DownloadStatus.cs           # Real-time download status
│   ├── ProgressInfo.cs             # Progress reporting data
│   ├── ExportOptions.cs            # Export configuration options
│   ├── ExportFormat.cs             # Export format enumeration
│   ├── ExportRequest.cs            # Export operation request
│   ├── ExportResult.cs             # Export operation result
│   ├── ValidationResult.cs         # Validation operation result
│   ├── ApiCredentials.cs           # API authentication credentials
│   ├── AppConfig.cs                # Application configuration
│   └── LogLevel.cs                 # Logging level enumeration
├── Exceptions/                     # Core layer custom exceptions
│   ├── TelegramCoreException.cs    # Base exception for core layer
│   ├── DownloadException.cs        # Download-related exceptions
│   ├── ExportException.cs          # Export-related exceptions
│   ├── ValidationException.cs      # Validation-related exceptions
│   └── ConfigurationException.cs   # Configuration-related exceptions
├── Extensions/                     # Service registration extensions
│   └── ServiceCollectionExtensions.cs  # DI container registration
└── TelegramChannelDownloader.Core.csproj  # Project configuration
```

## Core Services

### IDownloadService / DownloadService

**Purpose**: Orchestrates the complete download workflow from request validation through completion.

**Key Methods**:
- `DownloadChannelAsync()`: Main download orchestration method
- `ValidateDownloadRequestAsync()`: Pre-download validation
- `ValidateChannelAsync()`: Channel accessibility validation  
- `CancelDownloadAsync()`: Download cancellation handling
- `GetDownloadStatusAsync()`: Real-time status retrieval
- `EstimateDownloadSizeAsync()`: Pre-download size estimation

**Architecture Integration**:
```csharp
// Typical download flow
public async Task<DownloadResult> DownloadChannelAsync(
    DownloadRequest request, 
    IProgress<DownloadProgressInfo>? progress = null, 
    CancellationToken cancellationToken = default)
{
    // 1. Validate request through ValidationService
    var validation = await _validationService.ValidateDownloadRequestAsync(request);
    
    // 2. Get channel info through TelegramApi layer
    var channelInfo = await _telegramApiClient.GetChannelInfoAsync(request.ChannelUrl);
    
    // 3. Download messages with progress tracking
    var messages = await _telegramApiClient.DownloadChannelMessagesAsync(
        channelInfo, progress, cancellationToken);
    
    // 4. Export through ExportService
    var exportResult = await _exportService.ExportMessagesAsync(
        messages, request.ExportOptions);
    
    return new DownloadResult { /* populated result */ };
}
```

**State Management**: 
- Maintains active download tracking with unique IDs
- Fires `DownloadStatusChanged` events for UI updates
- Handles cancellation tokens for graceful shutdown

### IExportService / ExportService

**Purpose**: Handles conversion of downloaded messages to various output formats.

**Supported Formats**:
- **Markdown**: Rich text with formatting, links, and media references
- **JSON**: Structured data with full message metadata
- **Future formats**: CSV, HTML, Plain Text (extensible design)

**Key Features**:
- **Template-based Export**: Uses configurable templates for output formatting
- **Media Reference Handling**: Maintains links to downloaded media files
- **Metadata Preservation**: Includes timestamps, user info, forward history
- **Batch Processing**: Handles large message sets efficiently
- **Progress Reporting**: Provides export progress updates

**Usage Example**:
```csharp
var exportRequest = new ExportRequest
{
    Messages = downloadedMessages,
    Format = ExportFormat.Markdown,
    OutputPath = @"C:\Downloads\channel_export.md",
    Options = new ExportOptions
    {
        IncludeMedia = true,
        IncludeMetadata = true,
        GroupByDate = true
    }
};

var result = await _exportService.ExportMessagesAsync(exportRequest);
```

### IValidationService / ValidationService

**Purpose**: Centralized business rule validation for all Core layer operations.

**Validation Categories**:

1. **API Credentials Validation**:
   - API ID: Positive integer validation
   - API Hash: 32-character hexadecimal format
   - Phone Number: International format with country code

2. **Channel URL Validation**:
   - Telegram username format (@channel)
   - t.me URL format validation
   - Channel accessibility checking

3. **Download Request Validation**:
   - Output directory existence and permissions
   - Export format compatibility
   - Date range logical validation
   - File size and count limits

4. **Export Options Validation**:
   - Output path validity and permissions
   - Template existence and format
   - Media inclusion consistency checks

**Caching Strategy**:
- Validation results cached for 10 minutes (configurable)
- Cache keys based on validation input hash
- Memory-efficient LRU eviction policy

## Business Models

### DownloadRequest
**Purpose**: Encapsulates all parameters for a download operation.
```csharp
public class DownloadRequest
{
    public string ChannelUrl { get; set; }
    public string OutputDirectory { get; set; }
    public ExportFormat ExportFormat { get; set; }
    public DownloadOptions Options { get; set; }
    public string DownloadId { get; set; }
    public ApiCredentials? Credentials { get; set; }
}
```

### DownloadResult
**Purpose**: Contains the outcome and metrics of a download operation.
```csharp
public class DownloadResult
{
    public bool IsSuccess { get; set; }
    public string DownloadId { get; set; }
    public int TotalMessages { get; set; }
    public long TotalSize { get; set; }
    public TimeSpan Duration { get; set; }
    public string OutputPath { get; set; }
    public List<string> Errors { get; set; }
    public DownloadMetrics Metrics { get; set; }
}
```

### ProgressInfo
**Purpose**: Real-time progress reporting for download operations.
```csharp
public class DownloadProgressInfo
{
    public string DownloadId { get; set; }
    public string CurrentOperation { get; set; }
    public int ProcessedMessages { get; set; }
    public int TotalMessages { get; set; }
    public double ProgressPercentage { get; set; }
    public string EstimatedTimeRemaining { get; set; }
    public long BytesDownloaded { get; set; }
    public double DownloadSpeed { get; set; }
}
```

### ExportOptions
**Purpose**: Configuration for export operations.
```csharp
public class ExportOptions
{
    public bool IncludeMedia { get; set; }
    public bool IncludeMetadata { get; set; }
    public bool GroupByDate { get; set; }
    public string? CustomTemplate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int MaxFileSize { get; set; }
    public string DateTimeFormat { get; set; }
}
```

## Exception Handling Strategy

### Exception Hierarchy
```csharp
TelegramCoreException (base)
├── DownloadException (download-specific errors)
├── ExportException (export-specific errors)
├── ValidationException (validation failures)
└── ConfigurationException (configuration errors)
```

### Error Handling Patterns
```csharp
// Service-level error handling
try
{
    var result = await _telegramApiClient.GetChannelInfoAsync(channelUrl);
    return ValidationResult.Success(result);
}
catch (UnauthorizedAccessException ex)
{
    throw new ValidationException(
        "Channel access denied. Please verify authentication and permissions.",
        ex);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error validating channel {ChannelUrl}", channelUrl);
    throw new DownloadException(
        "An unexpected error occurred during channel validation.",
        ex);
}
```

## Configuration Management

### CoreServiceOptions
Comprehensive configuration for Core layer services:
```csharp
public class CoreServiceOptions
{
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
    public bool EnableConsoleLogging { get; set; } = true;
    public bool UseMemoryCaching { get; set; } = true;
    public int CacheExpirationMinutes { get; set; } = 10;
    public int MaxConcurrentDownloads { get; set; } = 3;
    public int DefaultBatchSize { get; set; } = 100;
    public bool EnablePerformanceLogging { get; set; } = false;
    public string? TempDirectory { get; set; }
    public long MaxExportFileSize { get; set; } = 0; // 0 = no limit
    public int DefaultTimeoutSeconds { get; set; } = 300; // 5 minutes
}
```

## Service Registration

### Dependency Injection Setup
The Core layer provides extension methods for clean service registration:

```csharp
// Basic registration
services.AddTelegramChannelDownloaderCore();

// Advanced registration with options
services.AddTelegramChannelDownloaderCore(options =>
{
    options.LogLevel = LogLevel.Debug;
    options.MaxConcurrentDownloads = 5;
    options.UseMemoryCaching = true;
    options.EnablePerformanceLogging = true;
});
```

### Service Lifetimes
- **ValidationService**: Singleton (stateless, thread-safe)
- **ExportService**: Scoped (may maintain state during operations)
- **DownloadService**: Scoped (maintains download state)

## Integration Patterns

### With Desktop Layer
The Core layer exposes clean interfaces consumed by ViewModels:
```csharp
// In AuthenticationViewModel
public async Task<ValidationResult> ValidateCredentialsAsync()
{
    var credentials = new ApiCredentials
    {
        ApiId = this.ApiId,
        ApiHash = this.ApiHash,
        PhoneNumber = this.PhoneNumber
    };
    
    return await _validationService.ValidateCredentialsAsync(credentials);
}
```

### With TelegramApi Layer
The Core layer orchestrates API operations without direct coupling:
```csharp
// Dependency injection ensures loose coupling
public DownloadService(
    ITelegramApiClient telegramApiClient,
    IExportService exportService,
    IValidationService validationService,
    ILogger<DownloadService> logger)
{
    // Implementation delegates to injected services
}
```

## Performance Considerations

### Memory Management
- **Streaming Processing**: Large message sets processed in configurable batches
- **Disposal Patterns**: All services implement proper resource disposal
- **Memory Caching**: Validation results cached with TTL and size limits

### Concurrency
- **Thread Safety**: All services designed for concurrent access
- **Async Patterns**: Full async/await support with cancellation tokens
- **Progress Reporting**: Lock-free progress reporting using immutable data

### Error Recovery
- **Retry Strategies**: Built-in retry logic for transient failures
- **Circuit Breaker**: Protection against cascading failures
- **Graceful Degradation**: Fallback behaviors when services are unavailable

## Testing Strategy

### Unit Testing Approach
- **Service Isolation**: Each service tested independently with mocked dependencies
- **Validation Logic**: Comprehensive test coverage for all validation rules
- **Exception Scenarios**: Test all error conditions and edge cases

### Integration Testing
- **Service Composition**: Test service interactions within the Core layer
- **Mock API Layer**: Use test doubles for TelegramApi dependencies
- **Configuration Scenarios**: Test different configuration combinations

### Recommended Test Structure
```
TelegramChannelDownloader.Core.Tests/
├── Services/
│   ├── DownloadServiceTests.cs
│   ├── ExportServiceTests.cs
│   └── ValidationServiceTests.cs
├── Models/
│   └── ValidationTests.cs
├── Extensions/
│   └── ServiceRegistrationTests.cs
└── Integration/
    └── ServiceCompositionTests.cs
```

## Future Enhancements

### Planned Features
1. **Database Integration**: Repository pattern for persistent storage
2. **Advanced Export Formats**: HTML, CSV, XML support
3. **Plugin Architecture**: Custom export format plugins
4. **Scheduling**: Automated download scheduling
5. **Filtering**: Advanced message filtering capabilities
6. **Compression**: Archive creation for large downloads

### Architecture Evolution
1. **CQRS Implementation**: Separate command and query responsibilities
2. **Event Sourcing**: Audit trail for all operations
3. **Distributed Caching**: Redis integration for multi-instance scenarios
4. **Metrics Collection**: Detailed performance and usage metrics

## Development Guidelines

### Coding Standards
- **Async/Await**: All public methods return Task or Task<T>
- **Cancellation Support**: CancellationToken parameters on all async methods
- **Null Safety**: Nullable reference types enabled throughout
- **Documentation**: XML documentation on all public APIs
- **Logging**: Structured logging with semantic information

### Error Handling Best Practices
- **Specific Exceptions**: Use typed exceptions for different error categories
- **Context Preservation**: Include relevant context in exception messages
- **Logging Strategy**: Log errors at appropriate levels with structured data
- **User-Friendly Messages**: Separate technical details from user messages

### Performance Guidelines
- **Batch Processing**: Process data in configurable batches
- **Resource Disposal**: Implement IDisposable/IAsyncDisposable where appropriate
- **Memory Efficiency**: Avoid loading entire datasets into memory
- **Caching Strategy**: Cache expensive operations with appropriate TTL

This Core layer documentation provides AI assistants with comprehensive understanding of the business logic tier, enabling effective development and maintenance of the Telegram Channel Downloader's core functionality.
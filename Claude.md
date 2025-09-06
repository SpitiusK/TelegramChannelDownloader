# Claude.md - Telegram Channel Downloader

## Project Overview

**Purpose**: A Windows Presentation Foundation (WPF) desktop application that enables users to download content from Telegram channels and chats. The application provides a graphical interface for authenticating with Telegram's API and downloading messages, media, and other content from specified channels.

**Technology Stack**:
- **Framework**: .NET 8.0 Windows
- **UI Framework**: WPF (Windows Presentation Foundation) with XAML
- **Telegram API**: WTelegramClient 3.7.1 (C# wrapper for Telegram's MTProto API)
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection 8.0.0
- **Hosting**: Microsoft.Extensions.Hosting 8.0.0
- **Architecture Pattern**: Clean 3-Layer Architecture with MVVM

**Key Features**:
- Multi-step Telegram authentication (phone number, verification code, 2FA)
- Real-time connection status and logging with color-coded display
- Input validation with real-time visual feedback and border coloring
- Progress tracking for download operations with detailed metrics
- Session persistence for authenticated users with automatic restoration
- Configurable output directory selection with browser dialog
- Channel URL validation and information preview
- Multiple export formats (Markdown, JSON with extensible architecture)
- Tabbed interface with Authentication, Download, Settings, and Log views
- Comprehensive error handling with user-friendly messages
- Auto-scrolling log display with level-based color coding
- Real-time property change notifications throughout UI

## Development Environment

### Prerequisites
- **Visual Studio 2022** or **JetBrains Rider** (recommended)
- **.NET 8.0 SDK** or later
- **Windows 10/11** (WPF is Windows-specific)
- **Telegram API Credentials** (API ID and Hash from https://my.telegram.org)

### Setup Instructions
1. Clone the repository
2. Open `TelegramChanelDowonloader.sln` in your IDE
3. Restore NuGet packages (`dotnet restore`)
4. Build the solution (`dotnet build`)
5. Run the application (`dotnet run` or F5 in IDE)

### Dependencies
**Core Layer**:
```xml
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Caching.Memory" Version="8.0.0" />
```

**Desktop Layer**:
```xml
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
```

**TelegramApi Layer**:
```xml
<PackageReference Include="WTelegramClient" Version="3.7.1" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
<PackageReference Include="System.Text.Json" Version="8.0.0" />
```

## Architecture Overview

The application has been transformed from a monolithic structure into a clean 3-layer architecture that promotes maintainability, testability, and extensibility:

### 3-Layer Architecture

```
TelegramChannelDownloader.Desktop (UI Layer)
    ↓ uses services from
TelegramChannelDownloader.Core (Business Logic Layer)
    ↓ uses services from  
TelegramChannelDownloader.TelegramApi (Data Access/API Layer)
```

**Layer Responsibilities:**
- **Desktop Layer**: WPF UI with MVVM pattern, user interaction handling
- **Core Layer**: Business logic, validation, download orchestration, export services
- **TelegramApi Layer**: Pure Telegram API integration, authentication, message handling

### Project Structure

```
TelegramChannelDownloader.sln
├── src/
│   ├── TelegramChannelDownloader.Desktop/     # UI Layer (WPF)
│   │   ├── App.xaml/App.xaml.cs              # Application entry point and DI setup
│   │   ├── MainWindow.xaml/xaml.cs           # Main tabbed interface
│   │   ├── ViewModels/                       # MVVM ViewModels
│   │   │   ├── MainViewModel.cs              # Main coordination ViewModel
│   │   │   ├── AuthenticationViewModel.cs    # Authentication tab logic
│   │   │   ├── DownloadViewModel.cs          # Download tab logic
│   │   │   └── SettingsViewModel.cs          # Settings tab logic
│   │   ├── Views/                           # User Controls for each tab
│   │   │   ├── AuthenticationView.xaml      # Authentication UI
│   │   │   ├── DownloadView.xaml            # Download configuration UI
│   │   │   ├── LogView.xaml                 # Log display UI
│   │   │   └── SettingsView.xaml            # Settings configuration UI
│   │   ├── Services/                        # UI-specific services
│   │   │   ├── IUIService.cs/UIService.cs   # UI interaction service
│   │   │   └── IDialogService.cs/DialogService.cs # Dialog management
│   │   └── Converters/                      # WPF value converters
│   │
│   ├── TelegramChannelDownloader.Core/       # Business Logic Layer
│   │   ├── Services/                        # Core business services
│   │   │   ├── IDownloadService.cs/DownloadService.cs
│   │   │   ├── IExportService.cs/ExportService.cs
│   │   │   └── IValidationService.cs/ValidationService.cs
│   │   ├── Models/                         # Business data models
│   │   │   ├── DownloadRequest.cs
│   │   │   ├── DownloadResult.cs
│   │   │   ├── ExportOptions.cs
│   │   │   └── ValidationResult.cs
│   │   └── Exceptions/                     # Custom exceptions
│   │       ├── DownloadException.cs
│   │       ├── ExportException.cs
│   │       └── ValidationException.cs
│   │
│   └── TelegramChannelDownloader.TelegramApi/  # API Integration Layer
│       ├── ITelegramApiClient.cs/TelegramApiClient.cs # Main API client
│       ├── Authentication/                  # Auth handling
│       │   ├── IAuthenticationHandler.cs/AuthenticationHandler.cs
│       │   └── Models/AuthenticationModels.cs
│       ├── Channels/                       # Channel operations
│       │   ├── IChannelService.cs/ChannelService.cs
│       │   └── Models/ChannelInfo.cs
│       ├── Messages/                       # Message operations
│       │   ├── IMessageService.cs/MessageService.cs
│       │   └── Models/MessageData.cs
│       ├── Session/                        # Session management
│       │   └── ISessionManager.cs/SessionManager.cs
│       └── Extensions/                     # Service registration
│           └── ServiceCollectionExtensions.cs

└── Original Monolithic Structure/          # Preserved for reference
    └── TelegramChannelDownloader/
├── MainWindow.xaml/xaml.cs        # Main application window UI and code-behind
├── Behaviors/                     # Custom WPF behaviors
│   └── AutoScrollBehavior.cs      # Auto-scroll behavior for log display
├── Commands/                      # Command pattern implementations
│   ├── AsyncRelayCommand.cs       # Async command wrapper for async operations
│   └── RelayCommand.cs           # Standard relay command implementation
├── Converters/                   # Value converters for data binding
│   ├── InverseBooleanToVisibilityConverter.cs
│   ├── LogLevelToColorConverter.cs
│   ├── StringToVisibilityConverter.cs
│   └── ValidationToBorderBrushConverter.cs
├── Models/                       # Data models and DTOs
│   ├── AuthenticationState.cs    # Enums and classes for auth state management
│   ├── LogEntry.cs              # Structured logging model
│   ├── LogLevel.cs              # Log level enumeration
│   ├── TelegramCredentials.cs   # API credentials and auth data
│   └── TelegramServiceExample.cs # Example/placeholder service
├── Services/                     # Business logic and external API integration
│   ├── ITelegramService.cs      # Telegram service interface
│   ├── TelegramService.cs       # WTelegramClient implementation
│   └── README.txt               # Services documentation placeholder
├── Utils/                       # Utility classes and helpers
│   ├── ObservableObject.cs     # Base class for INotifyPropertyChanged
│   └── ValidationHelper.cs     # Input validation utilities
├── ViewModels/                  # MVVM view models
│   └── MainViewModel.cs         # Main window view model with business logic
└── Views/                       # Additional views (currently empty)
```

## Architecture Patterns

### Clean 3-Layer Architecture
The application follows a clean architecture with strict separation of concerns:

**Layer Dependencies (following Dependency Inversion Principle):**
- Desktop Layer → Core Layer (through interfaces)
- Core Layer → TelegramApi Layer (through interfaces)
- No reverse dependencies allowed

**Benefits Achieved:**
- **Maintainability**: Each layer has focused responsibilities
- **Testability**: Layers can be tested in isolation with mocking
- **Extensibility**: Easy to add new features or swap implementations
- **Separation of Concerns**: UI, business logic, and data access are clearly separated

### MVVM in Desktop Layer
The UI layer follows MVVM pattern with enhanced organization:
- **Models**: Data transfer objects and display models
- **Views**: XAML UserControls organized by feature area
- **ViewModels**: Coordinating ViewModels that delegate to Core services

### Dependency Injection
Comprehensive DI setup across all layers:
```csharp
// App.xaml.cs - Service Registration
services.AddTelegramApi();              // TelegramApi layer services
services.AddTelegramChannelDownloaderCore(); // Core layer services
services.AddScoped<IUIService, UIService>(); // Desktop layer services
services.AddScoped<AuthenticationViewModel>(); // ViewModels
services.AddTransient<MainWindow>();          // Views
```

### Command Pattern and Async Operations
**Enhanced Command Implementation**:
```csharp
public ICommand ConnectCommand { get; }
ConnectCommand = new AsyncRelayCommand(ExecuteConnectAsync, CanExecuteConnect);

// Full async operation with error handling and progress
private async Task ExecuteConnectAsync()
{
    try
    {
        IsConnecting = true;
        var result = await _telegramApi.InitializeAsync(config);
        if (result.IsSuccess)
        {
            Status = result.AuthResult;
            AddLogMessage("Connection established successfully", LogLevel.Info);
        }
        else
        {
            AddLogMessage($"Connection failed: {result.ErrorMessage}", LogLevel.Error);
        }
    }
    catch (Exception ex)
    {
        AddLogMessage($"Connection error: {ex.Message}", LogLevel.Error);
        throw;
    }
    finally
    {
        IsConnecting = false;
    }
}
```

**Advanced Command Features**:
- **Cancellation Support**: CancellationToken integration
- **Progress Reporting**: IProgress<T> parameter support
- **Error Handling**: Comprehensive exception management
- **State Management**: UI state updates during operations
- **Thread Safety**: Proper UI thread marshalling

### State Management and Progress Tracking
**Authentication State Management**:
- `AuthenticationState` enum with complete state coverage
- `AuthenticationStatus` class with detailed user information
- Event-driven updates via `AuthenticationStatusChanged` event
- Cross-layer state propagation through dependency injection

**Download Progress Management**:
- `DownloadStatus` class with comprehensive progress tracking
- `DownloadPhase` enum covering all workflow stages
- Real-time progress reporting with metrics (speed, ETA, message counts)
- Cancellation token propagation through all layers
- Event-driven status updates with `DownloadStatusChanged` events

**Cross-Layer Communication**:
- Service-to-UI logging bridge via UILoggerProvider
- Progress reporting from TelegramApi to Core to Desktop layers
- Event aggregation in MainViewModel for centralized coordination
- Thread-safe property change notifications throughout UI

## Key Components by Layer

### TelegramApi Layer Components

#### TelegramApiClient (`TelegramApiClient.cs`)
- **Purpose**: Main facade for all Telegram API operations
- **Architecture**: Coordinates specialized service handlers
- **Key Services**:
  - `AuthenticationHandler`: Manages authentication flow
  - `ChannelService`: Handles channel operations
  - `MessageService`: Advanced message downloading with batch processing, progress tracking, and comprehensive error handling
- **Session Management**: Integrates with `SessionManager` for persistence

#### AuthenticationHandler (`Authentication/AuthenticationHandler.cs`)
- **Purpose**: Specialized service for Telegram authentication
- **Key Methods**:
  - `InitializeAsync()`: Sets up API connection
  - `AuthenticatePhoneAsync()`: Phone number authentication
  - `VerifyCodeAsync()`: SMS/app code verification
  - `VerifyTwoFactorAsync()`: 2FA password handling
- **State Management**: Tracks authentication state with events

### Core Layer Components

#### DownloadService (`Services/DownloadService.cs`)
- **Purpose**: Orchestrates the complete download workflow
- **Key Responsibilities**:
  - Channel validation and access verification
  - Download request processing and progress tracking
  - Integration between TelegramApi and export services
  - Error handling and recovery

#### ExportService (`Services/ExportService.cs`)
- **Purpose**: Handles message export to various formats
- **Supported Formats**: Markdown, JSON (extensible for more formats)
- **Features**: Rich formatting, media references, metadata inclusion

### Desktop Layer Components

#### MainViewModel (`ViewModels/MainViewModel.cs`)
- **Purpose**: Central coordination ViewModel for the application
- **Architecture**: Composes specialized ViewModels for each feature area
- **Key Responsibilities**:
  - Tab management and navigation
  - Cross-feature communication
  - Global application state management

#### AuthenticationViewModel (`ViewModels/AuthenticationViewModel.cs`)
- **Purpose**: Dedicated ViewModel for authentication flow
- **Features**: Step-by-step authentication, real-time validation, status display
- **Integration**: Delegates to Core layer services for business logic

### ValidationHelper (`Utils/ValidationHelper.cs`)
- **Purpose**: Centralized input validation logic
- **Validations Supported**:
  - API ID: Must be positive integer
  - API Hash: Must be 32-character hexadecimal string
  - Phone Number: 10-15 digits with flexible formatting
  - Verification Code: Exactly 5 digits
  - 2FA Password: 6-8 characters
  - Channel URL: Valid Telegram username or t.me link
  - Directory Path: Valid Windows file system path

### Authentication Models
```csharp
// AuthenticationState enum
public enum AuthenticationState
{
    Disconnected, Connecting, WaitingForPhoneNumber,
    WaitingForVerificationCode, WaitingForTwoFactorAuth,
    Authenticated, AuthenticationFailed, ConnectionError
}

// TelegramCredentials class
public class TelegramCredentials
{
    public int ApiId { get; set; }
    public string ApiHash { get; set; }
    public string PhoneNumber { get; set; }
    public string? VerificationCode { get; set; }
    public string? TwoFactorPassword { get; set; }
    public string? SessionData { get; set; }
}
```

## Development Workflow

### Git Workflow
- **Main Branch**: `master` 
- **Development Branch**: `development` (active development branch)
- **Commit Standards**: Conventional commits with clear descriptions
- **Recent Commits**: Major architecture refactoring and feature implementation completed

### Current Development Status
The project has undergone significant development with major milestones completed:

**Recent Major Updates** (from git history):
- `40dfa33`: Debug downloader - Final debugging and refinement
- `42cb93e`: Update README.md - Documentation updates  
- `b1f9da9`: Update Claude.md - Previous documentation update
- `5beed50`: Refactor: restructure with Clean Architecture - Major architectural overhaul
- `6d98584`: Initial release - Foundation implementation

**Architecture Transformation**:
The codebase has been completely restructured from a monolithic design to a clean 3-layer architecture with:
- Separation of concerns across Desktop, Core, and TelegramApi layers
- Comprehensive dependency injection setup
- Service-oriented architecture with interfaces
- Event-driven communication between layers

### Code Standards

#### C# Conventions
- **Naming**: PascalCase for public members, camelCase for private fields with underscore prefix
- **Async Methods**: Always end with "Async" suffix
- **Null Handling**: Nullable reference types enabled (`<Nullable>enable</Nullable>`)
- **Using Statements**: Implicit usings enabled for common namespaces
- **Documentation**: XML documentation comments for all public APIs

#### XAML Conventions
- **Naming**: PascalCase for x:Name attributes
- **Data Binding**: Use descriptive binding paths with proper StringFormat
- **Styles**: Centralized styles in Window.Resources with descriptive names
- **Converters**: Custom converters for complex data transformations

#### File Organization
- **One class per file** with matching filename
- **Namespace structure** matches folder structure
- **Interface implementations** in same folder as interface
- **Partial classes** for XAML code-behind only

### Testing Strategy
Currently the project lacks formal testing infrastructure. Recommended additions:
- **Unit Tests**: xUnit for ViewModels and Services
- **Integration Tests**: Test Telegram API interactions with mock services
- **UI Tests**: Consider WPF UI testing frameworks for automation

### Error Handling Patterns
```csharp
// Service layer error handling
try
{
    await _telegramService.SomeOperation();
    AddLogMessage("Operation successful", LogLevel.Info);
}
catch (Exception ex)
{
    Status = new AuthenticationStatus
    {
        State = AuthenticationState.AuthenticationFailed,
        Message = "User-friendly message",
        ErrorMessage = ex.Message
    };
    AddLogMessage($"Operation failed - {ex.Message}", LogLevel.Error);
    throw new InvalidOperationException("Detailed error context", ex);
}
finally
{
    IsConnecting = false; // Always reset state
}
```

## Configuration Management

### Application Settings
- **Session File**: `session.dat` for Telegram session persistence
- **Default Paths**: Documents/TelegramDownloads for downloads
- **API Configuration**: Provided at runtime through UI (not stored)

### WTelegramClient Configuration
Configuration handled through callback pattern:
```csharp
private string? Config(string what) => what switch
{
    "api_id" => _credentials.ApiId.ToString(),
    "api_hash" => _credentials.ApiHash,
    "phone_number" => _credentials.PhoneNumber,
    "verification_code" => _credentials.VerificationCode,
    "password" => _credentials.TwoFactorPassword,
    "session_pathname" => "session.dat",
    _ => null
};
```

### Environment Variables
Currently no environment variables are used. All configuration is provided through the UI.

## Logging and Monitoring

### Comprehensive Logging System
The application implements a sophisticated dual-layer logging system that bridges Microsoft.Extensions.Logging to the WPF UI:

**Logging Architecture**:
- **Microsoft.Extensions.Logging**: Standard .NET logging framework for service layer
- **UILoggerProvider**: Custom logger provider that bridges service logs to UI
- **UI Log Display**: Real-time log visualization with color coding and filtering
- **Log Rotation**: Configurable maximum entries with automatic cleanup

**UILoggerProvider Implementation**:
```csharp
// Bridge between Microsoft.Extensions.Logging and WPF UI
public class UILoggerProvider : ILoggerProvider
{
    private readonly Func<MainViewModel> _mainViewModelFactory;
    
    public ILogger CreateLogger(string categoryName)
    {
        return new UILogger(categoryName, _mainViewModelFactory);
    }
}

// Forwards service logs to UI with context and formatting
internal class UILogger : ILogger
{
    public void Log<TState>(
        Microsoft.Extensions.Logging.LogLevel logLevel, 
        EventId eventId, 
        TState state, 
        Exception? exception, 
        Func<TState, Exception?, string> formatter)
    {
        var mainViewModel = _mainViewModelFactory();
        var message = formatter(state, exception);
        var formattedMessage = $"[{GetCategoryShortName(_categoryName)}] {message}";
        var uiLogLevel = ConvertLogLevel(logLevel);
        mainViewModel.AddLogMessage(formattedMessage, uiLogLevel);
    }
}
```

**Service Integration**:
```csharp
// App.xaml.cs - Logging configuration
.ConfigureLogging((context, logging) =>
{
    logging.ClearProviders();
    logging.AddConsole(); // Development console logging
    
    // Add UI logger provider
    logging.Services.AddSingleton<ILoggerProvider>(serviceProvider =>
    {
        var mainViewModelFactory = new Func<MainViewModel>(() => 
            serviceProvider.GetRequiredService<MainViewModel>());
        return new UILoggerProvider(mainViewModelFactory);
    });
})
```

**Log Level Mapping**:
- Critical/Error → UI Error (Red)
- Warning → UI Warning (Orange)  
- Information/Debug/Trace → UI Info (Black)

**UI Integration Features**:
- **Category Context**: Service logs include class name context
- **Real-time Updates**: Logs appear immediately in UI
- **Thread Safety**: Safe cross-thread UI updates
- **Error Isolation**: Logging errors don't crash the application

## Security Considerations

### API Credentials
- **Storage**: API credentials are never persisted to disk in plain text
- **Session Management**: WTelegramClient handles session encryption automatically
- **Memory Safety**: Credentials cleared on disconnect
- **Validation**: Input validation prevents injection attacks

### Error Information
- **User Messages**: Sanitized error messages shown to users
- **Detailed Logging**: Full exception details in structured logs
- **Sensitive Data**: Phone numbers and codes not logged in full

## Common Issues and Solutions

### Authentication Issues
**Problem**: "Invalid API credentials"
**Solution**: Verify API ID and Hash from https://my.telegram.org are correct

**Problem**: "Phone number not accepted"
**Solution**: Ensure phone number includes country code (e.g., +1234567890)

**Problem**: "Session restoration failed"
**Solution**: Delete `session.dat` file and re-authenticate

### Connection Issues
**Problem**: "Connection timeout"
**Solutions**:
1. Check internet connection
2. Verify firewall settings allow the application
3. Try using VPN if Telegram is blocked in your region

**Problem**: "Client not initialized"
**Solution**: Always call `InitializeAsync()` before other operations

### Channel Access Issues
**Problem**: "CHANNEL_INVALID" errors
**Solution**: Enhanced error handling now provides specific guidance:
- Invalid access hash: Channel information not properly retrieved
- Channel not accessible: Verify permissions and authentication
- Private channel: Check if you have access to the channel

**Problem**: "FLOOD_WAIT" rate limiting
**Solution**: Automatic handling with configurable delays:
- MessageService extracts wait time from error message
- Automatic retry after specified wait period
- Progress reporting continues after wait completion

### Download Operation Issues
**Problem**: Memory issues with large channels
**Solution**: Batch processing implementation:
- Configurable batch sizes (default 100 messages)
- Memory-efficient streaming downloads
- Progress reporting per batch

**Problem**: Download cancellation
**Solution**: Comprehensive cancellation support:
- CancellationToken propagation through all layers
- Graceful cleanup of partial downloads
- Status tracking for cancelled operations

### Export and File Issues
**Problem**: Export format not supported
**Solution**: Enhanced export service supports:
- Markdown: Rich formatting with metadata
- JSON: Complete structured data
- CSV: Tabular format (planned)
- Extensible format architecture

**Problem**: File access permissions
**Solution**: Improved validation:
- Directory existence and write permission checks
- Disk space validation before download
- Automatic directory creation

### UI Responsiveness
**Problem**: UI freezing during operations
**Solution**: All long-running operations use `AsyncRelayCommand` to maintain UI thread responsiveness

### Logging and Debugging Issues
**Problem**: Missing service-level logging in UI
**Solution**: UILoggerProvider bridges all service logs to UI:
- Microsoft.Extensions.Logging integration
- Categorized log messages with source context
- Real-time log streaming to UI display

## Future Development

### Completed Recent Enhancements
1. ✅ **Complete Download Implementation**: Full message downloading with progress tracking
2. ✅ **Advanced Error Handling**: CHANNEL_INVALID and FLOOD_WAIT specific error handling
3. ✅ **Logging Bridge**: UILoggerProvider for service-to-UI log integration
4. ✅ **Export Functionality**: Markdown and JSON export with rich metadata
5. ✅ **Batch Processing**: Memory-efficient message downloading in configurable batches
6. ✅ **Progress Reporting**: Real-time download progress with detailed metrics
7. ✅ **Session Management**: Persistent session handling with WTelegramClient integration
8. ✅ **Cancellation Support**: Comprehensive cancellation token propagation
9. ✅ **Validation Framework**: Multi-layer validation with detailed error reporting
10. ✅ **Status Tracking**: Real-time download status monitoring

### Current Implementation Status
**Fully Implemented**:
- Complete 3-layer clean architecture
- Full authentication workflow with all states
- Message downloading with batch processing and progress reporting
- Export to Markdown and JSON formats with rich metadata
- Channel validation and information retrieval
- Real-time logging with service-to-UI bridge
- Comprehensive error handling and recovery
- Progress tracking and cancellation support
- Session persistence and restoration

**In Active Development**:
- Media file downloading (architecture ready)
- CSV export format (service interface ready)
- Advanced filtering options (validation framework ready)
- Settings persistence (UI components implemented)

### Next Phase Planned Features
1. **Media Handling**: Download and organize images, videos, documents
2. **Advanced Filtering**: Date ranges, message types, sender filters
3. **Batch Channel Operations**: Multiple channel downloads with queue management
4. **Export Format Extensions**: HTML, Plain Text, custom templates
5. **Performance Optimizations**: Parallel downloads, caching strategies
6. **UI Enhancements**: Dark mode, advanced progress visualization

### Technical Debt and Improvements
1. **Testing**: Add comprehensive unit and integration test coverage
2. **Performance**: Optimize large channel handling and memory usage
3. **Caching**: Implement channel metadata caching
4. **Configuration**: Enhanced settings persistence and management
5. **Accessibility**: Screen reader support and keyboard navigation

### Architecture Improvements
1. **Repository Pattern**: Abstract data access for future database integration
2. **Plugin Architecture**: Support for different download formats/destinations
3. **Offline Mode**: Cache channel information for offline browsing
4. **Multi-threading**: Parallel downloads with proper synchronization

## Layer-Specific Documentation

Each layer has its own dedicated Claude.md file for detailed technical specifications:

- **[Core Layer Documentation](src/TelegramChannelDownloader.Core/CLAUDE.md)**: Comprehensive guide to the business logic layer, including services, models, validation, export functionality, and integration patterns.

- **[Desktop Layer Documentation](src/TelegramChannelDownloader.Desktop/CLAUDE.md)**: Complete WPF UI implementation guide covering MVVM architecture, ViewModels, Views, data binding, commands, converters, and user experience design.

- **[TelegramApi Layer Documentation](src/TelegramChannelDownloader.TelegramApi/CLAUDE.md)**: Detailed data access layer documentation including Telegram API integration, authentication flow, session management, channel operations, and message handling.

These layer-specific documentation files provide deep technical details for working with each architectural layer independently while maintaining understanding of the overall system design.

## Summary

This documentation provides a comprehensive guide for AI assistants to understand and work effectively with the Telegram Channel Downloader project. The codebase is well-structured, follows modern C# and WPF practices, implements clean 3-layer architecture principles, and provides a solid foundation for further development.
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
- Real-time connection status and logging
- Input validation with visual feedback
- Progress tracking for download operations
- Session persistence for authenticated users
- Configurable output directory selection

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
```xml
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
<PackageReference Include="WTelegramClient" Version="3.7.1" />
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

### Command Pattern
Async operations use `AsyncRelayCommand` for proper async/await handling:
```csharp
public ICommand ConnectCommand { get; }
ConnectCommand = new AsyncRelayCommand(ExecuteConnectAsync, CanExecuteConnect);
```

### State Management
Authentication state is managed through:
- `AuthenticationState` enum for current auth step
- `AuthenticationStatus` class for detailed status with user info
- Event-driven updates via `AuthenticationStatusChanged` event

## Key Components by Layer

### TelegramApi Layer Components

#### TelegramApiClient (`TelegramApiClient.cs`)
- **Purpose**: Main facade for all Telegram API operations
- **Architecture**: Coordinates specialized service handlers
- **Key Services**:
  - `AuthenticationHandler`: Manages authentication flow
  - `ChannelService`: Handles channel operations
  - `MessageService`: Manages message downloading and export
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
- **Main Branch**: `master` (current working branch)
- **Commit Standards**: Use descriptive commit messages following conventional commits
- **File Handling**: Several files are staged but not committed yet (.gitignore, .idea files, solution file)

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

### Logging System
- **Structured Logging**: `LogEntry` class with timestamp, level, and message
- **Log Levels**: Info, Warning, Error (defined in `LogLevel` enum)
- **UI Integration**: Real-time log display with color coding
- **Log Rotation**: Maximum 100 entries in memory, 50 in text display

### Log Implementation
```csharp
private void AddLogMessage(string message, LogLevel level = LogLevel.Info)
{
    var logEntry = new LogEntry(level, message);
    _dispatcher.BeginInvoke(() =>
    {
        _logEntries.Add(logEntry);
        // Maintain maximum log entries limit
        while (_logEntries.Count > MaxLogEntries)
        {
            _logEntries.RemoveAt(0);
        }
    });
}
```

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

### UI Responsiveness
**Problem**: UI freezing during operations
**Solution**: All long-running operations use `AsyncRelayCommand` to maintain UI thread responsiveness

## Future Development

### Planned Features (Based on Current Architecture)
1. **Download Implementation**: Complete the download functionality (currently simulated)
2. **Media Handling**: Support for downloading images, videos, documents
3. **Filtering Options**: Date ranges, message types, user filters
4. **Export Formats**: JSON, CSV, HTML export options
5. **Batch Operations**: Multiple channel downloads
6. **Settings Persistence**: Save user preferences and API credentials securely

### Technical Debt
1. **Session Management**: Improve session data handling beyond placeholder implementation
2. **Testing**: Add comprehensive unit and integration test coverage
3. **Configuration**: Add proper settings management
4. **Performance**: Implement progress cancellation and background processing
5. **Error Handling**: More granular error categorization and recovery

### Architecture Improvements
1. **Repository Pattern**: Abstract data access for future database integration
2. **Plugin Architecture**: Support for different download formats/destinations
3. **Offline Mode**: Cache channel information for offline browsing
4. **Multi-threading**: Parallel downloads with proper synchronization

This documentation provides a comprehensive guide for AI assistants to understand and work effectively with the Telegram Channel Downloader project. The codebase is well-structured, follows modern C# and WPF practices, and provides a solid foundation for further development.
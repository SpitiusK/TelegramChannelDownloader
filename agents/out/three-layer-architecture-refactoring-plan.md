# Three-Layer Architecture Refactoring Plan for Telegram Channel Downloader

## Overview

This document provides a comprehensive plan to refactor the current working Telegram Channel Downloader MVP from a monolithic WPF application into a clean three-layer architecture with separate projects for UI, business logic, and Telegram API integration.

## Current Architecture Analysis

The existing project has a well-structured MVVM pattern but everything is contained in a single project. Key components include:

- **UI Layer**: WPF Views, ViewModels, Commands, Converters, Behaviors
- **Business Logic**: Mixed into ViewModels and Services
- **Telegram Integration**: TelegramService with WTelegramClient dependency
- **Data Models**: Various models and utilities
- **Infrastructure**: Started but incomplete Entity Framework setup

## Proposed 3-Layer Architecture

### 1. **TelegramChannelDownloader.TelegramApi** (DLL Library)
**Purpose**: Pure Telegram API integration layer - completely UI-agnostic

**Responsibilities**:
- Telegram API communication using WTelegramClient
- Authentication handling
- Message retrieval and processing
- Session management
- Channel information retrieval
- Raw data conversion from Telegram objects

**Key Components**:
```
TelegramChannelDownloader.TelegramApi/
├── ITelegramApiClient.cs              # Main API interface
├── TelegramApiClient.cs               # WTelegramClient implementation
├── Authentication/
│   ├── IAuthenticationHandler.cs      # Auth operations interface
│   ├── AuthenticationHandler.cs       # Auth implementation
│   └── Models/                        # Auth-specific models
├── Channels/
│   ├── IChannelService.cs             # Channel operations interface
│   ├── ChannelService.cs              # Channel implementation
│   └── Models/                        # Channel-specific models
├── Messages/
│   ├── IMessageService.cs             # Message operations interface
│   ├── MessageService.cs              # Message implementation
│   └── Models/                        # Message-specific models
├── Session/
│   ├── ISessionManager.cs             # Session persistence interface
│   └── SessionManager.cs              # Session implementation
└── Configuration/
    ├── TelegramApiConfig.cs           # API configuration
    └── TelegramApiSettings.cs         # Settings model
```

### 2. **TelegramChannelDownloader.Core** (Business Logic Library)
**Purpose**: Application business logic, orchestration, and domain services

**Responsibilities**:
- Download orchestration and workflow management
- Progress tracking and reporting
- File export operations (Markdown, JSON, etc.)
- Validation logic
- Error handling and recovery
- Data transformation and processing
- Configuration management

**Key Components**:
```
TelegramChannelDownloader.Core/
├── Services/
│   ├── IDownloadService.cs            # Main download orchestration
│   ├── DownloadService.cs             # Implementation
│   ├── IExportService.cs              # Export operations interface
│   ├── ExportService.cs               # Export implementation
│   ├── IValidationService.cs          # Validation interface
│   └── ValidationService.cs           # Validation implementation
├── Models/
│   ├── DownloadRequest.cs             # Download operation model
│   ├── DownloadResult.cs              # Download result model
│   ├── ExportRequest.cs               # Export operation model
│   ├── ValidationResult.cs            # Validation result model
│   └── ProgressInfo.cs                # Progress reporting model
├── Configuration/
│   ├── AppConfig.cs                   # Application configuration
│   └── DownloadSettings.cs            # Download-specific settings
├── Exceptions/
│   ├── DownloadException.cs           # Custom exceptions
│   ├── ValidationException.cs         
│   └── ExportException.cs
└── Extensions/
    ├── StringExtensions.cs            # Utility extensions
    └── FileExtensions.cs
```

### 3. **TelegramChannelDownloader.Desktop** (WPF UI Application)
**Purpose**: User interface layer using WPF with MVVM pattern

**Responsibilities**:
- User interface presentation
- User input handling
- Progress visualization
- Error display
- Configuration UI
- File system operations (directory selection)

**Key Components**:
```
TelegramChannelDownloader.Desktop/
├── App.xaml/App.xaml.cs               # Application entry point & DI setup
├── Views/
│   ├── MainWindow.xaml/xaml.cs        # Main application window
│   ├── AuthenticationView.xaml/xaml.cs # Auth UI component
│   ├── DownloadView.xaml/xaml.cs      # Download UI component
│   └── SettingsView.xaml/xaml.cs      # Settings UI component
├── ViewModels/
│   ├── MainViewModel.cs               # Main window VM
│   ├── AuthenticationViewModel.cs     # Authentication VM
│   ├── DownloadViewModel.cs           # Download operations VM
│   └── SettingsViewModel.cs           # Settings management VM
├── Commands/
│   ├── AsyncRelayCommand.cs           # Async command implementation
│   └── RelayCommand.cs                # Standard command implementation
├── Converters/
│   ├── [All existing converters]      # UI data converters
├── Behaviors/
│   ├── AutoScrollBehavior.cs          # UI behaviors
├── Services/
│   ├── IUIService.cs                  # UI-specific operations
│   ├── UIService.cs                   # Implementation
│   ├── IDialogService.cs              # Dialog operations
│   └── DialogService.cs               # Implementation
└── Utils/
    ├── ObservableObject.cs            # Base VM class
    └── UIHelpers.cs                   # UI utility methods
```

## Layer Boundaries and Communication Patterns

### Interface Definitions

**1. TelegramApi Layer Interfaces**
```csharp
// Primary interface for Telegram operations
public interface ITelegramApiClient
{
    Task<AuthResult> AuthenticateAsync(AuthRequest request);
    Task<ChannelInfo> GetChannelInfoAsync(string channelUrl);
    Task<MessageBatch> GetMessagesAsync(ChannelInfo channel, MessageRequest request);
    Task DisconnectAsync();
    event EventHandler<AuthStatusChangedEventArgs> AuthenticationStatusChanged;
}

// Authentication-specific interface
public interface IAuthenticationHandler
{
    Task<AuthResult> InitializeAsync(TelegramCredentials credentials);
    Task<AuthResult> AuthenticatePhoneAsync(string phoneNumber);
    Task<AuthResult> VerifyCodeAsync(string verificationCode);
    Task<AuthResult> VerifyTwoFactorAsync(string password);
}
```

**2. Core Layer Interfaces**
```csharp
// Main download orchestration interface
public interface IDownloadService
{
    Task<DownloadResult> DownloadChannelAsync(DownloadRequest request, IProgress<ProgressInfo> progress);
    Task<ValidationResult> ValidateRequestAsync(DownloadRequest request);
    Task CancelDownloadAsync(string downloadId);
}

// Export operations interface
public interface IExportService
{
    Task<ExportResult> ExportToMarkdownAsync(ExportRequest request);
    Task<ExportResult> ExportToJsonAsync(ExportRequest request);
    Task<ExportResult> ExportToCsvAsync(ExportRequest request);
}
```

**3. Desktop Layer Interfaces**
```csharp
// UI-specific services
public interface IUIService
{
    Task ShowErrorAsync(string title, string message);
    Task<bool> ShowConfirmationAsync(string title, string message);
    Task<string> SelectDirectoryAsync(string currentPath);
}

public interface IDialogService
{
    void ShowNotification(string message, NotificationType type);
    Task<MessageBoxResult> ShowMessageBoxAsync(string title, string message, MessageBoxButton buttons);
}
```

### Dependency Flow

```
Desktop Layer (UI)
    ↓ (depends on)
Core Layer (Business Logic)
    ↓ (depends on)
TelegramApi Layer (External Integration)
```

**Key Principles**:
- **Desktop** layer only references **Core** layer
- **Core** layer only references **TelegramApi** layer  
- **TelegramApi** layer has no dependencies on other layers
- All communication through interfaces using dependency injection

## Detailed Implementation Steps

### Phase 1: Project Structure Setup (1-2 hours)

**Step 1.1: Create New Project Structure**
```powershell
# Create new solution structure
mkdir src
mkdir src\TelegramChannelDownloader.TelegramApi
mkdir src\TelegramChannelDownloader.Core  
mkdir src\TelegramChannelDownloader.Desktop

# Create new project files
dotnet new classlib -n TelegramChannelDownloader.TelegramApi -o src\TelegramChannelDownloader.TelegramApi --framework net8.0
dotnet new classlib -n TelegramChannelDownloader.Core -o src\TelegramChannelDownloader.Core --framework net8.0
dotnet new wpf -n TelegramChannelDownloader.Desktop -o src\TelegramChannelDownloader.Desktop --framework net8.0-windows

# Create new solution and add projects
dotnet new sln -n TelegramChannelDownloader
dotnet sln add src\TelegramChannelDownloader.TelegramApi\TelegramChannelDownloader.TelegramApi.csproj
dotnet sln add src\TelegramChannelDownloader.Core\TelegramChannelDownloader.Core.csproj  
dotnet sln add src\TelegramChannelDownloader.Desktop\TelegramChannelDownloader.Desktop.csproj
```

**Step 1.2: Configure Project Dependencies**
- **TelegramApi**: Add WTelegramClient 3.7.1
- **Core**: Reference TelegramApi, add Microsoft.Extensions.* packages
- **Desktop**: Reference Core, add WPF packages and Microsoft.Extensions.Hosting

**Step 1.3: Update Solution File**
- Remove old monolithic project reference
- Ensure new projects are properly configured

### Phase 2: TelegramApi Layer Implementation (4-6 hours)

**Step 2.1: Extract Telegram Service** 
- Move current `ITelegramService.cs` and `TelegramService.cs` to TelegramApi project
- Rename to `ITelegramApiClient` and `TelegramApiClient`
- Remove UI-specific dependencies and events

**Step 2.2: Create Authentication Handler**
- Extract authentication methods from TelegramService
- Create `IAuthenticationHandler` and `AuthenticationHandler`
- Implement session management

**Step 2.3: Create Specialized Services**
- Extract channel operations → `IChannelService`/`ChannelService`
- Extract message operations → `IMessageService`/`MessageService`
- Ensure all services are stateless and testable

**Step 2.4: Create Data Models**
- Move and refactor existing models (TelegramCredentials, ChannelInfo, etc.)
- Remove UI-specific properties
- Add new models for service communication

### Phase 3: Core Layer Implementation (6-8 hours)

**Step 3.1: Create Download Orchestration**
- Create `IDownloadService` and `DownloadService`
- Move download logic from MainViewModel
- Implement progress tracking and cancellation

**Step 3.2: Create Export Services**
- Extract markdown export logic from TelegramService
- Create `IExportService` with multiple format support
- Implement file system operations

**Step 3.3: Create Validation Service**
- Move validation logic from ValidationHelper
- Create comprehensive validation for all operations
- Implement business rule validation

**Step 3.4: Error Handling and Configuration**
- Create custom exception types
- Implement configuration management
- Add logging infrastructure

### Phase 4: Desktop Layer Refactoring (6-8 hours)

**Step 4.1: Refactor ViewModels**
- Split MainViewModel into focused ViewModels:
  - `AuthenticationViewModel` - authentication flow
  - `DownloadViewModel` - download operations  
  - `SettingsViewModel` - application settings
- Remove direct service calls, use Core layer services instead

**Step 4.2: Create UI Services**
- Create `IUIService` for UI-specific operations
- Create `IDialogService` for dialogs and notifications
- Move file system UI operations (directory selection) to UI services

**Step 4.3: Update Views and Controls**
- Split MainWindow into smaller, focused UserControls
- Update data bindings for new ViewModel structure
- Ensure proper MVVM separation

**Step 4.4: Configure Dependency Injection**
- Update App.xaml.cs for new service registrations
- Configure service lifetimes appropriately
- Ensure proper disposal of resources

### Phase 5: Integration and Testing (4-6 hours)

**Step 5.1: Integration Testing**
- Test all layer boundaries
- Verify dependency injection configuration
- Test error handling across layers

**Step 5.2: Functionality Verification**
- Test complete authentication flow
- Test download operations
- Test export functionality
- Test cancellation and error scenarios

**Step 5.3: Performance Validation**
- Verify no performance regression
- Test memory usage with large downloads
- Test UI responsiveness

**Step 5.4: Documentation Updates**
- Update CLAUDE.md with new architecture
- Document layer responsibilities
- Update development workflow

## Migration Strategy

### Parallel Development Approach
1. **Keep existing project working** during refactoring
2. **Create new layers incrementally** 
3. **Migrate functionality piece by piece**
4. **Test each migration step thoroughly**
5. **Switch to new architecture only when complete**

### Risk Mitigation
- **Branch-based development** - work in feature branch
- **Incremental testing** - test each extracted component
- **Rollback plan** - maintain original working version
- **Configuration management** - externalize settings for easy switching

### Verification Checklist
- [ ] All original functionality preserved
- [ ] Authentication flow works identically  
- [ ] Download operations perform the same
- [ ] Export functionality unchanged
- [ ] Error handling maintains user experience
- [ ] Performance characteristics maintained
- [ ] Memory usage patterns similar
- [ ] UI responsiveness preserved

## Benefits of This Architecture

**1. Separation of Concerns**
- **TelegramApi**: Pure external integration, no business logic
- **Core**: Business logic independent of UI framework  
- **Desktop**: UI presentation without business logic complexity

**2. Testability**
- Each layer can be unit tested independently
- TelegramApi can be mocked for Core layer tests
- Core can be mocked for UI tests

**3. Maintainability** 
- Clear boundaries reduce coupling
- Changes in one layer have minimal impact on others
- Easier to locate and fix issues

**4. Extensibility**
- Easy to add new UI frameworks (Console, Web, etc.)
- Easy to swap Telegram client implementations
- Easy to add new export formats or business features

**5. Deployment Flexibility**
- TelegramApi can be packaged as reusable library
- Core business logic can be shared across applications
- Desktop UI can be updated independently

## Estimated Timeline

- **Phase 1 (Setup)**: 1-2 hours
- **Phase 2 (TelegramApi)**: 4-6 hours  
- **Phase 3 (Core)**: 6-8 hours
- **Phase 4 (Desktop)**: 6-8 hours
- **Phase 5 (Integration)**: 4-6 hours

**Total Estimated Time**: 21-30 hours over 1-2 weeks

## Package Dependencies

### TelegramChannelDownloader.TelegramApi
```xml
<PackageReference Include="WTelegramClient" Version="3.7.1" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
```

### TelegramChannelDownloader.Core  
```xml
<ProjectReference Include="..\TelegramChannelDownloader.TelegramApi\TelegramChannelDownloader.TelegramApi.csproj" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
<PackageReference Include="System.Text.Json" Version="8.0.0" />
```

### TelegramChannelDownloader.Desktop
```xml
<ProjectReference Include="..\TelegramChannelDownloader.Core\TelegramChannelDownloader.Core.csproj" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
```

## Summary

This plan provides a systematic approach to refactoring your working MVP into a clean, maintainable three-layer architecture while preserving all existing functionality. The architecture follows enterprise patterns and will serve as a solid foundation for future development.

The separation ensures:
- **Clear responsibilities** for each layer
- **Easy testing** and maintenance
- **Future extensibility** for new features and UI frameworks
- **Professional architecture** suitable for enterprise deployment

Following this plan will transform your current monolithic application into a well-structured, professional-grade software solution that can easily evolve and scale with your requirements.
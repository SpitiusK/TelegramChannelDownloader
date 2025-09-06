# Telegram Channel Downloader

A **production-ready** Windows desktop application for downloading and exporting content from Telegram channels to structured formats. Built with modern .NET 8 and WPF, featuring clean architecture, comprehensive error handling, and real-time progress tracking. This professional tool provides an intuitive interface for authenticating with Telegram's API and efficiently downloading messages, media metadata, and channel information.

![Platform](https://img.shields.io/badge/platform-Windows-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![WPF](https://img.shields.io/badge/WPF-Windows-blue)
![License](https://img.shields.io/badge/license-MIT-green)

## Features

### 🔐 **Secure Authentication**
- Multi-step Telegram authentication flow (phone number, SMS verification, 2FA)
- Automatic session persistence for convenience
- Secure credential handling with no plain-text storage
- Real-time authentication status and user information display

### 📱 **Channel Management**
- Real-time channel validation and information retrieval
- Support for both public channels (@username) and t.me links
- Display of channel metadata (title, description, subscriber count)
- Visual confirmation before download initiation

### 📥 **Advanced Download System**
- **Batch Processing**: Memory-efficient downloading with configurable batch sizes
- **Real-time Progress**: Live tracking with speed, ETA, and detailed metrics
- **Export Formats**: Structured Markdown and JSON with rich metadata
- **Error Recovery**: Automatic retry for rate limits (FLOOD_WAIT) and network issues
- **Robust Validation**: Comprehensive channel access and authentication verification
- **Background Processing**: Non-blocking downloads with full cancellation support

### 🎨 **Professional UI & Logging**
- **Modern WPF Interface**: Clean, responsive design with professional styling
- **Real-time Validation**: Instant feedback with color-coded input validation
- **Comprehensive Logging**: Integrated logging bridge displaying service-level operations
- **Live Status Updates**: Real-time authentication, connection, and download status
- **Progressive Disclosure**: Context-sensitive UI that shows relevant options
- **Auto-scrolling Log**: Real-time operation monitoring with color-coded levels

## Screenshots

*Note: Screenshots will be added upon first release*

## Installation

### Prerequisites

- **Operating System**: Windows 10 or Windows 11
- **.NET Runtime**: .NET 8.0 Desktop Runtime ([Download here](https://dotnet.microsoft.com/download/dotnet/8.0))
- **Telegram API Credentials**: API ID and Hash from [my.telegram.org](https://my.telegram.org)

### Getting API Credentials

1. Visit [my.telegram.org](https://my.telegram.org) and log in with your Telegram account
2. Navigate to "API development tools"
3. Create a new application (if you haven't already):
   - **App title**: Choose any name (e.g., "Channel Downloader")
   - **Short name**: Choose a short identifier
   - **Platform**: Desktop
   - **Description**: Optional description
4. Note down your **API ID** (integer) and **API Hash** (32-character string)
5. Keep these credentials secure and do not share them

### Installation

#### Build from Source
1. **Clone the repository**:
   ```bash
   git clone https://github.com/your-username/TelegramChannelDownloader.git
   cd TelegramChannelDownloader
   ```

2. **Install .NET 8 SDK** (if not already installed):
   - Download from [Microsoft .NET](https://dotnet.microsoft.com/download/dotnet/8.0)

3. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

4. **Build the application**:
   ```bash
   dotnet build --configuration Release
   ```

5. **Run the application**:
   ```bash
   dotnet run --project TelegramChannelDownloader
   ```

## Usage Guide

### First-Time Setup

1. **Launch the application**
2. **Enter API Credentials**:
   - Input your API ID (numbers only)
   - Input your API Hash (32-character hexadecimal string)
3. **Click "Connect"** to initiate authentication

### Authentication Process

1. **Phone Number**: Enter your Telegram-registered phone number (with country code)
2. **Verification Code**: Enter the 5-digit code received via SMS or Telegram app
3. **Two-Factor Authentication** (if enabled): Enter your 2FA password
4. **Success**: You'll see your user information and "Connected" status

### Downloading Channel Content

1. **Enter Channel Information**:
   - Public channels: `@channelname` or `channelname`
   - Channel links: `https://t.me/channelname`
2. **Validate Channel**: Click "Validate" to verify and display channel information
3. **Select Output Directory**: Choose where to save the downloaded files
4. **Start Download**: Click "Download" to begin the process
5. **Monitor Progress**: Watch real-time progress, speed, and estimated completion time

### Output Formats

The application supports multiple export formats with rich metadata:

#### **Markdown Export** (Primary Format)
```markdown
# Channel Name
**Description**: Channel description
**Channel**: @channelname
**URL**: https://t.me/channelname
**Messages**: 1,234
**Exported**: 2024-12-27 10:30:00 UTC

---

## Message 123
**Date**: 2024-12-27 09:15:23 UTC
**Sender**: Channel Name
**Views**: 1,542

Message content here with **formatting** preserved...

**Media Type**: Photo
**File**: photo_123.jpg  
**Size**: 1.2 MB
**Caption**: Photo caption if present

**Links:**
- https://example.com

**Mentions:**
- @username

**Hashtags:**
- #telegram

---
```

#### **JSON Export** (Structured Data)
```json
{
  "channelInfo": {
    "title": "Channel Name",
    "username": "channelname", 
    "description": "Channel description",
    "url": "https://t.me/channelname"
  },
  "messages": [
    {
      "messageId": 123,
      "timestamp": "2024-12-27T09:15:23Z",
      "senderDisplayName": "Channel Name",
      "content": "Message content...",
      "messageType": "Photo",
      "views": 1542,
      "media": {
        "fileName": "photo_123.jpg",
        "fileSize": 1258291,
        "mimeType": "image/jpeg"
      },
      "links": ["https://example.com"],
      "mentions": ["username"],
      "hashtags": ["telegram"]
    }
  ],
  "exportInfo": {
    "totalMessages": 1234,
    "exportDate": "2024-12-27T10:30:00Z",
    "format": "json"
  }
}
```

## Technical Architecture

### Technology Stack
- **Framework**: .NET 8.0 Windows
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Architecture Pattern**: Clean 3-Layer Architecture with MVVM
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection
- **Telegram API**: WTelegramClient 3.7.1

### Clean Architecture Overview

The application follows a clean 3-layer architecture that promotes maintainability, testability, and extensibility:

```
TelegramChannelDownloader.Desktop (UI Layer)
    ↓ uses services from
TelegramChannelDownloader.Core (Business Logic Layer)
    ↓ uses services from  
TelegramChannelDownloader.TelegramApi (Data Access/API Layer)
```

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
```

### Key Components by Layer

#### Desktop Layer (UI)
- **MainViewModel**: Central coordination ViewModel with cross-layer communication
- **AuthenticationViewModel**: Complete authentication flow with real-time validation
- **DownloadViewModel**: Download configuration, progress tracking, and status management
- **UILoggerProvider**: Logging bridge between Microsoft.Extensions.Logging and WPF UI
- **UIService**: UI interaction abstraction for dialogs and notifications
- **AsyncRelayCommand**: Thread-safe async command implementation

#### Core Layer (Business Logic)  
- **DownloadService**: Complete download orchestration with phase management
- **ExportService**: Multi-format export (Markdown, JSON) with rich metadata
- **ValidationService**: Comprehensive business rule validation with caching
- **Progress Tracking**: Real-time download metrics and phase reporting

#### TelegramApi Layer (Data Access)
- **TelegramApiClient**: Complete Telegram API facade with session management
- **AuthenticationHandler**: Robust multi-step authentication with error recovery
- **ChannelService**: Channel operations with access validation and caching
- **MessageService**: Batch message downloading with FLOOD_WAIT handling and progress tracking
- **SessionManager**: Secure session persistence and restoration

## Dependencies

### NuGet Packages
- **WTelegramClient** (3.7.1): C# wrapper for Telegram's MTProto API with full protocol support
- **Microsoft.Extensions.DependencyInjection** (8.0.0): Dependency injection container for clean architecture
- **Microsoft.Extensions.Hosting** (8.0.0): Application hosting with integrated logging
- **Microsoft.Extensions.Logging** (8.0.0): Structured logging with multiple providers
- **Microsoft.Extensions.Caching.Memory** (8.0.0): High-performance in-memory caching
- **System.Text.Json** (8.0.0): Modern JSON serialization for export functionality

### System Requirements
- **OS**: Windows 10 (1903+) or Windows 11
- **RAM**: Minimum 512 MB, Recommended 1 GB
- **Storage**: 100 MB free space (plus space for downloads)
- **Network**: Internet connection required for Telegram API access

## Configuration

### Settings
- **Session Data**: Automatically saved in `session.dat` (encrypted by WTelegramClient)
- **Download Location**: User-configurable, defaults to Documents/TelegramDownloads
- **API Credentials**: Entered at runtime, not stored persistently

### Environment Variables
The application does not currently use environment variables. All configuration is handled through the UI.

## Security & Privacy

### Data Handling
- **API Credentials**: Never stored in plain text or configuration files
- **Session Data**: Encrypted and managed by WTelegramClient library
- **User Data**: Only temporarily held in memory during operations
- **No Telemetry**: Application does not collect or send usage data

### Best Practices
- Keep your API credentials secure and private
- Don't share session files with others
- Regularly update the application for security patches
- Use 2FA on your Telegram account for enhanced security

## Troubleshooting

### Common Issues

#### Authentication Problems
**"Invalid API credentials"**
- Verify API ID and Hash are correct from my.telegram.org
- Ensure API Hash is exactly 32 characters
- Check for extra spaces or characters

**"Phone number not accepted"**
- Include country code (e.g., +1234567890)
- Use the same number registered with your Telegram account
- Remove any formatting (spaces, dashes, parentheses)

**"Session restoration failed"**
- Delete `session.dat` file and re-authenticate
- Check file permissions in application directory

#### Connection Issues
**"Connection timeout"**
- Verify internet connection
- Check firewall/antivirus settings
- Try using VPN if Telegram is blocked in your region

**"Channel not found"**
- Ensure channel username is correct
- Verify the channel is public
- Check if you have access to the channel

#### Download Problems
**"Download failed: CHANNEL_INVALID"**
- Verify channel URL is correct and channel exists
- Ensure you have access to the channel (not private/restricted)
- Check authentication status - may need to re-authenticate
- Try validating the channel first before downloading

**"Download failed: FLOOD_WAIT"**
- Application automatically handles rate limiting with delays
- If persistent, wait 10-15 minutes before retrying
- Large channels may hit rate limits more frequently

**"Download failed"** (General)
- Check available disk space for export files
- Verify write permissions to output directory  
- Ensure stable internet connection throughout download
- Check application logs in Log tab for specific error details

## Contributing

We welcome contributions!

### Development Setup
1. Fork the repository
2. Clone your fork locally
3. Install .NET 8 SDK
4. Install Visual Studio 2022 or JetBrains Rider
5. Open the solution and start developing

### Code Standards
- Follow C# naming conventions (PascalCase for public members)
- Use async/await for all asynchronous operations
- Include XML documentation for public APIs
- Write unit tests for new functionality
- Maintain MVVM architecture patterns

## Roadmap

### Planned Features
- [ ] **Media Download**: Support for downloading actual media files (images, videos, documents)
- [ ] **CSV Export Format**: Additional structured export option (architecture ready)
- [ ] **Filtering Options**: Date ranges, message types, and user filters  
- [ ] **Batch Operations**: Download multiple channels simultaneously
- [ ] **Schedule Downloads**: Automated periodic downloads
- [ ] **Advanced Search**: Full-text search within downloaded content

### Technical Improvements  
- [ ] **Unit Testing**: Comprehensive test coverage for all layers
- [ ] **Settings Persistence**: Enhanced user preferences and configuration
- [ ] **Performance Optimization**: Further large channel handling improvements
- [ ] **Plugin Architecture**: Extensible export format system
- [ ] **Offline Mode**: Browse previously downloaded content offline

### Recently Completed ✅
- [x] **Production-Ready Download System**: Complete batch processing with progress tracking
- [x] **Advanced Error Recovery**: FLOOD_WAIT automatic retry and CHANNEL_INVALID handling
- [x] **Integrated Logging Bridge**: Microsoft.Extensions.Logging to WPF UI connection
- [x] **Multi-format Export**: Rich Markdown and JSON export with comprehensive metadata
- [x] **Real-time Validation**: Input validation with caching and user-friendly error messages
- [x] **Clean 3-Layer Architecture**: Complete separation of concerns with dependency injection

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- **WTelegramClient**: Excellent C# library for Telegram API integration
- **Microsoft**: .NET platform and WPF framework
- **Telegram**: API access and documentation
- **Contributors**: All developers who have contributed to this project
---

**Disclaimer**: This tool is for legitimate use only. Users are responsible for complying with Telegram's Terms of Service and applicable laws. The developers assume no responsibility for misuse of this software.
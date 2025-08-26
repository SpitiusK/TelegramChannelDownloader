using TelegramChannelDownloader.Core.Models;
using TelegramChannelDownloader.TelegramApi;
using TelegramChannelDownloader.TelegramApi.Channels.Models;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace TelegramChannelDownloader.Core.Services;

/// <summary>
/// Service for handling all validation operations
/// </summary>
public class ValidationService : IValidationService
{
    private readonly ITelegramApiClient _telegramClient;
    private readonly ILogger<ValidationService> _logger;

    // Static reserved usernames to avoid API calls for obvious failures
    private static readonly HashSet<string> ReservedUsernames = new(StringComparer.OrdinalIgnoreCase)
    {
        "telegram", "admin", "api", "support", "help", "news", "settings", 
        "privacy", "contact", "about", "username", "channel", "group",
        "bot", "bots", "proxy", "socks", "http", "https", "ftp", "www",
        "joinchat", "addstickers", "login", "auth", "oauth", "iv"
    };

    public ValidationService(ITelegramApiClient telegramClient, ILogger<ValidationService> logger)
    {
        _telegramClient = telegramClient ?? throw new ArgumentNullException(nameof(telegramClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public ValidationResult ValidateApiId(string apiId)
    {
        if (string.IsNullOrWhiteSpace(apiId))
            return ValidationResult.Failure("API ID cannot be empty", "EMPTY_API_ID");

        if (!int.TryParse(apiId, out int id) || id <= 0)
            return ValidationResult.Failure("API ID must be a positive number", "INVALID_API_ID");

        return ValidationResult.Success();
    }

    /// <inheritdoc />
    public ValidationResult ValidateApiHash(string apiHash)
    {
        if (string.IsNullOrWhiteSpace(apiHash))
            return ValidationResult.Failure("API Hash cannot be empty", "EMPTY_API_HASH");

        // API Hash should be a 32-character hexadecimal string
        if (apiHash.Length != 32 || !Regex.IsMatch(apiHash, @"^[a-fA-F0-9]{32}$"))
            return ValidationResult.Failure("API Hash must be a 32-character hexadecimal string", "INVALID_API_HASH");

        return ValidationResult.Success();
    }

    /// <inheritdoc />
    public ValidationResult ValidateOutputDirectory(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            return ValidationResult.Failure("Output directory cannot be empty", "EMPTY_OUTPUT_DIRECTORY");

        try
        {
            // Check if path is valid
            var fullPath = Path.GetFullPath(directoryPath);
            
            // Check if directory exists or can be created
            if (!Directory.Exists(fullPath))
            {
                var parentDir = Path.GetDirectoryName(fullPath);
                if (parentDir != null && !Directory.Exists(parentDir))
                    return ValidationResult.Failure("Parent directory does not exist", "INVALID_PARENT_DIRECTORY");
            }

            // Check write permissions (try to create a temp file)
            var testFile = Path.Combine(fullPath, $"test_{Guid.NewGuid()}.tmp");
            try
            {
                if (!Directory.Exists(fullPath))
                    Directory.CreateDirectory(fullPath);

                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
            }
            catch (UnauthorizedAccessException)
            {
                return ValidationResult.Failure("No write access to the selected directory", "NO_WRITE_ACCESS");
            }
            catch (DirectoryNotFoundException)
            {
                return ValidationResult.Failure("Directory path is invalid", "INVALID_DIRECTORY_PATH");
            }

            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            return ValidationResult.Failure($"Invalid directory path: {ex.Message}", "INVALID_PATH");
        }
    }

    /// <inheritdoc />
    public ValidationResult ValidateApiCredentials(ApiCredentials credentials)
    {
        if (credentials == null)
            return ValidationResult.Failure("API credentials cannot be null");

        if (credentials.ApiId <= 0)
            return ValidationResult.Failure("API ID must be a positive number", "INVALID_API_ID");

        if (string.IsNullOrWhiteSpace(credentials.ApiHash))
            return ValidationResult.Failure("API Hash cannot be empty", "EMPTY_API_HASH");

        // API Hash should be a 32-character hexadecimal string
        if (credentials.ApiHash.Length != 32 || !Regex.IsMatch(credentials.ApiHash, @"^[a-fA-F0-9]{32}$"))
            return ValidationResult.Failure("API Hash must be a 32-character hexadecimal string", "INVALID_API_HASH");

        return ValidationResult.Success();
    }

    /// <inheritdoc />
    public ValidationResult ValidatePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return ValidationResult.Failure("Phone number cannot be empty", "EMPTY_PHONE_NUMBER");

        // Remove common formatting characters
        var cleaned = phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "");
        
        // Should be between 10-15 digits
        if (cleaned.Length < 10 || cleaned.Length > 15)
            return ValidationResult.Failure("Phone number must be 10-15 digits", "INVALID_PHONE_LENGTH");

        if (!Regex.IsMatch(cleaned, @"^\d+$"))
            return ValidationResult.Failure("Phone number can only contain digits and formatting characters", "INVALID_PHONE_FORMAT");

        return ValidationResult.Success();
    }

    /// <inheritdoc />
    public ValidationResult ValidateVerificationCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return ValidationResult.Failure("Verification code cannot be empty", "EMPTY_VERIFICATION_CODE");

        // Telegram verification codes are typically 5 digits
        if (code.Length != 5)
            return ValidationResult.Failure("Verification code must be exactly 5 digits", "INVALID_VERIFICATION_LENGTH");

        if (!Regex.IsMatch(code, @"^\d{5}$"))
            return ValidationResult.Failure("Verification code must be 5 digits", "INVALID_VERIFICATION_FORMAT");

        return ValidationResult.Success();
    }

    /// <inheritdoc />
    public ValidationResult ValidateTwoFactorCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return ValidationResult.Failure("Two-factor authentication code cannot be empty", "EMPTY_2FA_CODE");

        // 2FA codes can vary in length but are typically 6-8 characters
        if (code.Length < 6 || code.Length > 8)
            return ValidationResult.Failure("Two-factor authentication code must be 6-8 characters", "INVALID_2FA_LENGTH");

        return ValidationResult.Success();
    }

    /// <inheritdoc />
    public ValidationResult ValidateChannelUrl(string channelUrl)
    {
        var parseResult = ParseChannelUrl(channelUrl);
        
        if (!parseResult.IsValid)
            return ValidationResult.Failure(parseResult.ErrorMessage ?? "Invalid channel URL", "INVALID_CHANNEL_URL");

        return ValidationResult.Success();
    }

    /// <inheritdoc />
    public async Task<ValidationResult<ChannelInfo>> ValidateChannelAsync(string channelUrl)
    {
        try
        {
            // First validate the URL format
            var urlValidation = ValidateChannelUrl(channelUrl);
            if (!urlValidation.IsValid)
                return ValidationResult<ChannelInfo>.Failure(urlValidation.ErrorMessage!, urlValidation.ErrorCode);

            _logger.LogDebug("Validating channel access: {ChannelUrl}", channelUrl);

            // Use the Telegram API to validate the channel
            var channelInfo = await _telegramClient.GetChannelInfoAsync(channelUrl);
            
            if (channelInfo == null)
                return ValidationResult<ChannelInfo>.Failure("Channel not found or inaccessible", "CHANNEL_NOT_FOUND");

            if (!channelInfo.CanDownload)
            {
                var errorMessage = channelInfo.ValidationMessage ?? "Channel cannot be downloaded";
                return ValidationResult<ChannelInfo>.Failure(errorMessage, "CHANNEL_CANNOT_DOWNLOAD");
            }

            _logger.LogDebug("Channel validation successful: {ChannelName} ({MessageCount} messages)", 
                channelInfo.DisplayName, channelInfo.MessageCount);

            return ValidationResult<ChannelInfo>.Success(channelInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate channel: {ChannelUrl}", channelUrl);
            return ValidationResult<ChannelInfo>.Failure($"Channel validation failed: {ex.Message}", "VALIDATION_ERROR");
        }
    }

    /// <inheritdoc />
    public ValidationResult ValidateDirectoryPath(string directoryPath, bool checkWriteAccess = true, bool createIfNotExists = false)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            return ValidationResult.Failure("Directory path cannot be empty", "EMPTY_DIRECTORY_PATH");

        try
        {
            // Get the full path to normalize it
            var fullPath = Path.GetFullPath(directoryPath);
            
            if (string.IsNullOrWhiteSpace(fullPath))
                return ValidationResult.Failure("Directory path is invalid", "INVALID_DIRECTORY_PATH");

            // Check if directory exists
            if (!Directory.Exists(fullPath))
            {
                if (createIfNotExists)
                {
                    try
                    {
                        Directory.CreateDirectory(fullPath);
                        _logger.LogInformation("Created directory: {DirectoryPath}", fullPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create directory: {DirectoryPath}", fullPath);
                        return ValidationResult.Failure($"Failed to create directory: {ex.Message}", "DIRECTORY_CREATION_FAILED");
                    }
                }
                else
                {
                    return ValidationResult.Failure("Directory does not exist", "DIRECTORY_NOT_FOUND");
                }
            }

            // Check write access if requested
            if (checkWriteAccess)
            {
                var testFileName = Path.Combine(fullPath, $"test_{Guid.NewGuid()}.tmp");
                try
                {
                    File.WriteAllText(testFileName, "test");
                    File.Delete(testFileName);
                }
                catch (UnauthorizedAccessException)
                {
                    return ValidationResult.Failure("Insufficient permissions to write to directory", "DIRECTORY_ACCESS_DENIED");
                }
                catch (Exception ex)
                {
                    return ValidationResult.Failure($"Directory write test failed: {ex.Message}", "DIRECTORY_WRITE_TEST_FAILED");
                }
            }

            return ValidationResult.Success();
        }
        catch (ArgumentException ex)
        {
            return ValidationResult.Failure($"Invalid directory path: {ex.Message}", "INVALID_DIRECTORY_PATH");
        }
        catch (NotSupportedException ex)
        {
            return ValidationResult.Failure($"Directory path not supported: {ex.Message}", "DIRECTORY_PATH_NOT_SUPPORTED");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error validating directory path: {DirectoryPath}", directoryPath);
            return ValidationResult.Failure($"Directory validation failed: {ex.Message}", "DIRECTORY_VALIDATION_ERROR");
        }
    }

    /// <inheritdoc />
    public ValidationResult ValidateDirectorySpace(string directoryPath, long requiredSpaceBytes)
    {
        // First validate the directory path
        var pathValidation = ValidateDirectoryPath(directoryPath);
        if (!pathValidation.IsValid)
            return pathValidation;

        if (requiredSpaceBytes <= 0)
            return ValidationResult.Success(); // No space requirements

        try
        {
            var fullPath = Path.GetFullPath(directoryPath);
            var drive = new DriveInfo(Path.GetPathRoot(fullPath) ?? fullPath);
            
            if (drive.AvailableFreeSpace < requiredSpaceBytes)
            {
                var availableGB = drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                var neededGB = requiredSpaceBytes / (1024.0 * 1024.0 * 1024.0);
                
                var message = $"Insufficient disk space. Available: {availableGB:F1} GB, Required: {neededGB:F1} GB";
                _logger.LogWarning("Disk space validation failed: {Message}", message);
                
                return ValidationResult.Failure(message, "INSUFFICIENT_DISK_SPACE");
            }

            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check disk space for directory: {DirectoryPath}", directoryPath);
            return ValidationResult.Failure($"Could not check disk space: {ex.Message}", "DISK_SPACE_CHECK_FAILED");
        }
    }

    /// <inheritdoc />
    public async Task<ValidationResult> ValidateDownloadRequestAsync(DownloadRequest request)
    {
        if (request == null)
            return ValidationResult.Failure("Download request cannot be null", "NULL_REQUEST");

        // Validate channel URL
        var channelValidation = ValidateChannelUrl(request.ChannelUrl);
        if (!channelValidation.IsValid)
            return channelValidation;

        // Validate output directory
        var directoryValidation = ValidateDirectoryPath(request.OutputDirectory, checkWriteAccess: true);
        if (!directoryValidation.IsValid)
            return directoryValidation;

        // Validate API credentials if provided
        if (request.Credentials != null)
        {
            var credentialsValidation = ValidateApiCredentials(request.Credentials);
            if (!credentialsValidation.IsValid)
                return credentialsValidation;
        }

        // Validate download options
        if (request.Options.MaxMessages < 0)
            return ValidationResult.Failure("Maximum messages cannot be negative", "INVALID_MAX_MESSAGES");

        if (request.Options.BatchSize <= 0)
            return ValidationResult.Failure("Batch size must be positive", "INVALID_BATCH_SIZE");

        if (request.Options.StartDate.HasValue && request.Options.EndDate.HasValue && 
            request.Options.StartDate > request.Options.EndDate)
        {
            return ValidationResult.Failure("Start date cannot be after end date", "INVALID_DATE_RANGE");
        }

        return ValidationResult.Success();
    }

    /// <inheritdoc />
    public ValidationResult ValidateExportRequest(ExportRequest request)
    {
        if (request == null)
            return ValidationResult.Failure("Export request cannot be null", "NULL_REQUEST");

        if (request.Messages == null || !request.Messages.Any())
            return ValidationResult.Failure("Export request must contain messages", "NO_MESSAGES");

        if (string.IsNullOrWhiteSpace(request.OutputPath))
            return ValidationResult.Failure("Output path cannot be empty", "EMPTY_OUTPUT_PATH");

        try
        {
            var directory = Path.GetDirectoryName(request.OutputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                var directoryValidation = ValidateDirectoryPath(directory, checkWriteAccess: true);
                if (!directoryValidation.IsValid)
                    return directoryValidation;
            }
        }
        catch (Exception ex)
        {
            return ValidationResult.Failure($"Invalid output path: {ex.Message}", "INVALID_OUTPUT_PATH");
        }

        return ValidationResult.Success();
    }

    /// <inheritdoc />
    public string GetValidationErrorMessage(string fieldName, string value, string? additionalContext = null)
    {
        var baseMessage = fieldName.ToLowerInvariant() switch
        {
            "apiid" => "API ID must be a positive number",
            "apihash" => "API Hash must be a 32-character hexadecimal string",
            "phonenumber" => "Phone number must be 10-15 digits",
            "verificationcode" => "Verification code must be 5 digits",
            "twofactorcode" => "Two-factor authentication code must be 6-8 characters",
            "channelurl" => "Channel URL must be a valid Telegram username (5-32 characters)",
            "outputdirectory" => "Output directory path is invalid",
            _ => $"{fieldName} is invalid"
        };

        return !string.IsNullOrWhiteSpace(additionalContext) 
            ? $"{baseMessage}. {additionalContext}" 
            : baseMessage;
    }

    /// <inheritdoc />
    public ChannelUrlParseResult ParseChannelUrl(string channelUrl)
    {
        var result = new ChannelUrlParseResult();

        if (string.IsNullOrWhiteSpace(channelUrl))
        {
            result.ErrorMessage = "Channel URL cannot be empty";
            return result;
        }

        var input = channelUrl.Trim();
        string username;

        try
        {
            // Handle different URL formats
            if (input.StartsWith("@"))
            {
                // Direct username format: @username
                username = input.Substring(1);
                result.InputFormat = ChannelUrlFormat.Username;
            }
            else if (input.StartsWith("https://t.me/", StringComparison.OrdinalIgnoreCase))
            {
                // Full HTTPS URL: https://t.me/username
                username = input.Substring(13);
                result.InputFormat = ChannelUrlFormat.HttpsUrl;
            }
            else if (input.StartsWith("http://t.me/", StringComparison.OrdinalIgnoreCase))
            {
                // HTTP URL: http://t.me/username
                username = input.Substring(12);
                result.InputFormat = ChannelUrlFormat.HttpUrl;
            }
            else if (input.StartsWith("t.me/", StringComparison.OrdinalIgnoreCase))
            {
                // Short URL: t.me/username
                username = input.Substring(5);
                result.InputFormat = ChannelUrlFormat.ShortUrl;
            }
            else
            {
                // Assume it's a direct username without @ prefix
                username = input;
                result.InputFormat = ChannelUrlFormat.PlainUsername;
            }

            // Remove any additional path parameters (e.g., t.me/username/123)
            var slashIndex = username.IndexOf('/');
            if (slashIndex > 0)
            {
                username = username.Substring(0, slashIndex);
            }

            // Remove query parameters and fragments
            var queryIndex = username.IndexOf('?');
            if (queryIndex > 0)
            {
                username = username.Substring(0, queryIndex);
            }

            var hashIndex = username.IndexOf('#');
            if (hashIndex > 0)
            {
                username = username.Substring(0, hashIndex);
            }

            username = username.Trim();

            // Validate username format
            if (string.IsNullOrEmpty(username))
            {
                result.ErrorMessage = "Username cannot be empty after parsing URL";
                return result;
            }

            if (username.Length < 5)
            {
                result.ErrorMessage = "Username must be at least 5 characters long";
                return result;
            }

            if (username.Length > 32)
            {
                result.ErrorMessage = "Username cannot be longer than 32 characters";
                return result;
            }

            // Check username character rules:
            // - Can contain letters, digits, and underscores
            // - Cannot start or end with underscore (unless exactly 5 chars of letters/digits)
            // - Cannot have consecutive underscores
            if (!Regex.IsMatch(username, @"^[a-zA-Z0-9][a-zA-Z0-9_]*[a-zA-Z0-9]$") && 
                !Regex.IsMatch(username, @"^[a-zA-Z0-9]{5,32}$"))
            {
                result.ErrorMessage = "Username can only contain letters, digits, and underscores. Cannot start or end with underscore.";
                return result;
            }

            if (username.Contains("__"))
            {
                result.ErrorMessage = "Username cannot contain consecutive underscores";
                return result;
            }

            // Check for reserved usernames
            if (ReservedUsernames.Contains(username))
            {
                result.ErrorMessage = "This username is reserved and cannot be used";
                return result;
            }

            result.IsValid = true;
            result.CleanUsername = username;
            result.CanonicalUrl = $"https://t.me/{username}";
            result.ErrorMessage = null;

            return result;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Error parsing channel URL: {ex.Message}";
            return result;
        }
    }
}
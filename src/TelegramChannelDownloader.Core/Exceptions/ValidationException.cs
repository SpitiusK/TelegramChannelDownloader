using TelegramChannelDownloader.Core.Models;

namespace TelegramChannelDownloader.Core.Exceptions;

/// <summary>
/// Exception thrown when validation fails
/// </summary>
public class ValidationException : TelegramCoreException
{
    /// <summary>
    /// Validation result that contains the errors
    /// </summary>
    public ValidationResult ValidationResult { get; }

    /// <summary>
    /// Field or property that failed validation
    /// </summary>
    public string? FieldName { get; }

    /// <summary>
    /// Value that failed validation
    /// </summary>
    public object? Value { get; }

    /// <summary>
    /// Creates a new validation exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="fieldName">Field that failed validation</param>
    /// <param name="value">Value that failed validation</param>
    public ValidationException(string message, string? fieldName = null, object? value = null)
        : base(message)
    {
        FieldName = fieldName;
        Value = value;
        ValidationResult = ValidationResult.Failure(message, fieldName);
        
        if (fieldName != null)
            AddContext("FieldName", fieldName);
        if (value != null)
            AddContext("Value", value);
    }

    /// <summary>
    /// Creates a new validation exception from validation result
    /// </summary>
    /// <param name="validationResult">Validation result</param>
    public ValidationException(ValidationResult validationResult)
        : base(validationResult.ErrorMessage ?? "Validation failed")
    {
        ValidationResult = validationResult;
        FieldName = validationResult.FieldName;
        Value = validationResult.Value;

        // Add all context from validation result
        foreach (var kvp in validationResult.Context)
        {
            AddContext(kvp.Key, kvp.Value);
        }
    }

    /// <summary>
    /// Creates a new validation exception with inner exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    /// <param name="fieldName">Field that failed validation</param>
    /// <param name="value">Value that failed validation</param>
    public ValidationException(string message, Exception innerException, string? fieldName = null, object? value = null)
        : base(message, innerException)
    {
        FieldName = fieldName;
        Value = value;
        ValidationResult = ValidationResult.Failure(message, fieldName);
        
        if (fieldName != null)
            AddContext("FieldName", fieldName);
        if (value != null)
            AddContext("Value", value);
    }

    /// <summary>
    /// Gets all validation errors as a formatted string
    /// </summary>
    /// <returns>Formatted error string</returns>
    public string GetAllErrors()
    {
        if (!ValidationResult.Errors.Any())
            return Message;

        return string.Join(Environment.NewLine, ValidationResult.Errors);
    }

    /// <summary>
    /// Gets all validation warnings as a formatted string
    /// </summary>
    /// <returns>Formatted warning string</returns>
    public string GetAllWarnings()
    {
        if (!ValidationResult.Warnings.Any())
            return string.Empty;

        return string.Join(Environment.NewLine, ValidationResult.Warnings);
    }
}

/// <summary>
/// Exception thrown when API credentials are invalid
/// </summary>
public class InvalidCredentialsException : ValidationException
{
    /// <summary>
    /// Creates a new invalid credentials exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="credentialField">Specific credential field that is invalid</param>
    public InvalidCredentialsException(string message, string? credentialField = null)
        : base(message, credentialField)
    {
    }
}

/// <summary>
/// Exception thrown when channel URL is invalid
/// </summary>
public class InvalidChannelUrlException : ValidationException
{
    /// <summary>
    /// Channel URL that is invalid
    /// </summary>
    public string ChannelUrl { get; }

    /// <summary>
    /// Creates a new invalid channel URL exception
    /// </summary>
    /// <param name="channelUrl">Invalid channel URL</param>
    /// <param name="message">Error message</param>
    public InvalidChannelUrlException(string channelUrl, string message)
        : base(message, "ChannelUrl", channelUrl)
    {
        ChannelUrl = channelUrl;
        AddContext("ChannelUrl", channelUrl);
    }
}

/// <summary>
/// Exception thrown when output directory is invalid
/// </summary>
public class InvalidOutputDirectoryException : ValidationException
{
    /// <summary>
    /// Output directory path that is invalid
    /// </summary>
    public string DirectoryPath { get; }

    /// <summary>
    /// Creates a new invalid output directory exception
    /// </summary>
    /// <param name="directoryPath">Invalid directory path</param>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public InvalidOutputDirectoryException(string directoryPath, string message, Exception? innerException = null)
        : base(message, innerException, "OutputDirectory", directoryPath)
    {
        DirectoryPath = directoryPath;
        AddContext("DirectoryPath", directoryPath);
    }
}

/// <summary>
/// Exception thrown when insufficient permissions for operation
/// </summary>
public class InsufficientPermissionsException : ValidationException
{
    /// <summary>
    /// Required permission
    /// </summary>
    public string RequiredPermission { get; }

    /// <summary>
    /// Creates a new insufficient permissions exception
    /// </summary>
    /// <param name="requiredPermission">Required permission</param>
    /// <param name="message">Error message</param>
    public InsufficientPermissionsException(string requiredPermission, string message)
        : base(message)
    {
        RequiredPermission = requiredPermission;
        AddContext("RequiredPermission", requiredPermission);
    }
}
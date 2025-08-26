namespace TelegramChannelDownloader.Core.Models;

/// <summary>
/// Result of a validation operation
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Indicates if validation passed
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Primary error message
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Error code for programmatic handling
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// List of all validation errors
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// List of validation warnings (non-blocking issues)
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Field or property that failed validation
    /// </summary>
    public string? FieldName { get; set; }

    /// <summary>
    /// Value that was validated
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Additional context information
    /// </summary>
    public Dictionary<string, object> Context { get; set; } = new();

    /// <summary>
    /// Validation rule that was applied
    /// </summary>
    public string? ValidationRule { get; set; }

    /// <summary>
    /// Indicates if there are any warnings
    /// </summary>
    public bool HasWarnings => Warnings.Count > 0;

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    /// <returns>Valid result</returns>
    public static ValidationResult Success()
    {
        return new ValidationResult { IsValid = true };
    }

    /// <summary>
    /// Creates a failed validation result
    /// </summary>
    /// <param name="errorMessage">Error message</param>
    /// <param name="fieldName">Field name that failed validation</param>
    /// <param name="errorCode">Error code for programmatic handling</param>
    /// <returns>Invalid result</returns>
    public static ValidationResult Failure(string errorMessage, string? fieldName = null, string? errorCode = null)
    {
        return new ValidationResult
        {
            IsValid = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode,
            Errors = { errorMessage },
            FieldName = fieldName
        };
    }

    /// <summary>
    /// Creates a failed validation result with multiple errors
    /// </summary>
    /// <param name="errors">List of error messages</param>
    /// <param name="fieldName">Field name that failed validation</param>
    /// <returns>Invalid result</returns>
    public static ValidationResult Failure(IEnumerable<string> errors, string? fieldName = null)
    {
        var errorList = errors.ToList();
        return new ValidationResult
        {
            IsValid = false,
            ErrorMessage = errorList.FirstOrDefault(),
            Errors = errorList,
            FieldName = fieldName
        };
    }

    /// <summary>
    /// Adds an error to the validation result
    /// </summary>
    /// <param name="error">Error message</param>
    public void AddError(string error)
    {
        IsValid = false;
        Errors.Add(error);
        ErrorMessage ??= error;
    }

    /// <summary>
    /// Adds a warning to the validation result
    /// </summary>
    /// <param name="warning">Warning message</param>
    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }

    /// <summary>
    /// Adds context information
    /// </summary>
    /// <param name="key">Context key</param>
    /// <param name="value">Context value</param>
    public void AddContext(string key, object value)
    {
        Context[key] = value;
    }

    /// <summary>
    /// Gets context value by key
    /// </summary>
    /// <typeparam name="T">Type to cast to</typeparam>
    /// <param name="key">Context key</param>
    /// <returns>Context value or default</returns>
    public T? GetContext<T>(string key)
    {
        if (Context.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }

    /// <summary>
    /// Combines multiple validation results
    /// </summary>
    /// <param name="results">Results to combine</param>
    /// <returns>Combined result</returns>
    public static ValidationResult Combine(params ValidationResult[] results)
    {
        var combined = new ValidationResult { IsValid = true };

        foreach (var result in results)
        {
            if (!result.IsValid)
            {
                combined.IsValid = false;
            }

            combined.Errors.AddRange(result.Errors);
            combined.Warnings.AddRange(result.Warnings);

            // Merge context
            foreach (var kvp in result.Context)
            {
                combined.Context[kvp.Key] = kvp.Value;
            }
        }

        combined.ErrorMessage = combined.Errors.FirstOrDefault();
        return combined;
    }
}
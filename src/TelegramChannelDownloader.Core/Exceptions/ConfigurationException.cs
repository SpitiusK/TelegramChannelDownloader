namespace TelegramChannelDownloader.Core.Exceptions;

/// <summary>
/// Exception thrown when configuration errors occur
/// </summary>
public class ConfigurationException : TelegramCoreException
{
    /// <summary>
    /// Configuration section that caused the error
    /// </summary>
    public string? ConfigSection { get; }

    /// <summary>
    /// Configuration key that caused the error
    /// </summary>
    public string? ConfigKey { get; }

    /// <summary>
    /// Configuration value that caused the error
    /// </summary>
    public object? ConfigValue { get; }

    /// <summary>
    /// Creates a new configuration exception
    /// </summary>
    /// <param name="message">Error message</param>
    public ConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Creates a new configuration exception with inner exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public ConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Creates a new configuration exception with context
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="configSection">Configuration section</param>
    /// <param name="configKey">Configuration key</param>
    /// <param name="configValue">Configuration value</param>
    public ConfigurationException(string message, string configSection, string? configKey = null, object? configValue = null)
        : base(message)
    {
        ConfigSection = configSection;
        ConfigKey = configKey;
        ConfigValue = configValue;
        
        AddContext("ConfigSection", configSection);
        if (configKey != null)
            AddContext("ConfigKey", configKey);
        if (configValue != null)
            AddContext("ConfigValue", configValue);
    }

    /// <summary>
    /// Creates a new configuration exception with full context
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    /// <param name="configSection">Configuration section</param>
    /// <param name="configKey">Configuration key</param>
    /// <param name="configValue">Configuration value</param>
    public ConfigurationException(string message, Exception innerException, string configSection, string? configKey = null, object? configValue = null)
        : base(message, innerException)
    {
        ConfigSection = configSection;
        ConfigKey = configKey;
        ConfigValue = configValue;
        
        AddContext("ConfigSection", configSection);
        if (configKey != null)
            AddContext("ConfigKey", configKey);
        if (configValue != null)
            AddContext("ConfigValue", configValue);
    }
}

/// <summary>
/// Exception thrown when required configuration is missing
/// </summary>
public class MissingConfigurationException : ConfigurationException
{
    /// <summary>
    /// Creates a new missing configuration exception
    /// </summary>
    /// <param name="configSection">Configuration section</param>
    /// <param name="configKey">Configuration key that is missing</param>
    public MissingConfigurationException(string configSection, string configKey)
        : base($"Required configuration '{configKey}' in section '{configSection}' is missing", configSection, configKey)
    {
    }
}

/// <summary>
/// Exception thrown when configuration values are invalid
/// </summary>
public class InvalidConfigurationException : ConfigurationException
{
    /// <summary>
    /// Expected value type or format
    /// </summary>
    public string? ExpectedFormat { get; }

    /// <summary>
    /// Creates a new invalid configuration exception
    /// </summary>
    /// <param name="configSection">Configuration section</param>
    /// <param name="configKey">Configuration key</param>
    /// <param name="configValue">Invalid configuration value</param>
    /// <param name="expectedFormat">Expected format or type</param>
    public InvalidConfigurationException(string configSection, string configKey, object configValue, string expectedFormat)
        : base($"Configuration '{configKey}' in section '{configSection}' has invalid value '{configValue}'. Expected: {expectedFormat}", configSection, configKey, configValue)
    {
        ExpectedFormat = expectedFormat;
        AddContext("ExpectedFormat", expectedFormat);
    }
}

/// <summary>
/// Exception thrown when configuration file cannot be loaded
/// </summary>
public class ConfigurationFileException : ConfigurationException
{
    /// <summary>
    /// Configuration file path
    /// </summary>
    public string? FilePath { get; }

    /// <summary>
    /// Creates a new configuration file exception
    /// </summary>
    /// <param name="filePath">Configuration file path</param>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public ConfigurationFileException(string filePath, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        FilePath = filePath;
        AddContext("FilePath", filePath);
    }
}
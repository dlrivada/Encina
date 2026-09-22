using Microsoft.Extensions.Logging;

namespace Encina.Security.Secrets.HashiCorpVault;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators
/// for the HashiCorp Vault secret provider.
/// </summary>
internal static partial class Log
{
    // HashiCorp Vault operations: EventIds 5300-5307

    [LoggerMessage(EventId = 5300, Level = LogLevel.Debug,
        Message = "Secret '{SecretName}' retrieved from HashiCorp Vault")]
    public static partial void SecretRetrieved(ILogger logger, string secretName);

    [LoggerMessage(EventId = 5301, Level = LogLevel.Debug,
        Message = "Secret '{SecretName}' not found in HashiCorp Vault")]
    public static partial void SecretNotFound(ILogger logger, string secretName);

    [LoggerMessage(EventId = 5302, Level = LogLevel.Debug,
        Message = "Secret '{SecretName}' written to HashiCorp Vault")]
    public static partial void SecretWritten(ILogger logger, string secretName);

    [LoggerMessage(EventId = 5303, Level = LogLevel.Information,
        Message = "Secret '{SecretName}' rotated in HashiCorp Vault (new version created)")]
    public static partial void SecretRotated(ILogger logger, string secretName);

    [LoggerMessage(EventId = 5304, Level = LogLevel.Warning,
        Message = "Access denied to secret '{SecretName}' in HashiCorp Vault: {Reason}")]
    public static partial void AccessDenied(ILogger logger, string secretName, string reason, Exception exception);

    [LoggerMessage(EventId = 5305, Level = LogLevel.Warning,
        Message = "HashiCorp Vault provider is unavailable: {Reason}")]
    public static partial void ProviderUnavailable(ILogger logger, string reason, Exception exception);

    [LoggerMessage(EventId = 5306, Level = LogLevel.Error,
        Message = "Secret rotation failed for '{SecretName}' in HashiCorp Vault: {Reason}")]
    public static partial void RotationFailed(ILogger logger, string secretName, string reason, Exception exception);

    [LoggerMessage(EventId = 5307, Level = LogLevel.Warning,
        Message = "Failed to deserialize secret '{SecretName}' to type '{TargetType}' from HashiCorp Vault")]
    public static partial void DeserializationFailed(ILogger logger, string secretName, string targetType, Exception exception);
}

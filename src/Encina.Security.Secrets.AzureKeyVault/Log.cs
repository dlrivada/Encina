using Microsoft.Extensions.Logging;

namespace Encina.Security.Secrets.AzureKeyVault;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators
/// for the Azure Key Vault secret provider.
/// </summary>
internal static partial class Log
{
    // Azure Key Vault operations: EventIds 200-209

    [LoggerMessage(EventId = 200, Level = LogLevel.Debug,
        Message = "Secret '{SecretName}' retrieved from Azure Key Vault")]
    public static partial void SecretRetrieved(ILogger logger, string secretName);

    [LoggerMessage(EventId = 201, Level = LogLevel.Debug,
        Message = "Secret '{SecretName}' not found in Azure Key Vault")]
    public static partial void SecretNotFound(ILogger logger, string secretName);

    [LoggerMessage(EventId = 202, Level = LogLevel.Debug,
        Message = "Secret '{SecretName}' written to Azure Key Vault")]
    public static partial void SecretWritten(ILogger logger, string secretName);

    [LoggerMessage(EventId = 203, Level = LogLevel.Information,
        Message = "Secret '{SecretName}' rotated in Azure Key Vault (new version created)")]
    public static partial void SecretRotated(ILogger logger, string secretName);

    [LoggerMessage(EventId = 204, Level = LogLevel.Warning,
        Message = "Access denied to secret '{SecretName}' in Azure Key Vault: {Reason}")]
    public static partial void AccessDenied(ILogger logger, string secretName, string reason, Exception exception);

    [LoggerMessage(EventId = 205, Level = LogLevel.Warning,
        Message = "Azure Key Vault provider is unavailable: {Reason}")]
    public static partial void ProviderUnavailable(ILogger logger, string reason, Exception exception);

    [LoggerMessage(EventId = 206, Level = LogLevel.Error,
        Message = "Secret rotation failed for '{SecretName}' in Azure Key Vault: {Reason}")]
    public static partial void RotationFailed(ILogger logger, string secretName, string reason, Exception exception);

    [LoggerMessage(EventId = 207, Level = LogLevel.Warning,
        Message = "Failed to deserialize secret '{SecretName}' to type '{TargetType}' from Azure Key Vault")]
    public static partial void DeserializationFailed(ILogger logger, string secretName, string targetType, Exception exception);

    [LoggerMessage(EventId = 208, Level = LogLevel.Information,
        Message = "Azure Key Vault secrets provider registered for vault '{VaultUri}'")]
    public static partial void ProviderRegistered(ILogger logger, string vaultUri);
}

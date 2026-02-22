using Microsoft.Extensions.Logging;

namespace Encina.Security.Secrets.GoogleCloudSecretManager;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators
/// for the Google Cloud Secret Manager provider.
/// </summary>
internal static partial class Log
{
    // Google Cloud Secret Manager operations: EventIds 230-239

    [LoggerMessage(EventId = 230, Level = LogLevel.Debug,
        Message = "Secret '{SecretName}' retrieved from Google Cloud Secret Manager")]
    public static partial void SecretRetrieved(ILogger logger, string secretName);

    [LoggerMessage(EventId = 231, Level = LogLevel.Debug,
        Message = "Secret '{SecretName}' not found in Google Cloud Secret Manager")]
    public static partial void SecretNotFound(ILogger logger, string secretName);

    [LoggerMessage(EventId = 232, Level = LogLevel.Debug,
        Message = "Secret '{SecretName}' written to Google Cloud Secret Manager")]
    public static partial void SecretWritten(ILogger logger, string secretName);

    [LoggerMessage(EventId = 233, Level = LogLevel.Information,
        Message = "Secret '{SecretName}' rotated in Google Cloud Secret Manager (new version created)")]
    public static partial void SecretRotated(ILogger logger, string secretName);

    [LoggerMessage(EventId = 234, Level = LogLevel.Warning,
        Message = "Access denied to secret '{SecretName}' in Google Cloud Secret Manager: {Reason}")]
    public static partial void AccessDenied(ILogger logger, string secretName, string reason, Exception exception);

    [LoggerMessage(EventId = 235, Level = LogLevel.Warning,
        Message = "Google Cloud Secret Manager provider is unavailable: {Reason}")]
    public static partial void ProviderUnavailable(ILogger logger, string reason, Exception exception);

    [LoggerMessage(EventId = 236, Level = LogLevel.Error,
        Message = "Secret rotation failed for '{SecretName}' in Google Cloud Secret Manager: {Reason}")]
    public static partial void RotationFailed(ILogger logger, string secretName, string reason, Exception exception);

    [LoggerMessage(EventId = 237, Level = LogLevel.Warning,
        Message = "Failed to deserialize secret '{SecretName}' to type '{TargetType}' from Google Cloud Secret Manager")]
    public static partial void DeserializationFailed(ILogger logger, string secretName, string targetType, Exception exception);

    [LoggerMessage(EventId = 238, Level = LogLevel.Debug,
        Message = "Secret '{SecretName}' created in Google Cloud Secret Manager (did not exist, fallback from AddSecretVersion)")]
    public static partial void SecretCreated(ILogger logger, string secretName);
}

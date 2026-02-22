using Microsoft.Extensions.Logging;

namespace Encina.Security.Secrets.AwsSecretsManager;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators
/// for the AWS Secrets Manager secret provider.
/// </summary>
internal static partial class Log
{
    // AWS Secrets Manager operations: EventIds 210-219

    [LoggerMessage(EventId = 210, Level = LogLevel.Debug,
        Message = "Secret '{SecretName}' retrieved from AWS Secrets Manager")]
    public static partial void SecretRetrieved(ILogger logger, string secretName);

    [LoggerMessage(EventId = 211, Level = LogLevel.Debug,
        Message = "Secret '{SecretName}' not found in AWS Secrets Manager")]
    public static partial void SecretNotFound(ILogger logger, string secretName);

    [LoggerMessage(EventId = 212, Level = LogLevel.Debug,
        Message = "Secret '{SecretName}' written to AWS Secrets Manager")]
    public static partial void SecretWritten(ILogger logger, string secretName);

    [LoggerMessage(EventId = 213, Level = LogLevel.Information,
        Message = "Secret '{SecretName}' rotated in AWS Secrets Manager (new version created)")]
    public static partial void SecretRotated(ILogger logger, string secretName);

    [LoggerMessage(EventId = 214, Level = LogLevel.Warning,
        Message = "Access denied to secret '{SecretName}' in AWS Secrets Manager: {Reason}")]
    public static partial void AccessDenied(ILogger logger, string secretName, string reason, Exception exception);

    [LoggerMessage(EventId = 215, Level = LogLevel.Warning,
        Message = "AWS Secrets Manager provider is unavailable: {Reason}")]
    public static partial void ProviderUnavailable(ILogger logger, string reason, Exception exception);

    [LoggerMessage(EventId = 216, Level = LogLevel.Error,
        Message = "Secret rotation failed for '{SecretName}' in AWS Secrets Manager: {Reason}")]
    public static partial void RotationFailed(ILogger logger, string secretName, string reason, Exception exception);

    [LoggerMessage(EventId = 217, Level = LogLevel.Warning,
        Message = "Failed to deserialize secret '{SecretName}' to type '{TargetType}' from AWS Secrets Manager")]
    public static partial void DeserializationFailed(ILogger logger, string secretName, string targetType, Exception exception);

    [LoggerMessage(EventId = 218, Level = LogLevel.Debug,
        Message = "Secret '{SecretName}' created in AWS Secrets Manager (did not exist, fallback from PutSecretValue)")]
    public static partial void SecretCreated(ILogger logger, string secretName);
}

using Microsoft.Extensions.Logging;

namespace Encina.Security.Secrets.AwsSecretsManager;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators
/// for the AWS Secrets Manager secret provider.
/// </summary>
internal static partial class Log
{
    // AWS Secrets Manager operations: EventIds 5250-5258

    [LoggerMessage(EventId = 5250, Level = LogLevel.Debug,
        Message = "Secret '{SecretName}' retrieved from AWS Secrets Manager")]
    public static partial void SecretRetrieved(ILogger logger, string secretName);

    [LoggerMessage(EventId = 5251, Level = LogLevel.Debug,
        Message = "Secret '{SecretName}' not found in AWS Secrets Manager")]
    public static partial void SecretNotFound(ILogger logger, string secretName);

    [LoggerMessage(EventId = 5252, Level = LogLevel.Debug,
        Message = "Secret '{SecretName}' written to AWS Secrets Manager")]
    public static partial void SecretWritten(ILogger logger, string secretName);

    [LoggerMessage(EventId = 5253, Level = LogLevel.Information,
        Message = "Secret '{SecretName}' rotated in AWS Secrets Manager (new version created)")]
    public static partial void SecretRotated(ILogger logger, string secretName);

    [LoggerMessage(EventId = 5254, Level = LogLevel.Warning,
        Message = "Access denied to secret '{SecretName}' in AWS Secrets Manager: {Reason}")]
    public static partial void AccessDenied(ILogger logger, string secretName, string reason, Exception exception);

    [LoggerMessage(EventId = 5255, Level = LogLevel.Warning,
        Message = "AWS Secrets Manager provider is unavailable: {Reason}")]
    public static partial void ProviderUnavailable(ILogger logger, string reason, Exception exception);

    [LoggerMessage(EventId = 5256, Level = LogLevel.Error,
        Message = "Secret rotation failed for '{SecretName}' in AWS Secrets Manager: {Reason}")]
    public static partial void RotationFailed(ILogger logger, string secretName, string reason, Exception exception);

    [LoggerMessage(EventId = 5257, Level = LogLevel.Warning,
        Message = "Failed to deserialize secret '{SecretName}' to type '{TargetType}' from AWS Secrets Manager")]
    public static partial void DeserializationFailed(ILogger logger, string secretName, string targetType, Exception exception);

    [LoggerMessage(EventId = 5258, Level = LogLevel.Debug,
        Message = "Secret '{SecretName}' created in AWS Secrets Manager (did not exist, fallback from PutSecretValue)")]
    public static partial void SecretCreated(ILogger logger, string secretName);
}

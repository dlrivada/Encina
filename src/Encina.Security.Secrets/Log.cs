using Microsoft.Extensions.Logging;

namespace Encina.Security.Secrets;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators.
/// </summary>
internal static partial class Log
{
    // Secret operations: EventIds 1-19
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Secret '{SecretName}' retrieved from {ProviderName} provider")]
    public static partial void SecretRetrieved(ILogger logger, string secretName, string providerName);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Secret '{SecretName}' not found in {ProviderName} provider")]
    public static partial void SecretNotFound(ILogger logger, string secretName, string providerName);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Failed to deserialize secret '{SecretName}' to type '{TargetType}'")]
    public static partial void SecretDeserializationFailed(ILogger logger, string secretName, string targetType, Exception exception);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Secret provider '{ProviderName}' is unavailable")]
    public static partial void ProviderUnavailable(ILogger logger, string providerName, Exception exception);

    // Caching operations: EventIds 20-39
    [LoggerMessage(EventId = 20, Level = LogLevel.Debug, Message = "Cache hit for secret '{SecretName}'")]
    public static partial void CacheHit(ILogger logger, string secretName);

    [LoggerMessage(EventId = 21, Level = LogLevel.Debug, Message = "Cache miss for secret '{SecretName}'")]
    public static partial void CacheMiss(ILogger logger, string secretName);

    [LoggerMessage(EventId = 22, Level = LogLevel.Debug, Message = "Cache invalidated for secret '{SecretName}'")]
    public static partial void CacheInvalidated(ILogger logger, string secretName);

    // Configuration provider: EventIds 40-59
    [LoggerMessage(EventId = 40, Level = LogLevel.Information, Message = "Loading secrets into configuration from ISecretReader")]
    public static partial void ConfigurationLoadStarted(ILogger logger);

    [LoggerMessage(EventId = 41, Level = LogLevel.Information, Message = "Loaded {Count} secrets into configuration")]
    public static partial void ConfigurationLoadCompleted(ILogger logger, int count);

    [LoggerMessage(EventId = 42, Level = LogLevel.Warning, Message = "ISecretReader is not registered. No secrets will be loaded into configuration")]
    public static partial void ConfigurationNoReader(ILogger logger);

    [LoggerMessage(EventId = 43, Level = LogLevel.Warning, Message = "Failed to load secret '{SecretName}' into configuration: {ErrorMessage}")]
    public static partial void ConfigurationSecretLoadFailed(ILogger logger, string secretName, string errorMessage);

    [LoggerMessage(EventId = 44, Level = LogLevel.Debug, Message = "Configuration reload triggered for secrets")]
    public static partial void ConfigurationReloadTriggered(ILogger logger);

    // Health check: EventIds 60-69
    [LoggerMessage(EventId = 60, Level = LogLevel.Debug, Message = "Secrets health check started")]
    public static partial void HealthCheckStarted(ILogger logger);

    [LoggerMessage(EventId = 61, Level = LogLevel.Debug, Message = "Secrets health check completed: {Status}")]
    public static partial void HealthCheckCompleted(ILogger logger, string status);

    // Registration: EventIds 70-79
    [LoggerMessage(EventId = 70, Level = LogLevel.Information, Message = "Encina secrets services registered with caching={CachingEnabled}, healthCheck={HealthCheckEnabled}")]
    public static partial void ServicesRegistered(ILogger logger, bool cachingEnabled, bool healthCheckEnabled);

    // Failover operations: EventIds 80-89
    [LoggerMessage(EventId = 80, Level = LogLevel.Warning, Message = "Provider '{ProviderType}' failed for secret '{SecretName}', trying next provider")]
    public static partial void FailoverTriggered(ILogger logger, string providerType, string secretName);

    [LoggerMessage(EventId = 81, Level = LogLevel.Error, Message = "All {ProviderCount} providers failed to retrieve secret '{SecretName}'")]
    public static partial void FailoverExhausted(ILogger logger, int providerCount, string secretName);

    [LoggerMessage(EventId = 82, Level = LogLevel.Debug, Message = "Secret '{SecretName}' retrieved from provider '{ProviderType}' after {FailedCount} failover(s)")]
    public static partial void FailoverSuccess(ILogger logger, string secretName, string providerType, int failedCount);

    // Auditing operations: EventIds 90-99
    [LoggerMessage(EventId = 90, Level = LogLevel.Debug, Message = "Audit entry recorded for secret access: '{SecretName}'")]
    public static partial void AuditEntryRecorded(ILogger logger, string secretName);

    [LoggerMessage(EventId = 91, Level = LogLevel.Warning, Message = "Failed to record audit entry for secret '{SecretName}'")]
    public static partial void AuditEntryFailed(ILogger logger, string secretName, Exception exception);

    [LoggerMessage(EventId = 92, Level = LogLevel.Debug, Message = "Access audited for secret '{SecretName}' by user '{UserId}'")]
    public static partial void AccessAudited(ILogger logger, string secretName, string userId);

    // Rotation operations: EventIds 100-109
    [LoggerMessage(EventId = 100, Level = LogLevel.Information, Message = "Secret rotation started for '{SecretName}'")]
    public static partial void RotationStarted(ILogger logger, string secretName);

    [LoggerMessage(EventId = 101, Level = LogLevel.Information, Message = "Secret rotation completed for '{SecretName}'")]
    public static partial void RotationCompleted(ILogger logger, string secretName);

    [LoggerMessage(EventId = 103, Level = LogLevel.Information, Message = "Secret rotation completed for '{SecretName}' in {ElapsedMs:F2}ms")]
    public static partial void RotationCompletedWithDuration(ILogger logger, string secretName, double elapsedMs);

    [LoggerMessage(EventId = 102, Level = LogLevel.Error, Message = "Secret rotation failed for '{SecretName}': {Reason}")]
    public static partial void RotationFailed(ILogger logger, string secretName, string reason);

    // Secret injection operations: EventIds 110-119
    [LoggerMessage(EventId = 110, Level = LogLevel.Debug, Message = "Secret injection started for {RequestType} with {PropertyCount} injectable properties")]
    public static partial void SecretInjectionStarted(ILogger logger, string requestType, int propertyCount);

    [LoggerMessage(EventId = 111, Level = LogLevel.Debug, Message = "Secret injection completed for {RequestType}, {InjectedCount} secrets injected in {ElapsedMs:F2}ms")]
    public static partial void SecretInjectionCompleted(ILogger logger, string requestType, int injectedCount, double elapsedMs);

    [LoggerMessage(EventId = 112, Level = LogLevel.Debug, Message = "Secret injection skipped for {RequestType} (no injectable properties)")]
    public static partial void SecretInjectionSkipped(ILogger logger, string requestType);

    [LoggerMessage(EventId = 113, Level = LogLevel.Warning, Message = "Secret '{SecretName}' injection into property '{PropertyName}' failed but FailOnError is false, continuing")]
    public static partial void SecretInjectionSkippedOnError(ILogger logger, string secretName, string propertyName);

    [LoggerMessage(EventId = 114, Level = LogLevel.Error, Message = "Secret injection failed for {RequestType}: secret '{SecretName}' for property '{PropertyName}' could not be resolved")]
    public static partial void SecretInjectionFailed(ILogger logger, string requestType, string secretName, string propertyName);
}

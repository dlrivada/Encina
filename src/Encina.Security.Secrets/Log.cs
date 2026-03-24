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

    // ── Distributed caching operations: EventIds 8950-8968 (see EventIdRanges.SecuritySecrets) ──

    [LoggerMessage(EventId = 8950, Level = LogLevel.Debug, Message = "Cache hit for secret '{SecretName}'")]
    public static partial void CacheHit(ILogger logger, string secretName);

    [LoggerMessage(EventId = 8951, Level = LogLevel.Debug, Message = "Cache miss for secret '{SecretName}'")]
    public static partial void CacheMiss(ILogger logger, string secretName);

    [LoggerMessage(EventId = 8952, Level = LogLevel.Debug, Message = "Cache invalidated for secret '{SecretName}'")]
    public static partial void CacheInvalidated(ILogger logger, string secretName);

    [LoggerMessage(EventId = 8953, Level = LogLevel.Warning, Message = "Cache operation failed for secret '{SecretName}' with key '{CacheKey}'")]
    public static partial void CacheError(ILogger logger, string secretName, string cacheKey, Exception exception);

    [LoggerMessage(EventId = 8954, Level = LogLevel.Debug, Message = "PubSub invalidation published for secret '{SecretName}' to channel '{Channel}'")]
    public static partial void PubSubInvalidationPublished(ILogger logger, string secretName, string channel);

    [LoggerMessage(EventId = 8955, Level = LogLevel.Debug, Message = "PubSub invalidation received for secret '{SecretName}' (operation: {Operation}) on channel '{Channel}'")]
    public static partial void PubSubInvalidationReceived(ILogger logger, string secretName, string operation, string channel);

    [LoggerMessage(EventId = 8956, Level = LogLevel.Information, Message = "PubSub subscription started on channel '{Channel}'")]
    public static partial void PubSubSubscriptionStarted(ILogger logger, string channel);

    [LoggerMessage(EventId = 8957, Level = LogLevel.Warning, Message = "PubSub subscription failed on channel '{Channel}'")]
    public static partial void PubSubSubscriptionFailed(ILogger logger, string channel, Exception exception);

    [LoggerMessage(EventId = 8958, Level = LogLevel.Warning, Message = "Serving stale (last-known-good) value for secret '{SecretName}'")]
    public static partial void CacheStaleFallbackServed(ILogger logger, string secretName);

    [LoggerMessage(EventId = 8959, Level = LogLevel.Debug, Message = "Bulk cache invalidation for pattern '{Pattern}'")]
    public static partial void CacheBulkInvalidated(ILogger logger, string pattern);

    [LoggerMessage(EventId = 8960, Level = LogLevel.Debug, Message = "Writer invalidated cache for secret '{SecretName}'")]
    public static partial void WriterCacheInvalidation(ILogger logger, string secretName);

    [LoggerMessage(EventId = 8961, Level = LogLevel.Warning, Message = "Cache write error for key '{CacheKey}'")]
    public static partial void CacheWriteError(ILogger logger, string cacheKey, Exception exception);

    [LoggerMessage(EventId = 8962, Level = LogLevel.Warning, Message = "Cache invalidation error for secret '{SecretName}'")]
    public static partial void CacheInvalidationError(ILogger logger, string secretName, Exception exception);

    [LoggerMessage(EventId = 8967, Level = LogLevel.Warning, Message = "Cache key/pattern removal error for '{KeyOrPattern}'")]
    public static partial void CacheKeyRemovalError(ILogger logger, string keyOrPattern, Exception exception);

    [LoggerMessage(EventId = 8963, Level = LogLevel.Warning, Message = "PubSub publish error for secret '{SecretName}' on channel '{Channel}'")]
    public static partial void PubSubPublishError(ILogger logger, string secretName, string channel, Exception exception);

    [LoggerMessage(EventId = 8964, Level = LogLevel.Information, Message = "PubSub subscription stopped on channel '{Channel}'")]
    public static partial void PubSubSubscriptionStopped(ILogger logger, string channel);

    [LoggerMessage(EventId = 8965, Level = LogLevel.Warning, Message = "PubSub subscription stop error on channel '{Channel}'")]
    public static partial void PubSubSubscriptionStopError(ILogger logger, string channel, Exception exception);

    [LoggerMessage(EventId = 8966, Level = LogLevel.Warning, Message = "Cache eviction error for secret '{SecretName}' (operation: {Operation})")]
    public static partial void CacheEvictionError(ILogger logger, string secretName, string operation, Exception exception);

    [LoggerMessage(EventId = 8968, Level = LogLevel.Information, Message = "PubSub provider not registered — cross-instance cache invalidation disabled for channel '{Channel}'")]
    public static partial void PubSubNotConfigured(ILogger logger, string channel);

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

    // Resilience operations: EventIds 120-129
    [LoggerMessage(EventId = 120, Level = LogLevel.Warning, Message = "Resilience retry attempt {AttemptNumber}/{MaxAttempts} after {DelayMs:F0}ms delay. Reason: {Reason}")]
    public static partial void ResilienceRetryAttempt(ILogger logger, int attemptNumber, int maxAttempts, double delayMs, string reason);

    [LoggerMessage(EventId = 121, Level = LogLevel.Warning, Message = "Secrets circuit breaker opened — subsequent requests will be rejected")]
    public static partial void ResilienceCircuitBreakerOpened(ILogger logger);

    [LoggerMessage(EventId = 122, Level = LogLevel.Information, Message = "Secrets circuit breaker closed — requests flowing normally")]
    public static partial void ResilienceCircuitBreakerClosed(ILogger logger);

    [LoggerMessage(EventId = 123, Level = LogLevel.Information, Message = "Secrets circuit breaker half-open — testing recovery with a single request")]
    public static partial void ResilienceCircuitBreakerHalfOpen(ILogger logger);

    [LoggerMessage(EventId = 124, Level = LogLevel.Warning, Message = "Secret operation timed out after {TimeoutSeconds:F0}s")]
    public static partial void ResilienceTimeoutExceeded(ILogger logger, double timeoutSeconds);

    // Stale fallback: superseded by CacheStaleFallbackServed (EventId 8958) in the registered range
}

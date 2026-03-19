using System.Diagnostics;

namespace Encina.Security.Secrets.Diagnostics;

/// <summary>
/// Provides the <see cref="ActivitySource"/> for distributed tracing of Encina secrets operations.
/// </summary>
/// <remarks>
/// <para>
/// All tracing is emitted under the <c>Encina.Security.Secrets</c> source name.
/// </para>
/// <para>
/// <b>Activities</b>:
/// <list type="bullet">
/// <item><c>Secrets.GetSecret</c> — Reading a secret from a provider (Client kind)</item>
/// <item><c>Secrets.SetSecret</c> — Writing a secret to a provider (Client kind)</item>
/// <item><c>Secrets.RotateSecret</c> — Rotating a secret (Client kind)</item>
/// <item><c>Secrets.InjectSecrets</c> — Pipeline injection of secrets (Internal kind)</item>
/// <item><c>Secrets.CacheHit</c> — Cache hit event on a secret read</item>
/// <item><c>Secrets.CacheMiss</c> — Cache miss event on a secret read</item>
/// </list>
/// </para>
/// <para>
/// <b>Standard tags</b>:
/// <list type="bullet">
/// <item><c>secrets.name</c> — The name of the secret being accessed</item>
/// <item><c>secrets.cache_hit</c> — Whether the secret was served from cache</item>
/// <item><c>secrets.provider_type</c> — The provider type name</item>
/// <item><c>secrets.error_code</c> — The error code on failure</item>
/// </list>
/// </para>
/// <para>
/// Tracing is gated by <see cref="SecretsOptions.EnableTracing"/>. When disabled,
/// activity methods return <c>null</c> immediately without allocating.
/// </para>
/// </remarks>
internal static class SecretsActivitySource
{
    internal const string SourceName = "Encina.Security.Secrets";
    internal const string SourceVersion = "1.0.0";

    internal static readonly ActivitySource Source = new(SourceName, SourceVersion);

    // Activity names
    internal const string GetSecretActivity = "Secrets.GetSecret";
    internal const string SetSecretActivity = "Secrets.SetSecret";
    internal const string RotateSecretActivity = "Secrets.RotateSecret";
    internal const string InjectSecretsActivity = "Secrets.InjectSecrets";
    internal const string CacheHitEvent = "Secrets.CacheHit";
    internal const string CacheMissEvent = "Secrets.CacheMiss";

    // Standard tag names
    internal const string TagSecretName = "secrets.name";
    internal const string TagCacheHit = "secrets.cache_hit";
    internal const string TagProviderType = "secrets.provider_type";
    internal const string TagErrorCode = "secrets.error_code";
    internal const string TagRequestType = "secrets.request_type";
    internal const string TagOperation = "secrets.operation";
    internal const string TagOutcome = "secrets.outcome";

    // Resilience-specific activity names
    internal const string ResilienceOperationActivity = "Secrets.Resilience";
    internal const string ResilienceRetryEvent = "Secrets.Resilience.Retry";
    internal const string ResilienceCircuitBreakerOpenEvent = "Secrets.Resilience.CircuitBreakerOpen";
    internal const string ResilienceCircuitBreakerClosedEvent = "Secrets.Resilience.CircuitBreakerClosed";
    internal const string ResilienceCircuitBreakerHalfOpenEvent = "Secrets.Resilience.CircuitBreakerHalfOpen";
    internal const string ResilienceTimeoutEvent = "Secrets.Resilience.Timeout";
    internal const string ResilienceStaleFallbackEvent = "Secrets.Resilience.StaleFallback";

    // Resilience-specific tag names
    internal const string TagRetryAttempt = "secrets.resilience.retry_attempt";
    internal const string TagRetryMaxAttempts = "secrets.resilience.retry_max_attempts";
    internal const string TagRetryDelayMs = "secrets.resilience.retry_delay_ms";
    internal const string TagCircuitBreakerState = "secrets.resilience.circuit_breaker_state";
    internal const string TagTimeoutDurationS = "secrets.resilience.timeout_duration_s";
    internal const string TagResilienceOutcome = "secrets.resilience.outcome";

    /// <summary>
    /// Starts a new activity for a secret read operation.
    /// </summary>
    /// <param name="secretName">The name of the secret being read.</param>
    /// <returns>The started activity, or <c>null</c> if no listeners are registered.</returns>
    internal static Activity? StartGetSecretActivity(string secretName)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity(GetSecretActivity, ActivityKind.Client);
        activity?.SetTag(TagSecretName, secretName);
        activity?.SetTag(TagOperation, "get");
        return activity;
    }

    /// <summary>
    /// Starts a new activity for a secret write operation.
    /// </summary>
    /// <param name="secretName">The name of the secret being written.</param>
    /// <returns>The started activity, or <c>null</c> if no listeners are registered.</returns>
    internal static Activity? StartSetSecretActivity(string secretName)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity(SetSecretActivity, ActivityKind.Client);
        activity?.SetTag(TagSecretName, secretName);
        activity?.SetTag(TagOperation, "set");
        return activity;
    }

    /// <summary>
    /// Starts a new activity for a secret rotation operation.
    /// </summary>
    /// <param name="secretName">The name of the secret being rotated.</param>
    /// <returns>The started activity, or <c>null</c> if no listeners are registered.</returns>
    internal static Activity? StartRotateSecretActivity(string secretName)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity(RotateSecretActivity, ActivityKind.Client);
        activity?.SetTag(TagSecretName, secretName);
        activity?.SetTag(TagOperation, "rotate");
        return activity;
    }

    /// <summary>
    /// Starts a new activity for the secret injection pipeline.
    /// </summary>
    /// <param name="requestType">The request type being processed.</param>
    /// <returns>The started activity, or <c>null</c> if no listeners are registered.</returns>
    internal static Activity? StartInjectSecretsActivity(Type requestType)
        => StartInjectSecretsActivity(requestType.Name);

    /// <summary>
    /// Starts a new activity for the secret injection pipeline.
    /// </summary>
    /// <param name="requestTypeName">The name of the request type being processed.</param>
    /// <returns>The started activity, or <c>null</c> if no listeners are registered.</returns>
    internal static Activity? StartInjectSecretsActivity(string requestTypeName)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity(InjectSecretsActivity, ActivityKind.Internal);
        activity?.SetTag(TagRequestType, requestTypeName);
        activity?.SetTag(TagOperation, "inject");
        return activity;
    }

    /// <summary>
    /// Records a cache hit event on an activity.
    /// </summary>
    internal static void RecordCacheHit(Activity? activity, string secretName)
    {
        activity?.SetTag(TagCacheHit, true);
        activity?.AddEvent(new ActivityEvent(CacheHitEvent, tags: new ActivityTagsCollection
        {
            { TagSecretName, secretName }
        }));
    }

    /// <summary>
    /// Records a cache miss event on an activity.
    /// </summary>
    internal static void RecordCacheMiss(Activity? activity, string secretName)
    {
        activity?.SetTag(TagCacheHit, false);
        activity?.AddEvent(new ActivityEvent(CacheMissEvent, tags: new ActivityTagsCollection
        {
            { TagSecretName, secretName }
        }));
    }

    /// <summary>
    /// Records that an operation completed successfully.
    /// </summary>
    internal static void RecordSuccess(Activity? activity)
    {
        activity?.SetTag(TagOutcome, "success");
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    /// <summary>
    /// Records that an operation failed.
    /// </summary>
    internal static void RecordFailure(Activity? activity, string errorCode, string? errorMessage = null)
    {
        activity?.SetTag(TagOutcome, "failure");
        activity?.SetTag(TagErrorCode, errorCode);
        activity?.SetStatus(ActivityStatusCode.Error, errorMessage ?? errorCode);
    }

    /// <summary>
    /// Starts a new activity for a resilient secret operation.
    /// </summary>
    /// <param name="secretName">The name of the secret being accessed.</param>
    /// <param name="operation">The operation type (get, set, rotate).</param>
    /// <returns>The started activity, or <c>null</c> if no listeners are registered.</returns>
    internal static Activity? StartResilienceActivity(string secretName, string operation)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity(ResilienceOperationActivity, ActivityKind.Internal);
        activity?.SetTag(TagSecretName, secretName);
        activity?.SetTag(TagOperation, operation);
        return activity;
    }

    /// <summary>
    /// Records a retry event on a resilience activity.
    /// </summary>
    internal static void RecordRetryEvent(Activity? activity, int attemptNumber, int maxAttempts, double delayMs, string reason)
    {
        activity?.AddEvent(new ActivityEvent(ResilienceRetryEvent, tags: new ActivityTagsCollection
        {
            { TagRetryAttempt, attemptNumber },
            { TagRetryMaxAttempts, maxAttempts },
            { TagRetryDelayMs, delayMs },
            { "reason", reason }
        }));
    }

    /// <summary>
    /// Records a circuit breaker state change event.
    /// </summary>
    internal static void RecordCircuitBreakerEvent(Activity? activity, string state)
    {
        var eventName = state switch
        {
            "opened" => ResilienceCircuitBreakerOpenEvent,
            "closed" => ResilienceCircuitBreakerClosedEvent,
            "half_open" => ResilienceCircuitBreakerHalfOpenEvent,
            _ => ResilienceCircuitBreakerOpenEvent
        };

        activity?.AddEvent(new ActivityEvent(eventName, tags: new ActivityTagsCollection
        {
            { TagCircuitBreakerState, state }
        }));
    }

    /// <summary>
    /// Records a timeout event on a resilience activity.
    /// </summary>
    internal static void RecordTimeoutEvent(Activity? activity, double timeoutSeconds)
    {
        activity?.AddEvent(new ActivityEvent(ResilienceTimeoutEvent, tags: new ActivityTagsCollection
        {
            { TagTimeoutDurationS, timeoutSeconds }
        }));
        activity?.SetTag(TagResilienceOutcome, "timeout");
        activity?.SetStatus(ActivityStatusCode.Error, "Operation timed out");
    }

    /// <summary>
    /// Records a stale fallback event on a resilience activity.
    /// </summary>
    internal static void RecordStaleFallbackEvent(Activity? activity, string secretName)
    {
        activity?.AddEvent(new ActivityEvent(ResilienceStaleFallbackEvent, tags: new ActivityTagsCollection
        {
            { TagSecretName, secretName }
        }));
        activity?.SetTag(TagResilienceOutcome, "stale_fallback");
    }
}

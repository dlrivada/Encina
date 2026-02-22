using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.Security.Secrets.Diagnostics;

/// <summary>
/// Legacy static façade for backward compatibility with existing code.
/// Delegates to <see cref="SecretsActivitySource"/> and provides static metric instruments.
/// </summary>
/// <remarks>
/// New code should use <see cref="SecretsActivitySource"/> for tracing
/// and <see cref="SecretsMetrics"/> (DI-injectable) for metrics.
/// </remarks>
internal static class SecretsDiagnostics
{
    internal const string SourceName = SecretsActivitySource.SourceName;
    internal const string SourceVersion = SecretsActivitySource.SourceVersion;

    internal static readonly ActivitySource ActivitySource = SecretsActivitySource.Source;
    internal static readonly Meter Meter = new(SourceName, SourceVersion);

    // Counters (static, for code not yet migrated to SecretsMetrics)
    internal static readonly Counter<long> OperationsTotal =
        Meter.CreateCounter<long>("secrets.operations",
            description: "Total number of secret operations.");

    internal static readonly Counter<long> FailuresTotal =
        Meter.CreateCounter<long>("secrets.failures",
            description: "Total number of failed secret operations.");

    internal static readonly Counter<long> CacheHitsTotal =
        Meter.CreateCounter<long>("secrets.cache.hits",
            description: "Total number of cache hits for secret reads.");

    internal static readonly Counter<long> CacheMissesTotal =
        Meter.CreateCounter<long>("secrets.cache.misses",
            description: "Total number of cache misses for secret reads.");

    // Histogram
    internal static readonly Histogram<double> OperationDuration =
        Meter.CreateHistogram<double>("secrets.duration",
            unit: "ms",
            description: "Duration of secret operations in milliseconds.");

    // Injection counters
    internal static readonly Counter<long> InjectionsTotal =
        Meter.CreateCounter<long>("secrets.injections",
            description: "Total number of secret injection pipeline executions.");

    internal static readonly Counter<long> PropertiesInjected =
        Meter.CreateCounter<long>("secrets.properties_injected",
            description: "Total number of properties injected with secrets.");

    // Tag constants — delegate to SecretsActivitySource
    internal const string TagSecretName = SecretsActivitySource.TagSecretName;
    internal const string TagOperation = SecretsActivitySource.TagOperation;
    internal const string TagProvider = SecretsActivitySource.TagProviderType;
    internal const string TagOutcome = SecretsActivitySource.TagOutcome;
    internal const string TagCached = SecretsActivitySource.TagCacheHit;
    internal const string TagRequestType = SecretsActivitySource.TagRequestType;
    internal const string TagPropertyName = "secrets.property_name";

    /// <summary>
    /// Starts a new activity for a secret read operation.
    /// </summary>
    internal static Activity? StartGetSecret(string secretName)
        => SecretsActivitySource.StartGetSecretActivity(secretName);

    /// <summary>
    /// Starts a new activity for a secret write operation.
    /// </summary>
    internal static Activity? StartSetSecret(string secretName)
        => SecretsActivitySource.StartSetSecretActivity(secretName);

    /// <summary>
    /// Starts a new activity for a secret rotation operation.
    /// </summary>
    internal static Activity? StartRotateSecret(string secretName)
        => SecretsActivitySource.StartRotateSecretActivity(secretName);

    /// <summary>
    /// Starts a new activity for a secret injection pipeline operation.
    /// </summary>
    internal static Activity? StartSecretInjection(string requestTypeName)
        => SecretsActivitySource.StartInjectSecretsActivity(requestTypeName);

    /// <summary>
    /// Records that an operation completed successfully.
    /// </summary>
    internal static void RecordSuccess(Activity? activity)
        => SecretsActivitySource.RecordSuccess(activity);

    /// <summary>
    /// Records that an operation failed.
    /// </summary>
    internal static void RecordFailure(Activity? activity, string errorMessage)
        => SecretsActivitySource.RecordFailure(activity, "unknown", errorMessage);
}

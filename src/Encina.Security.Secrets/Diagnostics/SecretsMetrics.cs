using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.Security.Secrets.Diagnostics;

/// <summary>
/// Provides OpenTelemetry-compatible metrics for Encina secrets management.
/// </summary>
/// <remarks>
/// <para>
/// This class uses <see cref="IMeterFactory"/> for proper DI integration and testability.
/// All metrics are emitted under the <c>Encina.Security.Secrets</c> meter name.
/// </para>
/// <para>
/// <b>Counters</b>:
/// <list type="bullet">
/// <item><c>secrets.get.count</c> — Total secret read operations</item>
/// <item><c>secrets.set.count</c> — Total secret write operations</item>
/// <item><c>secrets.rotation.count</c> — Total secret rotation operations</item>
/// <item><c>secrets.cache.hits</c> — Total cache hits</item>
/// <item><c>secrets.cache.misses</c> — Total cache misses</item>
/// <item><c>secrets.injection.count</c> — Total pipeline injection operations</item>
/// <item><c>secrets.failover.count</c> — Total failover transitions</item>
/// </list>
/// </para>
/// <para>
/// <b>Histograms</b>:
/// <list type="bullet">
/// <item><c>secrets.get.duration</c> — Duration of secret read operations (ms)</item>
/// <item><c>secrets.injection.duration</c> — Duration of injection pipeline operations (ms)</item>
/// </list>
/// </para>
/// <para>
/// Metrics are gated by <see cref="SecretsOptions.EnableMetrics"/>. Callers should check
/// the option before calling record methods to avoid unnecessary allocations.
/// </para>
/// </remarks>
internal sealed class SecretsMetrics
{
    internal const string MeterName = "Encina.Security.Secrets";
    internal const string MeterVersion = "1.0.0";

    // Counter instruments
    private readonly Counter<long> _getCount;
    private readonly Counter<long> _setCount;
    private readonly Counter<long> _rotationCount;
    private readonly Counter<long> _cacheHits;
    private readonly Counter<long> _cacheMisses;
    private readonly Counter<long> _injectionCount;
    private readonly Counter<long> _failoverCount;

    // Histogram instruments
    private readonly Histogram<double> _getDuration;
    private readonly Histogram<double> _injectionDuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretsMetrics"/> class.
    /// </summary>
    /// <param name="meterFactory">The meter factory used to create the meter instance.</param>
    public SecretsMetrics(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);

        var meter = meterFactory.Create(MeterName, MeterVersion);

        _getCount = meter.CreateCounter<long>(
            "secrets.get.count",
            description: "Total number of secret read operations.");

        _setCount = meter.CreateCounter<long>(
            "secrets.set.count",
            description: "Total number of secret write operations.");

        _rotationCount = meter.CreateCounter<long>(
            "secrets.rotation.count",
            description: "Total number of secret rotation operations.");

        _cacheHits = meter.CreateCounter<long>(
            "secrets.cache.hits",
            description: "Total number of cache hits for secret reads.");

        _cacheMisses = meter.CreateCounter<long>(
            "secrets.cache.misses",
            description: "Total number of cache misses for secret reads.");

        _injectionCount = meter.CreateCounter<long>(
            "secrets.injection.count",
            description: "Total number of secret injection pipeline executions.");

        _failoverCount = meter.CreateCounter<long>(
            "secrets.failover.count",
            description: "Total number of failover transitions between providers.");

        _getDuration = meter.CreateHistogram<double>(
            "secrets.get.duration",
            unit: "ms",
            description: "Duration of secret read operations in milliseconds.");

        _injectionDuration = meter.CreateHistogram<double>(
            "secrets.injection.duration",
            unit: "ms",
            description: "Duration of injection pipeline operations in milliseconds.");
    }

    /// <summary>
    /// Records a secret read operation.
    /// </summary>
    /// <param name="secretName">The name of the secret.</param>
    /// <param name="success">Whether the operation succeeded.</param>
    /// <param name="elapsed">The elapsed time of the operation.</param>
    /// <param name="errorCode">The error code if the operation failed.</param>
    public void RecordGetSecret(string secretName, bool success, TimeSpan elapsed, string? errorCode = null)
    {
        var tags = new TagList
        {
            { SecretsActivitySource.TagSecretName, secretName },
            { SecretsActivitySource.TagOutcome, success ? "success" : "failure" }
        };

        if (errorCode is not null)
        {
            tags.Add(SecretsActivitySource.TagErrorCode, errorCode);
        }

        _getCount.Add(1, tags);
        _getDuration.Record(elapsed.TotalMilliseconds, tags);
    }

    /// <summary>
    /// Records a secret write operation.
    /// </summary>
    /// <param name="secretName">The name of the secret.</param>
    /// <param name="success">Whether the operation succeeded.</param>
    /// <param name="errorCode">The error code if the operation failed.</param>
    public void RecordSetSecret(string secretName, bool success, string? errorCode = null)
    {
        var tags = new TagList
        {
            { SecretsActivitySource.TagSecretName, secretName },
            { SecretsActivitySource.TagOutcome, success ? "success" : "failure" }
        };

        if (errorCode is not null)
        {
            tags.Add(SecretsActivitySource.TagErrorCode, errorCode);
        }

        _setCount.Add(1, tags);
    }

    /// <summary>
    /// Records a secret rotation operation.
    /// </summary>
    /// <param name="secretName">The name of the secret.</param>
    /// <param name="success">Whether the operation succeeded.</param>
    /// <param name="errorCode">The error code if the operation failed.</param>
    public void RecordRotation(string secretName, bool success, string? errorCode = null)
    {
        var tags = new TagList
        {
            { SecretsActivitySource.TagSecretName, secretName },
            { SecretsActivitySource.TagOutcome, success ? "success" : "failure" }
        };

        if (errorCode is not null)
        {
            tags.Add(SecretsActivitySource.TagErrorCode, errorCode);
        }

        _rotationCount.Add(1, tags);
    }

    /// <summary>
    /// Records a cache hit for a secret read.
    /// </summary>
    /// <param name="secretName">The name of the secret.</param>
    public void RecordCacheHit(string secretName)
    {
        _cacheHits.Add(1,
            new KeyValuePair<string, object?>(SecretsActivitySource.TagSecretName, secretName));
    }

    /// <summary>
    /// Records a cache miss for a secret read.
    /// </summary>
    /// <param name="secretName">The name of the secret.</param>
    public void RecordCacheMiss(string secretName)
    {
        _cacheMisses.Add(1,
            new KeyValuePair<string, object?>(SecretsActivitySource.TagSecretName, secretName));
    }

    /// <summary>
    /// Records a pipeline injection operation.
    /// </summary>
    /// <param name="requestType">The request type being injected.</param>
    /// <param name="success">Whether the injection succeeded.</param>
    /// <param name="elapsed">The elapsed time of the injection.</param>
    /// <param name="propertiesInjected">The number of properties injected.</param>
    public void RecordInjection(Type requestType, bool success, TimeSpan elapsed, int propertiesInjected = 0)
    {
        var tags = new TagList
        {
            { SecretsActivitySource.TagRequestType, requestType.Name },
            { SecretsActivitySource.TagOutcome, success ? "success" : "failure" }
        };

        _injectionCount.Add(1, tags);
        _injectionDuration.Record(elapsed.TotalMilliseconds, tags);

        if (propertiesInjected > 0)
        {
            _getCount.Add(propertiesInjected, tags);
        }
    }

    /// <summary>
    /// Records a failover transition between providers.
    /// </summary>
    /// <param name="secretName">The name of the secret.</param>
    /// <param name="providerType">The provider type that failed.</param>
    public void RecordFailover(string secretName, string providerType)
    {
        var tags = new TagList
        {
            { SecretsActivitySource.TagSecretName, secretName },
            { SecretsActivitySource.TagProviderType, providerType }
        };

        _failoverCount.Add(1, tags);
    }
}

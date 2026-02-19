using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.Secrets.Diagnostics;

/// <summary>
/// Provides metrics instrumentation for Encina.Secrets operations.
/// </summary>
/// <remarks>
/// <para>
/// Exposes the following instruments:
/// <list type="bullet">
/// <item><c>encina.secrets.operations</c> — Counter of total secret operations</item>
/// <item><c>encina.secrets.duration</c> — Histogram of operation duration in milliseconds</item>
/// <item><c>encina.secrets.errors</c> — Counter of failed operations</item>
/// </list>
/// </para>
/// <para>
/// All instruments use standard tags: <c>secrets.operation</c>, <c>secrets.provider</c>,
/// and optionally <c>secrets.name</c> (when <see cref="SecretsInstrumentationOptions.RecordSecretNames"/> is enabled).
/// </para>
/// </remarks>
internal sealed class SecretsMetrics
{
    /// <summary>
    /// The meter name used for OpenTelemetry registration.
    /// </summary>
    public const string MeterName = "Encina.Secrets";

    private readonly Counter<long> _operationsCounter;
    private readonly Histogram<double> _durationHistogram;
    private readonly Counter<long> _errorsCounter;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretsMetrics"/> class.
    /// </summary>
    /// <param name="meterFactory">The meter factory to create the meter and instruments.</param>
    public SecretsMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _operationsCounter = meter.CreateCounter<long>(
            "encina.secrets.operations",
            unit: "{operations}",
            description: "Total number of secret provider operations.");

        _durationHistogram = meter.CreateHistogram<double>(
            "encina.secrets.duration",
            unit: "ms",
            description: "Duration of secret provider operations in milliseconds.");

        _errorsCounter = meter.CreateCounter<long>(
            "encina.secrets.errors",
            unit: "{errors}",
            description: "Total number of failed secret provider operations.");
    }

    /// <summary>
    /// Records a successful operation.
    /// </summary>
    /// <param name="operation">The operation name.</param>
    /// <param name="durationMs">The duration in milliseconds.</param>
    /// <param name="secretName">The secret name tag, or <c>null</c> if not recorded.</param>
    public void RecordSuccess(string operation, double durationMs, string? secretName = null)
    {
        var tags = CreateTags(operation, secretName);
        _operationsCounter.Add(1, tags);
        _durationHistogram.Record(durationMs, tags);
    }

    /// <summary>
    /// Records a failed operation.
    /// </summary>
    /// <param name="operation">The operation name.</param>
    /// <param name="durationMs">The duration in milliseconds.</param>
    /// <param name="errorCode">The error code from <see cref="SecretsErrorCodes"/>.</param>
    /// <param name="secretName">The secret name tag, or <c>null</c> if not recorded.</param>
    public void RecordError(string operation, double durationMs, string errorCode, string? secretName = null)
    {
        var tags = CreateTags(operation, secretName);
        _operationsCounter.Add(1, tags);
        _durationHistogram.Record(durationMs, tags);
        _errorsCounter.Add(1, new TagList
        {
            { "secrets.operation", operation },
            { "secrets.error_code", errorCode },
        });
    }

    private static TagList CreateTags(string operation, string? secretName)
    {
        var tags = new TagList
        {
            { "secrets.operation", operation },
        };

        if (secretName is not null)
        {
            tags.Add("secrets.name", secretName);
        }

        return tags;
    }
}

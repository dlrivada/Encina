namespace Encina.Secrets.Diagnostics;

/// <summary>
/// Configuration options for Encina.Secrets instrumentation.
/// </summary>
/// <remarks>
/// <para>
/// Controls which telemetry signals (tracing, metrics) are enabled and whether
/// secret names are included in telemetry data.
/// </para>
/// <para>
/// <b>Security note:</b> <see cref="RecordSecretNames"/> defaults to <c>false</c>
/// because secret names may contain sensitive information (e.g., environment names,
/// service identifiers) that should not appear in telemetry backends by default.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaSecretsInstrumentation(options =>
/// {
///     options.RecordSecretNames = true;
///     options.EnableTracing = true;
///     options.EnableMetrics = true;
/// });
/// </code>
/// </example>
public sealed class SecretsInstrumentationOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether secret names are recorded in traces and metrics.
    /// </summary>
    /// <value>
    /// <c>true</c> to include secret names as tags in activities and metric dimensions;
    /// <c>false</c> to omit them. Defaults to <c>false</c> for security.
    /// </value>
    public bool RecordSecretNames { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether distributed tracing via <see cref="System.Diagnostics.ActivitySource"/> is enabled.
    /// </summary>
    /// <value>
    /// <c>true</c> to create activities for secret operations; <c>false</c> to disable.
    /// Defaults to <c>true</c>.
    /// </value>
    public bool EnableTracing { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether metrics collection via <see cref="System.Diagnostics.Metrics.Meter"/> is enabled.
    /// </summary>
    /// <value>
    /// <c>true</c> to emit operation counters and duration histograms; <c>false</c> to disable.
    /// Defaults to <c>true</c>.
    /// </value>
    public bool EnableMetrics { get; set; } = true;
}

namespace Encina.OpenTelemetry.Migrations;

/// <summary>
/// Provides callback delegates for migration observable gauge metrics.
/// </summary>
/// <remarks>
/// <para>
/// This class bridges the <c>Encina</c> migration coordination package with
/// <c>Encina.OpenTelemetry</c> without creating a direct project reference.
/// The migration infrastructure registers an instance of this class with the
/// appropriate callbacks, and the <see cref="MigrationMetricsInitializer"/> uses
/// it to create the <see cref="MigrationMetrics"/> on startup.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registered by Encina when shard migration coordination is enabled:
/// services.AddSingleton(new MigrationMetricsCallbacks(
///     driftDetectedCountCallback: () => cachedDriftCount));
/// </code>
/// </example>
public sealed class MigrationMetricsCallbacks
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationMetricsCallbacks"/> class.
    /// </summary>
    /// <param name="driftDetectedCountCallback">
    /// Callback that returns the current number of shards with detected schema drift.
    /// </param>
    public MigrationMetricsCallbacks(
        Func<int> driftDetectedCountCallback)
    {
        ArgumentNullException.ThrowIfNull(driftDetectedCountCallback);

        DriftDetectedCountCallback = driftDetectedCountCallback;
    }

    /// <summary>
    /// Gets the callback that returns the number of shards with detected schema drift.
    /// </summary>
    internal Func<int> DriftDetectedCountCallback { get; }
}

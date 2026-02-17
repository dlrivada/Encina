namespace Encina.OpenTelemetry.Resharding;

/// <summary>
/// Provides callback delegates for resharding observable gauge metrics.
/// </summary>
/// <remarks>
/// <para>
/// This class bridges the <c>Encina</c> resharding module with
/// <c>Encina.OpenTelemetry</c> without creating a direct project reference.
/// The resharding orchestrator registers an instance of this class with the
/// appropriate callbacks, and the <see cref="ReshardingMetricsInitializer"/> uses
/// it to create the <see cref="ReshardingMetrics"/> on startup.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registered by Encina when resharding is enabled:
/// services.AddSingleton(new ReshardingMetricsCallbacks(
///     rowsPerSecondCallback: () => currentRowsPerSecond,
///     cdcLagMsCallback: () => currentCdcLagMs,
///     activeReshardingCountCallback: () => activeCount));
/// </code>
/// </example>
public sealed class ReshardingMetricsCallbacks
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReshardingMetricsCallbacks"/> class.
    /// </summary>
    /// <param name="rowsPerSecondCallback">
    /// Callback that returns the current rows-per-second throughput during the copy phase.
    /// </param>
    /// <param name="cdcLagMsCallback">
    /// Callback that returns the current CDC replication lag in milliseconds.
    /// </param>
    /// <param name="activeReshardingCountCallback">
    /// Callback that returns the number of currently active resharding operations.
    /// </param>
    public ReshardingMetricsCallbacks(
        Func<double> rowsPerSecondCallback,
        Func<double> cdcLagMsCallback,
        Func<int> activeReshardingCountCallback)
    {
        ArgumentNullException.ThrowIfNull(rowsPerSecondCallback);
        ArgumentNullException.ThrowIfNull(cdcLagMsCallback);
        ArgumentNullException.ThrowIfNull(activeReshardingCountCallback);

        RowsPerSecondCallback = rowsPerSecondCallback;
        CdcLagMsCallback = cdcLagMsCallback;
        ActiveReshardingCountCallback = activeReshardingCountCallback;
    }

    /// <summary>
    /// Gets the callback that returns the current rows-per-second throughput.
    /// </summary>
    internal Func<double> RowsPerSecondCallback { get; }

    /// <summary>
    /// Gets the callback that returns the current CDC replication lag in milliseconds.
    /// </summary>
    internal Func<double> CdcLagMsCallback { get; }

    /// <summary>
    /// Gets the callback that returns the number of active resharding operations.
    /// </summary>
    internal Func<int> ActiveReshardingCountCallback { get; }
}

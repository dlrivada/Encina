using System.Diagnostics.Metrics;
using Encina.Database;

namespace Encina.Diagnostics;

/// <summary>
/// Exposes database connection pool metrics via the <c>Encina</c> meter using
/// <see cref="ObservableGauge{T}"/> instruments.
/// </summary>
/// <remarks>
/// <para>
/// The following instruments are registered:
/// <list type="bullet">
/// <item><description><c>Encina.db.pool.connections.active</c> — Active connections in use</description></item>
/// <item><description><c>Encina.db.pool.connections.idle</c> — Idle connections available</description></item>
/// <item><description><c>Encina.db.pool.utilization</c> — Pool utilization ratio (0.0 to 1.0)</description></item>
/// <item><description><c>Encina.db.circuit_breaker.state</c> — Circuit breaker state (0 = closed, 1 = open)</description></item>
/// </list>
/// </para>
/// <para>
/// All instruments use the <c>db.provider</c> tag to identify the source provider
/// (e.g., <c>"ado-sqlserver"</c>, <c>"dapper-postgresql"</c>, <c>"efcore"</c>, <c>"mongodb"</c>).
/// </para>
/// <para>
/// Metrics are lazily observed: the callback is invoked by the metrics runtime only when
/// a listener (e.g., OpenTelemetry, Prometheus) is active.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Metrics are automatically registered when using AddEncinaOpenTelemetry
/// services.AddEncinaOpenTelemetry();
///
/// // Or manually:
/// services.AddSingleton&lt;DatabasePoolMetrics&gt;();
/// </code>
/// </example>
public sealed class DatabasePoolMetrics
{
    private static readonly Meter Meter = new("Encina", "1.0");

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabasePoolMetrics"/> class,
    /// registering observable gauge instruments for the given monitor.
    /// </summary>
    /// <param name="monitor">The database health monitor to observe.</param>
    public DatabasePoolMetrics(IDatabaseHealthMonitor monitor)
    {
        ArgumentNullException.ThrowIfNull(monitor);

        Meter.CreateObservableGauge(
            "Encina.db.pool.connections.active",
            () => CreateMeasurement(monitor, static s => s.ActiveConnections),
            unit: "{connections}",
            description: "Number of active database connections currently in use.");

        Meter.CreateObservableGauge(
            "Encina.db.pool.connections.idle",
            () => CreateMeasurement(monitor, static s => s.IdleConnections),
            unit: "{connections}",
            description: "Number of idle database connections available in the pool.");

        Meter.CreateObservableGauge(
            "Encina.db.pool.utilization",
            () => CreateMeasurement(monitor, static s => s.PoolUtilization),
            unit: "1",
            description: "Database connection pool utilization ratio (0.0 to 1.0).");

        Meter.CreateObservableGauge(
            "Encina.db.circuit_breaker.state",
            () => new Measurement<int>(
                monitor.IsCircuitOpen ? 1 : 0,
                new KeyValuePair<string, object?>("db.provider", monitor.ProviderName)),
            description: "Database circuit breaker state (0 = closed, 1 = open).");
    }

    private static Measurement<T> CreateMeasurement<T>(
        IDatabaseHealthMonitor monitor,
        Func<ConnectionPoolStats, T> selector)
        where T : struct
    {
        var stats = monitor.GetPoolStatistics();
        return new Measurement<T>(
            selector(stats),
            new KeyValuePair<string, object?>("db.provider", monitor.ProviderName));
    }
}

namespace Encina.Database;

/// <summary>
/// Provides connection pool monitoring, health checking, and circuit breaker state
/// for a database provider.
/// </summary>
/// <remarks>
/// <para>
/// This interface defines the core contract for database resilience monitoring.
/// Each of the 13 database providers (ADO.NET ×4, Dapper ×4, EF Core ×4, MongoDB ×1)
/// must implement this interface with provider-specific pool statistics and health checks.
/// </para>
/// <para>
/// Implementations should be registered as singletons in the DI container and are designed
/// to be consumed by ASP.NET Core health check endpoints, OpenTelemetry metrics, and
/// Kubernetes readiness/liveness probes.
/// </para>
/// <para>
/// The interface is purely abstract with no default implementations, following the Encina
/// convention of keeping core abstractions free from provider-specific logic.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Consuming the monitor in application code
/// public class PoolUtilizationAlertService(IDatabaseHealthMonitor monitor, ILogger logger)
/// {
///     public async Task CheckPoolHealthAsync(CancellationToken ct)
///     {
///         var stats = monitor.GetPoolStatistics();
///         if (stats.PoolUtilization > 0.8)
///         {
///             logger.LogWarning(
///                 "Pool utilization high for {Provider}: {Utilization:P0}",
///                 monitor.ProviderName, stats.PoolUtilization);
///         }
///
///         if (monitor.IsCircuitOpen)
///         {
///             logger.LogError("Circuit breaker is open for {Provider}", monitor.ProviderName);
///         }
///
///         var health = await monitor.CheckHealthAsync(ct);
///         if (health.Status != DatabaseHealthStatus.Healthy)
///         {
///             logger.LogWarning("Database health: {Status} - {Description}",
///                 health.Status, health.Description);
///         }
///     }
/// }
/// </code>
/// </example>
public interface IDatabaseHealthMonitor
{
    /// <summary>
    /// Gets the name of the database provider this monitor is associated with.
    /// </summary>
    /// <remarks>
    /// Should be a lowercase, kebab-case identifier (e.g., "ado-sqlserver", "dapper-postgresql",
    /// "efcore-mysql", "mongodb"). Used for metrics tagging and health check identification.
    /// </remarks>
    string ProviderName { get; }

    /// <summary>
    /// Gets a value indicating whether the circuit breaker is currently open.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, the circuit breaker has tripped due to repeated failures and
    /// new database operations will fail fast without attempting a connection.
    /// The circuit will close after the configured <see cref="DatabaseCircuitBreakerOptions.BreakDuration"/>.
    /// </remarks>
    /// <value><c>true</c> if the circuit breaker is open; otherwise, <c>false</c>.</value>
    bool IsCircuitOpen { get; }

    /// <summary>
    /// Gets a snapshot of the current connection pool statistics.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns a point-in-time view of the connection pool state. The data may be
    /// slightly stale depending on the provider's refresh interval.
    /// </para>
    /// <para>
    /// Providers that do not support pool statistics (e.g., SQLite in-memory databases)
    /// should return <see cref="ConnectionPoolStats.CreateEmpty"/> instead of throwing.
    /// </para>
    /// </remarks>
    /// <returns>A <see cref="ConnectionPoolStats"/> snapshot of the current pool state.</returns>
    ConnectionPoolStats GetPoolStatistics();

    /// <summary>
    /// Performs an active health check by attempting to connect to the database
    /// and execute a lightweight query.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method typically executes <c>SELECT 1</c> (or the equivalent for the provider)
    /// and reports the result. The check should respect the configured timeout.
    /// </para>
    /// <para>
    /// The returned <see cref="DatabaseHealthResult"/> may include pool statistics
    /// in its <see cref="DatabaseHealthResult.Data"/> dictionary for observability.
    /// </para>
    /// </remarks>
    /// <param name="cancellationToken">A token to cancel the health check.</param>
    /// <returns>A <see cref="DatabaseHealthResult"/> indicating the database health status.</returns>
    Task<DatabaseHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the connection pool, closing all idle and active connections.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This operation is provider-specific and may not be supported by all databases.
    /// For example, SQL Server supports <c>SqlConnection.ClearPool()</c>, while SQLite
    /// in-memory databases do not have a pool to clear.
    /// </para>
    /// <para>
    /// Providers that do not support pool clearing should complete the task successfully
    /// without throwing.
    /// </para>
    /// </remarks>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the pool has been cleared.</returns>
    Task ClearPoolAsync(CancellationToken cancellationToken = default);
}

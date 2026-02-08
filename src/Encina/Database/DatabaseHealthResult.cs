using System.Collections.Immutable;

namespace Encina.Database;

/// <summary>
/// Represents the result of a database health check operation.
/// </summary>
/// <remarks>
/// <para>
/// This is a lightweight, core-level health check result for database connection monitoring.
/// It is independent of the messaging-level <c>HealthCheckResult</c> in <c>Encina.Messaging.Health</c>
/// to avoid circular dependencies, but follows the same status semantics.
/// </para>
/// <para>
/// The status follows standard health check conventions:
/// <list type="bullet">
/// <item><description><b>Healthy</b>: Database connection is functioning correctly.</description></item>
/// <item><description><b>Degraded</b>: Database is reachable but experiencing issues (e.g., high pool utilization).</description></item>
/// <item><description><b>Unhealthy</b>: Database is unreachable or the circuit breaker is open.</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = await monitor.CheckHealthAsync(cancellationToken);
/// if (result.Status == DatabaseHealthStatus.Unhealthy)
/// {
///     logger.LogError("Database unhealthy: {Description}", result.Description);
/// }
/// </code>
/// </example>
public readonly record struct DatabaseHealthResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseHealthResult"/> struct.
    /// </summary>
    /// <param name="status">The health status of the database.</param>
    /// <param name="description">An optional human-readable description of the health status.</param>
    /// <param name="exception">An optional exception that caused the health check to fail.</param>
    /// <param name="data">Optional additional data about the health check (e.g., pool statistics).</param>
    public DatabaseHealthResult(
        DatabaseHealthStatus status,
        string? description = null,
        Exception? exception = null,
        IReadOnlyDictionary<string, object>? data = null)
    {
        Status = status;
        Description = description;
        Exception = exception;
        Data = data ?? ImmutableDictionary<string, object>.Empty;
    }

    /// <summary>
    /// Gets the health status of the database.
    /// </summary>
    public DatabaseHealthStatus Status { get; }

    /// <summary>
    /// Gets an optional human-readable description of the health status.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets an optional exception that caused the health check to fail.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets optional additional data about the health check.
    /// </summary>
    /// <remarks>
    /// Can include metrics such as pool utilization, active connections, or latency measurements.
    /// </remarks>
    public IReadOnlyDictionary<string, object> Data { get; }

    /// <summary>
    /// Creates a healthy result indicating the database connection is functioning correctly.
    /// </summary>
    /// <param name="description">An optional description.</param>
    /// <param name="data">Optional additional data.</param>
    /// <returns>A healthy <see cref="DatabaseHealthResult"/>.</returns>
    public static DatabaseHealthResult Healthy(
        string? description = null,
        IReadOnlyDictionary<string, object>? data = null)
        => new(DatabaseHealthStatus.Healthy, description, null, data);

    /// <summary>
    /// Creates a degraded result indicating the database is reachable but experiencing issues.
    /// </summary>
    /// <param name="description">An optional description.</param>
    /// <param name="exception">An optional exception.</param>
    /// <param name="data">Optional additional data.</param>
    /// <returns>A degraded <see cref="DatabaseHealthResult"/>.</returns>
    public static DatabaseHealthResult Degraded(
        string? description = null,
        Exception? exception = null,
        IReadOnlyDictionary<string, object>? data = null)
        => new(DatabaseHealthStatus.Degraded, description, exception, data);

    /// <summary>
    /// Creates an unhealthy result indicating the database is unreachable or failing.
    /// </summary>
    /// <param name="description">An optional description.</param>
    /// <param name="exception">An optional exception.</param>
    /// <param name="data">Optional additional data.</param>
    /// <returns>An unhealthy <see cref="DatabaseHealthResult"/>.</returns>
    public static DatabaseHealthResult Unhealthy(
        string? description = null,
        Exception? exception = null,
        IReadOnlyDictionary<string, object>? data = null)
        => new(DatabaseHealthStatus.Unhealthy, description, exception, data);
}

/// <summary>
/// Represents the health status of a database connection.
/// </summary>
public enum DatabaseHealthStatus
{
    /// <summary>
    /// The database is unreachable or not functioning correctly.
    /// </summary>
    Unhealthy = 0,

    /// <summary>
    /// The database is reachable but experiencing reduced functionality or performance.
    /// </summary>
    Degraded = 1,

    /// <summary>
    /// The database connection is functioning correctly.
    /// </summary>
    Healthy = 2
}

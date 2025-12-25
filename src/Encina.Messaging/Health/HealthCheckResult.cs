using System.Collections.Immutable;

namespace Encina.Messaging.Health;

/// <summary>
/// Represents the result of a health check operation.
/// </summary>
/// <remarks>
/// <para>
/// This is a provider-agnostic health check result that can be mapped to
/// ASP.NET Core's <c>HealthCheckResult</c> or other monitoring systems.
/// </para>
/// <para>
/// The status follows standard health check semantics:
/// <list type="bullet">
/// <item><description><b>Healthy</b>: Component is functioning correctly</description></item>
/// <item><description><b>Degraded</b>: Component is working but with reduced functionality</description></item>
/// <item><description><b>Unhealthy</b>: Component is not functioning correctly</description></item>
/// </list>
/// </para>
/// </remarks>
public readonly record struct HealthCheckResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HealthCheckResult"/> struct.
    /// </summary>
    /// <param name="status">The health status.</param>
    /// <param name="description">An optional description of the health status.</param>
    /// <param name="exception">An optional exception that caused the health check to fail.</param>
    /// <param name="data">Optional additional data about the health check.</param>
    public HealthCheckResult(
        HealthStatus status,
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
    /// Gets the health status.
    /// </summary>
    public HealthStatus Status { get; }

    /// <summary>
    /// Gets an optional description of the health status.
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
    /// Can include metrics like pending message counts, processing rates, or error counts.
    /// </remarks>
    public IReadOnlyDictionary<string, object> Data { get; }

    /// <summary>
    /// Creates a healthy result.
    /// </summary>
    /// <param name="description">An optional description.</param>
    /// <param name="data">Optional additional data.</param>
    /// <returns>A healthy result.</returns>
    public static HealthCheckResult Healthy(
        string? description = null,
        IReadOnlyDictionary<string, object>? data = null)
        => new(HealthStatus.Healthy, description, null, data);

    /// <summary>
    /// Creates a degraded result.
    /// </summary>
    /// <param name="description">An optional description.</param>
    /// <param name="exception">An optional exception.</param>
    /// <param name="data">Optional additional data.</param>
    /// <returns>A degraded result.</returns>
    public static HealthCheckResult Degraded(
        string? description = null,
        Exception? exception = null,
        IReadOnlyDictionary<string, object>? data = null)
        => new(HealthStatus.Degraded, description, exception, data);

    /// <summary>
    /// Creates an unhealthy result.
    /// </summary>
    /// <param name="description">An optional description.</param>
    /// <param name="exception">An optional exception.</param>
    /// <param name="data">Optional additional data.</param>
    /// <returns>An unhealthy result.</returns>
    public static HealthCheckResult Unhealthy(
        string? description = null,
        Exception? exception = null,
        IReadOnlyDictionary<string, object>? data = null)
        => new(HealthStatus.Unhealthy, description, exception, data);
}

/// <summary>
/// Represents the health status of a component.
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// The component is unhealthy and not functioning correctly.
    /// </summary>
    Unhealthy = 0,

    /// <summary>
    /// The component is working but with reduced functionality or performance.
    /// </summary>
    Degraded = 1,

    /// <summary>
    /// The component is functioning correctly.
    /// </summary>
    Healthy = 2
}

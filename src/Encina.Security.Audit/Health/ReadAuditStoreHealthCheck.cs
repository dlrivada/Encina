using Encina.Messaging.Health;

namespace Encina.Security.Audit.Health;

/// <summary>
/// Health check that verifies read audit store accessibility.
/// </summary>
/// <remarks>
/// <para>
/// Performs a lightweight query against the read audit store to verify connectivity.
/// Returns <see cref="HealthCheckResult"/> with <see cref="HealthStatus.Healthy"/> when the store
/// responds successfully, <see cref="HealthStatus.Degraded"/> when the store returns an error,
/// and <see cref="HealthStatus.Unhealthy"/> when the store is unreachable.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// builder.Services
///     .AddHealthChecks()
///     .AddCheck&lt;ReadAuditStoreHealthCheck&gt;(ReadAuditStoreHealthCheck.DefaultName);
/// </code>
/// </example>
public sealed class ReadAuditStoreHealthCheck : EncinaHealthCheck
{
    private readonly IReadAuditStore _readAuditStore;

    /// <summary>
    /// The default name for this health check.
    /// </summary>
    public const string DefaultName = "read_audit_store";

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadAuditStoreHealthCheck"/> class.
    /// </summary>
    /// <param name="readAuditStore">The read audit store to check.</param>
    public ReadAuditStoreHealthCheck(IReadAuditStore readAuditStore)
        : base(DefaultName, ["read-audit", "security", "ready"])
    {
        ArgumentNullException.ThrowIfNull(readAuditStore);
        _readAuditStore = readAuditStore;
    }

    /// <inheritdoc />
    protected override async Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken cancellationToken)
    {
        var result = await _readAuditStore.GetAccessHistoryAsync(
            "__health_check__", "__none__", cancellationToken);

        return result.Match(
            Right: _ => HealthCheckResult.Healthy("Read audit store is accessible."),
            Left: error => HealthCheckResult.Degraded(
                $"Read audit store returned an error: {error.Message}",
                data: new Dictionary<string, object>
                {
                    ["error"] = error.Message
                }));
    }
}

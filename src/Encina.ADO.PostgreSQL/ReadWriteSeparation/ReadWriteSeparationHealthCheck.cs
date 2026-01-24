using Encina.Messaging.Health;
using Encina.Messaging.ReadWriteSeparation;
using Npgsql;

namespace Encina.ADO.PostgreSQL.ReadWriteSeparation;

/// <summary>
/// Health check for read/write separation configuration that verifies connectivity
/// to both the primary database and all configured read replicas.
/// </summary>
/// <remarks>
/// <para>
/// This health check tests actual database connectivity by attempting to open connections
/// to the primary database and each configured read replica.
/// </para>
/// <para>
/// <b>Health Status:</b>
/// <list type="bullet">
///   <item>
///     <description>
///       <see cref="HealthStatus.Healthy"/>: Primary and all replicas are reachable.
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="HealthStatus.Degraded"/>: Primary is reachable but some replicas are not.
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="HealthStatus.Unhealthy"/>: Primary is not reachable (critical failure).
///     </description>
///   </item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registration is automatic when UseReadWriteSeparation is enabled
/// services.AddEncinaADOPostgreSQL(connectionString, config =>
/// {
///     config.UseReadWriteSeparation = true;
///     config.ReadWriteSeparationOptions.WriteConnectionString = "Host=primary;...";
///     config.ReadWriteSeparationOptions.ReadConnectionStrings.Add("Host=replica;...");
/// });
///
/// // Health check is available via IEncinaHealthCheck
/// var healthChecks = serviceProvider.GetServices&lt;IEncinaHealthCheck&gt;();
/// var rwHealthCheck = healthChecks.OfType&lt;ReadWriteSeparationHealthCheck&gt;().First();
/// var result = await rwHealthCheck.CheckHealthAsync();
/// </code>
/// </example>
public sealed class ReadWriteSeparationHealthCheck : EncinaHealthCheck
{
    /// <summary>
    /// The default name for this health check.
    /// </summary>
    public const string DefaultName = "encina-read-write-separation-ado-postgresql";

    private readonly ReadWriteSeparationOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadWriteSeparationHealthCheck"/> class.
    /// </summary>
    /// <param name="options">The read/write separation configuration options.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> is <see langword="null"/>.
    /// </exception>
    public ReadWriteSeparationHealthCheck(ReadWriteSeparationOptions options)
        : base(
            DefaultName,
            new System.Collections.Generic.HashSet<string> { "encina", "database", "read-write-separation", "ado", "postgresql", "ready" })
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <inheritdoc/>
    protected override async Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken cancellationToken)
    {
        var data = new Dictionary<string, object>();

        // Check primary database connectivity
        var primaryResult = await CheckPrimaryAsync(_options.WriteConnectionString, cancellationToken)
            .ConfigureAwait(false);

        data["primary"] = primaryResult.IsHealthy ? "reachable" : $"unreachable: {primaryResult.Error}";

        // If primary is unreachable, the system is unhealthy
        if (!primaryResult.IsHealthy)
        {
            return new HealthCheckResult(
                HealthStatus.Unhealthy,
                $"Primary database is unreachable: {primaryResult.Error}",
                data: data);
        }

        // If no replicas configured, return healthy
        if (_options.ReadConnectionStrings.Count == 0)
        {
            return new HealthCheckResult(
                HealthStatus.Healthy,
                "Primary database is reachable (no replicas configured)",
                data: data);
        }

        // Check all replicas
        var failedReplicas = new List<string>();
        var replicaIndex = 0;

        foreach (var replicaConnectionString in _options.ReadConnectionStrings)
        {
            var replicaResult = await CheckReplicaAsync(replicaConnectionString, cancellationToken)
                .ConfigureAwait(false);

            var replicaKey = $"replica_{replicaIndex}";
            data[replicaKey] = replicaResult.IsHealthy ? "reachable" : $"unreachable: {replicaResult.Error}";

            if (!replicaResult.IsHealthy)
            {
                failedReplicas.Add(replicaKey);
            }

            replicaIndex++;
        }

        // Determine overall status
        if (failedReplicas.Count == 0)
        {
            return new HealthCheckResult(
                HealthStatus.Healthy,
                $"Primary and all {_options.ReadConnectionStrings.Count} replica(s) are reachable",
                data: data);
        }

        if (failedReplicas.Count == _options.ReadConnectionStrings.Count)
        {
            return new HealthCheckResult(
                HealthStatus.Degraded,
                $"Primary is reachable but all {failedReplicas.Count} replica(s) are unreachable",
                data: data);
        }

        return new HealthCheckResult(
            HealthStatus.Degraded,
            $"Primary is reachable but {failedReplicas.Count} of {_options.ReadConnectionStrings.Count} replica(s) are unreachable",
            data: data);
    }

    private static async Task<(bool IsHealthy, string? Error)> CheckPrimaryAsync(
        string? connectionString,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            return (false, "Write connection string is not configured");
        }

        return await TryConnectAsync(connectionString, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<(bool IsHealthy, string? Error)> CheckReplicaAsync(
        string connectionString,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            return (false, "Connection string is empty");
        }

        return await TryConnectAsync(connectionString, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<(bool IsHealthy, string? Error)> TryConnectAsync(
        string connectionString,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            return (true, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return (false, ex.Message);
        }
    }
}

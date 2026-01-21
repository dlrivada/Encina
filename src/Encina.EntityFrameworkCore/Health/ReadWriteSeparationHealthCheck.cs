using Encina.Messaging.Health;
using Encina.Messaging.ReadWriteSeparation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.EntityFrameworkCore.Health;

/// <summary>
/// Health check for read/write database separation that verifies connectivity
/// to both the primary database and read replicas.
/// </summary>
/// <remarks>
/// <para>
/// This health check tests the connectivity of all configured databases in the
/// read/write separation topology:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       <b>Primary (Write) Database</b>: The main database for write operations
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Read Replicas</b>: All configured read replica databases (if any)
///     </description>
///   </item>
/// </list>
/// <para>
/// <b>Health Status:</b>
/// <list type="bullet">
///   <item>
///     <description>
///       <b>Healthy</b>: Primary and all replicas are reachable
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Degraded</b>: Primary is reachable but some replicas are not (read operations
///       will be routed to available replicas or fall back to primary)
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Unhealthy</b>: Primary database is not reachable
///     </description>
///   </item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registration happens automatically when UseReadWriteSeparation is enabled
/// services.AddEncinaEntityFrameworkCore&lt;AppDbContext&gt;(config =>
/// {
///     config.UseReadWriteSeparation = true;
///     config.ReadWriteSeparationOptions.WriteConnectionString = "Server=primary;...";
///     config.ReadWriteSeparationOptions.ReadConnectionStrings.Add("Server=replica1;...");
/// });
///
/// // Health check results include connection details
/// // GET /health
/// // {
/// //   "status": "Healthy",
/// //   "data": {
/// //     "primary": "reachable",
/// //     "replicas": { "replica1": "reachable", "replica2": "reachable" }
/// //   }
/// // }
/// </code>
/// </example>
public sealed class ReadWriteSeparationHealthCheck : EncinaHealthCheck
{
    /// <summary>
    /// The default name for the read/write separation health check.
    /// </summary>
    public const string DefaultName = "encina-read-write-separation";

    /// <summary>
    /// Tags that are always included in the health check.
    /// </summary>
    private static readonly string[] DefaultTags = ["encina", "database", "read-write-separation", "ready"];

    /// <summary>
    /// Default timeout for individual connection tests.
    /// </summary>
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    private readonly IServiceProvider _serviceProvider;
    private readonly ReadWriteSeparationOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadWriteSeparationHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for creating DbContext instances.</param>
    /// <param name="options">The read/write separation options containing connection strings.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public ReadWriteSeparationHealthCheck(
        IServiceProvider serviceProvider,
        ReadWriteSeparationOptions options)
        : base(DefaultName, DefaultTags)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(options);

        _serviceProvider = serviceProvider;
        _options = options;
    }

    /// <inheritdoc/>
    protected override async Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken cancellationToken)
    {
        var data = new Dictionary<string, object>();
        var unhealthyReplicas = new List<string>();
        var healthyReplicas = new List<string>();

        // Check primary (write) database using the connection selector
        bool primaryHealthy;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var connectionSelector = scope.ServiceProvider.GetService<IReadWriteConnectionSelector>();

            if (connectionSelector is null)
            {
                return HealthCheckResult.Unhealthy(
                    "IReadWriteConnectionSelector is not registered. " +
                    "Ensure UseReadWriteSeparation is enabled in the configuration.",
                    data: data);
            }

            // Test primary database connectivity by trying to create a context
            primaryHealthy = await CheckPrimaryAsync(scope.ServiceProvider, cancellationToken);
            data["primary"] = primaryHealthy ? "reachable" : "unreachable";

            if (!primaryHealthy)
            {
                return HealthCheckResult.Unhealthy(
                    "Primary database is not reachable. Write operations will fail.",
                    data: data);
            }

            // Check if replicas are configured
            if (!connectionSelector.HasReadReplicas)
            {
                data["replicas"] = "none configured (using primary for reads)";
                return HealthCheckResult.Healthy(
                    "Primary database is reachable (no replicas configured)",
                    data: data);
            }

            // Test read replica connectivity
            var replicaResults = new Dictionary<string, string>();
            var replicaIndex = 1;

            foreach (var replicaConnectionString in _options.ReadConnectionStrings)
            {
                var replicaName = GetReplicaIdentifier(replicaConnectionString, replicaIndex);
                var replicaHealthy = await CheckReplicaAsync(
                    scope.ServiceProvider,
                    replicaConnectionString,
                    cancellationToken);

                replicaResults[replicaName] = replicaHealthy ? "reachable" : "unreachable";

                if (replicaHealthy)
                {
                    healthyReplicas.Add(replicaName);
                }
                else
                {
                    unhealthyReplicas.Add(replicaName);
                }

                replicaIndex++;
            }

            data["replicas"] = replicaResults;
            data["healthy_replica_count"] = healthyReplicas.Count;
            data["total_replica_count"] = _options.ReadConnectionStrings.Count;
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"Failed to check database connectivity: {ex.Message}",
                exception: ex,
                data: data);
        }

        // Determine overall status
        if (unhealthyReplicas.Count == 0)
        {
            return HealthCheckResult.Healthy(
                $"Primary and all {_options.ReadConnectionStrings.Count} replicas are reachable",
                data: data);
        }

        if (healthyReplicas.Count > 0)
        {
            return HealthCheckResult.Degraded(
                $"Primary is reachable but {unhealthyReplicas.Count} of {_options.ReadConnectionStrings.Count} replicas are unreachable. " +
                $"Read operations will use {healthyReplicas.Count} available replicas.",
                data: data);
        }

        return HealthCheckResult.Degraded(
            "Primary is reachable but all replicas are unreachable. " +
            "Read operations will fall back to the primary database.",
            data: data);
    }

    private static async Task<bool> CheckPrimaryAsync(
        IServiceProvider scopedProvider,
        CancellationToken cancellationToken)
    {
        try
        {
            using var timeoutCts = new CancellationTokenSource(DefaultTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutCts.Token);

            // Use DbContext to verify connectivity
            var dbContext = scopedProvider.GetRequiredService<DbContext>();
            return await dbContext.Database.CanConnectAsync(linkedCts.Token);
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> CheckReplicaAsync(
        IServiceProvider scopedProvider,
        string connectionString,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        try
        {
            using var timeoutCts = new CancellationTokenSource(DefaultTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutCts.Token);

            // Get the base DbContext options to determine the provider
            var dbContext = scopedProvider.GetRequiredService<DbContext>();
            var database = dbContext.Database;

            // Get the relational connection and test with the replica connection string
            var relationalConnection = database.GetDbConnection();

            // Create a new connection with the replica connection string
            // We need to use the same provider type
            var connectionType = relationalConnection.GetType();
            var replicaConnection = (System.Data.Common.DbConnection)Activator.CreateInstance(connectionType)!;
            replicaConnection.ConnectionString = connectionString;

            try
            {
                await replicaConnection.OpenAsync(linkedCts.Token);
                return true;
            }
            finally
            {
                await replicaConnection.DisposeAsync();
            }
        }
        catch
        {
            return false;
        }
    }

    private static string GetReplicaIdentifier(string connectionString, int index)
    {
        // Try to extract a meaningful identifier from the connection string
        // This works for most ADO.NET providers that use Server= or Data Source= syntax
        try
        {
            var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var keyValue = part.Split('=', 2);
                if (keyValue.Length == 2)
                {
                    var key = keyValue[0].Trim().ToUpperInvariant();
                    if (key is "SERVER" or "DATA SOURCE" or "HOST")
                    {
                        var value = keyValue[1].Trim();
                        // Remove port if present
                        var serverName = value.Split(',')[0].Split(':')[0].Split('\\')[0];
                        if (!string.IsNullOrWhiteSpace(serverName))
                        {
                            return $"replica_{serverName}";
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return $"replica_{index}";
    }
}

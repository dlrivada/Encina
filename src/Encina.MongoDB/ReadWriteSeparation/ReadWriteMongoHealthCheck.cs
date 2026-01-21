using Encina.Messaging.Health;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Servers;

namespace Encina.MongoDB.ReadWriteSeparation;

/// <summary>
/// Health check for MongoDB read/write separation that verifies replica set topology
/// and availability of primary and secondary members.
/// </summary>
/// <remarks>
/// <para>
/// This health check inspects the MongoDB cluster topology to verify:
/// </para>
/// <list type="bullet">
///   <item><description>Primary member is available and reachable</description></item>
///   <item><description>At least one secondary member is available for read operations</description></item>
///   <item><description>The deployment is a replica set (required for read preferences)</description></item>
/// </list>
/// <para>
/// <b>Health Status:</b>
/// <list type="bullet">
///   <item>
///     <description>
///       <see cref="HealthStatus.Healthy"/>: Primary and at least one secondary are available.
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="HealthStatus.Degraded"/>: Primary is available but no secondaries are available.
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="HealthStatus.Unhealthy"/>: Primary is not available or not a replica set.
///     </description>
///   </item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registration is automatic when UseReadWriteSeparation is enabled
/// services.AddEncinaMongoDB(options =>
/// {
///     options.ConnectionString = "mongodb://localhost:27017/?replicaSet=rs0";
///     options.DatabaseName = "MyApp";
///     options.UseReadWriteSeparation = true;
/// });
///
/// // Health check is available via IEncinaHealthCheck
/// var healthChecks = serviceProvider.GetServices&lt;IEncinaHealthCheck&gt;();
/// var rwHealthCheck = healthChecks.OfType&lt;ReadWriteMongoHealthCheck&gt;().First();
/// var result = await rwHealthCheck.CheckHealthAsync();
/// </code>
/// </example>
public sealed class ReadWriteMongoHealthCheck : EncinaHealthCheck
{
    /// <summary>
    /// The default name for this health check.
    /// </summary>
    public const string DefaultName = "encina-read-write-separation-mongodb";

    private readonly IMongoClient _mongoClient;
    private readonly MongoReadWriteSeparationOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadWriteMongoHealthCheck"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="mongoOptions">The MongoDB configuration options.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="mongoClient"/> or <paramref name="mongoOptions"/> is <see langword="null"/>.
    /// </exception>
    public ReadWriteMongoHealthCheck(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> mongoOptions)
        : base(
            DefaultName,
            new HashSet<string> { "encina", "database", "read-write-separation", "mongodb", "ready" })
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(mongoOptions);

        _mongoClient = mongoClient;
        _options = mongoOptions.Value.ReadWriteSeparationOptions;
    }

    /// <inheritdoc/>
    protected override Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken cancellationToken)
    {
        var data = new Dictionary<string, object>();

        try
        {
            // Get the cluster description from the MongoDB client
            var clusterDescription = _mongoClient.Cluster.Description;

            // Check if we have any servers
            if (clusterDescription.Servers.Count == 0)
            {
                data["cluster_type"] = "unknown";
                data["servers"] = 0;

                return Task.FromResult(new HealthCheckResult(
                    HealthStatus.Unhealthy,
                    "MongoDB cluster has no servers available",
                    data: data));
            }

            // Determine cluster type
            var clusterType = clusterDescription.Type.ToString();
            data["cluster_type"] = clusterType;

            // Check if it's a replica set
            var isReplicaSet = clusterDescription.Type == ClusterType.ReplicaSet;

            if (!isReplicaSet)
            {
                // For standalone or sharded clusters, read preferences are limited
                var serverCount = clusterDescription.Servers.Count;
                data["servers"] = serverCount;

                if (clusterDescription.Type == ClusterType.Standalone)
                {
                    // Standalone servers only support Primary read preference
                    return Task.FromResult(new HealthCheckResult(
                        HealthStatus.Degraded,
                        "MongoDB is a standalone server. Read/write separation requires a replica set for full functionality. Only Primary read preference is available.",
                        data: data));
                }

                // For sharded clusters, some read preferences work
                return Task.FromResult(new HealthCheckResult(
                    HealthStatus.Healthy,
                    $"MongoDB sharded cluster with {serverCount} mongos server(s). Read preferences will be applied to shard replica sets.",
                    data: data));
            }

            // It's a replica set - check for primary and secondaries
            var primaryCount = 0;
            var secondaryCount = 0;
            var arbiterCount = 0;
            var otherCount = 0;

            foreach (var server in clusterDescription.Servers)
            {
                switch (server.Type)
                {
                    case ServerType.ReplicaSetPrimary:
                        primaryCount++;
                        break;
                    case ServerType.ReplicaSetSecondary:
                        secondaryCount++;
                        break;
                    case ServerType.ReplicaSetArbiter:
                        arbiterCount++;
                        break;
                    default:
                        otherCount++;
                        break;
                }
            }

            data["primary"] = primaryCount > 0 ? "available" : "unavailable";
            data["secondaries"] = secondaryCount;
            data["arbiters"] = arbiterCount;
            data["configured_read_preference"] = _options.ReadPreference.ToString();

            // Determine health status
            if (primaryCount == 0)
            {
                return Task.FromResult(new HealthCheckResult(
                    HealthStatus.Unhealthy,
                    "MongoDB replica set has no primary available",
                    data: data));
            }

            if (secondaryCount == 0)
            {
                return Task.FromResult(new HealthCheckResult(
                    HealthStatus.Degraded,
                    "MongoDB replica set has no secondaries available. All reads will use primary.",
                    data: data));
            }

            return Task.FromResult(new HealthCheckResult(
                HealthStatus.Healthy,
                $"MongoDB replica set healthy: 1 primary, {secondaryCount} secondary(ies) available",
                data: data));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            data["error"] = ex.Message;

            return Task.FromResult(new HealthCheckResult(
                HealthStatus.Unhealthy,
                $"Failed to check MongoDB cluster health: {ex.Message}",
                data: data));
        }
    }
}

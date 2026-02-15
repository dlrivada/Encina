using Encina.Sharding.ReferenceTables;
using Encina.Sharding.ReferenceTables.Health;

namespace Encina.Messaging.Health;

/// <summary>
/// Health check for reference table replication staleness.
/// </summary>
/// <remarks>
/// <para>
/// This health check queries <see cref="IReferenceTableStateStore"/> for the last replication
/// time of each registered reference table and compares it against configurable thresholds.
/// Tables that have never been replicated are treated as unhealthy.
/// </para>
/// <para>
/// The check reports:
/// <list type="bullet">
///   <item><description><b>Healthy</b>: All tables replicated within the degraded threshold.</description></item>
///   <item><description><b>Degraded</b>: Some tables are stale but within the unhealthy threshold.</description></item>
///   <item><description><b>Unhealthy</b>: Some tables exceed the unhealthy threshold or were never replicated.</description></item>
/// </list>
/// </para>
/// </remarks>
public class ReferenceTableHealthCheck : EncinaHealthCheck
{
    /// <summary>
    /// The default name for this health check.
    /// </summary>
    public const string DefaultName = "encina-reference-table-replication";

    /// <summary>
    /// The default tags for this health check.
    /// </summary>
    public static readonly IReadOnlyCollection<string> DefaultTags =
        ["ready", "database", "sharding", "replication"];

    private readonly IReferenceTableRegistry _registry;
    private readonly IReferenceTableStateStore _stateStore;
    private readonly ReferenceTableHealthCheckOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReferenceTableHealthCheck"/> class.
    /// </summary>
    /// <param name="registry">The reference table registry to query for registered tables.</param>
    /// <param name="stateStore">The state store to query for last replication times.</param>
    /// <param name="options">Health check options for configuring thresholds.</param>
    public ReferenceTableHealthCheck(
        IReferenceTableRegistry registry,
        IReferenceTableStateStore stateStore,
        ReferenceTableHealthCheckOptions? options = null)
        : base(DefaultName, DefaultTags)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(stateStore);

        _registry = registry;
        _stateStore = stateStore;
        _options = options ?? new ReferenceTableHealthCheckOptions();
    }

    /// <inheritdoc />
    protected override async Task<HealthCheckResult> CheckHealthCoreAsync(
        CancellationToken cancellationToken)
    {
        var configurations = _registry.GetAllConfigurations();

        if (configurations.Count == 0)
        {
            return HealthCheckResult.Healthy(
                "No reference tables registered",
                new Dictionary<string, object> { ["registered_tables"] = 0 });
        }

        var now = DateTime.UtcNow;
        var staleTables = new List<string>();
        var unhealthyTables = new List<string>();

        foreach (var config in configurations)
        {
            var lastReplication = await _stateStore
                .GetLastReplicationTimeAsync(config.EntityType, cancellationToken)
                .ConfigureAwait(false);

            if (lastReplication is null)
            {
                // Never replicated — treat as unhealthy
                unhealthyTables.Add(config.EntityType.Name);
                continue;
            }

            var staleness = now - lastReplication.Value;

            if (staleness > _options.UnhealthyThreshold)
            {
                unhealthyTables.Add(config.EntityType.Name);
            }
            else if (staleness > _options.DegradedThreshold)
            {
                staleTables.Add(config.EntityType.Name);
            }
        }

        var data = new Dictionary<string, object>
        {
            ["registered_tables"] = configurations.Count,
            ["tables_with_stale_data"] = staleTables.Count + unhealthyTables.Count,
            ["unhealthy_threshold_minutes"] = _options.UnhealthyThreshold.TotalMinutes,
            ["degraded_threshold_minutes"] = _options.DegradedThreshold.TotalMinutes
        };

        if (unhealthyTables.Count > 0)
        {
            data["unhealthy_tables"] = string.Join(", ", unhealthyTables);

            return HealthCheckResult.Unhealthy(
                $"{unhealthyTables.Count} reference table(s) exceeded unhealthy threshold " +
                $"({_options.UnhealthyThreshold.TotalMinutes}min): {string.Join(", ", unhealthyTables)}",
                data: data);
        }

        if (staleTables.Count > 0)
        {
            data["degraded_tables"] = string.Join(", ", staleTables);

            return HealthCheckResult.Degraded(
                $"{staleTables.Count} reference table(s) exceeded degraded threshold " +
                $"({_options.DegradedThreshold.TotalMinutes}min): {string.Join(", ", staleTables)}",
                data: data);
        }

        return HealthCheckResult.Healthy(
            $"All {configurations.Count} reference table(s) are within replication thresholds",
            data);
    }
}

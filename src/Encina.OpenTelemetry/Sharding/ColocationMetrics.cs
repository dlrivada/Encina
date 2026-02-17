using System.Diagnostics;
using System.Diagnostics.Metrics;
using Encina.Sharding.Colocation;

namespace Encina.OpenTelemetry.Sharding;

/// <summary>
/// Exposes co-location group metrics via the <c>Encina</c> meter.
/// </summary>
/// <remarks>
/// <para>
/// The following instruments are registered:
/// <list type="bullet">
///   <item><description><c>encina.sharding.colocation.groups_registered</c> (ObservableGauge) —
///   Current number of co-location groups registered in the topology.</description></item>
///   <item><description><c>encina.sharding.colocation.validation_failures_total</c> (Counter) —
///   Number of co-location validation failures, tagged with <c>root_entity</c> and
///   <c>failed_entity</c>.</description></item>
///   <item><description><c>encina.sharding.colocation.local_joins_total</c> (Counter) —
///   Number of routing decisions that leveraged co-location for local joins,
///   tagged with <c>group</c>.</description></item>
/// </list>
/// </para>
/// <para>
/// All instruments use the same <c>"Encina"</c> meter for consistency with
/// <c>ShardRoutingMetrics</c> and <c>DatabasePoolMetrics</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registered automatically via AddEncinaOpenTelemetry()
/// services.AddEncinaOpenTelemetry();
///
/// // Or manually:
/// var metrics = new ColocationMetrics(colocationRegistry);
/// metrics.RecordValidationFailure("Order", "OrderItem");
/// metrics.RecordLocalJoin("Order");
/// </code>
/// </example>
public sealed class ColocationMetrics
{
    private static readonly Meter Meter = new("Encina", "1.0");

    private readonly Counter<long> _validationFailures;
    private readonly Counter<long> _localJoins;

    /// <summary>
    /// Initializes a new instance of the <see cref="ColocationMetrics"/> class,
    /// registering all co-location metric instruments.
    /// </summary>
    /// <param name="registry">The co-location group registry for observable gauge callbacks.</param>
    public ColocationMetrics(ColocationGroupRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        Meter.CreateObservableGauge(
            "encina.sharding.colocation.groups_registered",
            () => new Measurement<int>(registry.GetAllGroups().Count),
            unit: "{groups}",
            description: "Current number of co-location groups registered in the topology.");

        _validationFailures = Meter.CreateCounter<long>(
            "encina.sharding.colocation.validation_failures_total",
            description: "Number of co-location validation failures.");

        _localJoins = Meter.CreateCounter<long>(
            "encina.sharding.colocation.local_joins_total",
            description: "Number of routing decisions that leveraged co-location for local joins.");
    }

    /// <summary>
    /// Records a co-location validation failure.
    /// </summary>
    /// <param name="rootEntityName">The root entity type name of the co-location group.</param>
    /// <param name="failedEntityName">The entity type name that failed validation.</param>
    public void RecordValidationFailure(string rootEntityName, string failedEntityName)
    {
        var tags = new TagList
        {
            { ActivityTagNames.Colocation.RootEntity, rootEntityName },
            { "failed_entity", failedEntityName }
        };

        _validationFailures.Add(1, tags);
    }

    /// <summary>
    /// Records a routing decision that leveraged co-location for a local join.
    /// </summary>
    /// <param name="groupName">The co-location group name (typically the root entity type name).</param>
    public void RecordLocalJoin(string groupName)
    {
        _localJoins.Add(1,
            new KeyValuePair<string, object?>(ActivityTagNames.Colocation.Group, groupName));
    }
}

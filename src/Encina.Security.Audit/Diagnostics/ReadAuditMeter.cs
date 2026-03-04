using System.Diagnostics.Metrics;

namespace Encina.Security.Audit.Diagnostics;

/// <summary>
/// Provides the meter and instruments for read audit metrics.
/// </summary>
/// <remarks>
/// <para>
/// Uses a dedicated <see cref="Meter"/> (<c>Encina.ReadAudit</c>) for metric aggregation.
/// Counters use tag-based dimensions (<c>entity_type</c>, <c>access_method</c>,
/// <c>query_type</c>) to enable flexible dashboard filtering without creating
/// separate counters per combination.
/// </para>
/// <para>
/// Key metrics:
/// <list type="bullet">
/// <item><description><c>read_audit.entries_logged.total</c> — tracks audit coverage</description></item>
/// <item><description><c>read_audit.log_failures.total</c> — monitors audit reliability</description></item>
/// <item><description><c>read_audit.queries.total</c> — tracks query workload</description></item>
/// <item><description><c>read_audit.entries_purged.total</c> — monitors retention enforcement</description></item>
/// <item><description><c>read_audit.purge.duration.ms</c> — monitors purge performance</description></item>
/// </list>
/// </para>
/// </remarks>
internal static class ReadAuditMeter
{
    /// <summary>
    /// The meter name for external registration with OpenTelemetry.
    /// </summary>
    internal const string MeterName = "Encina.ReadAudit";

    /// <summary>
    /// The meter version.
    /// </summary>
    internal const string MeterVersion = "1.0";

    internal static readonly Meter Meter = new(MeterName, MeterVersion);

    // ---- Tag constants ----

    /// <summary>The type of entity being audited.</summary>
    internal const string TagEntityType = "entity_type";

    /// <summary>The access method used (Repository, DirectQuery, Api, Export, Custom).</summary>
    internal const string TagAccessMethod = "access_method";

    /// <summary>The type of query operation (access_history, user_access_history, paginated_query).</summary>
    internal const string TagQueryType = "query_type";

    // ---- Counters ----

    /// <summary>
    /// Total number of read audit entries logged, tagged with <c>entity_type</c> and <c>access_method</c>.
    /// </summary>
    internal static readonly Counter<long> EntriesLoggedTotal =
        Meter.CreateCounter<long>("read_audit.entries_logged.total",
            description: "Total number of read audit entries logged.");

    /// <summary>
    /// Total number of failed read audit log attempts, tagged with <c>entity_type</c>.
    /// </summary>
    internal static readonly Counter<long> LogFailuresTotal =
        Meter.CreateCounter<long>("read_audit.log_failures.total",
            description: "Total number of failed read audit log attempts.");

    /// <summary>
    /// Total number of read audit queries executed, tagged with <c>query_type</c>.
    /// </summary>
    internal static readonly Counter<long> QueriesTotal =
        Meter.CreateCounter<long>("read_audit.queries.total",
            description: "Total number of read audit queries executed.");

    /// <summary>
    /// Total number of read audit entries purged by the retention service.
    /// </summary>
    internal static readonly Counter<long> EntriesPurgedTotal =
        Meter.CreateCounter<long>("read_audit.entries_purged.total",
            description: "Total number of read audit entries purged.");

    // ---- Histograms ----

    /// <summary>
    /// Duration of read audit log operations in milliseconds.
    /// </summary>
    internal static readonly Histogram<double> LogDuration =
        Meter.CreateHistogram<double>("read_audit.log.duration.ms",
            unit: "ms",
            description: "Duration of read audit log operations in milliseconds.");

    /// <summary>
    /// Duration of read audit query operations in milliseconds.
    /// </summary>
    internal static readonly Histogram<double> QueryDuration =
        Meter.CreateHistogram<double>("read_audit.query.duration.ms",
            unit: "ms",
            description: "Duration of read audit query operations in milliseconds.");

    /// <summary>
    /// Duration of read audit purge operations in milliseconds.
    /// </summary>
    internal static readonly Histogram<double> PurgeDuration =
        Meter.CreateHistogram<double>("read_audit.purge.duration.ms",
            unit: "ms",
            description: "Duration of read audit purge operations in milliseconds.");
}

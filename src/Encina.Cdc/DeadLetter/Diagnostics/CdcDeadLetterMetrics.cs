using System.Diagnostics.Metrics;

namespace Encina.Cdc.DeadLetter.Diagnostics;

/// <summary>
/// Exposes CDC dead letter queue metrics via the <c>Encina.Cdc.DeadLetter</c> meter.
/// </summary>
/// <remarks>
/// <para>
/// The following instruments are registered:
/// <list type="bullet">
/// <item><description><c>cdc.dead_letter.added</c> — Total events moved to the dead letter queue</description></item>
/// <item><description><c>cdc.dead_letter.resolved</c> — Total dead letter entries resolved</description></item>
/// <item><description><c>cdc.dead_letter.pending</c> — Current count of pending dead letter entries</description></item>
/// </list>
/// </para>
/// <para>
/// All instruments include dimension tags such as <c>connector_id</c>,
/// <c>table_name</c>, and <c>resolution_type</c> where applicable.
/// </para>
/// </remarks>
internal sealed class CdcDeadLetterMetrics
{
    private static readonly Meter Meter = new("Encina.Cdc.DeadLetter", "1.0");

    private static readonly Counter<long> AddedCounter = Meter.CreateCounter<long>(
        "cdc.dead_letter.added",
        unit: "{entry}",
        description: "Total number of CDC events moved to the dead letter queue.");

    private static readonly Counter<long> ResolvedCounter = Meter.CreateCounter<long>(
        "cdc.dead_letter.resolved",
        unit: "{entry}",
        description: "Total number of CDC dead letter entries resolved.");

    private static long s_pendingCount;

    /// <summary>
    /// Gets or sets the observable gauge registration callback.
    /// The gauge is registered lazily on first use.
    /// </summary>
    private static readonly ObservableGauge<long> PendingGauge = Meter.CreateObservableGauge(
        "cdc.dead_letter.pending",
        observeValue: () => Interlocked.Read(ref s_pendingCount),
        unit: "{entry}",
        description: "Current number of pending CDC dead letter entries.");

    /// <summary>
    /// Records that an event has been added to the dead letter queue.
    /// </summary>
    /// <param name="connectorId">The CDC connector that produced the event.</param>
    /// <param name="tableName">The table name associated with the failed event.</param>
    internal static void RecordAdded(string connectorId, string tableName)
    {
        AddedCounter.Add(1,
            new KeyValuePair<string, object?>("connector_id", connectorId),
            new KeyValuePair<string, object?>("table_name", tableName));

        Interlocked.Increment(ref s_pendingCount);
    }

    /// <summary>
    /// Records that a dead letter entry has been resolved.
    /// </summary>
    /// <param name="connectorId">The CDC connector that produced the original event.</param>
    /// <param name="resolutionType">The resolution type applied (e.g., "Replay", "Discard").</param>
    internal static void RecordResolved(string connectorId, string resolutionType)
    {
        ResolvedCounter.Add(1,
            new KeyValuePair<string, object?>("connector_id", connectorId),
            new KeyValuePair<string, object?>("resolution_type", resolutionType));

        Interlocked.Decrement(ref s_pendingCount);
    }

    /// <summary>
    /// Updates the pending count directly, typically called when the store is queried.
    /// </summary>
    /// <param name="count">The current number of pending entries.</param>
    internal static void UpdatePendingCount(long count)
    {
        Interlocked.Exchange(ref s_pendingCount, count);
    }
}

using System.Diagnostics.Metrics;

namespace Encina.Messaging.Diagnostics;

/// <summary>
/// Counters of the dead letter queue on the <c>Encina</c> meter.
/// </summary>
/// <remarks>
/// Dimensions are bounded and carry no personal data: the source pattern, an outcome, a reason, an
/// operation name and an error code. The tenant id, the request payload and the text of an error or an
/// exception are never a dimension (SPEC-002 REQ-062).
/// </remarks>
internal static class DeadLetterMetrics
{
    private static readonly Meter Meter = new("Encina", "1.0");

    private static readonly Counter<long> MessagesAdded = Meter.CreateCounter<long>(
        "encina.dlq.messages_added_total",
        unit: "{messages}",
        description: "Total number of messages added to the dead letter queue.");

    private static readonly Counter<long> DuplicatesIgnored = Meter.CreateCounter<long>(
        "encina.dlq.duplicates_ignored_total",
        unit: "{messages}",
        description: "Total number of captures ignored because the source message was already dead-lettered.");

    private static readonly Counter<long> MessagesReplayed = Meter.CreateCounter<long>(
        "encina.dlq.messages_replayed_total",
        unit: "{messages}",
        description: "Total number of dead letter replays, by outcome.");

    private static readonly Counter<long> MessagesDeleted = Meter.CreateCounter<long>(
        "encina.dlq.messages_deleted_total",
        unit: "{messages}",
        description: "Total number of dead letter messages deleted, by reason.");

    private static readonly Counter<long> StoreFailures = Meter.CreateCounter<long>(
        "encina.dlq.store_failures_total",
        unit: "{failures}",
        description: "Total number of dead letter store operations that failed, by operation and error code.");

    /// <summary>Outcome tag value of a replay that succeeded.</summary>
    internal const string OutcomeSucceeded = "succeeded";

    /// <summary>Outcome tag value of a replay that failed.</summary>
    internal const string OutcomeFailed = "failed";

    /// <summary>Reason tag value of a deletion requested by an operator.</summary>
    internal const string ReasonManual = "manual";

    /// <summary>Reason tag value of a deletion of expired messages.</summary>
    internal const string ReasonExpired = "expired";

    internal static void RecordAdded(string sourcePattern)
        => MessagesAdded.Add(1, new KeyValuePair<string, object?>("source_pattern", sourcePattern));

    internal static void RecordDuplicateIgnored(string sourcePattern)
        => DuplicatesIgnored.Add(1, new KeyValuePair<string, object?>("source_pattern", sourcePattern));

    internal static void RecordReplayed(bool succeeded)
        => MessagesReplayed.Add(1, new KeyValuePair<string, object?>("outcome", succeeded ? OutcomeSucceeded : OutcomeFailed));

    internal static void RecordDeleted(int count, string reason)
    {
        if (count > 0)
        {
            MessagesDeleted.Add(count, new KeyValuePair<string, object?>("reason", reason));
        }
    }

    internal static void RecordStoreFailure(string operation, string errorCode)
        => StoreFailures.Add(
            1,
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("error_code", errorCode));
}

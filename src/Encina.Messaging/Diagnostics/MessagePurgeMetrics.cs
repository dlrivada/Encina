using System.Diagnostics.Metrics;

namespace Encina.Messaging.Diagnostics;

/// <summary>
/// Counters of the message purge service on the <c>Encina</c> meter.
/// </summary>
/// <remarks>
/// Dimensions are bounded and carry no personal data: the messaging pattern and an error code or an
/// exception type. The text of an error or an exception is never a dimension.
/// </remarks>
internal static class MessagePurgeMetrics
{
    private static readonly Meter Meter = new("Encina", "1.0");

    private static readonly Counter<long> MessagesDeleted = Meter.CreateCounter<long>(
        "encina.messaging.purge.messages_deleted_total",
        unit: "{messages}",
        description: "Total number of expired messages removed by the purge service, by pattern.");

    private static readonly Counter<long> CycleFailures = Meter.CreateCounter<long>(
        "encina.messaging.purge.cycle_failures_total",
        unit: "{failures}",
        description: "Total number of purge cycles that failed, by pattern and error code or exception type.");

    internal static void RecordDeleted(string pattern, int count)
    {
        if (count > 0)
        {
            MessagesDeleted.Add(count, new KeyValuePair<string, object?>("pattern", pattern));
        }
    }

    internal static void RecordCycleFailure(string pattern, string errorCode)
        => CycleFailures.Add(
            1,
            new KeyValuePair<string, object?>("pattern", pattern),
            new KeyValuePair<string, object?>("error_code", errorCode));
}

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.Messaging.Diagnostics;

/// <summary>
/// Exposes metrics for the <c>ScheduledMessageProcessor</c> background service via the
/// shared <c>"Encina"</c> meter.
/// </summary>
/// <remarks>
/// <para>
/// This class registers processor-specific instruments that complement the store-level
/// scheduling metrics already provided by <see cref="MessagingStoreMetrics"/>. The
/// store metrics track scheduling and execution at the persistence level; these metrics
/// track the processor's polling cycles and dispatch outcomes.
/// </para>
/// <para>
/// A static <c>Meter("Encina", "1.0")</c> is created with the same name and version
/// as other Encina metric classes (e.g., <see cref="MessagingStoreMetrics"/>).
/// OpenTelemetry aggregates instruments by meter name, so all <c>"Encina"</c> meters
/// appear under a single logical meter in exporters.
/// </para>
/// </remarks>
internal sealed class SchedulingProcessorMetrics
{
    private static readonly Meter Meter = new("Encina", "1.0");

    private readonly Counter<long> _messagesProcessed;
    private readonly Histogram<double> _cycleDuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchedulingProcessorMetrics"/> class,
    /// registering processor-specific metric instruments.
    /// </summary>
    internal SchedulingProcessorMetrics()
    {
        _messagesProcessed = Meter.CreateCounter<long>(
            "encina.scheduling.processor.messages_total",
            unit: "{messages}",
            description: "Total number of scheduled messages processed by the ScheduledMessageProcessor.");

        _cycleDuration = Meter.CreateHistogram<double>(
            "encina.scheduling.processor.cycle_duration_seconds",
            unit: "s",
            description: "Duration of a single ScheduledMessageProcessor processing cycle in seconds.");
    }

    /// <summary>
    /// Records messages dispatched in a single cycle.
    /// </summary>
    /// <param name="successCount">Number of messages successfully dispatched.</param>
    /// <param name="failureCount">Number of messages that failed dispatch (routed to retry policy).</param>
    internal void RecordBatch(int successCount, int failureCount)
    {
        if (successCount > 0)
        {
            _messagesProcessed.Add(successCount,
                new KeyValuePair<string, object?>("outcome", "success"));
        }

        if (failureCount > 0)
        {
            _messagesProcessed.Add(failureCount,
                new KeyValuePair<string, object?>("outcome", "failure"));
        }
    }

    /// <summary>
    /// Records the duration of a processing cycle.
    /// </summary>
    /// <param name="elapsed">The elapsed time of the cycle.</param>
    internal void RecordCycleDuration(TimeSpan elapsed)
    {
        _cycleDuration.Record(elapsed.TotalSeconds);
    }
}

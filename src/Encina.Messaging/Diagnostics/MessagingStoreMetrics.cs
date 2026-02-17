using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.Messaging.Diagnostics;

/// <summary>
/// Exposes messaging store metrics (Outbox, Inbox, Saga, Scheduling) via the <c>Encina</c> meter.
/// </summary>
/// <remarks>
/// <para>
/// This class consolidates metrics for all four messaging stores into a single class
/// since they share the same meter and follow similar patterns.
/// </para>
/// </remarks>
public sealed class MessagingStoreMetrics
{
    private static readonly Meter Meter = new("Encina", "1.0");

    // Outbox
    private readonly Counter<long> _outboxAddedTotal;
    private readonly Counter<long> _outboxProcessedTotal;
    private readonly Histogram<double> _outboxProcessingDuration;

    // Inbox
    private readonly Counter<long> _inboxReceivedTotal;
    private readonly Counter<long> _inboxDuplicatesTotal;

    // Saga
    private readonly Counter<long> _sagaTransitionsTotal;

    // Scheduling
    private readonly Counter<long> _schedulingScheduledTotal;
    private readonly Counter<long> _schedulingExecutedTotal;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagingStoreMetrics"/> class,
    /// registering all messaging store metric instruments.
    /// </summary>
    /// <param name="callbacks">Optional callbacks for observable gauge instruments.</param>
    public MessagingStoreMetrics(MessagingStoreMetricsCallbacks? callbacks = null)
    {
        // Outbox instruments
        _outboxAddedTotal = Meter.CreateCounter<long>(
            "encina.outbox.messages_added_total",
            unit: "{messages}",
            description: "Total number of messages added to the outbox.");

        _outboxProcessedTotal = Meter.CreateCounter<long>(
            "encina.outbox.messages_processed_total",
            unit: "{messages}",
            description: "Total number of outbox messages processed.");

        _outboxProcessingDuration = Meter.CreateHistogram<double>(
            "encina.outbox.processing_duration_ms",
            unit: "ms",
            description: "Duration of outbox message processing in milliseconds.");

        // Inbox instruments
        _inboxReceivedTotal = Meter.CreateCounter<long>(
            "encina.inbox.messages_received_total",
            unit: "{messages}",
            description: "Total number of messages received in the inbox.");

        _inboxDuplicatesTotal = Meter.CreateCounter<long>(
            "encina.inbox.duplicates_detected_total",
            unit: "{duplicates}",
            description: "Total number of duplicate inbox messages detected.");

        // Saga instruments
        _sagaTransitionsTotal = Meter.CreateCounter<long>(
            "encina.saga.transitions_total",
            unit: "{transitions}",
            description: "Total number of saga state transitions.");

        // Scheduling instruments
        _schedulingScheduledTotal = Meter.CreateCounter<long>(
            "encina.scheduling.messages_scheduled_total",
            unit: "{messages}",
            description: "Total number of messages scheduled.");

        _schedulingExecutedTotal = Meter.CreateCounter<long>(
            "encina.scheduling.messages_executed_total",
            unit: "{messages}",
            description: "Total number of scheduled messages executed.");

        // Observable gauges
        if (callbacks is not null)
        {
            Meter.CreateObservableGauge(
                "encina.outbox.pending_count",
                callbacks.GetOutboxPendingCount,
                unit: "{messages}",
                description: "Current number of pending outbox messages.");

            Meter.CreateObservableGauge(
                "encina.saga.active_count",
                callbacks.GetActiveSagaCount,
                unit: "{sagas}",
                description: "Current number of active sagas.");
        }
    }

    // --- Outbox ---

    /// <summary>Records an outbox message addition.</summary>
    /// <param name="messageType">The message type name.</param>
    public void RecordOutboxAdded(string messageType)
    {
        _outboxAddedTotal.Add(1,
            new KeyValuePair<string, object?>("message_type", messageType));
    }

    /// <summary>Records an outbox message processing completion.</summary>
    /// <param name="outcome">The processing outcome (success, failed).</param>
    /// <param name="durationMs">The processing duration in milliseconds.</param>
    public void RecordOutboxProcessed(string outcome, double durationMs)
    {
        _outboxProcessedTotal.Add(1,
            new KeyValuePair<string, object?>("outcome", outcome));
        _outboxProcessingDuration.Record(durationMs,
            new KeyValuePair<string, object?>("outcome", outcome));
    }

    // --- Inbox ---

    /// <summary>Records an inbox message reception.</summary>
    /// <param name="messageType">The message type name.</param>
    public void RecordInboxReceived(string messageType)
    {
        _inboxReceivedTotal.Add(1,
            new KeyValuePair<string, object?>("message_type", messageType));
    }

    /// <summary>Records a duplicate inbox message detection.</summary>
    public void RecordInboxDuplicate()
    {
        _inboxDuplicatesTotal.Add(1);
    }

    // --- Saga ---

    /// <summary>Records a saga state transition.</summary>
    /// <param name="sagaType">The saga type name.</param>
    /// <param name="fromStep">The step transitioned from.</param>
    /// <param name="toStep">The step transitioned to.</param>
    public void RecordSagaTransition(string sagaType, string fromStep, string toStep)
    {
        _sagaTransitionsTotal.Add(1, new TagList
        {
            { "saga_type", sagaType },
            { "from_step", fromStep },
            { "to_step", toStep }
        });
    }

    // --- Scheduling ---

    /// <summary>Records a message being scheduled.</summary>
    /// <param name="messageType">The message type name.</param>
    public void RecordScheduled(string messageType)
    {
        _schedulingScheduledTotal.Add(1,
            new KeyValuePair<string, object?>("message_type", messageType));
    }

    /// <summary>Records a scheduled message execution.</summary>
    /// <param name="outcome">The execution outcome (success, failed).</param>
    public void RecordExecuted(string outcome)
    {
        _schedulingExecutedTotal.Add(1,
            new KeyValuePair<string, object?>("outcome", outcome));
    }
}

/// <summary>
/// Provides callback functions for observable messaging store metrics gauges.
/// </summary>
public sealed class MessagingStoreMetricsCallbacks
{
    private readonly Func<long> _getOutboxPendingCount;
    private readonly Func<long> _getActiveSagaCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagingStoreMetricsCallbacks"/> class.
    /// </summary>
    /// <param name="getOutboxPendingCount">Function that returns the current pending outbox message count.</param>
    /// <param name="getActiveSagaCount">Function that returns the current active saga count.</param>
    public MessagingStoreMetricsCallbacks(
        Func<long> getOutboxPendingCount,
        Func<long> getActiveSagaCount)
    {
        _getOutboxPendingCount = getOutboxPendingCount ?? throw new ArgumentNullException(nameof(getOutboxPendingCount));
        _getActiveSagaCount = getActiveSagaCount ?? throw new ArgumentNullException(nameof(getActiveSagaCount));
    }

    /// <summary>Gets the current number of pending outbox messages.</summary>
    public long GetOutboxPendingCount() => _getOutboxPendingCount();

    /// <summary>Gets the current number of active sagas.</summary>
    public long GetActiveSagaCount() => _getActiveSagaCount();
}

using System.Diagnostics.Metrics;

namespace Encina.Messaging.Diagnostics;

/// <summary>
/// Exposes the delivery outcomes of the outbox processors via the shared <c>"Encina"</c> meter.
/// </summary>
/// <remarks>
/// <para>
/// The counter <c>encina.outbox.processor.messages_total</c> is tagged with <c>outcome</c>:
/// <c>success</c> (delivered and marked processed), <c>failure</c> (a retry was scheduled),
/// <c>exhausted</c> (the failure used up <c>OutboxOptions.MaxRetries</c>, so the message
/// will not be fetched again), <c>store_error</c> (the store failed to record the message's
/// outcome, so it will be fetched again with its previous state) and <c>unsaved</c> (the outcome
/// was recorded but the store's <c>SaveChangesAsync</c> failed, so it was rolled back and the message
/// will be fetched again). Alert on <c>outcome=exhausted</c>, <c>store_error</c> and <c>unsaved</c>.
/// </para>
/// <para>
/// The outcomes of a batch are recorded once per batch, after the background processor has saved
/// them: a batch whose save fails is counted entirely as <c>unsaved</c>, never as <c>success</c>.
/// <c>OutboxOrchestrator.ProcessPendingMessagesAsync</c> leaves the save to its caller, so it records
/// the outcomes when the batch ends.
/// A message interrupted by cancellation is not counted; it keeps its state and retry budget.
/// </para>
/// <para>
/// The counter <c>encina.outbox.messages_requeued_total</c> counts the exhausted messages returned to
/// the pending state by <c>OutboxOrchestrator.RequeueExhaustedAsync</c>.
/// </para>
/// <para>
/// The gauge <c>encina.outbox.messages_exhausted</c> reports the number of exhausted messages last
/// observed by <c>OutboxHealthCheck</c> or <c>OutboxOrchestrator.GetExhaustedCountAsync</c>. It does
/// not query the store itself, so it reports nothing until one of them has run and it is only as
/// fresh as the last health check.
/// </para>
/// <para>
/// A single instance is shared by every processor in the process so that the instruments are
/// created once, following the pattern of <see cref="SchedulingProcessorMetrics"/>.
/// </para>
/// </remarks>
internal sealed class OutboxProcessorMetrics
{
    internal const string OutcomeSuccess = "success";
    internal const string OutcomeFailure = "failure";
    internal const string OutcomeExhausted = "exhausted";
    internal const string OutcomeStoreError = "store_error";
    internal const string OutcomeUnsaved = "unsaved";

    private static readonly Meter Meter = new("Encina", "1.0");

    private readonly Counter<long> _messagesProcessed;
    private readonly Counter<long> _messagesRequeued;

    // -1 until a count has been observed, so that the gauge reports nothing rather than a false zero.
    private long _lastExhaustedCount = -1;

    private OutboxProcessorMetrics()
    {
        _messagesProcessed = Meter.CreateCounter<long>(
            "encina.outbox.processor.messages_total",
            unit: "{messages}",
            description: "Total number of outbox messages handled by the outbox processors, by outcome (success, failure, exhausted, store_error, unsaved).");

        _messagesRequeued = Meter.CreateCounter<long>(
            "encina.outbox.messages_requeued_total",
            unit: "{messages}",
            description: "Total number of exhausted outbox messages returned to the pending state.");

        Meter.CreateObservableGauge(
            "encina.outbox.messages_exhausted",
            ObserveExhaustedCount,
            unit: "{messages}",
            description: "Number of outbox messages whose retries are exhausted, as last observed by the outbox health check or orchestrator.");
    }

    /// <summary>
    /// Gets the process-wide instance.
    /// </summary>
    internal static OutboxProcessorMetrics Instance { get; } = new();

    /// <summary>
    /// Gets the last observed number of exhausted messages, or <c>-1</c> when none has been observed.
    /// </summary>
    internal long LastExhaustedCount => Interlocked.Read(ref _lastExhaustedCount);

    /// <summary>
    /// Records the saved outcomes of one batch.
    /// </summary>
    /// <param name="result">The outcome counts of the batch.</param>
    internal void RecordBatch(Outbox.OutboxBatchResult result)
    {
        RecordOutcome(OutcomeSuccess, result.Succeeded);
        RecordOutcome(OutcomeFailure, result.Failed);
        RecordOutcome(OutcomeExhausted, result.Exhausted);
        RecordOutcome(OutcomeStoreError, result.StoreErrors);
    }

    /// <summary>
    /// Records a batch whose outcomes were not saved, counting every message in it as <see cref="OutcomeUnsaved"/>.
    /// </summary>
    /// <param name="result">The outcome counts of the batch that could not be saved.</param>
    internal void RecordUnsavedBatch(Outbox.OutboxBatchResult result)
    {
        RecordOutcome(OutcomeUnsaved, result.Total);
    }

    private void RecordOutcome(string outcome, int count)
    {
        if (count > 0)
        {
            _messagesProcessed.Add(count, new KeyValuePair<string, object?>("outcome", outcome));
        }
    }

    /// <summary>
    /// Records the number of exhausted messages returned to the pending state.
    /// </summary>
    /// <param name="count">The number of messages requeued.</param>
    internal void RecordRequeued(int count)
    {
        if (count > 0)
        {
            _messagesRequeued.Add(count);
        }
    }

    /// <summary>
    /// Stores the number of exhausted messages reported by the gauge.
    /// </summary>
    /// <param name="count">The number of exhausted messages just counted in the store.</param>
    internal void SetExhaustedCount(int count)
    {
        Interlocked.Exchange(ref _lastExhaustedCount, count);
    }

    private IEnumerable<Measurement<long>> ObserveExhaustedCount()
    {
        var count = LastExhaustedCount;
        return count < 0 ? [] : [new Measurement<long>(count)];
    }
}

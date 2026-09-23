using System.Diagnostics.Metrics;

namespace Encina.Messaging.Diagnostics;

/// <summary>
/// Exposes the delivery outcomes of the outbox processors via the shared <c>"Encina"</c> meter.
/// </summary>
/// <remarks>
/// <para>
/// The counter <c>encina.outbox.processor.messages_total</c> is tagged with <c>outcome</c>:
/// <c>success</c> (delivered and marked processed), <c>failure</c> (a retry was scheduled)
/// and <c>exhausted</c> (the failure used up <c>OutboxOptions.MaxRetries</c>, so the message
/// will not be fetched again). Alert on <c>outcome=exhausted</c>.
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
            description: "Total number of outbox messages handled by the outbox processors, by outcome (success, failure, exhausted).");

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
    /// Records one message outcome.
    /// </summary>
    /// <param name="outcome">One of <see cref="OutcomeSuccess"/>, <see cref="OutcomeFailure"/> or <see cref="OutcomeExhausted"/>.</param>
    internal void RecordOutcome(string outcome)
    {
        _messagesProcessed.Add(1, new KeyValuePair<string, object?>("outcome", outcome));
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

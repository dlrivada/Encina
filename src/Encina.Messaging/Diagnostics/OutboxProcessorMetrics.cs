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
/// A single instance is shared by every processor in the process so that the instrument is
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

    private OutboxProcessorMetrics()
    {
        _messagesProcessed = Meter.CreateCounter<long>(
            "encina.outbox.processor.messages_total",
            unit: "{messages}",
            description: "Total number of outbox messages handled by the outbox processors, by outcome (success, failure, exhausted).");
    }

    /// <summary>
    /// Gets the process-wide instance.
    /// </summary>
    internal static OutboxProcessorMetrics Instance { get; } = new();

    /// <summary>
    /// Records one message outcome.
    /// </summary>
    /// <param name="outcome">One of <see cref="OutcomeSuccess"/>, <see cref="OutcomeFailure"/> or <see cref="OutcomeExhausted"/>.</param>
    internal void RecordOutcome(string outcome)
    {
        _messagesProcessed.Add(1, new KeyValuePair<string, object?>("outcome", outcome));
    }
}

using Encina.Messaging.Recoverability;
using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests;

/// <summary>
/// Invariants of the delayed-retry chain marker (#2083): a re-dispatch marker applies to the
/// re-dispatched request instance only, and a restored chain keeps its identity.
/// </summary>
public sealed class DelayedRetryChainProperties
{
    private sealed record Payload(int Value);

    [Property(MaxTest = 100)]
    public bool Marker_IsTakenOnceAndOnlyByItsRequestType(NonNegativeInt extraConsumes)
    {
        using var scope = DelayedRetryRedispatch.Begin(typeof(Payload));

        var otherTypeTakesIt = DelayedRetryRedispatch.Consume(typeof(string)) is not null;
        var first = DelayedRetryRedispatch.Consume(typeof(Payload));
        var laterTakes = Enumerable.Range(0, extraConsumes.Get % 5 + 1)
            .Any(_ => DelayedRetryRedispatch.Consume(typeof(Payload)) is not null);

        return !otherTypeTakesIt && ReferenceEquals(first, scope.Marker) && !laterTakes;
    }

    [Property(MaxTest = 100)]
    public bool Marker_ReportsTheLastClassification_AndEndsWithItsScope(NonNegativeInt classificationSeed)
    {
        var values = Enum.GetValues<ErrorClassification>();
        var classification = values[classificationSeed.Get % values.Length];

        var scope = DelayedRetryRedispatch.Begin(typeof(Payload));
        scope.Marker.Report(classification);
        var reported = scope.Marker.Classification == classification;
        scope.Dispose();

        return reported && DelayedRetryRedispatch.Consume(typeof(Payload)) is null;
    }

    [Property(MaxTest = 100)]
    public bool RestoreChain_KeepsTheLogicalMessageIdentity(Guid id, DateTime startedAt)
    {
        var started = DateTime.SpecifyKind(startedAt, DateTimeKind.Utc);
        var context = new RecoverabilityContext();

        context.RestoreChain(id, started);
        context.IncrementDelayedRetry();
        context.IncrementDelayedRetry();

        return context.Id == id && context.StartedAtUtc == started && context.DelayedRetryCount == 2;
    }
}

using Encina.Messaging.Recoverability;
using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests;

/// <summary>
/// Invariants of the delayed-retry chain marker (#2083): a re-dispatch marker matches the
/// re-dispatched request (by reference for a class, by value for a struct) every time it is asked
/// and no other request, and a restored chain keeps its identity.
/// </summary>
public sealed class DelayedRetryChainProperties
{
    private sealed record Payload(int Value);

    private readonly record struct StructPayload(int Value);

    [Property(MaxTest = 100)]
    public bool Marker_MatchesTheSameClassInstanceEveryTime_AndNoOtherInstance(int value, NonNegativeInt askSeed)
    {
        var request = new Payload(value);
        using var scope = DelayedRetryRedispatch.Begin(request);

        var everyTime = Enumerable.Range(0, askSeed.Get % 5 + 1)
            .All(_ => ReferenceEquals(DelayedRetryRedispatch.For(request), scope.Marker));

        return everyTime && DelayedRetryRedispatch.For(new Payload(value)) is null;
    }

    [Property(MaxTest = 100)]
    public bool Marker_MatchesAStructByValue(int value, int other)
    {
        using var scope = DelayedRetryRedispatch.Begin(new StructPayload(value));

        var equalMatches = ReferenceEquals(DelayedRetryRedispatch.For(new StructPayload(value)), scope.Marker);
        var otherMatches = DelayedRetryRedispatch.For(new StructPayload(other)) is not null;

        return equalMatches && otherMatches == (other == value);
    }

    [Property(MaxTest = 100)]
    public bool Marker_ReportsTheLastClassification_AndEndsWithItsScope(NonNegativeInt classificationSeed)
    {
        var values = Enum.GetValues<ErrorClassification>();
        var classification = values[classificationSeed.Get % values.Length];
        var request = new Payload(1);
        var attempt = new RecoverabilityContext();
        attempt.RecordFailedAttempt(EncinaError.New("failure"), null, classification);

        var scope = DelayedRetryRedispatch.Begin(request);
        scope.Marker.Report(attempt);
        var reported = scope.Marker.Classification == classification;
        scope.Dispose();

        return reported && DelayedRetryRedispatch.For(request) is null;
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

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
    public bool Marker_AppliesOnlyToTheReDispatchedInstance(int value, NonNegativeInt othersSeed)
    {
        var marked = new Payload(value);
        var others = Enumerable.Range(0, othersSeed.Get % 5 + 1).Select(_ => new Payload(value)).ToArray();

        using var scope = DelayedRetryRedispatch.Begin(marked);

        return ReferenceEquals(DelayedRetryRedispatch.For(marked), scope.Marker)
            && others.All(other => DelayedRetryRedispatch.For(other) is null);
    }

    [Property(MaxTest = 100)]
    public bool Marker_ReportsTheLastClassification_AndEndsWithItsScope(NonNegativeInt classificationSeed)
    {
        var values = Enum.GetValues<ErrorClassification>();
        var classification = values[classificationSeed.Get % values.Length];
        var request = new Payload(1);

        var scope = DelayedRetryRedispatch.Begin(request);
        scope.Marker.Report(classification);
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

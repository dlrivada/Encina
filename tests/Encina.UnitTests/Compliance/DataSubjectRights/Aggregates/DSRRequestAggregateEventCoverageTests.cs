using Encina.Compliance.DataSubjectRights;
using Encina.Compliance.DataSubjectRights.Aggregates;
using Encina.Compliance.DataSubjectRights.Events;

using Shouldly;

namespace Encina.UnitTests.Compliance.DataSubjectRights.Aggregates;

/// <summary>
/// Exhaustiveness of the aggregate's event handler table: every event record the module publishes
/// must change the aggregate when replayed, so a new event cannot be added without its handler.
/// </summary>
public sealed class DSRRequestAggregateEventCoverageTests
{
    private static readonly Guid Id = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    // One sample per event type; the exhaustiveness test below fails when an event is missing here.
    private static readonly DSRRequestSubmitted Submitted =
        new(Id, "subject-1", DataSubjectRight.Access, Now, Now.AddDays(30), "details", "tenant-1", "module-1");

    private static readonly Dictionary<Type, (object Event, Action<DSRRequestAggregate> Assert)> Samples = new()
    {
        [typeof(DSRRequestSubmitted)] = (Submitted, a =>
        {
            a.Status.ShouldBe(DSRRequestStatus.Received);
            a.SubjectId.ShouldBe("subject-1");
            a.TenantId.ShouldBe("tenant-1");
        }),
        [typeof(DSRRequestVerified)] = (new DSRRequestVerified(Id, "admin", Now.AddHours(1), "tenant-1", "module-1"), a =>
        {
            a.Status.ShouldBe(DSRRequestStatus.IdentityVerified);
            a.VerifiedAtUtc.ShouldBe(Now.AddHours(1));
        }),
        [typeof(DSRRequestProcessing)] = (new DSRRequestProcessing(Id, "worker", Now.AddHours(2), "tenant-1", "module-1"), a =>
        {
            a.Status.ShouldBe(DSRRequestStatus.InProgress);
            a.ProcessedByUserId.ShouldBe("worker");
        }),
        [typeof(DSRRequestCompleted)] = (new DSRRequestCompleted(Id, Now.AddHours(3), "tenant-1", "module-1"), a =>
        {
            a.Status.ShouldBe(DSRRequestStatus.Completed);
            a.CompletedAtUtc.ShouldBe(Now.AddHours(3));
        }),
        [typeof(DSRRequestDenied)] = (new DSRRequestDenied(
            Id, DSRRejectionReason.Excessive, "repeated", Now.AddHours(4), "tenant-1", "module-1"), a =>
        {
            a.Status.ShouldBe(DSRRequestStatus.Rejected);
            a.RejectionReason.ShouldBe(DSRRejectionReason.Excessive);
            a.RejectionExplanation.ShouldBe("repeated");
        }),
        [typeof(DSRRequestExtended)] = (new DSRRequestExtended(
            Id, "complex", Now.AddDays(90), Now.AddDays(20), "tenant-1", "module-1"), a =>
        {
            a.Status.ShouldBe(DSRRequestStatus.Extended);
            a.ExtensionReason.ShouldBe("complex");
            a.ExtendedDeadlineAtUtc.ShouldBe(Now.AddDays(90));
        }),
        [typeof(DSRRequestExpired)] = (new DSRRequestExpired(Id, Now.AddDays(100), "tenant-1", "module-1"), a =>
            a.Status.ShouldBe(DSRRequestStatus.Expired))
    };

    public static TheoryData<Type> EventTypes => new(Samples.Keys);

    [Fact]
    public void EveryDsrEventRecord_HasASampleInThisTest()
    {
        var published = typeof(DSRRequestSubmitted).Assembly.GetTypes()
            .Where(t => t.Namespace == typeof(DSRRequestSubmitted).Namespace && t.IsSealed && t.IsPublic
                        && t.Name.StartsWith("DSRRequest", StringComparison.Ordinal))
            .ToHashSet();

        published.ShouldBe(Samples.Keys, ignoreOrder: true);
    }

    [Theory]
    [MemberData(nameof(EventTypes))]
    public void Replaying_AnEvent_AppliesItsStateChange(Type eventType)
    {
        var (domainEvent, assertState) = Samples[eventType];
        var aggregate = new DSRRequestAggregate();

        aggregate.LoadFromHistory(eventType == typeof(DSRRequestSubmitted)
            ? [domainEvent]
            : [Submitted, domainEvent]);

        assertState(aggregate);
    }

    [Fact]
    public void Replaying_AnUnknownEvent_IsIgnored()
    {
        var aggregate = new DSRRequestAggregate();
        aggregate.LoadFromHistory([Submitted, new object()]);

        aggregate.Status.ShouldBe(DSRRequestStatus.Received);
    }
}

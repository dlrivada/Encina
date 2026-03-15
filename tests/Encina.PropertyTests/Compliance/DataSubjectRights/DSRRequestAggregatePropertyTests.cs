using Encina.Compliance.DataSubjectRights;
using Encina.Compliance.DataSubjectRights.Aggregates;

namespace Encina.PropertyTests.Compliance.DataSubjectRights;

/// <summary>
/// Property-based tests for <see cref="DSRRequestAggregate"/> verifying domain invariants
/// using FsCheck random data generation.
/// </summary>
public class DSRRequestAggregatePropertyTests
{
    #region Submit Invariants

    /// <summary>
    /// Invariant: DeadlineAtUtc is always exactly 30 days after ReceivedAtUtc.
    /// Per GDPR Article 12(3), the controller must respond within one month of receipt.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property_Submit_DeadlineAlways30DaysAfterReceivedAt(NonEmptyString subjectId)
    {
        var receivedAt = DateTimeOffset.UtcNow;
        var aggregate = DSRRequestAggregate.Submit(
            Guid.NewGuid(), subjectId.Get, DataSubjectRight.Access, receivedAt);

        return aggregate.DeadlineAtUtc == receivedAt.AddDays(30);
    }

    /// <summary>
    /// Invariant: Newly submitted requests always have Received status.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Property_Submit_StatusAlwaysReceived(NonEmptyString subjectId)
    {
        var aggregate = DSRRequestAggregate.Submit(
            Guid.NewGuid(), subjectId.Get, DataSubjectRight.Erasure, DateTimeOffset.UtcNow);

        return aggregate.Status == DSRRequestStatus.Received;
    }

    /// <summary>
    /// Invariant: Version is always 1 after Submit (one event raised).
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Property_Submit_VersionAlwaysOne(NonEmptyString subjectId)
    {
        var aggregate = DSRRequestAggregate.Submit(
            Guid.NewGuid(), subjectId.Get, DataSubjectRight.Access, DateTimeOffset.UtcNow);

        return aggregate.Version == 1;
    }

    /// <summary>
    /// Invariant: SubjectId and RightType are always preserved as provided.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Property_Submit_IdentityPreserved(NonEmptyString subjectId)
    {
        var id = Guid.NewGuid();
        var aggregate = DSRRequestAggregate.Submit(
            id, subjectId.Get, DataSubjectRight.Portability, DateTimeOffset.UtcNow);

        return aggregate.Id == id
            && aggregate.SubjectId == subjectId.Get
            && aggregate.RightType == DataSubjectRight.Portability;
    }

    /// <summary>
    /// Invariant: Optional fields are always null after Submit.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Property_Submit_OptionalFieldsAlwaysNull(NonEmptyString subjectId)
    {
        var aggregate = DSRRequestAggregate.Submit(
            Guid.NewGuid(), subjectId.Get, DataSubjectRight.Access, DateTimeOffset.UtcNow);

        return aggregate.CompletedAtUtc is null
            && aggregate.VerifiedAtUtc is null
            && aggregate.ExtendedDeadlineAtUtc is null
            && aggregate.ExtensionReason is null
            && aggregate.RejectionReason is null
            && aggregate.ProcessedByUserId is null;
    }

    #endregion

    #region Lifecycle Invariants

    /// <summary>
    /// Invariant: After the full happy-path lifecycle (Submit → Verify → Process → Complete),
    /// the version equals the number of transitions + 1.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Property_FullLifecycle_VersionEquals4(NonEmptyString subjectId)
    {
        var now = DateTimeOffset.UtcNow;
        var aggregate = DSRRequestAggregate.Submit(
            Guid.NewGuid(), subjectId.Get, DataSubjectRight.Erasure, now);
        aggregate.Verify("admin", now.AddHours(1));
        aggregate.StartProcessing("operator", now.AddHours(2));
        aggregate.Complete(now.AddDays(5));

        return aggregate.Version == 4
            && aggregate.Status == DSRRequestStatus.Completed
            && aggregate.UncommittedEvents.Count == 4;
    }

    /// <summary>
    /// Invariant: Terminal statuses (Completed, Rejected, Expired) are never overdue.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Property_TerminalStatus_NeverOverdue(NonEmptyString subjectId, PositiveInt daysAfterDeadline)
    {
        var now = DateTimeOffset.UtcNow;
        var aggregate = DSRRequestAggregate.Submit(
            Guid.NewGuid(), subjectId.Get, DataSubjectRight.Access, now);
        aggregate.Verify("admin", now.AddHours(1));
        aggregate.StartProcessing("operator", now.AddHours(2));
        aggregate.Complete(now.AddDays(5));

        var checkTime = aggregate.DeadlineAtUtc.AddDays(daysAfterDeadline.Get);
        return !aggregate.IsOverdue(checkTime);
    }

    /// <summary>
    /// Invariant: Extended deadline is always DeadlineAtUtc + 2 months.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Property_Extend_DeadlineAlwaysOriginalPlus2Months(NonEmptyString subjectId)
    {
        var now = DateTimeOffset.UtcNow;
        var aggregate = DSRRequestAggregate.Submit(
            Guid.NewGuid(), subjectId.Get, DataSubjectRight.Access, now);

        var originalDeadline = aggregate.DeadlineAtUtc;
        aggregate.Extend("Complex request", now.AddDays(10));

        return aggregate.ExtendedDeadlineAtUtc == originalDeadline.AddMonths(2);
    }

    /// <summary>
    /// Invariant: GetEffectiveDeadline returns ExtendedDeadlineAtUtc when extended, DeadlineAtUtc otherwise.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Property_GetEffectiveDeadline_ReturnsExtendedWhenAvailable(NonEmptyString subjectId)
    {
        var now = DateTimeOffset.UtcNow;
        var aggregate = DSRRequestAggregate.Submit(
            Guid.NewGuid(), subjectId.Get, DataSubjectRight.Access, now);

        var beforeExtend = aggregate.GetEffectiveDeadline() == aggregate.DeadlineAtUtc;

        aggregate.Extend("Reason", now.AddDays(10));

        var afterExtend = aggregate.GetEffectiveDeadline() == aggregate.ExtendedDeadlineAtUtc;

        return beforeExtend && afterExtend;
    }

    #endregion
}

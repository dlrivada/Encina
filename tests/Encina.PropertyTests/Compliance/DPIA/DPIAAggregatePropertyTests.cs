using Encina.Compliance.DPIA.Aggregates;
using Encina.Compliance.DPIA.Events;
using Encina.Compliance.DPIA.Model;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.DPIA;

/// <summary>
/// Property-based tests for <see cref="DPIAAggregate"/> verifying domain invariants
/// using FsCheck random data generation.
/// </summary>
public class DPIAAggregatePropertyTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 16, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Invariant: Creating a DPIA aggregate with any valid inputs always produces Draft status.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Create_AlwaysProducesDraftStatus(NonEmptyString requestTypeName)
    {
        if (string.IsNullOrWhiteSpace(requestTypeName.Get)) return true; // skip whitespace-only inputs

        var aggregate = DPIAAggregate.Create(Guid.NewGuid(), requestTypeName.Get, Now);
        return aggregate.Status == DPIAAssessmentStatus.Draft;
    }

    /// <summary>
    /// Invariant: The RequestTypeName on the aggregate always matches the input provided at creation.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Create_AlwaysSetsRequestTypeName(NonEmptyString requestTypeName)
    {
        if (string.IsNullOrWhiteSpace(requestTypeName.Get)) return true;

        var aggregate = DPIAAggregate.Create(Guid.NewGuid(), requestTypeName.Get, Now);
        return aggregate.RequestTypeName == requestTypeName.Get;
    }

    /// <summary>
    /// Invariant: Creating a DPIA aggregate always raises exactly one uncommitted event.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Create_AlwaysRaisesOneEvent(NonEmptyString requestTypeName)
    {
        if (string.IsNullOrWhiteSpace(requestTypeName.Get)) return true;

        var aggregate = DPIAAggregate.Create(Guid.NewGuid(), requestTypeName.Get, Now);
        return aggregate.UncommittedEvents.Count == 1
            && aggregate.UncommittedEvents[0] is DPIACreated;
    }

    /// <summary>
    /// Invariant: Evaluating a Draft assessment with any valid DPIAResult always transitions to InReview.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Evaluate_FromDraft_AlwaysTransitionsToInReview(NonEmptyString requestTypeName, bool requiresConsultation)
    {
        if (string.IsNullOrWhiteSpace(requestTypeName.Get)) return true;

        var aggregate = DPIAAggregate.Create(Guid.NewGuid(), requestTypeName.Get, Now);
        var result = CreateResult(requiresConsultation: requiresConsultation);

        aggregate.Evaluate(result, Now);

        return aggregate.Status == DPIAAssessmentStatus.InReview;
    }

    /// <summary>
    /// Invariant: Approving an InReview assessment always transitions to Approved.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Approve_FromInReview_AlwaysTransitionsToApproved(NonEmptyString requestTypeName, NonEmptyString approvedBy)
    {
        if (string.IsNullOrWhiteSpace(requestTypeName.Get) || string.IsNullOrWhiteSpace(approvedBy.Get)) return true;

        var aggregate = CreateInReviewAggregate(requestTypeName.Get);

        aggregate.Approve(approvedBy.Get, Now);

        return aggregate.Status == DPIAAssessmentStatus.Approved;
    }

    /// <summary>
    /// Invariant: An approved assessment with a future NextReviewAtUtc is always current.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool IsCurrent_ApprovedWithFutureReview_AlwaysReturnsTrue(NonEmptyString requestTypeName, PositiveInt daysAhead)
    {
        if (string.IsNullOrWhiteSpace(requestTypeName.Get)) return true;

        var aggregate = CreateInReviewAggregate(requestTypeName.Get);
        var futureReview = Now.AddDays(daysAhead.Get);

        aggregate.Approve("approver", Now, nextReviewAtUtc: futureReview);

        return aggregate.IsCurrent(Now);
    }

    /// <summary>
    /// Invariant: A non-approved assessment is never current, regardless of NextReviewAtUtc.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool IsCurrent_NonApproved_AlwaysReturnsFalse(NonEmptyString requestTypeName)
    {
        if (string.IsNullOrWhiteSpace(requestTypeName.Get)) return true;

        var draftAggregate = DPIAAggregate.Create(Guid.NewGuid(), requestTypeName.Get, Now);

        var inReviewAggregate = CreateInReviewAggregate(requestTypeName.Get);

        var rejectedAggregate = CreateInReviewAggregate(requestTypeName.Get);
        rejectedAggregate.Reject("reviewer", "Insufficient mitigations", Now);

        var revisionAggregate = CreateInReviewAggregate(requestTypeName.Get);
        revisionAggregate.RequestRevision("reviewer", "Needs more detail", Now);

        return !draftAggregate.IsCurrent(Now)
            && !inReviewAggregate.IsCurrent(Now)
            && !rejectedAggregate.IsCurrent(Now)
            && !revisionAggregate.IsCurrent(Now);
    }

    /// <summary>
    /// Invariant: Rejecting an InReview assessment always transitions to Rejected.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Reject_FromInReview_AlwaysTransitionsToRejected(NonEmptyString requestTypeName, NonEmptyString rejectedBy, NonEmptyString reason)
    {
        if (string.IsNullOrWhiteSpace(requestTypeName.Get)
            || string.IsNullOrWhiteSpace(rejectedBy.Get)
            || string.IsNullOrWhiteSpace(reason.Get)) return true;

        var aggregate = CreateInReviewAggregate(requestTypeName.Get);

        aggregate.Reject(rejectedBy.Get, reason.Get, Now);

        return aggregate.Status == DPIAAssessmentStatus.Rejected;
    }

    private static DPIAAggregate CreateInReviewAggregate(string requestTypeName)
    {
        var aggregate = DPIAAggregate.Create(Guid.NewGuid(), requestTypeName, Now);
        aggregate.Evaluate(CreateResult(), Now);
        return aggregate;
    }

    private static DPIAResult CreateResult(RiskLevel risk = RiskLevel.Medium, bool requiresConsultation = false) => new()
    {
        OverallRisk = risk,
        IdentifiedRisks = [],
        ProposedMitigations = [],
        RequiresPriorConsultation = requiresConsultation,
        AssessedAtUtc = Now,
    };
}

using Encina.Compliance.DPIA.Model;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.DPIA;

/// <summary>
/// Property-based tests for <see cref="DPIAAssessment"/> verifying domain invariants
/// using FsCheck random data generation.
/// </summary>
public class DPIAAssessmentPropertyTests
{
    #region IsCurrent Invariants

    /// <summary>
    /// Invariant: An approved assessment with no review date is always current.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool IsCurrent_ApprovedWithNoReviewDate_AlwaysTrue()
    {
        var assessment = new DPIAAssessment
        {
            Id = Guid.NewGuid(),
            RequestTypeName = "Ns.TestCommand",
            Status = DPIAAssessmentStatus.Approved,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            NextReviewAtUtc = null
        };

        return assessment.IsCurrent(DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Invariant: An approved assessment with future review date is always current.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool IsCurrent_ApprovedWithFutureReview_AlwaysTrue(PositiveInt daysAhead)
    {
        var now = DateTimeOffset.UtcNow;
        var assessment = new DPIAAssessment
        {
            Id = Guid.NewGuid(),
            RequestTypeName = "Ns.TestCommand",
            Status = DPIAAssessmentStatus.Approved,
            CreatedAtUtc = now,
            NextReviewAtUtc = now.AddDays(daysAhead.Get)
        };

        return assessment.IsCurrent(now);
    }

    /// <summary>
    /// Invariant: An approved assessment with past review date is never current.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool IsCurrent_ApprovedWithPastReview_AlwaysFalse(PositiveInt daysBehind)
    {
        var now = DateTimeOffset.UtcNow;
        var assessment = new DPIAAssessment
        {
            Id = Guid.NewGuid(),
            RequestTypeName = "Ns.TestCommand",
            Status = DPIAAssessmentStatus.Approved,
            CreatedAtUtc = now.AddDays(-365),
            NextReviewAtUtc = now.AddDays(-daysBehind.Get)
        };

        return !assessment.IsCurrent(now);
    }

    /// <summary>
    /// Invariant: A non-approved assessment is never current regardless of review date.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool IsCurrent_NonApprovedStatus_AlwaysFalse(PositiveInt daysAhead)
    {
        var now = DateTimeOffset.UtcNow;
        var nonApprovedStatuses = new[]
        {
            DPIAAssessmentStatus.Draft,
            DPIAAssessmentStatus.InReview,
            DPIAAssessmentStatus.Rejected,
            DPIAAssessmentStatus.RequiresRevision,
            DPIAAssessmentStatus.Expired
        };

        return nonApprovedStatuses.All(status =>
        {
            var assessment = new DPIAAssessment
            {
                Id = Guid.NewGuid(),
                RequestTypeName = "Ns.TestCommand",
                Status = status,
                CreatedAtUtc = now,
                NextReviewAtUtc = now.AddDays(daysAhead.Get)
            };
            return !assessment.IsCurrent(now);
        });
    }

    #endregion

    #region Record Immutability

    /// <summary>
    /// Invariant: 'with' expression creates a new instance preserving all other properties.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool WithExpression_PreservesOtherProperties(NonEmptyString requestType)
    {
        var original = new DPIAAssessment
        {
            Id = Guid.NewGuid(),
            RequestTypeName = requestType.Get,
            Status = DPIAAssessmentStatus.Draft,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var modified = original with { Status = DPIAAssessmentStatus.Approved };

        return modified.Id == original.Id
            && modified.RequestTypeName == original.RequestTypeName
            && modified.CreatedAtUtc == original.CreatedAtUtc
            && modified.Status == DPIAAssessmentStatus.Approved;
    }

    #endregion
}

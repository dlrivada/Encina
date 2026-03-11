#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.DPIA.Model;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.DPIA;

/// <summary>
/// Unit tests for <see cref="DPIAAssessment"/>.
/// </summary>
public class DPIAAssessmentTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    private static DPIAAssessment CreateDefaultAssessment(
        DPIAAssessmentStatus status = DPIAAssessmentStatus.Draft,
        DateTimeOffset? nextReviewAtUtc = null) => new()
    {
        Id = Guid.NewGuid(),
        RequestTypeName = "TestNamespace.TestCommand",
        Status = status,
        CreatedAtUtc = FixedNow.AddDays(-30),
        NextReviewAtUtc = nextReviewAtUtc,
    };

    #region IsCurrent Tests

    [Fact]
    public void IsCurrent_Approved_NoReviewDate_ReturnsTrue()
    {
        var assessment = CreateDefaultAssessment(DPIAAssessmentStatus.Approved);

        assessment.IsCurrent(FixedNow).Should().BeTrue();
    }

    [Fact]
    public void IsCurrent_Approved_FutureReviewDate_ReturnsTrue()
    {
        var assessment = CreateDefaultAssessment(
            DPIAAssessmentStatus.Approved,
            nextReviewAtUtc: FixedNow.AddDays(30));

        assessment.IsCurrent(FixedNow).Should().BeTrue();
    }

    [Fact]
    public void IsCurrent_Approved_PastReviewDate_ReturnsFalse()
    {
        var assessment = CreateDefaultAssessment(
            DPIAAssessmentStatus.Approved,
            nextReviewAtUtc: FixedNow.AddDays(-1));

        assessment.IsCurrent(FixedNow).Should().BeFalse();
    }

    [Fact]
    public void IsCurrent_Approved_ExactReviewDate_ReturnsFalse()
    {
        var assessment = CreateDefaultAssessment(
            DPIAAssessmentStatus.Approved,
            nextReviewAtUtc: FixedNow);

        assessment.IsCurrent(FixedNow).Should().BeFalse();
    }

    [Theory]
    [InlineData(DPIAAssessmentStatus.Draft)]
    [InlineData(DPIAAssessmentStatus.InReview)]
    [InlineData(DPIAAssessmentStatus.Rejected)]
    [InlineData(DPIAAssessmentStatus.RequiresRevision)]
    [InlineData(DPIAAssessmentStatus.Expired)]
    public void IsCurrent_NonApprovedStatus_ReturnsFalse(DPIAAssessmentStatus status)
    {
        var assessment = CreateDefaultAssessment(status);

        assessment.IsCurrent(FixedNow).Should().BeFalse();
    }

    #endregion

    #region Record Immutability Tests

    [Fact]
    public void WithExpression_ShouldCreateNewInstance()
    {
        var original = CreateDefaultAssessment(DPIAAssessmentStatus.Draft);

        var updated = original with { Status = DPIAAssessmentStatus.Approved };

        updated.Status.Should().Be(DPIAAssessmentStatus.Approved);
        original.Status.Should().Be(DPIAAssessmentStatus.Draft);
        updated.Id.Should().Be(original.Id);
    }

    [Fact]
    public void AuditTrail_DefaultsToEmptyList()
    {
        var assessment = CreateDefaultAssessment();

        assessment.AuditTrail.Should().BeEmpty();
    }

    #endregion
}

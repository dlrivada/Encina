using Encina.Compliance.GDPR;
using Encina.Compliance.LawfulBasis.Aggregates;

namespace Encina.GuardTests.Compliance.LawfulBasis;

/// <summary>
/// Guard tests for <see cref="LIAAggregate"/> verifying argument validation and state guards.
/// </summary>
public class LIAAggregateGuardTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly IReadOnlyList<string> Alternatives = ["Manual review"];
    private static readonly IReadOnlyList<string> Safeguards = ["Encryption"];

    private static LIAAggregate CreateValid() =>
        LIAAggregate.Create(
            Guid.NewGuid(),
            "LIA-001",
            "Test LIA",
            "Test purpose",
            "Interest",
            "Benefits",
            "Consequences",
            "Necessity",
            Alternatives,
            "Data min",
            "Nature",
            "Reasonable",
            "Impact",
            Safeguards,
            "DPO",
            true,
            Now);

    private static LIAAggregate CreateApproved()
    {
        var agg = CreateValid();
        agg.Approve("Approved conclusion", "Approver", Now.AddDays(1));
        return agg;
    }

    private static LIAAggregate CreateRejected()
    {
        var agg = CreateValid();
        agg.Reject("Rejected conclusion", "Rejecter", Now.AddDays(1));
        return agg;
    }

    // ================================================================
    // Create guards (required strings)
    // ================================================================

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_NullOrEmptyReference_Throws(string? reference)
    {
        Should.Throw<ArgumentException>(() =>
            LIAAggregate.Create(
                Guid.NewGuid(), reference!, "name", "purpose",
                "interest", "benefits", "consequences", "necessity",
                Alternatives, "min", "nature", "reasonable",
                "impact", Safeguards, "DPO", true, Now));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Create_NullOrEmptyName_Throws(string? name)
    {
        Should.Throw<ArgumentException>(() =>
            LIAAggregate.Create(
                Guid.NewGuid(), "LIA-001", name!, "purpose",
                "interest", "benefits", "consequences", "necessity",
                Alternatives, "min", "nature", "reasonable",
                "impact", Safeguards, "DPO", true, Now));
    }

    [Fact]
    public void Create_NullAlternatives_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            LIAAggregate.Create(
                Guid.NewGuid(), "LIA-001", "name", "purpose",
                "interest", "benefits", "consequences", "necessity",
                null!, "min", "nature", "reasonable",
                "impact", Safeguards, "DPO", true, Now));
    }

    [Fact]
    public void Create_NullSafeguards_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            LIAAggregate.Create(
                Guid.NewGuid(), "LIA-001", "name", "purpose",
                "interest", "benefits", "consequences", "necessity",
                Alternatives, "min", "nature", "reasonable",
                "impact", null!, "DPO", true, Now));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_NullOrEmptyAssessedBy_Throws(string? assessedBy)
    {
        Should.Throw<ArgumentException>(() =>
            LIAAggregate.Create(
                Guid.NewGuid(), "LIA-001", "name", "purpose",
                "interest", "benefits", "consequences", "necessity",
                Alternatives, "min", "nature", "reasonable",
                "impact", Safeguards, assessedBy!, true, Now));
    }

    // ================================================================
    // Approve guards
    // ================================================================

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Approve_NullOrEmptyConclusion_ThrowsArgumentException(string? conclusion)
    {
        var agg = CreateValid();
        Should.Throw<ArgumentException>(() =>
            agg.Approve(conclusion!, "Approver", Now.AddDays(1)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Approve_NullOrEmptyApprovedBy_ThrowsArgumentException(string? approvedBy)
    {
        var agg = CreateValid();
        Should.Throw<ArgumentException>(() =>
            agg.Approve("Conclusion", approvedBy!, Now.AddDays(1)));
    }

    [Fact]
    public void Approve_WhenAlreadyApproved_ThrowsInvalidOperation()
    {
        var agg = CreateApproved();
        Should.Throw<InvalidOperationException>(() =>
            agg.Approve("Again", "Approver", Now.AddDays(2)));
    }

    [Fact]
    public void Approve_WhenRejected_ThrowsInvalidOperation()
    {
        var agg = CreateRejected();
        Should.Throw<InvalidOperationException>(() =>
            agg.Approve("Approve rejected", "Approver", Now.AddDays(2)));
    }

    // ================================================================
    // Reject guards
    // ================================================================

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Reject_NullOrEmptyConclusion_ThrowsArgumentException(string? conclusion)
    {
        var agg = CreateValid();
        Should.Throw<ArgumentException>(() =>
            agg.Reject(conclusion!, "Rejecter", Now.AddDays(1)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Reject_NullOrEmptyRejectedBy_ThrowsArgumentException(string? rejectedBy)
    {
        var agg = CreateValid();
        Should.Throw<ArgumentException>(() =>
            agg.Reject("Conclusion", rejectedBy!, Now.AddDays(1)));
    }

    [Fact]
    public void Reject_WhenAlreadyApproved_ThrowsInvalidOperation()
    {
        var agg = CreateApproved();
        Should.Throw<InvalidOperationException>(() =>
            agg.Reject("Reject approved", "Rejecter", Now.AddDays(2)));
    }

    [Fact]
    public void Reject_WhenAlreadyRejected_ThrowsInvalidOperation()
    {
        var agg = CreateRejected();
        Should.Throw<InvalidOperationException>(() =>
            agg.Reject("Again", "Rejecter", Now.AddDays(2)));
    }

    // ================================================================
    // ScheduleReview state guards
    // ================================================================

    [Fact]
    public void ScheduleReview_WhenRequiresReview_ThrowsInvalidOperation()
    {
        var agg = CreateValid();
        Should.Throw<InvalidOperationException>(() =>
            agg.ScheduleReview(Now.AddYears(1), "Scheduler", Now));
    }

    [Fact]
    public void ScheduleReview_WhenRejected_ThrowsInvalidOperation()
    {
        var agg = CreateRejected();
        Should.Throw<InvalidOperationException>(() =>
            agg.ScheduleReview(Now.AddYears(1), "Scheduler", Now));
    }

    [Fact]
    public void ScheduleReview_WhenApproved_Succeeds()
    {
        var agg = CreateApproved();
        Should.NotThrow(() =>
            agg.ScheduleReview(Now.AddYears(1), "Scheduler", Now.AddDays(2)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ScheduleReview_NullOrEmptyScheduledBy_Throws(string? scheduledBy)
    {
        var agg = CreateApproved();
        Should.Throw<ArgumentException>(() =>
            agg.ScheduleReview(Now.AddYears(1), scheduledBy!, Now.AddDays(2)));
    }
}

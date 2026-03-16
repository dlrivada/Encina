using Encina.Compliance.GDPR;
using Encina.Compliance.LawfulBasis.Aggregates;
using Encina.Compliance.LawfulBasis.Events;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.LawfulBasisModule.Aggregates;

/// <summary>
/// Unit tests for <see cref="LIAAggregate"/>.
/// </summary>
public class LIAAggregateTests
{
    private static readonly Guid DefaultId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly IReadOnlyList<string> DefaultAlternatives = ["Manual review", "Outsourced processing"];
    private static readonly IReadOnlyList<string> DefaultSafeguards = ["Encryption at rest", "Access controls", "Audit logging"];

    #region Create (Static Factory)

    [Fact]
    public void Create_WithValidParameters_SetsAllProperties()
    {
        // Act
        var aggregate = CreateDefaultLIA();

        // Assert
        aggregate.Id.Should().Be(DefaultId);
        aggregate.Reference.Should().Be("LIA-2024-FRAUD-001");
        aggregate.Name.Should().Be("Fraud Detection LIA");
        aggregate.Purpose.Should().Be("Fraud prevention processing");
        aggregate.LegitimateInterest.Should().Be("Preventing financial fraud");
        aggregate.Benefits.Should().Be("Protects customers and business from fraud losses");
        aggregate.ConsequencesIfNotProcessed.Should().Be("Increased fraud exposure");
        aggregate.NecessityJustification.Should().Be("No less intrusive alternative available");
        aggregate.AlternativesConsidered.Should().BeEquivalentTo(DefaultAlternatives);
        aggregate.DataMinimisationNotes.Should().Be("Only transaction metadata is processed");
        aggregate.NatureOfData.Should().Be("Financial transaction data");
        aggregate.ReasonableExpectations.Should().Be("Customers expect fraud protection");
        aggregate.ImpactAssessment.Should().Be("Minimal impact on privacy rights");
        aggregate.Safeguards.Should().BeEquivalentTo(DefaultSafeguards);
        aggregate.AssessedBy.Should().Be("DPO Jane Smith");
        aggregate.DPOInvolvement.Should().BeTrue();
        aggregate.Conditions.Should().Be("Review annually");
        aggregate.TenantId.Should().Be("tenant-1");
        aggregate.ModuleId.Should().Be("module-1");
    }

    [Fact]
    public void Create_SetsOutcomeToRequiresReview()
    {
        // Act
        var aggregate = CreateDefaultLIA();

        // Assert
        aggregate.Outcome.Should().Be(LIAOutcome.RequiresReview);
        aggregate.Conclusion.Should().BeNull();
        aggregate.NextReviewAtUtc.Should().BeNull();
    }

    [Fact]
    public void Create_RaisesLIACreatedEvent()
    {
        // Act
        var aggregate = CreateDefaultLIA();

        // Assert
        aggregate.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<LIACreated>()
            .Which.Should().BeEquivalentTo(new
            {
                LIAId = DefaultId,
                Reference = "LIA-2024-FRAUD-001",
                Name = "Fraud Detection LIA",
                Purpose = "Fraud prevention processing",
                AssessedBy = "DPO Jane Smith",
                DPOInvolvement = true,
                TenantId = (string?)"tenant-1",
                ModuleId = (string?)"module-1",
            });
        aggregate.Version.Should().Be(1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhiteSpaceReference_ThrowsArgumentException(string? reference)
    {
        // Act
        var act = () => LIAAggregate.Create(
            DefaultId, reference!, "Name", "Purpose",
            "Interest", "Benefits", "Consequences",
            "Necessity", DefaultAlternatives, "Minimisation",
            "Nature", "Expectations", "Impact",
            DefaultSafeguards, "Assessor", true, Now);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("reference");
    }

    [Fact]
    public void Create_WithNullAlternatives_ThrowsArgumentNullException()
    {
        // Act
        var act = () => LIAAggregate.Create(
            DefaultId, "LIA-001", "Name", "Purpose",
            "Interest", "Benefits", "Consequences",
            "Necessity", null!, "Minimisation",
            "Nature", "Expectations", "Impact",
            DefaultSafeguards, "Assessor", true, Now);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("alternativesConsidered");
    }

    [Fact]
    public void Create_WithNullSafeguards_ThrowsArgumentNullException()
    {
        // Act
        var act = () => LIAAggregate.Create(
            DefaultId, "LIA-001", "Name", "Purpose",
            "Interest", "Benefits", "Consequences",
            "Necessity", DefaultAlternatives, "Minimisation",
            "Nature", "Expectations", "Impact",
            null!, "Assessor", true, Now);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("safeguards");
    }

    #endregion

    #region Approve

    [Fact]
    public void Approve_WhenRequiresReview_SetsOutcomeToApproved()
    {
        // Arrange
        var aggregate = CreateDefaultLIA();

        // Act
        aggregate.Approve("Legitimate interest outweighs rights", "DPO Jane Smith", Now.AddDays(7));

        // Assert
        aggregate.Outcome.Should().Be(LIAOutcome.Approved);
        aggregate.Conclusion.Should().Be("Legitimate interest outweighs rights");
    }

    [Fact]
    public void Approve_WhenAlreadyApproved_ThrowsInvalidOperation()
    {
        // Arrange
        var aggregate = CreateApprovedLIA();

        // Act
        var act = () => aggregate.Approve("Second approval", "Admin", Now.AddDays(14));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Approved*");
    }

    [Fact]
    public void Approve_WhenRejected_ThrowsInvalidOperation()
    {
        // Arrange
        var aggregate = CreateRejectedLIA();

        // Act
        var act = () => aggregate.Approve("Trying to approve rejected", "Admin", Now.AddDays(14));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Rejected*");
    }

    [Fact]
    public void Approve_RaisesLIAApprovedEvent()
    {
        // Arrange
        var aggregate = CreateDefaultLIA();

        // Act
        aggregate.Approve("Assessment passed", "DPO Jane Smith", Now.AddDays(7));

        // Assert
        aggregate.UncommittedEvents.Should().HaveCount(2);
        var approvedEvent = aggregate.UncommittedEvents[^1].Should().BeOfType<LIAApproved>().Subject;
        approvedEvent.LIAId.Should().Be(DefaultId);
        approvedEvent.Conclusion.Should().Be("Assessment passed");
        approvedEvent.ApprovedBy.Should().Be("DPO Jane Smith");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Approve_WithNullOrWhiteSpaceConclusion_ThrowsArgumentException(string? conclusion)
    {
        // Arrange
        var aggregate = CreateDefaultLIA();

        // Act
        var act = () => aggregate.Approve(conclusion!, "Approver", Now.AddDays(7));

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("conclusion");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Approve_WithNullOrWhiteSpaceApprovedBy_ThrowsArgumentException(string? approvedBy)
    {
        // Arrange
        var aggregate = CreateDefaultLIA();

        // Act
        var act = () => aggregate.Approve("Conclusion", approvedBy!, Now.AddDays(7));

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("approvedBy");
    }

    #endregion

    #region Reject

    [Fact]
    public void Reject_WhenRequiresReview_SetsOutcomeToRejected()
    {
        // Arrange
        var aggregate = CreateDefaultLIA();

        // Act
        aggregate.Reject("Rights override interest", "DPO Jane Smith", Now.AddDays(7));

        // Assert
        aggregate.Outcome.Should().Be(LIAOutcome.Rejected);
        aggregate.Conclusion.Should().Be("Rights override interest");
    }

    [Fact]
    public void Reject_WhenAlreadyRejected_ThrowsInvalidOperation()
    {
        // Arrange
        var aggregate = CreateRejectedLIA();

        // Act
        var act = () => aggregate.Reject("Second rejection", "Admin", Now.AddDays(14));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Rejected*");
    }

    [Fact]
    public void Reject_WhenApproved_ThrowsInvalidOperation()
    {
        // Arrange
        var aggregate = CreateApprovedLIA();

        // Act
        var act = () => aggregate.Reject("Trying to reject approved", "Admin", Now.AddDays(14));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Approved*");
    }

    [Fact]
    public void Reject_RaisesLIARejectedEvent()
    {
        // Arrange
        var aggregate = CreateDefaultLIA();

        // Act
        aggregate.Reject("Balancing test failed", "DPO Jane Smith", Now.AddDays(7));

        // Assert
        aggregate.UncommittedEvents.Should().HaveCount(2);
        var rejectedEvent = aggregate.UncommittedEvents[^1].Should().BeOfType<LIARejected>().Subject;
        rejectedEvent.LIAId.Should().Be(DefaultId);
        rejectedEvent.Conclusion.Should().Be("Balancing test failed");
        rejectedEvent.RejectedBy.Should().Be("DPO Jane Smith");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Reject_WithNullOrWhiteSpaceConclusion_ThrowsArgumentException(string? conclusion)
    {
        // Arrange
        var aggregate = CreateDefaultLIA();

        // Act
        var act = () => aggregate.Reject(conclusion!, "Rejector", Now.AddDays(7));

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("conclusion");
    }

    #endregion

    #region ScheduleReview

    [Fact]
    public void ScheduleReview_WhenApproved_SetsNextReviewDate()
    {
        // Arrange
        var aggregate = CreateApprovedLIA();
        var nextReview = Now.AddDays(365);

        // Act
        aggregate.ScheduleReview(nextReview, "Governance Team", Now.AddDays(10));

        // Assert
        aggregate.NextReviewAtUtc.Should().Be(nextReview);
    }

    [Fact]
    public void ScheduleReview_WhenRequiresReview_ThrowsInvalidOperation()
    {
        // Arrange
        var aggregate = CreateDefaultLIA();

        // Act
        var act = () => aggregate.ScheduleReview(Now.AddDays(365), "Admin", Now.AddDays(1));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*RequiresReview*");
    }

    [Fact]
    public void ScheduleReview_WhenRejected_ThrowsInvalidOperation()
    {
        // Arrange
        var aggregate = CreateRejectedLIA();

        // Act
        var act = () => aggregate.ScheduleReview(Now.AddDays(365), "Admin", Now.AddDays(1));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Rejected*");
    }

    [Fact]
    public void ScheduleReview_RaisesLIAReviewScheduledEvent()
    {
        // Arrange
        var aggregate = CreateApprovedLIA();
        var nextReview = Now.AddDays(365);

        // Act
        aggregate.ScheduleReview(nextReview, "Governance Team", Now.AddDays(10));

        // Assert
        aggregate.UncommittedEvents.Should().HaveCount(3);
        var scheduledEvent = aggregate.UncommittedEvents[^1].Should().BeOfType<LIAReviewScheduled>().Subject;
        scheduledEvent.LIAId.Should().Be(DefaultId);
        scheduledEvent.NextReviewAtUtc.Should().Be(nextReview);
        scheduledEvent.ScheduledBy.Should().Be("Governance Team");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ScheduleReview_WithNullOrWhiteSpaceScheduledBy_ThrowsArgumentException(string? scheduledBy)
    {
        // Arrange
        var aggregate = CreateApprovedLIA();

        // Act
        var act = () => aggregate.ScheduleReview(Now.AddDays(365), scheduledBy!, Now.AddDays(10));

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("scheduledBy");
    }

    #endregion

    #region Version Tracking

    [Fact]
    public void Version_IncreasesWithEachEvent()
    {
        // Create
        var aggregate = CreateDefaultLIA();
        aggregate.Version.Should().Be(1);

        // Approve
        aggregate.Approve("Approved", "DPO", Now.AddDays(7));
        aggregate.Version.Should().Be(2);

        // Schedule review
        aggregate.ScheduleReview(Now.AddDays(365), "Governance", Now.AddDays(10));
        aggregate.Version.Should().Be(3);
    }

    #endregion

    #region Helper Methods

    private static LIAAggregate CreateDefaultLIA()
    {
        return LIAAggregate.Create(
            DefaultId,
            "LIA-2024-FRAUD-001",
            "Fraud Detection LIA",
            "Fraud prevention processing",
            "Preventing financial fraud",
            "Protects customers and business from fraud losses",
            "Increased fraud exposure",
            "No less intrusive alternative available",
            DefaultAlternatives,
            "Only transaction metadata is processed",
            "Financial transaction data",
            "Customers expect fraud protection",
            "Minimal impact on privacy rights",
            DefaultSafeguards,
            "DPO Jane Smith",
            true,
            Now,
            "Review annually",
            "tenant-1",
            "module-1");
    }

    private static LIAAggregate CreateApprovedLIA()
    {
        var aggregate = CreateDefaultLIA();
        aggregate.Approve("Legitimate interest outweighs rights", "DPO Jane Smith", Now.AddDays(7));
        return aggregate;
    }

    private static LIAAggregate CreateRejectedLIA()
    {
        var aggregate = CreateDefaultLIA();
        aggregate.Reject("Rights override interest", "DPO Jane Smith", Now.AddDays(7));
        return aggregate;
    }

    #endregion
}

using Encina.Compliance.GDPR;
using Encina.Compliance.LawfulBasis.Aggregates;
using Encina.Compliance.LawfulBasis.Events;
using Shouldly;

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
        aggregate.Id.ShouldBe(DefaultId);
        aggregate.Reference.ShouldBe("LIA-2024-FRAUD-001");
        aggregate.Name.ShouldBe("Fraud Detection LIA");
        aggregate.Purpose.ShouldBe("Fraud prevention processing");
        aggregate.LegitimateInterest.ShouldBe("Preventing financial fraud");
        aggregate.Benefits.ShouldBe("Protects customers and business from fraud losses");
        aggregate.ConsequencesIfNotProcessed.ShouldBe("Increased fraud exposure");
        aggregate.NecessityJustification.ShouldBe("No less intrusive alternative available");
        aggregate.AlternativesConsidered.ShouldBe(DefaultAlternatives);
        aggregate.DataMinimisationNotes.ShouldBe("Only transaction metadata is processed");
        aggregate.NatureOfData.ShouldBe("Financial transaction data");
        aggregate.ReasonableExpectations.ShouldBe("Customers expect fraud protection");
        aggregate.ImpactAssessment.ShouldBe("Minimal impact on privacy rights");
        aggregate.Safeguards.ShouldBe(DefaultSafeguards);
        aggregate.AssessedBy.ShouldBe("DPO Jane Smith");
        aggregate.DPOInvolvement.ShouldBeTrue();
        aggregate.Conditions.ShouldBe("Review annually");
        aggregate.TenantId.ShouldBe("tenant-1");
        aggregate.ModuleId.ShouldBe("module-1");
    }

    [Fact]
    public void Create_SetsOutcomeToRequiresReview()
    {
        // Act
        var aggregate = CreateDefaultLIA();

        // Assert
        aggregate.Outcome.ShouldBe(LIAOutcome.RequiresReview);
        aggregate.Conclusion.ShouldBeNull();
        aggregate.NextReviewAtUtc.ShouldBeNull();
    }

    [Fact]
    public void Create_RaisesLIACreatedEvent()
    {
        // Act
        var aggregate = CreateDefaultLIA();

        // Assert
        aggregate.UncommittedEvents.ShouldHaveSingleItem().ShouldBeOfType<LIACreated>().ShouldSatisfyAllConditions(
            e => e.LIAId.ShouldBe(DefaultId),
            e => e.Reference.ShouldBe("LIA-2024-FRAUD-001"),
            e => e.Name.ShouldBe("Fraud Detection LIA"),
            e => e.Purpose.ShouldBe("Fraud prevention processing"),
            e => e.AssessedBy.ShouldBe("DPO Jane Smith"),
            e => e.DPOInvolvement.ShouldBeTrue(),
            e => e.TenantId.ShouldBe("tenant-1"),
            e => e.ModuleId.ShouldBe("module-1"));
        aggregate.Version.ShouldBe(1);
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
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reference");
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
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("alternativesConsidered");
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
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("safeguards");
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
        aggregate.Outcome.ShouldBe(LIAOutcome.Approved);
        aggregate.Conclusion.ShouldBe("Legitimate interest outweighs rights");
    }

    [Fact]
    public void Approve_WhenAlreadyApproved_ThrowsInvalidOperation()
    {
        // Arrange
        var aggregate = CreateApprovedLIA();

        // Act
        var act = () => aggregate.Approve("Second approval", "Admin", Now.AddDays(14));

        // Assert
        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("Approved");
    }

    [Fact]
    public void Approve_WhenRejected_ThrowsInvalidOperation()
    {
        // Arrange
        var aggregate = CreateRejectedLIA();

        // Act
        var act = () => aggregate.Approve("Trying to approve rejected", "Admin", Now.AddDays(14));

        // Assert
        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("Rejected");
    }

    [Fact]
    public void Approve_RaisesLIAApprovedEvent()
    {
        // Arrange
        var aggregate = CreateDefaultLIA();

        // Act
        aggregate.Approve("Assessment passed", "DPO Jane Smith", Now.AddDays(7));

        // Assert
        aggregate.UncommittedEvents.Count.ShouldBe(2);
        var approvedEvent = aggregate.UncommittedEvents[^1].ShouldBeOfType<LIAApproved>();
        approvedEvent.LIAId.ShouldBe(DefaultId);
        approvedEvent.Conclusion.ShouldBe("Assessment passed");
        approvedEvent.ApprovedBy.ShouldBe("DPO Jane Smith");
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
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("conclusion");
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
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("approvedBy");
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
        aggregate.Outcome.ShouldBe(LIAOutcome.Rejected);
        aggregate.Conclusion.ShouldBe("Rights override interest");
    }

    [Fact]
    public void Reject_WhenAlreadyRejected_ThrowsInvalidOperation()
    {
        // Arrange
        var aggregate = CreateRejectedLIA();

        // Act
        var act = () => aggregate.Reject("Second rejection", "Admin", Now.AddDays(14));

        // Assert
        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("Rejected");
    }

    [Fact]
    public void Reject_WhenApproved_ThrowsInvalidOperation()
    {
        // Arrange
        var aggregate = CreateApprovedLIA();

        // Act
        var act = () => aggregate.Reject("Trying to reject approved", "Admin", Now.AddDays(14));

        // Assert
        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("Approved");
    }

    [Fact]
    public void Reject_RaisesLIARejectedEvent()
    {
        // Arrange
        var aggregate = CreateDefaultLIA();

        // Act
        aggregate.Reject("Balancing test failed", "DPO Jane Smith", Now.AddDays(7));

        // Assert
        aggregate.UncommittedEvents.Count.ShouldBe(2);
        var rejectedEvent = aggregate.UncommittedEvents[^1].ShouldBeOfType<LIARejected>();
        rejectedEvent.LIAId.ShouldBe(DefaultId);
        rejectedEvent.Conclusion.ShouldBe("Balancing test failed");
        rejectedEvent.RejectedBy.ShouldBe("DPO Jane Smith");
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
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("conclusion");
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
        aggregate.NextReviewAtUtc.ShouldBe(nextReview);
    }

    [Fact]
    public void ScheduleReview_WhenRequiresReview_ThrowsInvalidOperation()
    {
        // Arrange
        var aggregate = CreateDefaultLIA();

        // Act
        var act = () => aggregate.ScheduleReview(Now.AddDays(365), "Admin", Now.AddDays(1));

        // Assert
        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("RequiresReview");
    }

    [Fact]
    public void ScheduleReview_WhenRejected_ThrowsInvalidOperation()
    {
        // Arrange
        var aggregate = CreateRejectedLIA();

        // Act
        var act = () => aggregate.ScheduleReview(Now.AddDays(365), "Admin", Now.AddDays(1));

        // Assert
        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("Rejected");
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
        aggregate.UncommittedEvents.Count.ShouldBe(3);
        var scheduledEvent = aggregate.UncommittedEvents[^1].ShouldBeOfType<LIAReviewScheduled>();
        scheduledEvent.LIAId.ShouldBe(DefaultId);
        scheduledEvent.NextReviewAtUtc.ShouldBe(nextReview);
        scheduledEvent.ScheduledBy.ShouldBe("Governance Team");
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
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("scheduledBy");
    }

    #endregion

    #region Version Tracking

    [Fact]
    public void Version_IncreasesWithEachEvent()
    {
        // Create
        var aggregate = CreateDefaultLIA();
        aggregate.Version.ShouldBe(1);

        // Approve
        aggregate.Approve("Approved", "DPO", Now.AddDays(7));
        aggregate.Version.ShouldBe(2);

        // Schedule review
        aggregate.ScheduleReview(Now.AddDays(365), "Governance", Now.AddDays(10));
        aggregate.Version.ShouldBe(3);
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

using Encina.Compliance.DPIA.Aggregates;
using Encina.Compliance.DPIA.Model;

namespace Encina.GuardTests.Compliance.DPIA;

/// <summary>
/// Guard tests for <see cref="DPIAAggregate"/> to verify null and invalid parameter handling
/// across all factory and instance methods.
/// </summary>
public class DPIAAggregateGuardTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 16, 12, 0, 0, TimeSpan.Zero);

    #region Create Guards — requestTypeName

    [Fact]
    public void Create_NullRequestTypeName_ThrowsArgumentException()
    {
        var act = () => DPIAAggregate.Create(Guid.NewGuid(), null!, Now);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("requestTypeName");
    }

    [Fact]
    public void Create_EmptyRequestTypeName_ThrowsArgumentException()
    {
        var act = () => DPIAAggregate.Create(Guid.NewGuid(), "", Now);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("requestTypeName");
    }

    [Fact]
    public void Create_WhitespaceRequestTypeName_ThrowsArgumentException()
    {
        var act = () => DPIAAggregate.Create(Guid.NewGuid(), "   ", Now);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("requestTypeName");
    }

    #endregion

    #region Evaluate Guards — result

    [Fact]
    public void Evaluate_NullResult_ThrowsArgumentNullException()
    {
        var aggregate = CreateDraftAggregate();

        var act = () => aggregate.Evaluate(null!, Now);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("result");
    }

    #endregion

    #region RequestDPOConsultation Guards — dpoName

    [Fact]
    public void RequestDPOConsultation_NullDPOName_ThrowsArgumentException()
    {
        var aggregate = CreateInReviewAggregate();

        var act = () => aggregate.RequestDPOConsultation(Guid.NewGuid(), null!, "dpo@example.com", Now);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("dpoName");
    }

    [Fact]
    public void RequestDPOConsultation_EmptyDPOName_ThrowsArgumentException()
    {
        var aggregate = CreateInReviewAggregate();

        var act = () => aggregate.RequestDPOConsultation(Guid.NewGuid(), "", "dpo@example.com", Now);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("dpoName");
    }

    [Fact]
    public void RequestDPOConsultation_WhitespaceDPOName_ThrowsArgumentException()
    {
        var aggregate = CreateInReviewAggregate();

        var act = () => aggregate.RequestDPOConsultation(Guid.NewGuid(), "   ", "dpo@example.com", Now);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("dpoName");
    }

    #endregion

    #region RequestDPOConsultation Guards — dpoEmail

    [Fact]
    public void RequestDPOConsultation_NullDPOEmail_ThrowsArgumentException()
    {
        var aggregate = CreateInReviewAggregate();

        var act = () => aggregate.RequestDPOConsultation(Guid.NewGuid(), "Jane DPO", null!, Now);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("dpoEmail");
    }

    [Fact]
    public void RequestDPOConsultation_EmptyDPOEmail_ThrowsArgumentException()
    {
        var aggregate = CreateInReviewAggregate();

        var act = () => aggregate.RequestDPOConsultation(Guid.NewGuid(), "Jane DPO", "", Now);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("dpoEmail");
    }

    [Fact]
    public void RequestDPOConsultation_WhitespaceDPOEmail_ThrowsArgumentException()
    {
        var aggregate = CreateInReviewAggregate();

        var act = () => aggregate.RequestDPOConsultation(Guid.NewGuid(), "Jane DPO", "   ", Now);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("dpoEmail");
    }

    #endregion

    #region Approve Guards — approvedBy

    [Fact]
    public void Approve_NullApprovedBy_ThrowsArgumentException()
    {
        var aggregate = CreateInReviewAggregate();

        var act = () => aggregate.Approve(null!, Now);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("approvedBy");
    }

    [Fact]
    public void Approve_EmptyApprovedBy_ThrowsArgumentException()
    {
        var aggregate = CreateInReviewAggregate();

        var act = () => aggregate.Approve("", Now);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("approvedBy");
    }

    [Fact]
    public void Approve_WhitespaceApprovedBy_ThrowsArgumentException()
    {
        var aggregate = CreateInReviewAggregate();

        var act = () => aggregate.Approve("   ", Now);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("approvedBy");
    }

    #endregion

    #region Reject Guards — rejectedBy

    [Fact]
    public void Reject_NullRejectedBy_ThrowsArgumentException()
    {
        var aggregate = CreateInReviewAggregate();

        var act = () => aggregate.Reject(null!, "Risk too high", Now);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("rejectedBy");
    }

    [Fact]
    public void Reject_EmptyRejectedBy_ThrowsArgumentException()
    {
        var aggregate = CreateInReviewAggregate();

        var act = () => aggregate.Reject("", "Risk too high", Now);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("rejectedBy");
    }

    [Fact]
    public void Reject_WhitespaceRejectedBy_ThrowsArgumentException()
    {
        var aggregate = CreateInReviewAggregate();

        var act = () => aggregate.Reject("   ", "Risk too high", Now);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("rejectedBy");
    }

    #endregion

    #region Reject Guards — reason

    [Fact]
    public void Reject_NullReason_ThrowsArgumentException()
    {
        var aggregate = CreateInReviewAggregate();

        var act = () => aggregate.Reject("admin", null!, Now);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reason");
    }

    [Fact]
    public void Reject_EmptyReason_ThrowsArgumentException()
    {
        var aggregate = CreateInReviewAggregate();

        var act = () => aggregate.Reject("admin", "", Now);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reason");
    }

    [Fact]
    public void Reject_WhitespaceReason_ThrowsArgumentException()
    {
        var aggregate = CreateInReviewAggregate();

        var act = () => aggregate.Reject("admin", "   ", Now);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reason");
    }

    #endregion

    #region RequestRevision Guards — requestedBy

    [Fact]
    public void RequestRevision_NullRequestedBy_ThrowsArgumentException()
    {
        var aggregate = CreateInReviewAggregate();

        var act = () => aggregate.RequestRevision(null!, "Needs more detail", Now);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("requestedBy");
    }

    [Fact]
    public void RequestRevision_EmptyRequestedBy_ThrowsArgumentException()
    {
        var aggregate = CreateInReviewAggregate();

        var act = () => aggregate.RequestRevision("", "Needs more detail", Now);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("requestedBy");
    }

    [Fact]
    public void RequestRevision_WhitespaceRequestedBy_ThrowsArgumentException()
    {
        var aggregate = CreateInReviewAggregate();

        var act = () => aggregate.RequestRevision("   ", "Needs more detail", Now);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("requestedBy");
    }

    #endregion

    #region RequestRevision Guards — reason

    [Fact]
    public void RequestRevision_NullReason_ThrowsArgumentException()
    {
        var aggregate = CreateInReviewAggregate();

        var act = () => aggregate.RequestRevision("reviewer", null!, Now);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reason");
    }

    [Fact]
    public void RequestRevision_EmptyReason_ThrowsArgumentException()
    {
        var aggregate = CreateInReviewAggregate();

        var act = () => aggregate.RequestRevision("reviewer", "", Now);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reason");
    }

    [Fact]
    public void RequestRevision_WhitespaceReason_ThrowsArgumentException()
    {
        var aggregate = CreateInReviewAggregate();

        var act = () => aggregate.RequestRevision("reviewer", "   ", Now);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reason");
    }

    #endregion

    #region Helpers

    private static DPIAAggregate CreateDraftAggregate()
    {
        return DPIAAggregate.Create(Guid.NewGuid(), "MyApp.Commands.ProcessData", Now);
    }

    private static DPIAAggregate CreateInReviewAggregate()
    {
        var aggregate = CreateDraftAggregate();
        aggregate.Evaluate(CreateValidResult(), Now);
        return aggregate;
    }

    private static DPIAResult CreateValidResult() => new()
    {
        OverallRisk = RiskLevel.Medium,
        IdentifiedRisks = [],
        ProposedMitigations = [],
        RequiresPriorConsultation = false,
        AssessedAtUtc = Now,
    };

    #endregion
}

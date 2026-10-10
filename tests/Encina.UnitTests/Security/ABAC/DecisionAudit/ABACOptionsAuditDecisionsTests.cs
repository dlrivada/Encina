using Encina.Security.ABAC;
using Encina.Security.ABAC.DecisionAudit;

using Shouldly;

namespace Encina.UnitTests.Security.ABAC.DecisionAudit;

/// <summary>
/// The <c>ABACOptions.AuditDecisions</c> builder (#751 Phase 4).
/// </summary>
public sealed class ABACOptionsAuditDecisionsTests
{
    [Fact]
    public void AuditDecisions_WithoutAnAction_EnablesTheAudit()
    {
        var options = new ABACOptions();

        options.AuditDecisions().ShouldBeSameAs(options);

        options.DecisionAudit.Enabled.ShouldBeTrue();
    }

    [Fact]
    public void AuditDecisions_WithAnAction_EnablesAndConfiguresTheAudit()
    {
        var options = new ABACOptions();

        options.AuditDecisions(audit =>
        {
            audit.Outcomes = ABACDecisionAuditOutcomes.Denied;
            audit.WriteTimeout = TimeSpan.FromSeconds(2);
        });

        options.DecisionAudit.Enabled.ShouldBeTrue();
        options.DecisionAudit.Outcomes.ShouldBe(ABACDecisionAuditOutcomes.Denied);
        options.DecisionAudit.WriteTimeout.ShouldBe(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void AuditDecisions_ActionTurningTheAuditOff_Wins()
    {
        var options = new ABACOptions();

        options.AuditDecisions(audit => audit.Enabled = false);

        options.DecisionAudit.Enabled.ShouldBeFalse();
    }
}

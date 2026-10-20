using Encina.Security.ABAC;
using Encina.Security.ABAC.DecisionAudit;

using Shouldly;

namespace Encina.UnitTests.Security.ABAC.DecisionAudit;

/// <summary>
/// The decision audit rules of <see cref="ABACOptionsValidator"/> beyond the timeout and trace bounds
/// (#751 Phase 4): enum values and the audit-with-disabled-enforcement combination.
/// </summary>
public sealed class ABACOptionsValidatorDecisionAuditTests
{
    [Fact]
    public void Validate_UndefinedFailureMode_IsRejected()
    {
        var options = new ABACOptions();
        options.DecisionAudit.FailureMode = (ABACDecisionAuditFailureMode)42;

        var result = new ABACOptionsValidator().Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("FailureMode");
    }

    [Fact]
    public void Validate_OutcomesWithUnknownBits_IsRejected()
    {
        var options = new ABACOptions();
        options.DecisionAudit.Outcomes = (ABACDecisionAuditOutcomes)64;

        var result = new ABACOptionsValidator().Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("Outcomes");
    }

    [Theory]
    [InlineData(ABACDecisionAuditOutcomes.None)]
    [InlineData(ABACDecisionAuditOutcomes.Denied)]
    [InlineData(ABACDecisionAuditOutcomes.Granted | ABACDecisionAuditOutcomes.NotEnforced)]
    [InlineData(ABACDecisionAuditOutcomes.All)]
    public void Validate_EveryCombinationOfTheDefinedFlags_IsAccepted(ABACDecisionAuditOutcomes outcomes)
    {
        var options = new ABACOptions();
        options.DecisionAudit.Outcomes = outcomes;

        new ABACOptionsValidator().Validate(null, options).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_InfiniteTimeout_IsRejected()
    {
        var options = new ABACOptions();
        options.DecisionAudit.WriteTimeout = Timeout.InfiniteTimeSpan;

        new ABACOptionsValidator().Validate(null, options).Failed.ShouldBeTrue();
    }

    [Fact]
    public void Validate_LargestTimeoutATokenSourceAccepts_IsAccepted()
    {
        var options = new ABACOptions();
        options.DecisionAudit.WriteTimeout = TimeSpan.FromMilliseconds(int.MaxValue);

        new ABACOptionsValidator().Validate(null, options).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_AuditEnabledWithDisabledEnforcement_IsRejected()
    {
        var options = new ABACOptions { EnforcementMode = ABACEnforcementMode.Disabled };
        options.AuditDecisions();

        var result = new ABACOptionsValidator().Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("EnforcementMode.Disabled");
    }

    [Theory]
    [InlineData(ABACEnforcementMode.Block)]
    [InlineData(ABACEnforcementMode.Warn)]
    public void Validate_AuditEnabledWithAnEnforcingMode_IsAccepted(ABACEnforcementMode mode)
    {
        var options = new ABACOptions { EnforcementMode = mode };
        options.AuditDecisions();

        new ABACOptionsValidator().Validate(null, options).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_AuditDisabledWithDisabledEnforcement_IsAccepted()
    {
        var options = new ABACOptions { EnforcementMode = ABACEnforcementMode.Disabled };

        new ABACOptionsValidator().Validate(null, options).Succeeded.ShouldBeTrue();
    }
}

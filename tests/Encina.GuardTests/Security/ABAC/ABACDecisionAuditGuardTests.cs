using Encina.Security.ABAC;
using Encina.Security.ABAC.DecisionAudit;
using Encina.Security.ABAC.Evaluation;

using Shouldly;

namespace Encina.GuardTests.Security.ABAC;

/// <summary>
/// Guard tests for the public and internal methods with parameters added by #751 Phase 1.
/// </summary>
public sealed class ABACDecisionAuditGuardTests
{
    [Fact]
    public void DecisionAuditFailed_NullRequestType_Throws() =>
        Should.Throw<ArgumentNullException>(() => ABACErrors.DecisionAuditFailed(null!, "code"))
            .ParamName.ShouldBe("requestType");

    [Fact]
    public void InvalidDecisionAuditQuery_NullReason_Throws() =>
        Should.Throw<ArgumentNullException>(() => ABACErrors.InvalidDecisionAuditQuery(null!))
            .ParamName.ShouldBe("reason");

    [Fact]
    public void DecisionAuditFactories_WithValidArguments_BuildErrorsWithTheirCodes()
    {
        ABACErrors.DecisionAuditFailed(typeof(string), null).GetCode().IfNone("").ShouldBe(ABACErrors.DecisionAuditFailedCode);
        ABACErrors.InvalidDecisionAuditQuery("pageSize").GetCode().IfNone("").ShouldBe(ABACErrors.InvalidDecisionAuditQueryCode);
        ABACErrors.DecisionAuditStoreUnavailable().GetCode().IfNone("").ShouldBe(ABACErrors.DecisionAuditStoreUnavailableCode);
        ABACErrors.DecisionAuditTenantRequired().GetCode().IfNone("").ShouldBe(ABACErrors.DecisionAuditTenantRequiredCode);
        ABACErrors.DecisionAuditTenantMismatch().GetCode().IfNone("").ShouldBe(ABACErrors.DecisionAuditTenantMismatchCode);
    }

    [Fact]
    public void ResolveDecisiveRuleId_NullNodes_Throws() =>
        Should.Throw<ArgumentNullException>(() => PolicyEvaluationTraceResolver.ResolveDecisiveRuleId(null!, Effect.Deny))
            .ParamName.ShouldBe("nodes");

    [Fact]
    public void DecisionRecord_WithOnlyTheRequiredMembers_BuildsWithNoSubject()
    {
        var record = new ABACDecisionRecord
        {
            DecisionId = Guid.NewGuid(),
            IdentityKind = IdentityKind.Anonymous,
            RequestType = "R",
            EnforcedOutcome = ABACEnforcedOutcome.Denied,
            ReasonCode = EncinaErrorCodes.AuthorizationUnauthenticated,
            EnforcementMode = ABACEnforcementMode.Block,
            StartedAtUtc = DateTimeOffset.UnixEpoch,
            CompletedAtUtc = DateTimeOffset.UnixEpoch
        };

        record.UserId.ShouldBeNull();
        record.RequestType.ShouldBe("R");
    }

    [Fact]
    public void DecisionRecord_WithEveryOptionalMember_KeepsEachValue()
    {
        var record = new ABACDecisionRecord
        {
            DecisionId = Guid.NewGuid(),
            IdentityKind = IdentityKind.Service,
            RequestType = "R",
            EnforcedOutcome = ABACEnforcedOutcome.DeniedNotEnforced,
            ReasonCode = ABACErrors.AccessDeniedCode,
            EnforcementMode = ABACEnforcementMode.Warn,
            StartedAtUtc = DateTimeOffset.UnixEpoch,
            CompletedAtUtc = DateTimeOffset.UnixEpoch,
            UserId = "service:billing",
            TenantId = "t",
            CorrelationId = "c",
            ModuleId = "m",
            IpAddress = "::1",
            UserAgent = "ua",
            ResourceId = "res",
            Effect = Effect.Deny,
            PolicyId = "p",
            RuleId = "r",
            TraceTruncated = true,
            ObligationIds = ["o"],
            AdviceIds = ["a"]
        };

        (record.UserId, record.TenantId, record.CorrelationId, record.ModuleId).ShouldBe(("service:billing", "t", "c", "m"));
        (record.IpAddress, record.UserAgent, record.ResourceId).ShouldBe(("::1", "ua", "res"));
        (record.PolicyId, record.RuleId, record.Effect, record.TraceTruncated).ShouldBe(("p", "r", Effect.Deny, true));
        record.ObligationIds.ShouldBe(["o"]);
        record.AdviceIds.ShouldBe(["a"]);
    }
}

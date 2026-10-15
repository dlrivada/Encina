using Encina.Security.ABAC;
using Encina.Security.ABAC.DecisionAudit;
using Encina.Security.ABAC.Evaluation;
using Encina.Security.Audit;

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
    public void Query_WithNoFilters_PrintsOnlyThePaging()
    {
        var printed = new ABACDecisionAuditQuery().ToString();

        printed.ShouldContain("PageNumber = 1");
        printed.ShouldNotContain("UserId");
        printed.ShouldNotContain("TenantId");
    }

    [Fact]
    public void Query_WithNullFilters_IsAValidQueryThatPrintsOnlyThePaging()
    {
        var query = new ABACDecisionAuditQuery { UserId = null, TenantId = null, ResourceId = null, CorrelationId = null, PageSize = 5 };

        query.ToString().ShouldContain("PageSize = 5");
    }

    [Fact]
    public void Record_WithOnlyTheRequiredMembers_PrintsOnlyTheDecisionIdOutcomeAndReason()
    {
        var record = new ABACDecisionAuditRecord
        {
            DecisionId = Guid.Empty,
            CorrelationId = "corr-secret",
            RequestType = "SecretRequest",
            Outcome = AuditOutcome.Denied,
            Enforced = true,
            ReasonCode = "abac.access_denied",
            StartedAtUtc = DateTimeOffset.UnixEpoch,
            CompletedAtUtc = DateTimeOffset.UnixEpoch
        };

        var printed = record.ToString();

        printed.ShouldContain("abac.access_denied");
        printed.ShouldNotContain("secret", Case.Insensitive);
    }
}

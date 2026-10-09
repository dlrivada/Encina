#pragma warning disable CA2012 // Use ValueTasks correctly -- NSubstitute mock setup pattern

using Encina.Security.ABAC;
using Encina.Security.ABAC.Administration;
using Encina.Security.ABAC.CombiningAlgorithms;
using Encina.Security.ABAC.DecisionAudit;
using Encina.Security.ABAC.Evaluation;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.ContractTests.Security.ABAC;

/// <summary>
/// Contract tests for the evaluation trace (#751 Phase 1): turning the trace on never changes the
/// decision, and the new public types keep their shape.
/// </summary>
public sealed class ABACDecisionTraceContractTests
{
    private static XACMLPolicyDecisionPoint CreatePdp(params Policy[] policies)
    {
        var pap = Substitute.For<IPolicyAdministrationPoint>();
        pap.GetPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<PolicySet>>([]));
        pap.GetPoliciesAsync(null, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<Policy>>(policies));
        var registry = new DefaultFunctionRegistry();
        return new XACMLPolicyDecisionPoint(
            pap,
            new TargetEvaluator(registry),
            new ConditionEvaluator(registry),
            new CombiningAlgorithmFactory(),
            NullLogger<XACMLPolicyDecisionPoint>.Instance);
    }

    private static Policy MakePolicy(string id, Effect effect) => new()
    {
        Id = id,
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Rules = [new Rule { Id = id + "-rule", Effect = effect, Obligations = [], Advice = [] }],
        Obligations = [],
        Advice = [],
        VariableDefinitions = []
    };

    private static PolicyEvaluationContext Context(bool trace) => new()
    {
        SubjectAttributes = new Dictionary<string, AttributeBag>(),
        ResourceAttributes = new Dictionary<string, AttributeBag>(),
        EnvironmentAttributes = new Dictionary<string, AttributeBag>(),
        ActionAttributes = new Dictionary<string, AttributeBag>(),
        RequestType = typeof(object),
        IncludeEvaluationTrace = trace
    };

    [Theory]
    [InlineData(Effect.Permit, Effect.Permit)]
    [InlineData(Effect.Permit, Effect.Deny)]
    [InlineData(Effect.Deny, Effect.Deny)]
    [InlineData(Effect.NotApplicable, Effect.Permit)]
    public async Task Pdp_TraceOnOrOff_ProducesTheSameDecision(Effect first, Effect second)
    {
        var pdp = CreatePdp(MakePolicy("a", first), MakePolicy("b", second));

        var plain = await pdp.EvaluateAsync(Context(trace: false));
        var traced = await pdp.EvaluateAsync(Context(trace: true));

        traced.Effect.ShouldBe(plain.Effect);
        traced.PolicyId.ShouldBe(plain.PolicyId);
        traced.Obligations.ShouldBe(plain.Obligations);
        traced.Status?.StatusCode.ShouldBe(plain.Status?.StatusCode);
    }

    [Fact]
    public void PolicyEvaluationContext_TraceIsOffByDefault()
    {
        var context = Context(trace: false);

        context.IncludeEvaluationTrace.ShouldBeFalse();
        context.MaxTraceEntries.ShouldBe(64);
    }

    [Fact]
    public void PolicyDecision_WithoutATrace_HasAnEmptyTraceList()
    {
        var decision = new PolicyDecision
        {
            Effect = Effect.Permit,
            Obligations = [],
            Advice = [],
            EvaluationDuration = TimeSpan.Zero
        };

        decision.EvaluatedPolicies.ShouldBeEmpty();
        decision.RuleId.ShouldBeNull();
    }

    [Fact]
    public void TraceReason_KeepsItsFourValues() =>
        Enum.GetValues<PolicyTraceReason>().ShouldBe(
            [PolicyTraceReason.Evaluated, PolicyTraceReason.Disabled, PolicyTraceReason.TargetNotMatched, PolicyTraceReason.TargetIndeterminate]);

    [Fact]
    public void ResourceIdentity_IsAnInterfaceWithOneReadOnlyMember()
    {
        var members = typeof(IABACResourceIdentity).GetProperties();

        typeof(IABACResourceIdentity).IsInterface.ShouldBeTrue();
        members.Length.ShouldBe(1);
        members[0].Name.ShouldBe(nameof(IABACResourceIdentity.ResourceId));
        members[0].CanWrite.ShouldBeFalse();
    }

    [Fact]
    public void DecisionAuditErrorFamilies_AuthorizationDenialsAre403Codes_ServerFailuresAreNot()
    {
        new[] { ABACErrors.DecisionAuditTenantRequiredCode, ABACErrors.DecisionAuditTenantMismatchCode }
            .ShouldAllBe(code => code.StartsWith(EncinaErrorCodes.AuthorizationPrefix, StringComparison.Ordinal));
        new[] { ABACErrors.DecisionAuditFailedCode, ABACErrors.InvalidDecisionAuditQueryCode, ABACErrors.DecisionAuditStoreUnavailableCode }
            .ShouldAllBe(code => !code.StartsWith(EncinaErrorCodes.AuthorizationPrefix, StringComparison.Ordinal));
    }
}

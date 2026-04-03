using Encina.Security.ABAC;
using Encina.Security.ABAC.CombiningAlgorithms;
using Encina.Security.ABAC.Evaluation;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.ContractTests.Security.ABAC;

/// <summary>
/// Behavioral contract tests for <see cref="XACMLPolicyDecisionPoint"/>.
/// Exercises the real PDP evaluation pipeline (PAP retrieval, target evaluation,
/// rule evaluation, combining algorithms, obligation/advice filtering) to verify
/// the XACML 3.0 decision contracts.
/// </summary>
[Trait("Category", "Contract")]
[Trait("Feature", "ABAC")]
public sealed class XACMLPolicyDecisionPointContractTests
{
    private readonly DefaultFunctionRegistry _functionRegistry = new();
    private readonly CombiningAlgorithmFactory _algorithmFactory = new();

    // ── Helpers ──────────────────────────────────────────────────────

    private XACMLPolicyDecisionPoint CreatePdp(IPolicyAdministrationPoint pap)
    {
        var targetEvaluator = new TargetEvaluator(_functionRegistry);
        var conditionEvaluator = new ConditionEvaluator(_functionRegistry);
        var logger = NullLogger<XACMLPolicyDecisionPoint>.Instance;

        return new XACMLPolicyDecisionPoint(pap, targetEvaluator, conditionEvaluator, _algorithmFactory, logger);
    }

    private static PolicyEvaluationContext MakeContext(bool includeAdvice = true) => new()
    {
        SubjectAttributes = AttributeBag.Of(
            new AttributeValue { DataType = XACMLDataTypes.String, Value = "admin" }),
        ResourceAttributes = AttributeBag.Of(
            new AttributeValue { DataType = XACMLDataTypes.String, Value = "document" }),
        ActionAttributes = AttributeBag.Of(
            new AttributeValue { DataType = XACMLDataTypes.String, Value = "read" }),
        EnvironmentAttributes = AttributeBag.Empty,
        RequestType = typeof(object),
        IncludeAdvice = includeAdvice
    };

    private static PolicySet MakePolicySet(
        string id,
        CombiningAlgorithmId algorithm = CombiningAlgorithmId.DenyOverrides,
        bool isEnabled = true,
        IReadOnlyList<Policy>? policies = null,
        IReadOnlyList<Obligation>? obligations = null,
        IReadOnlyList<AdviceExpression>? advice = null) => new()
        {
            Id = id,
            IsEnabled = isEnabled,
            Algorithm = algorithm,
            Target = null, // null target = matches all
            Policies = policies ?? [],
            PolicySets = [],
            Obligations = obligations ?? [],
            Advice = advice ?? []
        };

    private static Policy MakePolicy(
        string id,
        CombiningAlgorithmId algorithm = CombiningAlgorithmId.PermitOverrides,
        bool isEnabled = true,
        IReadOnlyList<Rule>? rules = null,
        IReadOnlyList<Obligation>? obligations = null,
        IReadOnlyList<AdviceExpression>? advice = null) => new()
        {
            Id = id,
            IsEnabled = isEnabled,
            Algorithm = algorithm,
            Target = null, // null target = matches all
            Rules = rules ?? [],
            Obligations = obligations ?? [],
            Advice = advice ?? [],
            VariableDefinitions = []
        };

    private static Rule MakeRule(
        string id,
        Effect effect,
        IReadOnlyList<Obligation>? obligations = null,
        IReadOnlyList<AdviceExpression>? advice = null) => new()
        {
            Id = id,
            Effect = effect,
            Target = null,
            Condition = null, // null condition = unconditional
            Description = null,
            Obligations = obligations ?? [],
            Advice = advice ?? []
        };

    // ── Contract: Empty policy store yields NotApplicable ────────────

    [Fact]
    public async Task EvaluateAsync_NoPoliciesOrPolicySets_ShouldReturn_NotApplicable()
    {
        // Arrange
        var pap = Substitute.For<IPolicyAdministrationPoint>();
        pap.GetPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<PolicySet>>([]));
        pap.GetPoliciesAsync(null, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<Policy>>([]));

        var pdp = CreatePdp(pap);

        // Act
        var decision = await pdp.EvaluateAsync(MakeContext());

        // Assert
        decision.Effect.ShouldBe(Effect.NotApplicable,
            "PDP with no policies must return NotApplicable per XACML 3.0 §7.12");
    }

    // ── Contract: PAP error yields Indeterminate ─────────────────────

    [Fact]
    public async Task EvaluateAsync_PAPPolicySetRetrievalFails_ShouldReturn_Indeterminate()
    {
        // Arrange
        var pap = Substitute.For<IPolicyAdministrationPoint>();
        pap.GetPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, IReadOnlyList<PolicySet>>(
                EncinaError.New("Store unavailable")));

        var pdp = CreatePdp(pap);

        // Act
        var decision = await pdp.EvaluateAsync(MakeContext());

        // Assert
        decision.Effect.ShouldBe(Effect.Indeterminate,
            "PAP retrieval failure must produce Indeterminate");
        decision.Status.ShouldNotBeNull();
        decision.Status!.StatusCode.ShouldBe("processing-error");
    }

    // ── Contract: Permit rule produces Permit decision ───────────────

    [Fact]
    public async Task EvaluateAsync_SinglePermitRule_ShouldReturn_Permit()
    {
        // Arrange
        var rule = MakeRule("r1", Effect.Permit);
        var policy = MakePolicy("p1", rules: [rule]);
        var policySet = MakePolicySet("ps1", policies: [policy]);

        var pap = Substitute.For<IPolicyAdministrationPoint>();
        pap.GetPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<PolicySet>>([policySet]));
        pap.GetPoliciesAsync(null, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<Policy>>([]));

        var pdp = CreatePdp(pap);

        // Act
        var decision = await pdp.EvaluateAsync(MakeContext());

        // Assert
        decision.Effect.ShouldBe(Effect.Permit,
            "Single Permit rule with null target/condition must yield Permit");
        decision.EvaluationDuration.ShouldBeGreaterThan(TimeSpan.Zero);
    }

    // ── Contract: Deny rule produces Deny decision ──────────────────

    [Fact]
    public async Task EvaluateAsync_SingleDenyRule_ShouldReturn_Deny()
    {
        // Arrange
        var rule = MakeRule("r1", Effect.Deny);
        var policy = MakePolicy("p1",
            algorithm: CombiningAlgorithmId.DenyOverrides, rules: [rule]);
        var policySet = MakePolicySet("ps1", policies: [policy]);

        var pap = Substitute.For<IPolicyAdministrationPoint>();
        pap.GetPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<PolicySet>>([policySet]));
        pap.GetPoliciesAsync(null, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<Policy>>([]));

        var pdp = CreatePdp(pap);

        // Act
        var decision = await pdp.EvaluateAsync(MakeContext());

        // Assert
        decision.Effect.ShouldBe(Effect.Deny,
            "Single Deny rule with DenyOverrides algorithm must yield Deny");
    }

    // ── Contract: Disabled policy set is NotApplicable ───────────────

    [Fact]
    public async Task EvaluateAsync_DisabledPolicySet_ShouldReturn_NotApplicable()
    {
        // Arrange
        var rule = MakeRule("r1", Effect.Permit);
        var policy = MakePolicy("p1", rules: [rule]);
        var policySet = MakePolicySet("ps1", isEnabled: false, policies: [policy]);

        var pap = Substitute.For<IPolicyAdministrationPoint>();
        pap.GetPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<PolicySet>>([policySet]));
        pap.GetPoliciesAsync(null, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<Policy>>([]));

        var pdp = CreatePdp(pap);

        // Act
        var decision = await pdp.EvaluateAsync(MakeContext());

        // Assert
        decision.Effect.ShouldBe(Effect.NotApplicable,
            "Disabled policy sets must be skipped, yielding NotApplicable");
    }

    // ── Contract: Disabled policy is NotApplicable ───────────────────

    [Fact]
    public async Task EvaluateAsync_DisabledPolicy_ShouldReturn_NotApplicable()
    {
        // Arrange
        var rule = MakeRule("r1", Effect.Permit);
        var policy = MakePolicy("p1", isEnabled: false, rules: [rule]);
        var policySet = MakePolicySet("ps1", policies: [policy]);

        var pap = Substitute.For<IPolicyAdministrationPoint>();
        pap.GetPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<PolicySet>>([policySet]));
        pap.GetPoliciesAsync(null, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<Policy>>([]));

        var pdp = CreatePdp(pap);

        // Act
        var decision = await pdp.EvaluateAsync(MakeContext());

        // Assert
        decision.Effect.ShouldBe(Effect.NotApplicable,
            "Disabled policies must be skipped, yielding NotApplicable");
    }

    // ── Contract: Obligations are filtered by decision effect ────────

    [Fact]
    public async Task EvaluateAsync_ObligationsFilteredByEffect_ShouldIncludeOnlyMatching()
    {
        // Arrange
        var permitObligation = new Obligation
        {
            Id = "log-access",
            FulfillOn = FulfillOn.Permit,
            AttributeAssignments = []
        };
        var denyObligation = new Obligation
        {
            Id = "audit-deny",
            FulfillOn = FulfillOn.Deny,
            AttributeAssignments = []
        };

        var rule = MakeRule("r1", Effect.Permit, obligations: [permitObligation, denyObligation]);
        var policy = MakePolicy("p1",
            algorithm: CombiningAlgorithmId.PermitOverrides, rules: [rule]);
        var policySet = MakePolicySet("ps1", policies: [policy]);

        var pap = Substitute.For<IPolicyAdministrationPoint>();
        pap.GetPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<PolicySet>>([policySet]));
        pap.GetPoliciesAsync(null, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<Policy>>([]));

        var pdp = CreatePdp(pap);

        // Act
        var decision = await pdp.EvaluateAsync(MakeContext());

        // Assert
        decision.Effect.ShouldBe(Effect.Permit);
        decision.Obligations.ShouldContain(o => o.Id == "log-access",
            "Permit obligations must be included when decision is Permit");
        decision.Obligations.ShouldNotContain(o => o.Id == "audit-deny",
            "Deny obligations must be excluded when decision is Permit");
    }

    // ── Contract: Advice filtering respects IncludeAdvice flag ───────

    [Fact]
    public async Task EvaluateAsync_IncludeAdviceFalse_ShouldReturnEmptyAdvice()
    {
        // Arrange
        var advice = new AdviceExpression
        {
            Id = "suggest-mfa",
            AppliesTo = FulfillOn.Permit,
            AttributeAssignments = []
        };
        var rule = MakeRule("r1", Effect.Permit, advice: [advice]);
        var policy = MakePolicy("p1",
            algorithm: CombiningAlgorithmId.PermitOverrides, rules: [rule]);
        var policySet = MakePolicySet("ps1", policies: [policy]);

        var pap = Substitute.For<IPolicyAdministrationPoint>();
        pap.GetPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<PolicySet>>([policySet]));
        pap.GetPoliciesAsync(null, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<Policy>>([]));

        var pdp = CreatePdp(pap);

        // Act
        var decision = await pdp.EvaluateAsync(MakeContext(includeAdvice: false));

        // Assert
        decision.Effect.ShouldBe(Effect.Permit);
        decision.Advice.ShouldBeEmpty(
            "When IncludeAdvice is false, advice must be empty per PDP contract");
    }

    // ── Contract: Standalone policies are evaluated ──────────────────

    [Fact]
    public async Task EvaluateAsync_StandalonePolicy_ShouldBeEvaluated()
    {
        // Arrange
        var rule = MakeRule("r1", Effect.Permit);
        var standalonePolicy = MakePolicy("standalone-p1", rules: [rule]);

        var pap = Substitute.For<IPolicyAdministrationPoint>();
        pap.GetPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<PolicySet>>([]));
        pap.GetPoliciesAsync(null, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<Policy>>([standalonePolicy]));

        var pdp = CreatePdp(pap);

        // Act
        var decision = await pdp.EvaluateAsync(MakeContext());

        // Assert
        decision.Effect.ShouldBe(Effect.Permit,
            "Standalone policies must be evaluated alongside policy set policies");
    }

    // ── Contract: EvaluationDuration is always populated ─────────────

    [Fact]
    public async Task EvaluateAsync_ShouldAlwaysPopulate_EvaluationDuration()
    {
        // Arrange
        var pap = Substitute.For<IPolicyAdministrationPoint>();
        pap.GetPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<PolicySet>>([]));
        pap.GetPoliciesAsync(null, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<Policy>>([]));

        var pdp = CreatePdp(pap);

        // Act
        var decision = await pdp.EvaluateAsync(MakeContext());

        // Assert
        decision.EvaluationDuration.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero,
            "EvaluationDuration must always be set, even for empty evaluations");
    }
}

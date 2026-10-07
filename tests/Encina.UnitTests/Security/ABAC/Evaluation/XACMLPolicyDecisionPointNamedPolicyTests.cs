#pragma warning disable CA2012 // Use ValueTasks correctly -- NSubstitute mock setup pattern

using Encina.Security.ABAC;
using Encina.Security.ABAC.Administration;
using Encina.Security.ABAC.CombiningAlgorithms;
using Encina.Security.ABAC.Evaluation;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.ABAC.Evaluation;

/// <summary>
/// Unit tests for <see cref="XACMLPolicyDecisionPoint.EvaluatePolicyAsync"/>: the evaluation of
/// one named policy set or policy used to enforce <see cref="RequirePolicyAttribute"/> (#1634).
/// </summary>
public sealed class XACMLPolicyDecisionPointNamedPolicyTests
{
    private readonly FakeLogger<XACMLPolicyDecisionPoint> _logger = new();

    private XACMLPolicyDecisionPoint CreatePdp(IPolicyAdministrationPoint pap)
    {
        var registry = new DefaultFunctionRegistry();
        return new XACMLPolicyDecisionPoint(
            pap, new TargetEvaluator(registry), new ConditionEvaluator(registry), new CombiningAlgorithmFactory(), _logger);
    }

    private static PolicyEvaluationContext Context(bool includeAdvice = true) => new()
    {
        SubjectAttributes = new Dictionary<string, AttributeBag>(),
        ResourceAttributes = new Dictionary<string, AttributeBag>(),
        EnvironmentAttributes = new Dictionary<string, AttributeBag>(),
        ActionAttributes = new Dictionary<string, AttributeBag>(),
        RequestType = typeof(object),
        IncludeAdvice = includeAdvice
    };

    private static Policy MakePolicy(
        string id,
        Effect effect,
        bool isEnabled = true,
        IReadOnlyList<Obligation>? obligations = null,
        IReadOnlyList<AdviceExpression>? advice = null) => new()
        {
            Id = id,
            IsEnabled = isEnabled,
            Algorithm = CombiningAlgorithmId.DenyOverrides,
            Rules = [new Rule { Id = id + "-rule", Effect = effect, Obligations = [], Advice = [] }],
            Obligations = obligations ?? [],
            Advice = advice ?? [],
            VariableDefinitions = []
        };

    private static IPolicyAdministrationPoint Pap(PolicySet? policySet = null, Policy? policy = null)
    {
        var pap = Substitute.For<IPolicyAdministrationPoint>();
        pap.GetPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<PolicySet>>(policySet is null ? [] : [policySet]));
        pap.GetPoliciesAsync(null, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<Policy>>(policy is null ? [] : [policy]));
        return pap;
    }

    private static PolicySet MakePolicySet(string id, bool isEnabled, params Policy[] policies) => new()
    {
        Id = id,
        IsEnabled = isEnabled,
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Policies = policies,
        PolicySets = [],
        Obligations = [],
        Advice = []
    };

    private static PolicyDecision RightOrFail(Either<EncinaError, PolicyDecision> result) =>
        result.Match(Right: decision => decision, Left: error => throw new ShouldAssertException(error.Message));

    [Theory]
    [InlineData(Effect.Permit)]
    [InlineData(Effect.Deny)]
    public async Task EvaluatePolicyAsync_Policy_ReturnsItsEffect(Effect effect)
    {
        var pdp = CreatePdp(Pap(policy: MakePolicy("policy-a", effect)));

        var decision = RightOrFail(await pdp.EvaluatePolicyAsync("policy-a", Context()));

        decision.Effect.ShouldBe(effect);
        decision.PolicyId.ShouldBe("policy-a");
    }

    [Fact]
    public async Task EvaluatePolicyAsync_DisabledPolicy_IsNotApplicable()
    {
        var pdp = CreatePdp(Pap(policy: MakePolicy("policy-a", Effect.Permit, isEnabled: false)));

        var decision = RightOrFail(await pdp.EvaluatePolicyAsync("policy-a", Context()));

        decision.Effect.ShouldBe(Effect.NotApplicable);
    }

    [Fact]
    public async Task EvaluatePolicyAsync_PolicySetAndPolicyShareTheName_EvaluatesThePolicySet()
    {
        var policySet = MakePolicySet("shared", isEnabled: true, MakePolicy("inner", Effect.Deny));
        var pap = Pap(policySet, MakePolicy("shared", Effect.Permit));
        var pdp = CreatePdp(pap);

        var decision = RightOrFail(await pdp.EvaluatePolicyAsync("shared", Context()));

        decision.Effect.ShouldBe(Effect.Deny);
        await pap.DidNotReceiveWithAnyArgs().GetPoliciesAsync(default, default);
    }

    [Fact]
    public async Task EvaluatePolicyAsync_PolicyNestedInDisabledSet_IsNotFound()
    {
        // The real in-memory PAP finds nested policies through GetPolicyAsync; the PDP must not.
        var pap = new InMemoryPolicyAdministrationPoint(NullLogger<InMemoryPolicyAdministrationPoint>.Instance);
        (await pap.AddPolicySetAsync(MakePolicySet("parent", isEnabled: false, MakePolicy("child", Effect.Permit))))
            .IsRight.ShouldBeTrue();
        var pdp = CreatePdp(pap);

        var result = await pdp.EvaluatePolicyAsync("child", Context());

        result.Match(Right: _ => "<right>", Left: e => e.GetCode().IfNone("<none>")).ShouldBe(ABACErrors.PolicyNotFoundCode);
    }

    [Fact]
    public async Task EvaluatePolicyAsync_DisabledParentSetNamed_IsNotApplicable()
    {
        var pap = new InMemoryPolicyAdministrationPoint(NullLogger<InMemoryPolicyAdministrationPoint>.Instance);
        (await pap.AddPolicySetAsync(MakePolicySet("parent", isEnabled: false, MakePolicy("child", Effect.Permit))))
            .IsRight.ShouldBeTrue();
        var pdp = CreatePdp(pap);

        var decision = RightOrFail(await pdp.EvaluatePolicyAsync("parent", Context()));

        decision.Effect.ShouldBe(Effect.NotApplicable);
    }

    [Fact]
    public async Task EvaluatePolicyAsync_KeepsOnlyTheObligationsAndAdviceOfTheEffect()
    {
        var policy = MakePolicy(
            "policy-a",
            Effect.Permit,
            obligations:
            [
                new Obligation { Id = "on-permit", FulfillOn = FulfillOn.Permit, AttributeAssignments = [] },
                new Obligation { Id = "on-deny", FulfillOn = FulfillOn.Deny, AttributeAssignments = [] }
            ],
            advice: [new AdviceExpression { Id = "advice", AppliesTo = FulfillOn.Permit, AttributeAssignments = [] }]);
        var pdp = CreatePdp(Pap(policy: policy));

        var withAdvice = RightOrFail(await pdp.EvaluatePolicyAsync("policy-a", Context()));
        var withoutAdvice = RightOrFail(await pdp.EvaluatePolicyAsync("policy-a", Context(includeAdvice: false)));

        withAdvice.Obligations.Select(o => o.Id).ShouldBe(["on-permit"]);
        withAdvice.Advice.Select(a => a.Id).ShouldBe(["advice"]);
        withoutAdvice.Advice.ShouldBeEmpty();
    }

    [Fact]
    public async Task EvaluatePolicyAsync_NotFound_ReturnsLeftPolicyNotFound()
    {
        var pdp = CreatePdp(Pap());

        var result = await pdp.EvaluatePolicyAsync("missing", Context());

        result.Match(Right: _ => "<right>", Left: e => e.GetCode().IfNone("<none>")).ShouldBe(ABACErrors.PolicyNotFoundCode);
    }

    [Fact]
    public async Task EvaluatePolicyAsync_PolicySetLookupFails_IsIndeterminateAndLogsTheCodeOnly()
    {
        var pap = Substitute.For<IPolicyAdministrationPoint>();
        pap.GetPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, IReadOnlyList<PolicySet>>(EncinaErrors.Create("store.down", "secret connection detail")));
        var pdp = CreatePdp(pap);

        var decision = RightOrFail(await pdp.EvaluatePolicyAsync("policy-a", Context()));

        decision.Effect.ShouldBe(Effect.Indeterminate);
        var record = _logger.Collector.GetSnapshot().Single(r => r.Id.Id == 9072);
        record.Message.ShouldContain("store.down");
        record.Message.ShouldNotContain("secret connection detail");
    }

    [Fact]
    public async Task EvaluatePolicyAsync_PolicyLookupFails_IsIndeterminate()
    {
        var pap = Substitute.For<IPolicyAdministrationPoint>();
        pap.GetPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<PolicySet>>([]));
        pap.GetPoliciesAsync(null, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, IReadOnlyList<Policy>>(EncinaErrors.Create("store.down", "down")));
        var pdp = CreatePdp(pap);

        var decision = RightOrFail(await pdp.EvaluatePolicyAsync("policy-a", Context()));

        decision.Effect.ShouldBe(Effect.Indeterminate);
    }

    [Fact]
    public async Task EvaluatePolicyAsync_StoreThrows_IsIndeterminateAndLogsNoMessage()
    {
        var pap = Substitute.For<IPolicyAdministrationPoint>();
        pap.GetPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, IReadOnlyList<PolicySet>>>>(_ => throw new InvalidOperationException("secret detail"));
        var pdp = CreatePdp(pap);

        var decision = RightOrFail(await pdp.EvaluatePolicyAsync("policy-a", Context()));

        decision.Effect.ShouldBe(Effect.Indeterminate);
        var record = _logger.Collector.GetSnapshot().Single(r => r.Id.Id == 9073);
        record.Exception.ShouldNotBeNull();
        record.Exception!.Message.ShouldNotContain("secret detail");
    }

    [Fact]
    public async Task EvaluatePolicyAsync_Cancelled_PropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var pap = Substitute.For<IPolicyAdministrationPoint>();
        pap.GetPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, IReadOnlyList<PolicySet>>>>(_ => throw new OperationCanceledException(cts.Token));
        var pdp = CreatePdp(pap);

        await Should.ThrowAsync<OperationCanceledException>(
            async () => await pdp.EvaluatePolicyAsync("policy-a", Context(), cts.Token));
    }

    [Fact]
    public async Task EvaluatePolicyAsync_OperationCanceledWithUncancelledToken_IsIndeterminate()
    {
        // An OperationCanceledException the caller did not ask for (an HttpClient timeout in a
        // persistent store, for instance) is a store failure, not a cancellation: fail closed.
        var pap = Substitute.For<IPolicyAdministrationPoint>();
        pap.GetPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, IReadOnlyList<PolicySet>>>>(_ => throw new OperationCanceledException("timeout"));
        var pdp = CreatePdp(pap);

        var decision = RightOrFail(await pdp.EvaluatePolicyAsync("policy-a", Context(), CancellationToken.None));

        decision.Effect.ShouldBe(Effect.Indeterminate);
    }
}

#pragma warning disable CA2012 // Use ValueTasks correctly -- NSubstitute mock setup pattern

using Encina.Security.ABAC;
using Encina.Security.ABAC.CombiningAlgorithms;
using Encina.Security.ABAC.Evaluation;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;
using Rule = Encina.Security.ABAC.Rule;

namespace Encina.GuardTests.Security.ABAC.Evaluation;

/// <summary>
/// Guard clause tests for <see cref="XACMLPolicyDecisionPoint"/>.
/// Verifies constructor guards, EvaluateAsync null guards, and
/// policy evaluation paths (disabled policies, target mismatches, combining, etc.).
/// </summary>
public class XACMLPolicyDecisionPointGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullPap_ThrowsArgumentNullException()
    {
        var act = () => new XACMLPolicyDecisionPoint(
            null!,
            CreateTargetEvaluator(),
            CreateConditionEvaluator(),
            new CombiningAlgorithmFactory(),
            NullLoggerFactory.Instance.CreateLogger<XACMLPolicyDecisionPoint>());

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("pap");
    }

    [Fact]
    public void Constructor_NullTargetEvaluator_ThrowsArgumentNullException()
    {
        var act = () => new XACMLPolicyDecisionPoint(
            Substitute.For<IPolicyAdministrationPoint>(),
            null!,
            CreateConditionEvaluator(),
            new CombiningAlgorithmFactory(),
            NullLoggerFactory.Instance.CreateLogger<XACMLPolicyDecisionPoint>());

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("targetEvaluator");
    }

    [Fact]
    public void Constructor_NullConditionEvaluator_ThrowsArgumentNullException()
    {
        var act = () => new XACMLPolicyDecisionPoint(
            Substitute.For<IPolicyAdministrationPoint>(),
            CreateTargetEvaluator(),
            null!,
            new CombiningAlgorithmFactory(),
            NullLoggerFactory.Instance.CreateLogger<XACMLPolicyDecisionPoint>());

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("conditionEvaluator");
    }

    [Fact]
    public void Constructor_NullAlgorithmFactory_ThrowsArgumentNullException()
    {
        var act = () => new XACMLPolicyDecisionPoint(
            Substitute.For<IPolicyAdministrationPoint>(),
            CreateTargetEvaluator(),
            CreateConditionEvaluator(),
            null!,
            NullLoggerFactory.Instance.CreateLogger<XACMLPolicyDecisionPoint>());

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("algorithmFactory");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new XACMLPolicyDecisionPoint(
            Substitute.For<IPolicyAdministrationPoint>(),
            CreateTargetEvaluator(),
            CreateConditionEvaluator(),
            new CombiningAlgorithmFactory(),
            null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_AllValid_DoesNotThrow()
    {
        var act = () => CreatePDP();

        Should.NotThrow(act);
    }

    #endregion

    #region EvaluateAsync — Null Context Guard

    [Fact]
    public async Task EvaluateAsync_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreatePDP();

        // Act
        var act = async () => await sut.EvaluateAsync(null!);

        // Assert
        (await Should.ThrowAsync<ArgumentNullException>(act))
            .ParamName.ShouldBe("context");
    }

    #endregion

    #region EvaluateAsync — Empty PAP Returns NotApplicable

    [Fact]
    public async Task EvaluateAsync_NoPolicies_ReturnsNotApplicable()
    {
        // Arrange
        var pap = Substitute.For<IPolicyAdministrationPoint>();
        pap.GetPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<PolicySet>>(
                (IReadOnlyList<PolicySet>)new List<PolicySet>()));
        pap.GetPoliciesAsync(null, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<Policy>>(
                (IReadOnlyList<Policy>)new List<Policy>()));

        var sut = CreatePDP(pap: pap);
        var context = CreateMinimalContext();

        // Act
        var decision = await sut.EvaluateAsync(context);

        // Assert
        decision.Effect.ShouldBe(Effect.NotApplicable);
    }

    #endregion

    #region EvaluateAsync — PAP Error Returns Indeterminate

    [Fact]
    public async Task EvaluateAsync_PapPolicySetError_ReturnsIndeterminate()
    {
        // Arrange
        var pap = Substitute.For<IPolicyAdministrationPoint>();
        pap.GetPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, IReadOnlyList<PolicySet>>(
                EncinaErrors.Create("pap-error", "PAP failed")));

        var sut = CreatePDP(pap: pap);
        var context = CreateMinimalContext();

        // Act
        var decision = await sut.EvaluateAsync(context);

        // Assert
        decision.Effect.ShouldBe(Effect.Indeterminate);
        decision.Status.ShouldNotBeNull();
        decision.Status!.StatusCode.ShouldBe("processing-error");
    }

    #endregion

    #region EvaluateAsync — Disabled Policy Returns NotApplicable

    [Fact]
    public async Task EvaluateAsync_DisabledPolicy_ReturnsNotApplicable()
    {
        // Arrange
        var disabledPolicy = new Policy
        {
            Id = "disabled-pol",
            IsEnabled = false,
            Target = null,
            Algorithm = CombiningAlgorithmId.DenyOverrides,
            Rules = [],
            Obligations = [],
            Advice = [],
            VariableDefinitions = []
        };

        var pap = Substitute.For<IPolicyAdministrationPoint>();
        pap.GetPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<PolicySet>>(
                (IReadOnlyList<PolicySet>)new List<PolicySet>()));
        pap.GetPoliciesAsync(null, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<Policy>>(
                (IReadOnlyList<Policy>)new List<Policy> { disabledPolicy }));

        var sut = CreatePDP(pap: pap);
        var context = CreateMinimalContext();

        // Act
        var decision = await sut.EvaluateAsync(context);

        // Assert
        decision.Effect.ShouldBe(Effect.NotApplicable);
    }

    #endregion

    #region EvaluateAsync — Disabled PolicySet Returns NotApplicable

    [Fact]
    public async Task EvaluateAsync_DisabledPolicySet_ReturnsNotApplicable()
    {
        // Arrange
        var disabledPolicySet = new PolicySet
        {
            Id = "disabled-ps",
            IsEnabled = false,
            Target = null,
            Algorithm = CombiningAlgorithmId.DenyOverrides,
            Policies = [],
            PolicySets = [],
            Obligations = [],
            Advice = []
        };

        var pap = Substitute.For<IPolicyAdministrationPoint>();
        pap.GetPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<PolicySet>>(
                (IReadOnlyList<PolicySet>)new List<PolicySet> { disabledPolicySet }));
        pap.GetPoliciesAsync(null, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<Policy>>(
                (IReadOnlyList<Policy>)new List<Policy>()));

        var sut = CreatePDP(pap: pap);
        var context = CreateMinimalContext();

        // Act
        var decision = await sut.EvaluateAsync(context);

        // Assert
        decision.Effect.ShouldBe(Effect.NotApplicable);
    }

    #endregion

    #region EvaluateAsync — Policy With Permit Rule Returns Permit

    [Fact]
    public async Task EvaluateAsync_PolicyWithPermitRule_ReturnsPermit()
    {
        // Arrange
        var policy = new Policy
        {
            Id = "permit-pol",
            IsEnabled = true,
            Target = null, // Matches everything
            Algorithm = CombiningAlgorithmId.PermitOverrides,
            Rules =
            [
                new Rule
                {
                    Id = "permit-rule",
                    Effect = Effect.Permit,
                    Target = null,
                    Condition = null, // Unconditional
                    Obligations = [],
                    Advice = []
                }
            ],
            Obligations = [],
            Advice = [],
            VariableDefinitions = []
        };

        var pap = Substitute.For<IPolicyAdministrationPoint>();
        pap.GetPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<PolicySet>>(
                (IReadOnlyList<PolicySet>)new List<PolicySet>()));
        pap.GetPoliciesAsync(null, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<Policy>>(
                (IReadOnlyList<Policy>)new List<Policy> { policy }));

        var sut = CreatePDP(pap: pap);
        var context = CreateMinimalContext();

        // Act
        var decision = await sut.EvaluateAsync(context);

        // Assert
        decision.Effect.ShouldBe(Effect.Permit);
        decision.PolicyId.ShouldBe("permit-pol");
    }

    #endregion

    #region EvaluateAsync — Unexpected Exception Returns Indeterminate

    [Fact]
    public async Task EvaluateAsync_UnexpectedException_ReturnsIndeterminate()
    {
        // Arrange
        var pap = Substitute.For<IPolicyAdministrationPoint>();
        pap.GetPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, IReadOnlyList<PolicySet>>>>(_ =>
                throw new InvalidOperationException("Unexpected!"));

        var sut = CreatePDP(pap: pap);
        var context = CreateMinimalContext();

        // Act
        var decision = await sut.EvaluateAsync(context);

        // Assert
        decision.Effect.ShouldBe(Effect.Indeterminate);
        decision.Status.ShouldNotBeNull();
    }

    #endregion

    #region EvaluateAsync — Obligation Filtering by Effect

    [Fact]
    public async Task EvaluateAsync_PermitDecision_FiltersOnlyPermitObligations()
    {
        // Arrange
        var policy = new Policy
        {
            Id = "permit-with-obligations",
            IsEnabled = true,
            Target = null,
            Algorithm = CombiningAlgorithmId.PermitOverrides,
            Rules =
            [
                new Rule
                {
                    Id = "permit-rule",
                    Effect = Effect.Permit,
                    Target = null,
                    Condition = null,
                    Obligations =
                    [
                        new Obligation { Id = "permit-ob", FulfillOn = FulfillOn.Permit, AttributeAssignments = [] },
                        new Obligation { Id = "deny-ob", FulfillOn = FulfillOn.Deny, AttributeAssignments = [] }
                    ],
                    Advice = []
                }
            ],
            Obligations = [],
            Advice = [],
            VariableDefinitions = []
        };

        var pap = Substitute.For<IPolicyAdministrationPoint>();
        pap.GetPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<PolicySet>>(
                (IReadOnlyList<PolicySet>)new List<PolicySet>()));
        pap.GetPoliciesAsync(null, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<Policy>>(
                (IReadOnlyList<Policy>)new List<Policy> { policy }));

        var sut = CreatePDP(pap: pap);
        var context = CreateMinimalContext();

        // Act
        var decision = await sut.EvaluateAsync(context);

        // Assert
        decision.Effect.ShouldBe(Effect.Permit);
        var obligation = decision.Obligations.ShouldHaveSingleItem();
        obligation.Id.ShouldBe("permit-ob");
    }

    #endregion

    #region EvaluateAsync — Nested PolicySet Evaluation

    [Fact]
    public async Task EvaluateAsync_NestedPolicySet_EvaluatesRecursively()
    {
        // Arrange
        var innerPolicy = new Policy
        {
            Id = "inner-pol",
            IsEnabled = true,
            Target = null,
            Algorithm = CombiningAlgorithmId.DenyOverrides,
            Rules =
            [
                new Rule { Id = "r1", Effect = Effect.Deny, Target = null, Condition = null, Obligations = [], Advice = [] }
            ],
            Obligations = [],
            Advice = [],
            VariableDefinitions = []
        };

        var policySet = new PolicySet
        {
            Id = "outer-ps",
            IsEnabled = true,
            Target = null,
            Algorithm = CombiningAlgorithmId.DenyOverrides,
            Policies = [innerPolicy],
            PolicySets = [],
            Obligations = [],
            Advice = []
        };

        var pap = Substitute.For<IPolicyAdministrationPoint>();
        pap.GetPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<PolicySet>>(
                (IReadOnlyList<PolicySet>)new List<PolicySet> { policySet }));
        pap.GetPoliciesAsync(null, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<Policy>>(
                (IReadOnlyList<Policy>)new List<Policy>()));

        var sut = CreatePDP(pap: pap);
        var context = CreateMinimalContext();

        // Act
        var decision = await sut.EvaluateAsync(context);

        // Assert
        decision.Effect.ShouldBe(Effect.Deny);
    }

    #endregion

    // ── Helpers ──────────────────────────────────────────────────────

    private static TargetEvaluator CreateTargetEvaluator() =>
        new(Substitute.For<IFunctionRegistry>());

    private static ConditionEvaluator CreateConditionEvaluator() =>
        new(Substitute.For<IFunctionRegistry>());

    private static XACMLPolicyDecisionPoint CreatePDP(
        IPolicyAdministrationPoint? pap = null)
    {
        pap ??= Substitute.For<IPolicyAdministrationPoint>();
        var functionRegistry = new DefaultFunctionRegistry();
        var targetEvaluator = new TargetEvaluator(functionRegistry);
        var conditionEvaluator = new ConditionEvaluator(functionRegistry);
        var algorithmFactory = new CombiningAlgorithmFactory();
        var logger = NullLoggerFactory.Instance.CreateLogger<XACMLPolicyDecisionPoint>();
        return new XACMLPolicyDecisionPoint(pap, targetEvaluator, conditionEvaluator, algorithmFactory, logger);
    }

    private static PolicyEvaluationContext CreateMinimalContext() =>
        new()
        {
            SubjectAttributes = AttributeBag.Empty,
            ResourceAttributes = AttributeBag.Empty,
            ActionAttributes = AttributeBag.Empty,
            EnvironmentAttributes = AttributeBag.Empty,
            RequestType = typeof(object),
            IncludeAdvice = true
        };
}

#pragma warning disable CA2012 // Use ValueTasks correctly -- NSubstitute mock setup pattern

using Encina.Security.ABAC;
using Encina.Security.ABAC.Administration;
using Encina.Security.ABAC.CombiningAlgorithms;
using Encina.Security.ABAC.EEL;
using Encina.Security.ABAC.Evaluation;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

using static LanguageExt.Prelude;

using ISecurityContext = global::Encina.Security.ISecurityContext;
using ISecurityContextAccessor = global::Encina.Security.ISecurityContextAccessor;

namespace Encina.UnitTests.Security.ABAC;

/// <summary>
/// End-to-end enforcement tests for <c>[RequirePolicy]</c> and <c>[RequireCondition]</c>
/// through <see cref="ABACPipelineBehavior{TRequest, TResponse}"/> with the real
/// <see cref="XACMLPolicyDecisionPoint"/> and an in-memory policy store (#1634).
/// </summary>
public sealed class ABACRequirementEnforcementTests
{
    private static readonly EELCompiler Compiler = new();

    [RequirePolicy("policy-a")]
    private sealed record RequiresPolicyA : IRequest<string>;

    [RequirePolicy("allow-managers")]
    private sealed record RequiresNestedPolicy : IRequest<string>;

    [RequirePolicy("finance")]
    private sealed record RequiresFinanceSet : IRequest<string>;

    [RequireCondition("user.department == \"HR\"")]
    private sealed record RequiresHrDepartment : IRequest<string>;

    [Fact]
    public async Task Handle_RequiredPolicyNotApplicableWhileStorePermits_IsDenied()
    {
        // Arrange: the store as a whole permits (another standalone policy permits), but the
        // required policy is disabled, so it is NotApplicable.
        var pap = NewPap();
        (await pap.AddPolicyAsync(UnconditionalPolicy("policy-a", Effect.Permit, isEnabled: false), null)).IsRight.ShouldBeTrue();
        (await pap.AddPolicyAsync(UnconditionalPolicy("policy-b", Effect.Permit), null)).IsRight.ShouldBeTrue();

        var behavior = CreateBehavior<RequiresPolicyA>(pap, new Dictionary<string, object>());
        var nextCalled = false;

        // Act
        var result = await behavior.Handle(
            new RequiresPolicyA(), Substitute.For<IRequestContext>(), Next(() => nextCalled = true), CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue("the required policy does not apply, so the request must be denied");
        nextCalled.ShouldBeFalse();
        ErrorCode(result).ShouldBe(ABACErrors.AccessDeniedCode);
    }

    [Fact]
    public async Task Handle_RequiredPolicyNestedInDisabledSet_IsPolicyNotFound()
    {
        // Arrange: the permitting policy lives only inside a disabled policy set; naming it
        // directly must not bypass the set.
        var pap = NewPap();
        (await pap.AddPolicySetAsync(FinanceSet(isEnabled: false, obligations: []))).IsRight.ShouldBeTrue();

        var behavior = CreateBehavior<RequiresNestedPolicy>(pap, new Dictionary<string, object>());
        var nextCalled = false;

        // Act
        var result = await behavior.Handle(
            new RequiresNestedPolicy(), Substitute.For<IRequestContext>(), Next(() => nextCalled = true), CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue("a nested policy is not found by its own name");
        nextCalled.ShouldBeFalse();
        ErrorCode(result).ShouldBe(ABACErrors.PolicyNotFoundCode);
    }

    [Fact]
    public async Task Handle_RequiredPolicySetDisabled_IsDenied()
    {
        // Arrange: naming the parent set applies its enabled flag.
        var pap = NewPap();
        (await pap.AddPolicySetAsync(FinanceSet(isEnabled: false, obligations: []))).IsRight.ShouldBeTrue();

        var behavior = CreateBehavior<RequiresFinanceSet>(pap, new Dictionary<string, object>());
        var nextCalled = false;

        // Act
        var result = await behavior.Handle(
            new RequiresFinanceSet(), Substitute.For<IRequestContext>(), Next(() => nextCalled = true), CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue("a disabled policy set is NotApplicable, so the request must be denied");
        nextCalled.ShouldBeFalse();
        ErrorCode(result).ShouldBe(ABACErrors.AccessDeniedCode);
    }

    [Fact]
    public async Task Handle_RequiredPolicySetEnabled_UsesTheSetAlgorithmAndRunsTheSetObligations()
    {
        // Arrange: the set combines a Deny and a Permit child with permit-overrides, and carries
        // an on-Permit obligation of its own.
        var pap = NewPap();
        (await pap.AddPolicySetAsync(FinanceSet(
            isEnabled: true,
            obligations: [new Obligation { Id = "audit", FulfillOn = FulfillOn.Permit, AttributeAssignments = [] }])))
            .IsRight.ShouldBeTrue();

        var handler = Substitute.For<IObligationHandler>();
        handler.CanHandle("audit").Returns(true);
        handler.HandleAsync(Arg.Any<Obligation>(), Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(unit));

        var behavior = CreateBehavior<RequiresFinanceSet>(pap, new Dictionary<string, object>(), [handler]);
        var nextCalled = false;

        // Act
        var result = await behavior.Handle(
            new RequiresFinanceSet(), Substitute.For<IRequestContext>(), Next(() => nextCalled = true), CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue("permit-overrides of the set permits");
        nextCalled.ShouldBeTrue();
        await handler.Received(1).HandleAsync(
            Arg.Is<Obligation>(o => o.Id == "audit"), Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RequiredConditionFalseWhileStorePermits_IsDenied()
    {
        // Arrange: the store permits everything, but the subject is not in HR.
        var pap = NewPap();
        (await pap.AddPolicyAsync(UnconditionalPolicy("permit-all", Effect.Permit), null)).IsRight.ShouldBeTrue();

        var behavior = CreateBehavior<RequiresHrDepartment>(
            pap, new Dictionary<string, object> { ["department"] = "Finance" });
        var nextCalled = false;

        // Act
        var result = await behavior.Handle(
            new RequiresHrDepartment(), Substitute.For<IRequestContext>(), Next(() => nextCalled = true), CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue("the required condition is false, so the request must be denied");
        nextCalled.ShouldBeFalse();
        ErrorCode(result).ShouldBe(ABACErrors.ConditionNotMetCode);
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private static InMemoryPolicyAdministrationPoint NewPap() =>
        new(NullLogger<InMemoryPolicyAdministrationPoint>.Instance);

    private static PolicySet FinanceSet(bool isEnabled, IReadOnlyList<Obligation> obligations) => new()
    {
        Id = "finance",
        IsEnabled = isEnabled,
        Algorithm = CombiningAlgorithmId.PermitOverrides,
        Policies = [UnconditionalPolicy("deny-others", Effect.Deny), UnconditionalPolicy("allow-managers", Effect.Permit)],
        PolicySets = [],
        Obligations = obligations,
        Advice = []
    };

    private static Policy UnconditionalPolicy(string id, Effect effect, bool isEnabled = true) => new()
    {
        Id = id,
        IsEnabled = isEnabled,
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Rules =
        [
            new Rule { Id = id + "-rule", Effect = effect, Obligations = [], Advice = [] }
        ],
        Obligations = [],
        Advice = [],
        VariableDefinitions = []
    };

    private static ABACPipelineBehavior<TRequest, string> CreateBehavior<TRequest>(
        IPolicyAdministrationPoint pap,
        IReadOnlyDictionary<string, object> subjectAttributes,
        IEnumerable<IObligationHandler>? handlers = null)
        where TRequest : IRequest<string>
    {
        var registry = new DefaultFunctionRegistry();
        var pdp = new XACMLPolicyDecisionPoint(
            pap,
            new TargetEvaluator(registry),
            new ConditionEvaluator(registry),
            new CombiningAlgorithmFactory(),
            NullLogger<XACMLPolicyDecisionPoint>.Instance);

        var attributeProvider = Substitute.For<IAttributeProvider>();
        attributeProvider.GetSubjectAttributesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(subjectAttributes);
        attributeProvider.GetResourceAttributesAsync(Arg.Any<TRequest>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object>());
        attributeProvider.GetEnvironmentAttributesAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object>());

        var securityContext = Substitute.For<ISecurityContext>();
        securityContext.UserId.Returns("user-1");
        var accessor = Substitute.For<ISecurityContextAccessor>();
        accessor.SecurityContext.Returns(securityContext);

        return new ABACPipelineBehavior<TRequest, string>(
            pdp,
            attributeProvider,
            accessor,
            new ObligationExecutor(handlers ?? [], NullLogger<ObligationExecutor>.Instance),
            Compiler,
            Options.Create(new ABACOptions { EnforcementMode = ABACEnforcementMode.Block }),
            NullLogger<ABACPipelineBehavior<TRequest, string>>.Instance);
    }

    private static RequestHandlerCallback<string> Next(Action onCalled) => () =>
    {
        onCalled();
        return ValueTask.FromResult(Either<EncinaError, string>.Right("handled"));
    };

    private static string ErrorCode(Either<EncinaError, string> result) =>
        result.Match(
            Right: _ => string.Empty,
            Left: error => error.GetCode().IfNone(string.Empty));
}

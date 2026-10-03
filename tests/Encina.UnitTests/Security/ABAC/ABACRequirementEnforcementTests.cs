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

    [RequireCondition("user.department == \"HR\"")]
    private sealed record RequiresHrDepartment : IRequest<string>;

    [Fact]
    public async Task Handle_RequiredPolicyDeniesWhileStorePermits_IsDenied()
    {
        // Arrange: the store as a whole permits (permit-overrides), but the required policy denies.
        var pap = new InMemoryPolicyAdministrationPoint(NullLogger<InMemoryPolicyAdministrationPoint>.Instance);
        (await pap.AddPolicySetAsync(new PolicySet
        {
            Id = "root",
            Algorithm = CombiningAlgorithmId.PermitOverrides,
            Policies = [UnconditionalPolicy("policy-a", Effect.Deny), UnconditionalPolicy("policy-b", Effect.Permit)],
            PolicySets = [],
            Obligations = [],
            Advice = []
        })).IsRight.ShouldBeTrue();

        var behavior = CreateBehavior<RequiresPolicyA>(pap, new Dictionary<string, object>());
        var nextCalled = false;

        // Act
        var result = await behavior.Handle(
            new RequiresPolicyA(), Substitute.For<IRequestContext>(), Next(() => nextCalled = true), CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue("the required policy denies, so the request must be denied");
        nextCalled.ShouldBeFalse();
        ErrorCode(result).ShouldBe(ABACErrors.AccessDeniedCode);
    }

    [Fact]
    public async Task Handle_RequiredConditionFalseWhileStorePermits_IsDenied()
    {
        // Arrange: the store permits everything, but the subject is not in HR.
        var pap = new InMemoryPolicyAdministrationPoint(NullLogger<InMemoryPolicyAdministrationPoint>.Instance);
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

    private static Policy UnconditionalPolicy(string id, Effect effect) => new()
    {
        Id = id,
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
        IReadOnlyDictionary<string, object> subjectAttributes)
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
            new ObligationExecutor([], NullLogger<ObligationExecutor>.Instance),
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

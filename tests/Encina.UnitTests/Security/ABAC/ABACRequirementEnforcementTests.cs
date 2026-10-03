#pragma warning disable CA2012 // Use ValueTasks correctly -- NSubstitute mock setup pattern

using Encina.Security.ABAC;
using Encina.Security.ABAC.Administration;
using Encina.Security.ABAC.CombiningAlgorithms;
using Encina.Security.ABAC.EEL;
using Encina.Security.ABAC.Evaluation;

using System.Diagnostics;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
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

    // ── Store failures (#1676) ──────────────────────────────────────

    [Theory]
    [InlineData(ABACEnforcementMode.Block)]
    [InlineData(ABACEnforcementMode.Warn)]
    public async Task Handle_StandalonePolicyRetrievalFails_IsIndeterminateInEveryMode(ABACEnforcementMode mode)
    {
        // Arrange
        var behavior = CreateBehavior<RequiresPolicyA>(
            PapFailingStandaloneRetrieval("down"), new Dictionary<string, object>(), mode: mode);
        var nextCalled = false;

        // Act
        var result = await behavior.Handle(
            new RequiresPolicyA(), Substitute.For<IRequestContext>(), Next(() => nextCalled = true), CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue("a store that cannot be read is an error, not a verdict");
        nextCalled.ShouldBeFalse();
        ErrorCode(result).ShouldBe(ABACErrors.IndeterminateCode);
    }

    [Theory]
    [InlineData(ABACEnforcementMode.Block)]
    [InlineData(ABACEnforcementMode.Warn)]
    public async Task Handle_StandalonePolicyRetrievalFails_SentinelReachesNoLogNoActivityTagAndNoError(ABACEnforcementMode mode)
    {
        // Arrange
        const string Sentinel = "SENTINEL-1676-secret-connection-string";
        var pdpLogger = new FakeLogger<XACMLPolicyDecisionPoint>();
        var pepLogger = new FakeLogger<ABACPipelineBehavior<RequiresPolicyA, string>>();
        var behavior = CreateBehavior<RequiresPolicyA>(
            PapFailingStandaloneRetrieval(Sentinel), new Dictionary<string, object>(),
            mode: mode, pdpLogger: pdpLogger, pepLogger: pepLogger);

        var activities = new System.Collections.Concurrent.ConcurrentBag<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Encina.Security.ABAC",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activities.Add
        };
        ActivitySource.AddActivityListener(listener);

        // Act
        var result = await behavior.Handle(
            new RequiresPolicyA(), Substitute.For<IRequestContext>(), Next(() => { }), CancellationToken.None);

        // Assert
        ErrorCode(result).ShouldBe(ABACErrors.IndeterminateCode);
        result.IfLeft(error =>
        {
            error.Message.ShouldNotContain(Sentinel);
            error.GetDetails().Values.ShouldAllBe(value => value == null || !value.ToString()!.Contains(Sentinel));
        });

        var records = pdpLogger.Collector.GetSnapshot().Concat(pepLogger.Collector.GetSnapshot()).ToList();
        records.ShouldContain(r => r.Id.Id == 9072 && r.Message.Contains("store.down", StringComparison.Ordinal));
        records.ShouldAllBe(r => !r.Message.Contains(Sentinel, StringComparison.Ordinal));

        activities.ShouldAllBe(a =>
            (a.StatusDescription == null || !a.StatusDescription.Contains(Sentinel, StringComparison.Ordinal))
            && a.TagObjects.All(tag => tag.Value == null || !tag.Value.ToString()!.Contains(Sentinel, StringComparison.Ordinal)));
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
        IEnumerable<IObligationHandler>? handlers = null,
        ABACEnforcementMode mode = ABACEnforcementMode.Block,
        ILogger<XACMLPolicyDecisionPoint>? pdpLogger = null,
        ILogger<ABACPipelineBehavior<TRequest, string>>? pepLogger = null)
        where TRequest : IRequest<string>
    {
        var registry = new DefaultFunctionRegistry();
        var pdp = new XACMLPolicyDecisionPoint(
            pap,
            new TargetEvaluator(registry),
            new ConditionEvaluator(registry),
            new CombiningAlgorithmFactory(),
            pdpLogger ?? NullLogger<XACMLPolicyDecisionPoint>.Instance);

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
            Options.Create(new ABACOptions { EnforcementMode = mode }),
            pepLogger ?? NullLogger<ABACPipelineBehavior<TRequest, string>>.Instance);
    }

    /// <summary>A PAP whose policy sets load (empty) but whose standalone policies cannot be read.</summary>
    private static IPolicyAdministrationPoint PapFailingStandaloneRetrieval(string errorMessage)
    {
        var pap = Substitute.For<IPolicyAdministrationPoint>();
        pap.GetPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<PolicySet>>([]));
        pap.GetPoliciesAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, IReadOnlyList<Policy>>(EncinaErrors.Create("store.down", errorMessage)));
        return pap;
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

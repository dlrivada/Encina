#pragma warning disable CA2012 // Use ValueTasks correctly -- NSubstitute mock setup pattern

using Encina.Security.ABAC;
using Encina.Security.ABAC.EEL;

using Encina.Testing.Identity;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.ContractTests.Security.ABAC;

/// <summary>
/// Behavioral contract tests for <see cref="ABACPipelineBehavior{TRequest, TResponse}"/>.
/// Executes real code paths through the pipeline behavior to verify the PEP enforcement
/// contracts: disabled mode bypass, the named policy deciding the request (#1634), deny flow
/// with enforcement modes, a required policy that is NotApplicable, indeterminate handling,
/// obligation failure, and exception handling.
/// </summary>
[Trait("Category", "Contract")]
[Trait("Feature", "ABAC")]
public sealed class ABACPipelineBehaviorContractTests
{
    private static readonly EELCompiler Compiler = new();

    // -- Test request types --

    [RequirePolicy("test-policy")]
    private sealed record TestPolicyCommand(string Value) : ICommand<string>;

    private sealed record UnprotectedCommand(string Value) : ICommand<string>;

    // -- Helpers --

    private static PolicyDecision MakeDecision(
        Effect effect,
        string? reason = null,
        IReadOnlyList<Obligation>? obligations = null) => new()
        {
            Effect = effect,
            PolicyId = "test-policy",
            Reason = reason,
            Obligations = obligations ?? [],
            Advice = [],
            EvaluationDuration = TimeSpan.FromMilliseconds(1)
        };

    private static IPolicyDecisionPoint PdpReturning(PolicyDecision decision)
    {
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluatePolicyAsync("test-policy", Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, PolicyDecision>(decision)));
        return pdp;
    }

    private static ABACPipelineBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>(
        IPolicyDecisionPoint pdp,
        ABACOptions? options = null,
        ObligationExecutor? obligationExecutor = null)
        where TRequest : IRequest<TResponse>
    {
        var oblExec = obligationExecutor ?? new ObligationExecutor(
            [], NullLogger<ObligationExecutor>.Instance);

        return new ABACPipelineBehavior<TRequest, TResponse>(
            pdp,
            CreateAttributeProvider(),
            oblExec,
            Compiler,
            Options.Create(options ?? new ABACOptions()),
            Substitute.For<global::Encina.Security.ABAC.DecisionAudit.IABACDecisionRecorder>(),
            TimeProvider.System,
            NullLogger<ABACPipelineBehavior<TRequest, TResponse>>.Instance);
    }

    private static IAttributeProvider CreateAttributeProvider()
    {
        var provider = Substitute.For<IAttributeProvider>();
        provider.GetSubjectAttributesAsync(Arg.Any<RequestIdentity>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object>());
        provider.GetResourceAttributesAsync<TestPolicyCommand>(Arg.Any<TestPolicyCommand>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object>());
        provider.GetEnvironmentAttributesAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object>());
        return provider;
    }

    private static string CodeOf(Either<EncinaError, string> result) =>
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: err => err.GetCode().IfNone("<none>"));

    private static async Task<(Either<EncinaError, string> Result, bool NextCalled)> SendAsync(
        ABACPipelineBehavior<TestPolicyCommand, string> behavior,
        RequestIdentity? caller = null)
    {
        var nextCalled = false;
        RequestHandlerCallback<string> next = () =>
        {
            nextCalled = true;
            return ValueTask.FromResult(Right<EncinaError, string>("reached"));
        };

        // The PEP reads the caller from the context it receives (#1705): an authenticated user by default.
        var context = TestRequestContext.For(caller ?? TestIdentity.User("test-user"));
        var result = await behavior.Handle(new TestPolicyCommand("hello"), context, next, CancellationToken.None);
        return (result, nextCalled);
    }

    // -- Unauthenticated caller --

    [Theory]
    [InlineData(ABACEnforcementMode.Block)]
    [InlineData(ABACEnforcementMode.Warn)]
    public async Task Handle_WhenCallerIsAnonymous_ShouldDenyAsUnauthenticatedInEveryEnforcingMode(ABACEnforcementMode mode)
    {
        // Arrange
        var pdp = PdpReturning(MakeDecision(Effect.Permit));
        var behavior = CreateBehavior<TestPolicyCommand, string>(pdp, new ABACOptions { EnforcementMode = mode });

        // Act
        var (result, nextCalled) = await SendAsync(behavior, TestIdentity.Anonymous);

        // Assert
        nextCalled.ShouldBeFalse("an unauthenticated caller is never evaluated");
        CodeOf(result).ShouldBe(EncinaErrorCodes.AuthorizationUnauthenticated);
        await pdp.DidNotReceiveWithAnyArgs().EvaluatePolicyAsync(default!, default!, default);
    }

    [Fact]
    public async Task Handle_WhenCallerIsAServiceIdentity_ShouldEvaluateThePolicy()
    {
        // Arrange
        var pdp = PdpReturning(MakeDecision(Effect.Permit));
        var behavior = CreateBehavior<TestPolicyCommand, string>(pdp);

        // Act
        var (result, nextCalled) = await SendAsync(behavior, TestIdentity.Service("billing-job"));

        // Assert
        result.IsRight.ShouldBeTrue("a service identity is evaluated like a user; the policy decides");
        nextCalled.ShouldBeTrue();
    }

    // -- Disabled mode --

    [Fact]
    public async Task Handle_WhenDisabled_ShouldSkipEvaluationAndCallNextStep()
    {
        // Arrange
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        var behavior = CreateBehavior<TestPolicyCommand, string>(
            pdp, new ABACOptions { EnforcementMode = ABACEnforcementMode.Disabled });

        // Act
        var (result, nextCalled) = await SendAsync(behavior);

        // Assert
        result.IsRight.ShouldBeTrue("Disabled mode must pass through to next step");
        nextCalled.ShouldBeTrue();
        await pdp.DidNotReceiveWithAnyArgs().EvaluatePolicyAsync(default!, default!, default);
    }

    // -- No ABAC attributes --

    [Fact]
    public async Task Handle_WhenRequestHasNoABACAttributes_ShouldSkipEvaluation()
    {
        // Arrange
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        var behavior = CreateBehavior<UnprotectedCommand, string>(pdp);
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("ok"));

        // Act
        var result = await behavior.Handle(new UnprotectedCommand("hello"), RequestContext.Create(), next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue("Undecorated request must pass through without evaluation");
        await pdp.DidNotReceiveWithAnyArgs().EvaluatePolicyAsync(default!, default!, default);
        await pdp.DidNotReceiveWithAnyArgs().EvaluateAsync(default!, default);
    }

    // -- Permit flow --

    [Fact]
    public async Task Handle_WhenNamedPolicyPermits_ShouldCallNextStepWithoutEvaluatingTheWholeStore()
    {
        // Arrange
        var pdp = PdpReturning(MakeDecision(Effect.Permit));
        var behavior = CreateBehavior<TestPolicyCommand, string>(pdp);

        // Act
        var (result, nextCalled) = await SendAsync(behavior);

        // Assert
        result.IsRight.ShouldBeTrue("Permit decision must allow the request to proceed");
        nextCalled.ShouldBeTrue();
        await pdp.DidNotReceiveWithAnyArgs().EvaluateAsync(default!, default);
    }

    // -- Deny flow --

    [Fact]
    public async Task Handle_WhenNamedPolicyDenies_InBlockMode_ShouldReturnAccessDeniedError()
    {
        // Arrange
        var behavior = CreateBehavior<TestPolicyCommand, string>(
            PdpReturning(MakeDecision(Effect.Deny, reason: "Policy denied")),
            new ABACOptions { EnforcementMode = ABACEnforcementMode.Block });

        // Act
        var (result, nextCalled) = await SendAsync(behavior);

        // Assert
        nextCalled.ShouldBeFalse("Deny in Block mode must NOT call next step");
        CodeOf(result).ShouldBe(ABACErrors.AccessDeniedCode);
    }

    [Fact]
    public async Task Handle_WhenNamedPolicyDenies_InWarnMode_ShouldCallNextStep()
    {
        // Arrange
        var behavior = CreateBehavior<TestPolicyCommand, string>(
            PdpReturning(MakeDecision(Effect.Deny)),
            new ABACOptions { EnforcementMode = ABACEnforcementMode.Warn });

        // Act
        var (result, nextCalled) = await SendAsync(behavior);

        // Assert
        result.IsRight.ShouldBeTrue("Warn mode must allow the request through despite Deny decision");
        nextCalled.ShouldBeTrue();
    }

    // -- A required policy that is NotApplicable denies --

    [Fact]
    public async Task Handle_WhenNamedPolicyIsNotApplicable_ShouldDeny()
    {
        // Arrange
        var behavior = CreateBehavior<TestPolicyCommand, string>(
            PdpReturning(MakeDecision(Effect.NotApplicable)),
            new ABACOptions { EnforcementMode = ABACEnforcementMode.Block });

        // Act
        var (result, nextCalled) = await SendAsync(behavior);

        // Assert
        nextCalled.ShouldBeFalse("A required policy that does not apply cannot authorize the request");
        CodeOf(result).ShouldBe(ABACErrors.AccessDeniedCode);
    }

    // -- A required policy that is missing --

    [Fact]
    public async Task Handle_WhenNamedPolicyIsMissing_ShouldReturnPolicyNotFound()
    {
        // Arrange
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluatePolicyAsync(Arg.Any<string>(), Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Left<EncinaError, PolicyDecision>(ABACErrors.PolicyNotFound("test-policy"))));
        var behavior = CreateBehavior<TestPolicyCommand, string>(pdp);

        // Act
        var (result, nextCalled) = await SendAsync(behavior);

        // Assert
        nextCalled.ShouldBeFalse();
        CodeOf(result).ShouldBe(ABACErrors.RequiredPolicyNotFoundCode);
    }

    // -- Indeterminate --

    [Fact]
    public async Task Handle_WhenIndeterminate_InBlockMode_ShouldReturnIndeterminateError()
    {
        // Arrange
        var behavior = CreateBehavior<TestPolicyCommand, string>(
            PdpReturning(MakeDecision(Effect.Indeterminate, reason: "evaluation error")),
            new ABACOptions { EnforcementMode = ABACEnforcementMode.Block });

        // Act
        var (result, _) = await SendAsync(behavior);

        // Assert
        CodeOf(result).ShouldBe(ABACErrors.IndeterminateCode);
    }

    [Fact]
    public async Task Handle_WhenIndeterminate_InWarnMode_ShouldStillDeny()
    {
        // Arrange
        var behavior = CreateBehavior<TestPolicyCommand, string>(
            PdpReturning(MakeDecision(Effect.Indeterminate, reason: "something weird")),
            new ABACOptions { EnforcementMode = ABACEnforcementMode.Warn });

        // Act
        var (result, nextCalled) = await SendAsync(behavior);

        // Assert: Warn relaxes only definite denials; an evaluation error denies in every mode.
        result.IsLeft.ShouldBeTrue("Indeterminate is an evaluation error and must deny in Warn mode too");
        nextCalled.ShouldBeFalse();
    }

    // -- Exception during evaluation --

    [Fact]
    public async Task Handle_WhenPdpThrows_ShouldReturnEvaluationFailedError()
    {
        // Arrange
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluatePolicyAsync(Arg.Any<string>(), Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, PolicyDecision>>>(_ => throw new InvalidOperationException("PDP crashed"));
        var behavior = CreateBehavior<TestPolicyCommand, string>(pdp);

        // Act
        var (result, _) = await SendAsync(behavior);

        // Assert
        CodeOf(result).ShouldBe(ABACErrors.EvaluationFailedCode);
    }

    // -- Obligation failure on Permit --

    [Fact]
    public async Task Handle_WhenPermitButObligationFails_ShouldReturnObligationFailedError()
    {
        // Arrange: no handlers registered => the obligation cannot be fulfilled
        var obligations = new List<Obligation>
        {
            new() { Id = "mandatory-audit", FulfillOn = FulfillOn.Permit, AttributeAssignments = [] }
        };
        var behavior = CreateBehavior<TestPolicyCommand, string>(
            PdpReturning(MakeDecision(Effect.Permit, obligations: obligations)));

        // Act
        var (result, _) = await SendAsync(behavior);

        // Assert -- per XACML 7.18, obligation failure on Permit => Deny
        CodeOf(result).ShouldBe(ABACErrors.ObligationFailedCode);
    }
}

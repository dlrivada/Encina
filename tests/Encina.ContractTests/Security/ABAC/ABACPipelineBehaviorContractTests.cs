using Encina.Security;
using Encina.Security.ABAC;
using Encina.Security.ABAC.Diagnostics;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.ContractTests.Security.ABAC;

/// <summary>
/// Behavioral contract tests for <see cref="ABACPipelineBehavior{TRequest, TResponse}"/>.
/// Executes real code paths through the pipeline behavior to verify the PEP enforcement
/// contracts: disabled mode bypass, permit flow, deny flow with enforcement modes,
/// not-applicable handling, indeterminate handling, obligation failure, and exception handling.
/// </summary>
[Trait("Category", "Contract")]
[Trait("Feature", "ABAC")]
public sealed class ABACPipelineBehaviorContractTests
{
    // -- Test request types --

    [RequirePolicy("test-policy")]
    private sealed record TestPolicyCommand(string Value) : ICommand<string>;

    private sealed record UnprotectedCommand(string Value) : ICommand<string>;

    // -- Helpers --

    private static PolicyDecision MakeDecision(
        Effect effect,
        string? policyId = "test-policy",
        string? reason = null,
        IReadOnlyList<Obligation>? obligations = null,
        IReadOnlyList<AdviceExpression>? advice = null) => new()
        {
            Effect = effect,
            PolicyId = policyId,
            Reason = reason,
            Obligations = obligations ?? [],
            Advice = advice ?? [],
            EvaluationDuration = TimeSpan.FromMilliseconds(1)
        };

    private static ABACPipelineBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>(
        IPolicyDecisionPoint pdp,
        ABACOptions? options = null,
        ObligationExecutor? obligationExecutor = null,
        ISecurityContextAccessor? securityContextAccessor = null,
        IAttributeProvider? attributeProvider = null)
        where TRequest : IRequest<TResponse>
    {
        var opts = options ?? new ABACOptions();
        var secCtx = securityContextAccessor ?? CreateSecurityContextAccessor();
        var attrProvider = attributeProvider ?? CreateAttributeProvider();
        var oblExec = obligationExecutor ?? new ObligationExecutor(
            [], NullLogger<ObligationExecutor>.Instance);

        return new ABACPipelineBehavior<TRequest, TResponse>(
            pdp,
            attrProvider,
            secCtx,
            oblExec,
            Options.Create(opts),
            NullLogger<ABACPipelineBehavior<TRequest, TResponse>>.Instance);
    }

    private static ISecurityContextAccessor CreateSecurityContextAccessor()
    {
        var secCtx = Substitute.For<ISecurityContext>();
        secCtx.UserId.Returns("test-user");

        var accessor = Substitute.For<ISecurityContextAccessor>();
        accessor.SecurityContext.Returns(secCtx);
        return accessor;
    }

    private static IAttributeProvider CreateAttributeProvider()
    {
        var provider = Substitute.For<IAttributeProvider>();
        provider.GetSubjectAttributesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object>());
        provider.GetResourceAttributesAsync<TestPolicyCommand>(Arg.Any<TestPolicyCommand>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object>());
        provider.GetEnvironmentAttributesAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object>());
        return provider;
    }

    // -- Disabled mode --

    [Fact]
    public async Task Handle_WhenDisabled_ShouldSkipEvaluationAndCallNextStep()
    {
        // Arrange
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        var options = new ABACOptions { EnforcementMode = ABACEnforcementMode.Disabled };
        var behavior = CreateBehavior<TestPolicyCommand, string>(pdp, options);
        var request = new TestPolicyCommand("hello");
        var context = RequestContext.Create();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("ok"));

        // Act
        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue("Disabled mode must pass through to next step");
        result.Match(Right: v => v.ShouldBe("ok"), Left: _ => throw new InvalidOperationException());
        await pdp.DidNotReceive().EvaluateAsync(Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>());
    }

    // -- No ABAC attributes --

    [Fact]
    public async Task Handle_WhenRequestHasNoABACAttributes_ShouldSkipEvaluation()
    {
        // Arrange
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        var behavior = CreateBehavior<UnprotectedCommand, string>(pdp);
        var request = new UnprotectedCommand("hello");
        var context = RequestContext.Create();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("ok"));

        // Act
        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue("Undecorated request must pass through without evaluation");
        await pdp.DidNotReceive().EvaluateAsync(Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>());
    }

    // -- Permit flow --

    [Fact]
    public async Task Handle_WhenPdpReturnsPermit_ShouldCallNextStep()
    {
        // Arrange
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluateAsync(Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(MakeDecision(Effect.Permit));

        var behavior = CreateBehavior<TestPolicyCommand, string>(pdp);
        var request = new TestPolicyCommand("hello");
        var context = RequestContext.Create();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("permitted"));

        // Act
        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue("Permit decision must allow the request to proceed");
        result.Match(Right: v => v.ShouldBe("permitted"), Left: _ => throw new InvalidOperationException());
    }

    // -- Deny flow (Block mode) --

    [Fact]
    public async Task Handle_WhenPdpReturnsDeny_InBlockMode_ShouldReturnAccessDeniedError()
    {
        // Arrange
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluateAsync(Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(MakeDecision(Effect.Deny, reason: "Policy denied"));

        var options = new ABACOptions { EnforcementMode = ABACEnforcementMode.Block };
        var behavior = CreateBehavior<TestPolicyCommand, string>(pdp, options);
        var request = new TestPolicyCommand("hello");
        var context = RequestContext.Create();
        var nextCalled = false;
        RequestHandlerCallback<string> next = () =>
        {
            nextCalled = true;
            return ValueTask.FromResult(Right<EncinaError, string>("should-not-reach"));
        };

        // Act
        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue("Deny in Block mode must return an error");
        nextCalled.ShouldBeFalse("Deny in Block mode must NOT call next step");
        _ = result.Match(
            Right: _ => throw new InvalidOperationException(),
            Left: err => err.GetCode().Match(
                Some: code => { code.ShouldBe(ABACErrors.AccessDeniedCode); return code; },
                None: () => throw new InvalidOperationException("Expected error code")));
    }

    // -- Deny flow (Warn mode) --

    [Fact]
    public async Task Handle_WhenPdpReturnsDeny_InWarnMode_ShouldCallNextStep()
    {
        // Arrange
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluateAsync(Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(MakeDecision(Effect.Deny));

        var options = new ABACOptions { EnforcementMode = ABACEnforcementMode.Warn };
        var behavior = CreateBehavior<TestPolicyCommand, string>(pdp, options);
        var request = new TestPolicyCommand("hello");
        var context = RequestContext.Create();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("warn-pass"));

        // Act
        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue("Warn mode must allow the request through despite Deny decision");
        result.Match(Right: v => v.ShouldBe("warn-pass"), Left: _ => throw new InvalidOperationException());
    }

    // -- NotApplicable with default Deny --

    [Fact]
    public async Task Handle_WhenNotApplicable_WithDefaultDeny_ShouldReturnError()
    {
        // Arrange
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluateAsync(Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(MakeDecision(Effect.NotApplicable));

        var options = new ABACOptions
        {
            EnforcementMode = ABACEnforcementMode.Block,
            DefaultNotApplicableEffect = Effect.Deny
        };
        var behavior = CreateBehavior<TestPolicyCommand, string>(pdp, options);
        var request = new TestPolicyCommand("hello");
        var context = RequestContext.Create();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("should-not-reach"));

        // Act
        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue("NotApplicable with default Deny must block in Block mode");
    }

    // -- NotApplicable with default Permit --

    [Fact]
    public async Task Handle_WhenNotApplicable_WithDefaultPermit_ShouldCallNextStep()
    {
        // Arrange
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluateAsync(Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(MakeDecision(Effect.NotApplicable));

        var options = new ABACOptions
        {
            EnforcementMode = ABACEnforcementMode.Block,
            DefaultNotApplicableEffect = Effect.Permit
        };
        var behavior = CreateBehavior<TestPolicyCommand, string>(pdp, options);
        var request = new TestPolicyCommand("hello");
        var context = RequestContext.Create();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("open-world"));

        // Act
        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue("NotApplicable with default Permit must allow the request");
        result.Match(Right: v => v.ShouldBe("open-world"), Left: _ => throw new InvalidOperationException());
    }

    // -- Indeterminate --

    [Fact]
    public async Task Handle_WhenIndeterminate_InBlockMode_ShouldReturnIndeterminateError()
    {
        // Arrange
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluateAsync(Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(MakeDecision(Effect.Indeterminate, reason: "evaluation error"));

        var options = new ABACOptions { EnforcementMode = ABACEnforcementMode.Block };
        var behavior = CreateBehavior<TestPolicyCommand, string>(pdp, options);
        var request = new TestPolicyCommand("hello");
        var context = RequestContext.Create();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("should-not-reach"));

        // Act
        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue("Indeterminate in Block mode must return an error");
        _ = result.Match(
            Right: _ => throw new InvalidOperationException(),
            Left: err => err.GetCode().Match(
                Some: code => { code.ShouldBe(ABACErrors.IndeterminateCode); return code; },
                None: () => throw new InvalidOperationException("Expected error code")));
    }

    // -- Exception during evaluation --

    [Fact]
    public async Task Handle_WhenPdpThrows_ShouldReturnEvaluationFailedError()
    {
        // Arrange
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluateAsync(Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns<PolicyDecision>(_ => throw new InvalidOperationException("PDP crashed"));

        var behavior = CreateBehavior<TestPolicyCommand, string>(pdp);
        var request = new TestPolicyCommand("hello");
        var context = RequestContext.Create();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("should-not-reach"));

        // Act
        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue("Exception during evaluation must return an error");
        _ = result.Match(
            Right: _ => throw new InvalidOperationException(),
            Left: err => err.GetCode().Match(
                Some: code => { code.ShouldBe(ABACErrors.EvaluationFailedCode); return code; },
                None: () => throw new InvalidOperationException("Expected error code")));
    }

    // -- Obligation failure on Permit --

    [Fact]
    public async Task Handle_WhenPermitButObligationFails_ShouldReturnObligationFailedError()
    {
        // Arrange
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        var obligations = new List<Obligation>
        {
            new()
            {
                Id = "mandatory-audit",
                FulfillOn = FulfillOn.Permit,
                AttributeAssignments = []
            }
        };
        pdp.EvaluateAsync(Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(MakeDecision(Effect.Permit, obligations: obligations));

        // No handlers registered => obligation cannot be fulfilled
        var obligationExecutor = new ObligationExecutor(
            [], NullLogger<ObligationExecutor>.Instance);

        var behavior = CreateBehavior<TestPolicyCommand, string>(
            pdp, obligationExecutor: obligationExecutor);
        var request = new TestPolicyCommand("hello");
        var context = RequestContext.Create();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("should-not-reach"));

        // Act
        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        // Assert -- per XACML 7.18, obligation failure on Permit => Deny
        result.IsLeft.ShouldBeTrue("Obligation failure on Permit must result in Deny");
        _ = result.Match(
            Right: _ => throw new InvalidOperationException(),
            Left: err => err.GetCode().Match(
                Some: code => { code.ShouldBe(ABACErrors.ObligationFailedCode); return code; },
                None: () => throw new InvalidOperationException("Expected error code")));
    }

    // -- Indeterminate in Warn mode --

    [Fact]
    public async Task Handle_WhenIndeterminate_InWarnMode_ShouldCallNextStep()
    {
        // Arrange
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluateAsync(Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(MakeDecision(Effect.Indeterminate, reason: "something weird"));

        var options = new ABACOptions { EnforcementMode = ABACEnforcementMode.Warn };
        var behavior = CreateBehavior<TestPolicyCommand, string>(pdp, options);
        var request = new TestPolicyCommand("hello");
        var context = RequestContext.Create();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("warn-pass"));

        // Act
        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue("Indeterminate in Warn mode must allow the request");
        result.Match(Right: v => v.ShouldBe("warn-pass"), Left: _ => throw new InvalidOperationException());
    }
}

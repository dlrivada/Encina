using Encina.Security.ABAC;
using Encina.Security.ABAC.Evaluation;

using FluentAssertions;

using LanguageExt;
using static LanguageExt.Prelude;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using ISecurityContextAccessor = global::Encina.Security.ISecurityContextAccessor;
using ISecurityContext = global::Encina.Security.ISecurityContext;

namespace Encina.UnitTests.Security.ABAC;

/// <summary>
/// Unit tests for <see cref="ABACPipelineBehavior{TRequest, TResponse}"/>: the XACML 3.0
/// Policy Enforcement Point (PEP) that intercepts requests, evaluates policies, executes
/// obligations, and enforces decisions.
/// </summary>
public sealed class ABACPipelineBehaviorTests
{
    #region Test Request Types

    /// <summary>
    /// Request type with ABAC policy attribute — triggers PDP evaluation.
    /// </summary>
    [RequirePolicy("test-policy")]
    private sealed record ProtectedRequest : IRequest<ProtectedResponse>;

    /// <summary>
    /// Request type without ABAC attributes — should be skipped.
    /// </summary>
    private sealed record UnprotectedRequest : IRequest<UnprotectedResponse>;

    private sealed record ProtectedResponse(string Value);
    private sealed record UnprotectedResponse(string Value);

    #endregion

    #region Helpers

    private static IPolicyDecisionPoint CreateMockPdp(PolicyDecision decision)
    {
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluateAsync(Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(decision);
        return pdp;
    }

    private static IAttributeProvider CreateMockAttributeProvider()
    {
        var provider = Substitute.For<IAttributeProvider>();
        provider.GetSubjectAttributesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object>());
        provider.GetResourceAttributesAsync<ProtectedRequest>(Arg.Any<ProtectedRequest>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object>());
        provider.GetEnvironmentAttributesAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object>());
        return provider;
    }

    private static ISecurityContextAccessor CreateMockSecurityContext()
    {
        var accessor = Substitute.For<ISecurityContextAccessor>();
        var secCtx = Substitute.For<ISecurityContext>();
        secCtx.UserId.Returns("test-user");
        accessor.SecurityContext.Returns(secCtx);
        return accessor;
    }

    private static ObligationExecutor CreateObligationExecutor(
        params IObligationHandler[] handlers)
    {
        return new ObligationExecutor(handlers, NullLogger<ObligationExecutor>.Instance);
    }

    private static IOptions<ABACOptions> CreateOptions(
        ABACEnforcementMode mode = ABACEnforcementMode.Block,
        Effect defaultNotApplicable = Effect.Deny,
        bool includeAdvice = true)
    {
        var options = new ABACOptions
        {
            EnforcementMode = mode,
            DefaultNotApplicableEffect = defaultNotApplicable,
            IncludeAdvice = includeAdvice
        };
        return Options.Create(options);
    }

    private static PolicyDecision PermitDecision(
        IReadOnlyList<Obligation>? obligations = null,
        IReadOnlyList<AdviceExpression>? advice = null) =>
        new()
        {
            Effect = Effect.Permit,
            Obligations = obligations ?? [],
            Advice = advice ?? [],
            EvaluationDuration = TimeSpan.FromMilliseconds(1)
        };

    private static PolicyDecision DenyDecision(
        string? reason = null,
        IReadOnlyList<Obligation>? obligations = null) =>
        new()
        {
            Effect = Effect.Deny,
            Obligations = obligations ?? [],
            Advice = [],
            EvaluationDuration = TimeSpan.FromMilliseconds(1),
            Reason = reason ?? "Access denied."
        };

    private static PolicyDecision NotApplicableDecision() =>
        new()
        {
            Effect = Effect.NotApplicable,
            Obligations = [],
            Advice = [],
            EvaluationDuration = TimeSpan.FromMilliseconds(1)
        };

    private static PolicyDecision IndeterminateDecision(string? reason = null) =>
        new()
        {
            Effect = Effect.Indeterminate,
            Obligations = [],
            Advice = [],
            EvaluationDuration = TimeSpan.FromMilliseconds(1),
            Reason = reason ?? "Evaluation error."
        };

    private static ABACPipelineBehavior<ProtectedRequest, ProtectedResponse> CreateBehavior(
        IPolicyDecisionPoint pdp,
        IOptions<ABACOptions>? options = null,
        ObligationExecutor? obligationExecutor = null)
    {
        return new ABACPipelineBehavior<ProtectedRequest, ProtectedResponse>(
            pdp,
            CreateMockAttributeProvider(),
            CreateMockSecurityContext(),
            obligationExecutor ?? CreateObligationExecutor(),
            options ?? CreateOptions(),
            NullLogger<ABACPipelineBehavior<ProtectedRequest, ProtectedResponse>>.Instance);
    }

    private static RequestHandlerCallback<ProtectedResponse> SuccessCallback() =>
        () => new ValueTask<Either<EncinaError, ProtectedResponse>>(
            Either<EncinaError, ProtectedResponse>.Right(new ProtectedResponse("ok")));

    private static IRequestContext MockContext() => Substitute.For<IRequestContext>();

    #endregion

    #region Disabled Mode

    [Fact]
    public async Task Handle_DisabledMode_SkipsEvaluation()
    {
        var pdp = CreateMockPdp(PermitDecision());
        var options = CreateOptions(mode: ABACEnforcementMode.Disabled);
        var behavior = CreateBehavior(pdp, options);

        var result = await behavior.Handle(
            new ProtectedRequest(), MockContext(), SuccessCallback(), CancellationToken.None);

        result.IsRight.Should().BeTrue();
        await pdp.DidNotReceive()
            .EvaluateAsync(Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Unprotected Request

    [Fact]
    public async Task Handle_NoABACAttributes_SkipsEvaluation()
    {
        var pdp = CreateMockPdp(PermitDecision());
        var attributeProvider = Substitute.For<IAttributeProvider>();
        var securityContextAccessor = CreateMockSecurityContext();
        var obligationExecutor = CreateObligationExecutor();
        var options = CreateOptions();
        var logger = NullLogger<ABACPipelineBehavior<UnprotectedRequest, UnprotectedResponse>>.Instance;

        var behavior = new ABACPipelineBehavior<UnprotectedRequest, UnprotectedResponse>(
            pdp, attributeProvider, securityContextAccessor, obligationExecutor, options, logger);

        RequestHandlerCallback<UnprotectedResponse> callback = () =>
            new ValueTask<Either<EncinaError, UnprotectedResponse>>(
                Either<EncinaError, UnprotectedResponse>.Right(new UnprotectedResponse("ok")));

        var result = await behavior.Handle(
            new UnprotectedRequest(), MockContext(), callback, CancellationToken.None);

        result.IsRight.Should().BeTrue();
        await pdp.DidNotReceive()
            .EvaluateAsync(Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Permit Decision

    [Fact]
    public async Task Handle_PermitDecision_CallsNextStep()
    {
        var pdp = CreateMockPdp(PermitDecision());
        var behavior = CreateBehavior(pdp);

        var result = await behavior.Handle(
            new ProtectedRequest(), MockContext(), SuccessCallback(), CancellationToken.None);

        result.IsRight.Should().BeTrue();
        result.Match(Left: _ => null!, Right: r => r).Value.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_PermitWithObligations_ExecutesObligations()
    {
        var handler = new TestObligationHandler("log-access", success: true);
        var obligations = new List<Obligation>
        {
            new() { Id = "log-access", FulfillOn = FulfillOn.Permit, AttributeAssignments = [] }
        };
        var pdp = CreateMockPdp(PermitDecision(obligations: obligations));
        var behavior = CreateBehavior(pdp, obligationExecutor: CreateObligationExecutor(handler));

        var result = await behavior.Handle(
            new ProtectedRequest(), MockContext(), SuccessCallback(), CancellationToken.None);

        result.IsRight.Should().BeTrue();
        handler.Invocations.Should().Be(1);
    }

    [Fact]
    public async Task Handle_PermitObligationFails_ReturnsDeny()
    {
        var handler = new TestObligationHandler("critical-audit", success: false);
        var obligations = new List<Obligation>
        {
            new() { Id = "critical-audit", FulfillOn = FulfillOn.Permit, AttributeAssignments = [] }
        };
        var pdp = CreateMockPdp(PermitDecision(obligations: obligations));
        var behavior = CreateBehavior(pdp, obligationExecutor: CreateObligationExecutor(handler));

        var result = await behavior.Handle(
            new ProtectedRequest(), MockContext(), SuccessCallback(), CancellationToken.None);

        result.IsLeft.Should().BeTrue("failed obligation overrides Permit to Deny per XACML §7.18");
    }

    #endregion

    #region Deny Decision

    [Fact]
    public async Task Handle_DenyDecision_BlockMode_ReturnsLeft()
    {
        var pdp = CreateMockPdp(DenyDecision());
        var options = CreateOptions(mode: ABACEnforcementMode.Block);
        var behavior = CreateBehavior(pdp, options);

        var result = await behavior.Handle(
            new ProtectedRequest(), MockContext(), SuccessCallback(), CancellationToken.None);

        result.IsLeft.Should().BeTrue("Block mode should deny access");
    }

    [Fact]
    public async Task Handle_DenyDecision_WarnMode_ReturnsRight()
    {
        var pdp = CreateMockPdp(DenyDecision());
        var options = CreateOptions(mode: ABACEnforcementMode.Warn);
        var behavior = CreateBehavior(pdp, options);

        var result = await behavior.Handle(
            new ProtectedRequest(), MockContext(), SuccessCallback(), CancellationToken.None);

        result.IsRight.Should().BeTrue("Warn mode should allow access despite Deny decision");
    }

    #endregion

    #region NotApplicable Decision

    [Fact]
    public async Task Handle_NotApplicable_DefaultDeny_ReturnsLeft()
    {
        var pdp = CreateMockPdp(NotApplicableDecision());
        var options = CreateOptions(defaultNotApplicable: Effect.Deny);
        var behavior = CreateBehavior(pdp, options);

        var result = await behavior.Handle(
            new ProtectedRequest(), MockContext(), SuccessCallback(), CancellationToken.None);

        result.IsLeft.Should().BeTrue("NotApplicable with default=Deny should deny access");
    }

    [Fact]
    public async Task Handle_NotApplicable_DefaultPermit_ReturnsRight()
    {
        var pdp = CreateMockPdp(NotApplicableDecision());
        var options = CreateOptions(defaultNotApplicable: Effect.Permit);
        var behavior = CreateBehavior(pdp, options);

        var result = await behavior.Handle(
            new ProtectedRequest(), MockContext(), SuccessCallback(), CancellationToken.None);

        result.IsRight.Should().BeTrue("NotApplicable with default=Permit should allow access");
    }

    [Fact]
    public async Task Handle_NotApplicable_WarnMode_ReturnsRight()
    {
        var pdp = CreateMockPdp(NotApplicableDecision());
        var options = CreateOptions(
            mode: ABACEnforcementMode.Warn,
            defaultNotApplicable: Effect.Deny);
        var behavior = CreateBehavior(pdp, options);

        var result = await behavior.Handle(
            new ProtectedRequest(), MockContext(), SuccessCallback(), CancellationToken.None);

        result.IsRight.Should().BeTrue("Warn mode overrides even NotApplicable→Deny");
    }

    #endregion

    #region Indeterminate Decision

    [Fact]
    public async Task Handle_Indeterminate_BlockMode_ReturnsLeft()
    {
        var pdp = CreateMockPdp(IndeterminateDecision());
        var options = CreateOptions(mode: ABACEnforcementMode.Block);
        var behavior = CreateBehavior(pdp, options);

        var result = await behavior.Handle(
            new ProtectedRequest(), MockContext(), SuccessCallback(), CancellationToken.None);

        result.IsLeft.Should().BeTrue("Indeterminate should deny in Block mode");
    }

    [Fact]
    public async Task Handle_Indeterminate_WarnMode_ReturnsRight()
    {
        var pdp = CreateMockPdp(IndeterminateDecision());
        var options = CreateOptions(mode: ABACEnforcementMode.Warn);
        var behavior = CreateBehavior(pdp, options);

        var result = await behavior.Handle(
            new ProtectedRequest(), MockContext(), SuccessCallback(), CancellationToken.None);

        result.IsRight.Should().BeTrue("Warn mode should allow access despite Indeterminate");
    }

    #endregion

    #region PDP Exception Handling

    [Fact]
    public async Task Handle_PdpThrows_ReturnsLeft()
    {
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluateAsync(Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns<PolicyDecision>(_ => throw new InvalidOperationException("PDP internal error"));

        var behavior = CreateBehavior(pdp);

        var result = await behavior.Handle(
            new ProtectedRequest(), MockContext(), SuccessCallback(), CancellationToken.None);

        result.IsLeft.Should().BeTrue("PDP exception should produce Indeterminate → Left");
    }

    #endregion

    #region Guard Clauses

    [Fact]
    public void Constructor_NullPdp_Throws()
    {
        var act = () => new ABACPipelineBehavior<ProtectedRequest, ProtectedResponse>(
            null!,
            CreateMockAttributeProvider(),
            CreateMockSecurityContext(),
            CreateObligationExecutor(),
            CreateOptions(),
            NullLogger<ABACPipelineBehavior<ProtectedRequest, ProtectedResponse>>.Instance);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullAttributeProvider_Throws()
    {
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        var act = () => new ABACPipelineBehavior<ProtectedRequest, ProtectedResponse>(
            pdp,
            null!,
            CreateMockSecurityContext(),
            CreateObligationExecutor(),
            CreateOptions(),
            NullLogger<ABACPipelineBehavior<ProtectedRequest, ProtectedResponse>>.Instance);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullSecurityContextAccessor_Throws()
    {
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        var act = () => new ABACPipelineBehavior<ProtectedRequest, ProtectedResponse>(
            pdp,
            CreateMockAttributeProvider(),
            null!,
            CreateObligationExecutor(),
            CreateOptions(),
            NullLogger<ABACPipelineBehavior<ProtectedRequest, ProtectedResponse>>.Instance);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullObligationExecutor_Throws()
    {
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        var act = () => new ABACPipelineBehavior<ProtectedRequest, ProtectedResponse>(
            pdp,
            CreateMockAttributeProvider(),
            CreateMockSecurityContext(),
            null!,
            CreateOptions(),
            NullLogger<ABACPipelineBehavior<ProtectedRequest, ProtectedResponse>>.Instance);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullOptions_Throws()
    {
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        var act = () => new ABACPipelineBehavior<ProtectedRequest, ProtectedResponse>(
            pdp,
            CreateMockAttributeProvider(),
            CreateMockSecurityContext(),
            CreateObligationExecutor(),
            null!,
            NullLogger<ABACPipelineBehavior<ProtectedRequest, ProtectedResponse>>.Instance);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        var act = () => new ABACPipelineBehavior<ProtectedRequest, ProtectedResponse>(
            pdp,
            CreateMockAttributeProvider(),
            CreateMockSecurityContext(),
            CreateObligationExecutor(),
            CreateOptions(),
            null!);

        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Test Helper — ObligationHandler

    private sealed class TestObligationHandler(string obligationId, bool success) : IObligationHandler
    {
        public int Invocations { get; private set; }

        public bool CanHandle(string id) => id == obligationId;

        public ValueTask<Either<EncinaError, Unit>> HandleAsync(
            Obligation obligation,
            PolicyEvaluationContext context,
            CancellationToken cancellationToken)
        {
            Invocations++;

            return success
                ? new(Either<EncinaError, Unit>.Right(unit))
                : new(Either<EncinaError, Unit>.Left(
                    EncinaError.New($"Handler for '{obligation.Id}' failed.")));
        }
    }

    #endregion
}

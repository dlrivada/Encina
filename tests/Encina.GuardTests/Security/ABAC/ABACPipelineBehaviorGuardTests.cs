#pragma warning disable CA2012 // Use ValueTasks correctly -- NSubstitute mock setup pattern

using Encina.Security.ABAC;

using Shouldly;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Encina.GuardTests.Security.ABAC;

/// <summary>
/// Guard clause tests for <see cref="ABACPipelineBehavior{TRequest, TResponse}"/>.
/// Verifies constructor parameter validation and Handle method guards.
/// </summary>
public class ABACPipelineBehaviorGuardTests
{
    // Dummy request/response types for generic instantiation
    [RequirePolicy("test-policy")]
    private sealed record TestRequest : IRequest<string>;

    #region Constructor Guards

    [Fact]
    public void Constructor_NullPdp_ThrowsArgumentNullException()
    {
        var act = () => new ABACPipelineBehavior<TestRequest, string>(
            null!,
            Substitute.For<IAttributeProvider>(),
            Substitute.For<global::Encina.Security.ISecurityContextAccessor>(),
            CreateObligationExecutor(),
            Options.Create(new ABACOptions()),
            NullLoggerFactory.Instance.CreateLogger<ABACPipelineBehavior<TestRequest, string>>());

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("pdp");
    }

    [Fact]
    public void Constructor_NullAttributeProvider_ThrowsArgumentNullException()
    {
        var act = () => new ABACPipelineBehavior<TestRequest, string>(
            Substitute.For<IPolicyDecisionPoint>(),
            null!,
            Substitute.For<global::Encina.Security.ISecurityContextAccessor>(),
            CreateObligationExecutor(),
            Options.Create(new ABACOptions()),
            NullLoggerFactory.Instance.CreateLogger<ABACPipelineBehavior<TestRequest, string>>());

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("attributeProvider");
    }

    [Fact]
    public void Constructor_NullSecurityContextAccessor_ThrowsArgumentNullException()
    {
        var act = () => new ABACPipelineBehavior<TestRequest, string>(
            Substitute.For<IPolicyDecisionPoint>(),
            Substitute.For<IAttributeProvider>(),
            null!,
            CreateObligationExecutor(),
            Options.Create(new ABACOptions()),
            NullLoggerFactory.Instance.CreateLogger<ABACPipelineBehavior<TestRequest, string>>());

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("securityContextAccessor");
    }

    [Fact]
    public void Constructor_NullObligationExecutor_ThrowsArgumentNullException()
    {
        var act = () => new ABACPipelineBehavior<TestRequest, string>(
            Substitute.For<IPolicyDecisionPoint>(),
            Substitute.For<IAttributeProvider>(),
            Substitute.For<global::Encina.Security.ISecurityContextAccessor>(),
            null!,
            Options.Create(new ABACOptions()),
            NullLoggerFactory.Instance.CreateLogger<ABACPipelineBehavior<TestRequest, string>>());

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("obligationExecutor");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new ABACPipelineBehavior<TestRequest, string>(
            Substitute.For<IPolicyDecisionPoint>(),
            Substitute.For<IAttributeProvider>(),
            Substitute.For<global::Encina.Security.ISecurityContextAccessor>(),
            CreateObligationExecutor(),
            null!,
            NullLoggerFactory.Instance.CreateLogger<ABACPipelineBehavior<TestRequest, string>>());

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new ABACPipelineBehavior<TestRequest, string>(
            Substitute.For<IPolicyDecisionPoint>(),
            Substitute.For<IAttributeProvider>(),
            Substitute.For<global::Encina.Security.ISecurityContextAccessor>(),
            CreateObligationExecutor(),
            Options.Create(new ABACOptions()),
            null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_AllValidParameters_DoesNotThrow()
    {
        var act = () => CreateBehavior();

        Should.NotThrow(act);
    }

    #endregion

    #region Handle — Disabled Mode Skips Evaluation

    [Fact]
    public async Task Handle_DisabledMode_CallsNextStepWithoutEvaluation()
    {
        // Arrange
        var options = new ABACOptions { EnforcementMode = ABACEnforcementMode.Disabled };
        var sut = CreateBehavior(abacOptions: options);
        var request = new TestRequest();
        var context = Substitute.For<IRequestContext>();
        var called = false;
        RequestHandlerCallback<string> next = () =>
        {
            called = true;
            return ValueTask.FromResult(LanguageExt.Prelude.Right<EncinaError, string>("ok"));
        };

        // Act
        var result = await sut.Handle(request, context, next, CancellationToken.None);

        // Assert
        called.ShouldBeTrue();
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region Handle — Permit Decision Calls Next (no obligations)

    [Fact]
    public async Task Handle_PermitDecision_NoObligations_ExecutesNextStep()
    {
        // Arrange
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluateAsync(Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(new PolicyDecision
            {
                Effect = Effect.Permit,
                PolicyId = "test-policy",
                Obligations = [],
                Advice = [],
                EvaluationDuration = TimeSpan.FromMilliseconds(1)
            }));

        var sut = CreateBehavior(pdp: pdp);
        var request = new TestRequest();
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () =>
            ValueTask.FromResult(LanguageExt.Prelude.Right<EncinaError, string>("permitted"));

        // Act
        var result = await sut.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region Handle — Deny Decision in Block Mode

    [Fact]
    public async Task Handle_DenyDecision_BlockMode_ReturnsError()
    {
        // Arrange
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluateAsync(Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(new PolicyDecision
            {
                Effect = Effect.Deny,
                PolicyId = "deny-policy",
                Reason = "Unauthorized",
                Obligations = [],
                Advice = [],
                EvaluationDuration = TimeSpan.FromMilliseconds(1)
            }));

        var options = new ABACOptions { EnforcementMode = ABACEnforcementMode.Block };
        var sut = CreateBehavior(pdp: pdp, abacOptions: options);
        var request = new TestRequest();
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () =>
            ValueTask.FromResult(LanguageExt.Prelude.Right<EncinaError, string>("should-not-reach"));

        // Act
        var result = await sut.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Handle — Deny Decision in Warn Mode

    [Fact]
    public async Task Handle_DenyDecision_WarnMode_CallsNextStep()
    {
        // Arrange
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluateAsync(Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(new PolicyDecision
            {
                Effect = Effect.Deny,
                PolicyId = "deny-policy",
                Reason = "Unauthorized",
                Obligations = [],
                Advice = [],
                EvaluationDuration = TimeSpan.FromMilliseconds(1)
            }));

        var options = new ABACOptions { EnforcementMode = ABACEnforcementMode.Warn };
        var sut = CreateBehavior(pdp: pdp, abacOptions: options);
        var request = new TestRequest();
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () =>
            ValueTask.FromResult(LanguageExt.Prelude.Right<EncinaError, string>("warned-but-allowed"));

        // Act
        var result = await sut.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region Handle — NotApplicable With Default Deny

    [Fact]
    public async Task Handle_NotApplicable_DefaultDeny_BlockMode_ReturnsError()
    {
        // Arrange
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluateAsync(Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(new PolicyDecision
            {
                Effect = Effect.NotApplicable,
                PolicyId = null,
                Obligations = [],
                Advice = [],
                EvaluationDuration = TimeSpan.FromMilliseconds(1)
            }));

        var options = new ABACOptions
        {
            EnforcementMode = ABACEnforcementMode.Block,
            DefaultNotApplicableEffect = Effect.Deny
        };
        var sut = CreateBehavior(pdp: pdp, abacOptions: options);
        var request = new TestRequest();
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () =>
            ValueTask.FromResult(LanguageExt.Prelude.Right<EncinaError, string>("should-not-reach"));

        // Act
        var result = await sut.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Handle — NotApplicable With Default Permit

    [Fact]
    public async Task Handle_NotApplicable_DefaultPermit_CallsNextStep()
    {
        // Arrange
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluateAsync(Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(new PolicyDecision
            {
                Effect = Effect.NotApplicable,
                PolicyId = null,
                Obligations = [],
                Advice = [],
                EvaluationDuration = TimeSpan.FromMilliseconds(1)
            }));

        var options = new ABACOptions
        {
            EnforcementMode = ABACEnforcementMode.Block,
            DefaultNotApplicableEffect = Effect.Permit
        };
        var sut = CreateBehavior(pdp: pdp, abacOptions: options);
        var request = new TestRequest();
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () =>
            ValueTask.FromResult(LanguageExt.Prelude.Right<EncinaError, string>("permitted-na"));

        // Act
        var result = await sut.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region Handle — Indeterminate Decision

    [Fact]
    public async Task Handle_IndeterminateDecision_BlockMode_ReturnsError()
    {
        // Arrange
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluateAsync(Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(new PolicyDecision
            {
                Effect = Effect.Indeterminate,
                PolicyId = null,
                Reason = "Evaluation error",
                Obligations = [],
                Advice = [],
                EvaluationDuration = TimeSpan.FromMilliseconds(1)
            }));

        var options = new ABACOptions { EnforcementMode = ABACEnforcementMode.Block };
        var sut = CreateBehavior(pdp: pdp, abacOptions: options);
        var request = new TestRequest();
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () =>
            ValueTask.FromResult(LanguageExt.Prelude.Right<EncinaError, string>("should-not-reach"));

        // Act
        var result = await sut.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Handle — PDP Exception Returns Error

    [Fact]
    public async Task Handle_PdpThrows_ReturnsEvaluationFailedError()
    {
        // Arrange
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluateAsync(Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<PolicyDecision>>(_ => throw new InvalidOperationException("PDP crash"));

        var sut = CreateBehavior(pdp: pdp);
        var request = new TestRequest();
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () =>
            ValueTask.FromResult(LanguageExt.Prelude.Right<EncinaError, string>("should-not-reach"));

        // Act
        var result = await sut.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Handle — Permit With Obligations (real executor, no handlers = obligation fails)

    [Fact]
    public async Task Handle_PermitWithObligations_NoHandlers_ReturnsDeny()
    {
        // Arrange: PDP returns Permit with an obligation, but no handlers registered
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluateAsync(Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(new PolicyDecision
            {
                Effect = Effect.Permit,
                PolicyId = "test-policy",
                Obligations = [new Obligation { Id = "ob-1", FulfillOn = FulfillOn.Permit, AttributeAssignments = [] }],
                Advice = [],
                EvaluationDuration = TimeSpan.FromMilliseconds(1)
            }));

        // FailOnMissingObligationHandler = true by default, so missing handler = obligation failure = deny
        var options = new ABACOptions { EnforcementMode = ABACEnforcementMode.Block };
        var sut = CreateBehavior(pdp: pdp, abacOptions: options);
        var request = new TestRequest();
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () =>
            ValueTask.FromResult(LanguageExt.Prelude.Right<EncinaError, string>("should-not-reach"));

        // Act
        var result = await sut.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue("obligation failure should deny even when PDP says Permit per XACML 7.18");
    }

    #endregion

    // ── Helpers ──────────────────────────────────────────────────────

    private static ObligationExecutor CreateObligationExecutor()
    {
        return new ObligationExecutor(
            Enumerable.Empty<IObligationHandler>(),
            NullLoggerFactory.Instance.CreateLogger<ObligationExecutor>());
    }

    private static ABACPipelineBehavior<TestRequest, string> CreateBehavior(
        IPolicyDecisionPoint? pdp = null,
        ABACOptions? abacOptions = null)
    {
        pdp ??= Substitute.For<IPolicyDecisionPoint>();
        var attributeProvider = CreateDefaultAttributeProvider();
        var securityContextAccessor = Substitute.For<global::Encina.Security.ISecurityContextAccessor>();
        var obligationExecutor = CreateObligationExecutor();
        var effectiveOptions = abacOptions ?? new ABACOptions();
        var options = Options.Create(effectiveOptions);
        var logger = NullLoggerFactory.Instance.CreateLogger<ABACPipelineBehavior<TestRequest, string>>();

        return new ABACPipelineBehavior<TestRequest, string>(
            pdp, attributeProvider, securityContextAccessor, obligationExecutor, options, logger);
    }

    private static IAttributeProvider CreateDefaultAttributeProvider()
    {
        var provider = Substitute.For<IAttributeProvider>();
        provider.GetSubjectAttributesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<IReadOnlyDictionary<string, object>>(
                new Dictionary<string, object>()));
        provider.GetResourceAttributesAsync<TestRequest>(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<IReadOnlyDictionary<string, object>>(
                new Dictionary<string, object>()));
        provider.GetEnvironmentAttributesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<IReadOnlyDictionary<string, object>>(
                new Dictionary<string, object>()));
        return provider;
    }
}

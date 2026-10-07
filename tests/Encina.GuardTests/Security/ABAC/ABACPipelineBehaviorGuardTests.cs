#pragma warning disable CA2012 // Use ValueTasks correctly -- NSubstitute mock setup pattern

using Encina.Security.ABAC;
using Encina.Security.ABAC.EEL;
using Encina.Testing.Identity;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Security.ABAC;

/// <summary>
/// Guard clause tests for <see cref="ABACPipelineBehavior{TRequest, TResponse}"/>.
/// Verifies constructor parameter validation and Handle method guards. The caller comes from the
/// <see cref="IRequestContext"/> that <c>Handle</c> receives (#1705 Phase 4).
/// </summary>
public class ABACPipelineBehaviorGuardTests
{
    private static readonly EELCompiler Compiler = new();

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
            CreateObligationExecutor(),
            Compiler,
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
            CreateObligationExecutor(),
            Compiler,
            Options.Create(new ABACOptions()),
            NullLoggerFactory.Instance.CreateLogger<ABACPipelineBehavior<TestRequest, string>>());

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("attributeProvider");
    }

    [Fact]
    public void Constructor_NullObligationExecutor_ThrowsArgumentNullException()
    {
        var act = () => new ABACPipelineBehavior<TestRequest, string>(
            Substitute.For<IPolicyDecisionPoint>(),
            Substitute.For<IAttributeProvider>(),
            null!,
            Compiler,
            Options.Create(new ABACOptions()),
            NullLoggerFactory.Instance.CreateLogger<ABACPipelineBehavior<TestRequest, string>>());

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("obligationExecutor");
    }

    [Fact]
    public void Constructor_NullEelCompiler_ThrowsArgumentNullException()
    {
        var act = () => new ABACPipelineBehavior<TestRequest, string>(
            Substitute.For<IPolicyDecisionPoint>(),
            Substitute.For<IAttributeProvider>(),
            CreateObligationExecutor(),
            null!,
            Options.Create(new ABACOptions()),
            NullLoggerFactory.Instance.CreateLogger<ABACPipelineBehavior<TestRequest, string>>());

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("eelCompiler");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new ABACPipelineBehavior<TestRequest, string>(
            Substitute.For<IPolicyDecisionPoint>(),
            Substitute.For<IAttributeProvider>(),
            CreateObligationExecutor(),
            Compiler,
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
            CreateObligationExecutor(),
            Compiler,
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
        var context = TestRequestContext.For(TestIdentity.Anonymous);
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

    #region Handle — Unauthenticated Caller

    [Theory]
    [InlineData(ABACEnforcementMode.Block)]
    [InlineData(ABACEnforcementMode.Warn)]
    public async Task Handle_AnonymousCaller_DeniesWithTheSharedUnauthenticatedCode(ABACEnforcementMode mode)
    {
        var sut = CreateBehavior(pdp: PdpReturning(Effect.Permit), abacOptions: new ABACOptions { EnforcementMode = mode });
        RequestHandlerCallback<string> next = () =>
            ValueTask.FromResult(LanguageExt.Prelude.Right<EncinaError, string>("should-not-reach"));

        var result = await sut.Handle(new TestRequest(), TestRequestContext.For(TestIdentity.Anonymous), next, CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.GetCode().IfNone(string.Empty).ShouldBe(EncinaErrorCodes.AuthorizationUnauthenticated);
            error.GetDetails()["gate"].ShouldBe("abac");
        });
    }

    #endregion

    #region Handle — Named Policy Decisions

    [Theory]
    [InlineData(Effect.Permit, ABACEnforcementMode.Block, true)]
    [InlineData(Effect.Deny, ABACEnforcementMode.Block, false)]
    [InlineData(Effect.Deny, ABACEnforcementMode.Warn, true)]
    [InlineData(Effect.NotApplicable, ABACEnforcementMode.Block, false)]
    [InlineData(Effect.Indeterminate, ABACEnforcementMode.Block, false)]
    public async Task Handle_NamedPolicyDecision_IsEnforced(Effect effect, ABACEnforcementMode mode, bool proceeds)
    {
        // Arrange
        var pdp = PdpReturning(effect);
        var sut = CreateBehavior(pdp: pdp, abacOptions: new ABACOptions { EnforcementMode = mode });
        RequestHandlerCallback<string> next = () =>
            ValueTask.FromResult(LanguageExt.Prelude.Right<EncinaError, string>("reached"));

        // Act
        var result = await sut.Handle(new TestRequest(), UserContext(), next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBe(proceeds);
    }

    [Fact]
    public async Task Handle_PdpThrows_ReturnsEvaluationFailedError()
    {
        // Arrange
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluatePolicyAsync(Arg.Any<string>(), Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, PolicyDecision>>>(_ => throw new InvalidOperationException("PDP crash"));

        var sut = CreateBehavior(pdp: pdp);
        RequestHandlerCallback<string> next = () =>
            ValueTask.FromResult(LanguageExt.Prelude.Right<EncinaError, string>("should-not-reach"));

        // Act
        var result = await sut.Handle(new TestRequest(), UserContext(), next, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_PermitWithObligations_NoHandlers_ReturnsDeny()
    {
        // Arrange: the named policy permits with an obligation, but no handlers are registered
        var pdp = PdpReturning(Effect.Permit,
            [new Obligation { Id = "ob-1", FulfillOn = FulfillOn.Permit, AttributeAssignments = [] }]);

        var sut = CreateBehavior(pdp: pdp, abacOptions: new ABACOptions { EnforcementMode = ABACEnforcementMode.Block });
        RequestHandlerCallback<string> next = () =>
            ValueTask.FromResult(LanguageExt.Prelude.Right<EncinaError, string>("should-not-reach"));

        // Act
        var result = await sut.Handle(new TestRequest(), UserContext(), next, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue("obligation failure should deny even when the policy permits per XACML 7.18");
    }

    #endregion

    // ── Helpers ──────────────────────────────────────────────────────

    // An authenticated user: an anonymous caller denies before evaluation (#1676, #1705).
    private static IRequestContext UserContext() => TestRequestContext.For(TestIdentity.User("guard-user"));

    private static IPolicyDecisionPoint PdpReturning(Effect effect, IReadOnlyList<Obligation>? obligations = null)
    {
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluatePolicyAsync("test-policy", Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(LanguageExt.Prelude.Right<EncinaError, PolicyDecision>(new PolicyDecision
            {
                Effect = effect,
                PolicyId = "test-policy",
                Obligations = obligations ?? [],
                Advice = [],
                EvaluationDuration = TimeSpan.FromMilliseconds(1)
            })));
        return pdp;
    }

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
        var obligationExecutor = CreateObligationExecutor();
        var effectiveOptions = abacOptions ?? new ABACOptions();
        var options = Options.Create(effectiveOptions);
        var logger = NullLoggerFactory.Instance.CreateLogger<ABACPipelineBehavior<TestRequest, string>>();

        return new ABACPipelineBehavior<TestRequest, string>(
            pdp, attributeProvider, obligationExecutor, Compiler, options, logger);
    }

    private static IAttributeProvider CreateDefaultAttributeProvider()
    {
        var provider = Substitute.For<IAttributeProvider>();
        provider.GetSubjectAttributesAsync(Arg.Any<RequestIdentity>(), Arg.Any<CancellationToken>())
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

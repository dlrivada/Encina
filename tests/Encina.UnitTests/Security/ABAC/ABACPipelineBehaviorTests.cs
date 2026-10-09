#pragma warning disable CA2012 // Use ValueTasks correctly -- NSubstitute mock setup pattern

using System.Diagnostics;

using Encina.Security.ABAC;
using Encina.Security.ABAC.DecisionAudit;
using Encina.Security.ABAC.EEL;
using Encina.Testing.Identity;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.ABAC;

/// <summary>
/// Unit tests for <see cref="ABACPipelineBehavior{TRequest, TResponse}"/>: the XACML 3.0
/// Policy Enforcement Point (PEP). The decision comes from the policies named by
/// <see cref="RequirePolicyAttribute"/> and the expressions of <see cref="RequireConditionAttribute"/>
/// (#1634); the PDP is mocked per named policy.
/// </summary>
[Collection(ABACActivityListenerIsolation.Name)]
public sealed class ABACPipelineBehaviorTests
{
    private static readonly EELCompiler Compiler = new();

    #region Test Request Types

    [RequirePolicy("policy-a")]
    private sealed record PolicyARequest : IRequest<string>;

    [RequirePolicy("policy-a")]
    [RequirePolicy("policy-b")]
    private sealed record AllOfABRequest : IRequest<string>;

    [RequirePolicy("policy-a", AllMustPass = false)]
    [RequirePolicy("policy-b", AllMustPass = false)]
    private sealed record AnyOfABRequest : IRequest<string>;

    [RequirePolicy("policy-a")]
    [RequirePolicy("policy-b", AllMustPass = false)]
    [RequirePolicy("policy-c", AllMustPass = false)]
    private sealed record MixedRequest : IRequest<string>;

    [RequirePolicy("")]
    private sealed record EmptyPolicyNameRequest : IRequest<string>;

    [RequireCondition("user.department == \"HR\"")]
    private sealed record HrConditionRequest : IRequest<string>;

    [RequireCondition("user.department == \"HR\"")]
    [RequireCondition("action.name == \"TwoConditionsRequest\"")]
    private sealed record TwoConditionsRequest : IRequest<string>;

    [RequireCondition("this is ((( not C#")]
    private sealed record UncompilableConditionRequest : IRequest<string>;

    [RequireCondition("user.missingAttribute == 1")]
    private sealed record ThrowingConditionRequest : IRequest<string>;

    [RequirePolicy("policy-a")]
    [RequireCondition("user.department == \"HR\"")]
    private sealed record PolicyAndConditionRequest : IRequest<string>;

    [RequireCondition("resource.owner == \"alice\"")]
    private sealed record OwnedByAliceRequest(string Owner) : IRequest<string>;

    private sealed record UnprotectedRequest : IRequest<string>;

    #endregion

    #region Helpers

    private static readonly Dictionary<string, object> HrSubject = new() { ["department"] = "HR" };
    private static readonly Dictionary<string, object> FinanceSubject = new() { ["department"] = "Finance" };

    private static PolicyDecision Decision(
        Effect effect,
        IReadOnlyList<Obligation>? obligations = null,
        IReadOnlyList<AdviceExpression>? advice = null) => new()
        {
            Effect = effect,
            Obligations = obligations ?? [],
            Advice = advice ?? [],
            EvaluationDuration = TimeSpan.FromMilliseconds(1)
        };

    /// <summary>A PDP that returns the given decision per policy name and PolicyNotFound for any other name.</summary>
    private static IPolicyDecisionPoint PdpWithDecisions(params (string Name, PolicyDecision Decision)[] policies)
    {
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluatePolicyAsync(Arg.Any<string>(), Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var name = call.ArgAt<string>(0);
                var match = policies.Where(p => p.Name == name).Select(p => p.Decision).FirstOrDefault();
                return ValueTask.FromResult(match is null
                    ? Left<EncinaError, PolicyDecision>(ABACErrors.PolicyNotFound(name))
                    : Right<EncinaError, PolicyDecision>(match));
            });
        return pdp;
    }

    private static IPolicyDecisionPoint Pdp(params (string Name, Effect Effect)[] policies) =>
        PdpWithDecisions(policies.Select(p => (p.Name, Decision(p.Effect))).ToArray());

    private static ABACPipelineBehavior<TRequest, string> CreateBehavior<TRequest>(
        IPolicyDecisionPoint pdp,
        ABACEnforcementMode mode = ABACEnforcementMode.Block,
        IReadOnlyDictionary<string, object>? subject = null,
        ObligationExecutor? obligationExecutor = null,
        ILogger<ABACPipelineBehavior<TRequest, string>>? logger = null,
        Func<TRequest, IReadOnlyDictionary<string, object>>? resourceOf = null,
        IAttributeProvider? attributeProvider = null,
        ABACOptions? abacOptions = null,
        IABACDecisionRecorder? recorder = null,
        TimeProvider? timeProvider = null)
        where TRequest : IRequest<string>
    {
        if (abacOptions is not null)
        {
            abacOptions.EnforcementMode = mode;
        }

        if (attributeProvider is null)
        {
            attributeProvider = Substitute.For<IAttributeProvider>();
            attributeProvider.GetSubjectAttributesAsync(Arg.Any<RequestIdentity>(), Arg.Any<CancellationToken>())
                .Returns(subject ?? new Dictionary<string, object>());
            attributeProvider.GetResourceAttributesAsync(Arg.Any<TRequest>(), Arg.Any<CancellationToken>())
                .Returns(call => resourceOf is null
                    ? new Dictionary<string, object>()
                    : resourceOf(call.ArgAt<TRequest>(0)));
            attributeProvider.GetEnvironmentAttributesAsync(Arg.Any<CancellationToken>())
                .Returns(new Dictionary<string, object>());
        }

        return new ABACPipelineBehavior<TRequest, string>(
            pdp,
            attributeProvider,
            obligationExecutor ?? Executor(),
            Compiler,
            Options.Create(abacOptions ?? new ABACOptions { EnforcementMode = mode }),
            recorder ?? Substitute.For<IABACDecisionRecorder>(),
            timeProvider ?? TimeProvider.System,
            logger ??NullLogger<ABACPipelineBehavior<TRequest, string>>.Instance);
    }

    /// <summary>A request context whose caller is the authenticated user <paramref name="userId"/>.</summary>
    private static IRequestContext UserContext(string userId = "test-user") =>
        TestRequestContext.For(TestIdentity.User(userId));

    /// <summary>A request context with no authenticated caller.</summary>
    private static IRequestContext AnonymousContext() =>
        TestRequestContext.For(TestIdentity.Anonymous);

    private static ObligationExecutor Executor(params IObligationHandler[] handlers) =>
        new(handlers, NullLogger<ObligationExecutor>.Instance);

    private static async Task<(Either<EncinaError, string> Result, bool NextCalled)> SendAsync<TRequest>(
        ABACPipelineBehavior<TRequest, string> behavior,
        TRequest request,
        IRequestContext? context = null,
        CancellationToken cancellationToken = default)
        where TRequest : IRequest<string>
    {
        var nextCalled = false;
        var result = await behavior.Handle(
            request,
            context ?? UserContext(),
            () =>
            {
                nextCalled = true;
                return ValueTask.FromResult(Right<EncinaError, string>("handled"));
            },
            cancellationToken);

        return (result, nextCalled);
    }

    private static string Code(Either<EncinaError, string> result) =>
        result.Match(Right: _ => "<right>", Left: error => error.GetCode().IfNone("<none>"));

    private static Obligation Obligation(string id, FulfillOn on) =>
        new() { Id = id, FulfillOn = on, AttributeAssignments = [] };

    private static AdviceExpression Advice(string id, FulfillOn on) =>
        new() { Id = id, AppliesTo = on, AttributeAssignments = [] };

    #endregion

    #region Resource attributes come from the request

    [Theory]
    [InlineData("alice", true)]
    [InlineData("bob", false)]
    public async Task Handle_ResourceCondition_IsDecidedByTheAttributesOfTheRequest(string owner, bool permitted)
    {
        var behavior = CreateBehavior<OwnedByAliceRequest>(
            Pdp(),
            resourceOf: request => new Dictionary<string, object> { ["owner"] = request.Owner });

        var (result, nextCalled) = await SendAsync(behavior, new OwnedByAliceRequest(owner));

        result.IsRight.ShouldBe(permitted);
        nextCalled.ShouldBe(permitted);
        if (!permitted)
        {
            Code(result).ShouldBe(ABACErrors.ConditionNotMetCode);
        }
    }

    #endregion

    #region Skipped evaluation

    [Fact]
    public async Task Handle_DisabledMode_SkipsEvaluation()
    {
        var pdp = Pdp(("policy-a", Effect.Deny));
        var behavior = CreateBehavior<PolicyARequest>(pdp, ABACEnforcementMode.Disabled);

        var (result, nextCalled) = await SendAsync(behavior, new PolicyARequest());

        result.IsRight.ShouldBeTrue();
        nextCalled.ShouldBeTrue();
        await pdp.DidNotReceiveWithAnyArgs().EvaluatePolicyAsync(default!, default!, default);
    }

    [Fact]
    public async Task Handle_NoABACAttributes_SkipsEvaluation()
    {
        var pdp = Pdp();
        var behavior = CreateBehavior<UnprotectedRequest>(pdp);

        var (result, nextCalled) = await SendAsync(behavior, new UnprotectedRequest());

        result.IsRight.ShouldBeTrue();
        nextCalled.ShouldBeTrue();
        await pdp.DidNotReceiveWithAnyArgs().EvaluatePolicyAsync(default!, default!, default);
    }

    #endregion

    #region Named policy — Permit, Deny, NotApplicable, Indeterminate, missing

    [Fact]
    public async Task Handle_NamedPolicyPermits_CallsNextStepWithoutEvaluatingTheWholeStore()
    {
        var pdp = Pdp(("policy-a", Effect.Permit));
        var behavior = CreateBehavior<PolicyARequest>(pdp);

        var (result, nextCalled) = await SendAsync(behavior, new PolicyARequest());

        result.IsRight.ShouldBeTrue();
        nextCalled.ShouldBeTrue();
        await pdp.Received(1).EvaluatePolicyAsync("policy-a", Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>());
        await pdp.DidNotReceiveWithAnyArgs().EvaluateAsync(default!, default);
    }

    [Fact]
    public async Task Handle_NamedPolicyDenies_ReturnsAccessDenied()
    {
        var behavior = CreateBehavior<PolicyARequest>(Pdp(("policy-a", Effect.Deny)));

        var (result, nextCalled) = await SendAsync(behavior, new PolicyARequest());

        Code(result).ShouldBe(ABACErrors.AccessDeniedCode);
        nextCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_NamedPolicyNotApplicable_Denies()
    {
        var behavior = CreateBehavior<PolicyARequest>(Pdp(("policy-a", Effect.NotApplicable)));

        var (result, nextCalled) = await SendAsync(behavior, new PolicyARequest());

        Code(result).ShouldBe(ABACErrors.AccessDeniedCode);
        nextCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_NamedPolicyIndeterminate_ReturnsIndeterminate()
    {
        var behavior = CreateBehavior<PolicyARequest>(Pdp(("policy-a", Effect.Indeterminate)));

        var (result, nextCalled) = await SendAsync(behavior, new PolicyARequest());

        Code(result).ShouldBe(ABACErrors.IndeterminateCode);
        nextCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_NamedPolicyMissing_ReturnsPolicyNotFoundWithFixedMessage()
    {
        var behavior = CreateBehavior<PolicyARequest>(Pdp());

        var (result, nextCalled) = await SendAsync(behavior, new PolicyARequest());

        Code(result).ShouldBe(ABACErrors.RequiredPolicyNotFoundCode);
        nextCalled.ShouldBeFalse();
        result.IfLeft(error =>
        {
            error.Message.ShouldNotContain("policy-a");
            error.GetDetails()["policyId"].ShouldBe("policy-a");
        });
    }

    [Fact]
    public async Task Handle_EmptyPolicyName_ReturnsPolicyNotFoundWithoutCallingThePdp()
    {
        var pdp = Pdp();
        var behavior = CreateBehavior<EmptyPolicyNameRequest>(pdp);

        var (result, _) = await SendAsync(behavior, new EmptyPolicyNameRequest());

        Code(result).ShouldBe(ABACErrors.RequiredPolicyNotFoundCode);
        await pdp.DidNotReceiveWithAnyArgs().EvaluatePolicyAsync(default!, default!, default);
    }

    [Fact]
    public async Task Handle_PdpReturnsLeftWithAnotherCode_ReturnsIndeterminate()
    {
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluatePolicyAsync(Arg.Any<string>(), Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Left<EncinaError, PolicyDecision>(ABACErrors.PersistentStoreNotRegistered())));
        var behavior = CreateBehavior<PolicyARequest>(pdp);

        var (result, _) = await SendAsync(behavior, new PolicyARequest());

        Code(result).ShouldBe(ABACErrors.IndeterminateCode);
    }

    [Fact]
    public async Task Handle_PdpThrows_ReturnsEvaluationFailed()
    {
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluatePolicyAsync(Arg.Any<string>(), Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, PolicyDecision>>>(_ => throw new InvalidOperationException("PDP crash"));
        var behavior = CreateBehavior<PolicyARequest>(pdp);

        var (result, nextCalled) = await SendAsync(behavior, new PolicyARequest());

        Code(result).ShouldBe(ABACErrors.EvaluationFailedCode);
        nextCalled.ShouldBeFalse();
    }

    #endregion

    #region AllMustPass — AND, OR, mixed

    [Theory]
    [InlineData(Effect.Permit, Effect.Permit, true)]
    [InlineData(Effect.Permit, Effect.Deny, false)]
    [InlineData(Effect.NotApplicable, Effect.Permit, false)]
    public async Task Handle_AllMustPass_PermitsOnlyWhenEveryPolicyPermits(Effect a, Effect b, bool permitted)
    {
        var behavior = CreateBehavior<AllOfABRequest>(Pdp(("policy-a", a), ("policy-b", b)));

        var (result, nextCalled) = await SendAsync(behavior, new AllOfABRequest());

        result.IsRight.ShouldBe(permitted);
        nextCalled.ShouldBe(permitted);
    }

    [Fact]
    public async Task Handle_AllMustPass_DenyAndIndeterminate_IsAccessDenied()
    {
        var behavior = CreateBehavior<AllOfABRequest>(Pdp(("policy-a", Effect.Deny), ("policy-b", Effect.Indeterminate)));

        var (result, _) = await SendAsync(behavior, new AllOfABRequest());

        Code(result).ShouldBe(ABACErrors.AccessDeniedCode);
    }

    [Theory]
    [InlineData(Effect.Deny, Effect.Permit, "<right>")]
    [InlineData(Effect.Indeterminate, Effect.Permit, "<right>")]
    [InlineData(Effect.Deny, Effect.NotApplicable, ABACErrors.AccessDeniedCode)]
    [InlineData(Effect.Indeterminate, Effect.Deny, ABACErrors.IndeterminateCode)]
    public async Task Handle_AnyMustPass_PermitsWhenOnePolicyPermits(Effect a, Effect b, string expectedCode)
    {
        var behavior = CreateBehavior<AnyOfABRequest>(Pdp(("policy-a", a), ("policy-b", b)));

        var (result, _) = await SendAsync(behavior, new AnyOfABRequest());

        Code(result).ShouldBe(expectedCode);
    }

    [Theory]
    [InlineData(Effect.Permit, Effect.Deny, Effect.Permit, true)]
    [InlineData(Effect.Permit, Effect.Deny, Effect.Deny, false)]
    [InlineData(Effect.Deny, Effect.Permit, Effect.Permit, false)]
    public async Task Handle_MixedGroups_BothGroupsMustHold(Effect a, Effect b, Effect c, bool permitted)
    {
        var behavior = CreateBehavior<MixedRequest>(Pdp(("policy-a", a), ("policy-b", b), ("policy-c", c)));

        var (result, _) = await SendAsync(behavior, new MixedRequest());

        result.IsRight.ShouldBe(permitted);
    }

    [Fact]
    public async Task Handle_OnePolicyMissingAmongOthers_ReturnsPolicyNotFound()
    {
        var behavior = CreateBehavior<AnyOfABRequest>(Pdp(("policy-a", Effect.Permit)));

        var (result, _) = await SendAsync(behavior, new AnyOfABRequest());

        Code(result).ShouldBe(ABACErrors.RequiredPolicyNotFoundCode);
    }

    #endregion

    #region RequireCondition — true, false, compile error, runtime error

    [Fact]
    public async Task Handle_ConditionTrue_CallsNextStepWithoutAnyPolicy()
    {
        var pdp = Pdp();
        var behavior = CreateBehavior<HrConditionRequest>(pdp, subject: HrSubject);

        var (result, nextCalled) = await SendAsync(behavior, new HrConditionRequest());

        result.IsRight.ShouldBeTrue();
        nextCalled.ShouldBeTrue();
        await pdp.DidNotReceiveWithAnyArgs().EvaluatePolicyAsync(default!, default!, default);
        await pdp.DidNotReceiveWithAnyArgs().EvaluateAsync(default!, default);
    }

    [Fact]
    public async Task Handle_ConditionFalse_ReturnsConditionNotMet()
    {
        var behavior = CreateBehavior<HrConditionRequest>(Pdp(), subject: FinanceSubject);

        var (result, nextCalled) = await SendAsync(behavior, new HrConditionRequest());

        Code(result).ShouldBe(ABACErrors.ConditionNotMetCode);
        nextCalled.ShouldBeFalse();
        result.IfLeft(error => error.GetDetails()["conditionIndex"].ShouldBe(0));
    }

    [Fact]
    public async Task Handle_TwoConditionsBothTrue_UsesActionNameAndCallsNextStep()
    {
        var behavior = CreateBehavior<TwoConditionsRequest>(Pdp(), subject: HrSubject);

        var (result, _) = await SendAsync(behavior, new TwoConditionsRequest());

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_ConditionDoesNotCompile_ReturnsIndeterminateAndLogsTheErrorCode()
    {
        var logger = new FakeLogger<ABACPipelineBehavior<UncompilableConditionRequest, string>>();
        var behavior = CreateBehavior(Pdp(), logger: logger);

        var (result, nextCalled) = await SendAsync(behavior, new UncompilableConditionRequest());

        Code(result).ShouldBe(ABACErrors.IndeterminateCode);
        nextCalled.ShouldBeFalse();
        logger.Collector.GetSnapshot().ShouldContain(r => r.Id.Id == 9076 && r.Message.Contains(ABACErrors.InvalidConditionCode));
    }

    [Fact]
    public async Task Handle_ConditionThrowsAtRuntime_ReturnsIndeterminate()
    {
        var behavior = CreateBehavior<ThrowingConditionRequest>(Pdp(), subject: HrSubject);

        var (result, nextCalled) = await SendAsync(behavior, new ThrowingConditionRequest());

        Code(result).ShouldBe(ABACErrors.IndeterminateCode);
        nextCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_ConditionErrors_LogNoExceptionOrCompilerMessage()
    {
        var logger = new FakeLogger<ABACPipelineBehavior<ThrowingConditionRequest, string>>();
        var behavior = CreateBehavior(Pdp(), subject: HrSubject, logger: logger);

        await SendAsync(behavior, new ThrowingConditionRequest());

        var records = logger.Collector.GetSnapshot();
        records.ShouldContain(r => r.Id.Id == 9077);
        records.ShouldAllBe(r => !r.Message.Contains("missingAttribute", StringComparison.Ordinal));
        records.Where(r => r.Exception != null)
            .ShouldAllBe(r => !r.Exception!.Message.Contains("missingAttribute", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(Effect.Permit, "HR", "<right>")]
    [InlineData(Effect.Permit, "Finance", ABACErrors.ConditionNotMetCode)]
    [InlineData(Effect.Deny, "HR", ABACErrors.AccessDeniedCode)]
    public async Task Handle_PolicyAndCondition_CombineWithAnd(Effect policy, string department, string expectedCode)
    {
        var behavior = CreateBehavior<PolicyAndConditionRequest>(
            Pdp(("policy-a", policy)),
            subject: new Dictionary<string, object> { ["department"] = department });

        var (result, _) = await SendAsync(behavior, new PolicyAndConditionRequest());

        Code(result).ShouldBe(expectedCode);
    }

    #endregion

    #region Warn mode

    [Fact]
    public async Task Handle_WarnMode_DenyVerdict_LogsAndCallsNextStep()
    {
        var behavior = CreateBehavior<PolicyARequest>(Pdp(("policy-a", Effect.Deny)), ABACEnforcementMode.Warn);

        var (result, nextCalled) = await SendAsync(behavior, new PolicyARequest());

        result.IsRight.ShouldBeTrue();
        nextCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WarnMode_ConditionNotMet_LogsAndCallsNextStep()
    {
        var behavior = CreateBehavior<HrConditionRequest>(Pdp(), ABACEnforcementMode.Warn, FinanceSubject);

        var (result, nextCalled) = await SendAsync(behavior, new HrConditionRequest());

        result.IsRight.ShouldBeTrue();
        nextCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WarnMode_RequiredPolicyMissing_LogsAndCallsNextStep()
    {
        var behavior = CreateBehavior<PolicyARequest>(Pdp(), ABACEnforcementMode.Warn);

        var (result, nextCalled) = await SendAsync(behavior, new PolicyARequest());

        result.IsRight.ShouldBeTrue();
        nextCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WarnMode_Indeterminate_StillDenies()
    {
        var behavior = CreateBehavior<PolicyARequest>(Pdp(("policy-a", Effect.Indeterminate)), ABACEnforcementMode.Warn);

        var (result, nextCalled) = await SendAsync(behavior, new PolicyARequest());

        Code(result).ShouldBe(ABACErrors.IndeterminateCode);
        nextCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_WarnMode_ConditionDoesNotCompile_StillDenies()
    {
        var behavior = CreateBehavior<UncompilableConditionRequest>(Pdp(), ABACEnforcementMode.Warn);

        var (result, nextCalled) = await SendAsync(behavior, new UncompilableConditionRequest());

        Code(result).ShouldBe(ABACErrors.IndeterminateCode);
        nextCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_WarnMode_PdpReturnsLeftWithAnotherCode_StillDenies()
    {
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluatePolicyAsync(Arg.Any<string>(), Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Left<EncinaError, PolicyDecision>(ABACErrors.PersistentStoreNotRegistered())));
        var behavior = CreateBehavior<PolicyARequest>(pdp, ABACEnforcementMode.Warn);

        var (result, nextCalled) = await SendAsync(behavior, new PolicyARequest());

        Code(result).ShouldBe(ABACErrors.IndeterminateCode);
        nextCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_WarnMode_EvaluationException_StillReturnsLeft()
    {
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluatePolicyAsync(Arg.Any<string>(), Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, PolicyDecision>>>(_ => throw new InvalidOperationException("boom"));
        var behavior = CreateBehavior<PolicyARequest>(pdp, ABACEnforcementMode.Warn);

        var (result, nextCalled) = await SendAsync(behavior, new PolicyARequest());

        Code(result).ShouldBe(ABACErrors.EvaluationFailedCode);
        nextCalled.ShouldBeFalse();
    }

    #endregion

    #region Obligations and advice of the named policies

    [Fact]
    public async Task Handle_Permit_ExecutesTheObligationsOfEveryPermittingPolicy()
    {
        var handlerA = new TestObligationHandler("ob-a");
        var handlerB = new TestObligationHandler("ob-b");
        var pdp = PdpWithDecisions(
            ("policy-a", Decision(Effect.Permit, [Obligation("ob-a", FulfillOn.Permit)])),
            ("policy-b", Decision(Effect.Permit, [Obligation("ob-b", FulfillOn.Permit)])));
        var behavior = CreateBehavior<AllOfABRequest>(pdp, obligationExecutor: Executor(handlerA, handlerB));

        var (result, _) = await SendAsync(behavior, new AllOfABRequest());

        result.IsRight.ShouldBeTrue();
        handlerA.Invocations.ShouldBe(1);
        handlerB.Invocations.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_Deny_ExecutesTheOnDenyObligationsOfTheDenyingPolicy()
    {
        var handler = new TestObligationHandler("on-deny");
        var pdp = PdpWithDecisions(("policy-a", Decision(Effect.Deny, [Obligation("on-deny", FulfillOn.Deny)])));
        var behavior = CreateBehavior<PolicyARequest>(pdp, obligationExecutor: Executor(handler));

        var (result, _) = await SendAsync(behavior, new PolicyARequest());

        Code(result).ShouldBe(ABACErrors.AccessDeniedCode);
        handler.Invocations.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_PermitWithMandatoryObligationHandlerThatThrows_IsDenied()
    {
        var handler = new TestObligationHandler("ob-a", throws: new InvalidOperationException("handler crashed"));
        var pdp = PdpWithDecisions(("policy-a", Decision(Effect.Permit, [Obligation("ob-a", FulfillOn.Permit)])));
        var behavior = CreateBehavior<PolicyARequest>(pdp, obligationExecutor: Executor(handler));

        var (result, nextCalled) = await SendAsync(behavior, new PolicyARequest());

        Code(result).ShouldBe(ABACErrors.ObligationFailedCode);
        nextCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_PermitWithAdviceHandlerThatThrows_StillCallsNextStep()
    {
        var handler = new TestObligationHandler("advice-a", throws: new InvalidOperationException("advice crashed"));
        var pdp = PdpWithDecisions(("policy-a", Decision(Effect.Permit, advice: [Advice("advice-a", FulfillOn.Permit)])));
        var behavior = CreateBehavior<PolicyARequest>(pdp, obligationExecutor: Executor(handler));

        var (result, nextCalled) = await SendAsync(behavior, new PolicyARequest());

        result.IsRight.ShouldBeTrue();
        nextCalled.ShouldBeTrue();
        handler.Invocations.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_ObligationHandlerCancelled_PropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        var handler = new TestObligationHandler("ob-a", onInvoke: cts.Cancel);
        var pdp = PdpWithDecisions(("policy-a", Decision(Effect.Permit, [Obligation("ob-a", FulfillOn.Permit)])));
        var behavior = CreateBehavior<PolicyARequest>(pdp, obligationExecutor: Executor(handler));

        await Should.ThrowAsync<OperationCanceledException>(() => SendAsync(behavior, new PolicyARequest(), cancellationToken: cts.Token));
    }

    #endregion

    #region Cancellation

    [Fact]
    public async Task Handle_PdpCancelled_PropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluatePolicyAsync(Arg.Any<string>(), Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, PolicyDecision>>>(_ => throw new OperationCanceledException(cts.Token));
        var behavior = CreateBehavior<PolicyARequest>(pdp);

        await Should.ThrowAsync<OperationCanceledException>(() => SendAsync(behavior, new PolicyARequest(), cancellationToken: cts.Token));
    }

    [Fact]
    public async Task Handle_CancelledWhileCompilingACondition_PropagatesCancellation()
    {
        // A fresh compiler has no cached delegate, so CompileAsync waits on its lock with the
        // already-cancelled token and throws; the PEP must not turn that into Indeterminate.
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        using var freshCompiler = new EELCompiler();
        var behavior = new ABACPipelineBehavior<HrConditionRequest, string>(
            Pdp(),
            AttributeProviderReturningEmpty<HrConditionRequest>(),
            Executor(),
            freshCompiler,
            Options.Create(new ABACOptions()),
            Substitute.For<IABACDecisionRecorder>(),
            TimeProvider.System,
            NullLogger<ABACPipelineBehavior<HrConditionRequest, string>>.Instance);

        await Should.ThrowAsync<OperationCanceledException>(() => SendAsync(behavior, new HrConditionRequest(), cancellationToken: cts.Token));
    }

    private static IAttributeProvider AttributeProviderReturningEmpty<TRequest>()
    {
        var attributeProvider = Substitute.For<IAttributeProvider>();
        attributeProvider.GetSubjectAttributesAsync(Arg.Any<RequestIdentity>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object>());
        attributeProvider.GetResourceAttributesAsync(Arg.Any<TRequest>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object>());
        attributeProvider.GetEnvironmentAttributesAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object>());
        return attributeProvider;
    }

    #endregion

    #region Sentinel

    [Fact]
    public async Task Handle_ThrowingHandler_SentinelReachesNoLogNoActivityTagAndNoError()
    {
        // Arrange
        const string Sentinel = "SENTINEL-1634-secret-patient-data";
        var pepLogger = new FakeLogger<ABACPipelineBehavior<PolicyARequest, string>>();
        var executorLogger = new FakeLogger<ObligationExecutor>();
        var handler = new TestObligationHandler("ob-a", throws: new InvalidOperationException(Sentinel));
        var pdp = PdpWithDecisions(
            ("policy-a", Decision(Effect.Permit, [Obligation("ob-a", FulfillOn.Permit)], [Advice("ob-a", FulfillOn.Permit)])));
        var executor = new ObligationExecutor([handler], executorLogger);
        var behavior = CreateBehavior(pdp, obligationExecutor: executor, logger: pepLogger);

        var activities = new System.Collections.Concurrent.ConcurrentBag<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Encina.Security.ABAC",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activities.Add
        };
        ActivitySource.AddActivityListener(listener);

        // Act
        var (result, _) = await SendAsync(behavior, new PolicyARequest());

        // Assert
        Code(result).ShouldBe(ABACErrors.ObligationFailedCode);
        result.IfLeft(error =>
        {
            error.Message.ShouldNotContain(Sentinel);
            error.GetDetails().Values.ShouldAllBe(value => value == null || !value.ToString()!.Contains(Sentinel));
        });

        var records = pepLogger.Collector.GetSnapshot().Concat(executorLogger.Collector.GetSnapshot()).ToList();
        records.ShouldContain(r => r.Id.Id == 9078);
        records.ShouldAllBe(r => !r.Message.Contains(Sentinel, StringComparison.Ordinal));
        records.Where(r => r.Exception != null)
            .ShouldAllBe(r => !r.Exception!.ToString().Contains(Sentinel, StringComparison.Ordinal));

        activities.ShouldAllBe(a =>
            (a.StatusDescription == null || !a.StatusDescription.Contains(Sentinel, StringComparison.Ordinal))
            && a.TagObjects.All(tag => tag.Value == null || !tag.Value.ToString()!.Contains(Sentinel, StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Handle_AttributeProviderThrows_SentinelReachesNoLogNoActivityTagAndNoError()
    {
        // Arrange
        const string Sentinel = "SENTINEL-1676-secret-subject-data";
        var pepLogger = new FakeLogger<ABACPipelineBehavior<PolicyARequest, string>>();
        var attributeProvider = Substitute.For<IAttributeProvider>();
        attributeProvider.GetSubjectAttributesAsync(Arg.Any<RequestIdentity>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<IReadOnlyDictionary<string, object>>>(_ => throw new InvalidOperationException(Sentinel));
        var behavior = CreateBehavior<PolicyARequest>(
            Pdp(("policy-a", Effect.Permit)), logger: pepLogger, attributeProvider: attributeProvider);

        var activities = new System.Collections.Concurrent.ConcurrentBag<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Encina.Security.ABAC",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activities.Add
        };
        ActivitySource.AddActivityListener(listener);

        // Act
        var (result, nextCalled) = await SendAsync(behavior, new PolicyARequest());

        // Assert
        Code(result).ShouldBe(ABACErrors.EvaluationFailedCode);
        nextCalled.ShouldBeFalse();
        result.IfLeft(error =>
        {
            error.Message.ShouldNotContain(Sentinel);
            error.GetDetails().Values.ShouldAllBe(value => value == null || !value.ToString()!.Contains(Sentinel));
        });

        var records = pepLogger.Collector.GetSnapshot();
        records.ShouldContain(r => r.Id.Id == 9009);
        records.ShouldAllBe(r => !r.Message.Contains(Sentinel, StringComparison.Ordinal));
        records.Where(r => r.Exception != null)
            .ShouldAllBe(r => !r.Exception!.ToString().Contains(Sentinel, StringComparison.Ordinal));

        activities.ShouldAllBe(a =>
            (a.StatusDescription == null || !a.StatusDescription.Contains(Sentinel, StringComparison.Ordinal))
            && a.TagObjects.All(tag => tag.Value == null || !tag.Value.ToString()!.Contains(Sentinel, StringComparison.Ordinal)));
    }

    #endregion

    #region Unauthenticated caller (#1676, #1705 N1)

    [Theory]
    [InlineData(ABACEnforcementMode.Block)]
    [InlineData(ABACEnforcementMode.Warn)]
    public async Task Handle_AnonymousCaller_DeniesUnauthenticatedWithGateAbacBeforeCollectingAttributes(ABACEnforcementMode mode)
    {
        // Arrange: the named policy would permit, so only the missing caller can deny.
        var pdp = Pdp(("policy-a", Effect.Permit));
        var attributeProvider = Substitute.For<IAttributeProvider>();
        var behavior = CreateBehavior<PolicyARequest>(pdp, mode, attributeProvider: attributeProvider);

        // Act
        var (result, nextCalled) = await SendAsync(behavior, new PolicyARequest(), context: AnonymousContext());

        // Assert
        Code(result).ShouldBe(EncinaErrorCodes.AuthorizationUnauthenticated);
        result.IfLeft(error => error.GetDetails()["gate"].ShouldBe("abac"));
        nextCalled.ShouldBeFalse("an unauthenticated caller is not a definite verdict, so Warn mode denies too");
        await attributeProvider.DidNotReceiveWithAnyArgs().GetSubjectAttributesAsync(default!, default);
        await attributeProvider.DidNotReceiveWithAnyArgs().GetResourceAttributesAsync<PolicyARequest>(default!, default);
        await attributeProvider.DidNotReceiveWithAnyArgs().GetEnvironmentAttributesAsync(default);
        await pdp.DidNotReceiveWithAnyArgs().EvaluatePolicyAsync(default!, default!, default);
    }

    [Theory]
    [InlineData(ABACEnforcementMode.Block)]
    [InlineData(ABACEnforcementMode.Warn)]
    public async Task Handle_UnauthenticatedPrincipalWithSubjectClaim_IsAnonymousAndDenied(ABACEnforcementMode mode)
    {
        // Arrange: a substituted context whose identity is anonymous (an unauthenticated principal
        // with a subject claim maps to Anonymous) must not be evaluated as that user.
        var pdp = Pdp(("policy-a", Effect.Permit));
        var attributeProvider = Substitute.For<IAttributeProvider>();
        var behavior = CreateBehavior<PolicyARequest>(pdp, mode, attributeProvider: attributeProvider);
        var context = Substitute.For<IRequestContext>();
        context.Identity.Returns(RequestIdentity.Anonymous);

        // Act
        var (result, nextCalled) = await SendAsync(behavior, new PolicyARequest(), context: context);

        // Assert
        Code(result).ShouldBe(EncinaErrorCodes.AuthorizationUnauthenticated);
        nextCalled.ShouldBeFalse();
        await attributeProvider.DidNotReceiveWithAnyArgs().GetSubjectAttributesAsync(default!, default);
        await pdp.DidNotReceiveWithAnyArgs().EvaluatePolicyAsync(default!, default!, default);
    }

    [Theory]
    [InlineData(ABACEnforcementMode.Block)]
    [InlineData(ABACEnforcementMode.Warn)]
    public async Task Handle_AnonymousCaller_LogsTheCodeAndNeverTheWarnModeBypass(ABACEnforcementMode mode)
    {
        var logger = new FakeLogger<ABACPipelineBehavior<PolicyARequest, string>>();
        var behavior = CreateBehavior<PolicyARequest>(Pdp(("policy-a", Effect.Permit)), mode, logger: logger);

        await SendAsync(behavior, new PolicyARequest(), context: AnonymousContext());

        var records = logger.Collector.GetSnapshot();
        records.ShouldContain(r => r.Id.Id == 9091
            && r.Message.Contains(EncinaErrorCodes.AuthorizationUnauthenticated, StringComparison.Ordinal));
        records.ShouldNotContain(r => r.Id.Id == 9004, "Warn mode must not let an unauthenticated caller through");
    }

    [Fact]
    public async Task Handle_AnonymousCaller_ErrorCarriesNoUserIdentifier()
    {
        var behavior = CreateBehavior<PolicyARequest>(Pdp(("policy-a", Effect.Permit)));

        var (result, _) = await SendAsync(behavior, new PolicyARequest(), context: AnonymousContext());

        Code(result).ShouldBe(EncinaErrorCodes.AuthorizationUnauthenticated);
        result.IfLeft(error =>
        {
            error.GetDetails().ShouldNotContainKey("userId");
            error.GetDetails()["requestType"].ShouldBe(typeof(PolicyARequest).FullName);
        });
    }

    [Theory]
    [InlineData(ABACEnforcementMode.Block)]
    [InlineData(ABACEnforcementMode.Warn)]
    public async Task Handle_ContextIdentityThrows_DeniesWithEvaluationFailed(ABACEnforcementMode mode)
    {
        var context = Substitute.For<IRequestContext>();
        context.Identity.Returns(_ => throw new InvalidOperationException("context failed"));
        var attributeProvider = Substitute.For<IAttributeProvider>();
        var behavior = CreateBehavior<PolicyARequest>(
            Pdp(("policy-a", Effect.Permit)), mode, attributeProvider: attributeProvider);

        var (result, nextCalled) = await SendAsync(behavior, new PolicyARequest(), context: context);

        Code(result).ShouldBe(ABACErrors.EvaluationFailedCode);
        nextCalled.ShouldBeFalse();
        await attributeProvider.DidNotReceiveWithAnyArgs().GetSubjectAttributesAsync(default!, default);
    }

    [Fact]
    public async Task Handle_DisabledModeWithAnonymousCaller_StillSkipsEvaluation()
    {
        var behavior = CreateBehavior<PolicyARequest>(Pdp(), ABACEnforcementMode.Disabled);

        var (result, nextCalled) = await SendAsync(behavior, new PolicyARequest(), context: AnonymousContext());

        result.IsRight.ShouldBeTrue();
        nextCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_UnprotectedRequestWithAnonymousCaller_StillSkipsEvaluation()
    {
        var behavior = CreateBehavior<UnprotectedRequest>(Pdp());

        var (result, nextCalled) = await SendAsync(behavior, new UnprotectedRequest(), context: AnonymousContext());

        result.IsRight.ShouldBeTrue();
        nextCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_IdentityIsReadOnceForTheWholeEvaluation()
    {
        // The identity can turn anonymous between two reads when its scope ends (#1892): the PEP
        // reads it once and derives the subject from that snapshot.
        var context = Substitute.For<IRequestContext>();
        context.Identity.Returns(TestIdentity.User("alice"), RequestIdentity.Anonymous);
        var behavior = CreateBehavior<PolicyARequest>(Pdp(("policy-a", Effect.Permit)));

        var (result, nextCalled) = await SendAsync(behavior, new PolicyARequest(), context: context);

        result.IsRight.ShouldBeTrue();
        nextCalled.ShouldBeTrue();
        _ = context.Received(1).Identity;
    }

    #endregion

    #region Subject attributes from the request identity (#1705 N3)

    [Fact]
    public async Task Handle_UserCaller_ProviderReceivesTheRequestIdentityAndBuiltInsAreAdded()
    {
        var identity = TestIdentity.User("alice");
        var attributeProvider = ProviderCapturingSubject<PolicyARequest>(new Dictionary<string, object> { ["department"] = "HR" });
        PolicyEvaluationContext? evaluated = null;
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluatePolicyAsync(Arg.Any<string>(), Arg.Do<PolicyEvaluationContext>(c => evaluated = c), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, PolicyDecision>(Decision(Effect.Permit))));
        var behavior = CreateBehavior<PolicyARequest>(pdp, attributeProvider: attributeProvider);

        var (result, _) = await SendAsync(behavior, new PolicyARequest(), context: TestRequestContext.For(identity));

        result.IsRight.ShouldBeTrue();
        await attributeProvider.Received(1).GetSubjectAttributesAsync(identity, Arg.Any<CancellationToken>());
        evaluated.ShouldNotBeNull();
        evaluated.SubjectAttributes[ABACSubjectAttributes.SubjectId].SingleValue().Value.ShouldBe("alice");
        evaluated.SubjectAttributes[ABACSubjectAttributes.IdentityKind].SingleValue().Value.ShouldBe("user");
        evaluated.SubjectAttributes["department"].SingleValue().Value.ShouldBe("HR");
    }

    [Fact]
    public async Task Handle_ServiceCaller_IsEvaluatedWithServiceSubjectAndKind()
    {
        var identity = TestIdentity.Service("billing-job");
        var attributeProvider = ProviderCapturingSubject<KindConditionRequest>(new Dictionary<string, object>());
        var behavior = CreateBehavior<KindConditionRequest>(Pdp(), attributeProvider: attributeProvider);

        var (result, nextCalled) = await SendAsync(behavior, new KindConditionRequest(), context: TestRequestContext.For(identity));

        result.IsRight.ShouldBeTrue("the condition reads identity-kind == service from the built-in attributes");
        nextCalled.ShouldBeTrue();
        await attributeProvider.Received(1).GetSubjectAttributesAsync(
            Arg.Is<RequestIdentity>(i => i.Kind == IdentityKind.Service && i.UserId == "service:billing-job"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UserCaller_DoesNotSatisfyAServiceOnlyCondition()
    {
        var behavior = CreateBehavior<KindConditionRequest>(Pdp());

        var (result, nextCalled) = await SendAsync(behavior, new KindConditionRequest(), context: UserContext("alice"));

        Code(result).ShouldBe(ABACErrors.ConditionNotMetCode);
        nextCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_ProviderReturnsBuiltInNames_BuiltInsWin()
    {
        var attributeProvider = ProviderCapturingSubject<KindConditionRequest>(new Dictionary<string, object>
        {
            [ABACSubjectAttributes.SubjectId] = "forged",
            [ABACSubjectAttributes.IdentityKind] = "service"
        });
        var behavior = CreateBehavior<KindConditionRequest>(Pdp(), attributeProvider: attributeProvider);

        var (result, nextCalled) = await SendAsync(behavior, new KindConditionRequest(), context: UserContext("alice"));

        Code(result).ShouldBe(ABACErrors.ConditionNotMetCode, "a provider cannot turn a user into a service");
        nextCalled.ShouldBeFalse();
    }

    [RequireCondition("((System.Collections.Generic.IDictionary<string, object>)user)[\"identity-kind\"].ToString() == \"service\"")]
    private sealed record KindConditionRequest : IRequest<string>;

    private static IAttributeProvider ProviderCapturingSubject<TRequest>(IReadOnlyDictionary<string, object> subject)
    {
        var attributeProvider = Substitute.For<IAttributeProvider>();
        attributeProvider.GetSubjectAttributesAsync(Arg.Any<RequestIdentity>(), Arg.Any<CancellationToken>())
            .Returns(subject);
        attributeProvider.GetResourceAttributesAsync(Arg.Any<TRequest>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object>());
        attributeProvider.GetEnvironmentAttributesAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object>());
        return attributeProvider;
    }

    #endregion

    #region Constructor guards

    [Fact]
    public void Constructor_NullEelCompiler_Throws()
    {
        var act = () => new ABACPipelineBehavior<PolicyARequest, string>(
            Pdp(),
            Substitute.For<IAttributeProvider>(),
            Executor(),
            null!,
            Options.Create(new ABACOptions()),
            Substitute.For<IABACDecisionRecorder>(),
            TimeProvider.System,
            NullLogger<ABACPipelineBehavior<PolicyARequest, string>>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("eelCompiler");
    }

    #endregion

    #region Test Helper — ObligationHandler

    private sealed class TestObligationHandler(
        string obligationId,
        Exception? throws = null,
        Action? onInvoke = null) : IObligationHandler
    {
        public int Invocations { get; private set; }

        public bool CanHandle(string id) => id == obligationId;

        public ValueTask<Either<EncinaError, Unit>> HandleAsync(
            Obligation obligation,
            PolicyEvaluationContext context,
            CancellationToken cancellationToken)
        {
            Invocations++;
            onInvoke?.Invoke();
            cancellationToken.ThrowIfCancellationRequested();

            if (throws is not null)
            {
                throw throws;
            }

            return new(Right<EncinaError, Unit>(unit));
        }
    }

    #endregion
}

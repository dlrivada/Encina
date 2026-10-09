#pragma warning disable CA2012 // Use ValueTasks correctly -- NSubstitute mock setup pattern

using System.Diagnostics;

using Encina.Security.ABAC;
using Encina.Security.ABAC.DecisionAudit;
using Encina.Security.ABAC.EEL;
using Encina.Testing.Identity;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.ABAC.DecisionAudit;

/// <summary>
/// The decision path table of the ABAC decision audit (#751 Phase 2): for every path of the Policy
/// Enforcement Point, the enforced outcome, the reason code and whether the request proceeds, plus
/// the write-ahead, fail-closed and clock rules.
/// </summary>
[Collection(ABACActivityListenerIsolation.Name)]
public sealed class ABACPipelineDecisionAuditTests
{
    private static readonly EELCompiler Compiler = new();
    private static readonly DateTimeOffset Start = new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);

    #region Test request types

    [RequirePolicy("policy-a")]
    private sealed record PolicyARequest(string? Id = null) : IRequest<string>, IABACResourceIdentity
    {
        public string? ResourceId => Id;
    }

    [RequirePolicy("policy-a")]
    [RequirePolicy("policy-b")]
    private sealed record TwoPoliciesRequest : IRequest<string>;

    [RequireCondition("user.department == \"HR\"")]
    private sealed record ConditionRequest : IRequest<string>;

    [RequireCondition("user.department == \"HR\"")]
    [RequireCondition("user.department == \"Finance\"")]
    [RequireCondition("action.name == \"TwoConditionsAudit\"")]
    private sealed record TwoConditionsRequest : IRequest<string>;

    [RequireCondition("this is ((( not C#")]
    private sealed record UncompilableConditionRequest : IRequest<string>;

    [RequirePolicy("policy-a")]
    [RequireCondition("user.department == \"HR\"")]
    private sealed record PolicyAndConditionRequest : IRequest<string>;

    [RequirePolicy("policy-a")]
    private sealed record AttributeIdRequest : IRequest<string>;

    #endregion

    #region Helpers

    private sealed class RecordingRecorder : IABACDecisionRecorder
    {
        public List<ABACDecisionRecord> Records { get; } = [];

        public List<CancellationToken> Tokens { get; } = [];

        public Func<ABACDecisionRecord, ValueTask<Either<EncinaError, Unit>>>? Behavior { get; set; }

        public ValueTask<Either<EncinaError, Unit>> RecordAsync(ABACDecisionRecord record, CancellationToken cancellationToken = default)
        {
            Records.Add(record);
            Tokens.Add(cancellationToken);
            return Behavior?.Invoke(record) ?? ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
        }
    }

    private sealed class CountingTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public int Reads { get; private set; }

        public override DateTimeOffset GetUtcNow()
        {
            Reads++;
            return now.AddSeconds(Reads);
        }
    }

    private static PolicyDecision Decision(
        Effect effect,
        IReadOnlyList<Obligation>? obligations = null,
        IReadOnlyList<PolicyEvaluationTrace>? trace = null,
        bool truncated = false,
        string? ruleId = null) => new()
        {
            Effect = effect,
            Obligations = obligations ?? [],
            Advice = [],
            EvaluationDuration = TimeSpan.FromMilliseconds(1),
            EvaluatedPolicies = trace ?? [],
            EvaluationTraceTruncated = truncated,
            RuleId = ruleId
        };

    private static IPolicyDecisionPoint Pdp(params (string Name, PolicyDecision Decision)[] policies)
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

    private static IPolicyDecisionPoint Pdp(Effect effect) => Pdp(("policy-a", Decision(effect)));

    private static IAttributeProvider Attributes(
        IReadOnlyDictionary<string, object>? subject = null,
        IReadOnlyDictionary<string, object>? resource = null,
        IReadOnlyDictionary<string, object>? environment = null)
    {
        var provider = Substitute.For<IAttributeProvider>();
        provider.GetSubjectAttributesAsync(Arg.Any<RequestIdentity>(), Arg.Any<CancellationToken>())
            .Returns(subject ?? new Dictionary<string, object>());
        provider.GetResourceAttributesAsync(Arg.Any<object>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(resource ?? new Dictionary<string, object>());
        provider.GetEnvironmentAttributesAsync(Arg.Any<CancellationToken>())
            .Returns(environment ?? new Dictionary<string, object>());
        return provider;
    }

    private static ABACOptions Options(
        ABACEnforcementMode mode = ABACEnforcementMode.Block,
        bool enabled = true,
        ABACDecisionAuditFailureMode failureMode = ABACDecisionAuditFailureMode.FailClosed,
        ABACDecisionAuditOutcomes outcomes = ABACDecisionAuditOutcomes.All,
        int maxTrace = 64)
    {
        var options = new ABACOptions { EnforcementMode = mode };
        options.DecisionAudit.Enabled = enabled;
        options.DecisionAudit.FailureMode = failureMode;
        options.DecisionAudit.Outcomes = outcomes;
        options.DecisionAudit.MaxTraceEntries = maxTrace;
        return options;
    }

    private static ABACPipelineBehavior<TRequest, string> Behavior<TRequest>(
        IPolicyDecisionPoint pdp,
        ABACOptions options,
        IABACDecisionRecorder recorder,
        TimeProvider? timeProvider = null,
        IAttributeProvider? attributes = null,
        ObligationExecutor? executor = null,
        ILogger<ABACPipelineBehavior<TRequest, string>>? logger = null)
        where TRequest : IRequest<string> =>
        new(
            pdp,
            attributes ?? Attributes(new Dictionary<string, object> { ["department"] = "HR" }),
            executor ?? new ObligationExecutor([], Microsoft.Extensions.Logging.Abstractions.NullLogger<ObligationExecutor>.Instance),
            Compiler,
            Microsoft.Extensions.Options.Options.Create(options),
            recorder,
            timeProvider ?? new FakeTimeProvider(Start),
            logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ABACPipelineBehavior<TRequest, string>>.Instance);

    private static IRequestContext User(string id = "alice") =>
        TestRequestContext.For(TestIdentity.User(id), tenantId: "tenant-1", correlationId: "corr-1");

    private static async Task<(Either<EncinaError, string> Result, bool NextCalled)> SendAsync<TRequest>(
        ABACPipelineBehavior<TRequest, string> behavior,
        TRequest request,
        IRequestContext? context = null,
        Action? onNext = null,
        CancellationToken cancellationToken = default)
        where TRequest : IRequest<string>
    {
        var nextCalled = false;
        var result = await behavior.Handle(
            request,
            context ?? User(),
            () =>
            {
                nextCalled = true;
                onNext?.Invoke();
                return ValueTask.FromResult(Right<EncinaError, string>("handled"));
            },
            cancellationToken);

        return (result, nextCalled);
    }

    private static string Code(Either<EncinaError, string> result) =>
        result.Match(Right: _ => "<right>", Left: error => error.GetCode().IfNone("<none>"));

    private static ABACDecisionRecord Single(RecordingRecorder recorder)
    {
        recorder.Records.Count.ShouldBe(1);
        return recorder.Records[0];
    }

    #endregion

    #region Decision-path table: one test per row

    [Fact]
    public async Task Permit_RecordsGrantedWithThePermitReasonAndProceeds()
    {
        var recorder = new RecordingRecorder();
        var behavior = Behavior<PolicyARequest>(Pdp(Effect.Permit), Options(), recorder);

        var (result, next) = await SendAsync(behavior, new PolicyARequest());

        next.ShouldBeTrue();
        result.IsRight.ShouldBeTrue();
        var record = Single(recorder);
        record.EnforcedOutcome.ShouldBe(ABACEnforcedOutcome.Granted);
        record.ReasonCode.ShouldBe(ABACDecisionAuditSchema.PermitReasonCode);
        record.Effect.ShouldBe(Effect.Permit);
    }

    [Fact]
    public async Task PermitWithAMandatoryObligationNobodyHandles_RecordsDeniedAndDoesNotProceed()
    {
        var recorder = new RecordingRecorder();
        var obligation = new Obligation { Id = "ob-1", FulfillOn = FulfillOn.Permit, AttributeAssignments = [] };
        var behavior = Behavior<PolicyARequest>(
            Pdp(("policy-a", Decision(Effect.Permit, [obligation]))), Options(), recorder);

        var (result, next) = await SendAsync(behavior, new PolicyARequest());

        next.ShouldBeFalse();
        Code(result).ShouldBe(ABACErrors.ObligationFailedCode);
        var record = Single(recorder);
        record.EnforcedOutcome.ShouldBe(ABACEnforcedOutcome.Denied);
        record.ReasonCode.ShouldBe(ABACErrors.ObligationFailedCode);
        record.ObligationIds.ShouldBe(["ob-1"]);
    }

    [Fact]
    public async Task DenyInBlockMode_RecordsDeniedWithTheAccessDeniedCode()
    {
        var recorder = new RecordingRecorder();
        var behavior = Behavior<PolicyARequest>(Pdp(Effect.Deny), Options(), recorder);

        var (result, next) = await SendAsync(behavior, new PolicyARequest());

        next.ShouldBeFalse();
        Code(result).ShouldBe(ABACErrors.AccessDeniedCode);
        var record = Single(recorder);
        record.EnforcedOutcome.ShouldBe(ABACEnforcedOutcome.Denied);
        record.ReasonCode.ShouldBe(ABACErrors.AccessDeniedCode);
    }

    [Fact]
    public async Task DenyInWarnMode_RecordsNotEnforcedAndProceeds()
    {
        var recorder = new RecordingRecorder();
        var behavior = Behavior<PolicyARequest>(Pdp(Effect.Deny), Options(ABACEnforcementMode.Warn), recorder);

        var (result, next) = await SendAsync(behavior, new PolicyARequest());

        next.ShouldBeTrue();
        result.IsRight.ShouldBeTrue();
        var record = Single(recorder);
        record.EnforcedOutcome.ShouldBe(ABACEnforcedOutcome.DeniedNotEnforced);
        record.ReasonCode.ShouldBe(ABACErrors.AccessDeniedCode);
        record.EnforcementMode.ShouldBe(ABACEnforcementMode.Warn);
    }

    [Theory]
    [InlineData(ABACEnforcementMode.Block, ABACEnforcedOutcome.Denied, false)]
    [InlineData(ABACEnforcementMode.Warn, ABACEnforcedOutcome.DeniedNotEnforced, true)]
    public async Task UnknownRequiredPolicy_RecordsThePolicyNotFoundCode(
        ABACEnforcementMode mode, ABACEnforcedOutcome expected, bool proceeds)
    {
        var recorder = new RecordingRecorder();
        var behavior = Behavior<PolicyARequest>(Pdp(), Options(mode), recorder);

        var (_, next) = await SendAsync(behavior, new PolicyARequest());

        next.ShouldBe(proceeds);
        var record = Single(recorder);
        record.EnforcedOutcome.ShouldBe(expected);
        record.ReasonCode.ShouldBe(ABACErrors.RequiredPolicyNotFoundCode);
        record.PolicyId.ShouldBe("policy-a");
    }

    [Theory]
    [InlineData(ABACEnforcementMode.Block, ABACEnforcedOutcome.Denied, false)]
    [InlineData(ABACEnforcementMode.Warn, ABACEnforcedOutcome.DeniedNotEnforced, true)]
    public async Task ConditionThatIsNotMet_RecordsTheConditionNotMetCodeAndNamesTheConditionByIndex(
        ABACEnforcementMode mode, ABACEnforcedOutcome expected, bool proceeds)
    {
        var recorder = new RecordingRecorder();
        var attributes = Attributes(new Dictionary<string, object> { ["department"] = "Finance" });
        var behavior = Behavior<ConditionRequest>(Pdp(), Options(mode), recorder, attributes: attributes);

        var (_, next) = await SendAsync(behavior, new ConditionRequest());

        next.ShouldBe(proceeds);
        var record = Single(recorder);
        record.EnforcedOutcome.ShouldBe(expected);
        record.ReasonCode.ShouldBe(ABACErrors.ConditionNotMetCode);
        record.PolicyId.ShouldBe("condition:0");
        record.PolicyId!.ShouldNotContain("department");
    }

    [Theory]
    [InlineData(ABACEnforcementMode.Block)]
    [InlineData(ABACEnforcementMode.Warn)]
    public async Task IndeterminateDecision_IsDeniedInEveryModeAndRecordedWithTheIndeterminateCode(ABACEnforcementMode mode)
    {
        var recorder = new RecordingRecorder();
        var behavior = Behavior<PolicyARequest>(Pdp(Effect.Indeterminate), Options(mode), recorder);

        var (result, next) = await SendAsync(behavior, new PolicyARequest());

        next.ShouldBeFalse();
        Code(result).ShouldBe(ABACErrors.IndeterminateCode);
        var record = Single(recorder);
        record.EnforcedOutcome.ShouldBe(ABACEnforcedOutcome.Denied);
        record.ReasonCode.ShouldBe(ABACErrors.IndeterminateCode);
    }

    [Fact]
    public async Task ConditionThatCannotBeCompiled_IsIndeterminateAndNamedByIndex()
    {
        var recorder = new RecordingRecorder();
        var behavior = Behavior<UncompilableConditionRequest>(Pdp(), Options(), recorder);

        var (_, next) = await SendAsync(behavior, new UncompilableConditionRequest());

        next.ShouldBeFalse();
        var record = Single(recorder);
        record.ReasonCode.ShouldBe(ABACErrors.IndeterminateCode);
        record.PolicyId.ShouldBe("condition:0");
    }

    [Fact]
    public async Task PdpThatThrows_IsRecordedAsEvaluationFailedAndDenies()
    {
        var recorder = new RecordingRecorder();
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluatePolicyAsync(Arg.Any<string>(), Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, PolicyDecision>>>(_ => throw new InvalidOperationException("pdp secret"));
        var behavior = Behavior<PolicyARequest>(pdp, Options(ABACEnforcementMode.Warn), recorder);

        var (result, next) = await SendAsync(behavior, new PolicyARequest());

        next.ShouldBeFalse();
        Code(result).ShouldBe(ABACErrors.EvaluationFailedCode);
        var record = Single(recorder);
        record.EnforcedOutcome.ShouldBe(ABACEnforcedOutcome.Denied);
        record.ReasonCode.ShouldBe(ABACErrors.EvaluationFailedCode);
        record.Effect.ShouldBeNull();
        record.UserId.ShouldBe("alice");
    }

    [Fact]
    public async Task UnauthenticatedCaller_IsRecordedWithNoSubjectAndOnlyTheReasonCode()
    {
        var recorder = new RecordingRecorder();
        var behavior = Behavior<PolicyARequest>(Pdp(Effect.Permit), Options(), recorder);

        var (result, next) = await SendAsync(
            behavior, new PolicyARequest(), TestRequestContext.For(TestIdentity.Anonymous, correlationId: "corr-2"));

        next.ShouldBeFalse();
        Code(result).ShouldBe(EncinaErrorCodes.AuthorizationUnauthenticated);
        var record = Single(recorder);
        record.EnforcedOutcome.ShouldBe(ABACEnforcedOutcome.Denied);
        record.ReasonCode.ShouldBe(EncinaErrorCodes.AuthorizationUnauthenticated);
        record.UserId.ShouldBeNull();
        record.IdentityKind.ShouldBe(IdentityKind.Anonymous);
        record.CorrelationId.ShouldBe("corr-2");
        record.Effect.ShouldBeNull();
    }

    [Fact]
    public async Task CancellationBeforeTheRecordStep_RecordsNothingAndPropagates()
    {
        using var cts = new CancellationTokenSource();
        var recorder = new RecordingRecorder();
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluatePolicyAsync(Arg.Any<string>(), Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cts.Cancel();
                return ValueTask.FromResult(Right<EncinaError, PolicyDecision>(Decision(Effect.Permit)));
            });
        var behavior = Behavior<PolicyARequest>(pdp, Options(), recorder);

        await Should.ThrowAsync<OperationCanceledException>(
            () => SendAsync(behavior, new PolicyARequest(), cancellationToken: cts.Token));

        recorder.Records.ShouldBeEmpty();
    }

    [Fact]
    public async Task CancellationDuringTheWrite_DoesNotAbortItAndThenPropagates()
    {
        using var cts = new CancellationTokenSource();
        var recorder = new RecordingRecorder
        {
            Behavior = _ =>
            {
                cts.Cancel();
                return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
            }
        };
        var behavior = Behavior<PolicyARequest>(Pdp(Effect.Permit), Options(), recorder);
        var nextCalled = false;

        await Should.ThrowAsync<OperationCanceledException>(
            () => SendAsync(behavior, new PolicyARequest(), cancellationToken: cts.Token, onNext: () => nextCalled = true));

        recorder.Records.Count.ShouldBe(1);
        nextCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task TheWrite_IsNeverLinkedToTheClientToken()
    {
        using var cts = new CancellationTokenSource();
        var recorder = new RecordingRecorder();
        var behavior = Behavior<PolicyARequest>(Pdp(Effect.Permit), Options(), recorder);

        await SendAsync(behavior, new PolicyARequest(), cancellationToken: cts.Token);

        recorder.Tokens.Single().CanBeCanceled.ShouldBeFalse();
    }

    [RequirePolicy("or-1", AllMustPass = false)]
    [RequirePolicy("and-1")]
    private sealed record MixedGroupsRequest : IRequest<string>;

    [Fact]
    public async Task DenyFromMixedGroups_NamesTheFirstDenyingPolicyOfTheAllMustPassGroup()
    {
        var recorder = new RecordingRecorder();
        var behavior = Behavior<MixedGroupsRequest>(
            Pdp(("or-1", Decision(Effect.Deny)), ("and-1", Decision(Effect.Deny))), Options(), recorder);

        await SendAsync(behavior, new MixedGroupsRequest());

        Single(recorder).PolicyId.ShouldBe("and-1");
    }

    [RequirePolicy("policy-a")]
    private sealed class ThrowingResourceRequest : IRequest<string>, IABACResourceIdentity
    {
        public string? ResourceId => throw new InvalidOperationException("resource id broke");
    }

    [Fact]
    public async Task ARecordThatCannotBeBuilt_UnderFailClosed_DeniesAsAnAuditFailureInsteadOfEscaping()
    {
        var recorder = new RecordingRecorder();
        var behavior = Behavior<ThrowingResourceRequest>(Pdp(Effect.Permit), Options(), recorder);

        var (result, next) = await SendAsync(behavior, new ThrowingResourceRequest());

        next.ShouldBeFalse();
        Code(result).ShouldBe(ABACErrors.DecisionAuditFailedCode);
        result.IfLeft(error => error.GetDetails()["cause"].ShouldBe(nameof(InvalidOperationException)));
        recorder.Records.ShouldBeEmpty();
    }

    [Fact]
    public async Task ARecordThatCannotBeBuilt_UnderBestEffort_LogsAndLetsTheRequestProceed()
    {
        var logger = new FakeLogger<ABACPipelineBehavior<ThrowingResourceRequest, string>>();
        var behavior = Behavior<ThrowingResourceRequest>(
            Pdp(Effect.Permit),
            Options(failureMode: ABACDecisionAuditFailureMode.BestEffort),
            new RecordingRecorder(),
            logger: logger);

        var (result, next) = await SendAsync(behavior, new ThrowingResourceRequest());

        next.ShouldBeTrue();
        result.IsRight.ShouldBeTrue();
        logger.Collector.GetSnapshot().ShouldContain(r => r.Id.Id == 9081);
    }

    [Fact]
    public async Task ARecordThatCannotBeBuiltForADeniedRequest_KeepsTheOriginalDenialAndLogsIt()
    {
        var logger = new FakeLogger<ABACPipelineBehavior<ThrowingResourceRequest, string>>();
        var behavior = Behavior<ThrowingResourceRequest>(Pdp(Effect.Deny), Options(), new RecordingRecorder(), logger: logger);

        var (result, next) = await SendAsync(behavior, new ThrowingResourceRequest());

        next.ShouldBeFalse();
        Code(result).ShouldBe(ABACErrors.AccessDeniedCode);
        logger.Collector.GetSnapshot().ShouldContain(r => r.Id.Id == 9082);
    }

    [Fact]
    public async Task AnOutcomeTheFilterDrops_IsNeverBuiltSoABuildFailureCannotDenyIt()
    {
        var recorder = new RecordingRecorder();
        var behavior = Behavior<ThrowingResourceRequest>(
            Pdp(Effect.Permit), Options(outcomes: ABACDecisionAuditOutcomes.Denied), recorder);

        var (result, next) = await SendAsync(behavior, new ThrowingResourceRequest());

        next.ShouldBeTrue();
        result.IsRight.ShouldBeTrue();
        recorder.Records.ShouldBeEmpty();
    }

    #endregion

    #region Audit write failures

    private static RecordingRecorder FailingRecorder() => new()
    {
        Behavior = _ => ValueTask.FromResult(Left<EncinaError, Unit>(
            EncinaErrors.Create("store.down", "store message that must not travel")))
    };

    [Theory]
    [InlineData(ABACEnforcementMode.Block)]
    [InlineData(ABACEnforcementMode.Warn)]
    public async Task WriteFailureUnderFailClosed_DeniesARequestThatWouldProceed(ABACEnforcementMode mode)
    {
        var effect = mode == ABACEnforcementMode.Block ? Effect.Permit : Effect.Deny;
        var behavior = Behavior<PolicyARequest>(Pdp(effect), Options(mode), FailingRecorder());

        var (result, next) = await SendAsync(behavior, new PolicyARequest());

        next.ShouldBeFalse();
        Code(result).ShouldBe(ABACErrors.DecisionAuditFailedCode);
        result.IfLeft(error =>
        {
            error.GetDetails()["cause"].ShouldBe("store.down");
            error.Message.ShouldNotContain("store message");
        });
    }

    [Fact]
    public async Task WriteFailureUnderBestEffort_LetsTheRequestProceed()
    {
        var logger = new FakeLogger<ABACPipelineBehavior<PolicyARequest, string>>();
        var behavior = Behavior<PolicyARequest>(
            Pdp(Effect.Permit), Options(failureMode: ABACDecisionAuditFailureMode.BestEffort), FailingRecorder(), logger: logger);

        var (result, next) = await SendAsync(behavior, new PolicyARequest());

        next.ShouldBeTrue();
        result.IsRight.ShouldBeTrue();
        logger.Collector.GetSnapshot().ShouldContain(r => r.Id.Id == 9081);
    }

    [Theory]
    [InlineData(ABACDecisionAuditFailureMode.FailClosed)]
    [InlineData(ABACDecisionAuditFailureMode.BestEffort)]
    public async Task WriteFailureOfAnAlreadyDeniedRequest_KeepsTheOriginalDenial(ABACDecisionAuditFailureMode mode)
    {
        var logger = new FakeLogger<ABACPipelineBehavior<PolicyARequest, string>>();
        var behavior = Behavior<PolicyARequest>(Pdp(Effect.Deny), Options(failureMode: mode), FailingRecorder(), logger: logger);

        var (result, next) = await SendAsync(behavior, new PolicyARequest());

        next.ShouldBeFalse();
        Code(result).ShouldBe(ABACErrors.AccessDeniedCode);
        logger.Collector.GetSnapshot().ShouldContain(r => r.Id.Id == 9082);
    }

    [Fact]
    public async Task RecorderThatThrows_FailsClosedAndLeaksNoExceptionOrErrorMessage()
    {
        var statuses = new List<string?>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Encina.Security.ABAC",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => statuses.Add(activity.StatusDescription)
        };
        ActivitySource.AddActivityListener(listener);

        var logger = new FakeLogger<ABACPipelineBehavior<PolicyARequest, string>>();
        var recorder = new RecordingRecorder { Behavior = _ => throw new InvalidOperationException("recorder boom secret") };
        var behavior = Behavior<PolicyARequest>(Pdp(Effect.Permit), Options(), recorder, logger: logger);

        var (result, next) = await SendAsync(behavior, new PolicyARequest());

        next.ShouldBeFalse();
        Code(result).ShouldBe(ABACErrors.DecisionAuditFailedCode);
        result.IfLeft(error =>
        {
            error.GetDetails()["cause"].ShouldBe(nameof(InvalidOperationException));
            error.Message.ShouldNotContain("secret");
        });

        var records = logger.Collector.GetSnapshot();
        records.ShouldContain(r => r.Id.Id == 9083);
        records.ShouldContain(r => r.Id.Id == 9080);
        records.ShouldAllBe(r => !r.Message.Contains("secret") && (r.Exception == null || !r.Exception.ToString().Contains("secret")));
        statuses.ShouldAllBe(status => status == null || !status.Contains("secret"));
    }

    #endregion

    #region Telemetry follows the enforced outcome

    private sealed record Observed(Activity? Evaluate, long Permitted, long Denied, long Indeterminate);

    // Runs the action while listening to the ABAC evaluation span and its outcome counters.
    private static async Task<Observed> ObserveAsync(Func<Task> action)
    {
        var activities = new List<Activity>();
        long permitted = 0, denied = 0, indeterminate = 0;

        using var activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Encina.Security.ABAC",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activities.Add
        };
        ActivitySource.AddActivityListener(activityListener);

        using var meterListener = new System.Diagnostics.Metrics.MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "Encina.Security.ABAC" && instrument.Name.StartsWith("abac.evaluation.", StringComparison.Ordinal))
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, value, _, _) =>
        {
            switch (instrument.Name)
            {
                case "abac.evaluation.permitted": permitted += value; break;
                case "abac.evaluation.denied": denied += value; break;
                case "abac.evaluation.indeterminate": indeterminate += value; break;
            }
        });
        meterListener.Start();

        await action();

        return new Observed(activities.SingleOrDefault(a => a.OperationName == "ABAC.Evaluate"), permitted, denied, indeterminate);
    }

    [Fact]
    public async Task APermitThatTheAuditWriteDenies_ReportsADenialWithTheAuditFailureReason()
    {
        var behavior = Behavior<PolicyARequest>(Pdp(Effect.Permit), Options(), FailingRecorder());

        var observed = await ObserveAsync(() => SendAsync(behavior, new PolicyARequest()));

        observed.Evaluate.ShouldNotBeNull();
        observed.Evaluate.GetTagItem("abac.effect").ShouldBe("deny");
        observed.Evaluate.Status.ShouldBe(ActivityStatusCode.Error);
        observed.Evaluate.StatusDescription.ShouldBe(ABACErrors.DecisionAuditFailedCode);
        (observed.Permitted, observed.Denied).ShouldBe((0L, 1L));
    }

    [Fact]
    public async Task AWarnPassThroughThatTheAuditWriteDenies_ReportsADenial()
    {
        var logger = new FakeLogger<ABACPipelineBehavior<PolicyARequest, string>>();
        var behavior = Behavior<PolicyARequest>(Pdp(Effect.Deny), Options(ABACEnforcementMode.Warn), FailingRecorder(), logger: logger);

        var observed = await ObserveAsync(() => SendAsync(behavior, new PolicyARequest()));

        observed.Evaluate!.StatusDescription.ShouldBe(ABACErrors.DecisionAuditFailedCode);
        (observed.Permitted, observed.Denied).ShouldBe((0L, 1L));
        logger.Collector.GetSnapshot().ShouldNotContain(r => r.Message.Contains("proceeds in Warn", StringComparison.Ordinal));
    }

    [Fact]
    public async Task APermitThatIsRecordedOrWrittenBestEffort_StaysAPermit()
    {
        var recorded = Behavior<PolicyARequest>(Pdp(Effect.Permit), Options(), new RecordingRecorder());
        var bestEffort = Behavior<PolicyARequest>(
            Pdp(Effect.Permit), Options(failureMode: ABACDecisionAuditFailureMode.BestEffort), FailingRecorder());

        var first = await ObserveAsync(() => SendAsync(recorded, new PolicyARequest()));
        var second = await ObserveAsync(() => SendAsync(bestEffort, new PolicyARequest()));

        first.Evaluate!.GetTagItem("abac.effect").ShouldBe("permit");
        second.Evaluate!.GetTagItem("abac.effect").ShouldBe("permit");
        (first.Permitted, first.Denied, second.Permitted, second.Denied).ShouldBe((1L, 0L, 1L, 0L));
    }

    [Fact]
    public async Task AnIndeterminateDecision_IsCountedAsIndeterminateAfterTheRecordStep()
    {
        var behavior = Behavior<PolicyARequest>(Pdp(Effect.Indeterminate), Options(), new RecordingRecorder());

        var observed = await ObserveAsync(() => SendAsync(behavior, new PolicyARequest()));

        observed.Evaluate!.GetTagItem("abac.effect").ShouldBe("indeterminate");
        (observed.Permitted, observed.Denied, observed.Indeterminate).ShouldBe((0L, 0L, 1L));
    }

    [Fact]
    public async Task ADeniedRequestWhoseAuditWriteFails_KeepsTheOriginalDenialInTelemetry()
    {
        var behavior = Behavior<PolicyARequest>(Pdp(Effect.Deny), Options(), FailingRecorder());

        var observed = await ObserveAsync(() => SendAsync(behavior, new PolicyARequest()));

        observed.Evaluate!.StatusDescription.ShouldBe("A required policy did not permit the request.");
        (observed.Denied).ShouldBe(1L);
    }

    #endregion

    #region Filters, ordering and the clock

    [Theory]
    [InlineData(ABACDecisionAuditOutcomes.Denied, Effect.Permit, 0)]
    [InlineData(ABACDecisionAuditOutcomes.Granted, Effect.Permit, 1)]
    [InlineData(ABACDecisionAuditOutcomes.Granted, Effect.Deny, 0)]
    [InlineData(ABACDecisionAuditOutcomes.Denied, Effect.Deny, 1)]
    [InlineData(ABACDecisionAuditOutcomes.None, Effect.Deny, 0)]
    public async Task OutcomesFilter_SelectsWhichEnforcedOutcomesAreRecorded(
        ABACDecisionAuditOutcomes outcomes, Effect effect, int expectedRecords)
    {
        var recorder = new RecordingRecorder();
        var behavior = Behavior<PolicyARequest>(Pdp(effect), Options(outcomes: outcomes), recorder);

        await SendAsync(behavior, new PolicyARequest());

        recorder.Records.Count.ShouldBe(expectedRecords);
    }

    [Fact]
    public async Task OutcomesFilterNotEnforced_RecordsOnlyWarnModeDenials()
    {
        var recorder = new RecordingRecorder();
        var behavior = Behavior<PolicyARequest>(
            Pdp(Effect.Deny), Options(ABACEnforcementMode.Warn, outcomes: ABACDecisionAuditOutcomes.NotEnforced), recorder);

        await SendAsync(behavior, new PolicyARequest());

        Single(recorder).EnforcedOutcome.ShouldBe(ABACEnforcedOutcome.DeniedNotEnforced);
    }

    [Fact]
    public async Task OutcomesFilter_NeverHidesAnIndeterminateAnExceptionOrAnUnauthenticatedCaller()
    {
        var recorder = new RecordingRecorder();
        var options = Options(outcomes: ABACDecisionAuditOutcomes.None);

        await SendAsync(Behavior<PolicyARequest>(Pdp(Effect.Indeterminate), options, recorder), new PolicyARequest());
        await SendAsync(
            Behavior<PolicyARequest>(Pdp(Effect.Permit), options, recorder),
            new PolicyARequest(),
            TestRequestContext.For(TestIdentity.Anonymous));

        recorder.Records.Select(r => r.ReasonCode)
            .ShouldBe([ABACErrors.IndeterminateCode, EncinaErrorCodes.AuthorizationUnauthenticated]);
    }

    [Fact]
    public async Task TheRecordIsWrittenBeforeTheHandlerRuns()
    {
        var recorder = new RecordingRecorder();
        var recordedWhenHandlerRan = 0;
        var behavior = Behavior<PolicyARequest>(Pdp(Effect.Permit), Options(), recorder);

        await SendAsync(behavior, new PolicyARequest(), onNext: () => recordedWhenHandlerRan = recorder.Records.Count);

        recordedWhenHandlerRan.ShouldBe(1);
    }

    [Fact]
    public async Task AHandlerException_PropagatesUnchangedAndIsNotReportedAsAnEvaluationFailure()
    {
        var recorder = new RecordingRecorder();
        var behavior = Behavior<PolicyARequest>(Pdp(Effect.Permit), Options(), recorder);

        var thrown = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await behavior.Handle(
                new PolicyARequest(),
                User(),
                () => throw new InvalidOperationException("handler broke"),
                CancellationToken.None));

        thrown.Message.ShouldBe("handler broke");
        Single(recorder).ReasonCode.ShouldBe(ABACDecisionAuditSchema.PermitReasonCode);
    }

    [Fact]
    public async Task DisabledAudit_BuildsNothingReadsNoClockAndCallsNoRecorder()
    {
        var recorder = new RecordingRecorder();
        var clock = new CountingTimeProvider(Start);
        PolicyEvaluationContext? seen = null;
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluatePolicyAsync(Arg.Any<string>(), Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                seen = call.ArgAt<PolicyEvaluationContext>(1);
                return ValueTask.FromResult(Right<EncinaError, PolicyDecision>(Decision(Effect.Permit)));
            });
        var behavior = Behavior<PolicyARequest>(pdp, Options(enabled: false), recorder, clock);

        var (result, next) = await SendAsync(behavior, new PolicyARequest());

        next.ShouldBeTrue();
        result.IsRight.ShouldBeTrue();
        recorder.Records.ShouldBeEmpty();
        clock.Reads.ShouldBe(0);
        seen!.IncludeEvaluationTrace.ShouldBeFalse();
    }

    [Fact]
    public async Task EnabledAudit_ReadsTheClockOncePerEndAndStampsTheRecordFromTheTwoReads()
    {
        var recorder = new RecordingRecorder();
        var clock = new CountingTimeProvider(Start);
        var behavior = Behavior<PolicyARequest>(Pdp(Effect.Permit), Options(), recorder, clock);

        await SendAsync(behavior, new PolicyARequest());

        clock.Reads.ShouldBe(2);
        var record = Single(recorder);
        record.StartedAtUtc.ShouldBe(Start.AddSeconds(1));
        record.CompletedAtUtc.ShouldBe(Start.AddSeconds(2));
        record.DecisionId.Version.ShouldBe(7);
    }

    #endregion

    #region Record content

    [Fact]
    public async Task Record_CarriesIdentityContextRequestAndDecidingPolicy()
    {
        var recorder = new RecordingRecorder();
        var context = TestRequestContext.For(TestIdentity.User("alice"), tenantId: "tenant-9", correlationId: "corr-9")
            .WithMetadata("Encina.ModuleName", "Orders")
            .WithMetadata("Encina.Audit.IpAddress", "10.0.0.1")
            .WithMetadata("Encina.Audit.UserAgent", "agent/1");
        var pdp = Pdp(("policy-a", Decision(Effect.Permit, ruleId: "rule-7")));
        var behavior = Behavior<PolicyARequest>(pdp, Options(), recorder);

        await SendAsync(behavior, new PolicyARequest("order-5"), context);

        var record = Single(recorder);
        record.UserId.ShouldBe("alice");
        record.IdentityKind.ShouldBe(IdentityKind.User);
        record.TenantId.ShouldBe("tenant-9");
        record.CorrelationId.ShouldBe("corr-9");
        record.ModuleId.ShouldBe("Orders");
        record.IpAddress.ShouldBe("10.0.0.1");
        record.UserAgent.ShouldBe("agent/1");
        record.RequestType.ShouldBe(nameof(PolicyARequest));
        record.ResourceId.ShouldBe("order-5");
        record.PolicyId.ShouldBe("policy-a");
        record.RuleId.ShouldBe("rule-7");
    }

    [Fact]
    public async Task Record_OfAServiceIdentity_KeepsTheServicePrefixAndKind()
    {
        var recorder = new RecordingRecorder();
        var behavior = Behavior<PolicyARequest>(Pdp(Effect.Permit), Options(), recorder);

        await SendAsync(behavior, new PolicyARequest(), TestRequestContext.For(TestIdentity.Service("billing")));

        var record = Single(recorder);
        record.IdentityKind.ShouldBe(IdentityKind.Service);
        record.UserId.ShouldBe("service:billing");
    }

    [Fact]
    public async Task ResourceId_ComesFromTheDeclaredIdentityThenTheConfiguredAttributeAndOtherwiseStaysEmpty()
    {
        var recorder = new RecordingRecorder();
        var withAttribute = Attributes(resource: new Dictionary<string, object> { ["resourceId"] = 42 });
        var options = Options();

        await SendAsync(Behavior<PolicyARequest>(Pdp(Effect.Permit), options, recorder, attributes: withAttribute), new PolicyARequest("declared"));
        await SendAsync(Behavior<AttributeIdRequest>(Pdp(Effect.Permit), options, recorder, attributes: withAttribute), new AttributeIdRequest());
        await SendAsync(Behavior<PolicyARequest>(Pdp(Effect.Permit), options, recorder), new PolicyARequest());

        recorder.Records.Select(r => r.ResourceId).ShouldBe(["declared", "42", null]);
    }

    [Fact]
    public async Task AttributeNames_ExcludeTheBuiltInSubjectAttributesAndValuesAreRecordedOnlyOnTheAllowList()
    {
        var recorder = new RecordingRecorder();
        var options = Options();
        options.DecisionAudit.RecordedAttributeValues.Add("department");
        var attributes = Attributes(
            subject: new Dictionary<string, object> { ["department"] = "HR", ["clearance"] = 3 },
            resource: new Dictionary<string, object> { ["owner"] = "bob" },
            environment: new Dictionary<string, object> { ["hour"] = 9 });
        var behavior = Behavior<PolicyARequest>(Pdp(Effect.Permit), options, recorder, attributes: attributes);

        await SendAsync(behavior, new PolicyARequest());

        var record = Single(recorder);
        record.AttributeNames[AttributeCategory.Subject].ShouldBe(["clearance", "department"]);
        record.AttributeNames[AttributeCategory.Resource].ShouldBe(["owner"]);
        record.AttributeNames[AttributeCategory.Environment].ShouldBe(["hour"]);
        record.AttributeNames[AttributeCategory.Subject].ShouldNotContain(ABACSubjectAttributes.SubjectId);
        record.AttributeNames[AttributeCategory.Subject].ShouldNotContain(ABACSubjectAttributes.IdentityKind);
        record.RecordedValues.ShouldBe(new Dictionary<string, string> { ["department"] = "HR" });
    }

    #endregion

    #region Trace across several PDP calls

    private static PolicyEvaluationTrace Node(string id, params PolicyEvaluationTrace[] children) => new()
    {
        PolicyId = id,
        IsPolicySet = children.Length > 0,
        Effect = Effect.Permit,
        Reason = PolicyTraceReason.Evaluated,
        Children = children
    };

    [Fact]
    public async Task TraceBudget_CoversTheWholeRecord_EachPdpCallGetsWhatRemainsAndTruncationIsOred()
    {
        var recorder = new RecordingRecorder();
        var budgets = new List<int>();
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluatePolicyAsync(Arg.Any<string>(), Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                budgets.Add(call.ArgAt<PolicyEvaluationContext>(1).MaxTraceEntries);
                var first = call.ArgAt<string>(0) == "policy-a";
                return ValueTask.FromResult(Right<EncinaError, PolicyDecision>(first
                    ? Decision(Effect.Permit, trace: [Node("a", Node("a1"))])
                    : Decision(Effect.Permit, trace: [Node("b")], truncated: true)));
            });
        var behavior = Behavior<TwoPoliciesRequest>(pdp, Options(maxTrace: 3), recorder);

        await SendAsync(behavior, new TwoPoliciesRequest());

        budgets.ShouldBe([3, 1]);
        var record = Single(recorder);
        record.EvaluatedPolicies.Select(n => n.PolicyId).ShouldBe(["a", "b"]);
        record.TraceTruncated.ShouldBeTrue();
    }

    [Fact]
    public async Task TraceBudget_ShrinksToNothingAndTheCollectorDropsTheLeavesItCannotAfford()
    {
        var recorder = new RecordingRecorder();
        var pdp = Pdp(("policy-a", Decision(Effect.Permit, trace: [Node("a")])));
        var behavior = Behavior<PolicyAndConditionRequest>(pdp, Options(maxTrace: 1), recorder);

        await SendAsync(behavior, new PolicyAndConditionRequest());

        var record = Single(recorder);
        record.EvaluatedPolicies.Select(n => n.PolicyId).ShouldBe(["a"]);
        record.TraceTruncated.ShouldBeTrue();
    }

    [Fact]
    public async Task ConditionsSkippedByAnEarlierFailure_AreRecordedAsNotEvaluatedInDeclarationOrder()
    {
        var recorder = new RecordingRecorder();
        var behavior = Behavior<TwoConditionsRequest>(Pdp(), Options(), recorder);

        // user.department is HR: condition 0 passes, condition 1 fails, condition 2 never runs.
        await SendAsync(behavior, new TwoConditionsRequest());

        var trace = Single(recorder).EvaluatedPolicies;
        trace.Select(n => (n.PolicyId, n.Effect, n.Reason)).ShouldBe(
        [
            ("condition:0", Effect.Permit, PolicyTraceReason.Evaluated),
            ("condition:1", Effect.Deny, PolicyTraceReason.Evaluated),
            ("condition:2", Effect.NotApplicable, PolicyTraceReason.NotEvaluated)
        ]);
        Single(recorder).PolicyId.ShouldBe("condition:1");
    }

    [Fact]
    public async Task ConditionsSkippedBecauseAPolicyDenied_AreNotEvaluatedAndAMissingPolicyHasANode()
    {
        var recorder = new RecordingRecorder();
        var behavior = Behavior<PolicyAndConditionRequest>(Pdp(), Options(), recorder);

        await SendAsync(behavior, new PolicyAndConditionRequest());

        Single(recorder).EvaluatedPolicies.Select(n => (n.PolicyId, n.Reason)).ShouldBe(
        [
            ("policy-a", PolicyTraceReason.NotEvaluated),
            ("condition:0", PolicyTraceReason.NotEvaluated)
        ]);
    }

    [Fact]
    public async Task APolicyThatCannotBeRead_IsAnIndeterminateNotEvaluatedNode()
    {
        var recorder = new RecordingRecorder();
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluatePolicyAsync(Arg.Any<string>(), Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Left<EncinaError, PolicyDecision>(EncinaErrors.Create("store.down", "x"))));
        var behavior = Behavior<PolicyARequest>(pdp, Options(), recorder);

        await SendAsync(behavior, new PolicyARequest());

        var record = Single(recorder);
        record.ReasonCode.ShouldBe(ABACErrors.IndeterminateCode);
        record.EvaluatedPolicies.Single().ShouldSatisfyAllConditions(
            n => n.Effect.ShouldBe(Effect.Indeterminate),
            n => n.Reason.ShouldBe(PolicyTraceReason.NotEvaluated));
        record.PolicyId.ShouldBe("policy-a");
    }

    [Fact]
    public async Task TraceDisabledByOption_AsksThePdpForNoTrace()
    {
        var recorder = new RecordingRecorder();
        var options = Options();
        options.DecisionAudit.IncludeEvaluationTrace = false;
        PolicyEvaluationContext? seen = null;
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluatePolicyAsync(Arg.Any<string>(), Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                seen = call.ArgAt<PolicyEvaluationContext>(1);
                return ValueTask.FromResult(Right<EncinaError, PolicyDecision>(Decision(Effect.Permit)));
            });
        var behavior = Behavior<PolicyARequest>(pdp, options, recorder);

        await SendAsync(behavior, new PolicyARequest());

        seen!.IncludeEvaluationTrace.ShouldBeFalse();
        Single(recorder).EvaluatedPolicies.ShouldBeEmpty();
    }

    #endregion

    #region Registration

    [Fact]
    public void UnavailableRecorder_AlwaysFailsSoAnEnabledAuditWithoutADurableRecorderFailsClosed()
    {
        var provider = new Microsoft.Extensions.DependencyInjection.ServiceCollection()
            .AddEncinaABAC()
            .BuildServiceProvider();

        var recorder = provider.GetRequiredService<IABACDecisionRecorder>();
        var record = new ABACDecisionRecord
        {
            DecisionId = Guid.CreateVersion7(),
            IdentityKind = IdentityKind.User,
            CorrelationId = "c",
            RequestType = "R",
            EnforcedOutcome = ABACEnforcedOutcome.Granted,
            ReasonCode = ABACDecisionAuditSchema.PermitReasonCode,
            EnforcementMode = ABACEnforcementMode.Block,
            StartedAtUtc = Start,
            CompletedAtUtc = Start
        };

        var result = recorder.RecordAsync(record).AsTask().GetAwaiter().GetResult();

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetCode().IfNone(string.Empty).ShouldBe(ABACErrors.DecisionAuditStoreUnavailableCode));
        provider.GetRequiredService<TimeProvider>().ShouldNotBeNull();
    }

    #endregion
}

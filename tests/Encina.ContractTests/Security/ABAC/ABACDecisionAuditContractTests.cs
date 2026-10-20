#pragma warning disable CA2012 // Use ValueTasks correctly -- NSubstitute mock setup pattern

using Encina.Security.ABAC;
using Encina.Security.ABAC.DecisionAudit;
using Encina.Security.ABAC.EEL;
using Encina.Testing.Identity;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.ContractTests.Security.ABAC;

/// <summary>
/// Contract of the decision audit at the Policy Enforcement Point (#751 Phase 2): exactly one record
/// per evaluated request, written before the handler runs, and a request that would proceed never
/// proceeds without its record under the fail-closed default.
/// </summary>
[Trait("Category", "Contract")]
[Trait("Feature", "ABAC")]
public sealed class ABACDecisionAuditContractTests
{
    private static readonly EELCompiler Compiler = new();

    [RequirePolicy("test-policy")]
    private sealed record ProtectedCommand : ICommand<string>;

    private sealed record UnprotectedCommand : ICommand<string>;

    private sealed class CapturingRecorder : IABACDecisionRecorder
    {
        public List<ABACDecisionRecord> Records { get; } = [];

        public Func<ValueTask<Either<EncinaError, Unit>>> Outcome { get; set; } =
            () => ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));

        public ValueTask<Either<EncinaError, Unit>> RecordAsync(ABACDecisionRecord record, CancellationToken cancellationToken = default)
        {
            Records.Add(record);
            return Outcome();
        }
    }

    private static ABACPipelineBehavior<TRequest, string> Behavior<TRequest>(
        Effect effect,
        CapturingRecorder recorder,
        ABACOptions options)
        where TRequest : IRequest<string>
    {
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluatePolicyAsync("test-policy", Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, PolicyDecision>(new PolicyDecision
            {
                Effect = effect,
                Obligations = [],
                Advice = [],
                EvaluationDuration = TimeSpan.FromMilliseconds(1)
            })));

        var attributes = Substitute.For<IAttributeProvider>();
        attributes.GetSubjectAttributesAsync(Arg.Any<RequestIdentity>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object>());
        attributes.GetResourceAttributesAsync(Arg.Any<object>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(new Dictionary<string, object>());
        attributes.GetEnvironmentAttributesAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object>());

        return new ABACPipelineBehavior<TRequest, string>(
            pdp,
            attributes,
            new ObligationExecutor([], NullLogger<ObligationExecutor>.Instance),
            Compiler,
            Options.Create(options),
            recorder,
            TimeProvider.System,
            NullLogger<ABACPipelineBehavior<TRequest, string>>.Instance);
    }

    private static ABACOptions Audit(
        ABACEnforcementMode mode = ABACEnforcementMode.Block,
        ABACDecisionAuditFailureMode failureMode = ABACDecisionAuditFailureMode.FailClosed)
    {
        var options = new ABACOptions { EnforcementMode = mode };
        options.DecisionAudit.Enabled = true;
        options.DecisionAudit.FailureMode = failureMode;
        return options;
    }

    private static async Task<(Either<EncinaError, string> Result, int RecordsAtHandler, bool HandlerRan)> SendAsync<TRequest>(
        ABACPipelineBehavior<TRequest, string> behavior,
        TRequest request,
        CapturingRecorder recorder,
        IRequestContext? context = null)
        where TRequest : IRequest<string>
    {
        var recordsAtHandler = -1;
        var ran = false;
        var result = await behavior.Handle(
            request,
            context ?? TestRequestContext.For(TestIdentity.User("contract-user")),
            () =>
            {
                ran = true;
                recordsAtHandler = recorder.Records.Count;
                return ValueTask.FromResult(Right<EncinaError, string>("handled"));
            },
            CancellationToken.None);

        return (result, recordsAtHandler, ran);
    }

    [Theory]
    [InlineData(Effect.Permit)]
    [InlineData(Effect.Deny)]
    [InlineData(Effect.NotApplicable)]
    [InlineData(Effect.Indeterminate)]
    public async Task EveryEvaluatedRequest_WritesExactlyOneRecord(Effect effect)
    {
        var recorder = new CapturingRecorder();
        var behavior = Behavior<ProtectedCommand>(effect, recorder, Audit());

        await SendAsync(behavior, new ProtectedCommand(), recorder);

        recorder.Records.Count.ShouldBe(1);
    }

    [Fact]
    public async Task AnUnauthenticatedRequest_WritesExactlyOneRecord()
    {
        var recorder = new CapturingRecorder();
        var behavior = Behavior<ProtectedCommand>(Effect.Permit, recorder, Audit());

        await SendAsync(behavior, new ProtectedCommand(), recorder, TestRequestContext.For(TestIdentity.Anonymous));

        recorder.Records.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ARequestWithoutAbacAttributes_WritesNoRecord()
    {
        var recorder = new CapturingRecorder();
        var behavior = Behavior<UnprotectedCommand>(Effect.Deny, recorder, Audit());

        var (result, _, ran) = await SendAsync(behavior, new UnprotectedCommand(), recorder);

        ran.ShouldBeTrue();
        result.IsRight.ShouldBeTrue();
        recorder.Records.ShouldBeEmpty();
    }

    [Fact]
    public async Task WhenEnforcementIsDisabled_NoDecisionIsMadeAndNoRecordIsWritten()
    {
        var recorder = new CapturingRecorder();
        var behavior = Behavior<ProtectedCommand>(Effect.Deny, recorder, Audit(ABACEnforcementMode.Disabled));

        var (_, _, ran) = await SendAsync(behavior, new ProtectedCommand(), recorder);

        ran.ShouldBeTrue();
        recorder.Records.ShouldBeEmpty();
    }

    [Fact]
    public async Task TheRecordIsWrittenBeforeTheHandlerRuns()
    {
        var recorder = new CapturingRecorder();
        var behavior = Behavior<ProtectedCommand>(Effect.Permit, recorder, Audit());

        var (_, recordsAtHandler, _) = await SendAsync(behavior, new ProtectedCommand(), recorder);

        recordsAtHandler.ShouldBe(1);
    }

    [Theory]
    [InlineData(Effect.Permit, ABACEnforcementMode.Block)]
    [InlineData(Effect.Deny, ABACEnforcementMode.Warn)]
    public async Task UnderFailClosed_ARequestThatWouldProceedNeverProceedsWithoutItsRecord(Effect effect, ABACEnforcementMode mode)
    {
        var recorder = new CapturingRecorder
        {
            Outcome = () => ValueTask.FromResult(Left<EncinaError, Unit>(EncinaErrors.Create("store.down", "down")))
        };
        var behavior = Behavior<ProtectedCommand>(effect, recorder, Audit(mode));

        var (result, _, ran) = await SendAsync(behavior, new ProtectedCommand(), recorder);

        ran.ShouldBeFalse();
        result.Match(Right: _ => "<right>", Left: e => e.GetCode().IfNone("<none>")).ShouldBe(ABACErrors.DecisionAuditFailedCode);
    }

    [Fact]
    public async Task UnderBestEffort_AFailedWriteDoesNotStopTheRequest()
    {
        var recorder = new CapturingRecorder
        {
            Outcome = () => throw new InvalidOperationException("down")
        };
        var behavior = Behavior<ProtectedCommand>(
            Effect.Permit, recorder, Audit(failureMode: ABACDecisionAuditFailureMode.BestEffort));

        var (result, _, ran) = await SendAsync(behavior, new ProtectedCommand(), recorder);

        ran.ShouldBeTrue();
        result.IsRight.ShouldBeTrue();
    }
}

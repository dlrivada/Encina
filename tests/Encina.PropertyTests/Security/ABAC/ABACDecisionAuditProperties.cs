#pragma warning disable CA2012 // Use ValueTasks correctly -- NSubstitute mock setup pattern

using Encina.Security.ABAC;
using Encina.Security.ABAC.DecisionAudit;
using Encina.Security.ABAC.EEL;
using Encina.Testing.Identity;

using FsCheck;
using FsCheck.Xunit;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.PropertyTests.Security.ABAC;

/// <summary>
/// Invariants of the decision audit at the Policy Enforcement Point (#751 Phase 2): the enforced
/// outcome of a record is a total function of the decision and the mode, a request proceeds only when
/// it was granted or Warn mode let a definite denial through, and a failed write never lets a request
/// proceed under FailClosed.
/// </summary>
public sealed class ABACDecisionAuditProperties
{
    private static readonly EELCompiler Compiler = new();
    private static readonly Effect[] Effects = [Effect.Permit, Effect.Deny, Effect.NotApplicable, Effect.Indeterminate];

    [RequirePolicy("policy-a")]
    private sealed record GuardedRequest : IRequest<string>;

    private sealed class Recorder(int writeOutcome) : IABACDecisionRecorder
    {
        public List<ABACDecisionRecord> Records { get; } = [];

        public ValueTask<Either<EncinaError, Unit>> RecordAsync(ABACDecisionRecord record, CancellationToken cancellationToken = default)
        {
            Records.Add(record);
            return writeOutcome switch
            {
                0 => ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default)),
                1 => ValueTask.FromResult(Left<EncinaError, Unit>(EncinaErrors.Create("store.down", "down"))),
                _ => throw new InvalidOperationException("down")
            };
        }
    }

    private static (bool Proceeds, Recorder Recorder) Run(
        byte effectCode, bool warn, bool bestEffort, byte writeCode, byte outcomesCode, bool enabled, string? statusMessage = null)
    {
        var effect = Effects[effectCode % Effects.Length];
        var recorder = new Recorder(writeCode % 3);

        var options = new ABACOptions { EnforcementMode = warn ? ABACEnforcementMode.Warn : ABACEnforcementMode.Block };
        options.DecisionAudit.Enabled = enabled;
        options.DecisionAudit.FailureMode = bestEffort ? ABACDecisionAuditFailureMode.BestEffort : ABACDecisionAuditFailureMode.FailClosed;
        options.DecisionAudit.Outcomes = (ABACDecisionAuditOutcomes)(outcomesCode % 8);

        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluatePolicyAsync("policy-a", Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, PolicyDecision>(new PolicyDecision
            {
                Effect = effect,
                Obligations = [],
                Advice = [],
                EvaluationDuration = TimeSpan.Zero,
                Status = statusMessage is null
                    ? null
                    : new DecisionStatus { StatusCode = "processing-error", StatusMessage = statusMessage }
            })));

        var attributes = Substitute.For<IAttributeProvider>();
        attributes.GetSubjectAttributesAsync(Arg.Any<RequestIdentity>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object>());
        attributes.GetResourceAttributesAsync(Arg.Any<object>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(new Dictionary<string, object>());
        attributes.GetEnvironmentAttributesAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object>());

        var behavior = new ABACPipelineBehavior<GuardedRequest, string>(
            pdp,
            attributes,
            new ObligationExecutor([], NullLogger<ObligationExecutor>.Instance),
            Compiler,
            Options.Create(options),
            recorder,
            TimeProvider.System,
            NullLogger<ABACPipelineBehavior<GuardedRequest, string>>.Instance);

        var result = behavior.Handle(
                new GuardedRequest(),
                TestRequestContext.For(TestIdentity.User("property-user")),
                () => ValueTask.FromResult(Right<EncinaError, string>("handled")),
                CancellationToken.None)
            .AsTask().GetAwaiter().GetResult();

        return (result.IsRight, recorder);
    }

    // Permit proceeds; a required policy that is not applicable is a Deny verdict, which only Warn
    // mode lets through; Indeterminate never proceeds.
    private static bool ProceedsWithoutAudit(Effect effect, bool warn) =>
        effect == Effect.Permit || (warn && effect is Effect.Deny or Effect.NotApplicable);

    // The sentinel prefix is what the assertion looks for, so a random suffix cannot collide with the
    // fixed text of the record by chance.
    [Property(MaxTest = 200)]
    public bool AStatusMessageOfTheDecision_NeverAppearsInTheRecordOrTheStoredEntry(
        byte effectCode, bool warn, bool bestEffort, NonEmptyString suffix)
    {
        const string prefix = "STATUS-MESSAGE-SENTINEL:";
        var (_, recorder) = Run(effectCode, warn, bestEffort, writeCode: 0, outcomesCode: 7, enabled: true, statusMessage: prefix + suffix.Get);

        if (recorder.Records.Count != 1)
        {
            return false;
        }

        var record = recorder.Records[0];
        var entry = ABACDecisionAuditEntryMapper.ToOperationAuditEntry(record);
        // The whole entry, every column and every metadata value.
        var stored = System.Text.Json.JsonSerializer.Serialize(entry);

        return !stored.Contains(prefix, StringComparison.Ordinal)
            && !record.ToString().Contains(prefix, StringComparison.Ordinal)
            && !record.ReasonCode.Contains(prefix, StringComparison.Ordinal);
    }

    [Property(MaxTest = 300)]
    public bool AuditDisabled_NeverWritesAndDecidesAsWithoutAudit(byte effectCode, bool warn, bool bestEffort, byte writeCode, byte outcomesCode)
    {
        var (proceeds, recorder) = Run(effectCode, warn, bestEffort, writeCode, outcomesCode, enabled: false);

        return recorder.Records.Count == 0
            && proceeds == ProceedsWithoutAudit(Effects[effectCode % Effects.Length], warn);
    }

    [Property(MaxTest = 300)]
    public bool AtMostOneRecordPerRequest_AndItsOutcomeIsTotalOverTheDecisionAndMode(
        byte effectCode, bool warn, bool bestEffort, byte writeCode, byte outcomesCode)
    {
        var effect = Effects[effectCode % Effects.Length];
        var (_, recorder) = Run(effectCode, warn, bestEffort, writeCode, outcomesCode, enabled: true);

        if (recorder.Records.Count > 1)
        {
            return false;
        }

        if (recorder.Records.Count == 0)
        {
            return true;
        }

        var expected = effect switch
        {
            Effect.Permit => ABACEnforcedOutcome.Granted,
            Effect.Indeterminate => ABACEnforcedOutcome.Denied,
            _ => warn ? ABACEnforcedOutcome.DeniedNotEnforced : ABACEnforcedOutcome.Denied
        };

        return recorder.Records[0].EnforcedOutcome == expected;
    }

    [Property(MaxTest = 300)]
    public bool ARequestProceedsOnlyWhenGrantedOrWarnPassedAndAFailedWriteNeverProceedsUnderFailClosed(
        byte effectCode, bool warn, bool bestEffort, byte writeCode, byte outcomesCode)
    {
        var effect = Effects[effectCode % Effects.Length];
        var (proceeds, recorder) = Run(effectCode, warn, bestEffort, writeCode, outcomesCode, enabled: true);

        var wouldProceed = ProceedsWithoutAudit(effect, warn);
        var wroteAndFailed = recorder.Records.Count == 1 && writeCode % 3 != 0;
        var expected = wouldProceed && !(wroteAndFailed && !bestEffort);

        return proceeds == expected;
    }
}

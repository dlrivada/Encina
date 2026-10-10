#pragma warning disable CA2012 // Use ValueTasks correctly -- NSubstitute mock setup pattern

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;

using Encina.Security.ABAC;
using Encina.Security.ABAC.DecisionAudit;
using Encina.Security.ABAC.EEL;
using Encina.Testing.Identity;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.ABAC.DecisionAudit;

/// <summary>
/// The observability of the decision audit path (#751 Phase 6): the recorded and failed counters, the
/// duration histogram, the <c>ABAC.DecisionAudit.Record</c> span, the <c>abac.decision_id</c> tag and
/// the rule that no tag or log carries a subject, resource, tenant, attribute value or error message.
/// </summary>
[Collection(ABACActivityListenerIsolation.Name)]
public sealed class ABACDecisionAuditObservabilityTests
{
    private const string MeterName = "Encina.Security.ABAC";
    private const string SentinelSubject = "SENTINEL-subject-id";
    private const string SentinelTenant = "SENTINEL-tenant-id";
    private const string SentinelResource = "SENTINEL-resource-id";
    private const string SentinelAttribute = "SENTINEL-attribute-value";
    private const string SentinelMessage = "SENTINEL-error-message";

    [RequirePolicy("policy-a")]
    private sealed record PolicyARequest(string? Id = null) : IRequest<string>, IABACResourceIdentity
    {
        public string? ResourceId => Id;
    }

    private sealed record Measurement(string Instrument, double Value, IReadOnlyDictionary<string, object?> Tags);

    private sealed class Telemetry : IDisposable
    {
        private readonly MeterListener _meters = new();
        private readonly ActivityListener _activities;

        public Telemetry()
        {
            _meters.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == MeterName)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };
            _meters.SetMeasurementEventCallback<long>((instrument, value, tags, _) => Add(instrument, value, tags));
            _meters.SetMeasurementEventCallback<double>((instrument, value, tags, _) => Add(instrument, value, tags));
            _meters.Start();

            _activities = new ActivityListener
            {
                ShouldListenTo = source => source.Name == MeterName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = Stopped.Add
            };
            ActivitySource.AddActivityListener(_activities);
        }

        public ConcurrentBag<Measurement> Measurements { get; } = [];

        public ConcurrentBag<Activity> Stopped { get; } = [];

        public IReadOnlyList<Measurement> Of(string instrument) =>
            [.. Measurements.Where(m => m.Instrument == instrument)];

        public Activity Span(string name) => Stopped.Single(a => a.OperationName == name);

        public void Dispose()
        {
            _meters.Dispose();
            _activities.Dispose();
        }

        private void Add(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags) =>
            Measurements.Add(new Measurement(instrument.Name, value, tags.ToArray().ToDictionary(t => t.Key, t => t.Value)));
    }

    private sealed class Recorder(
        Func<ABACDecisionRecord, ValueTask<Either<EncinaError, Unit>>>? behavior = null) : IABACDecisionRecorder
    {
        public List<ABACDecisionRecord> Records { get; } = [];

        public ValueTask<Either<EncinaError, Unit>> RecordAsync(ABACDecisionRecord record, CancellationToken cancellationToken = default)
        {
            Records.Add(record);
            return behavior?.Invoke(record) ?? ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
        }
    }

    private static ABACPipelineBehavior<PolicyARequest, string> Behavior(
        IABACDecisionRecorder recorder,
        bool auditEnabled = true,
        ABACDecisionAuditFailureMode failureMode = ABACDecisionAuditFailureMode.FailClosed,
        FakeLogger<ABACPipelineBehavior<PolicyARequest, string>>? logger = null)
    {
        var pdp = Substitute.For<IPolicyDecisionPoint>();
        pdp.EvaluatePolicyAsync(Arg.Any<string>(), Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, PolicyDecision>(new PolicyDecision
            {
                Effect = Effect.Permit,
                Obligations = [],
                Advice = [],
                EvaluationDuration = TimeSpan.FromMilliseconds(1)
            })));

        var attributes = Substitute.For<IAttributeProvider>();
        attributes.GetSubjectAttributesAsync(Arg.Any<RequestIdentity>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object> { ["department"] = SentinelAttribute });
        attributes.GetResourceAttributesAsync(Arg.Any<object>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(new Dictionary<string, object>());
        attributes.GetEnvironmentAttributesAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object>());

        var options = new ABACOptions { EnforcementMode = ABACEnforcementMode.Block };
        options.DecisionAudit.Enabled = auditEnabled;
        options.DecisionAudit.FailureMode = failureMode;

        return new ABACPipelineBehavior<PolicyARequest, string>(
            pdp,
            attributes,
            new ObligationExecutor([], NullLogger<ObligationExecutor>.Instance),
            new EELCompiler(),
            Microsoft.Extensions.Options.Options.Create(options),
            recorder,
            new FakeTimeProvider(new DateTimeOffset(2026, 10, 10, 12, 0, 0, TimeSpan.Zero)),
            logger ?? new FakeLogger<ABACPipelineBehavior<PolicyARequest, string>>());
    }

    private static async Task<Either<EncinaError, string>> SendAsync(
        ABACPipelineBehavior<PolicyARequest, string> behavior, string resource = "doc-1")
    {
        var context = TestRequestContext.For(TestIdentity.User(SentinelSubject), tenantId: SentinelTenant, correlationId: "corr-1");
        return await behavior.Handle(
            new PolicyARequest(resource),
            context,
            () => ValueTask.FromResult(Right<EncinaError, string>("handled")),
            CancellationToken.None);
    }

    [Fact]
    public async Task SuccessfulWrite_CountsRecordedWithOutcomeAndEnforcementMode()
    {
        using var telemetry = new Telemetry();
        var behavior = Behavior(new Recorder());

        var result = await SendAsync(behavior);

        result.IsRight.ShouldBeTrue();
        var recorded = telemetry.Of("abac.decision_audit.recorded").ShouldHaveSingleItem();
        recorded.Value.ShouldBe(1);
        recorded.Tags["abac.outcome"].ShouldBe("Granted");
        recorded.Tags["abac.enforcement_mode"].ShouldBe("Block");
        telemetry.Of("abac.decision_audit.failed").ShouldBeEmpty();
    }

    [Fact]
    public async Task SuccessfulWrite_RecordsTheDurationHistogramOnce()
    {
        using var telemetry = new Telemetry();
        var behavior = Behavior(new Recorder());

        await SendAsync(behavior);

        var duration = telemetry.Of("abac.decision_audit.duration").ShouldHaveSingleItem();
        duration.Value.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task SuccessfulWrite_StartsTheRecordSpanAsAChildOfEvaluateAndTagsTheDecisionId()
    {
        using var telemetry = new Telemetry();
        var recorder = new Recorder();
        var behavior = Behavior(recorder);

        await SendAsync(behavior);

        var evaluate = telemetry.Span("ABAC.Evaluate");
        var span = telemetry.Span("ABAC.DecisionAudit.Record");
        span.ParentSpanId.ShouldBe(evaluate.SpanId);
        span.Status.ShouldBe(ActivityStatusCode.Ok);
        evaluate.GetTagItem("abac.decision_id").ShouldBe(recorder.Records.Single().DecisionId.ToString("D"));
    }

    [Theory]
    [InlineData(ABACDecisionAuditFailureMode.FailClosed)]
    [InlineData(ABACDecisionAuditFailureMode.BestEffort)]
    public async Task FailedWrite_CountsFailedWithFailureModeAndErrorCode(ABACDecisionAuditFailureMode failureMode)
    {
        using var telemetry = new Telemetry();
        var recorder = new Recorder(_ => ValueTask.FromResult(
            Left<EncinaError, Unit>(EncinaErrors.Create("store.down", SentinelMessage))));
        var behavior = Behavior(recorder, failureMode: failureMode);

        var result = await SendAsync(behavior);

        result.IsRight.ShouldBe(failureMode == ABACDecisionAuditFailureMode.BestEffort);
        var failed = telemetry.Of("abac.decision_audit.failed").ShouldHaveSingleItem();
        failed.Tags["abac.failure_mode"].ShouldBe(failureMode.ToString());
        failed.Tags["error.type"].ShouldBe("store.down");
        telemetry.Of("abac.decision_audit.recorded").ShouldBeEmpty();
        telemetry.Of("abac.decision_audit.duration").ShouldHaveSingleItem();

        var span = telemetry.Span("ABAC.DecisionAudit.Record");
        span.Status.ShouldBe(ActivityStatusCode.Error);
        span.GetTagItem("error.type").ShouldBe("store.down");
    }

    [Fact]
    public async Task RecorderThatThrows_CountsFailedWithTheExceptionTypeName()
    {
        using var telemetry = new Telemetry();
        var recorder = new Recorder(_ => throw new InvalidOperationException(SentinelMessage));
        var behavior = Behavior(recorder);

        await SendAsync(behavior);

        var failed = telemetry.Of("abac.decision_audit.failed").ShouldHaveSingleItem();
        failed.Tags["error.type"].ShouldBe(nameof(InvalidOperationException));
    }

    [Fact]
    public async Task DisabledAudit_EmitsNoAuditMetricNoRecordSpanAndNoDecisionId()
    {
        using var telemetry = new Telemetry();
        var behavior = Behavior(new Recorder(), auditEnabled: false);

        await SendAsync(behavior);

        telemetry.Measurements.ShouldNotContain(m => m.Instrument.StartsWith("abac.decision_audit", StringComparison.Ordinal));
        telemetry.Stopped.ShouldNotContain(a => a.OperationName == "ABAC.DecisionAudit.Record");
        telemetry.Span("ABAC.Evaluate").GetTagItem("abac.decision_id").ShouldBeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NoTagAndNoLogCarriesASubjectResourceTenantAttributeOrErrorMessage(bool recorderThrows)
    {
        using var telemetry = new Telemetry();
        var logger = new FakeLogger<ABACPipelineBehavior<PolicyARequest, string>>();
        var recorder = recorderThrows
            ? new Recorder(_ => throw new InvalidOperationException(SentinelMessage))
            : new Recorder(_ => ValueTask.FromResult(
                Left<EncinaError, Unit>(EncinaErrors.Create("store.down", SentinelMessage))));
        var behavior = Behavior(recorder, logger: logger);

        await SendAsync(behavior, SentinelResource);

        string[] sentinels = [SentinelSubject, SentinelTenant, SentinelResource, SentinelAttribute, SentinelMessage];

        telemetry.Measurements.Count.ShouldBeGreaterThan(0);
        foreach (var measurement in telemetry.Measurements)
        {
            measurement.Tags.Values.Any(value => ContainsSentinel(value?.ToString(), sentinels)).ShouldBeFalse();
        }

        telemetry.Stopped.Count.ShouldBe(2);
        foreach (var activity in telemetry.Stopped)
        {
            activity.TagObjects.Any(tag => ContainsSentinel(tag.Value?.ToString(), sentinels)).ShouldBeFalse();
            ContainsSentinel(activity.StatusDescription, sentinels).ShouldBeFalse();
        }

        var logs = logger.Collector.GetSnapshot();
        logs.ShouldNotBeEmpty();
        logs.ShouldAllBe(r => !ContainsSentinel(r.Message, sentinels));
        logs.Where(r => r.Exception is not null)
            .ShouldAllBe(r => !ContainsSentinel(r.Exception!.ToString(), sentinels));
        logs.ShouldAllBe(r => r.StructuredState == null
            || r.StructuredState.All(pair => !ContainsSentinel(pair.Value, sentinels)));
    }

    private static bool ContainsSentinel(string? text, string[] sentinels) =>
        text is not null && sentinels.Any(sentinel => text.Contains(sentinel, StringComparison.Ordinal));
}

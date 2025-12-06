using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using SimpleMediator;
using SimpleMediator.Tests.Fixtures;

namespace SimpleMediator.Tests;

public sealed class PipelineBehaviorsTests
{
    [Fact]
    public async Task CommandActivityBehavior_RecordsSuccessTelemetry()
    {
        var detector = new FunctionalFailureDetectorStub();
        var behavior = new CommandActivityPipelineBehavior<PingCommand, string>(detector);
        var request = new PingCommand("ping");

        using var listener = ActivityTestListener.Start(out var activities);
        var response = await behavior.Handle(request, CancellationToken.None, () => Task.FromResult("pong"));

        response.ShouldBe("pong");
        var activity = activities.ShouldHaveSingleItem();
        activity.DisplayName.ShouldBe("Mediator.Command.PingCommand");
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
        activity.GetTagItem("mediator.request_kind").ShouldBe("command");
        activity.GetTagItem("mediator.request_type").ShouldBe(typeof(PingCommand).FullName);
        activity.GetTagItem("mediator.response_type").ShouldBe(typeof(string).FullName);
    }

    [Fact]
    public async Task CommandActivityBehavior_AnnotatesFunctionalFailure()
    {
        var detector = new FunctionalFailureDetectorStub();
        detector.SetFailure("rule-broken", "ERR42", "Order already processed");
        var behavior = new CommandActivityPipelineBehavior<PingCommand, string>(detector);
        var request = new PingCommand("ping");

        using var listener = ActivityTestListener.Start(out var activities);
        var response = await behavior.Handle(request, CancellationToken.None, () => Task.FromResult("fail"));

        response.ShouldBe("fail");
        var activity = activities.ShouldHaveSingleItem();
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe("rule-broken");
        activity.GetTagItem("mediator.functional_failure").ShouldBe(true);
        activity.GetTagItem("mediator.failure_reason").ShouldBe("rule-broken");
        activity.GetTagItem("mediator.failure_code").ShouldBe("ERR42");
        activity.GetTagItem("mediator.failure_message").ShouldBe("Order already processed");
    }

    [Fact]
    public async Task CommandActivityBehavior_PropagatesCancellation()
    {
        var detector = new FunctionalFailureDetectorStub();
        var behavior = new CommandActivityPipelineBehavior<PingCommand, string>(detector);
        var request = new PingCommand("ping");
        using var listener = ActivityTestListener.Start(out var activities);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var action = () => behavior.Handle(request, cts.Token, () => throw new OperationCanceledException(cts.Token));

        await Should.ThrowAsync<OperationCanceledException>(action);
        var activity = activities.ShouldHaveSingleItem();
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe("cancelled");
        activity.GetTagItem("mediator.cancelled").ShouldBe(true);
    }

    [Fact]
    public async Task CommandActivityBehavior_RecordsExceptions()
    {
        var detector = new FunctionalFailureDetectorStub();
        var behavior = new CommandActivityPipelineBehavior<PingCommand, string>(detector);
        var request = new PingCommand("ping");
        using var listener = ActivityTestListener.Start(out var activities);

        var action = () => behavior.Handle(request, CancellationToken.None, () => throw new InvalidOperationException("boom"));

        var exception = await Should.ThrowAsync<InvalidOperationException>(action);
        exception.Message.ShouldBe("boom");
        var activity = activities.ShouldHaveSingleItem();
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.GetTagItem("exception.type").ShouldBe(typeof(InvalidOperationException).FullName);
        activity.GetTagItem("exception.message").ShouldBe("boom");
    }

    [Fact]
    public async Task QueryActivityBehavior_RecordsSuccessTelemetry()
    {
        var detector = new FunctionalFailureDetectorStub();
        var behavior = new QueryActivityPipelineBehavior<PongQuery, string>(detector);
        var request = new PongQuery(17);

        using var listener = ActivityTestListener.Start(out var activities);
        var response = await behavior.Handle(request, CancellationToken.None, () => Task.FromResult("pong"));

        response.ShouldBe("pong");
        var activity = activities.ShouldHaveSingleItem();
        activity.DisplayName.ShouldBe("Mediator.Query.PongQuery");
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
        activity.GetTagItem("mediator.request_kind").ShouldBe("query");
    }

    [Fact]
    public async Task QueryActivityBehavior_RecordsExceptions()
    {
        var detector = new FunctionalFailureDetectorStub();
        var behavior = new QueryActivityPipelineBehavior<PongQuery, string>(detector);
        var request = new PongQuery(21);
        using var listener = ActivityTestListener.Start(out var activities);

        var action = () => behavior.Handle(request, CancellationToken.None, () => throw new InvalidOperationException("fault"));

        await Should.ThrowAsync<InvalidOperationException>(action);
        var activity = activities.ShouldHaveSingleItem();
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.GetTagItem("exception.message").ShouldBe("fault");
    }

    [Fact]
    public async Task CommandMetricsBehavior_RecordsSuccess()
    {
        var metrics = new RecordingMetrics();
        var detector = new FunctionalFailureDetectorStub();
        var behavior = new CommandMetricsPipelineBehavior<PingCommand, string>(metrics, detector);
        var request = new PingCommand("ok");

        var response = await behavior.Handle(request, CancellationToken.None, () => Task.FromResult("ok"));

        response.ShouldBe("ok");
        metrics.Successes.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            item => item.requestKind.ShouldBe("command"),
            item => item.requestName.ShouldBe("PingCommand"),
            item => item.duration.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero));
        metrics.Failures.ShouldBeEmpty();
    }

    [Fact]
    public async Task CommandMetricsBehavior_RecordsFunctionalFailure()
    {
        var metrics = new RecordingMetrics();
        var detector = new FunctionalFailureDetectorStub();
        detector.SetFailure("validation-error");
        var behavior = new CommandMetricsPipelineBehavior<PingCommand, string>(metrics, detector);
        var request = new PingCommand("bad");

        var response = await behavior.Handle(request, CancellationToken.None, () => Task.FromResult("bad"));

        response.ShouldBe("bad");
        metrics.Failures.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.requestKind.ShouldBe("command"),
            failure => failure.requestName.ShouldBe("PingCommand"),
            failure => failure.reason.ShouldBe("validation-error"));
        metrics.Successes.ShouldBeEmpty();
    }

    [Fact]
    public async Task CommandMetricsBehavior_RecordsCancellation()
    {
        var metrics = new RecordingMetrics();
        var detector = new FunctionalFailureDetectorStub();
        var behavior = new CommandMetricsPipelineBehavior<PingCommand, string>(metrics, detector);
        var request = new PingCommand("cancel");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var action = () => behavior.Handle(request, cts.Token, () => throw new OperationCanceledException(cts.Token));

        await Should.ThrowAsync<OperationCanceledException>(action);
        metrics.Failures.ShouldHaveSingleItem().reason.ShouldBe("cancelled");
        metrics.Successes.ShouldBeEmpty();
    }

    [Fact]
    public async Task CommandMetricsBehavior_RecordsExceptions()
    {
        var metrics = new RecordingMetrics();
        var detector = new FunctionalFailureDetectorStub();
        var behavior = new CommandMetricsPipelineBehavior<PingCommand, string>(metrics, detector);
        var request = new PingCommand("boom");

        var action = () => behavior.Handle(request, CancellationToken.None, () => throw new InvalidOperationException("boom"));

        await Should.ThrowAsync<InvalidOperationException>(action);
        metrics.Failures.ShouldHaveSingleItem().reason.ShouldBe(nameof(InvalidOperationException));
    }

    [Fact]
    public async Task QueryMetricsBehavior_RecordsSuccess()
    {
        var metrics = new RecordingMetrics();
        var detector = new FunctionalFailureDetectorStub();
        var behavior = new QueryMetricsPipelineBehavior<PongQuery, string>(metrics, detector);
        var request = new PongQuery(1);

        var response = await behavior.Handle(request, CancellationToken.None, () => Task.FromResult("ok"));

        response.ShouldBe("ok");
        metrics.Successes.ShouldHaveSingleItem().requestKind.ShouldBe("query");
    }

    [Fact]
    public async Task QueryMetricsBehavior_RecordsExceptions()
    {
        var metrics = new RecordingMetrics();
        var detector = new FunctionalFailureDetectorStub();
        var behavior = new QueryMetricsPipelineBehavior<PongQuery, string>(metrics, detector);
        var request = new PongQuery(2);

        var action = () => behavior.Handle(request, CancellationToken.None, () => throw new InvalidOperationException("fail"));

        await Should.ThrowAsync<InvalidOperationException>(action);
        metrics.Failures.ShouldHaveSingleItem().reason.ShouldBe(nameof(InvalidOperationException));
    }

    private sealed class FunctionalFailureDetectorStub : IFunctionalFailureDetector
    {
        private bool _shouldFail;
        private string _reason = string.Empty;
        private string? _code;
        private string? _message;

        public void SetFailure(string reason, string? code = null, string? message = null)
        {
            _shouldFail = true;
            _reason = reason;
            _code = code;
            _message = message;
        }

        public bool TryExtractFailure(object? response, out string reason, out object? error)
        {
            if (_shouldFail)
            {
                reason = _reason;
                error = response;
                return true;
            }

            reason = string.Empty;
            error = null;
            return false;
        }

        public string? TryGetErrorCode(object? error) => _code;

        public string? TryGetErrorMessage(object? error) => _message;
    }

    private sealed class RecordingMetrics : IMediatorMetrics
    {
        public List<(string requestKind, string requestName, TimeSpan duration)> Successes { get; } = new();
        public List<(string requestKind, string requestName, TimeSpan duration, string reason)> Failures { get; } = new();

        public void TrackSuccess(string requestKind, string requestName, TimeSpan duration)
            => Successes.Add((requestKind, requestName, duration));

        public void TrackFailure(string requestKind, string requestName, TimeSpan duration, string reason)
            => Failures.Add((requestKind, requestName, duration, reason));
    }

    private sealed class ActivityTestListener : IDisposable
    {
        private readonly ActivityListener _listener;
        private readonly List<Activity> _activities;

        private ActivityTestListener(out List<Activity> activities)
        {
            _activities = new List<Activity>();
            activities = _activities;
            _listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == "SimpleMediator",
                Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
                SampleUsingParentId = (ref ActivityCreationOptions<string> options) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity => _activities.Add(activity)
            };
            ActivitySource.AddActivityListener(_listener);
        }

        public static ActivityTestListener Start(out List<Activity> activities)
            => new ActivityTestListener(out activities);

        public void Dispose() => _listener.Dispose();
    }
}

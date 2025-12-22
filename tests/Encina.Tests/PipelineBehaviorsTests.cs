using System.Diagnostics;
using Encina.Tests.Fixtures;
using LanguageExt;
using Shouldly;
using Xunit;
using static LanguageExt.Prelude;

namespace Encina.Tests;

[Collection("PipelineBehaviors")]
public sealed class PipelineBehaviorsTests
{
    [Fact]
    public async Task CommandActivityBehavior_RecordsSuccessTelemetry()
    {
        var detector = new FunctionalFailureDetectorStub();
        var behavior = new CommandActivityPipelineBehavior<PingCommand, string>(detector);
        var request = new PingCommand("ping");

        using var listener = ActivityTestListener.Start(out var activities);
        var outcome = await behavior.Handle(request, RequestContext.Create(), () => Success("pong"), CancellationToken.None);

        var response = ExpectSuccess(outcome);
        response.ShouldBe("pong");
        var activity = activities.ShouldHaveSingleItem();
        activity.DisplayName.ShouldBe("Mediator.Command.PingCommand");
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
        activity.GetTagItem("mediator.request_kind").ShouldBe("command");
        activity.GetTagItem("mediator.request_type").ShouldBe(typeof(PingCommand).FullName);
        activity.GetTagItem("mediator.request_name").ShouldBe(nameof(PingCommand));
        activity.GetTagItem("mediator.response_type").ShouldBe(typeof(string).FullName);
    }

    [Fact]
    public async Task CommandActivityBehavior_ThrowsWhenRequestIsNull()
    {
        var detector = new FunctionalFailureDetectorStub();
        var behavior = new CommandActivityPipelineBehavior<PingCommand, string>(detector);

        var result = await behavior.Handle(request: null!, RequestContext.Create(), () => Success("ok"), CancellationToken.None);

        var error = ExpectFailure(result, "mediator.behavior.null_request");
        error.Message.ShouldContain("received a null request");
    }

    [Fact]
    public async Task CommandActivityBehavior_ThrowsWhenNextIsNull()
    {
        var detector = new FunctionalFailureDetectorStub();
        var behavior = new CommandActivityPipelineBehavior<PingCommand, string>(detector);
        var request = new PingCommand("ping");

        var result = await behavior.Handle(request, RequestContext.Create(), nextStep: null!, CancellationToken.None);

        var error = ExpectFailure(result, "mediator.behavior.null_next");
        error.Message.ShouldContain("received a null callback");
    }

    [Fact]
    public async Task CommandActivityBehavior_AnnotatesFunctionalFailure()
    {
        var detector = new FunctionalFailureDetectorStub();
        detector.SetFailure("rule-broken", "ERR42", "Order already processed");
        var behavior = new CommandActivityPipelineBehavior<PingCommand, string>(detector);
        var request = new PingCommand("ping");

        using var listener = ActivityTestListener.Start(out var activities);
        var outcome = await behavior.Handle(request, RequestContext.Create(), () => Success("fail"), CancellationToken.None);

        var response = ExpectSuccess(outcome);
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
    public async Task CommandActivityBehavior_OmitsFailureReasonTagWhenEmpty()
    {
        var detector = new FunctionalFailureDetectorStub();
        detector.SetFailure("  ");
        var behavior = new CommandActivityPipelineBehavior<PingCommand, string>(detector);
        var request = new PingCommand("ping");

        using var listener = ActivityTestListener.Start(out var activities);
        var outcome = await behavior.Handle(request, RequestContext.Create(), () => Success("fail"), CancellationToken.None);

        var response = ExpectSuccess(outcome);
        response.ShouldBe("fail");
        var activity = activities.ShouldHaveSingleItem();
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe("  ");
        activity.GetTagItem("mediator.functional_failure").ShouldBe(true);
        activity.TagObjects.Any(tag => tag.Key == "mediator.failure_reason").ShouldBeFalse();
    }

    [Fact]
    public async Task CommandActivityBehavior_OmitsCodeAndMessageTagsWhenEmpty()
    {
        var detector = new FunctionalFailureDetectorStub();
        detector.SetFailure("validation-failed", code: "  ", message: "");
        var behavior = new CommandActivityPipelineBehavior<PingCommand, string>(detector);
        var request = new PingCommand("test");

        using var listener = ActivityTestListener.Start(out var activities);
        var outcome = await behavior.Handle(request, RequestContext.Create(), () => Success("result"), CancellationToken.None);

        var response = ExpectSuccess(outcome);
        response.ShouldBe("result");
        var activity = activities.ShouldHaveSingleItem();
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.GetTagItem("mediator.functional_failure").ShouldBe(true);
        activity.GetTagItem("mediator.failure_reason").ShouldBe("validation-failed");
        // Code and message tags should not be present when empty/whitespace
        activity.TagObjects.Any(tag => tag.Key == "mediator.failure_code").ShouldBeFalse();
        activity.TagObjects.Any(tag => tag.Key == "mediator.failure_message").ShouldBeFalse();
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

        var result = await behavior.Handle(request, RequestContext.Create(), () => ValueTask.FromCanceled<Either<MediatorError, string>>(cts.Token), cts.Token);
        var error = ExpectFailure(result, "mediator.behavior.cancelled");
        ExtractException(error).ShouldBeAssignableTo<OperationCanceledException>();
        error.Message.ShouldContain(behavior.GetType().Name);
        error.Message.ShouldContain(typeof(PingCommand).Name);
        var activity = activities.ShouldHaveSingleItem();
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe("cancelled");
        activity.GetTagItem("mediator.cancelled").ShouldBe(true);
    }

    [Fact]
    public async Task CommandActivityBehavior_DoesNotCaptureSynchronizationContext()
    {
        var detector = new FunctionalFailureDetectorStub();
        var behavior = new CommandActivityPipelineBehavior<PingCommand, string>(detector);
        var request = new PingCommand("sync");
        using var context = new RecordingSynchronizationContext();
        var originalContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context);

        try
        {
            using var listener = ActivityTestListener.Start(out _);
#pragma warning disable xUnit1030
            var outcome = await behavior
                .Handle(request, RequestContext.Create(), () => new ValueTask<Either<MediatorError, string>>(Task.Run(() => Right<MediatorError, string>("done"))), CancellationToken.None)
                .ConfigureAwait(false);
#pragma warning restore xUnit1030

            var response = ExpectSuccess(outcome);
            response.ShouldBe("done");
            context.PostCallCount.ShouldBe(0);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }
    }

    [Fact]
    public async Task CommandActivityBehavior_RecordsExceptions()
    {
        var detector = new FunctionalFailureDetectorStub();
        var behavior = new CommandActivityPipelineBehavior<PingCommand, string>(detector);
        var request = new PingCommand("ping");
        using var listener = ActivityTestListener.Start(out var activities);

        var result = await behavior.Handle(request, RequestContext.Create(), () => ValueTask.FromException<Either<MediatorError, string>>(new InvalidOperationException("boom")), CancellationToken.None);

        var error = ExpectFailure(result, "mediator.behavior.exception");
        var exception = ExtractException(error);
        exception.ShouldBeOfType<InvalidOperationException>();
        exception!.Message.ShouldBe("boom");
        var activity = activities.ShouldHaveSingleItem();
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.GetTagItem("exception.type").ShouldBe(typeof(InvalidOperationException).FullName);
        activity.GetTagItem("exception.message").ShouldBe("boom");
        error.Message.ShouldContain(behavior.GetType().Name);
    }

    [Fact]
    public async Task QueryActivityBehavior_RecordsSuccessTelemetry()
    {
        var detector = new FunctionalFailureDetectorStub();
        var behavior = new QueryActivityPipelineBehavior<PongQuery, string>(detector);
        var request = new PongQuery(17);

        using var listener = ActivityTestListener.Start(out var activities);
        var outcome = await behavior.Handle(request, RequestContext.Create(), () => Success("pong"), CancellationToken.None);

        var response = ExpectSuccess(outcome);
        response.ShouldBe("pong");
        var activity = activities.ShouldHaveSingleItem();
        activity.DisplayName.ShouldBe("Mediator.Query.PongQuery");
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
        activity.GetTagItem("mediator.request_kind").ShouldBe("query");
        activity.GetTagItem("mediator.request_type").ShouldBe(typeof(PongQuery).FullName);
        activity.GetTagItem("mediator.request_name").ShouldBe(nameof(PongQuery));
        activity.GetTagItem("mediator.response_type").ShouldBe(typeof(string).FullName);
    }

    [Fact]
    public async Task QueryActivityBehavior_AnnotatesFunctionalFailure()
    {
        var detector = new FunctionalFailureDetectorStub();
        detector.SetFailure("cache-miss", "CACHE_MISS", "Query not cached");
        var behavior = new QueryActivityPipelineBehavior<PongQuery, string>(detector);
        var request = new PongQuery(17);

        using var listener = ActivityTestListener.Start(out var activities);
        var outcome = await behavior.Handle(request, RequestContext.Create(), () => Success("fallback"), CancellationToken.None);

        var response = ExpectSuccess(outcome);
        response.ShouldBe("fallback");
        var activity = activities.ShouldHaveSingleItem();
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe("cache-miss");
        activity.GetTagItem("mediator.functional_failure").ShouldBe(true);
        activity.GetTagItem("mediator.failure_reason").ShouldBe("cache-miss");
        activity.GetTagItem("mediator.failure_code").ShouldBe("CACHE_MISS");
        activity.GetTagItem("mediator.failure_message").ShouldBe("Query not cached");
    }

    [Fact]
    public async Task QueryActivityBehavior_OmitsCodeAndMessageTagsWhenEmpty()
    {
        var detector = new FunctionalFailureDetectorStub();
        detector.SetFailure("not-found", code: null, message: "   ");
        var behavior = new QueryActivityPipelineBehavior<PongQuery, string>(detector);
        var request = new PongQuery(99);

        using var listener = ActivityTestListener.Start(out var activities);
        var outcome = await behavior.Handle(request, RequestContext.Create(), () => Success("fallback"), CancellationToken.None);

        var response = ExpectSuccess(outcome);
        response.ShouldBe("fallback");
        var activity = activities.ShouldHaveSingleItem();
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.GetTagItem("mediator.functional_failure").ShouldBe(true);
        activity.GetTagItem("mediator.failure_reason").ShouldBe("not-found");
        // Code and message tags should not be present when null/whitespace
        activity.TagObjects.Any(tag => tag.Key == "mediator.failure_code").ShouldBeFalse();
        activity.TagObjects.Any(tag => tag.Key == "mediator.failure_message").ShouldBeFalse();
    }

    [Fact]
    public async Task QueryActivityBehavior_PropagatesCancellation()
    {
        var detector = new FunctionalFailureDetectorStub();
        var behavior = new QueryActivityPipelineBehavior<PongQuery, string>(detector);
        var request = new PongQuery(5);
        using var listener = ActivityTestListener.Start(out var activities);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await behavior.Handle(request, RequestContext.Create(), () => ValueTask.FromCanceled<Either<MediatorError, string>>(cts.Token), cts.Token);
        var error = ExpectFailure(result, "mediator.behavior.cancelled");
        ExtractException(error).ShouldBeAssignableTo<OperationCanceledException>();
        error.Message.ShouldContain(behavior.GetType().Name);
        error.Message.ShouldContain(typeof(PongQuery).Name);
        var activity = activities.ShouldHaveSingleItem();
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe("cancelled");
        activity.GetTagItem("mediator.cancelled").ShouldBe(true);
    }

    [Fact]
    public async Task QueryActivityBehavior_ThrowsWhenRequestIsNull()
    {
        var detector = new FunctionalFailureDetectorStub();
        var behavior = new QueryActivityPipelineBehavior<PongQuery, string>(detector);

        var result = await behavior.Handle(request: null!, RequestContext.Create(), () => Success("ok"), CancellationToken.None);

        var error = ExpectFailure(result, "mediator.behavior.null_request");
        error.Message.ShouldContain("received a null request");
    }

    [Fact]
    public async Task QueryActivityBehavior_ThrowsWhenNextIsNull()
    {
        var detector = new FunctionalFailureDetectorStub();
        var behavior = new QueryActivityPipelineBehavior<PongQuery, string>(detector);
        var request = new PongQuery(42);

        var result = await behavior.Handle(request, RequestContext.Create(), nextStep: null!, CancellationToken.None);

        var error = ExpectFailure(result, "mediator.behavior.null_next");
        error.Message.ShouldContain("received a null callback");
    }

    [Fact]
    public async Task QueryActivityBehavior_RecordsExceptions()
    {
        var detector = new FunctionalFailureDetectorStub();
        var behavior = new QueryActivityPipelineBehavior<PongQuery, string>(detector);
        var request = new PongQuery(21);
        using var listener = ActivityTestListener.Start(out var activities);

        var result = await behavior.Handle(request, RequestContext.Create(), () => ValueTask.FromException<Either<MediatorError, string>>(new InvalidOperationException("fault")), CancellationToken.None);

        var error = ExpectFailure(result, "mediator.behavior.exception");
        ExtractException(error).ShouldBeOfType<InvalidOperationException>();
        error.Message.ShouldContain(behavior.GetType().Name);
        error.Message.ShouldContain(typeof(PongQuery).Name);
        var activity = activities.ShouldHaveSingleItem();
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.GetTagItem("exception.type").ShouldBe(typeof(InvalidOperationException).FullName);
        activity.GetTagItem("exception.message").ShouldBe("fault");
    }

    [Fact]
    public async Task QueryActivityBehavior_DoesNotCaptureSynchronizationContext()
    {
        var detector = new FunctionalFailureDetectorStub();
        var behavior = new QueryActivityPipelineBehavior<PongQuery, string>(detector);
        var request = new PongQuery(8);
        using var context = new RecordingSynchronizationContext();
        var originalContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context);

        try
        {
            using var listener = ActivityTestListener.Start(out _);
#pragma warning disable xUnit1030
            var outcome = await behavior
                .Handle(request, RequestContext.Create(), () => new ValueTask<Either<MediatorError, string>>(Task.Run(() => Right<MediatorError, string>("ok"))), CancellationToken.None)
                .ConfigureAwait(false);
#pragma warning restore xUnit1030

            var response = ExpectSuccess(outcome);
            response.ShouldBe("ok");
            context.PostCallCount.ShouldBe(0);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }
    }

    [Fact]
    public async Task CommandMetricsBehavior_RecordsSuccess()
    {
        var metrics = new RecordingMetrics();
        var detector = new FunctionalFailureDetectorStub();
        var behavior = new CommandMetricsPipelineBehavior<PingCommand, string>(metrics, detector);
        var request = new PingCommand("ok");

        var outcome = await behavior.Handle(request, RequestContext.Create(), () => Success("ok"), CancellationToken.None);

        var response = ExpectSuccess(outcome);
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

        var outcome = await behavior.Handle(request, RequestContext.Create(), () => Success("bad"), CancellationToken.None);

        var response = ExpectSuccess(outcome);
        response.ShouldBe("bad");
        detector.TryExtractFailureCallCount.ShouldBe(1);
        detector.LastResponse.ShouldBe(response);
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

        var result = await behavior.Handle(request, RequestContext.Create(), () => ValueTask.FromCanceled<Either<MediatorError, string>>(cts.Token), cts.Token);
        var error = ExpectFailure(result, "mediator.behavior.cancelled");
        ExtractException(error).ShouldBeAssignableTo<OperationCanceledException>();
        error.Message.ShouldContain(behavior.GetType().Name);
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

        var result = await behavior.Handle(request, RequestContext.Create(), () => ValueTask.FromException<Either<MediatorError, string>>(new InvalidOperationException("boom")), CancellationToken.None);

        var error = ExpectFailure(result, "mediator.behavior.exception");
        ExtractException(error).ShouldBeOfType<InvalidOperationException>();
        error.Message.ShouldContain(behavior.GetType().Name);
        metrics.Failures.ShouldHaveSingleItem().reason.ShouldBe(nameof(InvalidOperationException));
    }

    [Fact]
    public async Task CommandMetricsBehavior_ThrowsWhenRequestIsNull()
    {
        var metrics = new RecordingMetrics();
        var detector = new FunctionalFailureDetectorStub();
        var behavior = new CommandMetricsPipelineBehavior<PingCommand, string>(metrics, detector);

        var result = await behavior.Handle(request: null!, RequestContext.Create(), () => Success("ok"), CancellationToken.None);

        var error = ExpectFailure(result, "mediator.behavior.null_request");
        error.Message.ShouldContain("received a null request");
        metrics.Failures.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.requestKind.ShouldBe("command"),
            failure => failure.reason.ShouldBe("mediator.behavior.null_request"));
    }

    [Fact]
    public async Task CommandMetricsBehavior_ThrowsWhenNextIsNull()
    {
        var metrics = new RecordingMetrics();
        var detector = new FunctionalFailureDetectorStub();
        var behavior = new CommandMetricsPipelineBehavior<PingCommand, string>(metrics, detector);
        var request = new PingCommand("ok");

        var result = await behavior.Handle(request, RequestContext.Create(), nextStep: null!, CancellationToken.None);

        var error = ExpectFailure(result, "mediator.behavior.null_next");
        error.Message.ShouldContain("received a null callback");
        metrics.Failures.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.requestKind.ShouldBe("command"),
            failure => failure.reason.ShouldBe("mediator.behavior.null_next"));
    }

    [Fact]
    public async Task CommandMetricsBehavior_DoesNotCaptureSynchronizationContext()
    {
        var metrics = new RecordingMetrics();
        var detector = new FunctionalFailureDetectorStub();
        var behavior = new CommandMetricsPipelineBehavior<PingCommand, string>(metrics, detector);
        var request = new PingCommand("context");
        using var context = new RecordingSynchronizationContext();
        var originalContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context);

        try
        {
#pragma warning disable xUnit1030
            var outcome = await behavior
                .Handle(request, RequestContext.Create(), () => new ValueTask<Either<MediatorError, string>>(Task.Run(() => Right<MediatorError, string>("ok"))), CancellationToken.None)
                .ConfigureAwait(false);
#pragma warning restore xUnit1030

            var response = ExpectSuccess(outcome);
            response.ShouldBe("ok");
            context.PostCallCount.ShouldBe(0);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }
    }

    [Fact]
    public async Task QueryMetricsBehavior_RecordsSuccess()
    {
        var metrics = new RecordingMetrics();
        var detector = new FunctionalFailureDetectorStub();
        var behavior = new QueryMetricsPipelineBehavior<PongQuery, string>(metrics, detector);
        var request = new PongQuery(1);

        var outcome = await behavior.Handle(request, RequestContext.Create(), () => Success("ok"), CancellationToken.None);

        var response = ExpectSuccess(outcome);
        response.ShouldBe("ok");
        metrics.Successes.ShouldHaveSingleItem().requestKind.ShouldBe("query");
        metrics.Failures.ShouldBeEmpty();
    }

    [Fact]
    public async Task QueryMetricsBehavior_DoesNotCaptureSynchronizationContext()
    {
        var metrics = new RecordingMetrics();
        var detector = new FunctionalFailureDetectorStub();
        var behavior = new QueryMetricsPipelineBehavior<PongQuery, string>(metrics, detector);
        var request = new PongQuery(5);
        using var context = new RecordingSynchronizationContext();
        var originalContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context);

        try
        {
#pragma warning disable xUnit1030
            var outcome = await behavior
                .Handle(request, RequestContext.Create(), () => new ValueTask<Either<MediatorError, string>>(Task.Run(() => Right<MediatorError, string>("ok"))), CancellationToken.None)
                .ConfigureAwait(false);
#pragma warning restore xUnit1030

            var response = ExpectSuccess(outcome);
            response.ShouldBe("ok");
            context.PostCallCount.ShouldBe(0);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }
    }

    [Fact]
    public async Task QueryMetricsBehavior_ThrowsWhenRequestIsNull()
    {
        var metrics = new RecordingMetrics();
        var detector = new FunctionalFailureDetectorStub();
        var behavior = new QueryMetricsPipelineBehavior<PongQuery, string>(metrics, detector);

        var result = await behavior.Handle(request: null!, RequestContext.Create(), () => Success("ok"), CancellationToken.None);

        var error = ExpectFailure(result, "mediator.behavior.null_request");
        error.Message.ShouldContain("received a null request");
        metrics.Failures.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.requestKind.ShouldBe("query"),
            failure => failure.reason.ShouldBe("mediator.behavior.null_request"));
    }

    [Fact]
    public async Task QueryMetricsBehavior_ThrowsWhenNextIsNull()
    {
        var metrics = new RecordingMetrics();
        var detector = new FunctionalFailureDetectorStub();
        var behavior = new QueryMetricsPipelineBehavior<PongQuery, string>(metrics, detector);
        var request = new PongQuery(1);

        var result = await behavior.Handle(request, RequestContext.Create(), nextStep: null!, CancellationToken.None);

        var error = ExpectFailure(result, "mediator.behavior.null_next");
        error.Message.ShouldContain("received a null callback");
        metrics.Failures.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.requestKind.ShouldBe("query"),
            failure => failure.reason.ShouldBe("mediator.behavior.null_next"));
    }

    [Fact]
    public async Task QueryMetricsBehavior_RecordsFunctionalFailure()
    {
        var metrics = new RecordingMetrics();
        var detector = new FunctionalFailureDetectorStub();
        detector.SetFailure("validation");
        var behavior = new QueryMetricsPipelineBehavior<PongQuery, string>(metrics, detector);
        var request = new PongQuery(3);

        var outcome = await behavior.Handle(request, RequestContext.Create(), () => Success("fail"), CancellationToken.None);

        var response = ExpectSuccess(outcome);
        response.ShouldBe("fail");
        detector.TryExtractFailureCallCount.ShouldBe(1);
        detector.LastResponse.ShouldBe(response);
        metrics.Failures.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.requestKind.ShouldBe("query"),
            failure => failure.requestName.ShouldBe(nameof(PongQuery)),
            failure => failure.reason.ShouldBe("validation"));
        metrics.Successes.ShouldBeEmpty();
    }

    [Fact]
    public async Task QueryMetricsBehavior_RecordsCancellation()
    {
        var metrics = new RecordingMetrics();
        var detector = new FunctionalFailureDetectorStub();
        var behavior = new QueryMetricsPipelineBehavior<PongQuery, string>(metrics, detector);
        var request = new PongQuery(4);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await behavior.Handle(request, RequestContext.Create(), () => ValueTask.FromCanceled<Either<MediatorError, string>>(cts.Token), cts.Token);
        var error = ExpectFailure(result, "mediator.behavior.cancelled");
        ExtractException(error).ShouldBeAssignableTo<OperationCanceledException>();
        error.Message.ShouldContain(behavior.GetType().Name);
        metrics.Failures.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            failure => failure.requestKind.ShouldBe("query"),
            failure => failure.requestName.ShouldBe(nameof(PongQuery)),
            failure => failure.reason.ShouldBe("cancelled"));
        metrics.Successes.ShouldBeEmpty();
    }

    [Fact]
    public async Task QueryMetricsBehavior_RecordsExceptions()
    {
        var metrics = new RecordingMetrics();
        var detector = new FunctionalFailureDetectorStub();
        var behavior = new QueryMetricsPipelineBehavior<PongQuery, string>(metrics, detector);
        var request = new PongQuery(2);

        var result = await behavior.Handle(request, RequestContext.Create(), () => ValueTask.FromException<Either<MediatorError, string>>(new InvalidOperationException("fail")), CancellationToken.None);

        var error = ExpectFailure(result, "mediator.behavior.exception");
        ExtractException(error).ShouldBeOfType<InvalidOperationException>();
        error.Message.ShouldContain(behavior.GetType().Name);
        metrics.Failures.ShouldHaveSingleItem().reason.ShouldBe(nameof(InvalidOperationException));
    }

    private static ValueTask<Either<MediatorError, T>> Success<T>(T value)
        => ValueTask.FromResult(Right<MediatorError, T>(value));

    private static T ExpectSuccess<T>(Either<MediatorError, T> outcome)
    {
        outcome.IsRight.ShouldBeTrue("Expected the pipeline to succeed.");
        return outcome.Match(
            Left: static _ => throw new InvalidOperationException("Unexpected failure outcome."),
            Right: static value => value);
    }

    private static MediatorError ExpectFailure<T>(Either<MediatorError, T> outcome, string expectedCode)
    {
        outcome.IsLeft.ShouldBeTrue($"Expected a failure with code {expectedCode}.");
        var error = outcome.Match(
            Left: static err => err,
            Right: _ => throw new InvalidOperationException("Unexpected successful outcome."));
        error.GetMediatorCode().ShouldBe(expectedCode);
        return error;
    }

    private static Exception ExtractException(MediatorError error)
        => error.Exception.Match(
            Some: ex => ex,
            None: () => throw new InvalidOperationException("Expected the error to carry an exception."));

    private sealed class FunctionalFailureDetectorStub : IFunctionalFailureDetector
    {
        private bool _shouldFail;
        private string _reason = string.Empty;
        private string? _code;
        private string? _message;
        private int _callCount;

        public int TryExtractFailureCallCount => _callCount;
        public object? LastResponse { get; private set; }
        public object? LastErrorPayload { get; private set; }

        public void SetFailure(string reason, string? code = null, string? message = null)
        {
            _shouldFail = true;
            _reason = reason;
            _code = code;
            _message = message;
        }

        public bool TryExtractFailure(object? response, out string reason, out object? capturedFailure)
        {
            _callCount++;
            LastResponse = response;
            if (_shouldFail)
            {
                reason = _reason;
                capturedFailure = response;
                LastErrorPayload = capturedFailure;
                return true;
            }

            reason = string.Empty;
            capturedFailure = null;
            LastErrorPayload = null;
            return false;
        }

        public string? TryGetErrorCode(object? capturedFailure) => _code;

        public string? TryGetErrorMessage(object? capturedFailure) => _message;
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

    private sealed class RecordingSynchronizationContext : SynchronizationContext, IDisposable
    {
        private int _postCallCount;

        public int PostCallCount => Volatile.Read(ref _postCallCount);

        public override void Post(SendOrPostCallback d, object? state)
        {
            Interlocked.Increment(ref _postCallCount);
            ThreadPool.QueueUserWorkItem(static tuple =>
            {
                var (callback, callbackState) = ((SendOrPostCallback, object?))tuple!;
                callback(callbackState);
            }, (d, state));
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            Interlocked.Increment(ref _postCallCount);
            d(state);
        }

        public void Dispose()
        {
            // No resources to dispose.
        }
    }

    private sealed class ActivityTestListener : IDisposable
    {
        private readonly ActivityListener _listener;
        private readonly List<Activity> _activities;
        private readonly DateTime _startedAtUtc;

        private ActivityTestListener(out List<Activity> activities)
        {
            _activities = new List<Activity>();
            activities = _activities;
            _startedAtUtc = DateTime.UtcNow;
            _listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == "Encina",
                Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
                SampleUsingParentId = (ref ActivityCreationOptions<string> options) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity =>
                {
                    // Ignore activities that started before this listener subscribed so concurrent
                    // tests wrapping up earlier telemetry do not leak into these assertions.
                    if (activity.StartTimeUtc >= _startedAtUtc && activity.DisplayName.StartsWith("Mediator.", StringComparison.Ordinal))
                    {
                        _activities.Add(activity);
                    }
                }
            };
            ActivitySource.AddActivityListener(_listener);
        }

        public static ActivityTestListener Start(out List<Activity> activities)
            => new ActivityTestListener(out activities);

        public void Dispose() => _listener.Dispose();
    }
}

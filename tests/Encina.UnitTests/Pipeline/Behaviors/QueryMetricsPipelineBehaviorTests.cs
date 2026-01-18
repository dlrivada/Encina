using LanguageExt;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Pipeline.Behaviors;

public sealed class QueryMetricsPipelineBehaviorTests
{
    private sealed record TestQuery(string Value) : IQuery<string>;

    [Fact]
    public void Constructor_WithNullMetrics_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new QueryMetricsPipelineBehavior<TestQuery, string>(null!, NullFunctionalFailureDetector.Instance));
    }

    [Fact]
    public async Task Handle_WithNullRequest_TracksFailure()
    {
        var metrics = new SpyMetrics();
        var behavior = new QueryMetricsPipelineBehavior<TestQuery, string>(metrics, NullFunctionalFailureDetector.Instance);

        var result = await behavior.Handle(
            null!,
            RequestContext.Create(),
            () => ValueTask.FromResult(Right<EncinaError, string>("ok")),
            CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetEncinaCode().ShouldBe(EncinaErrorCodes.BehaviorNullRequest);

        metrics.Failures.Count.ShouldBe(1);
        var failure = metrics.Failures[0];
        failure.RequestKind.ShouldBe("query");
        failure.RequestName.ShouldBe("TestQuery");
        failure.Reason.ShouldBe(EncinaErrorCodes.BehaviorNullRequest);
    }

    [Fact]
    public async Task Handle_WithNullNextStep_TracksFailure()
    {
        var metrics = new SpyMetrics();
        var behavior = new QueryMetricsPipelineBehavior<TestQuery, string>(metrics, NullFunctionalFailureDetector.Instance);

        var result = await behavior.Handle(
            new TestQuery("test"),
            RequestContext.Create(),
            null!,
            CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetEncinaCode().ShouldBe(EncinaErrorCodes.BehaviorNullNext);

        metrics.Failures.Count.ShouldBe(1);
        metrics.Failures[0].Reason.ShouldBe(EncinaErrorCodes.BehaviorNullNext);
    }

    [Fact]
    public async Task Handle_WithSuccessfulHandler_TracksSuccess()
    {
        var metrics = new SpyMetrics();
        var behavior = new QueryMetricsPipelineBehavior<TestQuery, string>(metrics, NullFunctionalFailureDetector.Instance);

        var result = await behavior.Handle(
            new TestQuery("test"),
            RequestContext.Create(),
            () => ValueTask.FromResult(Right<EncinaError, string>("success")),
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        metrics.Successes.Count.ShouldBe(1);
        var success = metrics.Successes[0];
        success.RequestKind.ShouldBe("query");
        success.RequestName.ShouldBe("TestQuery");
    }

    [Fact]
    public async Task Handle_WithHandlerError_TracksFailure()
    {
        var metrics = new SpyMetrics();
        var behavior = new QueryMetricsPipelineBehavior<TestQuery, string>(metrics, NullFunctionalFailureDetector.Instance);
        var expectedError = EncinaErrors.Create("test.error", "Test error");

        var result = await behavior.Handle(
            new TestQuery("test"),
            RequestContext.Create(),
            () => ValueTask.FromResult(Left<EncinaError, string>(expectedError)),
            CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        metrics.Failures.Count.ShouldBe(1);
        metrics.Failures[0].Reason.ShouldBe("test.error");
    }

    [Fact]
    public async Task Handle_WithCancellation_TracksCancellationFailure()
    {
        var metrics = new SpyMetrics();
        var behavior = new QueryMetricsPipelineBehavior<TestQuery, string>(metrics, NullFunctionalFailureDetector.Instance);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await behavior.Handle(
            new TestQuery("test"),
            RequestContext.Create(),
            () => throw new OperationCanceledException(cts.Token),
            cts.Token);

        result.IsLeft.ShouldBeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetEncinaCode().ShouldBe(EncinaErrorCodes.BehaviorCancelled);

        metrics.Failures.Count.ShouldBe(1);
        metrics.Failures[0].Reason.ShouldBe("cancelled");
    }

    [Fact]
    public async Task Handle_WithException_TracksExceptionFailure()
    {
        var metrics = new SpyMetrics();
        var behavior = new QueryMetricsPipelineBehavior<TestQuery, string>(metrics, NullFunctionalFailureDetector.Instance);

        var result = await behavior.Handle(
            new TestQuery("test"),
            RequestContext.Create(),
            () => throw new InvalidOperationException("boom"),
            CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetEncinaCode().ShouldBe(EncinaErrorCodes.BehaviorException);

        metrics.Failures.Count.ShouldBe(1);
        metrics.Failures[0].Reason.ShouldBe("InvalidOperationException");
    }

    [Fact]
    public async Task Handle_WithFunctionalFailure_TracksFailure()
    {
        var metrics = new SpyMetrics();
        var detector = new TestFailureDetector(detectsFailure: true, failureReason: "not.found");
        var behavior = new QueryMetricsPipelineBehavior<TestQuery, string>(metrics, detector);

        await behavior.Handle(
            new TestQuery("test"),
            RequestContext.Create(),
            () => ValueTask.FromResult(Right<EncinaError, string>("failure-response")),
            CancellationToken.None);

        metrics.Failures.Count.ShouldBe(1);
        metrics.Failures[0].Reason.ShouldBe("not.found");
        metrics.Successes.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_WithNullFailureDetector_UsesNullDetector()
    {
        var metrics = new SpyMetrics();
        var behavior = new QueryMetricsPipelineBehavior<TestQuery, string>(metrics, null!);

        var result = await behavior.Handle(
            new TestQuery("test"),
            RequestContext.Create(),
            () => ValueTask.FromResult(Right<EncinaError, string>("success")),
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        metrics.Successes.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_TracksDuration()
    {
        var metrics = new SpyMetrics();
        var behavior = new QueryMetricsPipelineBehavior<TestQuery, string>(metrics, NullFunctionalFailureDetector.Instance);

        await behavior.Handle(
            new TestQuery("test"),
            RequestContext.Create(),
            async () =>
            {
                await Task.Delay(10, CancellationToken.None).ConfigureAwait(false);
                return Right<EncinaError, string>("success");
            },
            CancellationToken.None);

        metrics.Successes.Count.ShouldBe(1);
        metrics.Successes[0].Duration.ShouldBeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task Handle_WithNoFunctionalFailure_TracksSuccess()
    {
        var metrics = new SpyMetrics();
        var detector = new TestFailureDetector(detectsFailure: false, failureReason: null);
        var behavior = new QueryMetricsPipelineBehavior<TestQuery, string>(metrics, detector);

        await behavior.Handle(
            new TestQuery("test"),
            RequestContext.Create(),
            () => ValueTask.FromResult(Right<EncinaError, string>("success")),
            CancellationToken.None);

        metrics.Successes.Count.ShouldBe(1);
        metrics.Failures.Count.ShouldBe(0);
    }

    private sealed class SpyMetrics : IEncinaMetrics
    {
        public List<(string RequestKind, string RequestName, TimeSpan Duration)> Successes { get; } = [];
        public List<(string RequestKind, string RequestName, TimeSpan Duration, string Reason)> Failures { get; } = [];

        public void TrackSuccess(string requestKind, string requestName, TimeSpan duration)
            => Successes.Add((requestKind, requestName, duration));

        public void TrackFailure(string requestKind, string requestName, TimeSpan duration, string reason)
            => Failures.Add((requestKind, requestName, duration, reason));
    }

    private sealed class TestFailureDetector(bool detectsFailure, string? failureReason) : IFunctionalFailureDetector
    {
        public bool TryExtractFailure(object? response, out string reason, out object? capturedFailure)
        {
            reason = failureReason ?? string.Empty;
            capturedFailure = response;
            return detectsFailure;
        }

        public string? TryGetErrorCode(object? capturedFailure) => null;

        public string? TryGetErrorMessage(object? capturedFailure) => null;
    }
}

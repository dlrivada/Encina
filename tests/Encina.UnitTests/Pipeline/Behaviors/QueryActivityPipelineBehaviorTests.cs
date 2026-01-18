using System.Diagnostics;
using LanguageExt;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Pipeline.Behaviors;

public sealed class QueryActivityPipelineBehaviorTests : IDisposable
{
    private readonly ActivityListener _listener;
    private readonly List<Activity> _activities = [];

    public QueryActivityPipelineBehaviorTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => string.Equals(source.Name, "Encina", StringComparison.Ordinal),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => _activities.Add(activity)
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose()
    {
        _listener.Dispose();
    }

    private sealed record TestQuery(string Value) : IQuery<string>;

    [Fact]
    public async Task Handle_WithNullRequest_ReturnsNullRequestError()
    {
        var behavior = new QueryActivityPipelineBehavior<TestQuery, string>(NullFunctionalFailureDetector.Instance);

        var result = await behavior.Handle(
            null!,
            RequestContext.Create(),
            () => ValueTask.FromResult(Right<EncinaError, string>("ok")),
            CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetEncinaCode().ShouldBe(EncinaErrorCodes.BehaviorNullRequest);
    }

    [Fact]
    public async Task Handle_WithNullNextStep_ReturnsNullNextError()
    {
        var behavior = new QueryActivityPipelineBehavior<TestQuery, string>(NullFunctionalFailureDetector.Instance);

        var result = await behavior.Handle(
            new TestQuery("test"),
            RequestContext.Create(),
            null!,
            CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetEncinaCode().ShouldBe(EncinaErrorCodes.BehaviorNullNext);
    }

    [Fact]
    public async Task Handle_WithSuccessfulHandler_ReturnsSuccess()
    {
        var behavior = new QueryActivityPipelineBehavior<TestQuery, string>(NullFunctionalFailureDetector.Instance);

        var result = await behavior.Handle(
            new TestQuery("test"),
            RequestContext.Create(),
            () => ValueTask.FromResult(Right<EncinaError, string>("success")),
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        var value = result.Match(Left: _ => null!, Right: v => v);
        value.ShouldBe("success");
    }

    [Fact]
    public async Task Handle_WithHandlerError_ReturnsError()
    {
        var behavior = new QueryActivityPipelineBehavior<TestQuery, string>(NullFunctionalFailureDetector.Instance);
        var expectedError = EncinaErrors.Create("test.error", "Test error");

        var result = await behavior.Handle(
            new TestQuery("test"),
            RequestContext.Create(),
            () => ValueTask.FromResult(Left<EncinaError, string>(expectedError)),
            CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        var error = result.LeftAsEnumerable().First();
        error.Message.ShouldBe("Test error");
    }

    [Fact]
    public async Task Handle_WithCancellation_ReturnsCancellationError()
    {
        var behavior = new QueryActivityPipelineBehavior<TestQuery, string>(NullFunctionalFailureDetector.Instance);
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
        error.Message.ShouldContain("cancelled");
    }

    [Fact]
    public async Task Handle_WithException_ReturnsBehaviorExceptionError()
    {
        var behavior = new QueryActivityPipelineBehavior<TestQuery, string>(NullFunctionalFailureDetector.Instance);

        var result = await behavior.Handle(
            new TestQuery("test"),
            RequestContext.Create(),
            () => throw new InvalidOperationException("boom"),
            CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetEncinaCode().ShouldBe(EncinaErrorCodes.BehaviorException);
        error.Message.ShouldContain("Error running");
    }

    [Fact]
    public async Task Handle_CreatesActivityWithCorrectTags()
    {
        var behavior = new QueryActivityPipelineBehavior<TestQuery, string>(NullFunctionalFailureDetector.Instance);

        await behavior.Handle(
            new TestQuery("test"),
            RequestContext.Create(),
            () => ValueTask.FromResult(Right<EncinaError, string>("success")),
            CancellationToken.None);

        var activity = _activities.LastOrDefault(a => a.DisplayName.Contains("TestQuery"));
        activity.ShouldNotBeNull();
        activity.GetTagItem("Encina.request_kind").ShouldBe("query");
        ((string?)activity.GetTagItem("Encina.request_type"))!.ShouldContain("TestQuery");
        activity.GetTagItem("Encina.request_name").ShouldBe("TestQuery");
    }

    [Fact]
    public async Task Handle_WithFunctionalFailure_RecordsFailureInActivity()
    {
        var detector = new TestFailureDetector(detectsFailure: true, failureReason: "not.found", errorCode: "NF001", errorMessage: "Entity not found");
        var behavior = new QueryActivityPipelineBehavior<TestQuery, string>(detector);

        await behavior.Handle(
            new TestQuery("test"),
            RequestContext.Create(),
            () => ValueTask.FromResult(Right<EncinaError, string>("failure-response")),
            CancellationToken.None);

        var activity = _activities.LastOrDefault(a => a.DisplayName.Contains("TestQuery"));
        activity.ShouldNotBeNull();
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.GetTagItem("Encina.functional_failure").ShouldBe(true);
        activity.GetTagItem("Encina.failure_reason").ShouldBe("not.found");
        activity.GetTagItem("Encina.failure_code").ShouldBe("NF001");
        activity.GetTagItem("Encina.failure_message").ShouldBe("Entity not found");
    }

    [Fact]
    public async Task Handle_WithSuccess_SetsActivityStatusOk()
    {
        var behavior = new QueryActivityPipelineBehavior<TestQuery, string>(NullFunctionalFailureDetector.Instance);

        await behavior.Handle(
            new TestQuery("test"),
            RequestContext.Create(),
            () => ValueTask.FromResult(Right<EncinaError, string>("success")),
            CancellationToken.None);

        var activity = _activities.LastOrDefault(a => a.DisplayName.Contains("TestQuery"));
        activity.ShouldNotBeNull();
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    [Fact]
    public async Task Handle_WithNullFailureDetector_UsesNullDetector()
    {
        var behavior = new QueryActivityPipelineBehavior<TestQuery, string>(null!);

        var result = await behavior.Handle(
            new TestQuery("test"),
            RequestContext.Create(),
            () => ValueTask.FromResult(Right<EncinaError, string>("success")),
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WithPipelineError_ReturnsError()
    {
        // Note: Current implementation does NOT record pipeline errors in activity (only exceptions/cancellations)
        var behavior = new QueryActivityPipelineBehavior<TestQuery, string>(NullFunctionalFailureDetector.Instance);
        var error = EncinaErrors.Create("pipeline.error", "Pipeline failed");

        var result = await behavior.Handle(
            new TestQuery("test"),
            RequestContext.Create(),
            () => ValueTask.FromResult(Left<EncinaError, string>(error)),
            CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        var returnedError = result.LeftAsEnumerable().First();
        returnedError.GetEncinaCode().ShouldBe("pipeline.error");
    }

    [Fact]
    public async Task Handle_WithException_RecordsExceptionInActivity()
    {
        var behavior = new QueryActivityPipelineBehavior<TestQuery, string>(NullFunctionalFailureDetector.Instance);

        await behavior.Handle(
            new TestQuery("test"),
            RequestContext.Create(),
            () => throw new InvalidOperationException("Something went wrong"),
            CancellationToken.None);

        var activity = _activities.LastOrDefault(a => a.DisplayName.Contains("TestQuery"));
        activity.ShouldNotBeNull();
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.GetTagItem("exception.type").ShouldBe(typeof(InvalidOperationException).FullName);
        activity.GetTagItem("exception.message").ShouldBe("Something went wrong");
    }

    [Fact]
    public async Task Handle_WithCancellation_RecordsCancellationInActivity()
    {
        var behavior = new QueryActivityPipelineBehavior<TestQuery, string>(NullFunctionalFailureDetector.Instance);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await behavior.Handle(
            new TestQuery("test"),
            RequestContext.Create(),
            () => throw new OperationCanceledException(cts.Token),
            cts.Token);

        var activity = _activities.LastOrDefault(a => a.DisplayName.Contains("TestQuery"));
        activity.ShouldNotBeNull();
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.GetTagItem("Encina.cancelled").ShouldBe(true);
    }

    [Fact]
    public async Task Handle_WithFunctionalFailureNoCode_DoesNotSetCodeTag()
    {
        var detector = new TestFailureDetector(detectsFailure: true, failureReason: "failed", errorCode: null, errorMessage: null);
        var behavior = new QueryActivityPipelineBehavior<TestQuery, string>(detector);

        await behavior.Handle(
            new TestQuery("test"),
            RequestContext.Create(),
            () => ValueTask.FromResult(Right<EncinaError, string>("response")),
            CancellationToken.None);

        var activity = _activities.LastOrDefault(a => a.DisplayName.Contains("TestQuery"));
        activity.ShouldNotBeNull();
        activity.GetTagItem("Encina.failure_code").ShouldBeNull();
        activity.GetTagItem("Encina.failure_message").ShouldBeNull();
    }

    [Fact]
    public async Task Handle_ActivityName_IncludesQueryPrefix()
    {
        var behavior = new QueryActivityPipelineBehavior<TestQuery, string>(NullFunctionalFailureDetector.Instance);

        await behavior.Handle(
            new TestQuery("test"),
            RequestContext.Create(),
            () => ValueTask.FromResult(Right<EncinaError, string>("success")),
            CancellationToken.None);

        var activity = _activities.LastOrDefault(a => a.DisplayName.Contains("TestQuery"));
        activity.ShouldNotBeNull();
        activity.DisplayName.ShouldStartWith("Encina.Query.");
    }

    private sealed class TestFailureDetector(bool detectsFailure, string? failureReason, string? errorCode, string? errorMessage) : IFunctionalFailureDetector
    {
        public bool TryExtractFailure(object? response, out string reason, out object? capturedFailure)
        {
            reason = failureReason ?? string.Empty;
            capturedFailure = response;
            return detectsFailure;
        }

        public string? TryGetErrorCode(object? capturedFailure) => errorCode;

        public string? TryGetErrorMessage(object? capturedFailure) => errorMessage;
    }
}

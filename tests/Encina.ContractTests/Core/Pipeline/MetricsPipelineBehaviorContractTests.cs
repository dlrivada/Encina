using LanguageExt;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.ContractTests.Core.Pipeline;

/// <summary>
/// Behavioral contract tests for <see cref="CommandMetricsPipelineBehavior{TCommand, TResponse}"/>
/// and <see cref="QueryMetricsPipelineBehavior{TQuery, TResponse}"/> that execute real code paths:
/// success/failure metric tracking, exception handling, and functional failure detection.
/// </summary>
[Trait("Category", "Contract")]
[Trait("Feature", "MetricsPipeline")]
public sealed class MetricsPipelineBehaviorContractTests
{
    // -- Test request types --

    private sealed record TestCommand(string Value) : ICommand<string>;

    private sealed record TestQuery(string Value) : IQuery<string>;

    // -- Command Metrics Behavior --

    [Fact]
    public async Task Command_Handle_Success_ShouldTrackSuccessMetrics()
    {
        // Arrange
        var metrics = Substitute.For<IEncinaMetrics>();
        var detector = Substitute.For<IFunctionalFailureDetector>();
        detector.TryExtractFailure(Arg.Any<object?>(), out Arg.Any<string>()!, out Arg.Any<object?>()!)
            .Returns(false);

        var behavior = new CommandMetricsPipelineBehavior<TestCommand, string>(metrics, detector);
        var command = new TestCommand("ok");
        var context = RequestContext.Create();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("done"));

        // Act
        var result = await behavior.Handle(command, context, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        metrics.Received(1).TrackSuccess("command", nameof(TestCommand), Arg.Any<TimeSpan>());
        metrics.DidNotReceive().TrackFailure(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Command_Handle_WhenNextReturnsLeft_ShouldTrackFailureMetrics()
    {
        // Arrange
        var metrics = Substitute.For<IEncinaMetrics>();
        var detector = Substitute.For<IFunctionalFailureDetector>();
        var behavior = new CommandMetricsPipelineBehavior<TestCommand, string>(metrics, detector);
        var command = new TestCommand("fail");
        var context = RequestContext.Create();
        var error = EncinaError.New("command failed");
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Left<EncinaError, string>(error));

        // Act
        var result = await behavior.Handle(command, context, next, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        metrics.Received(1).TrackFailure("command", nameof(TestCommand), Arg.Any<TimeSpan>(), Arg.Any<string>());
        metrics.DidNotReceive().TrackSuccess(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>());
    }

    [Fact]
    public async Task Command_Handle_WhenNextThrows_ShouldTrackFailureWithExceptionType()
    {
        // Arrange
        var metrics = Substitute.For<IEncinaMetrics>();
        var detector = Substitute.For<IFunctionalFailureDetector>();
        var behavior = new CommandMetricsPipelineBehavior<TestCommand, string>(metrics, detector);
        var command = new TestCommand("throw");
        var context = RequestContext.Create();
        RequestHandlerCallback<string> next = () => throw new InvalidOperationException("Boom");

        // Act
        var result = await behavior.Handle(command, context, next, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        metrics.Received(1).TrackFailure("command", nameof(TestCommand), Arg.Any<TimeSpan>(), nameof(InvalidOperationException));
    }

    [Fact]
    public async Task Command_Handle_WhenCancelled_ShouldTrackCancelledFailure()
    {
        // Arrange
        var metrics = Substitute.For<IEncinaMetrics>();
        var detector = Substitute.For<IFunctionalFailureDetector>();
        var behavior = new CommandMetricsPipelineBehavior<TestCommand, string>(metrics, detector);
        var command = new TestCommand("cancel");
        var context = RequestContext.Create();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        RequestHandlerCallback<string> next = () => throw new OperationCanceledException();

        // Act
        var result = await behavior.Handle(command, context, next, cts.Token);

        // Assert
        result.IsLeft.ShouldBeTrue();
        metrics.Received(1).TrackFailure("command", nameof(TestCommand), Arg.Any<TimeSpan>(), "cancelled");
    }

    [Fact]
    public async Task Command_Handle_WithFunctionalFailure_ShouldTrackFailureWithReason()
    {
        // Arrange
        var metrics = Substitute.For<IEncinaMetrics>();
        var detector = Substitute.For<IFunctionalFailureDetector>();
        detector.TryExtractFailure(Arg.Any<object?>(), out Arg.Any<string>()!, out Arg.Any<object?>()!)
            .Returns(x =>
            {
                x[1] = "payment.declined";
                x[2] = (object?)"detail";
                return true;
            });

        var behavior = new CommandMetricsPipelineBehavior<TestCommand, string>(metrics, detector);
        var command = new TestCommand("payment");
        var context = RequestContext.Create();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("declined"));

        // Act
        var result = await behavior.Handle(command, context, next, CancellationToken.None);

        // Assert: Right is returned but failure metric is tracked
        result.IsRight.ShouldBeTrue();
        metrics.Received(1).TrackFailure("command", nameof(TestCommand), Arg.Any<TimeSpan>(), "payment.declined");
        metrics.DidNotReceive().TrackSuccess(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>());
    }

    [Fact]
    public void Command_Constructor_WithNullMetrics_ShouldThrow()
    {
        var detector = Substitute.For<IFunctionalFailureDetector>();
        Should.Throw<ArgumentNullException>(() =>
            new CommandMetricsPipelineBehavior<TestCommand, string>(null!, detector));
    }

    [Fact]
    public void Command_Constructor_WithNullDetector_ShouldFallbackToNullDetector()
    {
        var metrics = Substitute.For<IEncinaMetrics>();
        var behavior = new CommandMetricsPipelineBehavior<TestCommand, string>(metrics, null!);
        behavior.ShouldNotBeNull();
    }

    // -- Query Metrics Behavior --

    [Fact]
    public async Task Query_Handle_Success_ShouldTrackSuccessMetrics()
    {
        // Arrange
        var metrics = Substitute.For<IEncinaMetrics>();
        var detector = Substitute.For<IFunctionalFailureDetector>();
        detector.TryExtractFailure(Arg.Any<object?>(), out Arg.Any<string>()!, out Arg.Any<object?>()!)
            .Returns(false);

        var behavior = new QueryMetricsPipelineBehavior<TestQuery, string>(metrics, detector);
        var query = new TestQuery("search");
        var context = RequestContext.Create();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("results"));

        // Act
        var result = await behavior.Handle(query, context, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        metrics.Received(1).TrackSuccess("query", nameof(TestQuery), Arg.Any<TimeSpan>());
    }

    [Fact]
    public async Task Query_Handle_WhenNextReturnsLeft_ShouldTrackFailure()
    {
        // Arrange
        var metrics = Substitute.For<IEncinaMetrics>();
        var detector = Substitute.For<IFunctionalFailureDetector>();
        var behavior = new QueryMetricsPipelineBehavior<TestQuery, string>(metrics, detector);
        var query = new TestQuery("fail");
        var context = RequestContext.Create();
        var error = EncinaError.New("query failed");
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Left<EncinaError, string>(error));

        // Act
        var result = await behavior.Handle(query, context, next, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        metrics.Received(1).TrackFailure("query", nameof(TestQuery), Arg.Any<TimeSpan>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Query_Handle_WhenNextThrows_ShouldTrackFailureWithExceptionType()
    {
        // Arrange
        var metrics = Substitute.For<IEncinaMetrics>();
        var detector = Substitute.For<IFunctionalFailureDetector>();
        var behavior = new QueryMetricsPipelineBehavior<TestQuery, string>(metrics, detector);
        var query = new TestQuery("throw");
        var context = RequestContext.Create();
        RequestHandlerCallback<string> next = () => throw new ArgumentException("Bad query");

        // Act
        var result = await behavior.Handle(query, context, next, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        metrics.Received(1).TrackFailure("query", nameof(TestQuery), Arg.Any<TimeSpan>(), nameof(ArgumentException));
    }

    [Fact]
    public async Task Query_Handle_WhenCancelled_ShouldTrackCancelledFailure()
    {
        // Arrange
        var metrics = Substitute.For<IEncinaMetrics>();
        var detector = Substitute.For<IFunctionalFailureDetector>();
        var behavior = new QueryMetricsPipelineBehavior<TestQuery, string>(metrics, detector);
        var query = new TestQuery("cancel");
        var context = RequestContext.Create();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        RequestHandlerCallback<string> next = () => throw new OperationCanceledException();

        // Act
        var result = await behavior.Handle(query, context, next, cts.Token);

        // Assert
        result.IsLeft.ShouldBeTrue();
        metrics.Received(1).TrackFailure("query", nameof(TestQuery), Arg.Any<TimeSpan>(), "cancelled");
    }

    [Fact]
    public void Query_Constructor_WithNullMetrics_ShouldThrow()
    {
        var detector = Substitute.For<IFunctionalFailureDetector>();
        Should.Throw<ArgumentNullException>(() =>
            new QueryMetricsPipelineBehavior<TestQuery, string>(null!, detector));
    }
}

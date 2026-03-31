using static LanguageExt.Prelude;

namespace Encina.GuardTests.Core.Pipeline;

/// <summary>
/// Guard tests for <see cref="CommandMetricsPipelineBehavior{TCommand, TResponse}"/>
/// to verify constructor null guards and Handle error paths.
/// </summary>
public class CommandMetricsPipelineBehaviorGuardTests
{
    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when metrics is null.
    /// </summary>
    [Fact]
    public void Constructor_NullMetrics_ThrowsArgumentNullException()
    {
        var act = () => new CommandMetricsPipelineBehavior<TestCommand, TestResponse>(
            null!,
            Substitute.For<IFunctionalFailureDetector>());

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("metrics");
    }

    /// <summary>
    /// Verifies that the constructor accepts a null failure detector (falls back to NullFunctionalFailureDetector).
    /// </summary>
    [Fact]
    public void Constructor_NullFailureDetector_DoesNotThrow()
    {
        var act = () => new CommandMetricsPipelineBehavior<TestCommand, TestResponse>(
            Substitute.For<IEncinaMetrics>(),
            null!);

        Should.NotThrow(act);
    }

    /// <summary>
    /// Verifies that Handle returns Left with BehaviorNullRequest error when request is null,
    /// and tracks the failure via metrics.
    /// </summary>
    [Fact]
    public async Task Handle_NullRequest_ReturnsLeftAndTracksFailure()
    {
        var metrics = Substitute.For<IEncinaMetrics>();
        var behavior = CreateBehavior(metrics);

        var result = await behavior.Handle(
            null!,
            Substitute.For<IRequestContext>(),
            () => ValueTask.FromResult(Right<EncinaError, TestResponse>(new TestResponse())),
            CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.GetEncinaCode().ShouldBe(EncinaErrorCodes.BehaviorNullRequest));
        metrics.Received(1).TrackFailure(
            "command",
            nameof(TestCommand),
            TimeSpan.Zero,
            Arg.Any<string>());
    }

    /// <summary>
    /// Verifies that Handle returns Left with BehaviorNullNext error when nextStep is null,
    /// and tracks the failure via metrics.
    /// </summary>
    [Fact]
    public async Task Handle_NullNextStep_ReturnsLeftAndTracksFailure()
    {
        var metrics = Substitute.For<IEncinaMetrics>();
        var behavior = CreateBehavior(metrics);

        var result = await behavior.Handle(
            new TestCommand(),
            Substitute.For<IRequestContext>(),
            null!,
            CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.GetEncinaCode().ShouldBe(EncinaErrorCodes.BehaviorNullNext));
        metrics.Received(1).TrackFailure(
            "command",
            nameof(TestCommand),
            TimeSpan.Zero,
            Arg.Any<string>());
    }

    /// <summary>
    /// Verifies that Handle returns Left with BehaviorCancelled when cancellation is requested
    /// and the next step throws OperationCanceledException.
    /// </summary>
    [Fact]
    public async Task Handle_CancelledToken_ReturnsLeftAndTracksFailure()
    {
        var metrics = Substitute.For<IEncinaMetrics>();
        var behavior = CreateBehavior(metrics);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await behavior.Handle(
            new TestCommand(),
            Substitute.For<IRequestContext>(),
            () => throw new OperationCanceledException(cts.Token),
            cts.Token);

        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.GetEncinaCode().ShouldBe(EncinaErrorCodes.BehaviorCancelled));
        metrics.Received(1).TrackFailure(
            "command",
            nameof(TestCommand),
            Arg.Any<TimeSpan>(),
            "cancelled");
    }

    /// <summary>
    /// Verifies that Handle catches unexpected exceptions and tracks them as failures.
    /// </summary>
    [Fact]
    public async Task Handle_NextStepThrowsException_ReturnsLeftAndTracksFailure()
    {
        var metrics = Substitute.For<IEncinaMetrics>();
        var behavior = CreateBehavior(metrics);

        var result = await behavior.Handle(
            new TestCommand(),
            Substitute.For<IRequestContext>(),
            () => throw new InvalidOperationException("Boom"),
            CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.GetEncinaCode().ShouldBe(EncinaErrorCodes.BehaviorException));
        metrics.Received(1).TrackFailure(
            "command",
            nameof(TestCommand),
            Arg.Any<TimeSpan>(),
            nameof(InvalidOperationException));
    }

    /// <summary>
    /// Verifies that Handle tracks success via metrics when the next step returns Right.
    /// </summary>
    [Fact]
    public async Task Handle_SuccessfulNextStep_TracksSuccess()
    {
        var metrics = Substitute.For<IEncinaMetrics>();
        var behavior = CreateBehavior(metrics);

        var result = await behavior.Handle(
            new TestCommand(),
            Substitute.For<IRequestContext>(),
            () => ValueTask.FromResult(Right<EncinaError, TestResponse>(new TestResponse())),
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        metrics.Received(1).TrackSuccess(
            "command",
            nameof(TestCommand),
            Arg.Any<TimeSpan>());
    }

    /// <summary>
    /// Verifies that Handle tracks failure via metrics when the next step returns Left.
    /// </summary>
    [Fact]
    public async Task Handle_NextStepReturnsLeft_TracksFailure()
    {
        var metrics = Substitute.For<IEncinaMetrics>();
        var behavior = CreateBehavior(metrics);
        var error = EncinaError.New("pipeline failure");

        var result = await behavior.Handle(
            new TestCommand(),
            Substitute.For<IRequestContext>(),
            () => ValueTask.FromResult(Left<EncinaError, TestResponse>(error)),
            CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        metrics.Received(1).TrackFailure(
            "command",
            nameof(TestCommand),
            Arg.Any<TimeSpan>(),
            Arg.Any<string>());
    }

    private static CommandMetricsPipelineBehavior<TestCommand, TestResponse> CreateBehavior(
        IEncinaMetrics? metrics = null)
    {
        return new CommandMetricsPipelineBehavior<TestCommand, TestResponse>(
            metrics ?? Substitute.For<IEncinaMetrics>(),
            Substitute.For<IFunctionalFailureDetector>());
    }

    private sealed record TestCommand : ICommand<TestResponse>;

    private sealed record TestResponse;
}

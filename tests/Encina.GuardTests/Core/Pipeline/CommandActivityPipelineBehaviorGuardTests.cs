using static LanguageExt.Prelude;

namespace Encina.GuardTests.Core.Pipeline;

/// <summary>
/// Guard tests for <see cref="CommandActivityPipelineBehavior{TCommand, TResponse}"/>
/// to verify null parameter handling and error paths.
/// </summary>
public class CommandActivityPipelineBehaviorGuardTests
{
    /// <summary>
    /// Verifies that the constructor accepts a null failure detector and falls back to NullFunctionalFailureDetector.
    /// The behavior does NOT throw on null detector; it uses NullFunctionalFailureDetector.Instance instead.
    /// </summary>
    [Fact]
    public void Constructor_NullFailureDetector_DoesNotThrow()
    {
        var act = () => new CommandActivityPipelineBehavior<TestCommand, TestResponse>(null!);
        Should.NotThrow(act);
    }

    /// <summary>
    /// Verifies that Handle returns Left EncinaError when request is null
    /// instead of throwing, exercising the TryValidateRequest guard path.
    /// </summary>
    [Fact]
    public async Task Handle_NullRequest_ReturnsLeftWithBehaviorNullRequestError()
    {
        var behavior = CreateBehavior();

        var result = await behavior.Handle(
            null!,
            Substitute.For<IRequestContext>(),
            () => ValueTask.FromResult(Right<EncinaError, TestResponse>(new TestResponse())),
            CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.GetEncinaCode().ShouldBe(EncinaErrorCodes.BehaviorNullRequest));
    }

    /// <summary>
    /// Verifies that Handle returns Left EncinaError when nextStep is null
    /// instead of throwing, exercising the TryValidateNextStep guard path.
    /// </summary>
    [Fact]
    public async Task Handle_NullNextStep_ReturnsLeftWithBehaviorNullNextError()
    {
        var behavior = CreateBehavior();

        var result = await behavior.Handle(
            new TestCommand(),
            Substitute.For<IRequestContext>(),
            null!,
            CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.GetEncinaCode().ShouldBe(EncinaErrorCodes.BehaviorNullNext));
    }

    /// <summary>
    /// Verifies that Handle returns Left with BehaviorCancelled error code when
    /// the next step throws OperationCanceledException and the token is cancelled.
    /// </summary>
    [Fact]
    public async Task Handle_CancelledToken_ReturnsLeftWithBehaviorCancelledError()
    {
        var behavior = CreateBehavior();
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
    }

    /// <summary>
    /// Verifies that Handle catches unexpected exceptions from the next step
    /// and returns Left with BehaviorException error code.
    /// </summary>
    [Fact]
    public async Task Handle_NextStepThrowsException_ReturnsLeftWithBehaviorExceptionError()
    {
        var behavior = CreateBehavior();

        var result = await behavior.Handle(
            new TestCommand(),
            Substitute.For<IRequestContext>(),
            () => throw new InvalidOperationException("Boom"),
            CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.GetEncinaCode().ShouldBe(EncinaErrorCodes.BehaviorException));
    }

    /// <summary>
    /// Verifies that Handle propagates a successful Right result from the next step.
    /// </summary>
    [Fact]
    public async Task Handle_SuccessfulNextStep_ReturnsRight()
    {
        var behavior = CreateBehavior();
        var expected = new TestResponse { Value = "ok" };

        var result = await behavior.Handle(
            new TestCommand(),
            Substitute.For<IRequestContext>(),
            () => ValueTask.FromResult(Right<EncinaError, TestResponse>(expected)),
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that Handle propagates a Left error from the next step.
    /// </summary>
    [Fact]
    public async Task Handle_NextStepReturnsLeft_PropagatesError()
    {
        var behavior = CreateBehavior();
        var error = EncinaError.New("pipeline failed");

        var result = await behavior.Handle(
            new TestCommand(),
            Substitute.For<IRequestContext>(),
            () => ValueTask.FromResult(Left<EncinaError, TestResponse>(error)),
            CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
    }

    private static CommandActivityPipelineBehavior<TestCommand, TestResponse> CreateBehavior()
    {
        return new CommandActivityPipelineBehavior<TestCommand, TestResponse>(
            Substitute.For<IFunctionalFailureDetector>());
    }

    private sealed record TestCommand : ICommand<TestResponse>;

    private sealed record TestResponse
    {
        public string Value { get; init; } = string.Empty;
    }
}

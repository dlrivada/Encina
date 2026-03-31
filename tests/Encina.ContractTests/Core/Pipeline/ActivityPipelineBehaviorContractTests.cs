using LanguageExt;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.ContractTests.Core.Pipeline;

/// <summary>
/// Behavioral contract tests for <see cref="CommandActivityPipelineBehavior{TCommand, TResponse}"/>
/// and <see cref="QueryActivityPipelineBehavior{TQuery, TResponse}"/> that execute real code paths:
/// activity creation, success/failure/exception tracking, and functional failure detection.
/// </summary>
[Trait("Category", "Contract")]
[Trait("Feature", "ActivityPipeline")]
public sealed class ActivityPipelineBehaviorContractTests
{
    // -- Test request types --

    private sealed record TestCommand(string Value) : ICommand<string>;

    private sealed record TestQuery(string Value) : IQuery<string>;

    // -- Command Activity Behavior --

    [Fact]
    public async Task Command_Handle_Success_ShouldReturnRightAndInvokeNextStep()
    {
        // Arrange
        var detector = Substitute.For<IFunctionalFailureDetector>();
        detector.TryExtractFailure(Arg.Any<object?>(), out Arg.Any<string>()!, out Arg.Any<object?>()!)
            .Returns(false);

        var behavior = new CommandActivityPipelineBehavior<TestCommand, string>(detector);
        var command = new TestCommand("hello");
        var context = RequestContext.Create();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("ok"));

        // Act
        var result = await behavior.Handle(command, context, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(Right: v => v.ShouldBe("ok"), Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task Command_Handle_WhenNextStepReturnsLeft_ShouldReturnLeft()
    {
        // Arrange
        var detector = Substitute.For<IFunctionalFailureDetector>();
        var behavior = new CommandActivityPipelineBehavior<TestCommand, string>(detector);
        var command = new TestCommand("fail");
        var context = RequestContext.Create();
        var error = EncinaError.New("Pipeline error");
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Left<EncinaError, string>(error));

        // Act
        var result = await behavior.Handle(command, context, next, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Command_Handle_WhenNextStepThrows_ShouldCatchAndReturnLeft()
    {
        // Arrange
        var detector = Substitute.For<IFunctionalFailureDetector>();
        var behavior = new CommandActivityPipelineBehavior<TestCommand, string>(detector);
        var command = new TestCommand("throw");
        var context = RequestContext.Create();
        RequestHandlerCallback<string> next = () => throw new InvalidOperationException("Boom");

        // Act
        var result = await behavior.Handle(command, context, next, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: e => e.Message.ShouldContain("Error running"));
    }

    [Fact]
    public async Task Command_Handle_WhenCancelled_ShouldReturnCancelledError()
    {
        // Arrange
        var detector = Substitute.For<IFunctionalFailureDetector>();
        var behavior = new CommandActivityPipelineBehavior<TestCommand, string>(detector);
        var command = new TestCommand("cancel");
        var context = RequestContext.Create();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        RequestHandlerCallback<string> next = () => throw new OperationCanceledException();

        // Act
        var result = await behavior.Handle(command, context, next, cts.Token);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: e => e.Message.ShouldContain("cancelled"));
    }

    [Fact]
    public async Task Command_Handle_WithFunctionalFailure_ShouldDetectAndReturnRight()
    {
        // Arrange: detector reports a functional failure from a successful response
        var detector = Substitute.For<IFunctionalFailureDetector>();
        detector.TryExtractFailure(Arg.Any<object?>(), out Arg.Any<string>()!, out Arg.Any<object?>()!)
            .Returns(x =>
            {
                x[1] = "payment.declined";
                x[2] = (object?)"decline-detail";
                return true;
            });
        detector.TryGetErrorCode(Arg.Any<object?>()).Returns("PAY_001");
        detector.TryGetErrorMessage(Arg.Any<object?>()).Returns("Card declined");

        var behavior = new CommandActivityPipelineBehavior<TestCommand, string>(detector);
        var command = new TestCommand("payment");
        var context = RequestContext.Create();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("declined-response"));

        // Act
        var result = await behavior.Handle(command, context, next, CancellationToken.None);

        // Assert: the response is still Right (functional failure is recorded, not converted to Left)
        result.IsRight.ShouldBeTrue();
        detector.Received(1).TryExtractFailure(Arg.Any<object?>(), out Arg.Any<string>()!, out Arg.Any<object?>()!);
        detector.Received(1).TryGetErrorCode(Arg.Any<object?>());
        detector.Received(1).TryGetErrorMessage(Arg.Any<object?>());
    }

    [Fact]
    public void Command_Constructor_WithNullDetector_ShouldFallbackToNullDetector()
    {
        // Exercises the null-coalescing fallback to NullFunctionalFailureDetector.Instance
        var behavior = new CommandActivityPipelineBehavior<TestCommand, string>(null!);
        behavior.ShouldNotBeNull();
    }

    // -- Query Activity Behavior --

    [Fact]
    public async Task Query_Handle_Success_ShouldReturnRight()
    {
        // Arrange
        var detector = Substitute.For<IFunctionalFailureDetector>();
        detector.TryExtractFailure(Arg.Any<object?>(), out Arg.Any<string>()!, out Arg.Any<object?>()!)
            .Returns(false);

        var behavior = new QueryActivityPipelineBehavior<TestQuery, string>(detector);
        var query = new TestQuery("search");
        var context = RequestContext.Create();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("result"));

        // Act
        var result = await behavior.Handle(query, context, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(Right: v => v.ShouldBe("result"), Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task Query_Handle_WhenNextStepThrows_ShouldReturnLeft()
    {
        // Arrange
        var detector = Substitute.For<IFunctionalFailureDetector>();
        var behavior = new QueryActivityPipelineBehavior<TestQuery, string>(detector);
        var query = new TestQuery("boom");
        var context = RequestContext.Create();
        RequestHandlerCallback<string> next = () => throw new InvalidOperationException("Query failed");

        // Act
        var result = await behavior.Handle(query, context, next, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Query_Handle_WhenCancelled_ShouldReturnCancelledError()
    {
        // Arrange
        var detector = Substitute.For<IFunctionalFailureDetector>();
        var behavior = new QueryActivityPipelineBehavior<TestQuery, string>(detector);
        var query = new TestQuery("cancel");
        var context = RequestContext.Create();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        RequestHandlerCallback<string> next = () => throw new OperationCanceledException();

        // Act
        var result = await behavior.Handle(query, context, next, cts.Token);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: e => e.Message.ShouldContain("cancelled"));
    }

    [Fact]
    public async Task Query_Handle_WithFunctionalFailure_ShouldDetectAndReturnRight()
    {
        // Arrange
        var detector = Substitute.For<IFunctionalFailureDetector>();
        detector.TryExtractFailure(Arg.Any<object?>(), out Arg.Any<string>()!, out Arg.Any<object?>()!)
            .Returns(x =>
            {
                x[1] = "not.found";
                x[2] = (object?)"detail";
                return true;
            });
        detector.TryGetErrorCode(Arg.Any<object?>()).Returns("NOT_FOUND");
        detector.TryGetErrorMessage(Arg.Any<object?>()).Returns("Resource not found");

        var behavior = new QueryActivityPipelineBehavior<TestQuery, string>(detector);
        var query = new TestQuery("missing");
        var context = RequestContext.Create();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("empty"));

        // Act
        var result = await behavior.Handle(query, context, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        detector.Received(1).TryGetErrorCode(Arg.Any<object?>());
        detector.Received(1).TryGetErrorMessage(Arg.Any<object?>());
    }

    [Fact]
    public void Query_Constructor_WithNullDetector_ShouldFallbackToNullDetector()
    {
        var behavior = new QueryActivityPipelineBehavior<TestQuery, string>(null!);
        behavior.ShouldNotBeNull();
    }
}

using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.GuardTests.Core.Pipeline;

/// <summary>
/// Guard tests for <see cref="PipelineBuilder{TRequest, TResponse}"/> to verify constructor
/// null guards and Build parameter validation.
/// </summary>
public class PipelineBuilderGuardTests
{
    private readonly TestCommand _validRequest = new();
    private readonly IRequestHandler<TestCommand, string> _validHandler = Substitute.For<IRequestHandler<TestCommand, string>>();
    private readonly IRequestContext _validContext = RequestContext.Create();

    #region Constructor null guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when request is null.
    /// </summary>
    [Fact]
    public void Constructor_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        TestCommand request = null!;

        // Act & Assert
        var act = () => new PipelineBuilder<TestCommand, string>(request, _validHandler, _validContext, CancellationToken.None);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("request");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when handler is null.
    /// </summary>
    [Fact]
    public void Constructor_NullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        IRequestHandler<TestCommand, string> handler = null!;

        // Act & Assert
        var act = () => new PipelineBuilder<TestCommand, string>(_validRequest, handler, _validContext, CancellationToken.None);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("handler");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when context is null.
    /// </summary>
    [Fact]
    public void Constructor_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        IRequestContext context = null!;

        // Act & Assert
        var act = () => new PipelineBuilder<TestCommand, string>(_validRequest, _validHandler, context, CancellationToken.None);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("context");
    }

    /// <summary>
    /// Verifies that the constructor succeeds with all valid parameters.
    /// </summary>
    [Fact]
    public void Constructor_AllValidParameters_DoesNotThrow()
    {
        // Act & Assert
        var act = () => new PipelineBuilder<TestCommand, string>(_validRequest, _validHandler, _validContext, CancellationToken.None);
        Should.NotThrow(act);
    }

    #endregion

    #region Build null guard

    /// <summary>
    /// Verifies that Build throws ArgumentNullException when serviceProvider is null.
    /// </summary>
    [Fact]
    public void Build_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new PipelineBuilder<TestCommand, string>(_validRequest, _validHandler, _validContext, CancellationToken.None);
        IServiceProvider serviceProvider = null!;

        // Act & Assert
        var act = () => builder.Build(serviceProvider);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("serviceProvider");
    }

    /// <summary>
    /// Verifies that Build succeeds with a valid serviceProvider and returns a non-null callback.
    /// </summary>
    [Fact]
    public void Build_ValidServiceProvider_ReturnsNonNullCallback()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var builder = new PipelineBuilder<TestCommand, string>(_validRequest, _validHandler, _validContext, CancellationToken.None);

        // Act
        var callback = builder.Build(serviceProvider);

        // Assert
        callback.ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies that Build returns a delegate that invokes the handler when no behaviors are registered.
    /// </summary>
    [Fact]
    public async Task Build_NoBehaviors_InvokesHandler()
    {
        // Arrange
        var handler = Substitute.For<IRequestHandler<TestCommand, string>>();
        handler.Handle(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, string>>(Right<EncinaError, string>("success")));

        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var builder = new PipelineBuilder<TestCommand, string>(_validRequest, handler, _validContext, CancellationToken.None);

        // Act
        var callback = builder.Build(serviceProvider);
        var result = await callback();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: value => value.ShouldBe("success"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    /// <summary>
    /// Verifies that Build wraps handler cancellation into Left with correct error code.
    /// </summary>
    [Fact]
    public async Task Build_HandlerThrowsOperationCanceled_ReturnsLeftWithCancellationCode()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var handler = new ThrowingHandler();

        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var builder = new PipelineBuilder<TestCommand, string>(_validRequest, handler, _validContext, cts.Token);

        // Act
        var callback = builder.Build(serviceProvider);
        var result = await callback();

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.GetEncinaCode().ShouldBe(EncinaErrorCodes.HandlerCancelled));
    }

    #endregion

    #region Test Stubs

    public sealed class TestCommand : ICommand<string> { }

    private sealed class ThrowingHandler : IRequestHandler<TestCommand, string>
    {
        public Task<Either<EncinaError, string>> Handle(TestCommand request, CancellationToken cancellationToken)
            => throw new OperationCanceledException();
    }

    #endregion
}

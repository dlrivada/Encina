using Encina.EntityFrameworkCore.Modules;
using Encina.Modules;
using Encina.Modules.Isolation;
using LanguageExt;

namespace Encina.UnitTests.EntityFrameworkCore.Modules;

/// <summary>
/// Unit tests for <see cref="ModuleExecutionContextBehavior{TRequest, TResponse}"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ModuleExecutionContextBehaviorTests
{
    private readonly IModuleExecutionContext _moduleContext;
    private readonly IModuleHandlerRegistry _handlerRegistry;
    private readonly ILogger<ModuleExecutionContextBehavior<TestCommand, string>> _logger;

    public ModuleExecutionContextBehaviorTests()
    {
        _moduleContext = Substitute.For<IModuleExecutionContext>();
        _handlerRegistry = Substitute.For<IModuleHandlerRegistry>();
        _logger = Substitute.For<ILogger<ModuleExecutionContextBehavior<TestCommand, string>>>();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullModuleContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new ModuleExecutionContextBehavior<TestCommand, string>(null!, _handlerRegistry, _logger));
        ex.ParamName.ShouldBe("moduleContext");
    }

    [Fact]
    public void Constructor_NullHandlerRegistry_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new ModuleExecutionContextBehavior<TestCommand, string>(_moduleContext, null!, _logger));
        ex.ParamName.ShouldBe("handlerRegistry");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new ModuleExecutionContextBehavior<TestCommand, string>(_moduleContext, _handlerRegistry, null!));
        ex.ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Act
        var behavior = CreateBehavior();

        // Assert
        behavior.ShouldNotBeNull();
    }

    #endregion

    #region Handle - Null Argument Tests

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var behavior = CreateBehavior();
        var context = Substitute.For<IRequestContext>();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            behavior.Handle(null!, context, NextStep, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var behavior = CreateBehavior();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            behavior.Handle(new TestCommand(), null!, NextStep, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_NullNextStep_ThrowsArgumentNullException()
    {
        // Arrange
        var behavior = CreateBehavior();
        var context = Substitute.For<IRequestContext>();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            behavior.Handle(new TestCommand(), context, null!, CancellationToken.None).AsTask());
    }

    #endregion

    #region Handle - No Module Found Tests

    [Fact]
    public async Task Handle_NoModuleForHandler_CallsNextStepWithoutSettingContext()
    {
        // Arrange
        _handlerRegistry.GetModuleName(Arg.Any<Type>()).Returns((string?)null);
        var behavior = CreateBehavior();
        var context = CreateRequestContext();
        var nextStepCalled = false;

        RequestHandlerCallback<string> nextStep = () =>
        {
            nextStepCalled = true;
            return new ValueTask<Either<EncinaError, string>>(Either<EncinaError, string>.Right("result"));
        };

        // Act
        var result = await behavior.Handle(new TestCommand(), context, nextStep, CancellationToken.None);

        // Assert
        nextStepCalled.ShouldBeTrue();
        _moduleContext.DidNotReceive().CreateScope(Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_EmptyModuleName_CallsNextStepWithoutSettingContext()
    {
        // Arrange
        _handlerRegistry.GetModuleName(Arg.Any<Type>()).Returns("");
        var behavior = CreateBehavior();
        var context = CreateRequestContext();

        // Act
        var result = await behavior.Handle(new TestCommand(), context, NextStep, CancellationToken.None);

        // Assert
        _moduleContext.DidNotReceive().CreateScope(Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_WhitespaceModuleName_CallsNextStepWithoutSettingContext()
    {
        // Arrange
        _handlerRegistry.GetModuleName(Arg.Any<Type>()).Returns("   ");
        var behavior = CreateBehavior();
        var context = CreateRequestContext();

        // Act
        var result = await behavior.Handle(new TestCommand(), context, NextStep, CancellationToken.None);

        // Assert
        _moduleContext.DidNotReceive().CreateScope(Arg.Any<string>());
    }

    #endregion

    #region Handle - Module Context Set Tests

    [Fact]
    public async Task Handle_ModuleFound_CreatesModuleScope()
    {
        // Arrange
        _handlerRegistry.GetModuleName(Arg.Any<Type>()).Returns("Orders");
        var disposableScope = Substitute.For<IDisposable>();
        _moduleContext.CreateScope("Orders").Returns(disposableScope);
        var behavior = CreateBehavior();
        var context = CreateRequestContext();

        // Act
        await behavior.Handle(new TestCommand(), context, NextStep, CancellationToken.None);

        // Assert
        _moduleContext.Received(1).CreateScope("Orders");
    }

    [Fact]
    public async Task Handle_ModuleFound_DisposesScope()
    {
        // Arrange
        _handlerRegistry.GetModuleName(Arg.Any<Type>()).Returns("Orders");
        var disposableScope = Substitute.For<IDisposable>();
        _moduleContext.CreateScope("Orders").Returns(disposableScope);
        var behavior = CreateBehavior();
        var context = CreateRequestContext();

        // Act
        await behavior.Handle(new TestCommand(), context, NextStep, CancellationToken.None);

        // Assert
        disposableScope.Received(1).Dispose();
    }

    [Fact]
    public async Task Handle_ModuleFound_CallsNextStep()
    {
        // Arrange
        _handlerRegistry.GetModuleName(Arg.Any<Type>()).Returns("Orders");
        _moduleContext.CreateScope("Orders").Returns(Substitute.For<IDisposable>());
        var behavior = CreateBehavior();
        var context = CreateRequestContext();
        var nextStepCalled = false;

        RequestHandlerCallback<string> nextStep = () =>
        {
            nextStepCalled = true;
            return new ValueTask<Either<EncinaError, string>>(Either<EncinaError, string>.Right("result"));
        };

        // Act
        await behavior.Handle(new TestCommand(), context, nextStep, CancellationToken.None);

        // Assert
        nextStepCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_ModuleFound_ReturnsNextStepResult()
    {
        // Arrange
        _handlerRegistry.GetModuleName(Arg.Any<Type>()).Returns("Orders");
        _moduleContext.CreateScope("Orders").Returns(Substitute.For<IDisposable>());
        var behavior = CreateBehavior();
        var context = CreateRequestContext();
        var expected = "expected-result";

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<EncinaError, string>>(Either<EncinaError, string>.Right(expected));

        // Act
        var result = await behavior.Handle(new TestCommand(), context, nextStep, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: val => val.ShouldBe(expected),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region Handle - Error Propagation Tests

    [Fact]
    public async Task Handle_NextStepReturnsError_PropagatesError()
    {
        // Arrange
        _handlerRegistry.GetModuleName(Arg.Any<Type>()).Returns("Orders");
        _moduleContext.CreateScope("Orders").Returns(Substitute.For<IDisposable>());
        var behavior = CreateBehavior();
        var context = CreateRequestContext();
        var error = EncinaErrors.Create("TEST_ERROR", "test error");

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<EncinaError, string>>(Either<EncinaError, string>.Left(error));

        // Act
        var result = await behavior.Handle(new TestCommand(), context, nextStep, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_NextStepThrows_DisposesScope()
    {
        // Arrange
        _handlerRegistry.GetModuleName(Arg.Any<Type>()).Returns("Orders");
        var disposableScope = Substitute.For<IDisposable>();
        _moduleContext.CreateScope("Orders").Returns(disposableScope);
        var behavior = CreateBehavior();
        var context = CreateRequestContext();

        RequestHandlerCallback<string> nextStep = () =>
            throw new InvalidOperationException("Handler failed");

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() =>
            behavior.Handle(new TestCommand(), context, nextStep, CancellationToken.None).AsTask());

        // Scope should still be disposed even on exception (via using)
        disposableScope.Received(1).Dispose();
    }

    #endregion

    #region Handle - Handler Type Resolution Tests

    [Fact]
    public async Task Handle_LooksUpHandlerTypeFromRequestResponse()
    {
        // Arrange
        _handlerRegistry.GetModuleName(Arg.Any<Type>()).Returns((string?)null);
        var behavior = CreateBehavior();
        var context = CreateRequestContext();

        // Act
        await behavior.Handle(new TestCommand(), context, NextStep, CancellationToken.None);

        // Assert - should look up the IRequestHandler<TestCommand, string> type
        _handlerRegistry.Received(1).GetModuleName(typeof(IRequestHandler<TestCommand, string>));
    }

    #endregion

    #region Helpers

    private ModuleExecutionContextBehavior<TestCommand, string> CreateBehavior()
    {
        return new ModuleExecutionContextBehavior<TestCommand, string>(
            _moduleContext, _handlerRegistry, _logger);
    }

    private static IRequestContext CreateRequestContext()
    {
        var context = Substitute.For<IRequestContext>();
        context.CorrelationId.Returns(Guid.NewGuid().ToString());
        return context;
    }

    private static ValueTask<Either<EncinaError, string>> NextStep()
    {
        return new ValueTask<Either<EncinaError, string>>(
            Either<EncinaError, string>.Right("ok"));
    }

    #endregion

    #region Test Types

    public sealed record TestCommand : IRequest<string>;

    #endregion
}

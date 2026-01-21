using Encina.Dapper.SqlServer.ReadWriteSeparation;
using Encina.Messaging.ReadWriteSeparation;
using LanguageExt;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Dapper.SqlServer.ReadWriteSeparation;

/// <summary>
/// Unit tests for <see cref="ReadWriteRoutingPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ReadWriteRoutingPipelineBehaviorTests
{
    private readonly ILogger<ReadWriteRoutingPipelineBehavior<TestCommand, string>> _commandLogger;
    private readonly ILogger<ReadWriteRoutingPipelineBehavior<TestQuery, string>> _queryLogger;
    private readonly ILogger<ReadWriteRoutingPipelineBehavior<TestForceWriteQuery, string>> _forceWriteLogger;
    private readonly IRequestContext _context;

    public ReadWriteRoutingPipelineBehaviorTests()
    {
        _commandLogger = Substitute.For<ILogger<ReadWriteRoutingPipelineBehavior<TestCommand, string>>>();
        _queryLogger = Substitute.For<ILogger<ReadWriteRoutingPipelineBehavior<TestQuery, string>>>();
        _forceWriteLogger = Substitute.For<ILogger<ReadWriteRoutingPipelineBehavior<TestForceWriteQuery, string>>>();
        _context = Substitute.For<IRequestContext>();
        _context.CorrelationId.Returns("test-correlation-id");
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ReadWriteRoutingPipelineBehavior<TestCommand, string>(null!));
    }

    [Fact]
    public void Constructor_WithValidLogger_DoesNotThrow()
    {
        // Act
        var behavior = new ReadWriteRoutingPipelineBehavior<TestCommand, string>(_commandLogger);

        // Assert
        behavior.ShouldNotBeNull();
    }

    #endregion

    #region Handle - Null Parameter Tests

    [Fact]
    public async Task Handle_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var behavior = new ReadWriteRoutingPipelineBehavior<TestCommand, string>(_commandLogger);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await behavior.Handle(
                null!,
                _context,
                () => ValueTask.FromResult(Right<EncinaError, string>("success")),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var behavior = new ReadWriteRoutingPipelineBehavior<TestCommand, string>(_commandLogger);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await behavior.Handle(
                new TestCommand(),
                null!,
                () => ValueTask.FromResult(Right<EncinaError, string>("success")),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithNullNextStep_ThrowsArgumentNullException()
    {
        // Arrange
        var behavior = new ReadWriteRoutingPipelineBehavior<TestCommand, string>(_commandLogger);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await behavior.Handle(
                new TestCommand(),
                _context,
                null!,
                CancellationToken.None));
    }

    #endregion

    #region Handle - Intent Routing Tests

    [Fact]
    public async Task Handle_WithCommand_SetsDatabaseIntentToWrite()
    {
        // Arrange
        var behavior = new ReadWriteRoutingPipelineBehavior<TestCommand, string>(_commandLogger);
        DatabaseIntent? capturedIntent = null;

        // Act
        await behavior.Handle(
            new TestCommand(),
            _context,
            () =>
            {
                capturedIntent = DatabaseRoutingContext.CurrentIntent;
                return ValueTask.FromResult(Right<EncinaError, string>("success"));
            },
            CancellationToken.None);

        // Assert
        capturedIntent.ShouldBe(DatabaseIntent.Write);
    }

    [Fact]
    public async Task Handle_WithQuery_SetsDatabaseIntentToRead()
    {
        // Arrange
        var behavior = new ReadWriteRoutingPipelineBehavior<TestQuery, string>(_queryLogger);
        DatabaseIntent? capturedIntent = null;

        // Act
        await behavior.Handle(
            new TestQuery(),
            _context,
            () =>
            {
                capturedIntent = DatabaseRoutingContext.CurrentIntent;
                return ValueTask.FromResult(Right<EncinaError, string>("success"));
            },
            CancellationToken.None);

        // Assert
        capturedIntent.ShouldBe(DatabaseIntent.Read);
    }

    [Fact]
    public async Task Handle_WithForceWriteQuery_SetsDatabaseIntentToForceWrite()
    {
        // Arrange
        var behavior = new ReadWriteRoutingPipelineBehavior<TestForceWriteQuery, string>(_forceWriteLogger);
        DatabaseIntent? capturedIntent = null;

        // Act
        await behavior.Handle(
            new TestForceWriteQuery(),
            _context,
            () =>
            {
                capturedIntent = DatabaseRoutingContext.CurrentIntent;
                return ValueTask.FromResult(Right<EncinaError, string>("success"));
            },
            CancellationToken.None);

        // Assert
        capturedIntent.ShouldBe(DatabaseIntent.ForceWrite);
    }

    #endregion

    #region Handle - Context Management Tests

    [Fact]
    public async Task Handle_AfterExecution_RestoresOriginalContext()
    {
        // Arrange
        var behavior = new ReadWriteRoutingPipelineBehavior<TestCommand, string>(_commandLogger);

        // Set up a previous context
        using var outerScope = DatabaseRoutingScope.ForRead();

        // Act
        await behavior.Handle(
            new TestCommand(),
            _context,
            () => ValueTask.FromResult(Right<EncinaError, string>("success")),
            CancellationToken.None);

        // Assert - After the behavior completes, the original context should be restored
        DatabaseRoutingContext.CurrentIntent.ShouldBe(DatabaseIntent.Read);
    }

    [Fact]
    public async Task Handle_EnablesRoutingContextDuringExecution()
    {
        // Arrange
        var behavior = new ReadWriteRoutingPipelineBehavior<TestCommand, string>(_commandLogger);
        var wasEnabled = false;

        // Act
        await behavior.Handle(
            new TestCommand(),
            _context,
            () =>
            {
                wasEnabled = DatabaseRoutingContext.IsEnabled;
                return ValueTask.FromResult(Right<EncinaError, string>("success"));
            },
            CancellationToken.None);

        // Assert
        wasEnabled.ShouldBeTrue();
    }

    #endregion

    #region Handle - Result Propagation Tests

    [Fact]
    public async Task Handle_ReturnsResultFromNextStep()
    {
        // Arrange
        var behavior = new ReadWriteRoutingPipelineBehavior<TestCommand, string>(_commandLogger);
        var expectedResult = "expected result";

        // Act
        var result = await behavior.Handle(
            new TestCommand(),
            _context,
            () => ValueTask.FromResult(Right<EncinaError, string>(expectedResult)),
            CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Left: _ => string.Empty,
            Right: r => r).ShouldBe(expectedResult);
    }

    [Fact]
    public async Task Handle_PropagatesErrorFromNextStep()
    {
        // Arrange
        var behavior = new ReadWriteRoutingPipelineBehavior<TestCommand, string>(_commandLogger);
        var expectedError = EncinaError.New("Test error");

        // Act
        var result = await behavior.Handle(
            new TestCommand(),
            _context,
            () => ValueTask.FromResult(Left<EncinaError, string>(expectedError)),
            CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Interface Implementation Tests

    [Fact]
    public void ImplementsIPipelineBehavior()
    {
        // Arrange
        var behavior = new ReadWriteRoutingPipelineBehavior<TestCommand, string>(_commandLogger);

        // Assert
        (behavior is IPipelineBehavior<TestCommand, string>).ShouldBeTrue();
    }

    #endregion

    // Test types
    public sealed record TestCommand : ICommand<string>;

    public sealed record TestQuery : IQuery<string>;

    [ForceWriteDatabase(Reason = "Test force write")]
    public sealed record TestForceWriteQuery : IQuery<string>;
}

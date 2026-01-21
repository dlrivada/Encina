using Encina.Messaging;
using Encina.Messaging.ReadWriteSeparation;
using Encina.MongoDB.ReadWriteSeparation;
using LanguageExt;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.MongoDB.ReadWriteSeparation;

public sealed class ReadWriteRoutingPipelineBehaviorTests
{
    private readonly ILogger<ReadWriteRoutingPipelineBehavior<TestQuery, string>> _queryLogger;
    private readonly ILogger<ReadWriteRoutingPipelineBehavior<TestCommand, string>> _commandLogger;
    private readonly ILogger<ReadWriteRoutingPipelineBehavior<ForceWriteQuery, string>> _forceWriteLogger;
    private readonly IRequestContext _context;

    public ReadWriteRoutingPipelineBehaviorTests()
    {
        _queryLogger = Substitute.For<ILogger<ReadWriteRoutingPipelineBehavior<TestQuery, string>>>();
        _commandLogger = Substitute.For<ILogger<ReadWriteRoutingPipelineBehavior<TestCommand, string>>>();
        _forceWriteLogger = Substitute.For<ILogger<ReadWriteRoutingPipelineBehavior<ForceWriteQuery, string>>>();
        _context = Substitute.For<IRequestContext>();
        _context.CorrelationId.Returns("test-correlation-id");
    }

    [Fact]
    public void Constructor_ThrowsOnNullLogger()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ReadWriteRoutingPipelineBehavior<TestQuery, string>(null!));
    }

    [Fact]
    public async Task Handle_ThrowsOnNullRequest()
    {
        // Arrange
        var behavior = new ReadWriteRoutingPipelineBehavior<TestQuery, string>(_queryLogger);
        RequestHandlerCallback<string> nextStep = () => ValueTask.FromResult(Either<EncinaError, string>.Right("result"));

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await behavior.Handle(null!, _context, nextStep, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ThrowsOnNullContext()
    {
        // Arrange
        var behavior = new ReadWriteRoutingPipelineBehavior<TestQuery, string>(_queryLogger);
        var request = new TestQuery();
        RequestHandlerCallback<string> nextStep = () => ValueTask.FromResult(Either<EncinaError, string>.Right("result"));

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await behavior.Handle(request, null!, nextStep, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ThrowsOnNullNextStep()
    {
        // Arrange
        var behavior = new ReadWriteRoutingPipelineBehavior<TestQuery, string>(_queryLogger);
        var request = new TestQuery();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await behavior.Handle(request, _context, null!, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SetsReadIntentForQuery()
    {
        // Arrange
        var behavior = new ReadWriteRoutingPipelineBehavior<TestQuery, string>(_queryLogger);
        var request = new TestQuery();
        DatabaseIntent? capturedIntent = null;

        RequestHandlerCallback<string> nextStep = () =>
        {
            capturedIntent = DatabaseRoutingContext.CurrentIntent;
            return ValueTask.FromResult(Either<EncinaError, string>.Right("result"));
        };

        // Act
        await behavior.Handle(request, _context, nextStep, CancellationToken.None);

        // Assert
        capturedIntent.ShouldBe(DatabaseIntent.Read);
    }

    [Fact]
    public async Task Handle_SetsWriteIntentForCommand()
    {
        // Arrange
        var behavior = new ReadWriteRoutingPipelineBehavior<TestCommand, string>(_commandLogger);
        var request = new TestCommand();
        DatabaseIntent? capturedIntent = null;

        RequestHandlerCallback<string> nextStep = () =>
        {
            capturedIntent = DatabaseRoutingContext.CurrentIntent;
            return ValueTask.FromResult(Either<EncinaError, string>.Right("result"));
        };

        // Act
        await behavior.Handle(request, _context, nextStep, CancellationToken.None);

        // Assert
        capturedIntent.ShouldBe(DatabaseIntent.Write);
    }

    [Fact]
    public async Task Handle_SetsForceWriteIntentForQueryWithAttribute()
    {
        // Arrange
        var behavior = new ReadWriteRoutingPipelineBehavior<ForceWriteQuery, string>(_forceWriteLogger);
        var request = new ForceWriteQuery();
        DatabaseIntent? capturedIntent = null;

        RequestHandlerCallback<string> nextStep = () =>
        {
            capturedIntent = DatabaseRoutingContext.CurrentIntent;
            return ValueTask.FromResult(Either<EncinaError, string>.Right("result"));
        };

        // Act
        await behavior.Handle(request, _context, nextStep, CancellationToken.None);

        // Assert
        capturedIntent.ShouldBe(DatabaseIntent.ForceWrite);
    }

    [Fact]
    public async Task Handle_ReturnsResultFromNextStep()
    {
        // Arrange
        var behavior = new ReadWriteRoutingPipelineBehavior<TestQuery, string>(_queryLogger);
        var request = new TestQuery();
        var expectedResult = "expected-result";

        RequestHandlerCallback<string> nextStep = () =>
            ValueTask.FromResult(Either<EncinaError, string>.Right(expectedResult));

        // Act
        var result = await behavior.Handle(request, _context, nextStep, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(r => r.ShouldBe(expectedResult));
    }

    [Fact]
    public async Task Handle_ReturnsErrorFromNextStep()
    {
        // Arrange
        var behavior = new ReadWriteRoutingPipelineBehavior<TestQuery, string>(_queryLogger);
        var request = new TestQuery();
        var expectedError = EncinaError.New("Test error");

        RequestHandlerCallback<string> nextStep = () =>
            ValueTask.FromResult(Either<EncinaError, string>.Left(expectedError));

        // Act
        var result = await behavior.Handle(request, _context, nextStep, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.Message.ShouldBe("Test error"));
    }

    [Fact]
    public async Task Handle_RestoresContextAfterExecution()
    {
        // Arrange
        var behavior = new ReadWriteRoutingPipelineBehavior<TestQuery, string>(_queryLogger);
        var request = new TestQuery();

        RequestHandlerCallback<string> nextStep = () =>
            ValueTask.FromResult(Either<EncinaError, string>.Right("result"));

        // Get initial intent
        var initialIntent = DatabaseRoutingContext.CurrentIntent;

        // Act
        await behavior.Handle(request, _context, nextStep, CancellationToken.None);

        // Assert - context should be restored to initial state
        DatabaseRoutingContext.CurrentIntent.ShouldBe(initialIntent);
    }

    [Fact]
    public async Task Handle_RestoresContextEvenOnException()
    {
        // Arrange
        var behavior = new ReadWriteRoutingPipelineBehavior<TestQuery, string>(_queryLogger);
        var request = new TestQuery();

        RequestHandlerCallback<string> nextStep = () =>
            throw new InvalidOperationException("Test exception");

        // Get initial intent
        var initialIntent = DatabaseRoutingContext.CurrentIntent;

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await behavior.Handle(request, _context, nextStep, CancellationToken.None));

        // Context should be restored to initial state
        DatabaseRoutingContext.CurrentIntent.ShouldBe(initialIntent);
    }

    // Test types
    public sealed record TestQuery : IQuery<string>;

    public sealed record TestCommand : ICommand<string>;

    [ForceWriteDatabase(Reason = "Test read-after-write consistency")]
    public sealed record ForceWriteQuery : IQuery<string>;
}

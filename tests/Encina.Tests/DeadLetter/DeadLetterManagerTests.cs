using Encina.Messaging.DeadLetter;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace Encina.Tests.DeadLetter;

public sealed class DeadLetterManagerTests
{
    private readonly IDeadLetterStore _store;
    private readonly DeadLetterOrchestrator _orchestrator;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DeadLetterManager> _logger;
    private readonly DeadLetterManager _manager;

    public DeadLetterManagerTests()
    {
        _store = Substitute.For<IDeadLetterStore>();
        var factory = Substitute.For<IDeadLetterMessageFactory>();
        var options = new DeadLetterOptions();
        var orchestratorLogger = Substitute.For<ILogger<DeadLetterOrchestrator>>();
        _orchestrator = new DeadLetterOrchestrator(_store, factory, options, orchestratorLogger);
        _serviceProvider = Substitute.For<IServiceProvider>();
        _logger = Substitute.For<ILogger<DeadLetterManager>>();
        _manager = new DeadLetterManager(_store, _orchestrator, _serviceProvider, _logger);
    }

    [Fact]
    public void Constructor_WithNullStore_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new DeadLetterManager(null!, _orchestrator, _serviceProvider, _logger));
    }

    [Fact]
    public void Constructor_WithNullOrchestrator_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new DeadLetterManager(_store, null!, _serviceProvider, _logger));
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new DeadLetterManager(_store, _orchestrator, null!, _logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new DeadLetterManager(_store, _orchestrator, _serviceProvider, null!));
    }

    [Fact]
    public async Task ReplayAsync_WhenMessageNotFound_ReturnsError()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        _store.GetAsync(messageId, Arg.Any<CancellationToken>()).Returns((IDeadLetterMessage?)null);

        // Act
        var result = await _manager.ReplayAsync(messageId);

        // Assert
        var error = result.ShouldBeError();
        error.Message.ShouldContain(DeadLetterErrorCodes.NotFound);
    }

    [Fact]
    public async Task ReplayAsync_WhenMessageAlreadyReplayed_ReturnsError()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = Substitute.For<IDeadLetterMessage>();
        message.Id.Returns(messageId);
        message.IsReplayed.Returns(true);
        message.IsExpired.Returns(false);
        _store.GetAsync(messageId, Arg.Any<CancellationToken>()).Returns(message);

        // Act
        var result = await _manager.ReplayAsync(messageId);

        // Assert
        var error = result.ShouldBeError();
        error.Message.ShouldContain(DeadLetterErrorCodes.AlreadyReplayed);
    }

    [Fact]
    public async Task ReplayAsync_WhenMessageExpired_ReturnsError()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = Substitute.For<IDeadLetterMessage>();
        message.Id.Returns(messageId);
        message.IsReplayed.Returns(false);
        message.IsExpired.Returns(true);
        _store.GetAsync(messageId, Arg.Any<CancellationToken>()).Returns(message);

        // Act
        var result = await _manager.ReplayAsync(messageId);

        // Assert
        var error = result.ShouldBeError();
        error.Message.ShouldContain(DeadLetterErrorCodes.Expired);
    }

    [Fact]
    public async Task ReplayAsync_WhenRequestTypeCannotBeResolved_ReturnsError()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = Substitute.For<IDeadLetterMessage>();
        message.Id.Returns(messageId);
        message.IsReplayed.Returns(false);
        message.IsExpired.Returns(false);
        message.RequestType.Returns("NonExistent.Type, NonExistent.Assembly");
        message.RequestContent.Returns("{}");
        _store.GetAsync(messageId, Arg.Any<CancellationToken>()).Returns(message);

        // Act
        var result = await _manager.ReplayAsync(messageId);

        // Assert
        var error = result.ShouldBeError();
        error.Message.ShouldContain(DeadLetterErrorCodes.DeserializationFailed);
        await _store.Received(1).MarkAsReplayedAsync(messageId, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMessageAsync_DelegatesToStore()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = Substitute.For<IDeadLetterMessage>();
        _store.GetAsync(messageId, Arg.Any<CancellationToken>()).Returns(message);

        // Act
        var result = await _manager.GetMessageAsync(messageId);

        // Assert
        result.ShouldBe(message);
    }

    [Fact]
    public async Task GetMessagesAsync_DelegatesToStore()
    {
        // Arrange
        var messages = new List<IDeadLetterMessage>
        {
            Substitute.For<IDeadLetterMessage>(),
            Substitute.For<IDeadLetterMessage>()
        };
        _store.GetMessagesAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(messages);

        // Act
        var result = await _manager.GetMessagesAsync();

        // Assert
        result.Count().ShouldBe(2);
    }

    [Fact]
    public async Task GetCountAsync_DelegatesToStore()
    {
        // Arrange
        _store.GetCountAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<CancellationToken>())
            .Returns(42);

        // Act
        var result = await _manager.GetCountAsync();

        // Assert
        result.ShouldBe(42);
    }

    [Fact]
    public async Task DeleteAsync_DelegatesToStore()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        _store.DeleteAsync(messageId, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _manager.DeleteAsync(messageId);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteAllAsync_DeletesMatchingMessages()
    {
        // Arrange
        var filter = new DeadLetterFilter { SourcePattern = "Test" };
        var messages = new List<IDeadLetterMessage>
        {
            CreateMessage(Guid.NewGuid()),
            CreateMessage(Guid.NewGuid())
        };

        _store.GetMessagesAsync(filter, 0, int.MaxValue, Arg.Any<CancellationToken>())
            .Returns(messages);
        _store.DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _manager.DeleteAllAsync(filter);

        // Assert
        result.ShouldBe(2);
        await _store.Received(2).DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplayAllAsync_WithNullFilter_ThrowsArgumentNullException()
    {
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _manager.ReplayAllAsync(null!));
    }

    [Fact]
    public async Task ReplayAllAsync_SetsExcludeReplayedToTrue()
    {
        // Arrange
        var filter = new DeadLetterFilter { SourcePattern = "Test" };
        _store.GetMessagesAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        // Act
        await _manager.ReplayAllAsync(filter);

        // Assert
        filter.ExcludeReplayed.ShouldBe(true);
    }

    private static IDeadLetterMessage CreateMessage(Guid id)
    {
        var message = Substitute.For<IDeadLetterMessage>();
        message.Id.Returns(id);
        return message;
    }
}

using Encina.Messaging.DeadLetter;
using Encina.Messaging.Recoverability;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace Encina.Tests.DeadLetter;

public sealed class DeadLetterOrchestratorTests
{
    private readonly IDeadLetterStore _store;
    private readonly IDeadLetterMessageFactory _messageFactory;
    private readonly DeadLetterOptions _options;
    private readonly ILogger<DeadLetterOrchestrator> _logger;
    private readonly DeadLetterOrchestrator _orchestrator;

    public DeadLetterOrchestratorTests()
    {
        _store = Substitute.For<IDeadLetterStore>();
        _messageFactory = Substitute.For<IDeadLetterMessageFactory>();
        _options = new DeadLetterOptions();
        _logger = Substitute.For<ILogger<DeadLetterOrchestrator>>();
        _orchestrator = new DeadLetterOrchestrator(_store, _messageFactory, _options, _logger);
    }

    [Fact]
    public void Constructor_WithNullStore_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new DeadLetterOrchestrator(null!, _messageFactory, _options, _logger));
    }

    [Fact]
    public void Constructor_WithNullMessageFactory_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new DeadLetterOrchestrator(_store, null!, _options, _logger));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new DeadLetterOrchestrator(_store, _messageFactory, null!, _logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new DeadLetterOrchestrator(_store, _messageFactory, _options, null!));
    }

    [Fact]
    public async Task AddAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _orchestrator.AddAsync<object>(null!, EncinaError.New("test"), null, "Test", 1, DateTime.UtcNow));
    }

    [Fact]
    public async Task AddAsync_WithNullSourcePattern_ThrowsArgumentException()
    {
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _orchestrator.AddAsync("request", EncinaError.New("test"), null, null!, 1, DateTime.UtcNow));
    }

    [Fact]
    public async Task AddAsync_WithEmptySourcePattern_ThrowsArgumentException()
    {
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _orchestrator.AddAsync("request", EncinaError.New("test"), null, "", 1, DateTime.UtcNow));
    }

    [Fact]
    public async Task AddAsync_CreatesAndStoresMessage()
    {
        // Arrange
        var request = new TestRequest("test");
        var error = EncinaError.New("Test error");
        var sourcePattern = "Test";
        var firstFailedAt = DateTime.UtcNow.AddMinutes(-5);

        var createdMessage = Substitute.For<IDeadLetterMessage>();
        createdMessage.Id.Returns(Guid.NewGuid());
        createdMessage.RequestType.Returns(typeof(TestRequest).FullName!);
        createdMessage.ErrorMessage.Returns("Test error");

        _messageFactory.Create(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<DateTime>(),
            Arg.Any<DateTime>(),
            Arg.Any<DateTime?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>())
            .Returns(createdMessage);

        // Act
        var result = await _orchestrator.AddAsync(
            request,
            error,
            null,
            sourcePattern,
            3,
            firstFailedAt);

        // Assert
        result.ShouldBe(createdMessage);
        await _store.Received(1).AddAsync(createdMessage, Arg.Any<CancellationToken>());
        await _store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_InvokesOnDeadLetterCallback()
    {
        // Arrange
        var callbackInvoked = false;
        IDeadLetterMessage? receivedMessage = null;

        _options.OnDeadLetter = (msg, ct) =>
        {
            callbackInvoked = true;
            receivedMessage = msg;
            return Task.CompletedTask;
        };

        var createdMessage = Substitute.For<IDeadLetterMessage>();
        createdMessage.Id.Returns(Guid.NewGuid());
        createdMessage.RequestType.Returns("TestRequest");
        createdMessage.ErrorMessage.Returns("Test error");

        _messageFactory.Create(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<DateTime>(),
            Arg.Any<DateTime>(),
            Arg.Any<DateTime?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>())
            .Returns(createdMessage);

        // Act
        await _orchestrator.AddAsync(
            new TestRequest("test"),
            EncinaError.New("Test error"),
            null,
            "Test",
            1,
            DateTime.UtcNow);

        // Assert
        callbackInvoked.ShouldBeTrue();
        receivedMessage.ShouldBe(createdMessage);
    }

    [Fact]
    public async Task AddAsync_SetsExpirationBasedOnRetentionPeriod()
    {
        // Arrange
        _options.RetentionPeriod = TimeSpan.FromDays(7);

        var createdMessage = Substitute.For<IDeadLetterMessage>();
        createdMessage.Id.Returns(Guid.NewGuid());
        createdMessage.RequestType.Returns("TestRequest");
        createdMessage.ErrorMessage.Returns("Test error");

        DateTime? capturedExpiresAt = null;
        _messageFactory.Create(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<DateTime>(),
            Arg.Any<DateTime>(),
            Arg.Do<DateTime?>(x => capturedExpiresAt = x),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>())
            .Returns(createdMessage);

        // Act
        await _orchestrator.AddAsync(
            new TestRequest("test"),
            EncinaError.New("Test error"),
            null,
            "Test",
            1,
            DateTime.UtcNow);

        // Assert
        capturedExpiresAt.ShouldNotBeNull();
        capturedExpiresAt.Value.ShouldBeInRange(
            DateTime.UtcNow.AddDays(6),
            DateTime.UtcNow.AddDays(8));
    }

    [Fact]
    public async Task GetAsync_DelegatesToStore()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = Substitute.For<IDeadLetterMessage>();
        _store.GetAsync(messageId, Arg.Any<CancellationToken>()).Returns(message);

        // Act
        var result = await _orchestrator.GetAsync(messageId);

        // Assert
        result.ShouldBe(message);
        await _store.Received(1).GetAsync(messageId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPendingCountAsync_CallsStoreWithCorrectFilter()
    {
        // Arrange
        _store.GetCountAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<CancellationToken>())
            .Returns(42);

        // Act
        var result = await _orchestrator.GetPendingCountAsync();

        // Assert
        result.ShouldBe(42);
        await _store.Received(1).GetCountAsync(
            Arg.Is<DeadLetterFilter>(f => f.ExcludeReplayed == true),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CleanupExpiredAsync_DelegatesToStore()
    {
        // Arrange
        _store.DeleteExpiredAsync(Arg.Any<CancellationToken>()).Returns(5);

        // Act
        var result = await _orchestrator.CleanupExpiredAsync();

        // Assert
        result.ShouldBe(5);
        await _store.Received(1).DeleteExpiredAsync(Arg.Any<CancellationToken>());
    }

    private sealed record TestRequest(string Value);
}

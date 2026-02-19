using Encina.Messaging.DeadLetter;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

namespace Encina.UnitTests.Messaging.DeadLetter;

/// <summary>
/// Unit tests for <see cref="DeadLetterManager"/>.
/// </summary>
public sealed class DeadLetterManagerTests
{
    #region Constructor

    [Fact]
    public void Constructor_WithNullStore_ThrowsArgumentNullException()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var serviceProvider = Substitute.For<IServiceProvider>();
        var logger = NullLogger<DeadLetterManager>.Instance;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new DeadLetterManager(null!, orchestrator, serviceProvider, logger));
    }

    [Fact]
    public void Constructor_WithNullOrchestrator_ThrowsArgumentNullException()
    {
        // Arrange
        var store = Substitute.For<IDeadLetterStore>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        var logger = NullLogger<DeadLetterManager>.Instance;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new DeadLetterManager(store, null!, serviceProvider, logger));
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var store = Substitute.For<IDeadLetterStore>();
        var orchestrator = CreateOrchestrator();
        var logger = NullLogger<DeadLetterManager>.Instance;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new DeadLetterManager(store, orchestrator, null!, logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var store = Substitute.For<IDeadLetterStore>();
        var orchestrator = CreateOrchestrator();
        var serviceProvider = Substitute.For<IServiceProvider>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new DeadLetterManager(store, orchestrator, serviceProvider, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_Succeeds()
    {
        // Arrange & Act
        var (manager, _, _, _) = CreateManager();

        // Assert
        manager.ShouldNotBeNull();
    }

    #endregion

    #region ReplayAsync

    [Fact]
    public async Task ReplayAsync_WhenMessageNotFound_ReturnsError()
    {
        // Arrange
        var (manager, store, _, _) = CreateManager();
        var messageId = Guid.NewGuid();

        store.GetAsync(messageId, Arg.Any<CancellationToken>())
            .Returns((IDeadLetterMessage?)null);

        // Act
        var result = await manager.ReplayAsync(messageId);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: e => e.Message.ShouldContain("not found"));
    }

    [Fact]
    public async Task ReplayAsync_WhenAlreadyReplayed_ReturnsError()
    {
        // Arrange
        var (manager, store, _, _) = CreateManager();
        var messageId = Guid.NewGuid();

        var message = CreateMockMessage(messageId, isReplayed: true);
        store.GetAsync(messageId, Arg.Any<CancellationToken>())
            .Returns(message);

        // Act
        var result = await manager.ReplayAsync(messageId);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: e => e.Message.ShouldContain("already been replayed"));
    }

    [Fact]
    public async Task ReplayAsync_WhenExpired_ReturnsError()
    {
        // Arrange
        var (manager, store, _, _) = CreateManager();
        var messageId = Guid.NewGuid();

        var message = CreateMockMessage(messageId, isExpired: true);
        store.GetAsync(messageId, Arg.Any<CancellationToken>())
            .Returns(message);

        // Act
        var result = await manager.ReplayAsync(messageId);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: e => e.Message.ShouldContain("expired"));
    }

    [Fact]
    public async Task ReplayAsync_WhenUnknownRequestType_ReturnsError()
    {
        // Arrange
        var (manager, store, _, _) = CreateManager();
        var messageId = Guid.NewGuid();

        var message = CreateMockMessage(messageId, requestType: "NonExistent.Type, NonExistent.Assembly");
        store.GetAsync(messageId, Arg.Any<CancellationToken>())
            .Returns(message);

        // Act
        var result = await manager.ReplayAsync(messageId);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: e => e.Message.ShouldContain("Cannot resolve type"));
    }

    #endregion

    #region GetMessageAsync

    [Fact]
    public async Task GetMessageAsync_DelegatesToStore()
    {
        // Arrange
        var (manager, store, _, _) = CreateManager();
        var messageId = Guid.NewGuid();
        var message = CreateMockMessage(messageId);

        store.GetAsync(messageId, Arg.Any<CancellationToken>())
            .Returns(message);

        // Act
        var result = await manager.GetMessageAsync(messageId);

        // Assert
        result.ShouldBe(message);
        await store.Received(1).GetAsync(messageId, Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetMessagesAsync

    [Fact]
    public async Task GetMessagesAsync_DelegatesToStore()
    {
        // Arrange
        var (manager, store, _, _) = CreateManager();
        var filter = new DeadLetterFilter();
        var messages = new List<IDeadLetterMessage>();

        store.GetMessagesAsync(filter, 0, 100, Arg.Any<CancellationToken>())
            .Returns(messages);

        // Act
        var result = await manager.GetMessagesAsync(filter, 0, 100);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r => r.ShouldBe(messages),
            Left: _ => Assert.Fail("Expected Right"));
        await store.Received(1).GetMessagesAsync(filter, 0, 100, Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetCountAsync

    [Fact]
    public async Task GetCountAsync_DelegatesToStore()
    {
        // Arrange
        var (manager, store, _, _) = CreateManager();
        var filter = new DeadLetterFilter();

        store.GetCountAsync(filter, Arg.Any<CancellationToken>())
            .Returns(42);

        // Act
        var result = await manager.GetCountAsync(filter);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r => r.ShouldBe(42),
            Left: _ => Assert.Fail("Expected Right"));
        await store.Received(1).GetCountAsync(filter, Arg.Any<CancellationToken>());
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_DelegatesToStore()
    {
        // Arrange
        var (manager, store, _, _) = CreateManager();
        var messageId = Guid.NewGuid();

        store.DeleteAsync(messageId, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await manager.DeleteAsync(messageId);

        // Assert
        result.IsRight.ShouldBeTrue();
        await store.Received(1).DeleteAsync(messageId, Arg.Any<CancellationToken>());
    }

    #endregion

    #region DeleteAllAsync

    [Fact]
    public async Task DeleteAllAsync_WithNullFilter_ThrowsArgumentNullException()
    {
        // Arrange
        var (manager, _, _, _) = CreateManager();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
        {
            await manager.DeleteAllAsync(null!);
        });
    }

    [Fact]
    public async Task DeleteAllAsync_DeletesMatchingMessages()
    {
        // Arrange
        var (manager, store, _, _) = CreateManager();
        var filter = new DeadLetterFilter { RequestType = "TestType" };

        var message1 = CreateMockMessage(Guid.NewGuid());
        var message2 = CreateMockMessage(Guid.NewGuid());
        var messages = new List<IDeadLetterMessage> { message1, message2 };

        store.GetMessagesAsync(filter, 0, int.MaxValue, Arg.Any<CancellationToken>())
            .Returns(messages);
        store.DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await manager.DeleteAllAsync(filter);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r => r.ShouldBe(2),
            Left: _ => Assert.Fail("Expected Right"));
        await store.Received(2).DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAllAsync_WhenNoMatches_ReturnsZero()
    {
        // Arrange
        var (manager, store, _, _) = CreateManager();
        var filter = new DeadLetterFilter();

        store.GetMessagesAsync(filter, 0, int.MaxValue, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<IDeadLetterMessage>());

        // Act
        var result = await manager.DeleteAllAsync(filter);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r => r.ShouldBe(0),
            Left: _ => Assert.Fail("Expected Right"));
        await store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region CleanupExpiredAsync

    [Fact]
    public async Task CleanupExpiredAsync_DelegatesToOrchestrator()
    {
        // Arrange
        // DeadLetterOrchestrator is a sealed class - we test through the real orchestrator
        // which delegates to the mocked store
        var (manager, store, _, _) = CreateManager();

        store.GetMessagesAsync(
            Arg.Any<DeadLetterFilter>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>())
            .Returns(Array.Empty<IDeadLetterMessage>());

        // Act
        var result = await manager.CleanupExpiredAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r => r.ShouldBe(0),
            Left: _ => Assert.Fail("Expected Right"));
    }

    #endregion

    #region GetStatisticsAsync

    [Fact]
    public async Task GetStatisticsAsync_ReturnsStatisticsFromOrchestrator()
    {
        // Arrange
        // DeadLetterOrchestrator is a sealed class - we test through the real orchestrator
        // which queries the mocked store
        var (manager, store, _, _) = CreateManager();

        // Total count uses null filter
        store.GetCountAsync(null, Arg.Any<CancellationToken>())
            .Returns(10);
        // Other counts use various filter combinations
        store.GetCountAsync(Arg.Is<DeadLetterFilter>(f => f != null), Arg.Any<CancellationToken>())
            .Returns(5);

        // Act
        var result = await manager.GetStatisticsAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r => r.TotalCount.ShouldBe(10),
            Left: _ => Assert.Fail("Expected Right"));
    }

    #endregion

    #region ReplayAllAsync

    [Fact]
    public async Task ReplayAllAsync_WithNullFilter_ThrowsArgumentNullException()
    {
        // Arrange
        var (manager, _, _, _) = CreateManager();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
        {
            await manager.ReplayAllAsync(null!);
        });
    }

    [Fact]
    public async Task ReplayAllAsync_WhenNoMessages_ReturnsEmptyResult()
    {
        // Arrange
        var (manager, store, _, _) = CreateManager();
        var filter = new DeadLetterFilter();

        store.GetMessagesAsync(filter, 0, 100, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<IDeadLetterMessage>());

        // Act
        var result = await manager.ReplayAllAsync(filter);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r =>
            {
                r.TotalProcessed.ShouldBe(0);
                r.SuccessCount.ShouldBe(0);
                r.FailureCount.ShouldBe(0);
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region Helper Methods

    private static (DeadLetterManager Manager, IDeadLetterStore Store, DeadLetterOrchestrator Orchestrator, IServiceProvider ServiceProvider) CreateManager()
    {
        var store = Substitute.For<IDeadLetterStore>();
        var orchestrator = CreateOrchestrator(store);
        var serviceProvider = Substitute.For<IServiceProvider>();
        var logger = NullLogger<DeadLetterManager>.Instance;

        var manager = new DeadLetterManager(store, orchestrator, serviceProvider, logger);

        return (manager, store, orchestrator, serviceProvider);
    }

    private static DeadLetterOrchestrator CreateOrchestrator(IDeadLetterStore? store = null)
    {
        store ??= Substitute.For<IDeadLetterStore>();
        var messageFactory = Substitute.For<IDeadLetterMessageFactory>();
        var options = new DeadLetterOptions();
        var logger = NullLogger<DeadLetterOrchestrator>.Instance;

        return new DeadLetterOrchestrator(store, messageFactory, options, logger);
    }

    private static IDeadLetterMessage CreateMockMessage(
        Guid messageId,
        bool isReplayed = false,
        bool isExpired = false,
        string requestType = "System.Object, System.Runtime")
    {
        var message = Substitute.For<IDeadLetterMessage>();
        message.Id.Returns(messageId);
        message.IsReplayed.Returns(isReplayed);
        message.IsExpired.Returns(isExpired);
        message.RequestType.Returns(requestType);
        message.RequestContent.Returns("{}");
        message.ErrorMessage.Returns("Test error");
        message.CorrelationId.Returns("test-correlation");
        message.DeadLetteredAtUtc.Returns(DateTime.UtcNow);
        return message;
    }

    #endregion
}

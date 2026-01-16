using System.Text.Json;
using Encina.Messaging.Outbox;
using Microsoft.Extensions.Logging;

namespace Encina.UnitTests.Messaging.Outbox;

/// <summary>
/// Unit tests for OutboxOrchestrator.
/// </summary>
public sealed class OutboxOrchestratorTests
{
    private static readonly DateTime FixedUtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    #region Test Fixture

    private static TestFixture CreateTestFixture()
    {
        var store = Substitute.For<IOutboxStore>();
        var options = new OutboxOptions
        {
            BatchSize = 10,
            MaxRetries = 3,
            BaseRetryDelay = TimeSpan.FromSeconds(5)
        };
        var logger = Substitute.For<ILogger<OutboxOrchestrator>>();
        var messageFactory = Substitute.For<IOutboxMessageFactory>();
        var orchestrator = new OutboxOrchestrator(store, options, logger, messageFactory);

        return new TestFixture(store, options, logger, messageFactory, orchestrator);
    }

    private sealed record TestFixture(
        IOutboxStore Store,
        OutboxOptions Options,
        ILogger<OutboxOrchestrator> Logger,
        IOutboxMessageFactory MessageFactory,
        OutboxOrchestrator Orchestrator);

    #endregion

    #region Constructor Tests

    public static TheoryData<IOutboxStore?, OutboxOptions?, ILogger<OutboxOrchestrator>?, IOutboxMessageFactory?, string> ConstructorNullArgumentTestCases()
    {
        // Each row creates fresh instances to avoid shared state between theory cases
        return new TheoryData<IOutboxStore?, OutboxOptions?, ILogger<OutboxOrchestrator>?, IOutboxMessageFactory?, string>
        {
            {
                null,
                new OutboxOptions { BatchSize = 10, MaxRetries = 3 },
                Substitute.For<ILogger<OutboxOrchestrator>>(),
                Substitute.For<IOutboxMessageFactory>(),
                "store"
            },
            {
                Substitute.For<IOutboxStore>(),
                null,
                Substitute.For<ILogger<OutboxOrchestrator>>(),
                Substitute.For<IOutboxMessageFactory>(),
                "options"
            },
            {
                Substitute.For<IOutboxStore>(),
                new OutboxOptions { BatchSize = 10, MaxRetries = 3 },
                null,
                Substitute.For<IOutboxMessageFactory>(),
                "logger"
            },
            {
                Substitute.For<IOutboxStore>(),
                new OutboxOptions { BatchSize = 10, MaxRetries = 3 },
                Substitute.For<ILogger<OutboxOrchestrator>>(),
                null,
                "messageFactory"
            }
        };
    }

    [Theory]
    [MemberData(nameof(ConstructorNullArgumentTestCases))]
    public void Constructor_NullArgument_ThrowsArgumentNullException(
        IOutboxStore? store,
        OutboxOptions? options,
        ILogger<OutboxOrchestrator>? logger,
        IOutboxMessageFactory? messageFactory,
        string expectedParamName)
    {
        var act = () => new OutboxOrchestrator(
            store!,
            options!,
            logger!,
            messageFactory!);

        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe(expectedParamName);
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_ValidNotification_CreatesAndStoresMessage()
    {
        // Arrange
        var fixture = CreateTestFixture();
        var notification = new TestNotification { Id = Guid.NewGuid(), Message = "Test" };
        var expectedMessage = CreateTestOutboxMessage(Guid.NewGuid());

        fixture.MessageFactory.Create(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<DateTime>())
            .Returns(expectedMessage);

        // Act
        await fixture.Orchestrator.AddAsync(notification);

        // Assert
        await fixture.Store.Received(1).AddAsync(expectedMessage, Arg.Any<CancellationToken>());
        fixture.MessageFactory.Received(1).Create(
            Arg.Any<Guid>(),
            Arg.Is<string>(s => s == typeof(TestNotification).AssemblyQualifiedName),
            Arg.Any<string>(),
            Arg.Any<DateTime>());
    }

    [Fact]
    public async Task AddAsync_NullNotification_ThrowsArgumentNullException()
    {
        // Arrange
        var fixture = CreateTestFixture();

        // Act
        var act = async () => await fixture.Orchestrator.AddAsync<TestNotification>(null!);

        // Assert
        await act.ShouldThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region ProcessPendingMessagesAsync Tests

    [Fact]
    public async Task ProcessPendingMessagesAsync_NoMessages_ReturnsZero()
    {
        // Arrange
        var fixture = CreateTestFixture();
        fixture.Store.GetPendingMessagesAsync(
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>())
            .Returns([]);

        // Act
        var result = await fixture.Orchestrator.ProcessPendingMessagesAsync(
            (msg, type, obj) => Task.CompletedTask);

        // Assert
        result.ShouldBe(0);
    }

    [Fact]
    public async Task ProcessPendingMessagesAsync_WithMessages_ProcessesAndReturnsCount()
    {
        // Arrange
        var fixture = CreateTestFixture();
        var notification = new TestNotification { Id = Guid.NewGuid(), Message = "Test" };
        var notificationType = typeof(TestNotification).AssemblyQualifiedName!;
        var message = CreateTestOutboxMessage(Guid.NewGuid(), notificationType, notification);

        fixture.Store.GetPendingMessagesAsync(
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>())
            .Returns([message]);

        var publishedMessages = new List<object>();
        Func<IOutboxMessage, Type, object, Task> callback = (msg, type, obj) =>
        {
            publishedMessages.Add(obj);
            return Task.CompletedTask;
        };

        // Act
        var result = await fixture.Orchestrator.ProcessPendingMessagesAsync(callback);

        // Assert
        result.ShouldBe(1);
        publishedMessages.Count.ShouldBe(1);
        await fixture.Store.Received(1).MarkAsProcessedAsync(message.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessPendingMessagesAsync_UnknownType_MarksAsFailed()
    {
        // Arrange
        var fixture = CreateTestFixture();
        var message = CreateTestOutboxMessage(Guid.NewGuid(), "Unknown.Type, Unknown.Assembly");

        fixture.Store.GetPendingMessagesAsync(
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>())
            .Returns([message]);

        // Act
        var result = await fixture.Orchestrator.ProcessPendingMessagesAsync(
            (msg, type, obj) => Task.CompletedTask);

        // Assert
        result.ShouldBe(0);
        await fixture.Store.Received(1).MarkAsFailedAsync(
            message.Id,
            Arg.Is<string>(s => s.Contains("Unknown notification type")),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessPendingMessagesAsync_PublishThrows_MarksAsFailed()
    {
        // Arrange
        var fixture = CreateTestFixture();
        var notification = new TestNotification { Id = Guid.NewGuid(), Message = "Test" };
        var notificationType = typeof(TestNotification).AssemblyQualifiedName!;
        var message = CreateTestOutboxMessage(Guid.NewGuid(), notificationType, notification);

        fixture.Store.GetPendingMessagesAsync(
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>())
            .Returns([message]);

        Func<IOutboxMessage, Type, object, Task> callback = (msg, type, obj) =>
            Task.FromException(new InvalidOperationException("Publish failed"));

        // Act
        var result = await fixture.Orchestrator.ProcessPendingMessagesAsync(callback);

        // Assert
        result.ShouldBe(0);
        await fixture.Store.Received(1).MarkAsFailedAsync(
            message.Id,
            Arg.Is<string>(s => s.Contains("Publish failed")),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessPendingMessagesAsync_NullCallback_ThrowsArgumentNullException()
    {
        // Arrange
        var fixture = CreateTestFixture();

        // Act
        var act = async () => await fixture.Orchestrator.ProcessPendingMessagesAsync(null!);

        // Assert
        await act.ShouldThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ProcessPendingMessagesAsync_CancellationRequested_StopsProcessing()
    {
        // Arrange
        var fixture = CreateTestFixture();
        var notification = new TestNotification { Id = Guid.NewGuid(), Message = "Test" };
        var notificationType = typeof(TestNotification).AssemblyQualifiedName!;
        var message1 = CreateTestOutboxMessage(Guid.NewGuid(), notificationType, notification);
        var message2 = CreateTestOutboxMessage(Guid.NewGuid(), notificationType, notification);

        fixture.Store.GetPendingMessagesAsync(
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>())
            .Returns([message1, message2]);

        using var cts = new CancellationTokenSource();
        var processedCount = 0;

        Func<IOutboxMessage, Type, object, Task> callback = async (msg, type, obj) =>
        {
            processedCount++;
            if (processedCount == 1)
            {
                await cts.CancelAsync();
            }
            await Task.CompletedTask;
        };

        // Act
        var result = await fixture.Orchestrator.ProcessPendingMessagesAsync(callback, cts.Token);

        // Assert
        result.ShouldBe(1);
    }

    #endregion

    #region GetPendingCountAsync Tests

    [Fact]
    public async Task GetPendingCountAsync_NoMessages_ReturnsZero()
    {
        // Arrange
        var fixture = CreateTestFixture();
        fixture.Store.GetPendingMessagesAsync(
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>())
            .Returns([]);

        // Act
        var result = await fixture.Orchestrator.GetPendingCountAsync();

        // Assert
        result.ShouldBe(0);
    }

    [Fact]
    public async Task GetPendingCountAsync_WithMessages_ReturnsCount()
    {
        // Arrange
        var fixture = CreateTestFixture();
        var messages = new[]
        {
            CreateTestOutboxMessage(Guid.NewGuid()),
            CreateTestOutboxMessage(Guid.NewGuid()),
            CreateTestOutboxMessage(Guid.NewGuid())
        };

        fixture.Store.GetPendingMessagesAsync(
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>())
            .Returns(messages);

        // Act
        var result = await fixture.Orchestrator.GetPendingCountAsync();

        // Assert
        result.ShouldBe(3);
    }

    #endregion

    #region Helpers

    private static TestOutboxMessage CreateTestOutboxMessage(
        Guid id,
        string? notificationType = null,
        TestNotification? notification = null)
    {
        return new TestOutboxMessage
        {
            Id = id,
            NotificationType = notificationType ?? typeof(TestNotification).AssemblyQualifiedName!,
            Content = notification != null
                ? JsonSerializer.Serialize(notification, JsonOptions)
                : "{}",
            CreatedAtUtc = FixedUtcNow
        };
    }

    #endregion
}

/// <summary>
/// Test notification for unit tests.
/// </summary>
internal sealed class TestNotification
{
    public Guid Id { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Test implementation of IOutboxMessage for unit tests.
/// </summary>
internal sealed class TestOutboxMessage : IOutboxMessage
{
    public Guid Id { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextRetryAtUtc { get; set; }
    public string? ErrorMessage { get; set; }

    public bool IsProcessed => ProcessedAtUtc.HasValue;
    public bool IsDeadLettered(int maxRetries) => RetryCount >= maxRetries && !IsProcessed;
}

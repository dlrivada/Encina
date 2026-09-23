using System.Diagnostics.Metrics;
using System.Text.Json;
using Encina.Messaging.Outbox;
using Encina.Messaging.Serialization;
using Encina.Testing.Shouldly;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

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
        var messageSerializer = new JsonMessageSerializer();
        var orchestrator = new OutboxOrchestrator(store, options, logger, messageFactory, messageSerializer);

        return new TestFixture(store, options, logger, messageFactory, messageSerializer, orchestrator);
    }

    private sealed record TestFixture(
        IOutboxStore Store,
        OutboxOptions Options,
        ILogger<OutboxOrchestrator> Logger,
        IOutboxMessageFactory MessageFactory,
        IMessageSerializer MessageSerializer,
        OutboxOrchestrator Orchestrator);

    #endregion

    #region Constructor Tests

    public static TheoryData<IOutboxStore?, OutboxOptions?, ILogger<OutboxOrchestrator>?, IOutboxMessageFactory?, IMessageSerializer?, string> ConstructorNullArgumentTestCases()
    {
        // Each row creates fresh instances to avoid shared state between theory cases
        return new TheoryData<IOutboxStore?, OutboxOptions?, ILogger<OutboxOrchestrator>?, IOutboxMessageFactory?, IMessageSerializer?, string>
        {
            {
                null,
                new OutboxOptions { BatchSize = 10, MaxRetries = 3 },
                Substitute.For<ILogger<OutboxOrchestrator>>(),
                Substitute.For<IOutboxMessageFactory>(),
                new JsonMessageSerializer(),
                "store"
            },
            {
                Substitute.For<IOutboxStore>(),
                null,
                Substitute.For<ILogger<OutboxOrchestrator>>(),
                Substitute.For<IOutboxMessageFactory>(),
                new JsonMessageSerializer(),
                "options"
            },
            {
                Substitute.For<IOutboxStore>(),
                new OutboxOptions { BatchSize = 10, MaxRetries = 3 },
                null,
                Substitute.For<IOutboxMessageFactory>(),
                new JsonMessageSerializer(),
                "logger"
            },
            {
                Substitute.For<IOutboxStore>(),
                new OutboxOptions { BatchSize = 10, MaxRetries = 3 },
                Substitute.For<ILogger<OutboxOrchestrator>>(),
                null,
                new JsonMessageSerializer(),
                "messageFactory"
            },
            {
                Substitute.For<IOutboxStore>(),
                new OutboxOptions { BatchSize = 10, MaxRetries = 3 },
                Substitute.For<ILogger<OutboxOrchestrator>>(),
                Substitute.For<IOutboxMessageFactory>(),
                null,
                "messageSerializer"
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
        IMessageSerializer? messageSerializer,
        string expectedParamName)
    {
        var act = () => new OutboxOrchestrator(
            store!,
            options!,
            logger!,
            messageFactory!,
            messageSerializer!);

        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe(expectedParamName);
    }

    [Fact]
    public void Constructor_MaxRetryDelayBelowBaseRetryDelay_ThrowsArgumentException()
    {
        var options = new OutboxOptions
        {
            BaseRetryDelay = TimeSpan.FromMinutes(20),
            MaxRetryDelay = TimeSpan.FromMinutes(10)
        };

        var act = () => new OutboxOrchestrator(
            Substitute.For<IOutboxStore>(),
            options,
            Substitute.For<ILogger<OutboxOrchestrator>>(),
            Substitute.For<IOutboxMessageFactory>(),
            new JsonMessageSerializer());

        act.ShouldThrow<ArgumentException>().ParamName.ShouldBe("options");
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
        var result = await fixture.Orchestrator.AddAsync(notification);

        // Assert
        result.IsRight.ShouldBeTrue();
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
            .Returns(Right<EncinaError, IEnumerable<IOutboxMessage>>(Enumerable.Empty<IOutboxMessage>()));

        // Act
        var result = await fixture.Orchestrator.ProcessPendingMessagesAsync(
            (msg, type, obj) => Delivered());

        // Assert
        result.IsRight.ShouldBeTrue();
        result.RightAsEnumerable().First().ShouldBe(0);
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
            .Returns(Right<EncinaError, IEnumerable<IOutboxMessage>>(new List<IOutboxMessage> { message }));

        var publishedMessages = new List<object>();
        Func<IOutboxMessage, Type, object, ValueTask<Either<EncinaError, Unit>>> callback = (msg, type, obj) =>
        {
            publishedMessages.Add(obj);
            return Delivered();
        };

        // Act
        var result = await fixture.Orchestrator.ProcessPendingMessagesAsync(callback);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.RightAsEnumerable().First().ShouldBe(1);
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
            .Returns(Right<EncinaError, IEnumerable<IOutboxMessage>>(new List<IOutboxMessage> { message }));

        // Act
        var result = await fixture.Orchestrator.ProcessPendingMessagesAsync(
            (msg, type, obj) => Delivered());

        // Assert
        result.IsRight.ShouldBeTrue();
        result.RightAsEnumerable().First().ShouldBe(0);
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
            .Returns(Right<EncinaError, IEnumerable<IOutboxMessage>>(new List<IOutboxMessage> { message }));

        Func<IOutboxMessage, Type, object, ValueTask<Either<EncinaError, Unit>>> callback = (msg, type, obj) =>
            ValueTask.FromException<Either<EncinaError, Unit>>(new InvalidOperationException("Publish failed"));

        // Act
        var result = await fixture.Orchestrator.ProcessPendingMessagesAsync(callback);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.RightAsEnumerable().First().ShouldBe(0);
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
            .Returns(Right<EncinaError, IEnumerable<IOutboxMessage>>(new List<IOutboxMessage> { message1, message2 }));

        using var cts = new CancellationTokenSource();
        var processedCount = 0;

        Func<IOutboxMessage, Type, object, ValueTask<Either<EncinaError, Unit>>> callback = async (msg, type, obj) =>
        {
            processedCount++;
            if (processedCount == 1)
            {
                await cts.CancelAsync();
            }
            return Unit.Default;
        };

        // Act
        var result = await fixture.Orchestrator.ProcessPendingMessagesAsync(callback, cts.Token);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.RightAsEnumerable().First().ShouldBe(1);
    }

    #endregion

    #region GetPendingCountAsync Tests

    [Fact]
    public async Task GetPendingCountAsync_UsesStoreCountWithMaxRetries()
    {
        // Arrange
        var fixture = CreateTestFixture();
        fixture.Store.GetPendingCountAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(1234));

        // Act
        var result = await fixture.Orchestrator.GetPendingCountAsync();

        // Assert - an exact count, not the size of a fetched batch
        result.ShouldBeRight().ShouldBe(1234);
        await fixture.Store.Received(1).GetPendingCountAsync(fixture.Options.MaxRetries, Arg.Any<CancellationToken>());
        await fixture.Store.DidNotReceive().GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPendingCountAsync_StoreFails_ReturnsLeft()
    {
        // Arrange
        var fixture = CreateTestFixture();
        fixture.Store.GetPendingCountAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, int>(EncinaErrors.Create("outbox.get_pending_count_failed", "boom")));

        // Act
        var result = await fixture.Orchestrator.GetPendingCountAsync();

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region GetExhaustedCountAsync Tests

    [Fact]
    public async Task GetExhaustedCountAsync_UsesStoreCountWithMaxRetries()
    {
        // Arrange
        var fixture = CreateTestFixture();
        fixture.Store.GetExhaustedCountAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(4));

        // Act
        var result = await fixture.Orchestrator.GetExhaustedCountAsync();

        // Assert
        result.ShouldBeRight().ShouldBe(4);
        await fixture.Store.Received(1).GetExhaustedCountAsync(fixture.Options.MaxRetries, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetExhaustedCountAsync_StoreFails_ReturnsLeft()
    {
        // Arrange
        var fixture = CreateTestFixture();
        fixture.Store.GetExhaustedCountAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, int>(EncinaErrors.Create("outbox.get_exhausted_count_failed", "boom")));

        // Act
        var result = await fixture.Orchestrator.GetExhaustedCountAsync();

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region RequeueExhaustedAsync Tests

    [Fact]
    public async Task RequeueExhaustedAsync_All_RequeuesEveryExhaustedMessageAndSaves()
    {
        // Arrange
        var fixture = CreateTestFixture();
        fixture.Store.RequeueExhaustedAsync(Arg.Any<int>(), Arg.Any<IReadOnlyCollection<Guid>?>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(3));
        fixture.Store.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await fixture.Orchestrator.RequeueExhaustedAsync();

        // Assert
        result.ShouldBeRight().ShouldBe(3);
        await fixture.Store.Received(1).RequeueExhaustedAsync(fixture.Options.MaxRetries, null, Arg.Any<CancellationToken>());
        await fixture.Store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequeueExhaustedAsync_ByIds_PassesDistinctNonEmptyIds()
    {
        // Arrange
        var fixture = CreateTestFixture();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        IReadOnlyCollection<Guid>? passedIds = null;
        fixture.Store.RequeueExhaustedAsync(Arg.Any<int>(), Arg.Do<IReadOnlyCollection<Guid>?>(ids => passedIds = ids), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(2));
        fixture.Store.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await fixture.Orchestrator.RequeueExhaustedAsync([id1, id2, id1, Guid.Empty]);

        // Assert
        result.ShouldBeRight().ShouldBe(2);
        passedIds.ShouldNotBeNull();
        passedIds.ShouldBe([id1, id2], ignoreOrder: true);
    }

    [Fact]
    public async Task RequeueExhaustedAsync_NoUsableIds_ReturnsZeroWithoutCallingStore()
    {
        // Arrange
        var fixture = CreateTestFixture();

        // Act
        var result = await fixture.Orchestrator.RequeueExhaustedAsync([Guid.Empty]);

        // Assert
        result.ShouldBeRight().ShouldBe(0);
        await fixture.Store.DidNotReceive().RequeueExhaustedAsync(Arg.Any<int>(), Arg.Any<IReadOnlyCollection<Guid>?>(), Arg.Any<CancellationToken>());
        await fixture.Store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequeueExhaustedAsync_StoreFails_ReturnsLeftWithoutSaving()
    {
        // Arrange
        var fixture = CreateTestFixture();
        fixture.Store.RequeueExhaustedAsync(Arg.Any<int>(), Arg.Any<IReadOnlyCollection<Guid>?>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, int>(EncinaErrors.Create("outbox.requeue_exhausted_failed", "boom")));

        // Act
        var result = await fixture.Orchestrator.RequeueExhaustedAsync();

        // Assert
        result.IsLeft.ShouldBeTrue();
        await fixture.Store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequeueExhaustedAsync_SaveFails_ReturnsLeft()
    {
        // Arrange
        var fixture = CreateTestFixture();
        fixture.Store.RequeueExhaustedAsync(Arg.Any<int>(), Arg.Any<IReadOnlyCollection<Guid>?>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(1));
        fixture.Store.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(EncinaErrors.Create("outbox.save_failed", "save failed")));

        // Act
        var result = await fixture.Orchestrator.RequeueExhaustedAsync();

        // Assert
        result.ShouldBeLeft().Message.ShouldContain("save failed");
    }

    [Fact]
    public async Task RequeueExhaustedAsync_Success_LogsWithEventId2959()
    {
        // Arrange
        var fixture = CreateTestFixture();
        fixture.Logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        fixture.Store.RequeueExhaustedAsync(Arg.Any<int>(), Arg.Any<IReadOnlyCollection<Guid>?>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(2));
        fixture.Store.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        await fixture.Orchestrator.RequeueExhaustedAsync();

        // Assert
        fixture.Logger.Received(1).Log(
            LogLevel.Information,
            Arg.Is<EventId>(e => e.Id == 2959),
            Arg.Any<Arg.AnyType>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<Arg.AnyType, Exception?, string>>());
    }

    [Fact]
    public async Task RequeueExhaustedAsync_Success_RecordsRequeuedMetric()
    {
        // Arrange
        var fixture = CreateTestFixture();
        fixture.Store.RequeueExhaustedAsync(Arg.Any<int>(), Arg.Any<IReadOnlyCollection<Guid>?>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(7));
        fixture.Store.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        var measurements = new List<long>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == "Encina" && instrument.Name == "encina.outbox.messages_requeued_total")
            {
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, value, _, _) =>
        {
            lock (measurements)
            {
                measurements.Add(value);
            }
        });
        listener.Start();

        // Act
        await fixture.Orchestrator.RequeueExhaustedAsync();

        // Assert
        lock (measurements)
        {
            measurements.ShouldContain(7);
        }
    }

    #endregion

    #region Helpers

    private static ValueTask<Either<EncinaError, Unit>> Delivered()
        => ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));

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

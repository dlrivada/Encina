using Encina.Messaging.Scheduling;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Messaging.Scheduling;

/// <summary>
/// Unit tests for <see cref="SchedulerOrchestrator"/>.
/// </summary>
public sealed class SchedulerOrchestratorTests
{
    private sealed record TestRequest
    {
        public int Value { get; init; }
    }

    #region Constructor

    [Fact]
    public void Constructor_WithNullStore_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new SchedulingOptions();
        var logger = NullLogger<SchedulerOrchestrator>.Instance;
        var messageFactory = Substitute.For<IScheduledMessageFactory>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new SchedulerOrchestrator(null!, options, logger, messageFactory));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var store = Substitute.For<IScheduledMessageStore>();
        var logger = NullLogger<SchedulerOrchestrator>.Instance;
        var messageFactory = Substitute.For<IScheduledMessageFactory>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new SchedulerOrchestrator(store, null!, logger, messageFactory));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var store = Substitute.For<IScheduledMessageStore>();
        var options = new SchedulingOptions();
        var messageFactory = Substitute.For<IScheduledMessageFactory>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new SchedulerOrchestrator(store, options, null!, messageFactory));
    }

    [Fact]
    public void Constructor_WithNullMessageFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var store = Substitute.For<IScheduledMessageStore>();
        var options = new SchedulingOptions();
        var logger = NullLogger<SchedulerOrchestrator>.Instance;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new SchedulerOrchestrator(store, options, logger, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_Succeeds()
    {
        // Arrange & Act
        var orchestrator = CreateOrchestrator();

        // Assert
        orchestrator.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithOptionalCronParser_Succeeds()
    {
        // Arrange
        var store = Substitute.For<IScheduledMessageStore>();
        var options = new SchedulingOptions();
        var logger = NullLogger<SchedulerOrchestrator>.Instance;
        var messageFactory = Substitute.For<IScheduledMessageFactory>();
        var cronParser = Substitute.For<ICronParser>();

        // Act
        var orchestrator = new SchedulerOrchestrator(store, options, logger, messageFactory, cronParser);

        // Assert
        orchestrator.ShouldNotBeNull();
    }

    #endregion

    #region ScheduleAsync (DateTime)

    [Fact]
    public async Task ScheduleAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var executeAt = DateTime.UtcNow.AddHours(1);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => orchestrator.ScheduleAsync<TestRequest>(null!, executeAt));
    }

    [Fact]
    public async Task ScheduleAsync_WithPastTime_ThrowsArgumentException()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var request = new TestRequest { Value = 42 };
        var executeAt = DateTime.UtcNow.AddHours(-1); // Past time

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            () => orchestrator.ScheduleAsync(request, executeAt));
    }

    [Fact]
    public async Task ScheduleAsync_WithValidRequest_ReturnsMessageId()
    {
        // Arrange
        var (orchestrator, store, messageFactory) = CreateOrchestratorWithDependencies();
        var request = new TestRequest { Value = 42 };
        var executeAt = DateTime.UtcNow.AddHours(1);
        var messageId = Guid.NewGuid();

        var mockMessage = CreateMockMessage(messageId);
        messageFactory.Create(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<DateTime>(),
            Arg.Any<DateTime>(),
            Arg.Any<bool>(),
            Arg.Any<string?>())
            .Returns(mockMessage);

        // Act
        var result = await orchestrator.ScheduleAsync(request, executeAt);

        // Assert
        result.ShouldBe(messageId);
        await store.Received(1).AddAsync(mockMessage, Arg.Any<CancellationToken>());
    }

    #endregion

    #region ScheduleAsync (TimeSpan)

    [Fact]
    public async Task ScheduleAsync_WithZeroDelay_ThrowsArgumentException()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var request = new TestRequest { Value = 42 };

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            () => orchestrator.ScheduleAsync(request, TimeSpan.Zero));
    }

    [Fact]
    public async Task ScheduleAsync_WithNegativeDelay_ThrowsArgumentException()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var request = new TestRequest { Value = 42 };

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            () => orchestrator.ScheduleAsync(request, TimeSpan.FromSeconds(-5)));
    }

    [Fact]
    public async Task ScheduleAsync_WithValidDelay_ReturnsMessageId()
    {
        // Arrange
        var (orchestrator, store, messageFactory) = CreateOrchestratorWithDependencies();
        var request = new TestRequest { Value = 42 };
        var delay = TimeSpan.FromMinutes(5);
        var messageId = Guid.NewGuid();

        var mockMessage = CreateMockMessage(messageId);
        messageFactory.Create(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<DateTime>(),
            Arg.Any<DateTime>(),
            Arg.Any<bool>(),
            Arg.Any<string?>())
            .Returns(mockMessage);

        // Act
        var result = await orchestrator.ScheduleAsync(request, delay);

        // Assert
        result.ShouldBe(messageId);
        await store.Received(1).AddAsync(mockMessage, Arg.Any<CancellationToken>());
    }

    #endregion

    #region ScheduleRecurringAsync

    [Fact]
    public async Task ScheduleRecurringAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var orchestrator = CreateOrchestratorWithCronParser();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
        {
            await orchestrator.ScheduleRecurringAsync<TestRequest>(null!, "* * * * *");
        });
    }

    [Fact]
    public async Task ScheduleRecurringAsync_WithNullCronExpression_ThrowsArgumentException()
    {
        // Arrange
        var orchestrator = CreateOrchestratorWithCronParser();
        var request = new TestRequest { Value = 42 };

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
        {
            await orchestrator.ScheduleRecurringAsync(request, null!);
        });
    }

    [Fact]
    public async Task ScheduleRecurringAsync_WithEmptyCronExpression_ThrowsArgumentException()
    {
        // Arrange
        var orchestrator = CreateOrchestratorWithCronParser();
        var request = new TestRequest { Value = 42 };

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
        {
            await orchestrator.ScheduleRecurringAsync(request, "");
        });
    }

    [Fact]
    public async Task ScheduleRecurringAsync_WhenRecurringDisabled_ReturnsError()
    {
        // Arrange
        var options = new SchedulingOptions { EnableRecurringMessages = false };
        var orchestrator = CreateOrchestrator(options);
        var request = new TestRequest { Value = 42 };

        // Act
        var result = await orchestrator.ScheduleRecurringAsync(request, "* * * * *");

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.LeftAsEnumerable().First().GetCode().Match(
            code => code.ShouldBe(SchedulingErrorCodes.RecurringDisabled),
            () => throw new InvalidOperationException("Expected error code"));
    }

    [Fact]
    public async Task ScheduleRecurringAsync_WithNoCronParser_ReturnsError()
    {
        // Arrange
        var orchestrator = CreateOrchestrator(); // No cron parser
        var request = new TestRequest { Value = 42 };

        // Act
        var result = await orchestrator.ScheduleRecurringAsync(request, "* * * * *");

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.LeftAsEnumerable().First().GetCode().Match(
            code => code.ShouldBe(SchedulingErrorCodes.NoCronParser),
            () => throw new InvalidOperationException("Expected error code"));
    }

    [Fact]
    public async Task ScheduleRecurringAsync_WithInvalidCronExpression_ReturnsError()
    {
        // Arrange
        var (orchestrator, _, _, cronParser) = CreateOrchestratorWithCronParserAndDependencies();
        var request = new TestRequest { Value = 42 };
        var error = EncinaErrors.Create(SchedulingErrorCodes.InvalidCronExpression, "Invalid cron");

        cronParser.GetNextOccurrence(Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns(Left<EncinaError, DateTime>(error));

        // Act
        var result = await orchestrator.ScheduleRecurringAsync(request, "invalid cron");

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.LeftAsEnumerable().First().GetCode().Match(
            code => code.ShouldBe(SchedulingErrorCodes.InvalidCronExpression),
            () => throw new InvalidOperationException("Expected error code"));
    }

    [Fact]
    public async Task ScheduleRecurringAsync_WithValidCronExpression_ReturnsMessageId()
    {
        // Arrange
        var (orchestrator, store, messageFactory, cronParser) = CreateOrchestratorWithCronParserAndDependencies();
        var request = new TestRequest { Value = 42 };
        var messageId = Guid.NewGuid();
        var nextExecution = DateTime.UtcNow.AddMinutes(1);

        cronParser.GetNextOccurrence(Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns(Right<EncinaError, DateTime>(nextExecution));

        var mockMessage = CreateMockMessage(messageId, isRecurring: true, cronExpression: "* * * * *");
        messageFactory.Create(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<DateTime>(),
            Arg.Any<DateTime>(),
            Arg.Any<bool>(),
            Arg.Any<string?>())
            .Returns(mockMessage);

        // Act
        var result = await orchestrator.ScheduleRecurringAsync(request, "* * * * *");

        // Assert
        result.IsRight.ShouldBeTrue();
        var resultId = result.RightAsEnumerable().First();
        resultId.ShouldBe(messageId);
        await store.Received(1).AddAsync(mockMessage, Arg.Any<CancellationToken>());
    }

    #endregion

    #region CancelAsync

    [Fact]
    public async Task CancelAsync_CallsStore()
    {
        // Arrange
        var (orchestrator, store, _) = CreateOrchestratorWithDependencies();
        var messageId = Guid.NewGuid();

        // Act
        await orchestrator.CancelAsync(messageId);

        // Assert
        await store.Received(1).CancelAsync(messageId, Arg.Any<CancellationToken>());
    }

    #endregion

    #region ProcessDueMessagesAsync

    [Fact]
    public async Task ProcessDueMessagesAsync_WithNullCallback_ThrowsArgumentNullException()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => orchestrator.ProcessDueMessagesAsync(null!));
    }

    [Fact]
    public async Task ProcessDueMessagesAsync_WithNoMessages_ReturnsZero()
    {
        // Arrange
        var (orchestrator, store, _) = CreateOrchestratorWithDependencies();
        store.GetDueMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<IScheduledMessage>());

        // Act
        var result = await orchestrator.ProcessDueMessagesAsync((_, _, _) => Task.CompletedTask);

        // Assert
        result.ShouldBe(0);
    }

    [Fact]
    public async Task ProcessDueMessagesAsync_WithDueMessages_ExecutesCallback()
    {
        // Arrange
        var (orchestrator, store, _) = CreateOrchestratorWithDependencies();
        var messageId = Guid.NewGuid();
        var mockMessage = CreateMockMessage(messageId);
        mockMessage.RequestType.Returns(typeof(TestRequest).AssemblyQualifiedName!);
        mockMessage.Content.Returns("{\"value\":42}");

        store.GetDueMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new[] { mockMessage });

        var executed = false;

        // Act
        var result = await orchestrator.ProcessDueMessagesAsync((msg, type, request) =>
        {
            executed = true;
            msg.ShouldBe(mockMessage);
            type.ShouldBe(typeof(TestRequest));
            return Task.CompletedTask;
        });

        // Assert
        result.ShouldBe(1);
        executed.ShouldBeTrue();
        await store.Received(1).MarkAsProcessedAsync(messageId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessDueMessagesAsync_WithUnknownRequestType_MarksAsFailed()
    {
        // Arrange
        var (orchestrator, store, _) = CreateOrchestratorWithDependencies();
        var messageId = Guid.NewGuid();
        var mockMessage = CreateMockMessage(messageId);
        mockMessage.RequestType.Returns("UnknownType.DoesNotExist, UnknownAssembly");
        mockMessage.Content.Returns("{}");

        store.GetDueMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new[] { mockMessage });

        // Act
        var result = await orchestrator.ProcessDueMessagesAsync((_, _, _) => Task.CompletedTask);

        // Assert
        result.ShouldBe(0);
        await store.Received(1).MarkAsFailedAsync(
            messageId,
            Arg.Is<string>(s => s.Contains("Unknown request type")),
            Arg.Any<DateTime?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessDueMessagesAsync_WithDeserializationFailure_MarksAsFailed()
    {
        // Arrange
        var (orchestrator, store, _) = CreateOrchestratorWithDependencies();
        var messageId = Guid.NewGuid();
        var mockMessage = CreateMockMessage(messageId);
        mockMessage.RequestType.Returns(typeof(TestRequest).AssemblyQualifiedName!);
        mockMessage.Content.Returns("null"); // Will deserialize to null

        store.GetDueMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new[] { mockMessage });

        // Act
        var result = await orchestrator.ProcessDueMessagesAsync((_, _, _) => Task.CompletedTask);

        // Assert
        result.ShouldBe(0);
        await store.Received(1).MarkAsFailedAsync(
            messageId,
            Arg.Is<string>(s => s.Contains("deserialize")),
            Arg.Any<DateTime?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessDueMessagesAsync_WhenCallbackThrows_MarksAsFailed()
    {
        // Arrange
        var (orchestrator, store, _) = CreateOrchestratorWithDependencies();
        var messageId = Guid.NewGuid();
        var mockMessage = CreateMockMessage(messageId);
        mockMessage.RequestType.Returns(typeof(TestRequest).AssemblyQualifiedName!);
        mockMessage.Content.Returns("{\"value\":42}");

        store.GetDueMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new[] { mockMessage });

        // Act
        var result = await orchestrator.ProcessDueMessagesAsync((_, _, _) =>
            throw new InvalidOperationException("Callback failed"));

        // Assert
        result.ShouldBe(0);
        await store.Received(1).MarkAsFailedAsync(
            messageId,
            Arg.Is<string>(s => s.Contains("Callback failed")),
            Arg.Any<DateTime?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessDueMessagesAsync_WithRecurringMessage_ReschedulesMessage()
    {
        // Arrange
        var (orchestrator, store, _, cronParser) = CreateOrchestratorWithCronParserAndDependencies();
        var messageId = Guid.NewGuid();
        var nextExecution = DateTime.UtcNow.AddMinutes(5);

        var mockMessage = CreateMockMessage(messageId, isRecurring: true, cronExpression: "* * * * *");
        mockMessage.RequestType.Returns(typeof(TestRequest).AssemblyQualifiedName!);
        mockMessage.Content.Returns("{\"value\":42}");

        store.GetDueMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new[] { mockMessage });

        cronParser.GetNextOccurrence(Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns(Right<EncinaError, DateTime>(nextExecution));

        // Act
        var result = await orchestrator.ProcessDueMessagesAsync((_, _, _) => Task.CompletedTask);

        // Assert
        result.ShouldBe(1);
        await store.Received(1).RescheduleRecurringMessageAsync(
            messageId,
            nextExecution,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessDueMessagesAsync_WithRecurringMessageNoMoreOccurrences_MarksAsProcessed()
    {
        // Arrange
        var (orchestrator, store, _, cronParser) = CreateOrchestratorWithCronParserAndDependencies();
        var messageId = Guid.NewGuid();

        var mockMessage = CreateMockMessage(messageId, isRecurring: true, cronExpression: "* * * * *");
        mockMessage.RequestType.Returns(typeof(TestRequest).AssemblyQualifiedName!);
        mockMessage.Content.Returns("{\"value\":42}");

        store.GetDueMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new[] { mockMessage });

        var error = EncinaErrors.Create(SchedulingErrorCodes.InvalidCronExpression, "No more occurrences");
        cronParser.GetNextOccurrence(Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns(Left<EncinaError, DateTime>(error));

        // Act
        var result = await orchestrator.ProcessDueMessagesAsync((_, _, _) => Task.CompletedTask);

        // Assert
        result.ShouldBe(1);
        await store.Received(1).MarkAsProcessedAsync(messageId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessDueMessagesAsync_WithRecurringMessageNoCronParser_MarksAsProcessed()
    {
        // Arrange - orchestrator without cron parser but with recurring message
        var (orchestrator, store, _) = CreateOrchestratorWithDependencies();
        var messageId = Guid.NewGuid();

        var mockMessage = CreateMockMessage(messageId, isRecurring: true, cronExpression: "* * * * *");
        mockMessage.RequestType.Returns(typeof(TestRequest).AssemblyQualifiedName!);
        mockMessage.Content.Returns("{\"value\":42}");

        store.GetDueMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new[] { mockMessage });

        // Act
        var result = await orchestrator.ProcessDueMessagesAsync((_, _, _) => Task.CompletedTask);

        // Assert
        result.ShouldBe(1);
        await store.Received(1).MarkAsProcessedAsync(messageId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessDueMessagesAsync_WithRecurringMessageEmptyCronExpression_MarksAsProcessed()
    {
        // Arrange
        var (orchestrator, store, _, cronParser) = CreateOrchestratorWithCronParserAndDependencies();
        var messageId = Guid.NewGuid();

        var mockMessage = CreateMockMessage(messageId, isRecurring: true, cronExpression: "");
        mockMessage.RequestType.Returns(typeof(TestRequest).AssemblyQualifiedName!);
        mockMessage.Content.Returns("{\"value\":42}");

        store.GetDueMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new[] { mockMessage });

        // Act
        var result = await orchestrator.ProcessDueMessagesAsync((_, _, _) => Task.CompletedTask);

        // Assert
        result.ShouldBe(1);
        await store.Received(1).MarkAsProcessedAsync(messageId, Arg.Any<CancellationToken>());
        cronParser.DidNotReceive().GetNextOccurrence(Arg.Any<string>(), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task ProcessDueMessagesAsync_WithCancellation_StopsProcessing()
    {
        // Arrange
        var (orchestrator, store, _) = CreateOrchestratorWithDependencies();
        var cts = new CancellationTokenSource();

        var message1 = CreateMockMessage(Guid.NewGuid());
        message1.RequestType.Returns(typeof(TestRequest).AssemblyQualifiedName!);
        message1.Content.Returns("{\"value\":1}");

        var message2 = CreateMockMessage(Guid.NewGuid());
        message2.RequestType.Returns(typeof(TestRequest).AssemblyQualifiedName!);
        message2.Content.Returns("{\"value\":2}");

        store.GetDueMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new[] { message1, message2 });

        var processedCount = 0;

        // Act
        var result = await orchestrator.ProcessDueMessagesAsync((_, _, _) =>
        {
            processedCount++;
            if (processedCount == 1)
            {
                cts.Cancel(); // Cancel after first message
            }
            return Task.CompletedTask;
        }, cts.Token);

        // Assert
        result.ShouldBe(1); // Only first message was processed
    }

    #endregion

    #region GetPendingCountAsync

    [Fact]
    public async Task GetPendingCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var (orchestrator, store, _) = CreateOrchestratorWithDependencies();

        var messages = new[]
        {
            CreateMockMessage(Guid.NewGuid()),
            CreateMockMessage(Guid.NewGuid()),
            CreateMockMessage(Guid.NewGuid())
        };

        store.GetDueMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(messages);

        // Act
        var count = await orchestrator.GetPendingCountAsync();

        // Assert
        count.ShouldBe(3);
    }

    #endregion

    #region Helper Methods

    private static SchedulerOrchestrator CreateOrchestrator(SchedulingOptions? options = null)
    {
        var store = Substitute.For<IScheduledMessageStore>();
        var messageFactory = Substitute.For<IScheduledMessageFactory>();
        var logger = NullLogger<SchedulerOrchestrator>.Instance;

        return new SchedulerOrchestrator(store, options ?? new SchedulingOptions(), logger, messageFactory);
    }

    private static SchedulerOrchestrator CreateOrchestratorWithCronParser()
    {
        var store = Substitute.For<IScheduledMessageStore>();
        var messageFactory = Substitute.For<IScheduledMessageFactory>();
        var logger = NullLogger<SchedulerOrchestrator>.Instance;
        var cronParser = Substitute.For<ICronParser>();

        return new SchedulerOrchestrator(store, new SchedulingOptions(), logger, messageFactory, cronParser);
    }

    private static (SchedulerOrchestrator Orchestrator, IScheduledMessageStore Store, IScheduledMessageFactory MessageFactory) CreateOrchestratorWithDependencies()
    {
        var store = Substitute.For<IScheduledMessageStore>();
        var messageFactory = Substitute.For<IScheduledMessageFactory>();
        var logger = NullLogger<SchedulerOrchestrator>.Instance;

        var orchestrator = new SchedulerOrchestrator(store, new SchedulingOptions(), logger, messageFactory);

        return (orchestrator, store, messageFactory);
    }

    private static (SchedulerOrchestrator Orchestrator, IScheduledMessageStore Store, IScheduledMessageFactory MessageFactory, ICronParser CronParser) CreateOrchestratorWithCronParserAndDependencies()
    {
        var store = Substitute.For<IScheduledMessageStore>();
        var messageFactory = Substitute.For<IScheduledMessageFactory>();
        var logger = NullLogger<SchedulerOrchestrator>.Instance;
        var cronParser = Substitute.For<ICronParser>();

        var orchestrator = new SchedulerOrchestrator(store, new SchedulingOptions(), logger, messageFactory, cronParser);

        return (orchestrator, store, messageFactory, cronParser);
    }

    private static IScheduledMessage CreateMockMessage(
        Guid messageId,
        bool isRecurring = false,
        string? cronExpression = null)
    {
        var message = Substitute.For<IScheduledMessage>();
        message.Id.Returns(messageId);
        message.IsRecurring.Returns(isRecurring);
        message.CronExpression.Returns(cronExpression);
        return message;
    }

    #endregion
}

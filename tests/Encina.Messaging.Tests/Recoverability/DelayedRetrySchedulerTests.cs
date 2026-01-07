using Encina.Messaging.Recoverability;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

namespace Encina.Messaging.Tests.Recoverability;

/// <summary>
/// Unit tests for <see cref="DelayedRetryScheduler"/>.
/// </summary>
public sealed class DelayedRetrySchedulerTests
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
        var messageFactory = Substitute.For<IDelayedRetryMessageFactory>();
        var logger = NullLogger<DelayedRetryScheduler>.Instance;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new DelayedRetryScheduler(null!, messageFactory, logger));
    }

    [Fact]
    public void Constructor_WithNullMessageFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var store = Substitute.For<IDelayedRetryStore>();
        var logger = NullLogger<DelayedRetryScheduler>.Instance;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new DelayedRetryScheduler(store, null!, logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var store = Substitute.For<IDelayedRetryStore>();
        var messageFactory = Substitute.For<IDelayedRetryMessageFactory>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new DelayedRetryScheduler(store, messageFactory, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_Succeeds()
    {
        // Arrange & Act
        var scheduler = CreateScheduler();

        // Assert
        scheduler.ShouldNotBeNull();
    }

    #endregion

    #region ScheduleRetryAsync

    [Fact]
    public async Task ScheduleRetryAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var scheduler = CreateScheduler();
        var context = CreateContext();
        var delay = TimeSpan.FromSeconds(30);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
        {
            await scheduler.ScheduleRetryAsync<TestRequest>(null!, context, delay, 0);
        });
    }

    [Fact]
    public async Task ScheduleRetryAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var scheduler = CreateScheduler();
        var request = new TestRequest { Value = 42 };
        var delay = TimeSpan.FromSeconds(30);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
        {
            await scheduler.ScheduleRetryAsync(request, null!, delay, 0);
        });
    }

    [Fact]
    public async Task ScheduleRetryAsync_WithValidParameters_AddsMessageToStore()
    {
        // Arrange
        var (scheduler, store, messageFactory) = CreateSchedulerWithDependencies();
        var request = new TestRequest { Value = 42 };
        var context = CreateContext();
        var delay = TimeSpan.FromSeconds(30);
        var delayedRetryAttempt = 0;

        var mockMessage = CreateMockMessage();
        messageFactory.Create(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<DateTime>(),
            Arg.Any<DateTime>(),
            Arg.Any<string?>())
            .Returns(mockMessage);

        // Act
        await scheduler.ScheduleRetryAsync(request, context, delay, delayedRetryAttempt);

        // Assert
        await store.Received(1).AddAsync(mockMessage, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScheduleRetryAsync_PassesCorrectParametersToFactory()
    {
        // Arrange
        var (scheduler, store, messageFactory) = CreateSchedulerWithDependencies();
        var request = new TestRequest { Value = 42 };
        var correlationId = "test-correlation-id";
        var context = CreateContext(correlationId: correlationId);
        var delay = TimeSpan.FromSeconds(30);
        var delayedRetryAttempt = 2;

        var mockMessage = CreateMockMessage();
        messageFactory.Create(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<DateTime>(),
            Arg.Any<DateTime>(),
            Arg.Any<string?>())
            .Returns(mockMessage);

        // Act
        await scheduler.ScheduleRetryAsync(request, context, delay, delayedRetryAttempt);

        // Assert
        messageFactory.Received(1).Create(
            Arg.Any<Guid>(),
            context.Id,
            Arg.Is<string>(s => s.Contains(nameof(TestRequest))),
            Arg.Is<string>(s => s.Contains("42")),
            Arg.Any<string>(),
            delayedRetryAttempt,
            Arg.Any<DateTime>(),
            Arg.Any<DateTime>(),
            correlationId);
    }

    [Fact]
    public async Task ScheduleRetryAsync_WithContextWithoutCorrelationId_PassesNullCorrelationId()
    {
        // Arrange
        var (scheduler, store, messageFactory) = CreateSchedulerWithDependencies();
        var request = new TestRequest { Value = 42 };
        var context = CreateContext(correlationId: null);
        var delay = TimeSpan.FromSeconds(30);

        var mockMessage = CreateMockMessage();
        messageFactory.Create(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<DateTime>(),
            Arg.Any<DateTime>(),
            Arg.Any<string?>())
            .Returns(mockMessage);

        // Act
        await scheduler.ScheduleRetryAsync(request, context, delay, 0);

        // Assert
        messageFactory.Received(1).Create(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<DateTime>(),
            Arg.Any<DateTime>(),
            Arg.Is<string?>(s => s == null));
    }

    [Fact]
    public async Task ScheduleRetryAsync_SetsCorrectExecuteAtTime()
    {
        // Arrange
        var (scheduler, store, messageFactory) = CreateSchedulerWithDependencies();
        var request = new TestRequest { Value = 42 };
        var context = CreateContext();
        var delay = TimeSpan.FromMinutes(5);

        DateTime capturedScheduledAt = default;
        DateTime capturedExecuteAt = default;

        var mockMessage = CreateMockMessage();
        messageFactory.Create(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Do<DateTime>(d => capturedScheduledAt = d),
            Arg.Do<DateTime>(d => capturedExecuteAt = d),
            Arg.Any<string?>())
            .Returns(mockMessage);

        // Act
        var beforeCall = DateTime.UtcNow;
        await scheduler.ScheduleRetryAsync(request, context, delay, 0);
        var afterCall = DateTime.UtcNow;

        // Assert
        capturedScheduledAt.ShouldBeGreaterThanOrEqualTo(beforeCall);
        capturedScheduledAt.ShouldBeLessThanOrEqualTo(afterCall);

        var expectedExecuteAt = capturedScheduledAt.Add(delay);
        capturedExecuteAt.ShouldBe(expectedExecuteAt);
    }

    [Fact]
    public async Task ScheduleRetryAsync_SerializesContextWithError()
    {
        // Arrange
        var (scheduler, store, messageFactory) = CreateSchedulerWithDependencies();
        var request = new TestRequest { Value = 42 };
        var exception = new InvalidOperationException("Test error message");
        var context = CreateContext(lastError: exception);
        var delay = TimeSpan.FromSeconds(30);

        string capturedContextContent = string.Empty;

        var mockMessage = CreateMockMessage();
        messageFactory.Create(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Do<string>(s => capturedContextContent = s),
            Arg.Any<int>(),
            Arg.Any<DateTime>(),
            Arg.Any<DateTime>(),
            Arg.Any<string?>())
            .Returns(mockMessage);

        // Act
        await scheduler.ScheduleRetryAsync(request, context, delay, 0);

        // Assert
        capturedContextContent.ShouldContain("Test error message");
    }

    #endregion

    #region CancelScheduledRetryAsync

    [Fact]
    public async Task CancelScheduledRetryAsync_WhenRetryExists_ReturnsTrue()
    {
        // Arrange
        var (scheduler, store, _) = CreateSchedulerWithDependencies();
        var contextId = Guid.NewGuid();

        store.DeleteByContextIdAsync(contextId, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await scheduler.CancelScheduledRetryAsync(contextId);

        // Assert
        result.ShouldBeTrue();
        await store.Received(1).DeleteByContextIdAsync(contextId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelScheduledRetryAsync_WhenRetryDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var (scheduler, store, _) = CreateSchedulerWithDependencies();
        var contextId = Guid.NewGuid();

        store.DeleteByContextIdAsync(contextId, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await scheduler.CancelScheduledRetryAsync(contextId);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region Helper Methods

    private static DelayedRetryScheduler CreateScheduler()
    {
        var store = Substitute.For<IDelayedRetryStore>();
        var messageFactory = Substitute.For<IDelayedRetryMessageFactory>();
        var logger = NullLogger<DelayedRetryScheduler>.Instance;

        return new DelayedRetryScheduler(store, messageFactory, logger);
    }

    private static (DelayedRetryScheduler Scheduler, IDelayedRetryStore Store, IDelayedRetryMessageFactory MessageFactory) CreateSchedulerWithDependencies()
    {
        var store = Substitute.For<IDelayedRetryStore>();
        var messageFactory = Substitute.For<IDelayedRetryMessageFactory>();
        var logger = NullLogger<DelayedRetryScheduler>.Instance;

        var scheduler = new DelayedRetryScheduler(store, messageFactory, logger);

        return (scheduler, store, messageFactory);
    }

    private static RecoverabilityContext CreateContext(
        string? correlationId = "test-correlation",
        Exception? lastError = null)
    {
        var context = new RecoverabilityContext
        {
            CorrelationId = correlationId,
            IdempotencyKey = "test-key",
            RequestTypeName = typeof(TestRequest).FullName
        };

        if (lastError != null)
        {
            context.RecordFailedAttempt(
                EncinaErrors.Create("TEST_ERROR", lastError.Message),
                lastError,
                ErrorClassification.Transient);
        }

        return context;
    }

    private static IDelayedRetryMessage CreateMockMessage()
    {
        var message = Substitute.For<IDelayedRetryMessage>();
        message.Id.Returns(Guid.NewGuid());
        return message;
    }

    #endregion
}

using System.Data;

using Encina.Messaging.DeadLetter;
using Encina.Messaging.Health;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;

using LanguageExt;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Messaging.Health;

/// <summary>
/// Unit tests for health check classes.
/// </summary>
public sealed class HealthChecksTests
{
    #region EncinaHealthCheck (Base Class)

    [Fact]
    public async Task EncinaHealthCheck_WhenCheckThrows_ReturnsUnhealthy()
    {
        // Arrange
        var store = Substitute.For<IOutboxStore>();
        store.GetPendingCountAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        var healthCheck = new OutboxHealthCheck(store, new OutboxOptions());

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain("Database connection failed");
        result.Exception.ShouldNotBeNull();
    }

    [Fact]
    public async Task EncinaHealthCheck_WhenCancelled_ReturnsUnhealthy()
    {
        // Arrange
        var store = Substitute.For<IOutboxStore>();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        store.GetPendingCountAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        var healthCheck = new OutboxHealthCheck(store, new OutboxOptions());

        // Act
        var result = await healthCheck.CheckHealthAsync(cts.Token);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain("cancelled");
    }

    [Fact]
    public void EncinaHealthCheck_Tags_AlwaysIncludesEncinaTag()
    {
        // Arrange
        var store = Substitute.For<IOutboxStore>();
        var healthCheck = new OutboxHealthCheck(store, new OutboxOptions());

        // Act & Assert
        healthCheck.Tags.ShouldContain("encina");
    }

    [Fact]
    public void EncinaHealthCheck_Name_ReturnsCorrectName()
    {
        // Arrange
        var store = Substitute.For<IOutboxStore>();
        var healthCheck = new OutboxHealthCheck(store, new OutboxOptions());

        // Act & Assert
        healthCheck.Name.ShouldBe("encina-outbox");
    }

    #endregion

    #region OutboxHealthCheck

    [Fact]
    public void OutboxHealthCheck_WithNullStore_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new OutboxHealthCheck(null!, new OutboxOptions()));
    }

    [Fact]
    public async Task OutboxHealthCheck_WhenHealthy_ReturnsHealthy()
    {
        // Arrange
        var store = CreateOutboxStore(pending: 0, exhausted: 0);
        var healthCheck = new OutboxHealthCheck(store, new OutboxOptions());

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain("healthy");
        result.Data["pending_count"].ShouldBe(0);
        result.Data["exhausted_count"].ShouldBe(0);
    }

    [Fact]
    public async Task OutboxHealthCheck_QueriesCountsWithConfiguredMaxRetries()
    {
        // Arrange
        var store = CreateOutboxStore(pending: 0, exhausted: 0);
        var healthCheck = new OutboxHealthCheck(store, new OutboxOptions { MaxRetries = 7 });

        // Act
        await healthCheck.CheckHealthAsync();

        // Assert
        await store.Received(1).GetPendingCountAsync(7, Arg.Any<CancellationToken>());
        await store.Received(1).GetExhaustedCountAsync(7, Arg.Any<CancellationToken>());
        await store.DidNotReceive().GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OutboxHealthCheck_WhenPendingExceedsWarningThreshold_ReturnsDegraded()
    {
        // Arrange - a real count above the defaults, which the old batch-of-one sample could never reach
        var store = CreateOutboxStore(pending: 150, exhausted: 0);
        var healthCheck = new OutboxHealthCheck(store, new OutboxOptions());

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldContain("150 pending messages");
    }

    [Fact]
    public async Task OutboxHealthCheck_WhenPendingExceedsCriticalThreshold_ReturnsUnhealthy()
    {
        // Arrange
        var store = CreateOutboxStore(pending: 200, exhausted: 0);
        var options = new OutboxHealthCheckOptions
        {
            PendingMessageWarningThreshold = 50,
            PendingMessageCriticalThreshold = 100
        };
        var healthCheck = new OutboxHealthCheck(store, new OutboxOptions(), options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task OutboxHealthCheck_WithOneExhaustedMessage_ReturnsDegradedByDefault()
    {
        // Arrange
        var store = CreateOutboxStore(pending: 0, exhausted: 1);
        var healthCheck = new OutboxHealthCheck(store, new OutboxOptions());

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldContain("1 messages with exhausted retries");
        result.Data["exhausted_count"].ShouldBe(1);
    }

    [Fact]
    public async Task OutboxHealthCheck_WhenExhaustedExceedsCriticalThreshold_ReturnsUnhealthy()
    {
        // Arrange
        var store = CreateOutboxStore(pending: 0, exhausted: 5);
        var options = new OutboxHealthCheckOptions
        {
            ExhaustedMessageWarningThreshold = 2,
            ExhaustedMessageCriticalThreshold = 5
        };
        var healthCheck = new OutboxHealthCheck(store, new OutboxOptions(), options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task OutboxHealthCheck_ReportsTheWorseOfPendingAndExhaustedStatus()
    {
        // Arrange - pending is only degraded, exhausted is critical
        var store = CreateOutboxStore(pending: 150, exhausted: 100);
        var healthCheck = new OutboxHealthCheck(store, new OutboxOptions());

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("150 pending messages");
        result.Description!.ShouldContain("100 messages with exhausted retries");
    }

    [Fact]
    public async Task OutboxHealthCheck_WhenExhaustedThresholdsDisabled_IgnoresExhaustedMessages()
    {
        // Arrange
        var store = CreateOutboxStore(pending: 0, exhausted: 10);
        var options = new OutboxHealthCheckOptions
        {
            ExhaustedMessageWarningThreshold = int.MaxValue,
            ExhaustedMessageCriticalThreshold = int.MaxValue
        };
        var healthCheck = new OutboxHealthCheck(store, new OutboxOptions(), options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task OutboxHealthCheck_WhenPendingCountFails_ReturnsUnhealthy()
    {
        // Arrange
        var store = Substitute.For<IOutboxStore>();
        store.GetPendingCountAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, int>(EncinaErrors.Create("outbox.get_pending_count_failed", "pending count failed for subject patient-123")));
        var healthCheck = new OutboxHealthCheck(store, new OutboxOptions());

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);

        // Only the error code travels: EncinaError.Message can carry personal data (#1259 review).
        result.Description!.ShouldContain("outbox.get_pending_count_failed");
        result.Description!.ShouldNotContain("pending count failed for subject patient-123");
    }

    [Fact]
    public async Task OutboxHealthCheck_WhenExhaustedCountFails_ReturnsUnhealthy()
    {
        // Arrange
        var store = Substitute.For<IOutboxStore>();
        store.GetPendingCountAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(0));
        store.GetExhaustedCountAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, int>(EncinaErrors.Create("outbox.get_exhausted_count_failed", "exhausted count failed for subject patient-123")));
        var healthCheck = new OutboxHealthCheck(store, new OutboxOptions());

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);

        // Only the error code travels: EncinaError.Message can carry personal data (#1259 review).
        result.Description!.ShouldContain("outbox.get_exhausted_count_failed");
        result.Description!.ShouldNotContain("exhausted count failed for subject patient-123");
    }

    private static IOutboxStore CreateOutboxStore(int pending, int exhausted)
    {
        var store = Substitute.For<IOutboxStore>();
        store.GetPendingCountAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(pending));
        store.GetExhaustedCountAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(exhausted));
        return store;
    }

    #endregion

    #region InboxHealthCheck

    [Fact]
    public void InboxHealthCheck_WithNullStore_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new InboxHealthCheck(null!));
    }

    [Fact]
    public async Task InboxHealthCheck_WhenHealthy_ReturnsHealthy()
    {
        // Arrange
        var store = Substitute.For<IInboxStore>();
        store.GetExpiredMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IEnumerable<IInboxMessage>>(System.Array.Empty<IInboxMessage>()));

        var healthCheck = new InboxHealthCheck(store);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task InboxHealthCheck_WhenStoreThrows_ReturnsUnhealthy()
    {
        // Arrange
        var store = Substitute.For<IInboxStore>();
        store.GetExpiredMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Connection failed"));

        var healthCheck = new InboxHealthCheck(store);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    #endregion

    #region DeadLetterHealthCheck

    [Fact]
    public void DeadLetterHealthCheck_WithNullStore_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new DeadLetterHealthCheck(null!));
    }

    [Fact]
    public async Task DeadLetterHealthCheck_WhenHealthy_ReturnsHealthy()
    {
        // Arrange
        var store = Substitute.For<IDeadLetterStore>();
        store.GetCountAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(0));
        store.GetMessagesAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IEnumerable<IDeadLetterMessage>>(System.Array.Empty<IDeadLetterMessage>()));

        var healthCheck = new DeadLetterHealthCheck(store);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task DeadLetterHealthCheck_WhenExceedsWarningThreshold_ReturnsDegraded()
    {
        // Arrange
        var store = Substitute.For<IDeadLetterStore>();
        store.GetCountAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(50));
        store.GetMessagesAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IEnumerable<IDeadLetterMessage>>(System.Array.Empty<IDeadLetterMessage>()));

        var options = new DeadLetterHealthCheckOptions
        {
            PendingMessageWarningThreshold = 10,
            PendingMessageCriticalThreshold = 100,
            OldMessageThreshold = null // Disable old message check
        };
        var healthCheck = new DeadLetterHealthCheck(store, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task DeadLetterHealthCheck_WhenExceedsCriticalThreshold_ReturnsUnhealthy()
    {
        // Arrange
        var store = Substitute.For<IDeadLetterStore>();
        store.GetCountAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(100));
        store.GetMessagesAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IEnumerable<IDeadLetterMessage>>(System.Array.Empty<IDeadLetterMessage>()));

        var options = new DeadLetterHealthCheckOptions
        {
            PendingMessageWarningThreshold = 10,
            PendingMessageCriticalThreshold = 50,
            OldMessageThreshold = null
        };
        var healthCheck = new DeadLetterHealthCheck(store, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task DeadLetterHealthCheck_WithOldMessages_ReturnsDegraded()
    {
        // Arrange
        var store = Substitute.For<IDeadLetterStore>();
        store.GetCountAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(5)); // Below warning threshold

        var oldMessage = Substitute.For<IDeadLetterMessage>();
        store.GetMessagesAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IEnumerable<IDeadLetterMessage>>(new[] { oldMessage })); // Has old messages

        var options = new DeadLetterHealthCheckOptions
        {
            PendingMessageWarningThreshold = 10,
            PendingMessageCriticalThreshold = 100,
            OldMessageThreshold = TimeSpan.FromHours(24)
        };
        var healthCheck = new DeadLetterHealthCheck(store, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    #endregion

    #region SagaHealthCheck

    [Fact]
    public void SagaHealthCheck_WithNullStore_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new SagaHealthCheck(null!));
    }

    [Fact]
    public async Task SagaHealthCheck_WhenHealthy_ReturnsHealthy()
    {
        // Arrange
        var store = Substitute.For<ISagaStore>();
        store.GetStuckSagasAsync(Arg.Any<TimeSpan>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IEnumerable<ISagaState>>(System.Array.Empty<ISagaState>()));
        store.GetExpiredSagasAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IEnumerable<ISagaState>>(System.Array.Empty<ISagaState>()));

        var healthCheck = new SagaHealthCheck(store);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task SagaHealthCheck_WhenStuckSagasExceedWarning_ReturnsDegraded()
    {
        // Arrange
        var store = Substitute.For<ISagaStore>();
        var stuckSagas = Enumerable.Range(0, 15).Select(_ => Substitute.For<ISagaState>());
        store.GetStuckSagasAsync(Arg.Any<TimeSpan>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IEnumerable<ISagaState>>(stuckSagas));
        store.GetExpiredSagasAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IEnumerable<ISagaState>>(System.Array.Empty<ISagaState>()));

        var options = new SagaHealthCheckOptions
        {
            SagaWarningThreshold = 10,
            SagaCriticalThreshold = 50
        };
        var healthCheck = new SagaHealthCheck(store, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task SagaHealthCheck_WhenTotalProblematicExceedsCritical_ReturnsUnhealthy()
    {
        // Arrange
        var store = Substitute.For<ISagaStore>();
        var stuckSagas = Enumerable.Range(0, 30).Select(_ => Substitute.For<ISagaState>());
        var expiredSagas = Enumerable.Range(0, 25).Select(_ => Substitute.For<ISagaState>());
        store.GetStuckSagasAsync(Arg.Any<TimeSpan>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IEnumerable<ISagaState>>(stuckSagas));
        store.GetExpiredSagasAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IEnumerable<ISagaState>>(expiredSagas));

        var options = new SagaHealthCheckOptions
        {
            SagaWarningThreshold = 10,
            SagaCriticalThreshold = 50
        };
        var healthCheck = new SagaHealthCheck(store, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    #endregion

    #region SchedulingHealthCheck

    [Fact]
    public void SchedulingHealthCheck_WithNullStore_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new SchedulingHealthCheck(null!));
    }

    [Fact]
    public async Task SchedulingHealthCheck_WhenHealthy_ReturnsHealthy()
    {
        // Arrange
        var store = Substitute.For<IScheduledMessageStore>();
        store.GetDueMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IEnumerable<IScheduledMessage>>(System.Array.Empty<IScheduledMessage>()));

        var healthCheck = new SchedulingHealthCheck(store);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task SchedulingHealthCheck_WhenOverdueExceedsWarning_ReturnsDegraded()
    {
        // Arrange
        var store = Substitute.For<IScheduledMessageStore>();
        var overdueMessages = Enumerable.Range(0, 20).Select(i =>
        {
            var msg = Substitute.For<IScheduledMessage>();
            msg.ScheduledAtUtc.Returns(DateTime.UtcNow.AddMinutes(-30)); // 30 minutes overdue
            return msg;
        });
        store.GetDueMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IEnumerable<IScheduledMessage>>(overdueMessages));

        var options = new SchedulingHealthCheckOptions
        {
            OverdueTolerance = TimeSpan.FromMinutes(5),
            OverdueWarningThreshold = 10,
            OverdueCriticalThreshold = 50
        };
        var healthCheck = new SchedulingHealthCheck(store, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task SchedulingHealthCheck_WhenOverdueExceedsCritical_ReturnsUnhealthy()
    {
        // Arrange
        var store = Substitute.For<IScheduledMessageStore>();
        var overdueMessages = Enumerable.Range(0, 60).Select(i =>
        {
            var msg = Substitute.For<IScheduledMessage>();
            msg.ScheduledAtUtc.Returns(DateTime.UtcNow.AddMinutes(-30));
            return msg;
        });
        store.GetDueMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IEnumerable<IScheduledMessage>>(overdueMessages));

        var options = new SchedulingHealthCheckOptions
        {
            OverdueTolerance = TimeSpan.FromMinutes(5),
            OverdueWarningThreshold = 10,
            OverdueCriticalThreshold = 50
        };
        var healthCheck = new SchedulingHealthCheck(store, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task SchedulingHealthCheck_WhenDueButNotOverdue_ReturnsHealthy()
    {
        // Arrange
        var store = Substitute.For<IScheduledMessageStore>();
        var dueMessages = Enumerable.Range(0, 20).Select(i =>
        {
            var msg = Substitute.For<IScheduledMessage>();
            msg.ScheduledAtUtc.Returns(DateTime.UtcNow.AddMinutes(-2)); // Only 2 minutes ago
            return msg;
        });
        store.GetDueMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IEnumerable<IScheduledMessage>>(dueMessages));

        var options = new SchedulingHealthCheckOptions
        {
            OverdueTolerance = TimeSpan.FromMinutes(5), // 5 minute tolerance
            OverdueWarningThreshold = 10,
            OverdueCriticalThreshold = 50
        };
        var healthCheck = new SchedulingHealthCheck(store, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    #endregion

    #region DatabaseHealthCheck

    [Fact]
    public async Task DatabaseHealthCheck_WhenHealthy_ReturnsHealthy()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        connection.State.Returns(ConnectionState.Open);

        var command = Substitute.For<IDbCommand>();
        command.ExecuteScalar().Returns(1);
        connection.CreateCommand().Returns(command);

        Func<IDbConnection> connectionFactory = () => connection;

        var healthCheck = new TestDatabaseHealthCheck(connectionFactory);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task DatabaseHealthCheck_WhenConnectionFails_ReturnsUnhealthy()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        connection.State.Returns(ConnectionState.Closed);
        connection.When(x => x.Open()).Do(_ => throw new InvalidOperationException("Connection failed"));

        Func<IDbConnection> connectionFactory = () => connection;

        var healthCheck = new TestDatabaseHealthCheck(connectionFactory);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    /// <summary>
    /// Test implementation of DatabaseHealthCheck since it's protected.
    /// </summary>
    private sealed class TestDatabaseHealthCheck : DatabaseHealthCheck
    {
        public TestDatabaseHealthCheck(Func<IDbConnection> connectionFactory)
            : base("test-database", connectionFactory)
        {
        }
    }

    #endregion
}

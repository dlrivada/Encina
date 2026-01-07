using System.Data;

using Encina.Messaging.DeadLetter;
using Encina.Messaging.Health;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

namespace Encina.Messaging.Tests.Health;

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
        store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        var healthCheck = new OutboxHealthCheck(store);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain("Database connection failed");
        result.Exception.ShouldNotBeNull();
    }

    [Fact]
    public async Task EncinaHealthCheck_WhenCancelled_ReturnsUnhealthy()
    {
        // Arrange
        var store = Substitute.For<IOutboxStore>();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        var healthCheck = new OutboxHealthCheck(store);

        // Act
        var result = await healthCheck.CheckHealthAsync(cts.Token);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain("cancelled");
    }

    [Fact]
    public void EncinaHealthCheck_Tags_AlwaysIncludesEncinaTag()
    {
        // Arrange
        var store = Substitute.For<IOutboxStore>();
        var healthCheck = new OutboxHealthCheck(store);

        // Act & Assert
        healthCheck.Tags.ShouldContain("encina");
    }

    [Fact]
    public void EncinaHealthCheck_Name_ReturnsCorrectName()
    {
        // Arrange
        var store = Substitute.For<IOutboxStore>();
        var healthCheck = new OutboxHealthCheck(store);

        // Act & Assert
        healthCheck.Name.ShouldBe("encina-outbox");
    }

    #endregion

    #region OutboxHealthCheck

    [Fact]
    public void OutboxHealthCheck_WithNullStore_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new OutboxHealthCheck(null!));
    }

    [Fact]
    public async Task OutboxHealthCheck_WhenHealthy_ReturnsHealthy()
    {
        // Arrange
        var store = Substitute.For<IOutboxStore>();
        store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<IOutboxMessage>());

        var healthCheck = new OutboxHealthCheck(store);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain("healthy");
    }

    [Fact]
    public async Task OutboxHealthCheck_WhenExceedsWarningThreshold_ReturnsDegraded()
    {
        // Arrange
        var store = Substitute.For<IOutboxStore>();
        var messages = Enumerable.Range(0, 100).Select(_ => Substitute.For<IOutboxMessage>());
        store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(messages);

        var options = new OutboxHealthCheckOptions
        {
            PendingMessageWarningThreshold = 50,
            PendingMessageCriticalThreshold = 200
        };
        var healthCheck = new OutboxHealthCheck(store, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task OutboxHealthCheck_WhenExceedsCriticalThreshold_ReturnsUnhealthy()
    {
        // Arrange
        var store = Substitute.For<IOutboxStore>();
        var messages = Enumerable.Range(0, 200).Select(_ => Substitute.For<IOutboxMessage>());
        store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(messages);

        var options = new OutboxHealthCheckOptions
        {
            PendingMessageWarningThreshold = 50,
            PendingMessageCriticalThreshold = 100
        };
        var healthCheck = new OutboxHealthCheck(store, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
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
            .Returns(Array.Empty<IInboxMessage>());

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
            .Returns(0);
        store.GetMessagesAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<IDeadLetterMessage>());

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
            .Returns(50);
        store.GetMessagesAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<IDeadLetterMessage>());

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
            .Returns(100);
        store.GetMessagesAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<IDeadLetterMessage>());

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
            .Returns(5); // Below warning threshold

        var oldMessage = Substitute.For<IDeadLetterMessage>();
        store.GetMessagesAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new[] { oldMessage }); // Has old messages

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
            .Returns(Array.Empty<ISagaState>());
        store.GetExpiredSagasAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ISagaState>());

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
            .Returns(stuckSagas);
        store.GetExpiredSagasAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ISagaState>());

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
            .Returns(stuckSagas);
        store.GetExpiredSagasAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(expiredSagas);

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
            .Returns(Array.Empty<IScheduledMessage>());

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
            .Returns(overdueMessages);

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
            .Returns(overdueMessages);

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
            .Returns(dueMessages);

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

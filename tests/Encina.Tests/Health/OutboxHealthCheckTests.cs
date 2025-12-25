using Encina.Messaging.Health;
using Encina.Messaging.Outbox;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Encina.Tests.Health;

public sealed class OutboxHealthCheckTests
{
    private readonly IOutboxStore _store;
    private readonly OutboxHealthCheck _healthCheck;

    public OutboxHealthCheckTests()
    {
        _store = Substitute.For<IOutboxStore>();
        _healthCheck = new OutboxHealthCheck(_store);
    }

    [Fact]
    public void Constructor_WithNullStore_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new OutboxHealthCheck(null!));
    }

    [Fact]
    public void Name_ReturnsExpectedValue()
    {
        // Assert
        _healthCheck.Name.ShouldBe("encina-outbox");
    }

    [Fact]
    public void Tags_ContainsExpectedValues()
    {
        // Assert
        _healthCheck.Tags.ShouldContain("ready");
        _healthCheck.Tags.ShouldContain("database");
        _healthCheck.Tags.ShouldContain("messaging");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenNoPendingMessages_ReturnsHealthy()
    {
        // Arrange
        _store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        // Act
        var result = await _healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("healthy");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenPendingMessagesBelowThreshold_ReturnsHealthy()
    {
        // Arrange
        var options = new OutboxHealthCheckOptions { PendingMessageWarningThreshold = 100 };
        var healthCheck = new OutboxHealthCheck(_store, options);
        var pendingMessages = CreateMessages(50);

        _store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(pendingMessages);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenPendingMessagesExceedWarningThreshold_ReturnsDegraded()
    {
        // Arrange
        var options = new OutboxHealthCheckOptions
        {
            PendingMessageWarningThreshold = 10,
            PendingMessageCriticalThreshold = 100
        };
        var healthCheck = new OutboxHealthCheck(_store, options);
        var pendingMessages = CreateMessages(50);

        _store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(pendingMessages);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldContain("warning");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenPendingMessagesExceedCriticalThreshold_ReturnsUnhealthy()
    {
        // Arrange
        var options = new OutboxHealthCheckOptions
        {
            PendingMessageWarningThreshold = 10,
            PendingMessageCriticalThreshold = 50
        };
        var healthCheck = new OutboxHealthCheck(_store, options);
        var pendingMessages = CreateMessages(100);

        _store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(pendingMessages);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("critical");
    }

    [Fact]
    public async Task CheckHealthAsync_IncludesDataInResult()
    {
        // Arrange
        _store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        // Act
        var result = await _healthCheck.CheckHealthAsync();

        // Assert
        result.Data.ShouldContainKey("pending_sample");
        result.Data.ShouldContainKey("warning_threshold");
        result.Data.ShouldContainKey("critical_threshold");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenStoreThrows_ReturnsUnhealthy()
    {
        // Arrange
        _store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Database error"));

        // Act
        var result = await _healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Exception.ShouldNotBeNull();
        result.Description!.ShouldContain("Database error");
    }

    private static List<IOutboxMessage> CreateMessages(int count)
    {
        var messages = new List<IOutboxMessage>();
        for (int i = 0; i < count; i++)
        {
            var message = Substitute.For<IOutboxMessage>();
            messages.Add(message);
        }
        return messages;
    }
}

public sealed class OutboxHealthCheckOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new OutboxHealthCheckOptions();

        // Assert
        options.PendingMessageWarningThreshold.ShouldBe(100);
        options.PendingMessageCriticalThreshold.ShouldBe(1000);
    }
}

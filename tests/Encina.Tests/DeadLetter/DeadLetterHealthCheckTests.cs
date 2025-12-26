using Encina.Messaging.DeadLetter;
using Encina.Messaging.Health;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Encina.Tests.DeadLetter;

public sealed class DeadLetterHealthCheckTests
{
    private readonly IDeadLetterStore _store;
    private readonly DeadLetterHealthCheck _healthCheck;

    public DeadLetterHealthCheckTests()
    {
        _store = Substitute.For<IDeadLetterStore>();
        _healthCheck = new DeadLetterHealthCheck(_store);
    }

    [Fact]
    public void Constructor_WithNullStore_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new DeadLetterHealthCheck(null!));
    }

    [Fact]
    public void Name_ReturnsExpectedValue()
    {
        // Assert
        _healthCheck.Name.ShouldBe("encina-deadletter");
    }

    [Fact]
    public void Tags_ContainsExpectedValues()
    {
        // Assert
        _healthCheck.Tags.ShouldContain("encina");
        _healthCheck.Tags.ShouldContain("messaging");
        _healthCheck.Tags.ShouldContain("deadletter");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenNoPendingMessages_ReturnsHealthy()
    {
        // Arrange
        _store.GetCountAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<CancellationToken>())
            .Returns(0);
        _store.GetMessagesAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        // Act
        var result = await _healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("empty");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenPendingMessagesBelowThreshold_ReturnsHealthy()
    {
        // Arrange
        var options = new DeadLetterHealthCheckOptions { PendingMessageWarningThreshold = 100 };
        var healthCheck = new DeadLetterHealthCheck(_store, options);

        _store.GetCountAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<CancellationToken>())
            .Returns(5);
        _store.GetMessagesAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenPendingMessagesExceedWarningThreshold_ReturnsDegraded()
    {
        // Arrange
        var options = new DeadLetterHealthCheckOptions
        {
            PendingMessageWarningThreshold = 10,
            PendingMessageCriticalThreshold = 100,
            OldMessageThreshold = null
        };
        var healthCheck = new DeadLetterHealthCheck(_store, options);

        _store.GetCountAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<CancellationToken>())
            .Returns(50);

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
        var options = new DeadLetterHealthCheckOptions
        {
            PendingMessageWarningThreshold = 10,
            PendingMessageCriticalThreshold = 50,
            OldMessageThreshold = null
        };
        var healthCheck = new DeadLetterHealthCheck(_store, options);

        _store.GetCountAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<CancellationToken>())
            .Returns(100);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("critical");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenOldMessagesExist_ReturnsDegraded()
    {
        // Arrange
        var options = new DeadLetterHealthCheckOptions
        {
            PendingMessageWarningThreshold = 100,
            PendingMessageCriticalThreshold = 1000,
            OldMessageThreshold = TimeSpan.FromHours(24)
        };
        var healthCheck = new DeadLetterHealthCheck(_store, options);

        _store.GetCountAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<CancellationToken>())
            .Returns(5);

        var oldMessage = Substitute.For<IDeadLetterMessage>();
        oldMessage.DeadLetteredAtUtc.Returns(DateTime.UtcNow.AddDays(-2));

        _store.GetMessagesAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([oldMessage]);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Data.ShouldContainKey("has_old_messages");
    }

    [Fact]
    public async Task CheckHealthAsync_IncludesDataInResult()
    {
        // Arrange
        _store.GetCountAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<CancellationToken>())
            .Returns(0);
        _store.GetMessagesAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        // Act
        var result = await _healthCheck.CheckHealthAsync();

        // Assert
        result.Data.ShouldContainKey("pending_count");
        result.Data.ShouldContainKey("warning_threshold");
        result.Data.ShouldContainKey("critical_threshold");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenStoreThrows_ReturnsUnhealthy()
    {
        // Arrange
        _store.GetCountAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Database error"));

        // Act
        var result = await _healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Exception.ShouldNotBeNull();
    }
}

public sealed class DeadLetterHealthCheckOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new DeadLetterHealthCheckOptions();

        // Assert
        options.PendingMessageWarningThreshold.ShouldBe(10);
        options.PendingMessageCriticalThreshold.ShouldBe(100);
        options.OldMessageThreshold.ShouldBe(TimeSpan.FromHours(24));
    }
}

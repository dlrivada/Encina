using Encina.Messaging.Health;
using Encina.Messaging.Scheduling;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Encina.Tests.Health;

public sealed class SchedulingHealthCheckTests
{
    private readonly IScheduledMessageStore _store;
    private readonly SchedulingHealthCheck _healthCheck;

    public SchedulingHealthCheckTests()
    {
        _store = Substitute.For<IScheduledMessageStore>();
        _healthCheck = new SchedulingHealthCheck(_store);
    }

    [Fact]
    public void Constructor_WithNullStore_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new SchedulingHealthCheck(null!));
    }

    [Fact]
    public void Name_ReturnsExpectedValue()
    {
        // Assert
        _healthCheck.Name.ShouldBe("encina-scheduling");
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
    public async Task CheckHealthAsync_WhenNoDueMessages_ReturnsHealthy()
    {
        // Arrange
        _store.GetDueMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        // Act
        var result = await _healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("healthy");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenNoOverdueMessages_ReturnsHealthy()
    {
        // Arrange
        var options = new SchedulingHealthCheckOptions { OverdueTolerance = TimeSpan.FromMinutes(5) };
        var healthCheck = new SchedulingHealthCheck(_store, options);

        var recentMessages = CreateMessages(10, DateTime.UtcNow.AddMinutes(-2));
        _store.GetDueMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(recentMessages);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenOverdueMessagesExceedWarningThreshold_ReturnsDegraded()
    {
        // Arrange
        var options = new SchedulingHealthCheckOptions
        {
            OverdueTolerance = TimeSpan.FromMinutes(5),
            OverdueWarningThreshold = 5,
            OverdueCriticalThreshold = 20
        };
        var healthCheck = new SchedulingHealthCheck(_store, options);

        var overdueMessages = CreateMessages(10, DateTime.UtcNow.AddMinutes(-30));
        _store.GetDueMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(overdueMessages);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldContain("warning");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenOverdueMessagesExceedCriticalThreshold_ReturnsUnhealthy()
    {
        // Arrange
        var options = new SchedulingHealthCheckOptions
        {
            OverdueTolerance = TimeSpan.FromMinutes(5),
            OverdueWarningThreshold = 5,
            OverdueCriticalThreshold = 10
        };
        var healthCheck = new SchedulingHealthCheck(_store, options);

        var overdueMessages = CreateMessages(15, DateTime.UtcNow.AddMinutes(-30));
        _store.GetDueMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(overdueMessages);

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
        _store.GetDueMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        // Act
        var result = await _healthCheck.CheckHealthAsync();

        // Assert
        result.Data.ShouldContainKey("due_count");
        result.Data.ShouldContainKey("overdue_count");
        result.Data.ShouldContainKey("warning_threshold");
        result.Data.ShouldContainKey("critical_threshold");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenStoreThrows_ReturnsUnhealthy()
    {
        // Arrange
        _store.GetDueMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Database error"));

        // Act
        var result = await _healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Exception.ShouldNotBeNull();
    }

    private static List<IScheduledMessage> CreateMessages(int count, DateTime scheduledAt)
    {
        var messages = new List<IScheduledMessage>();
        for (int i = 0; i < count; i++)
        {
            var message = Substitute.For<IScheduledMessage>();
            message.ScheduledAtUtc.Returns(scheduledAt);
            messages.Add(message);
        }
        return messages;
    }
}

public sealed class SchedulingHealthCheckOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new SchedulingHealthCheckOptions();

        // Assert
        options.OverdueTolerance.ShouldBe(TimeSpan.FromMinutes(5));
        options.OverdueWarningThreshold.ShouldBe(10);
        options.OverdueCriticalThreshold.ShouldBe(50);
    }
}

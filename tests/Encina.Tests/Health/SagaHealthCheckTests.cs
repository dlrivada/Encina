using Encina.Messaging.Health;
using Encina.Messaging.Sagas;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Encina.Tests.Health;

public sealed class SagaHealthCheckTests
{
    private readonly ISagaStore _store;
    private readonly SagaHealthCheck _healthCheck;

    public SagaHealthCheckTests()
    {
        _store = Substitute.For<ISagaStore>();
        _healthCheck = new SagaHealthCheck(_store);
    }

    [Fact]
    public void Constructor_WithNullStore_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new SagaHealthCheck(null!));
    }

    [Fact]
    public void Name_ReturnsExpectedValue()
    {
        // Assert
        _healthCheck.Name.ShouldBe("encina-saga");
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
    public async Task CheckHealthAsync_WhenNoProblematicSagas_ReturnsHealthy()
    {
        // Arrange
        _store.GetStuckSagasAsync(Arg.Any<TimeSpan>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _store.GetExpiredSagasAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        // Act
        var result = await _healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("healthy");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenStuckSagasExceedWarningThreshold_ReturnsDegraded()
    {
        // Arrange
        var options = new SagaHealthCheckOptions
        {
            SagaWarningThreshold = 5,
            SagaCriticalThreshold = 20
        };
        var healthCheck = new SagaHealthCheck(_store, options);

        var stuckSagas = CreateSagas(10);
        _store.GetStuckSagasAsync(Arg.Any<TimeSpan>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(stuckSagas);
        _store.GetExpiredSagasAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldContain("warning");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenExpiredSagasExceedWarningThreshold_ReturnsDegraded()
    {
        // Arrange
        var options = new SagaHealthCheckOptions
        {
            SagaWarningThreshold = 5,
            SagaCriticalThreshold = 20
        };
        var healthCheck = new SagaHealthCheck(_store, options);

        var expiredSagas = CreateSagas(10);
        _store.GetStuckSagasAsync(Arg.Any<TimeSpan>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _store.GetExpiredSagasAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(expiredSagas);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenProblematicSagasExceedCriticalThreshold_ReturnsUnhealthy()
    {
        // Arrange
        var options = new SagaHealthCheckOptions
        {
            SagaWarningThreshold = 5,
            SagaCriticalThreshold = 10
        };
        var healthCheck = new SagaHealthCheck(_store, options);

        var stuckSagas = CreateSagas(8);
        var expiredSagas = CreateSagas(5);
        _store.GetStuckSagasAsync(Arg.Any<TimeSpan>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(stuckSagas);
        _store.GetExpiredSagasAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(expiredSagas);

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
        _store.GetStuckSagasAsync(Arg.Any<TimeSpan>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _store.GetExpiredSagasAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        // Act
        var result = await _healthCheck.CheckHealthAsync();

        // Assert
        result.Data.ShouldContainKey("stuck_count");
        result.Data.ShouldContainKey("expired_count");
        result.Data.ShouldContainKey("warning_threshold");
        result.Data.ShouldContainKey("critical_threshold");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenStoreThrows_ReturnsUnhealthy()
    {
        // Arrange
        _store.GetStuckSagasAsync(Arg.Any<TimeSpan>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Database error"));

        // Act
        var result = await _healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Exception.ShouldNotBeNull();
    }

    private static List<ISagaState> CreateSagas(int count)
    {
        var sagas = new List<ISagaState>();
        for (int i = 0; i < count; i++)
        {
            var saga = Substitute.For<ISagaState>();
            sagas.Add(saga);
        }
        return sagas;
    }
}

public sealed class SagaHealthCheckOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new SagaHealthCheckOptions();

        // Assert
        options.StuckSagaThreshold.ShouldBe(TimeSpan.FromMinutes(30));
        options.SagaWarningThreshold.ShouldBe(10);
        options.SagaCriticalThreshold.ShouldBe(50);
    }
}

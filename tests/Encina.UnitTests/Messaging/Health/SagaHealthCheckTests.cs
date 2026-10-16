using Encina.Messaging.Health;
using Encina.Messaging.Sagas;
using LanguageExt;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Messaging.Health;

public sealed class SagaHealthCheckTests
{
    private readonly ISagaStore _store = Substitute.For<ISagaStore>();

    [Fact]
    public void Constructor_WithNullStore_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new SagaHealthCheck(null!));
    }

    [Fact]
    public async Task CheckHealthAsync_NoProblematicSagas_ReturnsHealthy()
    {
        _store.GetStuckSagasAsync(Arg.Any<TimeSpan>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IEnumerable<ISagaState>>.Right(Array.Empty<ISagaState>()));
        _store.GetExpiredSagasAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IEnumerable<ISagaState>>.Right(Array.Empty<ISagaState>()));

        var sut = new SagaHealthCheck(_store);
        var result = await sut.CheckHealthAsync(CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_StuckExceedsCritical_ReturnsUnhealthy()
    {
        var options = new SagaHealthCheckOptions
        {
            SagaWarningThreshold = 2,
            SagaCriticalThreshold = 5
        };

        var stuckSagas = CreateSagas(6);
        _store.GetStuckSagasAsync(Arg.Any<TimeSpan>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IEnumerable<ISagaState>>.Right(stuckSagas));
        _store.GetExpiredSagasAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IEnumerable<ISagaState>>.Right(Array.Empty<ISagaState>()));

        var sut = new SagaHealthCheck(_store, options);
        var result = await sut.CheckHealthAsync(CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_StuckExceedsWarning_ReturnsDegraded()
    {
        var options = new SagaHealthCheckOptions
        {
            SagaWarningThreshold = 2,
            SagaCriticalThreshold = 50
        };

        var stuckSagas = CreateSagas(3);
        _store.GetStuckSagasAsync(Arg.Any<TimeSpan>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IEnumerable<ISagaState>>.Right(stuckSagas));
        _store.GetExpiredSagasAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IEnumerable<ISagaState>>.Right(Array.Empty<ISagaState>()));

        var sut = new SagaHealthCheck(_store, options);
        var result = await sut.CheckHealthAsync(CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task CheckHealthAsync_CombinedStuckAndExpiredExceedsCritical_ReturnsUnhealthy()
    {
        var options = new SagaHealthCheckOptions
        {
            SagaWarningThreshold = 5,
            SagaCriticalThreshold = 8
        };

        _store.GetStuckSagasAsync(Arg.Any<TimeSpan>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IEnumerable<ISagaState>>.Right(CreateSagas(4)));
        _store.GetExpiredSagasAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IEnumerable<ISagaState>>.Right(CreateSagas(5)));

        var sut = new SagaHealthCheck(_store, options);
        var result = await sut.CheckHealthAsync(CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_BothCallsReturnError_ReturnsUnhealthyWithoutMessage()
    {
        _store.GetStuckSagasAsync(Arg.Any<TimeSpan>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IEnumerable<ISagaState>>.Left(EncinaError.New("secret-detail")));
        _store.GetExpiredSagasAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IEnumerable<ISagaState>>.Left(EncinaError.New("secret-detail")));

        var sut = new SagaHealthCheck(_store);
        var result = await sut.CheckHealthAsync(CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldNotContain("secret-detail");
        result.Data["error"].ShouldBe("encina.unknown");
    }

    [Fact]
    public async Task CheckHealthAsync_StuckCallReturnsError_ReturnsUnhealthyWithErrorCode()
    {
        _store.GetStuckSagasAsync(Arg.Any<TimeSpan>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IEnumerable<ISagaState>>.Left(EncinaErrors.Create("saga.stuck_failed", "secret-detail")));

        var sut = new SagaHealthCheck(_store);
        var result = await sut.CheckHealthAsync(CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("saga.stuck_failed");
        result.Description!.ShouldNotContain("secret-detail");
        result.Data["error"].ShouldBe("saga.stuck_failed");
        await _store.DidNotReceive().GetExpiredSagasAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckHealthAsync_ExpiredCallReturnsError_ReturnsUnhealthyWithErrorCode()
    {
        _store.GetStuckSagasAsync(Arg.Any<TimeSpan>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IEnumerable<ISagaState>>.Right(Array.Empty<ISagaState>()));
        _store.GetExpiredSagasAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IEnumerable<ISagaState>>.Left(EncinaErrors.Create("saga.expired_failed", "secret-detail")));

        var sut = new SagaHealthCheck(_store);
        var result = await sut.CheckHealthAsync(CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("saga.expired_failed");
        result.Description!.ShouldNotContain("secret-detail");
        result.Data["error"].ShouldBe("saga.expired_failed");
    }

    private static IEnumerable<ISagaState> CreateSagas(int count)
    {
        return Enumerable.Range(0, count).Select(_ => Substitute.For<ISagaState>());
    }
}

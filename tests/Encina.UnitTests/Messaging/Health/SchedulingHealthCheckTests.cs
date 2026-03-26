using Encina.Messaging.Health;
using Encina.Messaging.Scheduling;
using LanguageExt;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Messaging.Health;

public sealed class SchedulingHealthCheckTests
{
    private readonly IScheduledMessageStore _store = Substitute.For<IScheduledMessageStore>();

    [Fact]
    public void Constructor_WithNullStore_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new SchedulingHealthCheck(null!));
    }

    [Fact]
    public void Constructor_WithDefaultOptions_CreatesInstance()
    {
        var sut = new SchedulingHealthCheck(_store);
        sut.ShouldNotBeNull();
    }

    [Fact]
    public async Task CheckHealthAsync_WhenStoreReturnsError_ReturnsUnhealthy()
    {
        var error = EncinaError.New("Store failure");
        _store.GetDueMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IEnumerable<IScheduledMessage>>.Left(error));

        var sut = new SchedulingHealthCheck(_store);
        var result = await sut.CheckHealthAsync(CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("Store failure");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenNoOverdueMessages_ReturnsHealthy()
    {
        var messages = Array.Empty<IScheduledMessage>();
        _store.GetDueMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IEnumerable<IScheduledMessage>>.Right(messages));

        var sut = new SchedulingHealthCheck(_store);
        var result = await sut.CheckHealthAsync(CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenOverdueExceedsCritical_ReturnsUnhealthy()
    {
        var options = new SchedulingHealthCheckOptions
        {
            OverdueTolerance = TimeSpan.FromMinutes(1),
            OverdueWarningThreshold = 1,
            OverdueCriticalThreshold = 2
        };

        var now = DateTime.UtcNow;
        var overdueMessage1 = CreateScheduledMessage(now.AddMinutes(-10));
        var overdueMessage2 = CreateScheduledMessage(now.AddMinutes(-15));
        var messages = new[] { overdueMessage1, overdueMessage2 };

        _store.GetDueMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IEnumerable<IScheduledMessage>>.Right(messages));

        var sut = new SchedulingHealthCheck(_store, options);
        var result = await sut.CheckHealthAsync(CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenOverdueExceedsWarning_ReturnsDegraded()
    {
        var options = new SchedulingHealthCheckOptions
        {
            OverdueTolerance = TimeSpan.FromMinutes(1),
            OverdueWarningThreshold = 1,
            OverdueCriticalThreshold = 10
        };

        var now = DateTime.UtcNow;
        var overdueMessage = CreateScheduledMessage(now.AddMinutes(-10));
        var messages = new[] { overdueMessage };

        _store.GetDueMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IEnumerable<IScheduledMessage>>.Right(messages));

        var sut = new SchedulingHealthCheck(_store, options);
        var result = await sut.CheckHealthAsync(CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    private static IScheduledMessage CreateScheduledMessage(DateTime scheduledAtUtc)
    {
        var msg = Substitute.For<IScheduledMessage>();
        msg.ScheduledAtUtc.Returns(scheduledAtUtc);
        return msg;
    }
}

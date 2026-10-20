using Encina.Security.Audit;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.Audit;

/// <summary>
/// Unit tests for <see cref="OperationAuditRetentionService"/>.
/// </summary>
public class OperationAuditRetentionServiceTests
{
    // Event ids of the retention purge log messages (see Log.cs).
    private const int PurgeCompletedEvent = 5006;
    private const int NothingToPurgeEvent = 5007;
    private const int PurgeFailedEvent = 5008;
    private const int PurgeCancelledEvent = 5009;
    private const int PurgeErrorEvent = 5010;

    private static readonly DateTimeOffset Start = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);

    private readonly IOperationAuditStore _mockAuditStore;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OperationAuditRetentionService> _logger;

    public OperationAuditRetentionServiceTests()
    {
        _mockAuditStore = Substitute.For<IOperationAuditStore>();
        _scopeFactory = new ServiceCollection()
            .AddSingleton(_mockAuditStore)
            .BuildServiceProvider()
            .GetRequiredService<IServiceScopeFactory>();
        _logger = NullLogger<OperationAuditRetentionService>.Instance;
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        private readonly List<int> _events = [];

        public List<int> Events
        {
            get
            {
                lock (_events)
                {
                    return [.. _events];
                }
            }
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            lock (_events)
            {
                _events.Add(eventId.Id);
            }
        }
    }

    // Models a purge still running when the host stops: blocks until the stopping token fires, then cancels.
    private static Either<EncinaError, int> BlockUntilCancelled(CancellationToken token)
    {
        token.WaitHandle.WaitOne();
        token.ThrowIfCancellationRequested();
        return Right<EncinaError, int>(0);
    }

    private static OperationAuditOptions Enabled(int retentionDays = 30) => new()
    {
        EnableAutoPurge = true,
        RetentionDays = retentionDays,
        PurgeIntervalHours = 1
    };

    private int PurgeCalls() => _mockAuditStore.ReceivedCalls()
        .Count(c => c.GetMethodInfo().Name == nameof(IOperationAuditStore.PurgeEntriesAsync));

    private static async Task AdvanceUntilAsync(FakeTimeProvider clock, Func<bool> condition)
    {
        for (var i = 0; i < 300 && !condition(); i++)
        {
            clock.Advance(TimeSpan.FromHours(1));
            await Task.Delay(10);
        }

        condition().ShouldBeTrue("the retention service did not reach the expected state in time");
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var i = 0; i < 300 && !condition(); i++)
        {
            await Task.Delay(10);
        }

        condition().ShouldBeTrue("the expected log entry was not written in time");
    }

    #region Disabled Service Tests

    [Fact]
    public async Task ExecuteAsync_WhenAutoPurgeDisabled_ShouldExitImmediately()
    {
        // Arrange
        var options = Options.Create(new OperationAuditOptions { EnableAutoPurge = false });
        var service = new OperationAuditRetentionService(_scopeFactory, options, _logger);
        using var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(100); // Give service time to start
        await service.StopAsync(cts.Token);

        // Assert - PurgeEntriesAsync should never be called
        PurgeCalls().ShouldBe(0);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullScopeFactory_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new OperationAuditOptions());

        // Act
        var act = () => new OperationAuditRetentionService(null!, options, _logger);

        // Assert
        Should.Throw<ArgumentNullException>(act)
                .ParamName.ShouldBe("scopeFactory");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new OperationAuditRetentionService(_scopeFactory, null!, _logger);

        // Assert
        Should.Throw<ArgumentNullException>(act)
                .ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new OperationAuditOptions());

        // Act
        var act = () => new OperationAuditRetentionService(_scopeFactory, options, null!);

        // Assert
        Should.Throw<ArgumentNullException>(act)
                .ParamName.ShouldBe("logger");
    }

    [Fact]
    public async Task Constructor_WithNullTimeProvider_UsesTheSystemClockAndDoesNotPurgeBeforeTheInterval()
    {
        // Arrange
        var options = Options.Create(Enabled());
        using var service = new OperationAuditRetentionService(_scopeFactory, options, _logger, null);

        // Act
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(50);
        await service.StopAsync(CancellationToken.None);

        // Assert: the first purge is one interval (1 hour of real time) away
        PurgeCalls().ShouldBe(0);
    }

    #endregion

    #region Cutoff Date Tests

    [Theory]
    [InlineData(7)]     // 1 week
    [InlineData(30)]    // 1 month
    [InlineData(365)]   // 1 year
    [InlineData(2555)]  // 7 years (SOX)
    public async Task ExecuteAsync_PurgesEntriesOlderThanNowMinusRetentionDays(int retentionDays)
    {
        // Arrange
        var clock = new FakeTimeProvider(Start);
        var purgedWith = new List<DateTime>();
        var calledAt = new List<DateTime>();
        _mockAuditStore.PurgeEntriesAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                lock (purgedWith)
                {
                    purgedWith.Add(call.Arg<DateTime>());
                    calledAt.Add(clock.GetUtcNow().UtcDateTime);
                }

                return Right<EncinaError, int>(0);
            });
        using var service = new OperationAuditRetentionService(_scopeFactory, Options.Create(Enabled(retentionDays)), _logger, clock);

        // Act
        await service.StartAsync(CancellationToken.None);
        await AdvanceUntilAsync(clock, () => { lock (purgedWith) { return purgedWith.Count > 0; } });
        await service.StopAsync(CancellationToken.None);

        // Assert: cutoff + RetentionDays is the clock reading the service took (Start plus a whole number of
        // one-hour advances), at most a few advances before the store call. A sign mutation or an off-by-one-day
        // mutation moves it by at least 24 hours and fails.
        DateTime cutoff;
        DateTime at;
        lock (purgedWith)
        {
            cutoff = purgedWith[0];
            at = calledAt[0];
        }

        cutoff.Kind.ShouldBe(DateTimeKind.Utc);
        var reading = cutoff.AddDays(retentionDays);
        (reading - Start.UtcDateTime).TotalHours.ShouldBeGreaterThanOrEqualTo(1);
        ((reading - Start.UtcDateTime).TotalHours % 1).ShouldBe(0);
        (at - reading).TotalHours.ShouldBeInRange(0, 3);
    }

    #endregion

    #region Purge Run Tests

    [Fact]
    public async Task ExecuteAsync_EveryPurgeRun_ResolvesTheScopedStoreFromItsOwnScope()
    {
        // Arrange: a scoped store (as every database provider registers it) and a fake clock
        var clock = new FakeTimeProvider(Start);
        var resolutions = 0;
        _mockAuditStore.PurgeEntriesAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(0));

        var services = new ServiceCollection();
        services.AddScoped(_ =>
        {
            Interlocked.Increment(ref resolutions);
            return _mockAuditStore;
        });
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        using var service = new OperationAuditRetentionService(
            provider.GetRequiredService<IServiceScopeFactory>(), Options.Create(Enabled()), _logger, clock);

        // Act
        await service.StartAsync(CancellationToken.None);
        await AdvanceUntilAsync(clock, () => PurgeCalls() >= 1);
        await service.StopAsync(CancellationToken.None);

        // Assert: exactly one store resolution (one scope) per purge run
        Volatile.Read(ref resolutions).ShouldBe(PurgeCalls());
    }

    [Fact]
    public async Task ExecuteAsync_WhenEntriesWerePurged_LogsCompletion()
    {
        // Arrange
        var clock = new FakeTimeProvider(Start);
        var logger = new CapturingLogger<OperationAuditRetentionService>();
        _mockAuditStore.PurgeEntriesAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(3));
        using var service = new OperationAuditRetentionService(_scopeFactory, Options.Create(Enabled()), logger, clock);

        // Act
        await service.StartAsync(CancellationToken.None);
        await AdvanceUntilAsync(clock, () => logger.Events.Contains(PurgeCompletedEvent));
        await service.StopAsync(CancellationToken.None);

        // Assert
        logger.Events.ShouldNotContain(NothingToPurgeEvent);
        logger.Events.ShouldNotContain(PurgeFailedEvent);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNothingWasPurged_LogsNothingToPurge()
    {
        // Arrange
        var clock = new FakeTimeProvider(Start);
        var logger = new CapturingLogger<OperationAuditRetentionService>();
        _mockAuditStore.PurgeEntriesAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(0));
        using var service = new OperationAuditRetentionService(_scopeFactory, Options.Create(Enabled()), logger, clock);

        // Act
        await service.StartAsync(CancellationToken.None);
        await AdvanceUntilAsync(clock, () => logger.Events.Contains(NothingToPurgeEvent));
        await service.StopAsync(CancellationToken.None);

        // Assert
        logger.Events.ShouldNotContain(PurgeCompletedEvent);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ExecuteAsync_WhenTheStoreReturnsLeft_ReportsFailureNotSuccessAndKeepsRunning()
    {
        // Arrange: the first run fails with a Left, the next one succeeds
        var clock = new FakeTimeProvider(Start);
        var logger = new CapturingLogger<OperationAuditRetentionService>();
        _mockAuditStore.PurgeEntriesAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(
                Left<EncinaError, int>(EncinaErrors.Create("audit.store_failure", "store failed")),
                Right<EncinaError, int>(2));
        using var service = new OperationAuditRetentionService(_scopeFactory, Options.Create(Enabled()), logger, clock);

        // Act
        await service.StartAsync(CancellationToken.None);
        await AdvanceUntilAsync(clock, () => logger.Events.Contains(PurgeCompletedEvent));
        await service.StopAsync(CancellationToken.None);

        // Assert: the Left is logged as a failure (never as completion) and the loop survived it
        var events = logger.Events;
        events.ShouldContain(PurgeFailedEvent);
        events.IndexOf(PurgeFailedEvent).ShouldBeLessThan(events.IndexOf(PurgeCompletedEvent));
        events.Count(e => e == PurgeCompletedEvent).ShouldBe(1);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTheStoreReturnsLeftOnly_NeverLogsCompletion()
    {
        // Arrange
        var clock = new FakeTimeProvider(Start);
        var logger = new CapturingLogger<OperationAuditRetentionService>();
        _mockAuditStore.PurgeEntriesAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, int>(EncinaErrors.Create("audit.store_failure", "store failed")));
        using var service = new OperationAuditRetentionService(_scopeFactory, Options.Create(Enabled()), logger, clock);

        // Act
        await service.StartAsync(CancellationToken.None);
        await AdvanceUntilAsync(clock, () => logger.Events.Contains(PurgeFailedEvent));
        await service.StopAsync(CancellationToken.None);

        // Assert
        logger.Events.ShouldNotContain(PurgeCompletedEvent);
        logger.Events.ShouldNotContain(NothingToPurgeEvent);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTheStoreThrows_LogsTheErrorAndKeepsRunning()
    {
        // Arrange: the first run throws, the next one succeeds
        var clock = new FakeTimeProvider(Start);
        var logger = new CapturingLogger<OperationAuditRetentionService>();
        var calls = 0;
        _mockAuditStore.PurgeEntriesAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(_ => Interlocked.Increment(ref calls) == 1
                ? throw new InvalidOperationException("boom")
                : Right<EncinaError, int>(1));
        using var service = new OperationAuditRetentionService(_scopeFactory, Options.Create(Enabled()), logger, clock);

        // Act
        await service.StartAsync(CancellationToken.None);
        await AdvanceUntilAsync(clock, () => logger.Events.Contains(PurgeCompletedEvent));
        await service.StopAsync(CancellationToken.None);

        // Assert
        logger.Events.ShouldContain(PurgeErrorEvent);
        logger.Events.IndexOf(PurgeErrorEvent).ShouldBeLessThan(logger.Events.IndexOf(PurgeCompletedEvent));
    }

    [Fact]
    public async Task ExecuteAsync_WhenStoppedMidPurge_LogsCancellationAndNeitherSuccessNorFailure()
    {
        // Arrange: the store blocks until the service's stopping token is cancelled
        var clock = new FakeTimeProvider(Start);
        var logger = new CapturingLogger<OperationAuditRetentionService>();
        using var storeEntered = new ManualResetEventSlim(false);
        _mockAuditStore.PurgeEntriesAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                storeEntered.Set();
                return BlockUntilCancelled(call.Arg<CancellationToken>());
            });
        using var service = new OperationAuditRetentionService(_scopeFactory, Options.Create(Enabled()), logger, clock);

        // Act: the clock advances on a pool thread because the timer callback may run the blocked store call inline
        await service.StartAsync(CancellationToken.None);
        var advancing = Task.Run(() =>
        {
            while (!storeEntered.IsSet)
            {
                clock.Advance(TimeSpan.FromHours(1));
                Thread.Sleep(10);
            }
        });
        storeEntered.Wait(TimeSpan.FromSeconds(30)).ShouldBeTrue("the purge never reached the store");
        await service.StopAsync(CancellationToken.None);
        await advancing.WaitAsync(TimeSpan.FromSeconds(30));
        await WaitUntilAsync(() => logger.Events.Contains(PurgeCancelledEvent));

        // Assert
        logger.Events.ShouldNotContain(PurgeCompletedEvent);
        logger.Events.ShouldNotContain(NothingToPurgeEvent);
        logger.Events.ShouldNotContain(PurgeFailedEvent);
        logger.Events.ShouldNotContain(PurgeErrorEvent);
    }

    [Fact]
    public async Task Service_WhenAutoPurgeDisabled_ShouldNotCallPurge()
    {
        // Arrange
        var options = Options.Create(new OperationAuditOptions
        {
            EnableAutoPurge = false
        });

        var service = new OperationAuditRetentionService(_scopeFactory, options, _logger);
        using var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(150); // Wait a bit
        await service.StopAsync(cts.Token);

        // Assert - Purge should never be called
        PurgeCalls().ShouldBe(0);
    }

    #endregion

    #region Service Lifecycle Tests

    [Fact]
    public async Task StartAsync_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(new OperationAuditOptions { EnableAutoPurge = false });
        var service = new OperationAuditRetentionService(_scopeFactory, options, _logger);
        using var cts = new CancellationTokenSource();

        // Act & Assert
        await Should.NotThrowAsync(async () => await service.StartAsync(cts.Token));

        await service.StopAsync(cts.Token);
    }

    [Fact]
    public async Task StopAsync_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(new OperationAuditOptions { EnableAutoPurge = false });
        var service = new OperationAuditRetentionService(_scopeFactory, options, _logger);
        using var cts = new CancellationTokenSource();

        await service.StartAsync(cts.Token);

        // Act & Assert
        await Should.NotThrowAsync(async () => await service.StopAsync(cts.Token));
    }

    [Fact]
    public async Task Service_WithCancellation_ShouldStopGracefully()
    {
        // Arrange
        var options = Options.Create(new OperationAuditOptions
        {
            EnableAutoPurge = true,
            PurgeIntervalHours = 24
        });

        _mockAuditStore.PurgeEntriesAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(0));

        var service = new OperationAuditRetentionService(_scopeFactory, options, _logger);
        using var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);
        cts.Cancel();

        // Assert - Should not throw
        await Should.NotThrowAsync(async () => await service.StopAsync(CancellationToken.None));
    }

    #endregion
}

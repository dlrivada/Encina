using Encina.Sharding;
using Encina.Sharding.TimeBased;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Sharding.TimeBased;

/// <summary>
/// Unit tests for <see cref="TierTransitionScheduler"/>.
/// Verifies tier transition execution, auto-shard creation, and scheduler lifecycle.
/// </summary>
public sealed class TierTransitionSchedulerTests
{
    #region Test Helpers

    private static ShardTierInfo CreateTierInfo(
        string shardId,
        ShardTier tier,
        DateOnly periodStart,
        DateOnly periodEnd)
    {
        return new ShardTierInfo(
            shardId,
            tier,
            periodStart,
            periodEnd,
            tier != ShardTier.Hot,
            $"Server=test;Database={shardId}",
            DateTime.UtcNow);
    }

    private static (TierTransitionScheduler Scheduler, ITierStore TierStore, IShardArchiver Archiver, FakeTimeProvider TimeProvider) CreateTestFixture(
        TimeBasedShardingOptions? options = null)
    {
        var tierStore = Substitute.For<ITierStore>();
        var archiver = Substitute.For<IShardArchiver>();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 2, 15, 12, 0, 0, TimeSpan.Zero));

        var services = new ServiceCollection();
        services.AddScoped(_ => tierStore);
        services.AddScoped(_ => archiver);
        var sp = services.BuildServiceProvider();

        var shardingOptions = options ?? new TimeBasedShardingOptions
        {
            Enabled = true,
            CheckInterval = TimeSpan.FromMilliseconds(50),
            Period = ShardPeriod.Monthly,
            ShardIdPrefix = "orders",
            ConnectionStringTemplate = "Server=hot;Database=orders_{0}",
            Transitions =
            [
                new TierTransition(ShardTier.Hot, ShardTier.Warm, TimeSpan.FromDays(30)),
                new TierTransition(ShardTier.Warm, ShardTier.Cold, TimeSpan.FromDays(90)),
            ],
        };

        var scheduler = new TierTransitionScheduler(
            sp,
            Options.Create(shardingOptions),
            NullLogger<TierTransitionScheduler>.Instance,
            timeProvider);

        return (scheduler, tierStore, archiver, timeProvider);
    }

    #endregion

    #region Constructor

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new TierTransitionScheduler(
                null!,
                Options.Create(new TimeBasedShardingOptions()),
                NullLogger<TierTransitionScheduler>.Instance));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var sp = new ServiceCollection().BuildServiceProvider();

        Should.Throw<ArgumentNullException>(() =>
            new TierTransitionScheduler(
                sp,
                null!,
                NullLogger<TierTransitionScheduler>.Instance));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var sp = new ServiceCollection().BuildServiceProvider();

        Should.Throw<ArgumentNullException>(() =>
            new TierTransitionScheduler(
                sp,
                Options.Create(new TimeBasedShardingOptions()),
                null!));
    }

    #endregion

    #region ExecuteAsync — Disabled

    [Fact]
    public async Task ExecuteAsync_Disabled_ExitsImmediately()
    {
        var (scheduler, tierStore, _, _) = CreateTestFixture(
            new TimeBasedShardingOptions { Enabled = false });

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        await scheduler.StartAsync(cts.Token);
        await Task.Delay(100);
        await scheduler.StopAsync(CancellationToken.None);

        // Should not query tier store when disabled
        await tierStore.DidNotReceive()
            .GetShardsDueForTransitionAsync(Arg.Any<ShardTier>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Tier Transitions

    [Fact]
    public async Task ExecuteTransitions_DueShards_TransitionsViaArchiver()
    {
        var (scheduler, tierStore, archiver, timeProvider) = CreateTestFixture();

        var dueShard = CreateTierInfo("orders-2025-12", ShardTier.Hot,
            new DateOnly(2025, 12, 1), new DateOnly(2026, 1, 1));

        tierStore.GetShardsDueForTransitionAsync(ShardTier.Hot, TimeSpan.FromDays(30), Arg.Any<CancellationToken>())
            .Returns(new List<ShardTierInfo> { dueShard });
        tierStore.GetShardsDueForTransitionAsync(ShardTier.Warm, TimeSpan.FromDays(90), Arg.Any<CancellationToken>())
            .Returns(new List<ShardTierInfo>());
        tierStore.GetTierInfoAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ShardTierInfo?)null); // For auto-shard creation check

        archiver.TransitionTierAsync("orders-2025-12", ShardTier.Warm, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(unit));

        // Start scheduler and allow the background task to enter the Task.Delay loop
        using var cts = new CancellationTokenSource();
        await scheduler.StartAsync(cts.Token);
        await Task.Yield();
        await Task.Delay(100);

        // Advance fake time past the CheckInterval to trigger a tick
        timeProvider.Advance(TimeSpan.FromMilliseconds(100));
        await Task.Delay(500); // Allow the tick to complete processing

        await cts.CancelAsync();
        await scheduler.StopAsync(CancellationToken.None);

        await archiver.Received().TransitionTierAsync(
            "orders-2025-12", ShardTier.Warm, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Auto-Shard Creation

    [Fact]
    public async Task AutoShardCreation_WithinLeadTime_CreatesShard()
    {
        // Set time to Feb 28 (2 days before March, within 1-day lead time)
        var timeProvider = new FakeTimeProvider(
            new DateTimeOffset(2026, 2, 28, 12, 0, 0, TimeSpan.Zero));

        var tierStore = Substitute.For<ITierStore>();
        var archiver = Substitute.For<IShardArchiver>();

        var services = new ServiceCollection();
        services.AddScoped(_ => tierStore);
        services.AddScoped(_ => archiver);
        var sp = services.BuildServiceProvider();

        var options = new TimeBasedShardingOptions
        {
            Enabled = true,
            CheckInterval = TimeSpan.FromMilliseconds(50),
            Period = ShardPeriod.Monthly,
            ShardIdPrefix = "orders",
            ConnectionStringTemplate = "Server=hot;Database=orders_{0}",
            ShardCreationLeadTime = TimeSpan.FromDays(3), // 3-day lead time
            Transitions = [],
            EnableAutoShardCreation = true,
        };

        var scheduler = new TierTransitionScheduler(
            sp,
            Options.Create(options),
            NullLogger<TierTransitionScheduler>.Instance,
            timeProvider);

        // The next shard (orders-2026-03) does not exist yet
        tierStore.GetTierInfoAsync("orders-2026-03", Arg.Any<CancellationToken>())
            .Returns((ShardTierInfo?)null);

        using var cts = new CancellationTokenSource();
        await scheduler.StartAsync(cts.Token);
        await Task.Yield();
        await Task.Delay(100);

        // Advance time past CheckInterval to trigger a tick
        timeProvider.Advance(TimeSpan.FromMilliseconds(100));
        await Task.Delay(500); // Allow the tick to complete processing

        await cts.CancelAsync();
        await scheduler.StopAsync(CancellationToken.None);

        await tierStore.Received().AddShardAsync(
            Arg.Is<ShardTierInfo>(t =>
                t.ShardId == "orders-2026-03" &&
                t.CurrentTier == ShardTier.Hot &&
                !t.IsReadOnly),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AutoShardCreation_NoTemplate_SkipsCreation()
    {
        var (scheduler, tierStore, _, timeProvider) = CreateTestFixture(
            new TimeBasedShardingOptions
            {
                Enabled = true,
                CheckInterval = TimeSpan.FromMilliseconds(50),
                Period = ShardPeriod.Monthly,
                ConnectionStringTemplate = null,
                Transitions = [],
                EnableAutoShardCreation = true,
            });

        tierStore.GetShardsDueForTransitionAsync(Arg.Any<ShardTier>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new List<ShardTierInfo>());

        using var cts = new CancellationTokenSource();
        await scheduler.StartAsync(cts.Token);
        await Task.Yield();
        await Task.Delay(100);

        timeProvider.Advance(TimeSpan.FromMilliseconds(100));
        await Task.Delay(500);

        await cts.CancelAsync();
        await scheduler.StopAsync(CancellationToken.None);

        await tierStore.DidNotReceive().AddShardAsync(
            Arg.Any<ShardTierInfo>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AutoShardCreation_ShardAlreadyExists_SkipsCreation()
    {
        // Set time close to end of period
        var timeProvider = new FakeTimeProvider(
            new DateTimeOffset(2026, 2, 28, 12, 0, 0, TimeSpan.Zero));

        var tierStore = Substitute.For<ITierStore>();
        var archiver = Substitute.For<IShardArchiver>();

        var services = new ServiceCollection();
        services.AddScoped(_ => tierStore);
        services.AddScoped(_ => archiver);
        var sp = services.BuildServiceProvider();

        var options = new TimeBasedShardingOptions
        {
            Enabled = true,
            CheckInterval = TimeSpan.FromMilliseconds(50),
            Period = ShardPeriod.Monthly,
            ShardIdPrefix = "orders",
            ConnectionStringTemplate = "Server=hot;Database=orders_{0}",
            ShardCreationLeadTime = TimeSpan.FromDays(3),
            Transitions = [],
            EnableAutoShardCreation = true,
        };

        var scheduler = new TierTransitionScheduler(
            sp,
            Options.Create(options),
            NullLogger<TierTransitionScheduler>.Instance,
            timeProvider);

        // Shard already exists
        var existingShard = CreateTierInfo("orders-2026-03", ShardTier.Hot,
            new DateOnly(2026, 3, 1), new DateOnly(2026, 4, 1));
        tierStore.GetTierInfoAsync("orders-2026-03", Arg.Any<CancellationToken>())
            .Returns(existingShard);

        using var cts = new CancellationTokenSource();
        await scheduler.StartAsync(cts.Token);
        await Task.Yield();
        await Task.Delay(100);

        timeProvider.Advance(TimeSpan.FromMilliseconds(100));
        await Task.Delay(500);

        await cts.CancelAsync();
        await scheduler.StopAsync(CancellationToken.None);

        await tierStore.DidNotReceive().AddShardAsync(
            Arg.Any<ShardTierInfo>(), Arg.Any<CancellationToken>());
    }

    #endregion
}

using Encina.Messaging.Health;
using Encina.Sharding.TimeBased;
using Encina.Sharding.TimeBased.Health;
using Microsoft.Extensions.Time.Testing;

namespace Encina.UnitTests.Sharding.TimeBased.Health;

/// <summary>
/// Unit tests for <see cref="ShardCreationHealthCheck"/>.
/// Verifies that missing current-period shards are Unhealthy and missing
/// next-period shards within the warning window are Degraded.
/// </summary>
public sealed class ShardCreationHealthCheckTests
{
    #region Test Helpers

    private static ShardTierInfo CreateTierInfo(
        string shardId,
        DateOnly periodStart,
        DateOnly periodEnd,
        ShardTier tier = ShardTier.Hot)
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

    private static (ShardCreationHealthCheck HealthCheck, ITierStore TierStore, FakeTimeProvider TimeProvider) CreateTestFixture(
        ShardCreationHealthCheckOptions? options = null)
    {
        var tierStore = Substitute.For<ITierStore>();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 2, 15, 0, 0, 0, TimeSpan.Zero));
        var healthCheck = new ShardCreationHealthCheck(tierStore, options, timeProvider);
        return (healthCheck, tierStore, timeProvider);
    }

    private static async Task<HealthCheckResult> RunCheckAsync(
        ShardCreationHealthCheck healthCheck)
    {
        return await healthCheck.CheckHealthAsync();
    }

    #endregion

    #region Constructor

    [Fact]
    public void Constructor_NullTierStore_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ShardCreationHealthCheck(null!));
    }

    [Fact]
    public void DefaultName_IsCorrect()
    {
        ShardCreationHealthCheck.DefaultName.ShouldBe("encina-shard-creation");
    }

    #endregion

    #region Healthy

    [Fact]
    public async Task CheckHealth_CurrentShardExists_ReturnsHealthy()
    {
        var (healthCheck, tierStore, _) = CreateTestFixture(new ShardCreationHealthCheckOptions
        {
            Period = ShardPeriod.Monthly,
            ShardIdPrefix = "orders",
            WarningWindowDays = 5,
        });

        // Current period: Feb 2026
        var currentShard = CreateTierInfo("orders-2026-02",
            new DateOnly(2026, 2, 1), new DateOnly(2026, 3, 1));

        tierStore.GetTierInfoAsync("orders-2026-02", Arg.Any<CancellationToken>())
            .Returns(currentShard);

        var result = await RunCheckAsync(healthCheck);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealth_HealthyResult_IncludesShardData()
    {
        var (healthCheck, tierStore, _) = CreateTestFixture(new ShardCreationHealthCheckOptions
        {
            Period = ShardPeriod.Monthly,
            ShardIdPrefix = "orders",
            WarningWindowDays = 5,
        });

        var currentShard = CreateTierInfo("orders-2026-02",
            new DateOnly(2026, 2, 1), new DateOnly(2026, 3, 1));

        tierStore.GetTierInfoAsync("orders-2026-02", Arg.Any<CancellationToken>())
            .Returns(currentShard);

        var result = await RunCheckAsync(healthCheck);

        result.Data.ShouldContainKey("current_shard_id");
        result.Data["current_shard_id"].ShouldBe("orders-2026-02");
    }

    #endregion

    #region Unhealthy

    [Fact]
    public async Task CheckHealth_CurrentShardMissing_ReturnsUnhealthy()
    {
        var (healthCheck, tierStore, _) = CreateTestFixture(new ShardCreationHealthCheckOptions
        {
            Period = ShardPeriod.Monthly,
            ShardIdPrefix = "orders",
        });

        tierStore.GetTierInfoAsync("orders-2026-02", Arg.Any<CancellationToken>())
            .Returns((ShardTierInfo?)null);

        var result = await RunCheckAsync(healthCheck);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("orders-2026-02");
        result.Data.ShouldContainKey("missing_shard_id");
    }

    #endregion

    #region Degraded

    [Fact]
    public async Task CheckHealth_NextShardMissing_WithinWarningWindow_ReturnsDegraded()
    {
        // Set time to Feb 27 (3 days from March 1, within 5-day warning)
        var tierStore = Substitute.For<ITierStore>();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 2, 27, 0, 0, 0, TimeSpan.Zero));
        var healthCheck = new ShardCreationHealthCheck(tierStore,
            new ShardCreationHealthCheckOptions
            {
                Period = ShardPeriod.Monthly,
                ShardIdPrefix = "orders",
                WarningWindowDays = 5,
            },
            timeProvider);

        // Current shard exists
        var currentShard = CreateTierInfo("orders-2026-02",
            new DateOnly(2026, 2, 1), new DateOnly(2026, 3, 1));
        tierStore.GetTierInfoAsync("orders-2026-02", Arg.Any<CancellationToken>())
            .Returns(currentShard);

        // Next shard does NOT exist
        tierStore.GetTierInfoAsync("orders-2026-03", Arg.Any<CancellationToken>())
            .Returns((ShardTierInfo?)null);

        var result = await RunCheckAsync(healthCheck);

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldContain("orders-2026-03");
        result.Data.ShouldContainKey("days_until_period_end");
    }

    [Fact]
    public async Task CheckHealth_NextShardExists_WithinWarningWindow_ReturnsHealthy()
    {
        var tierStore = Substitute.For<ITierStore>();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 2, 27, 0, 0, 0, TimeSpan.Zero));
        var healthCheck = new ShardCreationHealthCheck(tierStore,
            new ShardCreationHealthCheckOptions
            {
                Period = ShardPeriod.Monthly,
                ShardIdPrefix = "orders",
                WarningWindowDays = 5,
            },
            timeProvider);

        var currentShard = CreateTierInfo("orders-2026-02",
            new DateOnly(2026, 2, 1), new DateOnly(2026, 3, 1));
        var nextShard = CreateTierInfo("orders-2026-03",
            new DateOnly(2026, 3, 1), new DateOnly(2026, 4, 1));

        tierStore.GetTierInfoAsync("orders-2026-02", Arg.Any<CancellationToken>())
            .Returns(currentShard);
        tierStore.GetTierInfoAsync("orders-2026-03", Arg.Any<CancellationToken>())
            .Returns(nextShard);

        var result = await RunCheckAsync(healthCheck);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealth_NotInWarningWindow_DoesNotCheckNextShard()
    {
        // Feb 15 is 13 days from March 1, not within 5-day warning
        var (healthCheck, tierStore, _) = CreateTestFixture(new ShardCreationHealthCheckOptions
        {
            Period = ShardPeriod.Monthly,
            ShardIdPrefix = "orders",
            WarningWindowDays = 5,
        });

        var currentShard = CreateTierInfo("orders-2026-02",
            new DateOnly(2026, 2, 1), new DateOnly(2026, 3, 1));

        tierStore.GetTierInfoAsync("orders-2026-02", Arg.Any<CancellationToken>())
            .Returns(currentShard);

        var result = await RunCheckAsync(healthCheck);

        result.Status.ShouldBe(HealthStatus.Healthy);

        // Should NOT have checked the next shard since we're outside the warning window
        await tierStore.DidNotReceive()
            .GetTierInfoAsync("orders-2026-03", Arg.Any<CancellationToken>());
    }

    #endregion
}

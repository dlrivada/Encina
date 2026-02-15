using Encina.Messaging.Health;
using Encina.Sharding.TimeBased;
using Encina.Sharding.TimeBased.Health;
using Microsoft.Extensions.Time.Testing;

namespace Encina.UnitTests.Sharding.TimeBased.Health;

/// <summary>
/// Unit tests for <see cref="TierTransitionHealthCheck"/>.
/// Verifies health status based on shard age thresholds and tier distribution.
/// </summary>
public sealed class TierTransitionHealthCheckTests
{
    #region Test Helpers

    private static ShardTierInfo CreateTierInfo(
        string shardId,
        ShardTier tier,
        DateOnly periodEnd)
    {
        return new ShardTierInfo(
            shardId,
            tier,
            periodEnd.AddMonths(-1),
            periodEnd,
            tier != ShardTier.Hot,
            $"Server=test;Database={shardId}",
            DateTime.UtcNow);
    }

    private static (TierTransitionHealthCheck HealthCheck, ITierStore TierStore, FakeTimeProvider TimeProvider) CreateTestFixture(
        TierTransitionHealthCheckOptions? options = null)
    {
        var tierStore = Substitute.For<ITierStore>();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 2, 15, 0, 0, 0, TimeSpan.Zero));
        var healthCheck = new TierTransitionHealthCheck(tierStore, options, timeProvider);
        return (healthCheck, tierStore, timeProvider);
    }

    private static async Task<HealthCheckResult> RunCheckAsync(
        TierTransitionHealthCheck healthCheck)
    {
        return await healthCheck.CheckHealthAsync();
    }

    #endregion

    #region Constructor

    [Fact]
    public void Constructor_NullTierStore_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new TierTransitionHealthCheck(null!));
    }

    [Fact]
    public void DefaultName_IsCorrect()
    {
        TierTransitionHealthCheck.DefaultName.ShouldBe("encina-tier-transition");
    }

    #endregion

    #region Healthy

    [Fact]
    public async Task CheckHealth_NoShards_ReturnsHealthy()
    {
        var (healthCheck, tierStore, _) = CreateTestFixture();
        tierStore.GetAllTierInfoAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ShardTierInfo>());

        var result = await RunCheckAsync(healthCheck);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealth_AllShardsWithinThresholds_ReturnsHealthy()
    {
        var (healthCheck, tierStore, _) = CreateTestFixture(new TierTransitionHealthCheckOptions
        {
            MaxExpectedHotAgeDays = 35,
        });

        // Hot shard with period ending 10 days ago (within 35 day threshold)
        var shards = new List<ShardTierInfo>
        {
            CreateTierInfo("s1", ShardTier.Hot, new DateOnly(2026, 2, 5)),
        };

        tierStore.GetAllTierInfoAsync(Arg.Any<CancellationToken>())
            .Returns(shards);

        var result = await RunCheckAsync(healthCheck);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealth_ArchivedShards_Ignored()
    {
        var (healthCheck, tierStore, _) = CreateTestFixture();

        // Archived shard with very old period — should be ignored
        var shards = new List<ShardTierInfo>
        {
            CreateTierInfo("s1", ShardTier.Archived, new DateOnly(2020, 1, 1)),
        };

        tierStore.GetAllTierInfoAsync(Arg.Any<CancellationToken>())
            .Returns(shards);

        var result = await RunCheckAsync(healthCheck);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealth_ShardPeriodNotEndedYet_IsHealthy()
    {
        var (healthCheck, tierStore, _) = CreateTestFixture();

        // Hot shard whose period hasn't ended yet (future end date)
        var shards = new List<ShardTierInfo>
        {
            CreateTierInfo("s1", ShardTier.Hot, new DateOnly(2026, 3, 1)),
        };

        tierStore.GetAllTierInfoAsync(Arg.Any<CancellationToken>())
            .Returns(shards);

        var result = await RunCheckAsync(healthCheck);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    #endregion

    #region Degraded

    [Fact]
    public async Task CheckHealth_OverdueHotShard_ReturnsDegraded()
    {
        var (healthCheck, tierStore, _) = CreateTestFixture(new TierTransitionHealthCheckOptions
        {
            MaxExpectedHotAgeDays = 35,
            UnhealthyMultiplier = 2.0,
        });

        // Hot shard with period ending 40 days ago (exceeds 35, but not 70)
        var shards = new List<ShardTierInfo>
        {
            CreateTierInfo("s1", ShardTier.Hot, new DateOnly(2026, 1, 6)),
        };

        tierStore.GetAllTierInfoAsync(Arg.Any<CancellationToken>())
            .Returns(shards);

        var result = await RunCheckAsync(healthCheck);

        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    #endregion

    #region Unhealthy

    [Fact]
    public async Task CheckHealth_CriticallyOverdueShard_ReturnsUnhealthy()
    {
        var (healthCheck, tierStore, _) = CreateTestFixture(new TierTransitionHealthCheckOptions
        {
            MaxExpectedHotAgeDays = 35,
            UnhealthyMultiplier = 2.0,
        });

        // Hot shard with period ending 80 days ago (exceeds 35 * 2 = 70)
        var shards = new List<ShardTierInfo>
        {
            CreateTierInfo("s1", ShardTier.Hot, new DateOnly(2025, 11, 27)),
        };

        tierStore.GetAllTierInfoAsync(Arg.Any<CancellationToken>())
            .Returns(shards);

        var result = await RunCheckAsync(healthCheck);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    #endregion

    #region Data Dictionary

    [Fact]
    public async Task CheckHealth_IncludesTierDistribution()
    {
        var (healthCheck, tierStore, _) = CreateTestFixture();

        var shards = new List<ShardTierInfo>
        {
            CreateTierInfo("s1", ShardTier.Hot, new DateOnly(2026, 3, 1)),
            CreateTierInfo("s2", ShardTier.Warm, new DateOnly(2026, 2, 1)),
        };

        tierStore.GetAllTierInfoAsync(Arg.Any<CancellationToken>())
            .Returns(shards);

        var result = await RunCheckAsync(healthCheck);

        result.Data.ShouldContainKey("total_shards");
        result.Data["total_shards"].ShouldBe(2);
        result.Data.ShouldContainKey("tier_distribution");
    }

    #endregion
}

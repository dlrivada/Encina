using Encina.Sharding;
using Encina.Sharding.Routing;
using Encina.Sharding.TimeBased;

namespace Encina.GuardTests.Sharding.TimeBased;

/// <summary>
/// Guard clause tests for time-based sharding types.
/// Verifies null/invalid parameter handling across constructors and methods.
/// </summary>
public sealed class TimeBasedShardingGuardTests
{
    #region ShardTierInfo Guards

    /// <summary>
    /// Verifies that <see cref="ShardTierInfo"/> rejects null or whitespace shard IDs.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ShardTierInfo_NullOrWhitespaceShardId_ThrowsArgumentException(string? shardId)
    {
        Should.Throw<ArgumentException>(() =>
            new ShardTierInfo(
                shardId!,
                ShardTier.Hot,
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 2, 1),
                false,
                "Server=test;Database=db",
                DateTime.UtcNow));
    }

    /// <summary>
    /// Verifies that <see cref="ShardTierInfo"/> rejects null or whitespace connection strings.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ShardTierInfo_NullOrWhitespaceConnectionString_ThrowsArgumentException(string? connectionString)
    {
        Should.Throw<ArgumentException>(() =>
            new ShardTierInfo(
                "shard-1",
                ShardTier.Hot,
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 2, 1),
                false,
                connectionString!,
                DateTime.UtcNow));
    }

    /// <summary>
    /// Verifies that <see cref="ShardTierInfo"/> rejects PeriodStart >= PeriodEnd.
    /// </summary>
    [Fact]
    public void ShardTierInfo_PeriodStartAfterPeriodEnd_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            new ShardTierInfo(
                "shard-1",
                ShardTier.Hot,
                new DateOnly(2026, 3, 1),
                new DateOnly(2026, 2, 1),
                false,
                "Server=test;Database=db",
                DateTime.UtcNow));
    }

    /// <summary>
    /// Verifies that <see cref="ShardTierInfo"/> rejects PeriodStart equal to PeriodEnd.
    /// </summary>
    [Fact]
    public void ShardTierInfo_PeriodStartEqualsPeriodEnd_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            new ShardTierInfo(
                "shard-1",
                ShardTier.Hot,
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 1, 1),
                false,
                "Server=test;Database=db",
                DateTime.UtcNow));
    }

    #endregion

    #region TierTransition Guards

    /// <summary>
    /// Verifies that <see cref="TierTransition"/> rejects zero age threshold.
    /// </summary>
    [Fact]
    public void TierTransition_ZeroAgeThreshold_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            new TierTransition(ShardTier.Hot, ShardTier.Warm, TimeSpan.Zero));
    }

    /// <summary>
    /// Verifies that <see cref="TierTransition"/> rejects negative age threshold.
    /// </summary>
    [Fact]
    public void TierTransition_NegativeAgeThreshold_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            new TierTransition(ShardTier.Hot, ShardTier.Warm, TimeSpan.FromDays(-1)));
    }

    /// <summary>
    /// Verifies that <see cref="TierTransition"/> rejects backwards tier progression.
    /// </summary>
    [Fact]
    public void TierTransition_BackwardsTierProgression_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            new TierTransition(ShardTier.Warm, ShardTier.Hot, TimeSpan.FromDays(30)));
    }

    /// <summary>
    /// Verifies that <see cref="TierTransition"/> rejects same-tier progression.
    /// </summary>
    [Fact]
    public void TierTransition_SameTierProgression_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            new TierTransition(ShardTier.Hot, ShardTier.Hot, TimeSpan.FromDays(30)));
    }

    /// <summary>
    /// Verifies that <see cref="TierTransition"/> accepts valid forward progression.
    /// </summary>
    [Theory]
    [InlineData(ShardTier.Hot, ShardTier.Warm)]
    [InlineData(ShardTier.Warm, ShardTier.Cold)]
    [InlineData(ShardTier.Cold, ShardTier.Archived)]
    [InlineData(ShardTier.Hot, ShardTier.Archived)] // Skip tiers
    public void TierTransition_ValidProgression_Succeeds(ShardTier from, ShardTier to)
    {
        var transition = new TierTransition(from, to, TimeSpan.FromDays(30));

        transition.FromTier.ShouldBe(from);
        transition.ToTier.ShouldBe(to);
    }

    #endregion

    #region ArchiveOptions Guards

    /// <summary>
    /// Verifies that <see cref="ArchiveOptions"/> rejects null or whitespace destination.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ArchiveOptions_NullOrWhitespaceDestination_ThrowsArgumentException(string? destination)
    {
        Should.Throw<ArgumentException>(() =>
            new ArchiveOptions(destination!));
    }

    #endregion

    #region InMemoryTierStore Guards

    /// <summary>
    /// Verifies that <see cref="InMemoryTierStore.GetTierInfoAsync"/> rejects null shard ID.
    /// </summary>
    [Fact]
    public async Task InMemoryTierStore_GetTierInfoAsync_NullShardId_ThrowsArgumentNullException()
    {
        var store = new InMemoryTierStore();

        await Should.ThrowAsync<ArgumentNullException>(() =>
            store.GetTierInfoAsync(null!));
    }

    /// <summary>
    /// Verifies that <see cref="InMemoryTierStore.UpdateTierAsync"/> rejects null shard ID.
    /// </summary>
    [Fact]
    public async Task InMemoryTierStore_UpdateTierAsync_NullShardId_ThrowsArgumentNullException()
    {
        var store = new InMemoryTierStore();

        await Should.ThrowAsync<ArgumentNullException>(() =>
            store.UpdateTierAsync(null!, ShardTier.Warm));
    }

    /// <summary>
    /// Verifies that <see cref="InMemoryTierStore.AddShardAsync"/> rejects null tier info.
    /// </summary>
    [Fact]
    public async Task InMemoryTierStore_AddShardAsync_NullTierInfo_ThrowsArgumentNullException()
    {
        var store = new InMemoryTierStore();

        await Should.ThrowAsync<ArgumentNullException>(() =>
            store.AddShardAsync(null!));
    }

    /// <summary>
    /// Verifies that <see cref="InMemoryTierStore.AddShardAsync"/> rejects duplicate shard IDs.
    /// </summary>
    [Fact]
    public async Task InMemoryTierStore_AddShardAsync_DuplicateShardId_ThrowsArgumentException()
    {
        var store = new InMemoryTierStore();
        var tierInfo = new ShardTierInfo(
            "shard-1", ShardTier.Hot,
            new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 1),
            false, "Server=test;Database=db", DateTime.UtcNow);

        await store.AddShardAsync(tierInfo);

        await Should.ThrowAsync<ArgumentException>(() =>
            store.AddShardAsync(tierInfo));
    }

    #endregion

    #region TimeBasedShardRouter Guards

    /// <summary>
    /// Verifies that <see cref="TimeBasedShardRouter"/> rejects null topology.
    /// </summary>
    [Fact]
    public void TimeBasedShardRouter_NullTopology_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new TimeBasedShardRouter(null!, Array.Empty<ShardTierInfo>(), ShardPeriod.Monthly));
    }

    /// <summary>
    /// Verifies that <see cref="TimeBasedShardRouter"/> rejects null tier infos.
    /// </summary>
    [Fact]
    public void TimeBasedShardRouter_NullTierInfos_ThrowsArgumentNullException()
    {
        var topology = new ShardTopology(Array.Empty<ShardInfo>());

        Should.Throw<ArgumentNullException>(() =>
            new TimeBasedShardRouter(topology, null!, ShardPeriod.Monthly));
    }

    /// <summary>
    /// Verifies that <see cref="TimeBasedShardRouter.GetShardTier"/> rejects null shard ID.
    /// </summary>
    [Fact]
    public void TimeBasedShardRouter_GetShardTier_NullShardId_ThrowsArgumentNullException()
    {
        var topology = new ShardTopology(Array.Empty<ShardInfo>());
        var router = new TimeBasedShardRouter(topology, Array.Empty<ShardTierInfo>(), ShardPeriod.Monthly);

        Should.Throw<ArgumentNullException>(() => router.GetShardTier(null!));
    }

    /// <summary>
    /// Verifies that <see cref="TimeBasedShardRouter.GetShardId(string)"/> rejects null key.
    /// </summary>
    [Fact]
    public void TimeBasedShardRouter_GetShardId_NullKey_ThrowsArgumentNullException()
    {
        var topology = new ShardTopology(Array.Empty<ShardInfo>());
        var router = new TimeBasedShardRouter(topology, Array.Empty<ShardTierInfo>(), ShardPeriod.Monthly);

        Should.Throw<ArgumentNullException>(() => router.GetShardId((string)null!));
    }

    /// <summary>
    /// Verifies that <see cref="TimeBasedShardRouter.GetShardConnectionString"/> rejects null shard ID.
    /// </summary>
    [Fact]
    public void TimeBasedShardRouter_GetShardConnectionString_NullShardId_ThrowsArgumentNullException()
    {
        var topology = new ShardTopology(Array.Empty<ShardInfo>());
        var router = new TimeBasedShardRouter(topology, Array.Empty<ShardTierInfo>(), ShardPeriod.Monthly);

        Should.Throw<ArgumentNullException>(() => router.GetShardConnectionString(null!));
    }

    #endregion

    #region ShardArchiver Guards

    /// <summary>
    /// Verifies that <see cref="ShardArchiver"/> rejects null tier store.
    /// </summary>
    [Fact]
    public void ShardArchiver_NullTierStore_ThrowsArgumentNullException()
    {
        var topologyProvider = Substitute.For<IShardTopologyProvider>();

        Should.Throw<ArgumentNullException>(() =>
            new ShardArchiver(null!, topologyProvider));
    }

    /// <summary>
    /// Verifies that <see cref="ShardArchiver"/> rejects null topology provider.
    /// </summary>
    [Fact]
    public void ShardArchiver_NullTopologyProvider_ThrowsArgumentNullException()
    {
        var tierStore = Substitute.For<ITierStore>();

        Should.Throw<ArgumentNullException>(() =>
            new ShardArchiver(tierStore, null!));
    }

    #endregion

    #region PeriodBoundaryCalculator Guards

    /// <summary>
    /// Verifies that <see cref="PeriodBoundaryCalculator"/> rejects invalid period values.
    /// </summary>
    [Fact]
    public void PeriodBoundaryCalculator_InvalidPeriod_ThrowsArgumentOutOfRangeException()
    {
        var date = new DateOnly(2026, 1, 1);

        Should.Throw<ArgumentOutOfRangeException>(() =>
            PeriodBoundaryCalculator.GetPeriodStart(date, (ShardPeriod)99));

        Should.Throw<ArgumentOutOfRangeException>(() =>
            PeriodBoundaryCalculator.GetPeriodEnd(date, (ShardPeriod)99));

        Should.Throw<ArgumentOutOfRangeException>(() =>
            PeriodBoundaryCalculator.GetPeriodLabel(date, (ShardPeriod)99));
    }

    #endregion
}

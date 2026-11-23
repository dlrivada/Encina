using Encina.Sharding;
using Encina.Sharding.Routing;
using Encina.Sharding.Shadow;
using Encina.Sharding.TimeBased;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.UnitTests.Core.Sharding;

/// <summary>
/// Each case runs a real routing, shadow or time-based producer through the call site that
/// creates the error and reads the declared category back with <c>GetCategory()</c>
/// (#3719, ADR-062 and its R1 amendment: configuration and topology lookups are <c>Internal</c>).
/// </summary>
public sealed class ShardingErrorCategoryTests
{
    #region Helpers

    private static ShardTopology CreateTopology(params string[] shardIds) =>
        new(shardIds.Select(id => new ShardInfo(id, $"Server=test;Database={id}")));

    private static EncinaError LeftOf<T>(Either<EncinaError, T> result)
    {
        result.IsLeft.ShouldBeTrue();
        EncinaError error = default;
        _ = result.IfLeft(e => error = e);
        return error;
    }

    private static void AssertInternal<T>(Either<EncinaError, T> result, string expectedCode) =>
        AssertCategory(result, expectedCode, ErrorCategory.Internal);

    private static void AssertCategory<T>(Either<EncinaError, T> result, string expectedCode, ErrorCategory expectedCategory)
    {
        var error = LeftOf(result);
        error.GetCode().IfSome(c => c.ShouldBe(expectedCode));
        error.GetCode().IsSome.ShouldBeTrue();
        error.GetCategory().ShouldBe(expectedCategory);
    }

    private static ShardTierInfo CreateTierInfo(string shardId, ShardTier tier, DateOnly start, DateOnly end) =>
        new(shardId, tier, start, end, tier != ShardTier.Hot, $"Server=test;Database={shardId}", DateTime.UtcNow);

    private static TimeBasedShardRouter CreateTimeBasedRouter(params ShardTierInfo[] tiers) =>
        new(
            new ShardTopology(tiers.Select(t => new ShardInfo(t.ShardId, t.ConnectionString))),
            tiers,
            ShardPeriod.Monthly);

    private static TimeBasedShardRouter CreateJanuaryRouter(ShardTier tier) =>
        CreateTimeBasedRouter(
            CreateTierInfo("orders-2026-01", tier, new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 1)));

    #endregion

    #region DirectoryShardRouter

    [Fact]
    public void DirectoryShardRouter_UnmappedKey_IsShardNotFoundInternal()
    {
        var router = new DirectoryShardRouter(CreateTopology("shard-1"), new InMemoryShardDirectoryStore());

        AssertInternal(router.GetShardId("missing"), ShardingErrorCodes.ShardNotFound);
    }

    [Fact]
    public void DirectoryShardRouter_UnmatchedPartialKey_IsPartialKeyRoutingFailedInternal()
    {
        var router = new DirectoryShardRouter(CreateTopology("shard-1"), new InMemoryShardDirectoryStore());

        AssertInternal(router.GetShardIds(new CompoundShardKey("nobody")), ShardingErrorCodes.PartialKeyRoutingFailed);
    }

    #endregion

    #region RangeShardRouter

    [Fact]
    public void RangeShardRouter_NoRanges_IsNoActiveShardsInternal()
    {
        var router = new RangeShardRouter(CreateTopology("shard-0"), new List<ShardRange>());

        AssertInternal(router.GetShardId("A"), ShardingErrorCodes.NoActiveShards);
    }

    [Fact]
    public void RangeShardRouter_KeyOutsideRanges_IsKeyOutsideRangeInternal()
    {
        var router = new RangeShardRouter(CreateTopology("shard-0"), new List<ShardRange> { new("M", "Z", "shard-0") });

        AssertInternal(router.GetShardId("A"), ShardingErrorCodes.KeyOutsideRange);
    }

    [Fact]
    public void RangeShardRouter_UnmatchedPartialKey_IsPartialKeyRoutingFailedInternal()
    {
        var router = new RangeShardRouter(CreateTopology("shard-0"), new List<ShardRange> { new("M", "Z", "shard-0") });

        AssertInternal(router.GetShardIds(new CompoundShardKey("A")), ShardingErrorCodes.PartialKeyRoutingFailed);
    }

    #endregion

    #region HashShardRouter

    [Fact]
    public void HashShardRouter_EmptyTopology_IsNoActiveShardsInternal()
    {
        var router = new HashShardRouter(CreateTopology());

        AssertInternal(router.GetShardId("any"), ShardingErrorCodes.NoActiveShards);
    }

    #endregion

    #region GeoShardRouter

    [Fact]
    public void GeoShardRouter_UnknownRegionWithExactMatch_IsRegionNotFoundInternal()
    {
        var regions = new[] { new GeoRegion("us-east", "shard-us") };
        var options = new GeoShardRouterOptions { RequireExactMatch = true };
        var router = new GeoShardRouter(CreateTopology("shard-us"), regions, _ => "unknown", options);

        AssertInternal(router.GetShardId("key"), ShardingErrorCodes.RegionNotFound);
    }

    [Fact]
    public void GeoShardRouter_UnknownRegionWithoutDefault_IsRegionNotFoundInternal()
    {
        var regions = new[] { new GeoRegion("us-east", "shard-us") };
        var router = new GeoShardRouter(CreateTopology("shard-us"), regions, _ => "unknown");

        AssertInternal(router.GetShardId("key"), ShardingErrorCodes.RegionNotFound);
    }

    #endregion

    #region ShadowShardRouterDecorator

    private static ShadowShardRouterDecorator CreateDecorator(IShardRouter shadow) =>
        new(
            Substitute.For<IShardRouter>(),
            shadow,
            new ShadowShardingOptions { ShadowTopology = CreateTopology("shadow-1") },
            NullLogger<ShadowShardRouterDecorator>.Instance);

    // A hand-written router: IShardRouter has default interface methods, which a proxy cannot configure.
    private sealed class ThrowingShardRouter : IShardRouter
    {
        public Either<EncinaError, string> GetShardId(string shardKey) =>
            throw new InvalidOperationException("shadow boom");

        public IReadOnlyList<string> GetAllShardIds() => [];

        public Either<EncinaError, string> GetShardConnectionString(string shardId) =>
            throw new InvalidOperationException("shadow boom");
    }

    [Fact]
    public async Task ShadowShardRouterDecorator_ThrowingShadowRouterByString_IsShadowRoutingFailedInternal()
    {
        var result = await CreateDecorator(new ThrowingShardRouter()).RouteShadowAsync("key", CancellationToken.None);

        AssertInternal(result, ShardingErrorCodes.ShadowRoutingFailed);
    }

    [Fact]
    public async Task ShadowShardRouterDecorator_ThrowingShadowRouterByCompoundKey_IsShadowRoutingFailedInternal()
    {
        var result = await CreateDecorator(new ThrowingShardRouter())
            .RouteShadowAsync(new CompoundShardKey("a", "b"), CancellationToken.None);

        AssertInternal(result, ShardingErrorCodes.ShadowRoutingFailed);
    }

    #endregion

    #region ShardArchiver

    private static (ShardArchiver Archiver, ITierStore Store) CreateArchiver(bool withEnforcer = false)
    {
        var store = Substitute.For<ITierStore>();
        var enforcer = withEnforcer ? Substitute.For<IReadOnlyEnforcer>() : null;
        return (new ShardArchiver(store, Substitute.For<IShardTopologyProvider>(), enforcer), store);
    }

    [Fact]
    public async Task ShardArchiver_TransitionOfUnknownShard_IsTierTransitionFailedInternal()
    {
        var (archiver, store) = CreateArchiver();
        store.UpdateTierAsync(Arg.Any<string>(), Arg.Any<ShardTier>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var result = await archiver.TransitionTierAsync("s1", ShardTier.Warm);

        AssertInternal(result, ShardingErrorCodes.TierTransitionFailed);
    }

    [Fact]
    public async Task ShardArchiver_TransitionStoreThrows_IsTierTransitionFailedInternal()
    {
        var (archiver, store) = CreateArchiver();
        store.UpdateTierAsync(Arg.Any<string>(), Arg.Any<ShardTier>(), Arg.Any<CancellationToken>())
            .Returns<Task<bool>>(_ => throw new InvalidOperationException("store down"));

        var result = await archiver.TransitionTierAsync("s1", ShardTier.Warm);

        AssertInternal(result, ShardingErrorCodes.TierTransitionFailed);
    }

    [Fact]
    public async Task ShardArchiver_DeleteOfUnknownShard_IsRetentionPolicyFailedInternal()
    {
        var (archiver, store) = CreateArchiver();
        store.UpdateTierAsync(Arg.Any<string>(), Arg.Any<ShardTier>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var result = await archiver.DeleteShardDataAsync("s1");

        AssertInternal(result, ShardingErrorCodes.RetentionPolicyFailed);
    }

    [Fact]
    public async Task ShardArchiver_DeleteStoreThrows_IsRetentionPolicyFailedInternal()
    {
        var (archiver, store) = CreateArchiver();
        store.UpdateTierAsync(Arg.Any<string>(), Arg.Any<ShardTier>(), Arg.Any<CancellationToken>())
            .Returns<Task<bool>>(_ => throw new InvalidOperationException("store down"));

        var result = await archiver.DeleteShardDataAsync("s1");

        AssertInternal(result, ShardingErrorCodes.RetentionPolicyFailed);
    }

    [Fact]
    public async Task ShardArchiver_EnforceReadOnlyOfUnknownShard_IsShardNotFoundInternal()
    {
        var (archiver, store) = CreateArchiver(withEnforcer: true);
        store.GetTierInfoAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ShardTierInfo?>(null));

        var result = await archiver.EnforceReadOnlyAsync("s1");

        AssertInternal(result, ShardingErrorCodes.ShardNotFound);
    }

    [Fact]
    public async Task ShardArchiver_EnforceReadOnlyStoreThrows_IsTierTransitionFailedInternal()
    {
        var (archiver, store) = CreateArchiver(withEnforcer: true);
        store.GetTierInfoAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<ShardTierInfo?>>(_ => throw new InvalidOperationException("store down"));

        var result = await archiver.EnforceReadOnlyAsync("s1");

        AssertInternal(result, ShardingErrorCodes.TierTransitionFailed);
    }

    #endregion

    #region TimeBasedShardRouter

    [Fact]
    public void TimeBasedShardRouter_GetShardTierOfUnknownShard_IsShardNotFoundInternal()
    {
        var router = CreateJanuaryRouter(ShardTier.Hot);

        AssertInternal(router.GetShardTier("unknown"), ShardingErrorCodes.ShardNotFound);
    }

    [Fact]
    public void TimeBasedShardRouter_GetShardTierInfoOfUnknownShard_IsShardNotFoundInternal()
    {
        var router = CreateJanuaryRouter(ShardTier.Hot);

        AssertInternal(router.GetShardTierInfo("unknown"), ShardingErrorCodes.ShardNotFound);
    }

    [Fact]
    public void TimeBasedShardRouter_GetShardIdWithoutEntries_IsNoTimeBasedShardsInternal()
    {
        var router = CreateTimeBasedRouter();

        AssertInternal(router.GetShardId("2026-01-15"), ShardingErrorCodes.NoTimeBasedShards);
    }

    [Fact]
    public async Task TimeBasedShardRouter_RouteByTimestampWithoutEntries_IsNoTimeBasedShardsInternal()
    {
        var router = CreateTimeBasedRouter();

        var result = await router.RouteByTimestampAsync(new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc));

        AssertInternal(result, ShardingErrorCodes.NoTimeBasedShards);
    }

    [Fact]
    public async Task TimeBasedShardRouter_RouteWriteWithoutEntries_IsNoTimeBasedShardsInternal()
    {
        var router = CreateTimeBasedRouter();

        var result = await router.RouteWriteByTimestampAsync(new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc));

        AssertInternal(result, ShardingErrorCodes.NoTimeBasedShards);
    }

    [Fact]
    public async Task TimeBasedShardRouter_RangeWithoutEntries_IsNoTimeBasedShardsInternal()
    {
        var router = CreateTimeBasedRouter();

        var result = await router.GetShardsInRangeAsync(
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));

        AssertInternal(result, ShardingErrorCodes.NoTimeBasedShards);
    }

    [Fact]
    public void TimeBasedShardRouter_UnmatchedPartialKey_IsPartialKeyRoutingFailedInternal()
    {
        var router = CreateJanuaryRouter(ShardTier.Hot);

        AssertInternal(router.GetShardIds(new CompoundShardKey("9999")), ShardingErrorCodes.PartialKeyRoutingFailed);
    }

    [Fact]
    public async Task TimeBasedShardRouter_WriteToNonHotShard_IsShardReadOnlyConflict()
    {
        var router = CreateJanuaryRouter(ShardTier.Warm);

        var result = await router.RouteWriteByTimestampAsync(new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc));

        AssertCategory(result, ShardingErrorCodes.ShardReadOnly, ErrorCategory.Conflict);
    }

    [Fact]
    public async Task TimeBasedShardRouter_RangeOutsideAllPeriods_IsTimestampOutsideRangeInternal()
    {
        var router = CreateJanuaryRouter(ShardTier.Hot);

        var result = await router.GetShardsInRangeAsync(
            new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(1900, 2, 1, 0, 0, 0, DateTimeKind.Utc));

        AssertInternal(result, ShardingErrorCodes.TimestampOutsideRange);
    }

    [Fact]
    public void TimeBasedShardRouter_KeyOutsideAllPeriods_IsTimestampOutsideRangeInternal()
    {
        var router = CreateJanuaryRouter(ShardTier.Hot);

        AssertInternal(router.GetShardId("1900-01-01"), ShardingErrorCodes.TimestampOutsideRange);
    }

    #endregion
}

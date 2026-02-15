using Encina.Sharding;
using Encina.Sharding.TimeBased;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Sharding.TimeBased;

/// <summary>
/// Unit tests for <see cref="ShardArchiver"/>.
/// Verifies tier transitions, read-only enforcement, archival, and retention error handling.
/// </summary>
public sealed class ShardArchiverTests
{
    #region Test Helpers

    private static ShardTierInfo CreateTierInfo(
        string shardId = "orders-2025-12",
        ShardTier tier = ShardTier.Hot,
        bool isReadOnly = false)
    {
        return new ShardTierInfo(
            shardId,
            tier,
            new DateOnly(2025, 12, 1),
            new DateOnly(2026, 1, 1),
            isReadOnly,
            $"Server=test;Database={shardId}",
            DateTime.UtcNow);
    }

    private static (ShardArchiver Archiver, ITierStore TierStore, IShardTopologyProvider TopologyProvider, IReadOnlyEnforcer? Enforcer) CreateTestFixture(
        bool withEnforcer = false)
    {
        var tierStore = Substitute.For<ITierStore>();
        var topologyProvider = Substitute.For<IShardTopologyProvider>();
        var enforcer = withEnforcer ? Substitute.For<IReadOnlyEnforcer>() : null;
        var archiver = new ShardArchiver(tierStore, topologyProvider, enforcer);
        return (archiver, tierStore, topologyProvider, enforcer);
    }

    #endregion

    #region Constructor

    [Fact]
    public void Constructor_NullTierStore_ThrowsArgumentNullException()
    {
        var topologyProvider = Substitute.For<IShardTopologyProvider>();

        Should.Throw<ArgumentNullException>(() =>
            new ShardArchiver(null!, topologyProvider));
    }

    [Fact]
    public void Constructor_NullTopologyProvider_ThrowsArgumentNullException()
    {
        var tierStore = Substitute.For<ITierStore>();

        Should.Throw<ArgumentNullException>(() =>
            new ShardArchiver(tierStore, null!));
    }

    [Fact]
    public void Constructor_NullEnforcer_IsValid()
    {
        var tierStore = Substitute.For<ITierStore>();
        var topologyProvider = Substitute.For<IShardTopologyProvider>();

        var archiver = new ShardArchiver(tierStore, topologyProvider);

        archiver.ShouldNotBeNull();
    }

    #endregion

    #region TransitionTierAsync

    [Fact]
    public async Task TransitionTierAsync_SuccessfulTransition_ReturnsUnit()
    {
        var (archiver, tierStore, _, _) = CreateTestFixture();
        tierStore.UpdateTierAsync("shard-1", ShardTier.Warm, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await archiver.TransitionTierAsync("shard-1", ShardTier.Warm);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task TransitionTierAsync_ShardNotFound_ReturnsTierTransitionFailed()
    {
        var (archiver, tierStore, _, _) = CreateTestFixture();
        tierStore.UpdateTierAsync("unknown", ShardTier.Warm, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await archiver.TransitionTierAsync("unknown", ShardTier.Warm);

        result.IsLeft.ShouldBeTrue();
        EncinaError error = default;
        _ = result.IfLeft(e => error = e);
        error.GetCode().IfNone(string.Empty)
            .ShouldBe(ShardingErrorCodes.TierTransitionFailed);
    }

    [Fact]
    public async Task TransitionTierAsync_TierStoreThrows_ReturnsErrorWrappingException()
    {
        var (archiver, tierStore, _, _) = CreateTestFixture();
        tierStore.UpdateTierAsync("shard-1", ShardTier.Warm, Arg.Any<CancellationToken>())
            .Returns<bool>(x => throw new InvalidOperationException("DB failure"));

        var result = await archiver.TransitionTierAsync("shard-1", ShardTier.Warm);

        result.IsLeft.ShouldBeTrue();
        EncinaError error = default;
        _ = result.IfLeft(e => error = e);
        error.GetCode().IfNone(string.Empty)
            .ShouldBe(ShardingErrorCodes.TierTransitionFailed);
    }

    [Fact]
    public async Task TransitionTierAsync_NullShardId_ThrowsArgumentNullException()
    {
        var (archiver, _, _, _) = CreateTestFixture();

        await Should.ThrowAsync<ArgumentNullException>(() =>
            archiver.TransitionTierAsync(null!, ShardTier.Warm));
    }

    [Fact]
    public async Task TransitionTierAsync_ToHot_DoesNotCallEnforcer()
    {
        var (archiver, tierStore, _, enforcer) = CreateTestFixture(withEnforcer: true);
        tierStore.UpdateTierAsync("shard-1", ShardTier.Hot, Arg.Any<CancellationToken>())
            .Returns(true);

        await archiver.TransitionTierAsync("shard-1", ShardTier.Hot);

        await enforcer!.DidNotReceive()
            .EnforceReadOnlyAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TransitionTierAsync_ToWarm_WithEnforcer_CallsEnforcer()
    {
        var (archiver, tierStore, _, enforcer) = CreateTestFixture(withEnforcer: true);
        var tierInfo = CreateTierInfo("shard-1", ShardTier.Warm, isReadOnly: true);

        tierStore.UpdateTierAsync("shard-1", ShardTier.Warm, Arg.Any<CancellationToken>())
            .Returns(true);
        tierStore.GetTierInfoAsync("shard-1", Arg.Any<CancellationToken>())
            .Returns(tierInfo);
        enforcer!.EnforceReadOnlyAsync("shard-1", tierInfo.ConnectionString, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(unit));

        var result = await archiver.TransitionTierAsync("shard-1", ShardTier.Warm);

        result.IsRight.ShouldBeTrue();
        await enforcer.Received(1)
            .EnforceReadOnlyAsync("shard-1", tierInfo.ConnectionString, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TransitionTierAsync_ToWarm_EnforcerFails_ReturnsError()
    {
        var (archiver, tierStore, _, enforcer) = CreateTestFixture(withEnforcer: true);
        var tierInfo = CreateTierInfo("shard-1", ShardTier.Warm, isReadOnly: true);
        var enforcerError = EncinaErrors.Create(
            ShardingErrorCodes.TierTransitionFailed, "Enforcer failed");

        tierStore.UpdateTierAsync("shard-1", ShardTier.Warm, Arg.Any<CancellationToken>())
            .Returns(true);
        tierStore.GetTierInfoAsync("shard-1", Arg.Any<CancellationToken>())
            .Returns(tierInfo);
        enforcer!.EnforceReadOnlyAsync("shard-1", tierInfo.ConnectionString, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Left(enforcerError));

        var result = await archiver.TransitionTierAsync("shard-1", ShardTier.Warm);

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region ArchiveShardAsync

    [Fact]
    public async Task ArchiveShardAsync_DefaultImplementation_ReturnsUnit()
    {
        var (archiver, _, _, _) = CreateTestFixture();
        var options = new ArchiveOptions("s3://backups/archive");

        var result = await archiver.ArchiveShardAsync("shard-1", options);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task ArchiveShardAsync_NullShardId_ThrowsArgumentNullException()
    {
        var (archiver, _, _, _) = CreateTestFixture();
        var options = new ArchiveOptions("s3://backups/archive");

        await Should.ThrowAsync<ArgumentNullException>(() =>
            archiver.ArchiveShardAsync(null!, options));
    }

    [Fact]
    public async Task ArchiveShardAsync_NullOptions_ThrowsArgumentNullException()
    {
        var (archiver, _, _, _) = CreateTestFixture();

        await Should.ThrowAsync<ArgumentNullException>(() =>
            archiver.ArchiveShardAsync("shard-1", null!));
    }

    #endregion

    #region EnforceReadOnlyAsync

    [Fact]
    public async Task EnforceReadOnlyAsync_NoEnforcer_ReturnsUnit()
    {
        var (archiver, _, _, _) = CreateTestFixture(withEnforcer: false);

        var result = await archiver.EnforceReadOnlyAsync("shard-1");

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task EnforceReadOnlyAsync_WithEnforcer_DelegatesToEnforcer()
    {
        var (archiver, tierStore, _, enforcer) = CreateTestFixture(withEnforcer: true);
        var tierInfo = CreateTierInfo("shard-1", ShardTier.Warm, isReadOnly: true);

        tierStore.GetTierInfoAsync("shard-1", Arg.Any<CancellationToken>())
            .Returns(tierInfo);
        enforcer!.EnforceReadOnlyAsync("shard-1", tierInfo.ConnectionString, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(unit));

        var result = await archiver.EnforceReadOnlyAsync("shard-1");

        result.IsRight.ShouldBeTrue();
        await enforcer.Received(1)
            .EnforceReadOnlyAsync("shard-1", tierInfo.ConnectionString, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnforceReadOnlyAsync_ShardNotFound_ReturnsError()
    {
        var (archiver, tierStore, _, _) = CreateTestFixture(withEnforcer: true);
        tierStore.GetTierInfoAsync("unknown", Arg.Any<CancellationToken>())
            .Returns((ShardTierInfo?)null);

        var result = await archiver.EnforceReadOnlyAsync("unknown");

        result.IsLeft.ShouldBeTrue();
        EncinaError error = default;
        _ = result.IfLeft(e => error = e);
        error.GetCode().IfNone(string.Empty)
            .ShouldBe(ShardingErrorCodes.ShardNotFound);
    }

    [Fact]
    public async Task EnforceReadOnlyAsync_NullShardId_ThrowsArgumentNullException()
    {
        var (archiver, _, _, _) = CreateTestFixture();

        await Should.ThrowAsync<ArgumentNullException>(() =>
            archiver.EnforceReadOnlyAsync(null!));
    }

    #endregion

    #region DeleteShardDataAsync

    [Fact]
    public async Task DeleteShardDataAsync_Success_ReturnsUnit()
    {
        var (archiver, tierStore, _, _) = CreateTestFixture();
        tierStore.UpdateTierAsync("shard-1", ShardTier.Archived, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await archiver.DeleteShardDataAsync("shard-1");

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteShardDataAsync_ShardNotFound_ReturnsRetentionPolicyFailed()
    {
        var (archiver, tierStore, _, _) = CreateTestFixture();
        tierStore.UpdateTierAsync("unknown", ShardTier.Archived, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await archiver.DeleteShardDataAsync("unknown");

        result.IsLeft.ShouldBeTrue();
        EncinaError error = default;
        _ = result.IfLeft(e => error = e);
        error.GetCode().IfNone(string.Empty)
            .ShouldBe(ShardingErrorCodes.RetentionPolicyFailed);
    }

    [Fact]
    public async Task DeleteShardDataAsync_TierStoreThrows_ReturnsRetentionPolicyFailed()
    {
        var (archiver, tierStore, _, _) = CreateTestFixture();
        tierStore.UpdateTierAsync("shard-1", ShardTier.Archived, Arg.Any<CancellationToken>())
            .Returns<bool>(x => throw new InvalidOperationException("DB failure"));

        var result = await archiver.DeleteShardDataAsync("shard-1");

        result.IsLeft.ShouldBeTrue();
        EncinaError error = default;
        _ = result.IfLeft(e => error = e);
        error.GetCode().IfNone(string.Empty)
            .ShouldBe(ShardingErrorCodes.RetentionPolicyFailed);
    }

    [Fact]
    public async Task DeleteShardDataAsync_NullShardId_ThrowsArgumentNullException()
    {
        var (archiver, _, _, _) = CreateTestFixture();

        await Should.ThrowAsync<ArgumentNullException>(() =>
            archiver.DeleteShardDataAsync(null!));
    }

    #endregion
}

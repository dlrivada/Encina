using Encina.Sharding;
using Encina.Sharding.TimeBased;

using NSubstitute;

using Shouldly;

namespace Encina.GuardTests.Sharding.TimeBased;

/// <summary>
/// Guard clause tests for the methods of <see cref="ShardArchiver"/>.
/// </summary>
public sealed class ShardArchiverGuardTests
{
    private static ShardArchiver CreateArchiver() =>
        new(Substitute.For<ITierStore>(), Substitute.For<IShardTopologyProvider>());

    [Fact]
    public async Task TransitionTierAsync_NullShardId_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await CreateArchiver().TransitionTierAsync(null!, ShardTier.Warm));
        ex.ParamName.ShouldBe("shardId");
    }

    [Fact]
    public async Task ArchiveShardAsync_NullShardId_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await CreateArchiver().ArchiveShardAsync(null!, new ArchiveOptions("archive-destination")));
        ex.ParamName.ShouldBe("shardId");
    }

    [Fact]
    public async Task ArchiveShardAsync_NullOptions_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await CreateArchiver().ArchiveShardAsync("shard-1", null!));
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public async Task EnforceReadOnlyAsync_NullShardId_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await CreateArchiver().EnforceReadOnlyAsync(null!));
        ex.ParamName.ShouldBe("shardId");
    }

    [Fact]
    public async Task DeleteShardDataAsync_NullShardId_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await CreateArchiver().DeleteShardDataAsync(null!));
        ex.ParamName.ShouldBe("shardId");
    }
}

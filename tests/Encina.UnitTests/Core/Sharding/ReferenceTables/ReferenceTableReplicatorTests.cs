using System.Diagnostics;

using Encina.Sharding;
using Encina.Sharding.ReferenceTables;
using Encina.UnitTests.Support;

using Microsoft.Extensions.Logging.Testing;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Core.Sharding.ReferenceTables;

/// <summary>
/// Characterization tests for <see cref="ReferenceTableReplicator"/> (#4235): primary shard resolution, target
/// selection, primary read, per-shard upsert outcomes (success, error, exception, cancellation), parallelism,
/// the aggregated result, logging and activities, through both <c>ReplicateAsync</c> and <c>ReplicateAllAsync</c>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ReferenceTableReplicatorTests
{
    private const string Sentinel = "SENTINEL-REPLICATOR-DSN-4235";
    private const string ActivitySourceName = "Encina.ReferenceTable";
    private const string ReplicateOperation = "encina.reference_table.replicate";
    private const string SyncShardOperation = "encina.reference_table.sync_shard";

    private readonly IReferenceTableRegistry _registry = Substitute.For<IReferenceTableRegistry>();
    private readonly IShardTopologyProvider _topologyProvider = Substitute.For<IShardTopologyProvider>();
    private readonly IReferenceTableStoreFactory _storeFactory = Substitute.For<IReferenceTableStoreFactory>();
    private readonly Dictionary<string, IReferenceTableStore> _stores = new(StringComparer.Ordinal);
    private readonly FakeLogger<ReferenceTableReplicator> _logger = new();

    private readonly IReadOnlyList<Country> _rows = [new("ES"), new("PT"), new("FR")];

    public sealed record Country(string Code);

    private ReferenceTableReplicator CreateSut(
        IEnumerable<ShardInfo> shards,
        string? primaryShardId = "shard-1",
        int maxParallelShards = 4)
    {
        var shardList = shards.ToList();
        foreach (var shard in shardList)
        {
            var store = Substitute.For<IReferenceTableStore>();
            store.GetAllAsync<Country>(Arg.Any<CancellationToken>()).Returns(Right<EncinaError, IReadOnlyList<Country>>(_rows));
            store.UpsertAsync<Country>(Arg.Any<IEnumerable<Country>>(), Arg.Any<CancellationToken>())
                .Returns(Right<EncinaError, int>(_rows.Count));
            _stores[shard.ShardId] = store;
            _storeFactory.CreateForShard(shard.ConnectionString).Returns(store);
        }

        var config = new ReferenceTableConfiguration(typeof(Country), new ReferenceTableOptions { PrimaryShardId = primaryShardId });
        _registry.IsRegistered<Country>().Returns(true);
        _registry.GetConfiguration<Country>().Returns(config);
        _registry.GetAllConfigurations().Returns([config]);
        _topologyProvider.GetTopology().Returns(new ShardTopology(shardList));

        return new ReferenceTableReplicator(
            _registry,
            _topologyProvider,
            _storeFactory,
            Options.Create(new ReferenceTableGlobalOptions { MaxParallelShards = maxParallelShards }),
            _logger);
    }

    private static ShardInfo Shard(string id, bool isActive = true) => new(id, $"conn-{id}", IsActive: isActive);

    private static ShardInfo[] ThreeShards() => [Shard("shard-1"), Shard("shard-2"), Shard("shard-3")];

    private void UpsertReturns(string shardId, Task<Either<EncinaError, int>> result) =>
        _stores[shardId].UpsertAsync<Country>(Arg.Any<IEnumerable<Country>>(), Arg.Any<CancellationToken>()).Returns(result);

    [Fact]
    public async Task ReplicateAsync_EntityNotRegistered_ReturnsEntityNotRegistered()
    {
        var sut = CreateSut(ThreeShards());
        _registry.IsRegistered<Country>().Returns(false);

        var result = await sut.ReplicateAsync<Country>();

        var error = result.LeftValue();
        error.GetEncinaCode().ShouldBe(ReferenceTableErrorCodes.EntityNotRegistered);
        error.Message.ShouldBe("Entity type 'Country' is not registered as a reference table.");
    }

    [Fact]
    public async Task ReplicateAsync_EmptyTopologyAndNoConfiguredPrimary_ReturnsPrimaryShardNotFound()
    {
        var sut = CreateSut([], primaryShardId: null);

        var result = await sut.ReplicateAsync<Country>();

        var error = result.LeftValue();
        error.GetEncinaCode().ShouldBe(ReferenceTableErrorCodes.PrimaryShardNotFound);
        error.Message.ShouldBe("No primary shard found for reference table 'Country'.");
    }

    [Fact]
    public async Task ReplicateAsync_ConfiguredPrimaryMissingFromTopology_ReturnsPrimaryShardNotFound()
    {
        var sut = CreateSut(ThreeShards(), primaryShardId: "missing");

        var result = await sut.ReplicateAsync<Country>();

        var error = result.LeftValue();
        error.GetEncinaCode().ShouldBe(ReferenceTableErrorCodes.PrimaryShardNotFound);
        error.Message.ShouldBe("Primary shard 'missing' for reference table 'Country' does not exist in the topology.");
        _storeFactory.DidNotReceiveWithAnyArgs().CreateForShard(default!);
    }

    [Fact]
    public async Task ReplicateAsync_NoConfiguredPrimary_ReadsFromFirstTopologyShardAndWritesToTheOthers()
    {
        var sut = CreateSut(ThreeShards(), primaryShardId: null);
        var primary = new ShardTopology(ThreeShards()).AllShardIds[0];

        var result = await sut.ReplicateAsync<Country>();

        result.RightValue().ShardResults.Select(r => r.ShardId).ShouldNotContain(primary);
        await _stores[primary].Received(1).GetAllAsync<Country>(Arg.Any<CancellationToken>());
        await _stores[primary].DidNotReceiveWithAnyArgs().UpsertAsync<Country>(default!, default);
    }

    [Fact]
    public async Task ReplicateAsync_PrimaryIsTheOnlyActiveShard_ReturnsEmptyResultWithoutReading()
    {
        var sut = CreateSut([Shard("shard-1"), Shard("shard-2", isActive: false)]);

        var result = await sut.ReplicateAsync<Country>();

        var replication = result.RightValue();
        replication.RowsSynced.ShouldBe(0);
        replication.ShardResults.ShouldBeEmpty();
        replication.FailedShards.ShouldBeEmpty();
        await _stores["shard-1"].DidNotReceiveWithAnyArgs().GetAllAsync<Country>(default);
        await _stores["shard-2"].DidNotReceiveWithAnyArgs().UpsertAsync<Country>(default!, default);
    }

    [Fact]
    public async Task ReplicateAsync_PrimaryIdMatchesCaseInsensitively_ExcludesThePrimaryFromTargets()
    {
        var sut = CreateSut(ThreeShards(), primaryShardId: "SHARD-1");

        var result = await sut.ReplicateAsync<Country>();

        string.Join(",", result.RightValue().ShardResults.Select(r => r.ShardId).OrderBy(id => id, StringComparer.Ordinal))
            .ShouldBe("shard-2,shard-3");
        await _stores["shard-1"].DidNotReceiveWithAnyArgs().UpsertAsync<Country>(default!, default);
    }

    [Fact]
    public async Task ReplicateAsync_PrimaryReadFails_ReturnsPrimaryReadFailedWithoutUpserting()
    {
        var sut = CreateSut(ThreeShards(), primaryShardId: "SHARD-1");
        _stores["shard-1"].GetAllAsync<Country>(Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, IReadOnlyList<Country>>(EncinaErrors.Create("test.read_failed", "read failed")));

        var result = await sut.ReplicateAsync<Country>();

        var error = result.LeftValue();
        error.GetEncinaCode().ShouldBe(ReferenceTableErrorCodes.PrimaryReadFailed);
        error.Message.ShouldBe("Failed to read reference table data for 'Country' from primary shard 'SHARD-1'.");
        await _stores["shard-2"].DidNotReceiveWithAnyArgs().UpsertAsync<Country>(default!, default);
        await _stores["shard-3"].DidNotReceiveWithAnyArgs().UpsertAsync<Country>(default!, default);
    }

    [Fact]
    public async Task ReplicateAsync_AllTargetsSucceed_ReturnsCompleteResultAndLogsCompletion()
    {
        var sut = CreateSut(ThreeShards());
        UpsertReturns("shard-2", Task.FromResult(Right<EncinaError, int>(3)));
        UpsertReturns("shard-3", Task.FromResult(Right<EncinaError, int>(4)));

        var result = await sut.ReplicateAsync<Country>();

        var replication = result.RightValue();
        replication.RowsSynced.ShouldBe(7);
        replication.IsComplete.ShouldBeTrue();
        string.Join(",", replication.ShardResults.OrderBy(r => r.ShardId, StringComparer.Ordinal).Select(r => $"{r.ShardId}:{r.RowsUpserted}"))
            .ShouldBe("shard-2:3,shard-3:4");
        await _stores["shard-2"].Received(1).UpsertAsync<Country>(_rows, Arg.Any<CancellationToken>());
        await _stores["shard-3"].Received(1).UpsertAsync<Country>(_rows, Arg.Any<CancellationToken>());
        _logger.Collector.GetSnapshot().ShouldContain(r => r.Id.Id == 10033 && r.Message.Contains("7 rows to 2 shards"));
    }

    [Fact]
    public async Task ReplicateAsync_OneTargetReturnsError_ReturnsPartialResultAndLogsPartialFailure()
    {
        var sut = CreateSut(ThreeShards());
        var upsertError = EncinaErrors.Create("test.upsert_failed", "upsert failed");
        UpsertReturns("shard-3", Task.FromResult(Left<EncinaError, int>(upsertError)));

        var result = await sut.ReplicateAsync<Country>();

        var replication = result.RightValue();
        replication.IsPartial.ShouldBeTrue();
        replication.RowsSynced.ShouldBe(3);
        replication.ShardResults.ShouldHaveSingleItem().ShardId.ShouldBe("shard-2");
        var failure = replication.FailedShards.ShouldHaveSingleItem();
        failure.ShardId.ShouldBe("shard-3");
        failure.Error.GetEncinaCode().ShouldBe("test.upsert_failed");
        _logger.Collector.GetSnapshot().ShouldContain(r => r.Id.Id == 10034 && r.Message.Contains("1/2 shards succeeded, 1 failed"));
    }

    [Fact]
    public async Task ReplicateAsync_EveryTargetFails_ReturnsReplicationFailed()
    {
        var sut = CreateSut(ThreeShards());
        var upsertError = EncinaErrors.Create("test.upsert_failed", "upsert failed");
        UpsertReturns("shard-2", Task.FromResult(Left<EncinaError, int>(upsertError)));
        UpsertReturns("shard-3", Task.FromResult(Left<EncinaError, int>(upsertError)));

        var result = await sut.ReplicateAsync<Country>();

        var error = result.LeftValue();
        error.GetEncinaCode().ShouldBe(ReferenceTableErrorCodes.ReplicationFailed);
        error.Message.ShouldBe("Replication failed on all 2 target shards for reference table 'Country'.");
    }

    [Fact]
    public async Task ReplicateAsync_TargetThrows_RecordsReplicationFailedAndLogsRedactedException()
    {
        var sut = CreateSut(ThreeShards());
        UpsertReturns("shard-3", Task.FromException<Either<EncinaError, int>>(new InvalidOperationException(Sentinel)));

        var result = await sut.ReplicateAsync<Country>();

        var failure = result.RightValue().FailedShards.ShouldHaveSingleItem();
        failure.ShardId.ShouldBe("shard-3");
        failure.Error.GetEncinaCode().ShouldBe(ReferenceTableErrorCodes.ReplicationFailed);
        failure.Error.Message.ShouldBe("Unexpected error replicating to shard 'shard-3'.");
        RedactedExceptionLogAssert.LoggedOnlyRedacted(_logger, Sentinel);
    }

    [Fact]
    public async Task ReplicateAsync_TargetCancelled_PropagatesOperationCanceledException()
    {
        var sut = CreateSut(ThreeShards());
        UpsertReturns("shard-3", Task.FromCanceled<Either<EncinaError, int>>(new CancellationToken(canceled: true)));

        await Should.ThrowAsync<OperationCanceledException>(() => sut.ReplicateAsync<Country>());
    }

    [Fact]
    public async Task ReplicateAsync_TokenAlreadyCancelled_ThrowsBeforeAnyUpsert()
    {
        var sut = CreateSut(ThreeShards());
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.ReplicateAsync<Country>(cts.Token));

        await _stores["shard-2"].DidNotReceiveWithAnyArgs().UpsertAsync<Country>(default!, default);
        await _stores["shard-3"].DidNotReceiveWithAnyArgs().UpsertAsync<Country>(default!, default);
    }

    [Fact]
    public async Task ReplicateAsync_PassesTheCancellationTokenToReadAndUpserts()
    {
        var sut = CreateSut(ThreeShards());
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        await sut.ReplicateAsync<Country>(token);

        await _stores["shard-1"].Received(1).GetAllAsync<Country>(token);
        await _stores["shard-2"].Received(1).UpsertAsync<Country>(_rows, token);
        await _stores["shard-3"].Received(1).UpsertAsync<Country>(_rows, token);
    }

    [Fact]
    public async Task ReplicateAsync_MaxParallelShardsOne_UpsertsOneShardAtATime()
    {
        var shards = new[] { Shard("shard-1"), Shard("shard-2"), Shard("shard-3"), Shard("shard-4") };
        var sut = CreateSut(shards, maxParallelShards: 1);
        var current = 0;
        var observedMax = 0;
        foreach (var target in new[] { "shard-2", "shard-3", "shard-4" })
        {
            _stores[target].UpsertAsync<Country>(Arg.Any<IEnumerable<Country>>(), Arg.Any<CancellationToken>())
                .Returns(async _ =>
                {
                    var now = Interlocked.Increment(ref current);
                    InterlockedMax(ref observedMax, now);
                    await Task.Yield();
                    Interlocked.Decrement(ref current);
                    return Right<EncinaError, int>(1);
                });
        }

        var result = await sut.ReplicateAsync<Country>();

        result.RightValue().RowsSynced.ShouldBe(3);
        observedMax.ShouldBe(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ReplicateAsync_NonPositiveMaxParallelShards_FallsBackToProcessorCount(int maxParallelShards)
    {
        var sut = CreateSut(ThreeShards(), maxParallelShards: maxParallelShards);

        var result = await sut.ReplicateAsync<Country>();

        result.RightValue().ShardResults.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ReplicateAsync_Success_CompletesReplicateAndShardActivitiesWithTags()
    {
        var sut = CreateSut(ThreeShards());
        using var capture = new ActivityCapture(ActivitySourceName);

        await sut.ReplicateAsync<Country>();

        var replicate = capture.Stopped(ReplicateOperation).ShouldHaveSingleItem();
        replicate.Status.ShouldBe(ActivityStatusCode.Ok);
        replicate.GetTagItem("reference_table.entity_type").ShouldBe("Country");
        replicate.GetTagItem("reference_table.rows_synced").ShouldBe(6);
        replicate.GetTagItem("reference_table.shard_count").ShouldBe(2);
        replicate.GetTagItem("reference_table.duration_ms").ShouldBeOfType<double>();
        var syncs = capture.Stopped(SyncShardOperation);
        string.Join(",", syncs.Select(a => (string?)a.GetTagItem("reference_table.shard_id")).OrderBy(id => id, StringComparer.Ordinal))
            .ShouldBe("shard-2,shard-3");
        syncs.ShouldAllBe(a => a.Status == ActivityStatusCode.Ok && Equals(a.GetTagItem("reference_table.rows_synced"), 3));
    }

    [Fact]
    public async Task ReplicateAsync_PartialFailure_CompletesReplicateActivityWithFailedShardSummary()
    {
        var sut = CreateSut(ThreeShards());
        UpsertReturns("shard-3", Task.FromResult(Left<EncinaError, int>(EncinaErrors.Create("test.upsert_failed", "upsert failed"))));
        using var capture = new ActivityCapture(ActivitySourceName);

        await sut.ReplicateAsync<Country>();

        var replicate = capture.Stopped(ReplicateOperation).ShouldHaveSingleItem();
        replicate.Status.ShouldBe(ActivityStatusCode.Error);
        replicate.StatusDescription.ShouldBe("1 of 2 shards failed");
        replicate.GetTagItem("reference_table.rows_synced").ShouldBe(3);
        replicate.GetTagItem("reference_table.shard_count").ShouldBe(1);
        var failedSync = capture.Stopped(SyncShardOperation).Single(a => Equals(a.GetTagItem("reference_table.shard_id"), "shard-3"));
        failedSync.Status.ShouldBe(ActivityStatusCode.Error);
        failedSync.StatusDescription.ShouldBe("test.upsert_failed");
    }

    [Fact]
    public async Task ReplicateAsync_EveryTargetFails_CompletesReplicateActivityWithoutResultTags()
    {
        var sut = CreateSut(ThreeShards());
        var upsertError = EncinaErrors.Create("test.upsert_failed", "upsert failed");
        UpsertReturns("shard-2", Task.FromResult(Left<EncinaError, int>(upsertError)));
        UpsertReturns("shard-3", Task.FromResult(Left<EncinaError, int>(upsertError)));
        using var capture = new ActivityCapture(ActivitySourceName);

        await sut.ReplicateAsync<Country>();

        var replicate = capture.Stopped(ReplicateOperation).ShouldHaveSingleItem();
        replicate.Status.ShouldBe(ActivityStatusCode.Error);
        replicate.StatusDescription.ShouldBe("All 2 target shards failed");
        replicate.GetTagItem("reference_table.rows_synced").ShouldBeNull();
    }

    [Fact]
    public async Task ReplicateAsync_PrimaryReadFails_LeavesReplicateActivityUnset()
    {
        var sut = CreateSut(ThreeShards());
        _stores["shard-1"].GetAllAsync<Country>(Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, IReadOnlyList<Country>>(EncinaErrors.Create("test.read_failed", "read failed")));
        using var capture = new ActivityCapture(ActivitySourceName);

        await sut.ReplicateAsync<Country>();

        capture.Stopped(ReplicateOperation).ShouldHaveSingleItem().Status.ShouldBe(ActivityStatusCode.Unset);
        capture.Stopped(SyncShardOperation).ShouldBeEmpty();
    }

    [Fact]
    public async Task ReplicateAllAsync_RegisteredTable_ReplicatesThroughTheRuntimeTypedPath()
    {
        var sut = CreateSut(ThreeShards());

        var result = await sut.ReplicateAllAsync();

        var replication = result.RightValue();
        replication.RowsSynced.ShouldBe(6);
        replication.ShardResults.Count.ShouldBe(2);
        replication.FailedShards.ShouldBeEmpty();
    }

    [Fact]
    public async Task ReplicateAllAsync_TableFails_LogsErrorCodeAndReturnsEmptyTotals()
    {
        var sut = CreateSut(ThreeShards(), primaryShardId: "missing");

        var result = await sut.ReplicateAllAsync();

        var replication = result.RightValue();
        replication.RowsSynced.ShouldBe(0);
        replication.ShardResults.ShouldBeEmpty();
        _logger.Collector.GetSnapshot().ShouldContain(r =>
            r.Id.Id == 10035 && r.Message.Contains(ReferenceTableErrorCodes.PrimaryShardNotFound));
    }

    private static void InterlockedMax(ref int target, int value)
    {
        int snapshot;
        do
        {
            snapshot = target;
            if (value <= snapshot)
            {
                return;
            }
        }
        while (Interlocked.CompareExchange(ref target, value, snapshot) != snapshot);
    }
}

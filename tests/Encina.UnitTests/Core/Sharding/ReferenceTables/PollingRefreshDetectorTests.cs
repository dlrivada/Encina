using System.Diagnostics;

using Encina.Sharding;
using Encina.Sharding.ReferenceTables;
using Encina.UnitTests.Support;

using Microsoft.Extensions.Logging.Testing;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Core.Sharding.ReferenceTables;

/// <summary>
/// Characterization tests for <see cref="PollingRefreshDetector.CheckAndReplicateAsync"/> (#4235): primary shard
/// resolution, hash comparison, replication, state persistence order, activity tags and propagated exceptions.
/// </summary>
[Trait("Category", "Unit")]
public sealed class PollingRefreshDetectorTests
{
    private const string ActivitySourceName = "Encina.ReferenceTable";
    private const string DetectChangesOperation = "encina.reference_table.detect_changes";

    private readonly IReferenceTableReplicator _replicator = Substitute.For<IReferenceTableReplicator>();
    private readonly IReferenceTableStoreFactory _storeFactory = Substitute.For<IReferenceTableStoreFactory>();
    private readonly IShardTopologyProvider _topologyProvider = Substitute.For<IShardTopologyProvider>();
    private readonly IReferenceTableStateStore _stateStore = Substitute.For<IReferenceTableStateStore>();
    private readonly IReferenceTableStore _store = Substitute.For<IReferenceTableStore>();
    private readonly FakeLogger<PollingRefreshDetector> _logger = new();

    public sealed class Country;

    private PollingRefreshDetector CreateSut(params ShardInfo[] shards)
    {
        _topologyProvider.GetTopology().Returns(new ShardTopology(shards));
        _storeFactory.CreateForShard(Arg.Any<string>()).Returns(_store);
        return new PollingRefreshDetector(_replicator, _storeFactory, _topologyProvider, _stateStore, _logger);
    }

    private static ReferenceTableConfiguration Config(string? primaryShardId = null) =>
        new(typeof(Country), new ReferenceTableOptions { PrimaryShardId = primaryShardId });

    private static ShardInfo Shard(string id) => new(id, $"conn-{id}");

    private static ReplicationResult Replicated() =>
        new(3, TimeSpan.FromMilliseconds(5), [new ShardReplicationResult("shard-2", 3, TimeSpan.FromMilliseconds(5))], []);

    private void HashIs(string hash) =>
        _store.GetHashAsync<Country>(Arg.Any<CancellationToken>()).Returns(Right<EncinaError, string>(hash));

    private void LastHashIs(string? hash) =>
        _stateStore.GetLastHashAsync(typeof(Country), Arg.Any<CancellationToken>()).Returns(hash);

    private void ReplicationReturns(Either<EncinaError, ReplicationResult> result) =>
        _replicator.ReplicateAsync<Country>(Arg.Any<CancellationToken>()).Returns(result);

    [Fact]
    public async Task CheckAndReplicateAsync_NullConfig_ThrowsArgumentNullException()
    {
        var sut = CreateSut(Shard("shard-1"));

        await Should.ThrowAsync<ArgumentNullException>(() => sut.CheckAndReplicateAsync(null!));
    }

    [Fact]
    public async Task CheckAndReplicateAsync_EmptyTopologyAndNoConfiguredPrimary_ReturnsPrimaryShardNotFound()
    {
        var sut = CreateSut();

        var result = await sut.CheckAndReplicateAsync(Config());

        var error = result.LeftValue();
        error.GetEncinaCode().ShouldBe(ReferenceTableErrorCodes.PrimaryShardNotFound);
        error.Message.ShouldBe("No primary shard found for reference table 'Country'.");
        _storeFactory.DidNotReceiveWithAnyArgs().CreateForShard(default!);
    }

    [Fact]
    public async Task CheckAndReplicateAsync_ConfiguredPrimaryMissingFromTopology_ReturnsPrimaryShardNotFound()
    {
        var sut = CreateSut(Shard("shard-1"));

        var result = await sut.CheckAndReplicateAsync(Config("missing"));

        var error = result.LeftValue();
        error.GetEncinaCode().ShouldBe(ReferenceTableErrorCodes.PrimaryShardNotFound);
        error.Message.ShouldBe("Primary shard 'missing' not found in topology.");
        _storeFactory.DidNotReceiveWithAnyArgs().CreateForShard(default!);
    }

    [Fact]
    public async Task CheckAndReplicateAsync_NoConfiguredPrimary_UsesFirstTopologyShard()
    {
        var sut = CreateSut(Shard("shard-1"), Shard("shard-2"), Shard("shard-3"));
        var expectedPrimary = new ShardTopology([Shard("shard-1"), Shard("shard-2"), Shard("shard-3")]).AllShardIds[0];
        HashIs("h2");
        LastHashIs("h1");
        ReplicationReturns(Replicated());

        await sut.CheckAndReplicateAsync(Config());

        _storeFactory.Received(1).CreateForShard($"conn-{expectedPrimary}");
    }

    [Fact]
    public async Task CheckAndReplicateAsync_ConfiguredPrimary_UsesThatShardConnection()
    {
        var sut = CreateSut(Shard("shard-1"), Shard("shard-2"));
        HashIs("h2");
        LastHashIs("h1");
        ReplicationReturns(Replicated());

        await sut.CheckAndReplicateAsync(Config("SHARD-2"));

        _storeFactory.Received(1).CreateForShard("conn-shard-2");
    }

    [Fact]
    public async Task CheckAndReplicateAsync_HashComputationFails_ReturnsStoreErrorWithoutTouchingState()
    {
        var sut = CreateSut(Shard("shard-1"));
        var storeError = EncinaErrors.Create("test.hash_failed", "hash failed");
        _store.GetHashAsync<Country>(Arg.Any<CancellationToken>()).Returns(Left<EncinaError, string>(storeError));

        var result = await sut.CheckAndReplicateAsync(Config());

        result.LeftValue().GetEncinaCode().ShouldBe("test.hash_failed");
        await _stateStore.DidNotReceiveWithAnyArgs().GetLastHashAsync(default!, default);
        await _replicator.DidNotReceiveWithAnyArgs().ReplicateAsync<Country>(default);
    }

    // Current behavior, pinned as found: the no-change path converts a null ReplicationResult to Either, which
    // LanguageExt rejects with ValueIsNullException (the documented Right(null) is never returned). The fix is a
    // separate bug; this refactor (#4235) keeps the behavior identical.
    [Fact]
    public async Task CheckAndReplicateAsync_HashUnchanged_ThrowsValueIsNullExceptionWithoutReplicating()
    {
        var sut = CreateSut(Shard("shard-1"));
        HashIs("same-hash");
        LastHashIs("same-hash");

        await Should.ThrowAsync<ValueIsNullException>(() => sut.CheckAndReplicateAsync(Config()));

        await _replicator.DidNotReceiveWithAnyArgs().ReplicateAsync<Country>(default);
        await _stateStore.DidNotReceiveWithAnyArgs().SaveHashAsync(default!, default!, default);
        await _stateStore.DidNotReceiveWithAnyArgs().SaveReplicationTimeAsync(default!, default, default);
        _logger.Collector.GetSnapshot().ShouldContain(r => r.Id.Id == 136 && r.Message.Contains("same-hash"));
    }

    [Fact]
    public async Task CheckAndReplicateAsync_HashComparisonIsOrdinal_CaseDifferenceTriggersReplication()
    {
        var sut = CreateSut(Shard("shard-1"));
        HashIs("ABC");
        LastHashIs("abc");
        ReplicationReturns(Replicated());

        await sut.CheckAndReplicateAsync(Config());

        await _replicator.Received(1).ReplicateAsync<Country>(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckAndReplicateAsync_HashChanged_ReplicatesThenSavesHashThenReplicationTime()
    {
        var sut = CreateSut(Shard("shard-1"));
        var replicated = Replicated();
        HashIs("new-hash");
        LastHashIs("old-hash");
        ReplicationReturns(replicated);
        var before = DateTime.UtcNow;

        var result = await sut.CheckAndReplicateAsync(Config());

        result.RightValue().ShouldBeSameAs(replicated);
        Received.InOrder(() =>
        {
            _stateStore.GetLastHashAsync(typeof(Country), Arg.Any<CancellationToken>());
            _replicator.ReplicateAsync<Country>(Arg.Any<CancellationToken>());
            _stateStore.SaveHashAsync(typeof(Country), "new-hash", Arg.Any<CancellationToken>());
            _stateStore.SaveReplicationTimeAsync(typeof(Country), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        });
        await _stateStore.Received(1).SaveReplicationTimeAsync(
            typeof(Country),
            Arg.Is<DateTime>(t => t.Kind == DateTimeKind.Utc && t >= before && t <= DateTime.UtcNow),
            Arg.Any<CancellationToken>());
        _logger.Collector.GetSnapshot().ShouldContain(r =>
            r.Id.Id == 137 && r.Message.Contains("'old-hash'") && r.Message.Contains("'new-hash'"));
    }

    [Fact]
    public async Task CheckAndReplicateAsync_NoPreviousHash_LogsNonePlaceholder()
    {
        var sut = CreateSut(Shard("shard-1"));
        HashIs("first-hash");
        LastHashIs(null);
        ReplicationReturns(Replicated());

        await sut.CheckAndReplicateAsync(Config());

        _logger.Collector.GetSnapshot().ShouldContain(r => r.Id.Id == 137 && r.Message.Contains("'(none)'"));
    }

    [Fact]
    public async Task CheckAndReplicateAsync_ReplicationFails_ReturnsErrorWithoutSavingState()
    {
        var sut = CreateSut(Shard("shard-1"));
        HashIs("new-hash");
        LastHashIs("old-hash");
        ReplicationReturns(EncinaErrors.Create("test.replication_failed", "replication failed"));

        var result = await sut.CheckAndReplicateAsync(Config());

        result.LeftValue().GetEncinaCode().ShouldBe("test.replication_failed");
        await _stateStore.DidNotReceiveWithAnyArgs().SaveHashAsync(default!, default!, default);
        await _stateStore.DidNotReceiveWithAnyArgs().SaveReplicationTimeAsync(default!, default, default);
    }

    [Fact]
    public async Task CheckAndReplicateAsync_PassesTheCancellationTokenToEveryCollaborator()
    {
        var sut = CreateSut(Shard("shard-1"));
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        HashIs("new-hash");
        LastHashIs("old-hash");
        ReplicationReturns(Replicated());

        await sut.CheckAndReplicateAsync(Config(), token);

        await _store.Received(1).GetHashAsync<Country>(token);
        await _stateStore.Received(1).GetLastHashAsync(typeof(Country), token);
        await _replicator.Received(1).ReplicateAsync<Country>(token);
        await _stateStore.Received(1).SaveHashAsync(typeof(Country), "new-hash", token);
        await _stateStore.Received(1).SaveReplicationTimeAsync(typeof(Country), Arg.Any<DateTime>(), token);
    }

    [Fact]
    public async Task CheckAndReplicateAsync_StateStoreThrows_PropagatesTheException()
    {
        var sut = CreateSut(Shard("shard-1"));
        HashIs("new-hash");
        _stateStore.GetLastHashAsync(typeof(Country), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string?>(new InvalidOperationException("state store down")));

        await Should.ThrowAsync<InvalidOperationException>(() => sut.CheckAndReplicateAsync(Config()));
        await _replicator.DidNotReceiveWithAnyArgs().ReplicateAsync<Country>(default);
    }

    [Fact]
    public async Task CheckAndReplicateAsync_ReplicationCancelled_PropagatesWithoutSavingState()
    {
        var sut = CreateSut(Shard("shard-1"));
        HashIs("new-hash");
        LastHashIs("old-hash");
        _replicator.ReplicateAsync<Country>(Arg.Any<CancellationToken>())
            .Returns(Task.FromCanceled<Either<EncinaError, ReplicationResult>>(new CancellationToken(canceled: true)));

        await Should.ThrowAsync<OperationCanceledException>(() => sut.CheckAndReplicateAsync(Config()));
        await _stateStore.DidNotReceiveWithAnyArgs().SaveHashAsync(default!, default!, default);
    }

    [Fact]
    public async Task CheckAndReplicateAsync_HashUnchanged_CompletesActivityOkWithChangeDetectedFalse()
    {
        var sut = CreateSut(Shard("shard-1"));
        HashIs("same-hash");
        LastHashIs("same-hash");
        using var capture = new ActivityCapture(ActivitySourceName);

        await Should.ThrowAsync<ValueIsNullException>(() => sut.CheckAndReplicateAsync(Config()));

        var activity = capture.Stopped(DetectChangesOperation).ShouldHaveSingleItem();
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
        activity.GetTagItem("reference_table.entity_type").ShouldBe("Country");
        activity.GetTagItem("reference_table.change_detected").ShouldBe(false);
        activity.GetTagItem("reference_table.hash").ShouldBe("same-hash");
    }

    [Fact]
    public async Task CheckAndReplicateAsync_HashChanged_CompletesActivityWithReplicationOutcome()
    {
        var sut = CreateSut(Shard("shard-1"));
        HashIs("new-hash");
        LastHashIs("old-hash");
        ReplicationReturns(EncinaErrors.Create("test.replication_failed", "replication failed"));
        using var capture = new ActivityCapture(ActivitySourceName);

        await sut.CheckAndReplicateAsync(Config());

        var activity = capture.Stopped(DetectChangesOperation).ShouldHaveSingleItem();
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.GetTagItem("reference_table.change_detected").ShouldBe(true);
        activity.GetTagItem("reference_table.hash").ShouldBe("new-hash");
    }

    [Fact]
    public async Task CheckAndReplicateAsync_PrimaryShardMissing_CompletesActivityWithErrorCode()
    {
        var sut = CreateSut(Shard("shard-1"));
        using var capture = new ActivityCapture(ActivitySourceName);

        await sut.CheckAndReplicateAsync(Config("missing"));

        var activity = capture.Stopped(DetectChangesOperation).ShouldHaveSingleItem();
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe(ReferenceTableErrorCodes.PrimaryShardNotFound);
    }

    [Fact]
    public async Task CheckAndReplicateAsync_HashComputationFails_CompletesActivityWithFixedDescription()
    {
        var sut = CreateSut(Shard("shard-1"));
        _store.GetHashAsync<Country>(Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, string>(EncinaErrors.Create("test.hash_failed", "hash failed")));
        using var capture = new ActivityCapture(ActivitySourceName);

        await sut.CheckAndReplicateAsync(Config());

        var activity = capture.Stopped(DetectChangesOperation).ShouldHaveSingleItem();
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe("Hash computation failed");
    }
}

/// <summary>Reads the sides of an <see cref="Either{L, R}"/> in the reference table assertions.</summary>
internal static class ReferenceTableEitherAssert
{
    public static EncinaError LeftValue<T>(this Either<EncinaError, T> result)
    {
        result.IsLeft.ShouldBeTrue("expected Left");
        return result.Match(Right: _ => default, Left: e => e);
    }

    public static T RightValue<T>(this Either<EncinaError, T> result)
    {
        result.IsRight.ShouldBeTrue("expected Right");
        return result.Match(Right: r => r, Left: _ => default!);
    }
}

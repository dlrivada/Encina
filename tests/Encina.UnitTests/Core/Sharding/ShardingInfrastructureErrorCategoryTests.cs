using Encina.Sharding;
using Encina.Sharding.Colocation;
using Encina.Sharding.Configuration;
using Encina.Sharding.Execution;
using Encina.Sharding.Migrations;
using Encina.Sharding.Migrations.Strategies;
using Encina.Sharding.ReferenceTables;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Core.Sharding;

/// <summary>
/// Each case runs a real key extractor, topology, executor, migration coordinator, migration strategy,
/// reference-table replicator or polling detector through the call site that creates the error and
/// reads the declared category back with <c>GetCategory()</c> (#3720, ADR-062 and its R1 amendment).
/// </summary>
/// <remarks>
/// All 35 call sites of the slice are reached through real producers; none is left to the static scan alone.
/// </remarks>
public sealed class ShardingInfrastructureErrorCategoryTests
{
    #region Helpers

    public sealed class RefEntity;

    private static ShardTopology CreateTopology(params string[] shardIds) =>
        new(shardIds.Select(id => new ShardInfo(id, $"conn-{id}")));

    private static EncinaError LeftOf<T>(Either<EncinaError, T> result)
    {
        result.IsLeft.ShouldBeTrue();
        EncinaError error = default;
        _ = result.IfLeft(e => error = e);
        return error;
    }

    private static void AssertError(EncinaError error, string expectedCode, ErrorCategory expectedCategory)
    {
        error.GetCode().IsSome.ShouldBeTrue();
        error.GetCode().IfSome(c => c.ShouldBe(expectedCode));
        error.GetCategory().ShouldBe(expectedCategory);
    }

    private static void AssertLeft<T>(Either<EncinaError, T> result, string expectedCode, ErrorCategory expectedCategory) =>
        AssertError(LeftOf(result), expectedCode, expectedCategory);

    #endregion

    #region ShardKeyExtractor

    public sealed class EmptyShardable : IShardable
    {
        public string GetShardKey() => "";
    }

    public sealed class NoKeyEntity;

    public sealed class NullAttributeEntity
    {
        [ShardKey]
        public string? Key { get; init; }
    }

    public sealed class EmptyAttributeEntity
    {
        [ShardKey]
        public string Key { get; init; } = "";
    }

    [Fact]
    public void ShardKeyExtractor_EmptyShardable_IsShardKeyEmptyInternal() =>
        AssertLeft(ShardKeyExtractor.Extract(new EmptyShardable()), ShardingErrorCodes.ShardKeyEmpty, ErrorCategory.Internal);

    [Fact]
    public void ShardKeyExtractor_NoShardKey_IsShardKeyNotConfiguredInternal() =>
        AssertLeft(ShardKeyExtractor.Extract(new NoKeyEntity()), ShardingErrorCodes.ShardKeyNotConfigured, ErrorCategory.Internal);

    [Fact]
    public void ShardKeyExtractor_NullAttributeValue_IsShardKeyEmptyInternal() =>
        AssertLeft(ShardKeyExtractor.Extract(new NullAttributeEntity()), ShardingErrorCodes.ShardKeyEmpty, ErrorCategory.Internal);

    [Fact]
    public void ShardKeyExtractor_EmptyAttributeValue_IsShardKeyEmptyInternal() =>
        AssertLeft(ShardKeyExtractor.Extract(new EmptyAttributeEntity()), ShardingErrorCodes.ShardKeyEmpty, ErrorCategory.Internal);

    #endregion

    #region CompoundShardKeyExtractor

    public sealed class NullCompound : ICompoundShardable
    {
        public CompoundShardKey GetCompoundShardKey() => null!;
    }

    public sealed class EmptyComponentCompound : ICompoundShardable
    {
        public CompoundShardKey GetCompoundShardKey() => new("us-east", "");
    }

    public sealed class DuplicateOrderEntity
    {
        [ShardKey(Order = 0)]
        public string A { get; init; } = "a";

        [ShardKey(Order = 0)]
        public string B { get; init; } = "b";
    }

    public sealed class NullComponentEntity
    {
        [ShardKey(Order = 0)]
        public string? A { get; init; }

        [ShardKey(Order = 1)]
        public string B { get; init; } = "b";
    }

    public sealed class EmptyComponentEntity
    {
        [ShardKey(Order = 0)]
        public string A { get; init; } = "";

        [ShardKey(Order = 1)]
        public string B { get; init; } = "b";
    }

    [Fact]
    public void CompoundShardKeyExtractor_NullCompoundKey_IsCompoundShardKeyEmptyInternal() =>
        AssertLeft(CompoundShardKeyExtractor.Extract(new NullCompound()), ShardingErrorCodes.CompoundShardKeyEmpty, ErrorCategory.Internal);

    [Fact]
    public void CompoundShardKeyExtractor_EmptyCompoundComponent_IsComponentEmptyInternal() =>
        AssertLeft(CompoundShardKeyExtractor.Extract(new EmptyComponentCompound()), ShardingErrorCodes.CompoundShardKeyComponentEmpty, ErrorCategory.Internal);

    [Fact]
    public void CompoundShardKeyExtractor_NoShardKey_IsShardKeyNotConfiguredInternal() =>
        AssertLeft(CompoundShardKeyExtractor.Extract(new NoKeyEntity()), ShardingErrorCodes.ShardKeyNotConfigured, ErrorCategory.Internal);

    [Fact]
    public void CompoundShardKeyExtractor_DuplicateOrder_IsDuplicateShardKeyOrderInternal() =>
        AssertLeft(CompoundShardKeyExtractor.Extract(new DuplicateOrderEntity()), ShardingErrorCodes.DuplicateShardKeyOrder, ErrorCategory.Internal);

    [Fact]
    public void CompoundShardKeyExtractor_NullAttributeComponent_IsComponentEmptyInternal() =>
        AssertLeft(CompoundShardKeyExtractor.Extract(new NullComponentEntity()), ShardingErrorCodes.CompoundShardKeyComponentEmpty, ErrorCategory.Internal);

    [Fact]
    public void CompoundShardKeyExtractor_EmptyAttributeComponent_IsComponentEmptyInternal() =>
        AssertLeft(CompoundShardKeyExtractor.Extract(new EmptyComponentEntity()), ShardingErrorCodes.CompoundShardKeyComponentEmpty, ErrorCategory.Internal);

    [Fact]
    public void CompoundShardKeyExtractor_EmptyShardable_IsShardKeyEmptyInternal() =>
        AssertLeft(CompoundShardKeyExtractor.Extract(new EmptyShardable()), ShardingErrorCodes.ShardKeyEmpty, ErrorCategory.Internal);

    [Fact]
    public void CompoundShardKeyExtractor_NullSingleAttribute_IsShardKeyEmptyInternal() =>
        AssertLeft(CompoundShardKeyExtractor.Extract(new NullAttributeEntity()), ShardingErrorCodes.ShardKeyEmpty, ErrorCategory.Internal);

    [Fact]
    public void CompoundShardKeyExtractor_EmptySingleAttribute_IsShardKeyEmptyInternal() =>
        AssertLeft(CompoundShardKeyExtractor.Extract(new EmptyAttributeEntity()), ShardingErrorCodes.ShardKeyEmpty, ErrorCategory.Internal);

    #endregion

    #region ShardTopology and ColocationViolationException

    [Fact]
    public void ShardTopology_GetShard_UnknownId_IsShardNotFoundInternal() =>
        AssertLeft(CreateTopology("shard-1").GetShard("missing"), ShardingErrorCodes.ShardNotFound, ErrorCategory.Internal);

    [Fact]
    public void ShardTopology_GetConnectionString_UnknownId_IsShardNotFoundInternal() =>
        AssertLeft(CreateTopology("shard-1").GetConnectionString("missing"), ShardingErrorCodes.ShardNotFound, ErrorCategory.Internal);

    [Fact]
    public void ColocationViolationException_ToEncinaError_IsColocationViolationInternal()
    {
        var exception = new ColocationViolationException(typeof(RefEntity), typeof(NoKeyEntity), "key types differ");

        AssertError(exception.ToEncinaError(), ColocationViolationException.ErrorCode, ErrorCategory.Internal);
    }

    [Fact]
    public void ColocationViolationException_ToEncinaError_WithKeyTypesAndNoEntities_IsColocationViolationInternal()
    {
        var exception = new ColocationViolationException(null, null, "key types differ", "Guid", "string");

        AssertError(exception.ToEncinaError(), ColocationViolationException.ErrorCode, ErrorCategory.Internal);
    }

    #endregion

    #region ShardedQueryExecutor

    private static ShardedQueryExecutor CreateExecutor(ShardTopology topology, TimeSpan timeout, bool allowPartialResults) =>
        new(
            topology,
            new ScatterGatherOptions { MaxParallelism = -1, Timeout = timeout, AllowPartialResults = allowPartialResults },
            NullLogger<ShardedQueryExecutor>.Instance);

    [Fact]
    public async Task ShardedQueryExecutor_QueryExceedsTimeout_IsScatterGatherTimeoutTimeout()
    {
        var executor = CreateExecutor(CreateTopology("shard-1"), TimeSpan.FromMilliseconds(50), allowPartialResults: true);

        var result = await executor.ExecuteAsync<string>(
            ["shard-1"],
            async (_, ct) =>
            {
                await Task.Delay(Timeout.Infinite, ct);
                return Either<EncinaError, IReadOnlyList<string>>.Right((IReadOnlyList<string>)[]);
            },
            CancellationToken.None);

        AssertLeft(result, ShardingErrorCodes.ScatterGatherTimeout, ErrorCategory.Timeout);
    }

    [Fact]
    public async Task ShardedQueryExecutor_ShardFailsWithoutPartialResults_IsScatterGatherPartialFailureInternal()
    {
        var executor = CreateExecutor(CreateTopology("shard-1", "shard-2"), TimeSpan.FromSeconds(30), allowPartialResults: false);

        var result = await executor.ExecuteAsync<string>(
            ["shard-1", "shard-2"],
            (shardId, _) => Task.FromResult(shardId == "shard-1"
                ? Either<EncinaError, IReadOnlyList<string>>.Right((IReadOnlyList<string>)["ok"])
                : Either<EncinaError, IReadOnlyList<string>>.Left(EncinaErrors.Create("test.shard_failed", "boom"))),
            CancellationToken.None);

        AssertLeft(result, ShardingErrorCodes.ScatterGatherPartialFailure, ErrorCategory.Internal);
    }

    [Fact]
    public async Task ShardedQueryExecutor_QueryThrows_ShardFailureIsShardNotFoundInternal()
    {
        var executor = CreateExecutor(CreateTopology("shard-1"), TimeSpan.FromSeconds(30), allowPartialResults: true);

        var result = await executor.ExecuteAsync<string>(
            ["shard-1"],
            (_, _) => throw new InvalidOperationException("query failed"),
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(qr =>
        {
            qr.FailedShards.Count.ShouldBe(1);
            AssertError(qr.FailedShards[0].Error, ShardingErrorCodes.ShardNotFound, ErrorCategory.Internal);
        });
    }

    #endregion

    #region ShardedMigrationCoordinator

    private static readonly MigrationScript Script = new("m1", "UP", "DOWN", "Test migration", "sha256:abc");

    private static IMigrationHistoryStore CreateHistoryStore()
    {
        var store = Substitute.For<IMigrationHistoryStore>();
        store.EnsureHistoryTableExistsAsync(Arg.Any<ShardInfo>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(unit));
        store.RecordAppliedAsync(Arg.Any<string>(), Arg.Any<MigrationScript>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(unit));
        return store;
    }

    private static IMigrationExecutor CreateExecutorSucceeding()
    {
        var executor = Substitute.For<IMigrationExecutor>();
        executor.ExecuteSqlAsync(Arg.Any<ShardInfo>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(unit));
        return executor;
    }

    private static ShardedMigrationCoordinator CreateCoordinator(
        ShardTopology topology,
        IMigrationExecutor? executor = null,
        ISchemaIntrospector? introspector = null) =>
        new(
            topology,
            executor ?? CreateExecutorSucceeding(),
            introspector ?? Substitute.For<ISchemaIntrospector>(),
            CreateHistoryStore(),
            NullLogger<ShardedMigrationCoordinator>.Instance);

    [Fact]
    public async Task ShardedMigrationCoordinator_EmptyTopology_IsNoActiveShardsInternal()
    {
        var coordinator = CreateCoordinator(CreateTopology());

        var result = await coordinator.ApplyToAllShardsAsync(Script, new MigrationOptions());

        AssertLeft(result, ShardingErrorCodes.NoActiveShards, ErrorCategory.Internal);
    }

    [Fact]
    public async Task ShardedMigrationCoordinator_RollbackUnknownMigration_IsMigrationNotFoundNotFound()
    {
        var coordinator = CreateCoordinator(CreateTopology("shard-1"));
        var unknown = new MigrationResult(Guid.NewGuid(), new Dictionary<string, ShardMigrationStatus>(), TimeSpan.Zero, DateTimeOffset.UtcNow);

        var result = await coordinator.RollbackAsync(unknown);

        AssertLeft(result, MigrationErrorCodes.MigrationNotFound, ErrorCategory.NotFound);
    }

    [Fact]
    public async Task ShardedMigrationCoordinator_UnknownProgressId_IsMigrationNotFoundNotFound()
    {
        var coordinator = CreateCoordinator(CreateTopology("shard-1"));

        var result = await coordinator.GetProgressAsync(Guid.NewGuid());

        AssertLeft(result, MigrationErrorCodes.MigrationNotFound, ErrorCategory.NotFound);
    }

    [Fact]
    public async Task ShardedMigrationCoordinator_UnknownShardHistory_IsShardNotFoundInternal()
    {
        var coordinator = CreateCoordinator(CreateTopology("shard-1"));

        var result = await coordinator.GetAppliedMigrationsAsync("missing");

        AssertLeft(result, ShardingErrorCodes.ShardNotFound, ErrorCategory.Internal);
    }

    [Fact]
    public async Task ShardedMigrationCoordinator_DownSqlFails_IsRollbackFailedInternal()
    {
        var executor = CreateExecutorSucceeding();
        var coordinator = CreateCoordinator(CreateTopology("shard-1"), executor);
        var applied = await coordinator.ApplyToAllShardsAsync(Script, new MigrationOptions());
        var migration = applied.Match(Right: r => r, Left: _ => throw new InvalidOperationException("apply failed"));
        executor.ExecuteSqlAsync(Arg.Any<ShardInfo>(), Script.DownSql, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Left(EncinaErrors.Create("test.down_failed", "boom")));

        var result = await coordinator.RollbackAsync(migration);

        AssertLeft(result, MigrationErrorCodes.RollbackFailed, ErrorCategory.Internal);
    }

    [Fact]
    public async Task ShardedMigrationCoordinator_IntrospectorFails_IsSchemaComparisonFailedInternal()
    {
        var introspector = Substitute.For<ISchemaIntrospector>();
        introspector.CompareAsync(Arg.Any<ShardInfo>(), Arg.Any<ShardInfo>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, ShardSchemaDiff>.Left(EncinaErrors.Create("test.compare_failed", "boom")));
        var coordinator = CreateCoordinator(CreateTopology("shard-1", "shard-2"), introspector: introspector);

        var result = await coordinator.DetectDriftAsync();

        AssertLeft(result, MigrationErrorCodes.SchemaComparisonFailed, ErrorCategory.Internal);
    }

    #endregion

    #region SequentialMigrationStrategy

    [Fact]
    public async Task SequentialMigrationStrategy_ActionExceedsTimeout_IsMigrationTimeoutTimeout()
    {
        var status = await SequentialMigrationStrategy.ExecuteOnShardAsync(
            new ShardInfo("shard-1", "conn-shard-1"),
            async (_, ct) =>
            {
                await Task.Delay(Timeout.Infinite, ct);
                return Either<EncinaError, Unit>.Right(unit);
            },
            TimeSpan.FromMilliseconds(50),
            CancellationToken.None);

        status.Outcome.ShouldBe(MigrationOutcome.Failed);
        AssertError(status.Error!.Value, MigrationErrorCodes.MigrationTimeout, ErrorCategory.Timeout);
    }

    [Fact]
    public async Task SequentialMigrationStrategy_ActionThrows_IsMigrationFailedInternal()
    {
        var status = await SequentialMigrationStrategy.ExecuteOnShardAsync(
            new ShardInfo("shard-1", "conn-shard-1"),
            (_, _) => throw new InvalidOperationException("script failed"),
            TimeSpan.FromSeconds(30),
            CancellationToken.None);

        status.Outcome.ShouldBe(MigrationOutcome.Failed);
        AssertError(status.Error!.Value, MigrationErrorCodes.MigrationFailed, ErrorCategory.Internal);
    }

    #endregion

    #region ReferenceTableReplicator

    private sealed class ReplicationFixture
    {
        public IReferenceTableRegistry Registry { get; } = Substitute.For<IReferenceTableRegistry>();
        public IShardTopologyProvider TopologyProvider { get; } = Substitute.For<IShardTopologyProvider>();
        public IReferenceTableStoreFactory StoreFactory { get; } = Substitute.For<IReferenceTableStoreFactory>();

        public ReplicationFixture(ShardTopology topology, string? primaryShardId, bool registered = true)
        {
            Registry.IsRegistered<RefEntity>().Returns(registered);
            Registry.GetConfiguration<RefEntity>().Returns(
                new ReferenceTableConfiguration(typeof(RefEntity), new ReferenceTableOptions { PrimaryShardId = primaryShardId }));
            TopologyProvider.GetTopology().Returns(topology);
        }

        public IReferenceTableStore AddStore(string shardId)
        {
            var store = Substitute.For<IReferenceTableStore>();
            StoreFactory.CreateForShard($"conn-{shardId}").Returns(store);
            return store;
        }

        public ReferenceTableReplicator CreateReplicator() =>
            new(
                Registry,
                TopologyProvider,
                StoreFactory,
                Options.Create(new ReferenceTableGlobalOptions { MaxParallelShards = 2 }),
                NullLogger<ReferenceTableReplicator>.Instance);
    }

    [Fact]
    public async Task ReferenceTableReplicator_UnregisteredEntity_IsEntityNotRegisteredInternal()
    {
        var fixture = new ReplicationFixture(CreateTopology("a"), primaryShardId: null, registered: false);

        var result = await fixture.CreateReplicator().ReplicateAsync<RefEntity>();

        AssertLeft(result, ReferenceTableErrorCodes.EntityNotRegistered, ErrorCategory.Internal);
    }

    [Fact]
    public async Task ReferenceTableReplicator_EmptyTopology_IsPrimaryShardNotFoundInternal()
    {
        var fixture = new ReplicationFixture(CreateTopology(), primaryShardId: null);

        var result = await fixture.CreateReplicator().ReplicateAsync<RefEntity>();

        AssertLeft(result, ReferenceTableErrorCodes.PrimaryShardNotFound, ErrorCategory.Internal);
    }

    [Fact]
    public async Task ReferenceTableReplicator_ConfiguredPrimaryMissing_IsPrimaryShardNotFoundInternal()
    {
        var fixture = new ReplicationFixture(CreateTopology("a"), primaryShardId: "ghost");

        var result = await fixture.CreateReplicator().ReplicateAsync<RefEntity>();

        AssertLeft(result, ReferenceTableErrorCodes.PrimaryShardNotFound, ErrorCategory.Internal);
    }

    [Fact]
    public async Task ReferenceTableReplicator_PrimaryReadFails_IsPrimaryReadFailedInternal()
    {
        var fixture = new ReplicationFixture(CreateTopology("a", "b"), primaryShardId: "a");
        fixture.AddStore("a").GetAllAsync<RefEntity>(Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IReadOnlyList<RefEntity>>.Left(EncinaErrors.Create("test.read_failed", "boom")));

        var result = await fixture.CreateReplicator().ReplicateAsync<RefEntity>();

        AssertLeft(result, ReferenceTableErrorCodes.PrimaryReadFailed, ErrorCategory.Internal);
    }

    [Fact]
    public async Task ReferenceTableReplicator_AllTargetsThrow_IsReplicationFailedInternal()
    {
        var fixture = new ReplicationFixture(CreateTopology("a", "b"), primaryShardId: "a");
        fixture.AddStore("a").GetAllAsync<RefEntity>(Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IReadOnlyList<RefEntity>>.Right((IReadOnlyList<RefEntity>)[new RefEntity()]));
        fixture.AddStore("b").UpsertAsync<RefEntity>(Arg.Any<IEnumerable<RefEntity>>(), Arg.Any<CancellationToken>())
            .Returns<Task<Either<EncinaError, int>>>(_ => throw new InvalidOperationException("upsert failed"));

        var result = await fixture.CreateReplicator().ReplicateAsync<RefEntity>();

        AssertLeft(result, ReferenceTableErrorCodes.ReplicationFailed, ErrorCategory.Internal);
    }

    [Fact]
    public async Task ReferenceTableReplicator_OneTargetThrows_ShardFailureIsReplicationFailedInternal()
    {
        var fixture = new ReplicationFixture(CreateTopology("a", "b", "c"), primaryShardId: "a");
        fixture.AddStore("a").GetAllAsync<RefEntity>(Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IReadOnlyList<RefEntity>>.Right((IReadOnlyList<RefEntity>)[new RefEntity()]));
        fixture.AddStore("b").UpsertAsync<RefEntity>(Arg.Any<IEnumerable<RefEntity>>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, int>.Right(1));
        fixture.AddStore("c").UpsertAsync<RefEntity>(Arg.Any<IEnumerable<RefEntity>>(), Arg.Any<CancellationToken>())
            .Returns<Task<Either<EncinaError, int>>>(_ => throw new InvalidOperationException("upsert failed"));

        var result = await fixture.CreateReplicator().ReplicateAsync<RefEntity>();

        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(r =>
        {
            r.FailedShards.Count.ShouldBe(1);
            AssertError(r.FailedShards[0].Error, ReferenceTableErrorCodes.ReplicationFailed, ErrorCategory.Internal);
        });
    }

    #endregion

    #region PollingRefreshDetector

    private static PollingRefreshDetector CreateDetector(ShardTopology topology)
    {
        var topologyProvider = Substitute.For<IShardTopologyProvider>();
        topologyProvider.GetTopology().Returns(topology);

        return new PollingRefreshDetector(
            Substitute.For<IReferenceTableReplicator>(),
            Substitute.For<IReferenceTableStoreFactory>(),
            topologyProvider,
            Substitute.For<IReferenceTableStateStore>(),
            NullLogger<PollingRefreshDetector>.Instance);
    }

    [Fact]
    public async Task PollingRefreshDetector_EmptyTopology_IsPrimaryShardNotFoundInternal()
    {
        var detector = CreateDetector(CreateTopology());

        var result = await detector.CheckAndReplicateAsync(
            new ReferenceTableConfiguration(typeof(RefEntity), new ReferenceTableOptions()));

        AssertLeft(result, ReferenceTableErrorCodes.PrimaryShardNotFound, ErrorCategory.Internal);
    }

    [Fact]
    public async Task PollingRefreshDetector_ConfiguredPrimaryMissing_IsPrimaryShardNotFoundInternal()
    {
        var detector = CreateDetector(CreateTopology("a"));

        var result = await detector.CheckAndReplicateAsync(
            new ReferenceTableConfiguration(typeof(RefEntity), new ReferenceTableOptions { PrimaryShardId = "ghost" }));

        AssertLeft(result, ReferenceTableErrorCodes.PrimaryShardNotFound, ErrorCategory.Internal);
    }

    #endregion
}

using Encina.Caching;
using Encina.Cdc;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Caching;
using Encina.Cdc.DeadLetter;
using Encina.Cdc.Health;
using Encina.Cdc.Messaging;
using Encina.Cdc.Processing;
using Encina.Cdc.Sharding;
using Encina.Sharding.ReferenceTables;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Encina.GuardTests.Cdc;

/// <summary>
/// Guard tests covering constructor null guards for Encina.Cdc service classes whose guard
/// coverage was previously absent: health checks, processors, handlers, stores and services.
///
/// These tests target the files marked "guard" in .github/coverage-manifest/Encina.Cdc.json
/// that previously had no dedicated guard test file.
/// </summary>
[Trait("Category", "Guard")]
public sealed class CdcHappyPathAndConstructorGuardTests
{
    // ─── CdcDeadLetterHealthCheck ───

    [Fact]
    public void CdcDeadLetterHealthCheck_NullStore_Throws()
    {
        var options = new CdcDeadLetterHealthCheckOptions();
        Should.Throw<ArgumentNullException>(() => new CdcDeadLetterHealthCheck(null!, options));
    }

    [Fact]
    public void CdcDeadLetterHealthCheck_NullOptions_Throws()
    {
        var store = Substitute.For<ICdcDeadLetterStore>();
        Should.Throw<ArgumentNullException>(() => new CdcDeadLetterHealthCheck(store, null!));
    }

    [Fact]
    public void CdcDeadLetterHealthCheck_ValidArgs_Constructs()
    {
        var store = Substitute.For<ICdcDeadLetterStore>();
        var options = new CdcDeadLetterHealthCheckOptions();

        var sut = new CdcDeadLetterHealthCheck(store, options);

        sut.ShouldNotBeNull();
    }

    // ─── ShardedCdcHealthCheck ───

    [Fact]
    public void ShardedCdcHealthCheck_NullConnector_Throws()
    {
        var store = Substitute.For<IShardedCdcPositionStore>();
        Should.Throw<ArgumentNullException>(() => new ShardedCdcHealthCheck(null!, store));
    }

    [Fact]
    public void ShardedCdcHealthCheck_NullPositionStore_Throws()
    {
        var connector = Substitute.For<IShardedCdcConnector>();
        Should.Throw<ArgumentNullException>(() => new ShardedCdcHealthCheck(connector, null!));
    }

    [Fact]
    public void ShardedCdcHealthCheck_ValidArgs_Constructs()
    {
        var connector = Substitute.For<IShardedCdcConnector>();
        var store = Substitute.For<IShardedCdcPositionStore>();

        var sut = new ShardedCdcHealthCheck(connector, store);

        sut.ShouldNotBeNull();
    }

    [Fact]
    public void ShardedCdcHealthCheck_WithProviderTags_Constructs()
    {
        var connector = Substitute.For<IShardedCdcConnector>();
        var store = Substitute.For<IShardedCdcPositionStore>();

        var sut = new ShardedCdcHealthCheck(connector, store, ["custom-tag"]);

        sut.ShouldNotBeNull();
    }

    // ─── CacheInvalidationSubscriberHealthCheck ───

    [Fact]
    public void CacheInvalidationSubscriberHealthCheck_NullPubSubProvider_Throws()
    {
        var options = Options.Create(new QueryCacheInvalidationOptions());
        Should.Throw<ArgumentNullException>(() =>
            new CacheInvalidationSubscriberHealthCheck(null!, options));
    }

    [Fact]
    public void CacheInvalidationSubscriberHealthCheck_NullOptions_Throws()
    {
        var pubSub = Substitute.For<IPubSubProvider>();
        Should.Throw<ArgumentNullException>(() =>
            new CacheInvalidationSubscriberHealthCheck(pubSub, null!));
    }

    [Fact]
    public void CacheInvalidationSubscriberHealthCheck_ValidArgs_Constructs()
    {
        var pubSub = Substitute.For<IPubSubProvider>();
        var options = Options.Create(new QueryCacheInvalidationOptions());

        var sut = new CacheInvalidationSubscriberHealthCheck(pubSub, options);

        sut.ShouldNotBeNull();
    }

    // ─── CacheInvalidationSubscriberService ───

    [Fact]
    public void CacheInvalidationSubscriberService_NullPubSubProvider_Throws()
    {
        var cache = Substitute.For<ICacheProvider>();
        var options = Options.Create(new QueryCacheInvalidationOptions());

        Should.Throw<ArgumentNullException>(() => new CacheInvalidationSubscriberService(
            null!,
            cache,
            options,
            NullLogger<CacheInvalidationSubscriberService>.Instance));
    }

    [Fact]
    public void CacheInvalidationSubscriberService_NullCacheProvider_Throws()
    {
        var pubSub = Substitute.For<IPubSubProvider>();
        var options = Options.Create(new QueryCacheInvalidationOptions());

        Should.Throw<ArgumentNullException>(() => new CacheInvalidationSubscriberService(
            pubSub,
            null!,
            options,
            NullLogger<CacheInvalidationSubscriberService>.Instance));
    }

    [Fact]
    public void CacheInvalidationSubscriberService_NullOptions_Throws()
    {
        var pubSub = Substitute.For<IPubSubProvider>();
        var cache = Substitute.For<ICacheProvider>();

        Should.Throw<ArgumentNullException>(() => new CacheInvalidationSubscriberService(
            pubSub,
            cache,
            null!,
            NullLogger<CacheInvalidationSubscriberService>.Instance));
    }

    [Fact]
    public void CacheInvalidationSubscriberService_NullLogger_Throws()
    {
        var pubSub = Substitute.For<IPubSubProvider>();
        var cache = Substitute.For<ICacheProvider>();
        var options = Options.Create(new QueryCacheInvalidationOptions());

        Should.Throw<ArgumentNullException>(() => new CacheInvalidationSubscriberService(
            pubSub,
            cache,
            options,
            null!));
    }

    [Fact]
    public void CacheInvalidationSubscriberService_ValidArgs_Constructs()
    {
        var pubSub = Substitute.For<IPubSubProvider>();
        var cache = Substitute.For<ICacheProvider>();
        var options = Options.Create(new QueryCacheInvalidationOptions());

        var sut = new CacheInvalidationSubscriberService(
            pubSub,
            cache,
            options,
            NullLogger<CacheInvalidationSubscriberService>.Instance);

        sut.ShouldNotBeNull();
    }

    // ─── OutboxCdcHandler ───

    [Fact]
    public void OutboxCdcHandler_NullEncina_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new OutboxCdcHandler(null!, NullLogger<OutboxCdcHandler>.Instance));
    }

    [Fact]
    public void OutboxCdcHandler_NullLogger_Throws()
    {
        var encina = Substitute.For<IEncina>();
        Should.Throw<ArgumentNullException>(() => new OutboxCdcHandler(encina, null!));
    }

    [Fact]
    public void OutboxCdcHandler_ValidArgs_Constructs()
    {
        var encina = Substitute.For<IEncina>();
        var sut = new OutboxCdcHandler(encina, NullLogger<OutboxCdcHandler>.Instance);
        sut.ShouldNotBeNull();
    }

    // ─── CdcProcessor ───

    [Fact]
    public void CdcProcessor_NullServiceProvider_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new CdcProcessor(
            null!,
            NullLogger<CdcProcessor>.Instance,
            new CdcOptions()));
    }

    [Fact]
    public void CdcProcessor_NullLogger_Throws()
    {
        var sp = Substitute.For<IServiceProvider>();
        Should.Throw<ArgumentNullException>(() => new CdcProcessor(sp, null!, new CdcOptions()));
    }

    [Fact]
    public void CdcProcessor_NullOptions_Throws()
    {
        var sp = Substitute.For<IServiceProvider>();
        Should.Throw<ArgumentNullException>(() => new CdcProcessor(
            sp,
            NullLogger<CdcProcessor>.Instance,
            null!));
    }

    [Fact]
    public void CdcProcessor_ValidArgs_Constructs()
    {
        var sp = Substitute.For<IServiceProvider>();
        var sut = new CdcProcessor(sp, NullLogger<CdcProcessor>.Instance, new CdcOptions());
        sut.ShouldNotBeNull();
    }

    // ─── ShardedCdcProcessor ───

    [Fact]
    public void ShardedCdcProcessor_NullServiceProvider_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new ShardedCdcProcessor(
            null!,
            NullLogger<ShardedCdcProcessor>.Instance,
            new CdcOptions()));
    }

    [Fact]
    public void ShardedCdcProcessor_NullLogger_Throws()
    {
        var sp = Substitute.For<IServiceProvider>();
        Should.Throw<ArgumentNullException>(() => new ShardedCdcProcessor(sp, null!, new CdcOptions()));
    }

    [Fact]
    public void ShardedCdcProcessor_NullOptions_Throws()
    {
        var sp = Substitute.For<IServiceProvider>();
        Should.Throw<ArgumentNullException>(() => new ShardedCdcProcessor(
            sp,
            NullLogger<ShardedCdcProcessor>.Instance,
            null!));
    }

    [Fact]
    public void ShardedCdcProcessor_ValidArgs_Constructs()
    {
        var sp = Substitute.For<IServiceProvider>();
        var sut = new ShardedCdcProcessor(sp, NullLogger<ShardedCdcProcessor>.Instance, new CdcOptions());
        sut.ShouldNotBeNull();
    }

    // ─── CdcHealthCheck (via derived test subclass to exercise protected ctor) ───

    [Fact]
    public void CdcHealthCheck_DerivedNullConnector_Throws()
    {
        var store = Substitute.For<ICdcPositionStore>();
        Should.Throw<ArgumentNullException>(() => new DerivedCdcHealthCheck(null!, store));
    }

    [Fact]
    public void CdcHealthCheck_DerivedNullPositionStore_Throws()
    {
        var connector = Substitute.For<ICdcConnector>();
        Should.Throw<ArgumentNullException>(() => new DerivedCdcHealthCheck(connector, null!));
    }

    [Fact]
    public void CdcHealthCheck_DerivedValidArgs_Constructs()
    {
        var connector = Substitute.For<ICdcConnector>();
        var store = Substitute.For<ICdcPositionStore>();
        var sut = new DerivedCdcHealthCheck(connector, store);
        sut.ShouldNotBeNull();
    }

    // ─── InMemoryCdcDeadLetterStore (internal) ───

    [Fact]
    public async Task InMemoryCdcDeadLetterStore_AddThenGetPending_ReturnsEntry()
    {
        var store = new InMemoryCdcDeadLetterStore();
        var entry = new CdcDeadLetterEntry(
            Id: Guid.NewGuid(),
            OriginalEvent: new ChangeEvent(
                "Orders",
                ChangeOperation.Insert,
                null,
                new { Id = 1 },
                new ChangeMetadata(new TestCdcPosition(1), DateTime.UtcNow, null, null, null)),
            ErrorMessage: "failure",
            StackTrace: "stack",
            RetryCount: 3,
            FailedAtUtc: DateTime.UtcNow,
            ConnectorId: "test",
            Status: CdcDeadLetterStatus.Pending);

        var addResult = await store.AddAsync(entry);
        addResult.IsRight.ShouldBeTrue();

        var pending = await store.GetPendingAsync(10);
        pending.IsRight.ShouldBeTrue();
        pending.IfRight(list => list.Count.ShouldBe(1));
    }

    [Fact]
    public async Task InMemoryCdcDeadLetterStore_ResolveUnknownEntry_ReturnsError()
    {
        var store = new InMemoryCdcDeadLetterStore();
        var result = await store.ResolveAsync(Guid.NewGuid(), CdcDeadLetterResolution.Replay);
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task InMemoryCdcDeadLetterStore_ResolveTwice_SecondReturnsError()
    {
        var store = new InMemoryCdcDeadLetterStore();
        var entry = new CdcDeadLetterEntry(
            Id: Guid.NewGuid(),
            OriginalEvent: new ChangeEvent(
                "Orders",
                ChangeOperation.Insert,
                null,
                null,
                new ChangeMetadata(new TestCdcPosition(1), DateTime.UtcNow, null, null, null)),
            ErrorMessage: "err",
            StackTrace: "st",
            RetryCount: 1,
            FailedAtUtc: DateTime.UtcNow,
            ConnectorId: "c",
            Status: CdcDeadLetterStatus.Pending);

        await store.AddAsync(entry);

        var first = await store.ResolveAsync(entry.Id, CdcDeadLetterResolution.Discard);
        first.IsRight.ShouldBeTrue();

        var second = await store.ResolveAsync(entry.Id, CdcDeadLetterResolution.Discard);
        second.IsLeft.ShouldBeTrue();
    }

    // ─── CdcDrivenRefreshHandler (internal) ───

    [Fact]
    public void CdcDrivenRefreshHandler_NullReplicator_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new CdcDrivenRefreshHandler<TestEntity>(
            null!,
            NullLogger<CdcDrivenRefreshHandler<TestEntity>>.Instance));
    }

    [Fact]
    public void CdcDrivenRefreshHandler_NullLogger_Throws()
    {
        var replicator = Substitute.For<IReferenceTableReplicator>();
        Should.Throw<ArgumentNullException>(() => new CdcDrivenRefreshHandler<TestEntity>(replicator, null!));
    }

    [Fact]
    public void CdcDrivenRefreshHandler_ValidArgs_Constructs()
    {
        var replicator = Substitute.For<IReferenceTableReplicator>();
        var sut = new CdcDrivenRefreshHandler<TestEntity>(
            replicator,
            NullLogger<CdcDrivenRefreshHandler<TestEntity>>.Instance);
        sut.ShouldNotBeNull();
    }

    // ─── ServiceCollectionExtensions guard ───

    [Fact]
    public void AddEncinaCdc_NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaCdc(_ => { }));
    }

    [Fact]
    public void AddEncinaCdc_NullConfigure_Throws()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() => services.AddEncinaCdc(null!));
    }

    // ─── Test helpers ───

    private sealed class DerivedCdcHealthCheck : CdcHealthCheck
    {
        public DerivedCdcHealthCheck(ICdcConnector connector, ICdcPositionStore positionStore)
            : base("derived-test", connector, positionStore)
        {
        }
    }

    private sealed class TestEntity;

    private sealed class TestCdcPosition(long value) : CdcPosition
    {
        public long Value { get; } = value;
        public override byte[] ToBytes() => BitConverter.GetBytes(Value);
        public override int CompareTo(CdcPosition? other) => other is TestCdcPosition t ? Value.CompareTo(t.Value) : 1;
        public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}

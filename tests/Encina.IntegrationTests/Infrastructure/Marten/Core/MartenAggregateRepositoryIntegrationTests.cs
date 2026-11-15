using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;
using Encina.IntegrationTests.Infrastructure.Marten.Snapshots;
using Encina.Marten;
using Encina.Marten.Snapshots;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Encina.IntegrationTests.Infrastructure.Marten.Core;

/// <summary>
/// Integration tests for <see cref="MartenAggregateRepository{TAggregate}"/> against a real PostgreSQL database.
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public sealed class MartenAggregateRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly MartenFixture _fixture;

    public MartenAggregateRepositoryIntegrationTests(MartenFixture fixture)
    {
        _fixture = fixture;
    }

    public ValueTask InitializeAsync()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "Marten PostgreSQL container not available");
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static MartenAggregateRepository<TestSnapshotableAggregate> CreateRepo(global::Marten.IDocumentSession session)
    {
        return new MartenAggregateRepository<TestSnapshotableAggregate>(
            session,
            Substitute.For<IRequestContext>(),
            NullLogger<MartenAggregateRepository<TestSnapshotableAggregate>>.Instance,
            Microsoft.Extensions.Options.Options.Create(new EncinaMartenOptions()));
    }

    [Fact]
    public async Task CreateAsync_NewAggregate_PersistsEvents()
    {
        await using var session = _fixture.Store!.LightweightSession();
        var repo = CreateRepo(session);

        var aggregate = new TestSnapshotableAggregate(Guid.NewGuid(), "Test Order");
        aggregate.AddItem(50m);

        var result = await repo.CreateAsync(aggregate);

        result.IsRight.ShouldBeTrue($"CreateAsync should succeed: {result}");
    }

    [Fact]
    public async Task CreateAsync_ThenLoad_ReturnsAggregate()
    {
        await using var session = _fixture.Store!.LightweightSession();
        var repo = CreateRepo(session);

        var id = Guid.NewGuid();
        var aggregate = new TestSnapshotableAggregate(id, "LoadTest");
        aggregate.AddItem(100m);
        aggregate.AddItem(200m);

        await repo.CreateAsync(aggregate);

        // Load from a fresh session
        await using var loadSession = _fixture.Store.LightweightSession();
        var loadRepo = CreateRepo(loadSession);

        var loaded = await loadRepo.LoadAsync(id);

        loaded.IsRight.ShouldBeTrue($"LoadAsync should succeed: {loaded}");
        loaded.IfRight(agg =>
        {
            agg.Name.ShouldBe("LoadTest");
            agg.Total.ShouldBe(300m);
            agg.ItemCount.ShouldBe(2);
        });
    }

    [Fact]
    public async Task SaveAsync_ExistingAggregate_AppendsEvents()
    {
        await using var session = _fixture.Store!.LightweightSession();
        var repo = CreateRepo(session);

        var id = Guid.NewGuid();
        var aggregate = new TestSnapshotableAggregate(id, "SaveTest");
        await repo.CreateAsync(aggregate);

        // Load and modify
        await using var updateSession = _fixture.Store.LightweightSession();
        var updateRepo = CreateRepo(updateSession);
        var loadResult = await updateRepo.LoadAsync(id);

        loadResult.IsRight.ShouldBeTrue();
        var loaded = loadResult.Match(agg => agg, _ => null!);
        loaded.AddItem(500m);

        var saveResult = await updateRepo.SaveAsync(loaded);

        saveResult.IsRight.ShouldBeTrue($"SaveAsync should succeed: {saveResult}");

        // Verify from a third session that the stream now holds both events
        await using var verifySession = _fixture.Store.LightweightSession();
        var verified = await CreateRepo(verifySession).LoadAsync(id);
        verified.IsRight.ShouldBeTrue();
        verified.IfRight(agg =>
        {
            agg.Total.ShouldBe(500m);
            agg.Version.ShouldBe(2);
        });
    }

    [Fact]
    public async Task SaveAsync_StreamModifiedByAnotherSession_ReturnsConcurrencyConflict()
    {
        // Marten 9 defaults to EventAppendMode.QuickWithServerTimestamps; this test pins that the
        // expected-version check behind SaveAsync is still enforced under the default store options.
        await using var createSession = _fixture.Store!.LightweightSession();
        var id = Guid.NewGuid();
        var created = await CreateRepo(createSession).CreateAsync(new TestSnapshotableAggregate(id, "ConcurrencyTest"));
        created.IsRight.ShouldBeTrue($"CreateAsync should succeed: {created}");

        // Two independent sessions load the same stream at version 1
        await using var sessionA = _fixture.Store.LightweightSession();
        await using var sessionB = _fixture.Store.LightweightSession();
        var repoA = CreateRepo(sessionA);
        var repoB = CreateRepo(sessionB);

        var aggregateA = (await repoA.LoadAsync(id)).Match(agg => agg, _ => null!);
        var aggregateB = (await repoB.LoadAsync(id)).Match(agg => agg, _ => null!);
        aggregateA.ShouldNotBeNull();
        aggregateB.ShouldNotBeNull();

        // First writer wins
        aggregateA.AddItem(10m);
        var saveA = await repoA.SaveAsync(aggregateA);
        saveA.IsRight.ShouldBeTrue($"first save should succeed: {saveA}");

        // Second writer appends on a stale version and must be rejected, not silently interleaved
        aggregateB.AddItem(20m);
        var saveB = await repoB.SaveAsync(aggregateB);

        saveB.IsLeft.ShouldBeTrue("second save on a stale version must fail with a concurrency conflict");
        var error = saveB.LeftToSeq().Head;
        error.GetCode().Match(
            code => code.ShouldBe(MartenErrorCodes.ConcurrencyConflict),
            () => throw new ShouldAssertException("Expected a concurrency error code but got none"));

        // The stream holds exactly the winner's event
        await using var verifySession = _fixture.Store.LightweightSession();
        var verified = await CreateRepo(verifySession).LoadAsync(id);
        verified.IsRight.ShouldBeTrue();
        verified.IfRight(agg =>
        {
            agg.Version.ShouldBe(2);
            agg.Total.ShouldBe(10m);
        });
    }

    [Fact]
    public async Task LoadAsync_NonExistentId_ReturnsLeft()
    {
        await using var session = _fixture.Store!.LightweightSession();
        var repo = CreateRepo(session);

        var result = await repo.LoadAsync(Guid.NewGuid());

        result.IsLeft.ShouldBeTrue("Loading non-existent aggregate should return Left");
    }

    [Fact]
    public async Task LoadAsync_AtVersion_ReturnsAggregateAtThatVersion()
    {
        await using var session = _fixture.Store!.LightweightSession();
        var repo = CreateRepo(session);

        var id = Guid.NewGuid();
        var aggregate = new TestSnapshotableAggregate(id, "VersionTest");
        aggregate.AddItem(10m);  // event 2
        aggregate.AddItem(20m);  // event 3
        aggregate.AddItem(30m);  // event 4

        await repo.CreateAsync(aggregate);

        // Load at version 2 (only Created + first AddItem)
        await using var loadSession = _fixture.Store.LightweightSession();
        var loadRepo = CreateRepo(loadSession);
        var result = await loadRepo.LoadAsync(id, 2);

        result.IsRight.ShouldBeTrue();
        result.IfRight(agg =>
        {
            agg.Name.ShouldBe("VersionTest");
            agg.Total.ShouldBe(10m);
            agg.ItemCount.ShouldBe(1);
        });
    }

    [Fact]
    public async Task CreateAsync_MultipleAggregates_AllPersist()
    {
        for (int i = 0; i < 3; i++)
        {
            await using var session = _fixture.Store!.LightweightSession();
            var repo = CreateRepo(session);

            var aggregate = new TestSnapshotableAggregate(Guid.NewGuid(), $"Multi-{i}");
            var result = await repo.CreateAsync(aggregate);
            result.IsRight.ShouldBeTrue($"Aggregate {i} should persist");
        }
    }

    // #3787: a failed SaveChangesAsync must leave the scoped session clean so later writes succeed.

    private static void ShouldHaveErrorCode(Either<EncinaError, Unit> result, string expectedCode)
    {
        result.IsLeft.ShouldBeTrue("the operation should fail");
        result.LeftToSeq().Head.GetCode().Match(
            code => code.ShouldBe(expectedCode),
            () => throw new ShouldAssertException("Expected an error code but got none"));
    }

    private static SnapshotAwareAggregateRepository<TestSnapshotableAggregate> CreateSnapshotAwareRepo(global::Marten.IDocumentSession session)
    {
        return new SnapshotAwareAggregateRepository<TestSnapshotableAggregate>(
            session,
            new MartenSnapshotStore<TestSnapshotableAggregate>(
                session,
                NullLogger<MartenSnapshotStore<TestSnapshotableAggregate>>.Instance),
            Substitute.For<IRequestContext>(),
            NullLogger<SnapshotAwareAggregateRepository<TestSnapshotableAggregate>>.Instance,
            Microsoft.Extensions.Options.Options.Create(new EncinaMartenOptions()));
    }

    [Fact]
    public async Task CreateAsync_StreamAlreadyExists_ThenSaveOnSameSession_Succeeds()
    {
        await using var session = _fixture.Store!.LightweightSession();
        var repo = CreateRepo(session);
        var id = Guid.NewGuid();

        var original = new TestSnapshotableAggregate(id, "Collision");
        (await repo.CreateAsync(original)).IsRight.ShouldBeTrue();

        // Same fixed stream id: the second create collides and returns Left
        var duplicate = await repo.CreateAsync(new TestSnapshotableAggregate(id, "Collision"));
        ShouldHaveErrorCode(duplicate, MartenErrorCodes.StreamAlreadyExists);

        // A later write on the same session must not replay the failed StartStream
        original.AddItem(25m);
        var save = await repo.SaveAsync(original);

        save.IsRight.ShouldBeTrue($"SaveAsync after a collision should succeed: {save}");

        await using var verifySession = _fixture.Store.LightweightSession();
        var verified = await CreateRepo(verifySession).LoadAsync(id);
        verified.IsRight.ShouldBeTrue();
        verified.IfRight(agg => agg.Total.ShouldBe(25m));
    }

    [Fact]
    public async Task SaveAsync_ConcurrencyConflict_ThenCreateOnSameSession_Succeeds()
    {
        await using var createSession = _fixture.Store!.LightweightSession();
        var id = Guid.NewGuid();
        (await CreateRepo(createSession).CreateAsync(new TestSnapshotableAggregate(id, "Conflict"))).IsRight.ShouldBeTrue();

        await using var sessionA = _fixture.Store.LightweightSession();
        await using var sessionB = _fixture.Store.LightweightSession();
        var repoA = CreateRepo(sessionA);
        var repoB = CreateRepo(sessionB);
        var aggregateA = (await repoA.LoadAsync(id)).Match(agg => agg, _ => null!);
        var aggregateB = (await repoB.LoadAsync(id)).Match(agg => agg, _ => null!);

        aggregateB.AddItem(1m);
        (await repoB.SaveAsync(aggregateB)).IsRight.ShouldBeTrue();

        aggregateA.AddItem(2m);
        ShouldHaveErrorCode(await repoA.SaveAsync(aggregateA), MartenErrorCodes.ConcurrencyConflict);

        // The rejected append must not be replayed with the next write on session A
        var other = new TestSnapshotableAggregate(Guid.NewGuid(), "After conflict");
        var create = await repoA.CreateAsync(other);

        create.IsRight.ShouldBeTrue($"CreateAsync after a concurrency conflict should succeed: {create}");
    }

    [Fact]
    public async Task SnapshotAware_CreateAsync_StreamAlreadyExists_ThenSaveOnSameSession_Succeeds()
    {
        await using var session = _fixture.Store!.LightweightSession();
        var repo = CreateSnapshotAwareRepo(session);
        var id = Guid.NewGuid();

        var original = new TestSnapshotableAggregate(id, "SnapshotCollision");
        (await repo.CreateAsync(original)).IsRight.ShouldBeTrue();

        var duplicate = await repo.CreateAsync(new TestSnapshotableAggregate(id, "SnapshotCollision"));
        ShouldHaveErrorCode(duplicate, MartenErrorCodes.StreamAlreadyExists);

        original.AddItem(40m);
        var save = await repo.SaveAsync(original);

        save.IsRight.ShouldBeTrue($"SaveAsync after a collision should succeed: {save}");
    }

    [Fact]
    public async Task SnapshotAware_SaveAsync_ConcurrencyConflict_ThenCreateOnSameSession_Succeeds()
    {
        await using var createSession = _fixture.Store!.LightweightSession();
        var id = Guid.NewGuid();
        (await CreateSnapshotAwareRepo(createSession).CreateAsync(new TestSnapshotableAggregate(id, "SnapshotConflict")))
            .IsRight.ShouldBeTrue();

        await using var sessionA = _fixture.Store.LightweightSession();
        await using var sessionB = _fixture.Store.LightweightSession();
        var repoA = CreateSnapshotAwareRepo(sessionA);
        var repoB = CreateSnapshotAwareRepo(sessionB);
        var aggregateA = (await repoA.LoadAsync(id)).Match(agg => agg, _ => null!);
        var aggregateB = (await repoB.LoadAsync(id)).Match(agg => agg, _ => null!);

        aggregateB.AddItem(1m);
        (await repoB.SaveAsync(aggregateB)).IsRight.ShouldBeTrue();

        aggregateA.AddItem(2m);
        ShouldHaveErrorCode(await repoA.SaveAsync(aggregateA), MartenErrorCodes.ConcurrencyConflict);

        var create = await repoA.CreateAsync(new TestSnapshotableAggregate(Guid.NewGuid(), "After conflict"));

        create.IsRight.ShouldBeTrue($"CreateAsync after a concurrency conflict should succeed: {create}");
    }
}

using Encina.Marten.IntegrationTests.Fixtures;
using Encina.Marten.Snapshots;
using Marten;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Encina.Marten.IntegrationTests.Snapshots;

/// <summary>
/// Integration tests for MartenSnapshotStore against a real PostgreSQL database.
/// </summary>
[Collection(MartenCollection.Name)]
public sealed class MartenSnapshotStoreIntegrationTests
{
    private readonly MartenFixture _fixture;

    public MartenSnapshotStoreIntegrationTests(MartenFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    [Trait("Category", "Integration")]
    public async Task SaveAsync_PersistsSnapshotToDatabase()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

        // Arrange
        await using var session = _fixture.Store!.LightweightSession();
        var logger = Substitute.For<ILogger<MartenSnapshotStore<TestSnapshotableAggregate>>>();
        var store = new MartenSnapshotStore<TestSnapshotableAggregate>(session, logger);

        var aggregateId = Guid.NewGuid();
        var aggregate = new TestSnapshotableAggregate(aggregateId, "Test Order");
        aggregate.AddItem(100m);
        aggregate.AddItem(200m);

        var snapshot = new Snapshot<TestSnapshotableAggregate>(
            aggregateId,
            2,
            aggregate,
            DateTime.UtcNow);

        // Act
        await store.SaveAsync(snapshot);

        // Assert - verify using a new session
        await using var verifySession = _fixture.Store.LightweightSession();
        var envelopes = await verifySession
            .Query<SnapshotEnvelope<TestSnapshotableAggregate>>()
            .Where(e => e.AggregateId == aggregateId)
            .ToListAsync();

        envelopes.Should().HaveCount(1);
        envelopes[0].Version.Should().Be(2);
        envelopes[0].State.Should().NotBeNull();
        envelopes[0].State!.Name.Should().Be("Test Order");
        envelopes[0].State!.Total.Should().Be(300m);
    }

    [SkippableFact]
    [Trait("Category", "Integration")]
    public async Task GetLatestAsync_ReturnsLatestSnapshot()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

        // Arrange
        await using var session = _fixture.Store!.LightweightSession();
        var logger = Substitute.For<ILogger<MartenSnapshotStore<TestSnapshotableAggregate>>>();
        var store = new MartenSnapshotStore<TestSnapshotableAggregate>(session, logger);

        var aggregateId = Guid.NewGuid();
        var aggregate = new TestSnapshotableAggregate(aggregateId, "Test");

        // Save multiple snapshots
        for (int i = 1; i <= 3; i++)
        {
            aggregate.AddItem(i * 10m);
            var snapshot = new Snapshot<TestSnapshotableAggregate>(
                aggregateId,
                i,
                aggregate,
                DateTime.UtcNow);
            await store.SaveAsync(snapshot);
        }

        // Act
        var result = await store.GetLatestAsync(aggregateId);

        // Assert
        result.IsRight.Should().BeTrue();
        result.IfRight(snapshot =>
        {
            snapshot.Should().NotBeNull();
            snapshot!.Version.Should().Be(3);
            snapshot.State.Total.Should().Be(60m); // 10 + 20 + 30
        });
    }

    [SkippableFact]
    [Trait("Category", "Integration")]
    public async Task GetLatestAsync_NonExistentAggregate_ReturnsNull()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

        // Arrange
        await using var session = _fixture.Store!.LightweightSession();
        var logger = Substitute.For<ILogger<MartenSnapshotStore<TestSnapshotableAggregate>>>();
        var store = new MartenSnapshotStore<TestSnapshotableAggregate>(session, logger);

        // Act
        var result = await store.GetLatestAsync(Guid.NewGuid());

        // Assert
        result.IsRight.Should().BeTrue();
        result.IfRight(snapshot => snapshot.Should().BeNull());
    }

    [SkippableFact]
    [Trait("Category", "Integration")]
    public async Task PruneAsync_RemovesOldSnapshots()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

        // Arrange
        await using var session = _fixture.Store!.LightweightSession();
        var logger = Substitute.For<ILogger<MartenSnapshotStore<TestSnapshotableAggregate>>>();
        var store = new MartenSnapshotStore<TestSnapshotableAggregate>(session, logger);

        var aggregateId = Guid.NewGuid();
        var aggregate = new TestSnapshotableAggregate(aggregateId, "Prune Test");

        // Save 5 snapshots
        for (int i = 1; i <= 5; i++)
        {
            aggregate.AddItem(i * 10m);
            var snapshot = new Snapshot<TestSnapshotableAggregate>(
                aggregateId,
                i,
                aggregate,
                DateTime.UtcNow);
            await store.SaveAsync(snapshot);
        }

        // Act - keep only 2 snapshots
        await store.PruneAsync(aggregateId, keepCount: 2);

        // Assert - verify using a new session
        await using var verifySession = _fixture.Store.LightweightSession();
        var envelopes = await verifySession
            .Query<SnapshotEnvelope<TestSnapshotableAggregate>>()
            .Where(e => e.AggregateId == aggregateId)
            .OrderByDescending(e => e.Version)
            .ToListAsync();

        envelopes.Should().HaveCount(2);
        envelopes[0].Version.Should().Be(5);
        envelopes[1].Version.Should().Be(4);
    }

    [SkippableFact]
    [Trait("Category", "Integration")]
    public async Task RoundTrip_PreservesAggregateState()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

        // Arrange
        await using var session = _fixture.Store!.LightweightSession();
        var logger = Substitute.For<ILogger<MartenSnapshotStore<TestSnapshotableAggregate>>>();
        var store = new MartenSnapshotStore<TestSnapshotableAggregate>(session, logger);

        var aggregateId = Guid.NewGuid();
        var originalAggregate = new TestSnapshotableAggregate(aggregateId, "Round Trip Test");
        originalAggregate.AddItem(100m);
        originalAggregate.AddItem(50.50m);
        originalAggregate.Complete();

        var originalSnapshot = new Snapshot<TestSnapshotableAggregate>(
            aggregateId,
            3,
            originalAggregate,
            DateTime.UtcNow);

        // Act
        await store.SaveAsync(originalSnapshot);
        var result = await store.GetLatestAsync(aggregateId);

        // Assert
        result.IsRight.Should().BeTrue();
        result.IfRight(restored =>
        {
            restored.Should().NotBeNull();
            restored!.AggregateId.Should().Be(aggregateId);
            restored.Version.Should().Be(3);
            restored.State.Name.Should().Be("Round Trip Test");
            restored.State.Total.Should().Be(150.50m);
            restored.State.ItemCount.Should().Be(2);
            restored.State.Status.Should().Be("Completed");
        });
    }

    [SkippableFact]
    [Trait("Category", "Integration")]
    public async Task MultipleAggregates_AreIsolated()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

        // Arrange
        await using var session = _fixture.Store!.LightweightSession();
        var logger = Substitute.For<ILogger<MartenSnapshotStore<TestSnapshotableAggregate>>>();
        var store = new MartenSnapshotStore<TestSnapshotableAggregate>(session, logger);

        var aggregateId1 = Guid.NewGuid();
        var aggregateId2 = Guid.NewGuid();

        var aggregate1 = new TestSnapshotableAggregate(aggregateId1, "Aggregate 1");
        aggregate1.AddItem(100m);

        var aggregate2 = new TestSnapshotableAggregate(aggregateId2, "Aggregate 2");
        aggregate2.AddItem(200m);

        await store.SaveAsync(new Snapshot<TestSnapshotableAggregate>(
            aggregateId1, 1, aggregate1, DateTime.UtcNow));
        await store.SaveAsync(new Snapshot<TestSnapshotableAggregate>(
            aggregateId2, 1, aggregate2, DateTime.UtcNow));

        // Act
        var result1 = await store.GetLatestAsync(aggregateId1);
        var result2 = await store.GetLatestAsync(aggregateId2);

        // Assert
        result1.IsRight.Should().BeTrue();
        result2.IsRight.Should().BeTrue();

        result1.IfRight(s => s!.State.Name.Should().Be("Aggregate 1"));
        result1.IfRight(s => s!.State.Total.Should().Be(100m));

        result2.IfRight(s => s!.State.Name.Should().Be("Aggregate 2"));
        result2.IfRight(s => s!.State.Total.Should().Be(200m));
    }
}

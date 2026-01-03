using Encina.Marten.Snapshots;

namespace Encina.Marten.Tests.Snapshots;

public sealed class SnapshotEnvelopeTests
{
    [Fact]
    public void Create_SetsAllProperties()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var aggregate = new TestSnapshotableAggregate();
        var createdAt = DateTime.UtcNow;
        var snapshot = new Snapshot<TestSnapshotableAggregate>(
            aggregateId, 10, aggregate, createdAt);

        // Act
        var envelope = SnapshotEnvelope.Create(snapshot);

        // Assert
        envelope.AggregateId.ShouldBe(aggregateId);
        envelope.Version.ShouldBe(10);
        envelope.State.ShouldBe(aggregate);
        envelope.CreatedAtUtc.ShouldBe(createdAt);
        envelope.AggregateType.ShouldContain("TestSnapshotableAggregate");
    }

    [Fact]
    public void Create_GeneratesCompositeId()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var aggregate = new TestSnapshotableAggregate();
        var snapshot = new Snapshot<TestSnapshotableAggregate>(
            aggregateId, 5, aggregate, DateTime.UtcNow);

        // Act
        var envelope = SnapshotEnvelope.Create(snapshot);

        // Assert
        envelope.Id.ShouldContain("TestSnapshotableAggregate");
        envelope.Id.ShouldContain(aggregateId.ToString());
        envelope.Id.ShouldContain("5");
    }

    [Fact]
    public void Create_GeneratesExpectedIdFormat()
    {
        // Arrange
        var aggregateId = Guid.Parse("12345678-1234-1234-1234-123456789012");
        var aggregate = new TestSnapshotableAggregate();
        var snapshot = new Snapshot<TestSnapshotableAggregate>(
            aggregateId, 42, aggregate, DateTime.UtcNow);

        // Act
        var envelope = SnapshotEnvelope.Create(snapshot);

        // Assert
        envelope.Id.ShouldBe("TestSnapshotableAggregate:12345678-1234-1234-1234-123456789012:42");
    }

    [Fact]
    public void Create_NullSnapshot_ThrowsArgumentNullException()
    {
        // Act
        var act = () => SnapshotEnvelope.Create<TestSnapshotableAggregate>(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void ToSnapshot_ReturnsCorrectSnapshot()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var aggregate = new TestSnapshotableAggregate();
        var createdAt = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var envelope = new SnapshotEnvelope<TestSnapshotableAggregate>
        {
            Id = $"TestSnapshotableAggregate:{aggregateId}:10",
            AggregateId = aggregateId,
            Version = 10,
            State = aggregate,
            CreatedAtUtc = createdAt,
            AggregateType = typeof(TestSnapshotableAggregate).FullName!
        };

        // Act
        var snapshot = envelope.ToSnapshot();

        // Assert
        snapshot.AggregateId.ShouldBe(aggregateId);
        snapshot.Version.ShouldBe(10);
        snapshot.State.ShouldBe(aggregate);
        snapshot.CreatedAtUtc.ShouldBe(createdAt);
    }

    [Fact]
    public void ToSnapshot_WithNullState_ThrowsInvalidOperationException()
    {
        // Arrange
        var envelope = new SnapshotEnvelope<TestSnapshotableAggregate>
        {
            Id = "test:id:1",
            AggregateId = Guid.NewGuid(),
            Version = 1,
            State = null,
            CreatedAtUtc = DateTime.UtcNow,
            AggregateType = typeof(TestSnapshotableAggregate).FullName!
        };

        // Act
        var act = () => envelope.ToSnapshot();

        // Assert
        var ex = Should.Throw<InvalidOperationException>(act);
        ex.Message.ShouldContain("State is null");
    }

    [Fact]
    public void RoundTrip_PreservesAllData()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var aggregate = new TestSnapshotableAggregate(aggregateId, "Test");
        aggregate.AddItem(100m);
        var createdAt = DateTime.UtcNow;
        var originalSnapshot = new Snapshot<TestSnapshotableAggregate>(
            aggregateId, 2, aggregate, createdAt);

        // Act
        var envelope = SnapshotEnvelope.Create(originalSnapshot);
        var restoredSnapshot = envelope.ToSnapshot();

        // Assert
        restoredSnapshot.AggregateId.ShouldBe(originalSnapshot.AggregateId);
        restoredSnapshot.Version.ShouldBe(originalSnapshot.Version);
        restoredSnapshot.CreatedAtUtc.ShouldBe(originalSnapshot.CreatedAtUtc);
        restoredSnapshot.State.Name.ShouldBe("Test");
        restoredSnapshot.State.Total.ShouldBe(100m);
    }

    [Fact]
    public void Envelope_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var envelope = new SnapshotEnvelope<TestSnapshotableAggregate>();

        // Assert
        envelope.Id.ShouldBeEmpty();
        envelope.AggregateId.ShouldBe(Guid.Empty);
        envelope.Version.ShouldBe(0);
        envelope.State.ShouldBeNull();
        envelope.CreatedAtUtc.ShouldBe(default);
        envelope.AggregateType.ShouldBeEmpty();
    }
}

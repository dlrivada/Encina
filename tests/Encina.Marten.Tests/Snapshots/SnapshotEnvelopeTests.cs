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
        envelope.AggregateId.Should().Be(aggregateId);
        envelope.Version.Should().Be(10);
        envelope.State.Should().Be(aggregate);
        envelope.CreatedAtUtc.Should().Be(createdAt);
        envelope.AggregateType.Should().Contain("TestSnapshotableAggregate");
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
        envelope.Id.Should().Contain("TestSnapshotableAggregate");
        envelope.Id.Should().Contain(aggregateId.ToString());
        envelope.Id.Should().Contain("5");
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
        envelope.Id.Should().Be("TestSnapshotableAggregate:12345678-1234-1234-1234-123456789012:42");
    }

    [Fact]
    public void Create_NullSnapshot_ThrowsArgumentNullException()
    {
        // Act
        var act = () => SnapshotEnvelope.Create<TestSnapshotableAggregate>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
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
        snapshot.AggregateId.Should().Be(aggregateId);
        snapshot.Version.Should().Be(10);
        snapshot.State.Should().Be(aggregate);
        snapshot.CreatedAtUtc.Should().Be(createdAt);
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*State is null*");
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
        restoredSnapshot.AggregateId.Should().Be(originalSnapshot.AggregateId);
        restoredSnapshot.Version.Should().Be(originalSnapshot.Version);
        restoredSnapshot.CreatedAtUtc.Should().Be(originalSnapshot.CreatedAtUtc);
        restoredSnapshot.State.Name.Should().Be("Test");
        restoredSnapshot.State.Total.Should().Be(100m);
    }

    [Fact]
    public void Envelope_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var envelope = new SnapshotEnvelope<TestSnapshotableAggregate>();

        // Assert
        envelope.Id.Should().BeEmpty();
        envelope.AggregateId.Should().Be(Guid.Empty);
        envelope.Version.Should().Be(0);
        envelope.State.Should().BeNull();
        envelope.CreatedAtUtc.Should().Be(default);
        envelope.AggregateType.Should().BeEmpty();
    }
}

using Encina.Marten.Snapshots;

namespace Encina.Marten.Tests.Snapshots;

public sealed class SnapshotTests
{
    [Fact]
    public void Snapshot_Constructor_SetsAllProperties()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var version = 10;
        var aggregate = new TestSnapshotableAggregate();
        var createdAt = DateTime.UtcNow;

        // Act
        var snapshot = new Snapshot<TestSnapshotableAggregate>(
            aggregateId,
            version,
            aggregate,
            createdAt);

        // Assert
        snapshot.AggregateId.ShouldBe(aggregateId);
        snapshot.Version.ShouldBe(version);
        snapshot.State.ShouldBe(aggregate);
        snapshot.CreatedAtUtc.ShouldBe(createdAt);
    }

    [Fact]
    public void Snapshot_WithNullState_ThrowsArgumentNullException()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();

        // Act
        var act = () => new Snapshot<TestSnapshotableAggregate>(
            aggregateId,
            10,
            null!,
            DateTime.UtcNow);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("state");
    }

    [Fact]
    public void Snapshot_ImplementsISnapshot_AggregateId()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var aggregate = new TestSnapshotableAggregate();
        var snapshot = new Snapshot<TestSnapshotableAggregate>(
            aggregateId,
            5,
            aggregate,
            DateTime.UtcNow);

        // Act & Assert
        snapshot.AggregateId.ShouldBe(aggregateId);
    }

    [Fact]
    public void Snapshot_ImplementsISnapshot_Version()
    {
        // Arrange
        var aggregate = new TestSnapshotableAggregate();
        var snapshot = new Snapshot<TestSnapshotableAggregate>(
            Guid.NewGuid(),
            42,
            aggregate,
            DateTime.UtcNow);

        // Act & Assert
        snapshot.Version.ShouldBe(42);
    }

    [Fact]
    public void Snapshot_ImplementsISnapshot_CreatedAtUtc()
    {
        // Arrange
        var createdAt = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var aggregate = new TestSnapshotableAggregate();
        var snapshot = new Snapshot<TestSnapshotableAggregate>(
            Guid.NewGuid(),
            1,
            aggregate,
            createdAt);

        // Act & Assert
        snapshot.CreatedAtUtc.ShouldBe(createdAt);
    }

    [Fact]
    public void Snapshot_PreservesAggregateState()
    {
        // Arrange
        var aggregate = new TestSnapshotableAggregate(Guid.NewGuid(), "Test Order");
        aggregate.AddItem(100m);
        aggregate.AddItem(200m);

        // Act
        var snapshot = new Snapshot<TestSnapshotableAggregate>(
            aggregate.Id,
            aggregate.Version,
            aggregate,
            DateTime.UtcNow);

        // Assert
        snapshot.State.Name.ShouldBe("Test Order");
        snapshot.State.Total.ShouldBe(300m);
        snapshot.State.ItemCount.ShouldBe(2);
    }

    [Fact]
    public void Snapshot_CanBeAssignedToISnapshot()
    {
        // Arrange
        var aggregate = new TestSnapshotableAggregate();
        var snapshot = new Snapshot<TestSnapshotableAggregate>(
            Guid.NewGuid(),
            5,
            aggregate,
            DateTime.UtcNow);

        // Act - Verifies the type implements the interface correctly
        // Use method that takes interface to prove compatibility
        static int GetVersion(ISnapshot<TestSnapshotableAggregate> s) => s.Version;

        // Assert
        snapshot.ShouldNotBeNull();
        GetVersion(snapshot).ShouldBe(5);
    }
}

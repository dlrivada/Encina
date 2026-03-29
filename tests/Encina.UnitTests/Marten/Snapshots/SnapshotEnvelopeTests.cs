using Encina.DomainModeling;
using Encina.Marten.Snapshots;
using Shouldly;

namespace Encina.UnitTests.Marten.Snapshots;

public class SnapshotEnvelopeTests
{
    // Snapshot<TAggregate> tests

    [Fact]
    public void Snapshot_Constructor_NullState_ThrowsArgumentNullException()
    {
        var act = () => new Snapshot<TestAggregate>(Guid.NewGuid(), 1, null!, DateTime.UtcNow);
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Snapshot_Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var aggregate = new TestAggregate { Id = id };
        var now = DateTime.UtcNow;

        // Act
        var snapshot = new Snapshot<TestAggregate>(id, 10, aggregate, now);

        // Assert
        snapshot.AggregateId.ShouldBe(id);
        snapshot.Version.ShouldBe(10);
        snapshot.State.ShouldBe(aggregate);
        snapshot.CreatedAtUtc.ShouldBe(now);
    }

    // SnapshotEnvelope factory tests

    [Fact]
    public void Create_NullSnapshot_ThrowsArgumentNullException()
    {
        var act = () => SnapshotEnvelope.Create<TestAggregate>(null!);
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Create_ValidSnapshot_CreatesEnvelopeWithCorrectProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var aggregate = new TestAggregate { Id = id, Version = 10 };
        var now = DateTime.UtcNow;
        var snapshot = new Snapshot<TestAggregate>(id, 10, aggregate, now);

        // Act
        var envelope = SnapshotEnvelope.Create(snapshot);

        // Assert
        envelope.AggregateId.ShouldBe(id);
        envelope.Version.ShouldBe(10);
        envelope.State.ShouldBe(aggregate);
        envelope.CreatedAtUtc.ShouldBe(now);
        envelope.AggregateType.ShouldContain("TestAggregate");
        envelope.Id.ShouldContain(id.ToString());
        envelope.Id.ShouldContain("10");
    }

    // SnapshotEnvelope<TAggregate>.ToSnapshot tests

    [Fact]
    public void ToSnapshot_NullState_ThrowsInvalidOperationException()
    {
        // Arrange
        var envelope = new SnapshotEnvelope<TestAggregate>
        {
            Id = "test:id:1",
            AggregateId = Guid.NewGuid(),
            Version = 1,
            State = null,
            CreatedAtUtc = DateTime.UtcNow,
            AggregateType = "TestAggregate"
        };

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => envelope.ToSnapshot());
    }

    [Fact]
    public void ToSnapshot_ValidState_ReturnsSnapshot()
    {
        // Arrange
        var id = Guid.NewGuid();
        var aggregate = new TestAggregate { Id = id };
        var now = DateTime.UtcNow;
        var envelope = new SnapshotEnvelope<TestAggregate>
        {
            Id = $"TestAggregate:{id}:5",
            AggregateId = id,
            Version = 5,
            State = aggregate,
            CreatedAtUtc = now,
            AggregateType = "TestAggregate"
        };

        // Act
        var snapshot = envelope.ToSnapshot();

        // Assert
        snapshot.AggregateId.ShouldBe(id);
        snapshot.Version.ShouldBe(5);
        snapshot.State.ShouldBe(aggregate);
        snapshot.CreatedAtUtc.ShouldBe(now);
    }

    [Fact]
    public void RoundTrip_CreateAndToSnapshot_PreservesData()
    {
        // Arrange
        var id = Guid.NewGuid();
        var aggregate = new TestAggregate { Id = id, Version = 10, Name = "round-trip" };
        var now = DateTime.UtcNow;
        var original = new Snapshot<TestAggregate>(id, 10, aggregate, now);

        // Act
        var envelope = SnapshotEnvelope.Create(original);
        var restored = envelope.ToSnapshot();

        // Assert
        restored.AggregateId.ShouldBe(original.AggregateId);
        restored.Version.ShouldBe(original.Version);
        restored.CreatedAtUtc.ShouldBe(original.CreatedAtUtc);
        restored.State.Name.ShouldBe("round-trip");
    }

    // SnapshotEnvelope<TAggregate> property tests

    [Fact]
    public void SnapshotEnvelope_DefaultProperties_AreEmptyOrDefault()
    {
        // Arrange & Act
        var envelope = new SnapshotEnvelope<TestAggregate>();

        // Assert
        envelope.Id.ShouldBe(string.Empty);
        envelope.AggregateId.ShouldBe(Guid.Empty);
        envelope.Version.ShouldBe(0);
        envelope.State.ShouldBeNull();
        envelope.CreatedAtUtc.ShouldBe(default(DateTime));
        envelope.AggregateType.ShouldBe(string.Empty);
    }

    // Test types

    public sealed class TestAggregate : AggregateBase, ISnapshotable<TestAggregate>
    {
        public string Name { get; set; } = string.Empty;

        public new Guid Id
        {
            get => base.Id;
            set => base.Id = value;
        }

        protected override void Apply(object domainEvent) { }
    }
}

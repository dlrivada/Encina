using System.Globalization;
using Encina.Marten.Snapshots;

namespace Encina.Marten.Tests.Snapshots;

/// <summary>
/// Property-based tests for snapshot functionality.
/// Uses Theory with inline data to test various scenarios.
/// </summary>
public sealed class SnapshotPropertyTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(int.MaxValue)]
    public void Snapshot_Version_IsAlwaysNonNegative(int version)
    {
        // Arrange
        var aggregate = new TestSnapshotableAggregate();

        // Act
        var snapshot = new Snapshot<TestSnapshotableAggregate>(
            Guid.NewGuid(),
            version,
            aggregate,
            DateTime.UtcNow);

        // Assert
        snapshot.Version.ShouldBeGreaterThanOrEqualTo(0);
        snapshot.Version.ShouldBe(version);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(999)]
    public void SnapshotEnvelope_RoundTrip_PreservesVersion(int version)
    {
        // Arrange
        var aggregate = new TestSnapshotableAggregate();
        var original = new Snapshot<TestSnapshotableAggregate>(
            Guid.NewGuid(),
            version,
            aggregate,
            DateTime.UtcNow);

        // Act
        var envelope = SnapshotEnvelope.Create(original);
        var restored = envelope.ToSnapshot();

        // Assert
        restored.Version.ShouldBe(original.Version);
    }

    public static IEnumerable<object[]> AggregateIdTestCases()
    {
        yield return [Guid.Empty];
        yield return [Guid.Parse("12345678-1234-1234-1234-123456789012")];
        yield return [Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")];
    }

    [Theory]
    [MemberData(nameof(AggregateIdTestCases))]
    public void SnapshotEnvelope_RoundTrip_PreservesAggregateId(Guid aggregateId)
    {
        // Arrange
        var aggregate = new TestSnapshotableAggregate();
        var original = new Snapshot<TestSnapshotableAggregate>(
            aggregateId,
            10,
            aggregate,
            DateTime.UtcNow);

        // Act
        var envelope = SnapshotEnvelope.Create(original);
        var restored = envelope.ToSnapshot();

        // Assert
        restored.AggregateId.ShouldBe(original.AggregateId);
    }

    [Theory]
    [MemberData(nameof(AggregateIdTestCases))]
    public void SnapshotEnvelope_Id_ContainsAggregateId(Guid aggregateId)
    {
        // Arrange
        var aggregate = new TestSnapshotableAggregate();
        var snapshot = new Snapshot<TestSnapshotableAggregate>(
            aggregateId,
            10,
            aggregate,
            DateTime.UtcNow);

        // Act
        var envelope = SnapshotEnvelope.Create(snapshot);

        // Assert
        envelope.Id.ShouldContain(aggregateId.ToString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(100)]
    [InlineData(999)]
    public void SnapshotEnvelope_Id_ContainsVersion(int version)
    {
        // Arrange
        var aggregate = new TestSnapshotableAggregate();
        var snapshot = new Snapshot<TestSnapshotableAggregate>(
            Guid.NewGuid(),
            version,
            aggregate,
            DateTime.UtcNow);

        // Act
        var envelope = SnapshotEnvelope.Create(snapshot);

        // Assert
        envelope.Id.ShouldContain(version.ToString(CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(500)]
    public void SnapshotOptions_SnapshotEvery_AcceptsPositiveValues(int positiveValue)
    {
        // Arrange & Act
        var options = new SnapshotOptions { SnapshotEvery = positiveValue };

        // Assert
        options.SnapshotEvery.ShouldBe(positiveValue);
        options.SnapshotEvery.ShouldBeGreaterThan(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    public void SnapshotOptions_KeepSnapshots_AcceptsNonNegativeValues(int nonNegativeValue)
    {
        // Arrange & Act
        var options = new SnapshotOptions { KeepSnapshots = nonNegativeValue };

        // Assert
        options.KeepSnapshots.ShouldBe(nonNegativeValue);
        options.KeepSnapshots.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Theory]
    [InlineData(25, 5)]
    [InlineData(50, 10)]
    [InlineData(100, 3)]
    [InlineData(200, 1)]
    public void SnapshotOptions_ConfigureAggregate_AcceptsValidParameters(
        int snapshotEvery,
        int keepSnapshots)
    {
        // Arrange
        var options = new SnapshotOptions();

        // Act - Should not throw
        var result = options.ConfigureAggregate<TestSnapshotableAggregate>(
            snapshotEvery: snapshotEvery,
            keepSnapshots: keepSnapshots);

        // Assert
        result.ShouldBeSameAs(options);
    }

    [Fact]
    public void SnapshotEnvelope_DifferentAggregateIds_HaveDifferentIds()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var aggregate = new TestSnapshotableAggregate();

        var snapshot1 = new Snapshot<TestSnapshotableAggregate>(id1, 1, aggregate, DateTime.UtcNow);
        var snapshot2 = new Snapshot<TestSnapshotableAggregate>(id2, 1, aggregate, DateTime.UtcNow);

        // Act
        var envelope1 = SnapshotEnvelope.Create(snapshot1);
        var envelope2 = SnapshotEnvelope.Create(snapshot2);

        // Assert
        envelope1.Id.ShouldNotBe(envelope2.Id);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(5, 10)]
    [InlineData(100, 200)]
    public void SnapshotEnvelope_DifferentVersions_HaveDifferentIds(int v1, int v2)
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var aggregate = new TestSnapshotableAggregate();

        var snapshot1 = new Snapshot<TestSnapshotableAggregate>(aggregateId, v1, aggregate, DateTime.UtcNow);
        var snapshot2 = new Snapshot<TestSnapshotableAggregate>(aggregateId, v2, aggregate, DateTime.UtcNow);

        // Act
        var envelope1 = SnapshotEnvelope.Create(snapshot1);
        var envelope2 = SnapshotEnvelope.Create(snapshot2);

        // Assert
        envelope1.Id.ShouldNotBe(envelope2.Id);
    }

    [Fact]
    public void SnapshotEnvelope_SameAggregateIdAndVersion_HaveSameIds()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var aggregate1 = new TestSnapshotableAggregate();
        var aggregate2 = new TestSnapshotableAggregate();

        var snapshot1 = new Snapshot<TestSnapshotableAggregate>(aggregateId, 10, aggregate1, DateTime.UtcNow);
        var snapshot2 = new Snapshot<TestSnapshotableAggregate>(aggregateId, 10, aggregate2, DateTime.UtcNow);

        // Act
        var envelope1 = SnapshotEnvelope.Create(snapshot1);
        var envelope2 = SnapshotEnvelope.Create(snapshot2);

        // Assert
        envelope1.Id.ShouldBe(envelope2.Id);
    }
}

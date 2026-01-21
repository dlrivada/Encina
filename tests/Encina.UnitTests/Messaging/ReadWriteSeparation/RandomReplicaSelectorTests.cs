using Encina.Messaging.ReadWriteSeparation;
using Shouldly;

namespace Encina.UnitTests.Messaging.ReadWriteSeparation;

/// <summary>
/// Unit tests for <see cref="RandomReplicaSelector"/>.
/// </summary>
public sealed class RandomReplicaSelectorTests
{
    private static readonly string[] TestReplicas =
    [
        "Server=replica1;Database=test;",
        "Server=replica2;Database=test;",
        "Server=replica3;Database=test;"
    ];

    [Fact]
    public void Constructor_WithNullReplicas_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new RandomReplicaSelector(null!));
    }

    [Fact]
    public void Constructor_WithEmptyReplicas_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => new RandomReplicaSelector([]));
        exception.ParamName.ShouldBe("replicas");
    }

    [Fact]
    public void SelectReplica_WithSingleReplica_AlwaysReturnsSameReplica()
    {
        // Arrange
        var selector = new RandomReplicaSelector(["Server=single;"]);

        // Act & Assert
        for (var i = 0; i < 10; i++)
        {
            selector.SelectReplica().ShouldBe("Server=single;");
        }
    }

    [Fact]
    public void SelectReplica_ReturnsValidReplica()
    {
        // Arrange
        var selector = new RandomReplicaSelector(TestReplicas);

        // Act & Assert
        for (var i = 0; i < 100; i++)
        {
            selector.SelectReplica().ShouldBeOneOf(TestReplicas);
        }
    }

    [Fact]
    public void SelectReplica_UsesAllReplicasOverManySelections()
    {
        // Arrange
        var selector = new RandomReplicaSelector(TestReplicas);
        var selectedReplicas = new System.Collections.Generic.HashSet<string>();

        // Act - Select many times to statistically ensure all replicas are used
        for (var i = 0; i < 1000; i++)
        {
            selectedReplicas.Add(selector.SelectReplica());
        }

        // Assert - All replicas should have been selected at least once
        selectedReplicas.Count.ShouldBe(3);
        selectedReplicas.ShouldContain(TestReplicas[0]);
        selectedReplicas.ShouldContain(TestReplicas[1]);
        selectedReplicas.ShouldContain(TestReplicas[2]);
    }

    [Fact]
    public void SelectReplica_DistributesApproximatelyEvenly()
    {
        // Arrange
        var selector = new RandomReplicaSelector(TestReplicas);
        var counts = new Dictionary<string, int>
        {
            [TestReplicas[0]] = 0,
            [TestReplicas[1]] = 0,
            [TestReplicas[2]] = 0
        };

        // Act
        for (var i = 0; i < 3000; i++)
        {
            var selected = selector.SelectReplica();
            counts[selected]++;
        }

        // Assert - Distribution should be roughly even (within 20% of expected)
        var expected = 1000;
        foreach (var count in counts.Values)
        {
            count.ShouldBeGreaterThan(expected - 200); // At least 80% of expected
            count.ShouldBeLessThan(expected + 200);    // At most 120% of expected
        }
    }

    [Fact]
    public void SelectReplica_IsThreadSafe()
    {
        // Arrange
        var selector = new RandomReplicaSelector(TestReplicas);
        var results = new System.Collections.Concurrent.ConcurrentBag<string>();

        // Act
        Parallel.For(0, 1000, _ =>
        {
            results.Add(selector.SelectReplica());
        });

        // Assert - Should have selected from all replicas without exceptions
        var grouped = results.GroupBy(r => r).ToDictionary(g => g.Key, g => g.Count());
        grouped.Keys.Count.ShouldBe(3);
        results.All(r => TestReplicas.Contains(r)).ShouldBeTrue();
    }

    [Fact]
    public void SelectReplica_ImplementsIReplicaSelector()
    {
        // Arrange
        var selector = new RandomReplicaSelector(TestReplicas);

        // Act & Assert - Verify it implements interface
        (selector is IReplicaSelector).ShouldBeTrue();
        selector.SelectReplica().ShouldBeOneOf(TestReplicas);
    }
}

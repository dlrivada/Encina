using Encina.Messaging.ReadWriteSeparation;
using Shouldly;

namespace Encina.UnitTests.Messaging.ReadWriteSeparation;

/// <summary>
/// Unit tests for <see cref="RoundRobinReplicaSelector"/>.
/// </summary>
public sealed class RoundRobinReplicaSelectorTests
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
        Should.Throw<ArgumentNullException>(() => new RoundRobinReplicaSelector(null!));
    }

    [Fact]
    public void Constructor_WithEmptyReplicas_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => new RoundRobinReplicaSelector([]));
        exception.ParamName.ShouldBe("replicas");
    }

    [Fact]
    public void SelectReplica_WithSingleReplica_AlwaysReturnsSameReplica()
    {
        // Arrange
        var selector = new RoundRobinReplicaSelector(["Server=single;"]);

        // Act & Assert
        for (var i = 0; i < 10; i++)
        {
            selector.SelectReplica().ShouldBe("Server=single;");
        }
    }

    [Fact]
    public void SelectReplica_CyclesThroughReplicasInOrder()
    {
        // Arrange
        var selector = new RoundRobinReplicaSelector(TestReplicas);

        // Act & Assert
        selector.SelectReplica().ShouldBe(TestReplicas[0]);
        selector.SelectReplica().ShouldBe(TestReplicas[1]);
        selector.SelectReplica().ShouldBe(TestReplicas[2]);
        selector.SelectReplica().ShouldBe(TestReplicas[0]); // Wraps around
        selector.SelectReplica().ShouldBe(TestReplicas[1]);
        selector.SelectReplica().ShouldBe(TestReplicas[2]);
    }

    [Fact]
    public void SelectReplica_DistributesEvenly()
    {
        // Arrange
        var selector = new RoundRobinReplicaSelector(TestReplicas);
        var counts = new Dictionary<string, int>
        {
            [TestReplicas[0]] = 0,
            [TestReplicas[1]] = 0,
            [TestReplicas[2]] = 0
        };

        // Act
        for (var i = 0; i < 300; i++)
        {
            var selected = selector.SelectReplica();
            counts[selected]++;
        }

        // Assert - Each replica should be selected exactly 100 times
        counts[TestReplicas[0]].ShouldBe(100);
        counts[TestReplicas[1]].ShouldBe(100);
        counts[TestReplicas[2]].ShouldBe(100);
    }

    [Fact]
    public void SelectReplica_IsThreadSafe()
    {
        // Arrange
        var selector = new RoundRobinReplicaSelector(TestReplicas);
        var results = new System.Collections.Concurrent.ConcurrentBag<string>();

        // Act
        Parallel.For(0, 1000, _ =>
        {
            results.Add(selector.SelectReplica());
        });

        // Assert - Should have selected from all replicas
        var grouped = results.GroupBy(r => r).ToDictionary(g => g.Key, g => g.Count());
        grouped.Keys.Count.ShouldBe(3);

        // Distribution should be roughly equal (allowing for thread scheduling variance)
        foreach (var count in grouped.Values)
        {
            count.ShouldBeGreaterThan(250); // At least 25% each
            count.ShouldBeLessThan(450);    // At most 45% each
        }
    }

    [Fact]
    public void SelectReplica_ImplementsIReplicaSelector()
    {
        // Arrange
        var selector = new RoundRobinReplicaSelector(TestReplicas);

        // Act & Assert - Verify it implements interface
        (selector is IReplicaSelector).ShouldBeTrue();
        selector.SelectReplica().ShouldBeOneOf(TestReplicas);
    }
}

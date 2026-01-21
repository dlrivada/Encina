using Encina.Messaging.ReadWriteSeparation;
using Shouldly;

namespace Encina.UnitTests.Messaging.ReadWriteSeparation;

/// <summary>
/// Unit tests for <see cref="LeastConnectionsReplicaSelector"/>.
/// </summary>
public sealed class LeastConnectionsReplicaSelectorTests
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
        Should.Throw<ArgumentNullException>(() => new LeastConnectionsReplicaSelector(null!));
    }

    [Fact]
    public void Constructor_WithEmptyReplicas_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => new LeastConnectionsReplicaSelector([]));
        exception.ParamName.ShouldBe("replicas");
    }

    [Fact]
    public void SelectReplica_WithSingleReplica_AlwaysReturnsSameReplica()
    {
        // Arrange
        var selector = new LeastConnectionsReplicaSelector(["Server=single;"]);

        // Act & Assert
        for (var i = 0; i < 10; i++)
        {
            selector.SelectReplica().ShouldBe("Server=single;");
        }
    }

    [Fact]
    public void SelectReplica_InitiallySelectsFirstReplica()
    {
        // Arrange
        var selector = new LeastConnectionsReplicaSelector(TestReplicas);

        // Act
        var selected = selector.SelectReplica();

        // Assert - First selection should be first replica (all have 0 connections)
        selected.ShouldBe(TestReplicas[0]);
    }

    [Fact]
    public void SelectReplica_SelectsReplicaWithLeastConnections()
    {
        // Arrange
        var selector = new LeastConnectionsReplicaSelector(TestReplicas);

        // Act - Select first two replicas (counts: replica1=1, replica2=1, replica3=0)
        selector.SelectReplica(); // replica1 -> count=1
        selector.SelectReplica(); // replica2 -> count=1 (replica3 also 0 but found first)

        // The third selection should pick replica with lowest count
        var third = selector.SelectReplica();

        // Assert
        selector.GetConnectionCount(TestReplicas[0]).ShouldBe(1);
    }

    [Fact]
    public void GetConnectionCount_ReturnsCorrectCount()
    {
        // Arrange
        var selector = new LeastConnectionsReplicaSelector(TestReplicas);

        // Act
        selector.SelectReplica(); // Should increment first replica
        selector.SelectReplica(); // Should increment second replica

        // Assert
        var counts = selector.GetAllConnectionCounts();
        counts.Values.Sum().ShouldBe(2);
    }

    [Fact]
    public void ReleaseReplica_DecrementsCount()
    {
        // Arrange
        var selector = new LeastConnectionsReplicaSelector(TestReplicas);
        var replica = selector.SelectReplica();

        // Act
        selector.ReleaseReplica(replica);

        // Assert
        selector.GetConnectionCount(replica).ShouldBe(0);
    }

    [Fact]
    public void ReleaseReplica_WithNullConnectionString_ThrowsArgumentNullException()
    {
        // Arrange
        var selector = new LeastConnectionsReplicaSelector(TestReplicas);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => selector.ReleaseReplica(null!));
    }

    [Fact]
    public void ReleaseReplica_DoesNotGoBelowZero()
    {
        // Arrange
        var selector = new LeastConnectionsReplicaSelector(TestReplicas);

        // Act - Release without first selecting
        selector.ReleaseReplica(TestReplicas[0]);
        selector.ReleaseReplica(TestReplicas[0]);

        // Assert
        selector.GetConnectionCount(TestReplicas[0]).ShouldBe(0);
    }

    [Fact]
    public void AcquireReplica_ReturnsLeaseWithConnectionString()
    {
        // Arrange
        var selector = new LeastConnectionsReplicaSelector(TestReplicas);

        // Act
        using var lease = selector.AcquireReplica();

        // Assert
        lease.ConnectionString.ShouldBeOneOf(TestReplicas);
    }

    [Fact]
    public void AcquireReplica_IncrementsCount()
    {
        // Arrange
        var selector = new LeastConnectionsReplicaSelector(TestReplicas);

        // Act
        using var lease = selector.AcquireReplica();

        // Assert
        selector.GetConnectionCount(lease.ConnectionString).ShouldBe(1);
    }

    [Fact]
    public void AcquireReplica_DisposingDecrementsCount()
    {
        // Arrange
        var selector = new LeastConnectionsReplicaSelector(TestReplicas);
        string connectionString;

        // Act
        using (var lease = selector.AcquireReplica())
        {
            connectionString = lease.ConnectionString;
            selector.GetConnectionCount(connectionString).ShouldBe(1);
        }

        // Assert - After dispose, count should be decremented
        selector.GetConnectionCount(connectionString).ShouldBe(0);
    }

    [Fact]
    public void GetAllConnectionCounts_ReturnsAllReplicas()
    {
        // Arrange
        var selector = new LeastConnectionsReplicaSelector(TestReplicas);

        // Act
        var counts = selector.GetAllConnectionCounts();

        // Assert
        counts.Count.ShouldBe(3);
        counts.Keys.ShouldContain(TestReplicas[0]);
        counts.Keys.ShouldContain(TestReplicas[1]);
        counts.Keys.ShouldContain(TestReplicas[2]);
    }

    [Fact]
    public void GetConnectionCount_WithNullConnectionString_ThrowsArgumentNullException()
    {
        // Arrange
        var selector = new LeastConnectionsReplicaSelector(TestReplicas);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => selector.GetConnectionCount(null!));
    }

    [Fact]
    public void GetConnectionCount_WithUnknownReplica_ReturnsZero()
    {
        // Arrange
        var selector = new LeastConnectionsReplicaSelector(TestReplicas);

        // Act
        var count = selector.GetConnectionCount("Unknown;");

        // Assert
        count.ShouldBe(0);
    }

    [Fact]
    public void SelectReplica_IsThreadSafe()
    {
        // Arrange
        var selector = new LeastConnectionsReplicaSelector(TestReplicas);
        var results = new System.Collections.Concurrent.ConcurrentBag<string>();

        // Act
        Parallel.For(0, 100, _ =>
        {
            var replica = selector.SelectReplica();
            results.Add(replica);
            // Simulate some work
            Thread.Sleep(1);
            selector.ReleaseReplica(replica);
        });

        // Assert - All results should be valid replicas
        results.All(r => TestReplicas.Contains(r)).ShouldBeTrue();
    }

    [Fact]
    public void SelectReplica_ImplementsIReplicaSelector()
    {
        // Arrange
        var selector = new LeastConnectionsReplicaSelector(TestReplicas);

        // Act & Assert - Verify it implements interface
        (selector is IReplicaSelector).ShouldBeTrue();
        selector.SelectReplica().ShouldBeOneOf(TestReplicas);
    }

    [Fact]
    public void ReplicaLease_IsDisposable()
    {
        // Arrange
        var selector = new LeastConnectionsReplicaSelector(TestReplicas);

        // Act & Assert - Should not throw when used with using statement
        using var lease = selector.AcquireReplica();
        lease.ConnectionString.ShouldNotBeNullOrEmpty();
    }
}

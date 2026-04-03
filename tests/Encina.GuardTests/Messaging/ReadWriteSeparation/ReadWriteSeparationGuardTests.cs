using Encina.Messaging.ReadWriteSeparation;
using FluentAssertions;

namespace Encina.GuardTests.Messaging.ReadWriteSeparation;

/// <summary>
/// Guard clause tests for LeastConnectionsReplicaSelector constructor,
/// SelectReplica, ReleaseReplica, GetConnectionCount, and AcquireReplica.
/// </summary>
public class ReadWriteSeparationGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullReplicas_ThrowsArgumentNullException()
    {
        var act = () => new LeastConnectionsReplicaSelector(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("replicas");
    }

    [Fact]
    public void Constructor_EmptyReplicas_ThrowsArgumentException()
    {
        var act = () => new LeastConnectionsReplicaSelector(new List<string>());
        act.Should().Throw<ArgumentException>().WithParameterName("replicas");
    }

    [Fact]
    public void Constructor_SingleReplica_Succeeds()
    {
        var sut = new LeastConnectionsReplicaSelector(new List<string> { "Server=replica1" });
        sut.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_MultipleReplicas_Succeeds()
    {
        var replicas = new List<string> { "Server=replica1", "Server=replica2", "Server=replica3" };
        var sut = new LeastConnectionsReplicaSelector(replicas);
        sut.Should().NotBeNull();
    }

    #endregion

    #region SelectReplica

    [Fact]
    public void SelectReplica_SingleReplica_ReturnsThatReplica()
    {
        var sut = new LeastConnectionsReplicaSelector(new List<string> { "Server=replica1" });

        var selected = sut.SelectReplica();

        selected.Should().Be("Server=replica1");
    }

    [Fact]
    public void SelectReplica_MultipleReplicas_ReturnsOneWithLeastConnections()
    {
        var replicas = new List<string> { "Server=replica1", "Server=replica2" };
        var sut = new LeastConnectionsReplicaSelector(replicas);

        // First call selects replica1 (both start at 0, first is picked)
        var first = sut.SelectReplica();
        first.Should().Be("Server=replica1");

        // Second call selects replica2 (replica1 now has 1 connection)
        var second = sut.SelectReplica();
        second.Should().Be("Server=replica2");
    }

    [Fact]
    public void SelectReplica_AfterRelease_RebalancesCorrectly()
    {
        var replicas = new List<string> { "Server=replica1", "Server=replica2" };
        var sut = new LeastConnectionsReplicaSelector(replicas);

        // Acquire two connections on replica1
        sut.SelectReplica(); // replica1 -> count 1
        sut.SelectReplica(); // replica2 -> count 1
        sut.SelectReplica(); // replica1 -> count 2

        // Release one from replica1
        sut.ReleaseReplica("Server=replica1");

        // Now replica1=1, replica2=1. First encountered (replica1) should be selected.
        var next = sut.SelectReplica();
        next.Should().Be("Server=replica1");
    }

    #endregion

    #region ReleaseReplica Guards

    [Fact]
    public void ReleaseReplica_NullConnectionString_ThrowsArgumentNullException()
    {
        var sut = new LeastConnectionsReplicaSelector(new List<string> { "Server=replica1" });
        var act = () => sut.ReleaseReplica(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("connectionString");
    }

    [Fact]
    public void ReleaseReplica_UnknownReplica_DoesNotThrow()
    {
        var sut = new LeastConnectionsReplicaSelector(new List<string> { "Server=replica1" });

        // Releasing an unknown replica should not throw
        var act = () => sut.ReleaseReplica("Server=unknown");
        act.Should().NotThrow();
    }

    [Fact]
    public void ReleaseReplica_DoubleRelease_DoesNotGoBelowZero()
    {
        var sut = new LeastConnectionsReplicaSelector(new List<string> { "Server=replica1" });

        sut.SelectReplica(); // count = 1
        sut.ReleaseReplica("Server=replica1"); // count = 0
        sut.ReleaseReplica("Server=replica1"); // count should stay at 0

        sut.GetConnectionCount("Server=replica1").Should().Be(0);
    }

    #endregion

    #region GetConnectionCount Guards

    [Fact]
    public void GetConnectionCount_NullConnectionString_ThrowsArgumentNullException()
    {
        var sut = new LeastConnectionsReplicaSelector(new List<string> { "Server=replica1" });
        var act = () => sut.GetConnectionCount(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("connectionString");
    }

    [Fact]
    public void GetConnectionCount_UnknownReplica_ReturnsZero()
    {
        var sut = new LeastConnectionsReplicaSelector(new List<string> { "Server=replica1" });
        var count = sut.GetConnectionCount("Server=unknown");
        count.Should().Be(0);
    }

    [Fact]
    public void GetConnectionCount_AfterSelect_ReturnsCorrectCount()
    {
        var sut = new LeastConnectionsReplicaSelector(new List<string> { "Server=replica1" });

        sut.SelectReplica();
        sut.SelectReplica();

        sut.GetConnectionCount("Server=replica1").Should().Be(2);
    }

    #endregion

    #region AcquireReplica and ReplicaLease

    [Fact]
    public void AcquireReplica_ReturnsLease()
    {
        var sut = new LeastConnectionsReplicaSelector(new List<string> { "Server=replica1" });

        using var lease = sut.AcquireReplica();

        lease.ConnectionString.Should().Be("Server=replica1");
        sut.GetConnectionCount("Server=replica1").Should().Be(1);
    }

    [Fact]
    public void AcquireReplica_DisposingLease_DecrementsCount()
    {
        var sut = new LeastConnectionsReplicaSelector(new List<string> { "Server=replica1" });

        var lease = sut.AcquireReplica();
        sut.GetConnectionCount("Server=replica1").Should().Be(1);

        lease.Dispose();
        sut.GetConnectionCount("Server=replica1").Should().Be(0);
    }

    [Fact]
    public void AcquireReplica_MultipleLeases_TracksCorrectly()
    {
        var replicas = new List<string> { "Server=replica1", "Server=replica2" };
        var sut = new LeastConnectionsReplicaSelector(replicas);

        using var lease1 = sut.AcquireReplica(); // replica1
        using var lease2 = sut.AcquireReplica(); // replica2

        lease1.ConnectionString.Should().Be("Server=replica1");
        lease2.ConnectionString.Should().Be("Server=replica2");
        sut.GetConnectionCount("Server=replica1").Should().Be(1);
        sut.GetConnectionCount("Server=replica2").Should().Be(1);
    }

    #endregion

    #region GetAllConnectionCounts

    [Fact]
    public void GetAllConnectionCounts_InitialState_AllZero()
    {
        var replicas = new List<string> { "Server=replica1", "Server=replica2" };
        var sut = new LeastConnectionsReplicaSelector(replicas);

        var counts = sut.GetAllConnectionCounts();

        counts.Should().HaveCount(2);
        counts["Server=replica1"].Should().Be(0);
        counts["Server=replica2"].Should().Be(0);
    }

    [Fact]
    public void GetAllConnectionCounts_AfterSelections_ReflectsState()
    {
        var replicas = new List<string> { "Server=replica1", "Server=replica2" };
        var sut = new LeastConnectionsReplicaSelector(replicas);

        sut.SelectReplica(); // replica1 -> 1
        sut.SelectReplica(); // replica2 -> 1
        sut.SelectReplica(); // replica1 -> 2

        var counts = sut.GetAllConnectionCounts();

        counts["Server=replica1"].Should().Be(2);
        counts["Server=replica2"].Should().Be(1);
    }

    #endregion

    #region Thread Safety (Basic Verification)

    [Fact]
    public void SelectReplica_ConcurrentCalls_DoNotCorrupt()
    {
        var replicas = new List<string> { "Server=replica1", "Server=replica2", "Server=replica3" };
        var sut = new LeastConnectionsReplicaSelector(replicas);

        // Run multiple selects in parallel
        Parallel.For(0, 100, _ => sut.SelectReplica());

        var counts = sut.GetAllConnectionCounts();
        var total = counts.Values.Sum();
        total.Should().Be(100);
    }

    #endregion
}

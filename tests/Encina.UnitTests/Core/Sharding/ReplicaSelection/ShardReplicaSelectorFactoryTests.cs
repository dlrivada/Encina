using Encina.Sharding.ReplicaSelection;

namespace Encina.UnitTests.Core.Sharding.ReplicaSelection;

public sealed class ShardReplicaSelectorFactoryTests
{
    // ────────────────────────────────────────────────────────────
    //  Create — Valid Strategies
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Create_RoundRobin_ReturnsRoundRobinSelector()
    {
        var selector = ShardReplicaSelectorFactory.Create(ReplicaSelectionStrategy.RoundRobin);
        selector.ShouldBeOfType<RoundRobinShardReplicaSelector>();
    }

    [Fact]
    public void Create_Random_ReturnsRandomSelector()
    {
        var selector = ShardReplicaSelectorFactory.Create(ReplicaSelectionStrategy.Random);
        selector.ShouldBeOfType<RandomShardReplicaSelector>();
    }

    [Fact]
    public void Create_LeastLatency_ReturnsLeastLatencySelector()
    {
        var selector = ShardReplicaSelectorFactory.Create(ReplicaSelectionStrategy.LeastLatency);
        selector.ShouldBeOfType<LeastLatencyShardReplicaSelector>();
    }

    [Fact]
    public void Create_LeastConnections_ReturnsLeastConnectionsSelector()
    {
        var selector = ShardReplicaSelectorFactory.Create(ReplicaSelectionStrategy.LeastConnections);
        selector.ShouldBeOfType<LeastConnectionsShardReplicaSelector>();
    }

    [Fact]
    public void Create_WeightedRandom_ReturnsWeightedRandomSelector()
    {
        var selector = ShardReplicaSelectorFactory.Create(ReplicaSelectionStrategy.WeightedRandom);
        selector.ShouldBeOfType<WeightedRandomShardReplicaSelector>();
    }

    [Fact]
    public void Create_WeightedRandom_WithWeights_PassesWeightsToSelector()
    {
        // Arrange
        int[] weights = [5, 3, 1];

        // Act
        var selector = ShardReplicaSelectorFactory.Create(ReplicaSelectionStrategy.WeightedRandom, weights);

        // Assert — verify it's the right type and works
        selector.ShouldBeOfType<WeightedRandomShardReplicaSelector>();
        IReadOnlyList<string> replicas = ["a", "b", "c"];
        var result = selector.SelectReplica(replicas);
        replicas.ShouldContain(result);
    }

    // ────────────────────────────────────────────────────────────
    //  Create — All Strategies Implement IShardReplicaSelector
    // ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ReplicaSelectionStrategy.RoundRobin)]
    [InlineData(ReplicaSelectionStrategy.Random)]
    [InlineData(ReplicaSelectionStrategy.LeastLatency)]
    [InlineData(ReplicaSelectionStrategy.LeastConnections)]
    [InlineData(ReplicaSelectionStrategy.WeightedRandom)]
    public void Create_AllStrategies_ReturnIShardReplicaSelector(ReplicaSelectionStrategy strategy)
    {
        var selector = ShardReplicaSelectorFactory.Create(strategy);
        selector.ShouldBeAssignableTo<IShardReplicaSelector>();
    }

    [Theory]
    [InlineData(ReplicaSelectionStrategy.RoundRobin)]
    [InlineData(ReplicaSelectionStrategy.Random)]
    [InlineData(ReplicaSelectionStrategy.LeastLatency)]
    [InlineData(ReplicaSelectionStrategy.LeastConnections)]
    [InlineData(ReplicaSelectionStrategy.WeightedRandom)]
    public void Create_AllStrategies_CanSelectReplica(ReplicaSelectionStrategy strategy)
    {
        // Arrange
        var selector = ShardReplicaSelectorFactory.Create(strategy);
        IReadOnlyList<string> replicas = ["Server=replica1;", "Server=replica2;"];

        // Act
        var selected = selector.SelectReplica(replicas);

        // Assert
        replicas.ShouldContain(selected);
    }

    // ────────────────────────────────────────────────────────────
    //  Create — Invalid Strategy
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Create_InvalidStrategy_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            ShardReplicaSelectorFactory.Create((ReplicaSelectionStrategy)999));
    }

    // ────────────────────────────────────────────────────────────
    //  Create — Each Call Returns New Instance
    // ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ReplicaSelectionStrategy.RoundRobin)]
    [InlineData(ReplicaSelectionStrategy.Random)]
    [InlineData(ReplicaSelectionStrategy.LeastLatency)]
    [InlineData(ReplicaSelectionStrategy.LeastConnections)]
    [InlineData(ReplicaSelectionStrategy.WeightedRandom)]
    public void Create_MultipleCalls_ReturnsNewInstances(ReplicaSelectionStrategy strategy)
    {
        var selector1 = ShardReplicaSelectorFactory.Create(strategy);
        var selector2 = ShardReplicaSelectorFactory.Create(strategy);

        selector1.ShouldNotBeSameAs(selector2);
    }
}

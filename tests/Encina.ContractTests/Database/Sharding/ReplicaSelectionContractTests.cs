using Encina.Sharding.ReplicaSelection;
using Shouldly;

namespace Encina.ContractTests.Database.Sharding;

/// <summary>
/// Contract tests verifying that all <see cref="IShardReplicaSelector"/> implementations
/// follow the expected behavioral contract.
/// </summary>
[Trait("Category", "Contract")]
public sealed class ReplicaSelectionContractTests
{
    private static readonly IReadOnlyList<string> TwoReplicas = ["Server=replica1;", "Server=replica2;"];
    private static readonly IReadOnlyList<string> ThreeReplicas = ["Server=r1;", "Server=r2;", "Server=r3;"];

    private static IEnumerable<IShardReplicaSelector> AllSelectors()
    {
        yield return new RoundRobinShardReplicaSelector();
        yield return new RandomShardReplicaSelector();
        yield return new LeastLatencyShardReplicaSelector();
        yield return new LeastConnectionsShardReplicaSelector();
        yield return new WeightedRandomShardReplicaSelector();
    }

    // ────────────────────────────────────────────────────────────
    //  All selectors return a valid replica from the list
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Contract_AllSelectors_ReturnReplicaFromProvidedList()
    {
        foreach (var selector in AllSelectors())
        {
            var result = selector.SelectReplica(ThreeReplicas);
            ThreeReplicas.ShouldContain(result,
                $"{selector.GetType().Name} returned a value not in the replica list");
        }
    }

    // ────────────────────────────────────────────────────────────
    //  All selectors handle single replica
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Contract_AllSelectors_SingleReplica_ReturnsThatReplica()
    {
        IReadOnlyList<string> singleReplica = ["Server=only;"];

        foreach (var selector in AllSelectors())
        {
            var result = selector.SelectReplica(singleReplica);
            result.ShouldBe("Server=only;",
                $"{selector.GetType().Name} should return the only available replica");
        }
    }

    // ────────────────────────────────────────────────────────────
    //  All selectors throw on null input
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Contract_AllSelectors_NullInput_ThrowsArgumentNullException()
    {
        foreach (var selector in AllSelectors())
        {
            Should.Throw<ArgumentNullException>(
                () => selector.SelectReplica(null!),
                $"{selector.GetType().Name} should throw ArgumentNullException for null input");
        }
    }

    // ────────────────────────────────────────────────────────────
    //  All selectors throw on empty input
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Contract_AllSelectors_EmptyInput_ThrowsArgumentException()
    {
        foreach (var selector in AllSelectors())
        {
            Should.Throw<ArgumentException>(
                () => selector.SelectReplica([]),
                $"{selector.GetType().Name} should throw ArgumentException for empty input");
        }
    }

    // ────────────────────────────────────────────────────────────
    //  All selectors are thread-safe (no exceptions under concurrency)
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Contract_AllSelectors_ConcurrentAccess_NoExceptions()
    {
        foreach (var selector in AllSelectors())
        {
            var tasks = Enumerable.Range(0, 100)
                .Select(_ => Task.Run(() => selector.SelectReplica(TwoReplicas)))
                .ToList();

            var results = await Task.WhenAll(tasks);

            foreach (var result in results)
            {
                TwoReplicas.ShouldContain(result,
                    $"{selector.GetType().Name} returned invalid result under concurrency");
            }
        }
    }

    // ────────────────────────────────────────────────────────────
    //  Factory creates all expected types
    // ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ReplicaSelectionStrategy.RoundRobin, typeof(RoundRobinShardReplicaSelector))]
    [InlineData(ReplicaSelectionStrategy.Random, typeof(RandomShardReplicaSelector))]
    [InlineData(ReplicaSelectionStrategy.LeastLatency, typeof(LeastLatencyShardReplicaSelector))]
    [InlineData(ReplicaSelectionStrategy.LeastConnections, typeof(LeastConnectionsShardReplicaSelector))]
    [InlineData(ReplicaSelectionStrategy.WeightedRandom, typeof(WeightedRandomShardReplicaSelector))]
    public void Contract_Factory_CreatesExpectedType(ReplicaSelectionStrategy strategy, Type expectedType)
    {
        var selector = ShardReplicaSelectorFactory.Create(strategy);
        selector.GetType().ShouldBe(expectedType);
    }

    // ────────────────────────────────────────────────────────────
    //  All selector types implement IShardReplicaSelector
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Contract_AllSelectorTypes_ImplementInterface()
    {
        var selectorTypes = new[]
        {
            typeof(RoundRobinShardReplicaSelector),
            typeof(RandomShardReplicaSelector),
            typeof(LeastLatencyShardReplicaSelector),
            typeof(LeastConnectionsShardReplicaSelector),
            typeof(WeightedRandomShardReplicaSelector),
        };

        foreach (var type in selectorTypes)
        {
            typeof(IShardReplicaSelector).IsAssignableFrom(type)
                .ShouldBeTrue($"{type.Name} should implement IShardReplicaSelector");
        }
    }

    // ────────────────────────────────────────────────────────────
    //  All selector types are sealed
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Contract_AllSelectorTypes_AreSealed()
    {
        var selectorTypes = new[]
        {
            typeof(RoundRobinShardReplicaSelector),
            typeof(RandomShardReplicaSelector),
            typeof(LeastLatencyShardReplicaSelector),
            typeof(LeastConnectionsShardReplicaSelector),
            typeof(WeightedRandomShardReplicaSelector),
        };

        foreach (var type in selectorTypes)
        {
            type.IsSealed.ShouldBeTrue($"{type.Name} should be sealed");
        }
    }
}

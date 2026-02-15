using System.ComponentModel.DataAnnotations;
using Encina.Sharding;
using Encina.Sharding.ReferenceTables;

namespace Encina.PropertyTests.Database.Sharding;

/// <summary>
/// Property-based tests for reference table infrastructure types:
/// <see cref="ReferenceTableHashComputer"/>, <see cref="ReferenceTableRegistry"/>,
/// and <see cref="ReplicationResult"/>.
/// </summary>
[Trait("Category", "Property")]
public sealed class ReferenceTablePropertyTests
{
    #region Test Entities

    private sealed class TestProduct
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
    }

    private sealed class TestCategory
    {
        public int Id { get; set; }
        public string Label { get; set; } = "";
    }

    private sealed class TestRegion
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
    }

    #endregion

    #region ReferenceTableHashComputer — Determinism

    [Property(MaxTest = 100)]
    public bool Property_ComputeHash_IsDeterministic(PositiveInt seed)
    {
        var entities = GenerateProducts(seed.Get % 20 + 1, seed.Get);

        var hash1 = ReferenceTableHashComputer.ComputeHash<TestProduct>(entities);
        var hash2 = ReferenceTableHashComputer.ComputeHash<TestProduct>(entities);

        return hash1 == hash2;
    }

    [Property(MaxTest = 100)]
    public bool Property_ComputeHash_IsOrderIndependent(PositiveInt seed)
    {
        var entities = GenerateProducts(seed.Get % 10 + 2, seed.Get);
        var reversed = entities.AsEnumerable().Reverse().ToList();

        var hash1 = ReferenceTableHashComputer.ComputeHash<TestProduct>(entities);
        var hash2 = ReferenceTableHashComputer.ComputeHash<TestProduct>(reversed);

        return hash1 == hash2;
    }

    #endregion

    #region ReferenceTableHashComputer — Format

    [Property(MaxTest = 50)]
    public bool Property_ComputeHash_AlwaysReturns16CharHexString(PositiveInt count)
    {
        var entities = GenerateProducts(count.Get % 20 + 1, count.Get);

        var hash = ReferenceTableHashComputer.ComputeHash<TestProduct>(entities);

        return hash.Length == 16 && hash.All(c => "0123456789abcdef".Contains(c));
    }

    [Fact]
    [Trait("Category", "Property")]
    public void Property_ComputeHash_EmptyCollection_ReturnsZeroHash()
    {
        var hash = ReferenceTableHashComputer.ComputeHash<TestProduct>([]);

        hash.ShouldBe("0000000000000000");
    }

    #endregion

    #region ReferenceTableHashComputer — Collision Resistance

    [Property(MaxTest = 50)]
    public bool Property_ComputeHash_DifferentData_ProducesDifferentHash(PositiveInt id)
    {
        var entities1 = new List<TestProduct>
        {
            new() { Id = id.Get, Name = "Product A", Price = 10.0m },
        };
        var entities2 = new List<TestProduct>
        {
            new() { Id = id.Get, Name = "Product B", Price = 20.0m },
        };

        var hash1 = ReferenceTableHashComputer.ComputeHash<TestProduct>(entities1);
        var hash2 = ReferenceTableHashComputer.ComputeHash<TestProduct>(entities2);

        return hash1 != hash2;
    }

    #endregion

    #region ReferenceTableRegistry — Lookup Consistency

    [Property(MaxTest = 50)]
    public bool Property_Registry_IsRegistered_ConsistentWithGetAllConfigurations(PositiveInt count)
    {
        var n = count.Get % 3 + 1; // 1..3 (we only have 3 test entity types)
        var types = GetEntityTypes().Take(n).ToList();

        var configs = types
            .Select(t => new ReferenceTableConfiguration(t, new ReferenceTableOptions()))
            .ToList();
        var registry = new ReferenceTableRegistry(configs);

        var allConfigs = registry.GetAllConfigurations();

        return allConfigs.Count == n
            && types.All(t => registry.IsRegistered(t));
    }

    [Property(MaxTest = 50)]
    public bool Property_Registry_TryGetConfiguration_ConsistentWithIsRegistered(PositiveInt _)
    {
        var configs = new[]
        {
            new ReferenceTableConfiguration(typeof(TestProduct), new ReferenceTableOptions()),
        };
        var registry = new ReferenceTableRegistry(configs);

        // Registered type: both methods must agree
        var isRegistered = registry.IsRegistered(typeof(TestProduct));
        var tryResult = registry.TryGetConfiguration(typeof(TestProduct), out var config);

        if (isRegistered != tryResult) return false;
        if (isRegistered && config is null) return false;

        // Unregistered type: both methods must agree
        var isNotRegistered = !registry.IsRegistered(typeof(TestCategory));
        var tryNotResult = !registry.TryGetConfiguration(typeof(TestCategory), out var notConfig);

        return isNotRegistered == tryNotResult && notConfig is null;
    }

    [Fact]
    [Trait("Category", "Property")]
    public void Property_Registry_UnregisteredType_IsRegisteredReturnsFalse()
    {
        var registry = new ReferenceTableRegistry([]);

        registry.IsRegistered(typeof(TestProduct)).ShouldBeFalse();
    }

    #endregion

    #region ReplicationResult — IsComplete / IsPartial

    [Property(MaxTest = 100)]
    public bool Property_ReplicationResult_IsComplete_WhenNoFailures(PositiveInt shardCount)
    {
        var n = shardCount.Get % 10 + 1;
        var shardResults = Enumerable.Range(0, n)
            .Select(i => new ShardReplicationResult($"shard-{i}", 100, TimeSpan.FromMilliseconds(50)))
            .ToList();

        var result = new ReplicationResult(n * 100, TimeSpan.FromSeconds(1), shardResults, []);

        return result.IsComplete && !result.IsPartial;
    }

    [Property(MaxTest = 100)]
    public bool Property_ReplicationResult_IsPartial_WhenBothSuccessAndFailure(
        PositiveInt successCount, PositiveInt failureCount)
    {
        var s = successCount.Get % 5 + 1;
        var f = failureCount.Get % 5 + 1;

        var shardResults = Enumerable.Range(0, s)
            .Select(i => new ShardReplicationResult($"shard-s-{i}", 10, TimeSpan.FromMilliseconds(50)))
            .ToList();
        var failures = Enumerable.Range(0, f)
            .Select(i => new ShardFailure($"shard-f-{i}", EncinaErrors.Create("test", "fail")))
            .ToList();

        var result = new ReplicationResult(s * 10, TimeSpan.FromSeconds(1), shardResults, failures);

        return result.IsPartial && !result.IsComplete;
    }

    #endregion

    #region ReplicationResult — TotalShardsTargeted

    [Property(MaxTest = 100)]
    public bool Property_ReplicationResult_TotalShardsTargeted_IsSumOfSuccessAndFailure(
        PositiveInt successCount, PositiveInt failureCount)
    {
        var s = successCount.Get % 5 + 1;
        var f = failureCount.Get % 5 + 1;

        var shardResults = Enumerable.Range(0, s)
            .Select(i => new ShardReplicationResult($"shard-s-{i}", 10, TimeSpan.FromMilliseconds(50)))
            .ToList();
        var failures = Enumerable.Range(0, f)
            .Select(i => new ShardFailure($"shard-f-{i}", EncinaErrors.Create("test", "fail")))
            .ToList();

        var result = new ReplicationResult(s * 10, TimeSpan.FromSeconds(1), shardResults, failures);

        return result.TotalShardsTargeted == s + f;
    }

    [Fact]
    [Trait("Category", "Property")]
    public void Property_ReplicationResult_AllFailures_IsNeitherCompleteNorPartial()
    {
        var failures = new List<ShardFailure>
        {
            new("shard-0", EncinaErrors.Create("test", "fail")),
            new("shard-1", EncinaErrors.Create("test", "fail")),
        };

        var result = new ReplicationResult(0, TimeSpan.FromSeconds(1), [], failures);

        result.IsComplete.ShouldBeFalse();
        result.IsPartial.ShouldBeFalse();
        result.TotalShardsTargeted.ShouldBe(2);
    }

    #endregion

    #region Helpers

    private static List<TestProduct> GenerateProducts(int count, int seed)
    {
        return Enumerable.Range(1, count)
            .Select(i => new TestProduct
            {
                Id = i,
                Name = $"Product-{seed}-{i}",
                Price = (i * 10.0m) + (seed % 100),
            })
            .ToList();
    }

    private static IEnumerable<Type> GetEntityTypes()
    {
        yield return typeof(TestProduct);
        yield return typeof(TestCategory);
        yield return typeof(TestRegion);
    }

    #endregion
}

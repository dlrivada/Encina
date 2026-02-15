using System.ComponentModel.DataAnnotations;
using BenchmarkDotNet.Attributes;
using Encina.Sharding.ReferenceTables;

namespace Encina.Benchmarks.Sharding;

/// <summary>
/// Performance benchmarks for reference table hashing and registry lookups.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
[SimpleJob]
public class ReferenceTableBenchmarks
{
    private sealed class BenchmarkEntity
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Code { get; set; } = "";
        public decimal Value { get; set; }
    }

    private List<BenchmarkEntity> _entities100 = null!;
    private List<BenchmarkEntity> _entities1000 = null!;
    private List<BenchmarkEntity> _entities5000 = null!;
    private ReferenceTableRegistry _registry = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _entities100 = GenerateEntities(100);
        _entities1000 = GenerateEntities(1000);
        _entities5000 = GenerateEntities(5000);

        var configs = new[]
        {
            new ReferenceTableConfiguration(typeof(BenchmarkEntity), new ReferenceTableOptions())
        };

        _registry = new ReferenceTableRegistry(configs);
    }

    #region Hash Computation Benchmarks

    [Benchmark(Baseline = true, Description = "Hash 100 rows")]
    public string ComputeHash_100Rows()
        => ReferenceTableHashComputer.ComputeHash<BenchmarkEntity>(_entities100);

    [Benchmark(Description = "Hash 1000 rows")]
    public string ComputeHash_1000Rows()
        => ReferenceTableHashComputer.ComputeHash<BenchmarkEntity>(_entities1000);

    [Benchmark(Description = "Hash 5000 rows")]
    public string ComputeHash_5000Rows()
        => ReferenceTableHashComputer.ComputeHash<BenchmarkEntity>(_entities5000);

    [Benchmark(Description = "Hash empty collection")]
    public string ComputeHash_Empty()
        => ReferenceTableHashComputer.ComputeHash<BenchmarkEntity>([]);

    #endregion

    #region Registry Lookup Benchmarks

    [Benchmark(Description = "Registry.IsRegistered (hit)")]
    public bool RegistryIsRegistered_Hit()
        => _registry.IsRegistered<BenchmarkEntity>();

    [Benchmark(Description = "Registry.GetConfiguration")]
    public ReferenceTableConfiguration RegistryGetConfiguration()
        => _registry.GetConfiguration<BenchmarkEntity>();

    [Benchmark(Description = "Registry.GetAllConfigurations")]
    public IReadOnlyCollection<ReferenceTableConfiguration> RegistryGetAllConfigurations()
        => _registry.GetAllConfigurations();

    #endregion

    #region EntityMetadataCache Benchmarks

    [Benchmark(Description = "EntityMetadataCache.GetOrCreate (cached)")]
    public EntityMetadata MetadataCache_GetOrCreate()
        => EntityMetadataCache.GetOrCreate<BenchmarkEntity>();

    #endregion

    private static List<BenchmarkEntity> GenerateEntities(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => new BenchmarkEntity
            {
                Id = i,
                Name = $"Entity-{i}",
                Code = $"E{i:D5}",
                Value = i * 1.5m
            })
            .ToList();
    }
}

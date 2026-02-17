using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Encina.Sharding.ReferenceTables;

namespace Encina.LoadTests.Sharding;

/// <summary>
/// Load tests verifying reference table infrastructure thread-safety and performance under concurrent access.
/// </summary>
internal static class ReferenceTableLoadTests
{
    private const int ConcurrentWorkers = 50;
    private const int IterationsPerWorker = 10_000;

    private sealed class Country
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Code { get; set; } = "";
    }

    private sealed class Currency
    {
        [Key]
        public int Id { get; set; }
        public string Code { get; set; } = "";
    }

    private sealed class Region
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public static async Task RunAllAsync()
    {
        Console.WriteLine();
        Console.WriteLine("=== Reference Table Load Tests ===");
        Console.WriteLine($"Workers: {ConcurrentWorkers}, Iterations/worker: {IterationsPerWorker:N0}");
        Console.WriteLine();

        await RunTestAsync("Registry — Concurrent Lookups", RegistryConcurrentLookupsAsync);
        await RunTestAsync("HashComputer — Concurrent Determinism", HashComputerConcurrentDeterminismAsync);
        await RunTestAsync("EntityMetadataCache — Concurrent Access", EntityMetadataCacheConcurrentAccessAsync);
        await RunTestAsync("Registry — IsRegistered Consistency", RegistryIsRegisteredConsistencyAsync);
    }

    private static async Task RegistryConcurrentLookupsAsync()
    {
        var configs = new[]
        {
            new ReferenceTableConfiguration(typeof(Country), new ReferenceTableOptions()),
            new ReferenceTableConfiguration(typeof(Currency), new ReferenceTableOptions()),
            new ReferenceTableConfiguration(typeof(Region), new ReferenceTableOptions())
        };
        var registry = new ReferenceTableRegistry(configs);

        var errors = new ConcurrentBag<string>();

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(() =>
        {
            for (var i = 0; i < IterationsPerWorker; i++)
            {
                if (!registry.IsRegistered<Country>())
                    errors.Add("Country should be registered");
                if (!registry.IsRegistered<Currency>())
                    errors.Add("Currency should be registered");

                var config = registry.GetConfiguration<Country>();
                if (config.EntityType != typeof(Country))
                    errors.Add("Config type mismatch");

                var all = registry.GetAllConfigurations();
                if (all.Count != 3)
                    errors.Add($"Expected 3 configs, got {all.Count}");
            }
        }));

        await Task.WhenAll(tasks);

        Assert(errors.IsEmpty, $"Found {errors.Count} errors during concurrent registry lookups");
        Console.WriteLine($"  {ConcurrentWorkers * IterationsPerWorker:N0} lookups completed without errors");
    }

    private static async Task HashComputerConcurrentDeterminismAsync()
    {
        var entities = Enumerable.Range(1, 100)
            .Select(i => new Country { Id = i, Name = $"Country-{i}", Code = $"C{i:D3}" })
            .ToList();

        var expectedHash = ReferenceTableHashComputer.ComputeHash<Country>(entities);
        var hashes = new ConcurrentBag<string>();

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(() =>
        {
            for (var i = 0; i < IterationsPerWorker / 10; i++)
            {
                var hash = ReferenceTableHashComputer.ComputeHash<Country>(entities);
                hashes.Add(hash);
            }
        }));

        await Task.WhenAll(tasks);

        var uniqueHashes = hashes.Distinct().ToList();
        Assert(uniqueHashes.Count == 1,
            $"Expected 1 unique hash, got {uniqueHashes.Count} ({string.Join(", ", uniqueHashes.Take(5))})");
        Assert(uniqueHashes[0] == expectedHash,
            $"Hash mismatch: expected {expectedHash}, got {uniqueHashes[0]}");
        Console.WriteLine($"  {hashes.Count:N0} hash computations all returned {expectedHash}");
    }

    private static async Task EntityMetadataCacheConcurrentAccessAsync()
    {
        var errors = new ConcurrentBag<string>();

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(() =>
        {
            for (var i = 0; i < IterationsPerWorker; i++)
            {
                var metadata = EntityMetadataCache.GetOrCreate<Country>();
                if (metadata.PrimaryKey.Property.Name != "Id")
                    errors.Add("PK mismatch");
                if (metadata.TableName != "Country")
                    errors.Add("Table name mismatch");
            }
        }));

        await Task.WhenAll(tasks);

        Assert(errors.IsEmpty, $"Found {errors.Count} errors during concurrent metadata cache access");
        Console.WriteLine($"  {ConcurrentWorkers * IterationsPerWorker:N0} cache lookups completed without errors");
    }

    private static async Task RegistryIsRegisteredConsistencyAsync()
    {
        var configs = new[]
        {
            new ReferenceTableConfiguration(typeof(Country), new ReferenceTableOptions())
        };
        var registry = new ReferenceTableRegistry(configs);

        var errors = new ConcurrentBag<string>();

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(() =>
        {
            for (var i = 0; i < IterationsPerWorker; i++)
            {
                var generic = registry.IsRegistered<Country>();
                var typed = registry.IsRegistered(typeof(Country));
                var tryResult = registry.TryGetConfiguration(typeof(Country), out ReferenceTableConfiguration? _);

                if (generic != typed || typed != tryResult)
                    errors.Add($"Inconsistent: generic={generic}, typed={typed}, try={tryResult}");

                var notRegistered = registry.IsRegistered<Currency>();
                if (notRegistered)
                    errors.Add("Currency should NOT be registered");
            }
        }));

        await Task.WhenAll(tasks);

        Assert(errors.IsEmpty, $"Found {errors.Count} consistency errors");
        Console.WriteLine($"  {ConcurrentWorkers * IterationsPerWorker:N0} consistency checks passed");
    }

    private static async Task RunTestAsync(string name, Func<Task> test)
    {
        Console.Write($"  [{name}] ...");
        var sw = Stopwatch.StartNew();
        try
        {
            await test();
            sw.Stop();
            Console.WriteLine($" PASS ({sw.ElapsedMilliseconds}ms)");
        }
        catch (Exception ex)
        {
            sw.Stop();
            Console.WriteLine($" FAIL ({sw.ElapsedMilliseconds}ms)");
            Console.WriteLine($"    Error: {ex.Message}");
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"Assertion failed: {message}");
        }
    }
}

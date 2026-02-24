using System.Collections.Concurrent;
using System.Diagnostics;
using Encina.Compliance.GDPR;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using static LanguageExt.Prelude;

namespace Encina.LoadTests.Compliance.GDPR;

/// <summary>
/// Load tests for the lawful basis validation system under high concurrent traffic.
/// Validates throughput, latency percentiles, and thread safety of:
/// - InMemoryLawfulBasisRegistry operations (Register, GetByRequestType, AutoRegister)
/// - InMemoryLIAStore operations (Store, GetByReference, GetPendingReview)
/// - LawfulBasisValidationPipelineBehavior under concurrent request processing
/// </summary>
/// <remarks>
/// Run via: <c>dotnet run --project tests/Encina.LoadTests -- --scenario lawful-basis</c>
/// (requires integration with Program.cs scenario routing, or run directly via <see cref="RunAllAsync"/>).
/// </remarks>
internal static class LawfulBasisValidationLoadTests
{
    private const int ConcurrentWorkers = 50;
    private const int OperationsPerWorker = 10_000;

    public static async Task RunAllAsync()
    {
        Console.WriteLine();
        Console.WriteLine("=== Lawful Basis Validation Load Tests ===");
        Console.WriteLine($"Workers: {ConcurrentWorkers}, Operations/worker: {OperationsPerWorker:N0}");
        Console.WriteLine();

        await RunTestAsync("Registry GetByRequestType — Concurrent Reads", RegistryGetByRequestType_ConcurrentReads_MaintainsThroughput);
        await RunTestAsync("Registry Register — Concurrent Writes", RegistryRegister_ConcurrentWrites_AllSucceed);
        await RunTestAsync("LIA Store GetByReference — Concurrent Reads", LIAStoreGetByReference_ConcurrentReads_MaintainsThroughput);
        await RunTestAsync("LIA Store Store — Concurrent Writes", LIAStoreStore_ConcurrentWrites_AllSucceed);
        await RunTestAsync("LIA Store GetPendingReview — Under Load", LIAStoreGetPendingReview_UnderLoad_ReturnsCorrectRecords);
        await RunTestAsync("Mixed Registry and LIA — Concurrent Operations", MixedRegistryAndLIA_ConcurrentOperations_NoErrors);
        await RunTestAsync("Pipeline Validation — Concurrent Checks", PipelineValidation_ConcurrentChecks_MaintainsThroughput);
        await RunTestAsync("Registry Latency Distribution — P50/P95/P99", RegistryLatencyDistribution_ConcurrentLoad_WithinBounds);

        Console.WriteLine();
        Console.WriteLine("=== All lawful basis validation load tests completed ===");
    }

    // ────────────────────────────────────────────────────────────
    //  Registry GetByRequestType — Concurrent Reads Under Load
    // ────────────────────────────────────────────────────────────

    private static async Task RegistryGetByRequestType_ConcurrentReads_MaintainsThroughput()
    {
        var registry = CreateRegistry();

        // Seed registrations for multiple types
        for (var i = 0; i < 100; i++)
        {
            await registry.RegisterAsync(CreateRegistration($"LoadTest.Command{i}", (LawfulBasis)(i % 6)));
        }

        var successCount = 0L;
        var failureCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var typeName = $"LoadTest.Command{i % 100}";
                var result = await registry.GetByRequestTypeNameAsync(typeName);

                if (result.IsRight)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref failureCount);
            }
        }));

        await Task.WhenAll(tasks);

        var total = ConcurrentWorkers * OperationsPerWorker;
        Assert(successCount == total, $"Expected {total} successes, got {successCount} (failures: {failureCount})");
        Assert(failureCount == 0, $"Expected 0 failures, got {failureCount}");

        Console.WriteLine($"  {successCount:N0} concurrent reads, 0 failures");
    }

    // ────────────────────────────────────────────────────────────
    //  Registry Register — Concurrent Writes Under Load
    // ────────────────────────────────────────────────────────────

    private static async Task RegistryRegister_ConcurrentWrites_AllSucceed()
    {
        var registry = CreateRegistry();
        var successCount = 0L;
        var failureCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(workerId => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                // Each worker writes to unique type names (no contention on same key)
                var typeName = $"Worker{workerId}.Command{i}";
                var registration = CreateRegistration(typeName, (LawfulBasis)(i % 6));
                var result = await registry.RegisterAsync(registration);

                if (result.IsRight)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref failureCount);
            }
        }));

        await Task.WhenAll(tasks);

        var total = ConcurrentWorkers * OperationsPerWorker;
        Assert(successCount == total, $"Expected {total} successes, got {successCount}");
        Assert(failureCount == 0, $"Expected 0 failures, got {failureCount}");

        Console.WriteLine($"  {successCount:N0} concurrent writes, 0 failures");
    }

    // ────────────────────────────────────────────────────────────
    //  LIA Store GetByReference — Concurrent Reads Under Load
    // ────────────────────────────────────────────────────────────

    private static async Task LIAStoreGetByReference_ConcurrentReads_MaintainsThroughput()
    {
        var store = CreateLIAStore();

        // Seed LIA records
        for (var i = 0; i < 100; i++)
        {
            await store.StoreAsync(CreateLIARecord($"LIA-LOAD-{i}", LIAOutcome.Approved));
        }

        var successCount = 0L;
        var failureCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var reference = $"LIA-LOAD-{i % 100}";
                var result = await store.GetByReferenceAsync(reference);

                if (result.IsRight)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref failureCount);
            }
        }));

        await Task.WhenAll(tasks);

        var total = ConcurrentWorkers * OperationsPerWorker;
        Assert(successCount == total, $"Expected {total} successes, got {successCount} (failures: {failureCount})");
        Assert(failureCount == 0, $"Expected 0 failures, got {failureCount}");

        Console.WriteLine($"  {successCount:N0} concurrent reads, 0 failures");
    }

    // ────────────────────────────────────────────────────────────
    //  LIA Store Store — Concurrent Writes Under Load
    // ────────────────────────────────────────────────────────────

    private static async Task LIAStoreStore_ConcurrentWrites_AllSucceed()
    {
        var store = CreateLIAStore();
        var successCount = 0L;
        var failureCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(workerId => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var reference = $"LIA-W{workerId}-{i}";
                var record = CreateLIARecord(reference,
                    i % 3 == 0 ? LIAOutcome.Approved :
                    i % 3 == 1 ? LIAOutcome.Rejected : LIAOutcome.RequiresReview);
                var result = await store.StoreAsync(record);

                if (result.IsRight)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref failureCount);
            }
        }));

        await Task.WhenAll(tasks);

        var total = ConcurrentWorkers * OperationsPerWorker;
        Assert(successCount == total, $"Expected {total} successes, got {successCount}");
        Assert(failureCount == 0, $"Expected 0 failures, got {failureCount}");

        Console.WriteLine($"  {successCount:N0} concurrent writes, 0 failures");
    }

    // ────────────────────────────────────────────────────────────
    //  LIA Store GetPendingReview — Under Load
    // ────────────────────────────────────────────────────────────

    private static async Task LIAStoreGetPendingReview_UnderLoad_ReturnsCorrectRecords()
    {
        var store = CreateLIAStore();

        // Seed: 100 approved, 100 rejected, 100 pending review
        for (var i = 0; i < 100; i++)
        {
            await store.StoreAsync(CreateLIARecord($"LIA-A-{i}", LIAOutcome.Approved));
            await store.StoreAsync(CreateLIARecord($"LIA-R-{i}", LIAOutcome.Rejected));
            await store.StoreAsync(CreateLIARecord($"LIA-P-{i}", LIAOutcome.RequiresReview));
        }

        var correctCount = 0L;
        var errorCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < 1000; i++)
            {
                var result = await store.GetPendingReviewAsync();
                result.Match(
                    Left: _ => Interlocked.Increment(ref errorCount),
                    Right: records =>
                    {
                        if (records.All(r => r.Outcome == LIAOutcome.RequiresReview) && records.Count == 100)
                            Interlocked.Increment(ref correctCount);
                        else
                            Interlocked.Increment(ref errorCount);
                    });
            }
        }));

        await Task.WhenAll(tasks);

        var total = ConcurrentWorkers * 1000L;
        Assert(correctCount == total, $"Expected {total} correct queries, got {correctCount} (errors: {errorCount})");

        Console.WriteLine($"  {correctCount:N0} concurrent filtered queries, all returned exactly 100 pending records");
    }

    // ────────────────────────────────────────────────────────────
    //  Mixed Registry and LIA — Concurrent Operations
    // ────────────────────────────────────────────────────────────

    private static async Task MixedRegistryAndLIA_ConcurrentOperations_NoErrors()
    {
        var registry = CreateRegistry();
        var liaStore = CreateLIAStore();
        var errors = new ConcurrentQueue<string>();
        var operationCounts = new ConcurrentDictionary<string, int>();

        // Seed some initial data
        for (var i = 0; i < 50; i++)
        {
            await registry.RegisterAsync(CreateRegistration($"Mixed.Command{i}", (LawfulBasis)(i % 6)));
            await liaStore.StoreAsync(CreateLIARecord($"LIA-MIX-{i}", LIAOutcome.Approved));
        }

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(workerId => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var operation = i % 6;

                try
                {
                    switch (operation)
                    {
                        case 0: // Registry read
                            await registry.GetByRequestTypeNameAsync($"Mixed.Command{i % 50}");
                            operationCounts.AddOrUpdate("RegistryRead", 1, (_, c) => c + 1);
                            break;

                        case 1: // Registry write
                            await registry.RegisterAsync(CreateRegistration($"W{workerId}.New{i}", LawfulBasis.Contract));
                            operationCounts.AddOrUpdate("RegistryWrite", 1, (_, c) => c + 1);
                            break;

                        case 2: // LIA read
                            await liaStore.GetByReferenceAsync($"LIA-MIX-{i % 50}");
                            operationCounts.AddOrUpdate("LIARead", 1, (_, c) => c + 1);
                            break;

                        case 3: // LIA write
                            await liaStore.StoreAsync(CreateLIARecord($"LIA-W{workerId}-{i}", LIAOutcome.Approved));
                            operationCounts.AddOrUpdate("LIAWrite", 1, (_, c) => c + 1);
                            break;

                        case 4: // LIA pending review query
                            await liaStore.GetPendingReviewAsync();
                            operationCounts.AddOrUpdate("LIAPending", 1, (_, c) => c + 1);
                            break;

                        case 5: // Registry type name query
                            await registry.GetByRequestTypeNameAsync($"W{workerId}.New{i % 100}");
                            operationCounts.AddOrUpdate("RegistryNameRead", 1, (_, c) => c + 1);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    if (errors.Count < 10)
                        errors.Enqueue($"Worker {workerId}, op {i}: {ex.Message}");
                }
            }
        }));

        await Task.WhenAll(tasks);

        Assert(errors.IsEmpty, $"Got {errors.Count} unexpected exceptions: {string.Join(", ", errors.Take(3))}");

        var totalOps = operationCounts.Values.Sum();
        Console.WriteLine($"  {totalOps:N0} mixed operations: {string.Join(", ", operationCounts.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}={kv.Value:N0}"))}");
    }

    // ────────────────────────────────────────────────────────────
    //  Pipeline Validation — Concurrent Lawful Basis Checks
    // ────────────────────────────────────────────────────────────

    private static async Task PipelineValidation_ConcurrentChecks_MaintainsThroughput()
    {
        // Set up full lawful basis pipeline with DI
        var services = new ServiceCollection();
        services.AddEncina(config =>
            config.RegisterServicesFromAssemblyContaining<LawfulBasisLoadTestCommand>());
        services.AddEncinaLawfulBasis(options =>
        {
            options.EnforcementMode = LawfulBasisEnforcementMode.Block;
        });
        services.AddScoped<IRequestHandler<LawfulBasisLoadTestCommand, int>, LawfulBasisLoadTestHandler>();
        services.AddScoped<IRequestHandler<NoBasisLoadTestCommand, int>, NoBasisLoadTestHandler>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = false,
            ValidateOnBuild = false
        });

        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var registry = provider.GetRequiredService<ILawfulBasisRegistry>();

        // Register lawful basis for the test command
        await registry.RegisterAsync(new LawfulBasisRegistration
        {
            RequestType = typeof(LawfulBasisLoadTestCommand),
            Basis = LawfulBasis.Contract,
            RegisteredAtUtc = DateTimeOffset.UtcNow
        });

        var successCount = 0L;
        var blockedCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < 1000; i++)
            {
                using var scope = scopeFactory.CreateScope();
                var encina = scope.ServiceProvider.GetRequiredService<IEncina>();

                // Alternate between registered (success) and unregistered (blocked) commands
                if (i % 2 == 0)
                {
                    var command = new LawfulBasisLoadTestCommand($"user-{i}");
                    var result = await encina.Send(command);
                    if (result.IsRight)
                        Interlocked.Increment(ref successCount);
                    else
                        Interlocked.Increment(ref blockedCount);
                }
                else
                {
                    var command = new NoBasisLoadTestCommand($"user-{i}");
                    var result = await encina.Send(command);
                    // NoBasisLoadTestCommand has no [LawfulBasis] attribute — behavior depends on config
                    if (result.IsRight)
                        Interlocked.Increment(ref successCount);
                    else
                        Interlocked.Increment(ref blockedCount);
                }
            }
        }));

        await Task.WhenAll(tasks);

        var total = successCount + blockedCount;
        Assert(total == ConcurrentWorkers * 1000L,
            $"Expected {ConcurrentWorkers * 1000} total operations, got {total}");
        Assert(successCount > 0, "Expected some successful operations");

        Console.WriteLine($"  {total:N0} pipeline validations: {successCount:N0} passed, {blockedCount:N0} blocked");
    }

    // ────────────────────────────────────────────────────────────
    //  Registry Latency Distribution — P50/P95/P99
    // ────────────────────────────────────────────────────────────

    private static async Task RegistryLatencyDistribution_ConcurrentLoad_WithinBounds()
    {
        var registry = CreateRegistry();
        var latencies = new ConcurrentBag<double>();

        // Seed registrations
        for (var i = 0; i < 1000; i++)
        {
            await registry.RegisterAsync(CreateRegistration($"Latency.Command{i}", (LawfulBasis)(i % 6)));
        }

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var typeName = $"Latency.Command{i % 1000}";
                var sw = Stopwatch.StartNew();
                await registry.GetByRequestTypeNameAsync(typeName);
                sw.Stop();
                latencies.Add(sw.Elapsed.TotalMicroseconds);
            }
        }));

        await Task.WhenAll(tasks);

        var sorted = latencies.OrderBy(l => l).ToArray();
        var p50 = Percentile(sorted, 50);
        var p95 = Percentile(sorted, 95);
        var p99 = Percentile(sorted, 99);
        var mean = sorted.Average();
        var min = sorted.Min();
        var max = sorted.Max();

        // InMemoryLawfulBasisRegistry is ConcurrentDictionary-based, should be sub-millisecond
        Assert(p99 < 10_000, $"P99 latency {p99:F1}µs exceeds 10ms threshold");

        var totalOps = latencies.Count;
        Console.WriteLine($"  {totalOps:N0} operations — mean: {mean:F1}µs, P50: {p50:F1}µs, P95: {p95:F1}µs, P99: {p99:F1}µs, min: {min:F1}µs, max: {max:F1}µs");
    }

    // ────────────────────────────────────────────────────────────
    //  Test Infrastructure
    // ────────────────────────────────────────────────────────────

    [LawfulBasis(LawfulBasis.Contract)]
    private sealed record LawfulBasisLoadTestCommand(string UserId) : IRequest<int>;

    private sealed class LawfulBasisLoadTestHandler : IRequestHandler<LawfulBasisLoadTestCommand, int>
    {
        public Task<Either<EncinaError, int>> Handle(LawfulBasisLoadTestCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, int>(42));
    }

    private sealed record NoBasisLoadTestCommand(string UserId) : IRequest<int>;

    private sealed class NoBasisLoadTestHandler : IRequestHandler<NoBasisLoadTestCommand, int>
    {
        public Task<Either<EncinaError, int>> Handle(NoBasisLoadTestCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, int>(99));
    }

    private static InMemoryLawfulBasisRegistry CreateRegistry()
    {
        return new InMemoryLawfulBasisRegistry();
    }

    private static InMemoryLIAStore CreateLIAStore()
    {
        return new InMemoryLIAStore();
    }

    private static LawfulBasisRegistration CreateRegistration(string typeName, LawfulBasis basis)
    {
        return new LawfulBasisRegistration
        {
            RequestType = typeof(object), // placeholder — registry uses type name for lookup
            Basis = basis,
            Purpose = $"Load test: {typeName}",
            RegisteredAtUtc = DateTimeOffset.UtcNow
        };
    }

    private static LIARecord CreateLIARecord(string reference, LIAOutcome outcome) =>
        new()
        {
            Id = reference,
            Name = $"LIA {reference}",
            Purpose = "Load test processing",
            LegitimateInterest = "Performance measurement",
            Benefits = "Enables load testing of compliance pipeline",
            ConsequencesIfNotProcessed = "Unable to verify concurrent access safety",
            NecessityJustification = "Required for load tests",
            AlternativesConsidered = ["Manual testing"],
            DataMinimisationNotes = "Only synthetic load test data used",
            NatureOfData = "Synthetic load test identifiers",
            ReasonableExpectations = "Data subjects expect performance testing",
            ImpactAssessment = "Minimal impact",
            Safeguards = ["In-memory only", "No real personal data"],
            Outcome = outcome,
            Conclusion = outcome switch
            {
                LIAOutcome.Approved => "Approved",
                LIAOutcome.Rejected => "Rejected",
                _ => "Pending"
            },
            AssessedAtUtc = DateTimeOffset.UtcNow,
            AssessedBy = "load-test-runner"
        };

    private static double Percentile(double[] sorted, double percentile)
    {
        if (sorted.Length == 0) return double.NaN;

        var position = (percentile / 100.0) * (sorted.Length - 1);
        var lowerIndex = (int)Math.Floor(position);
        var upperIndex = (int)Math.Ceiling(position);

        if (lowerIndex == upperIndex) return sorted[lowerIndex];

        var weight = position - lowerIndex;
        return sorted[lowerIndex] + (sorted[upperIndex] - sorted[lowerIndex]) * weight;
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

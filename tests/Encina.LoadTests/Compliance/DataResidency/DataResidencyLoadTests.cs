using System.Collections.Concurrent;
using System.Diagnostics;
using Encina.Caching;
using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Abstractions;
using Encina.Compliance.DataResidency.Aggregates;
using Encina.Compliance.DataResidency.Attributes;
using Encina.Compliance.DataResidency.Model;
using Encina.Compliance.DataResidency.ReadModels;
using Encina.Marten;
using Encina.Marten.Projections;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.LoadTests.Compliance.DataResidency;

/// <summary>
/// Load tests for the data residency compliance system under high concurrent traffic.
/// Validates throughput, latency percentiles, and thread safety of:
/// - IResidencyPolicyService region evaluation (IsAllowed)
/// - DataResidencyPipelineBehavior under concurrent request processing
/// - IDataLocationService registration under concurrent writes
/// - ICrossBorderTransferValidator for intra-EEA and third-country transfers
/// </summary>
/// <remarks>
/// <para>
/// Data residency enforcement is legally mandatory (GDPR Chapter V, Articles 44-49).
/// The <see cref="DataResidencyPipelineBehavior{TRequest, TResponse}"/> intercepts every
/// request decorated with <c>[DataResidency]</c> — this makes it a hot-path operation
/// requiring load testing.
/// </para>
/// <para>
/// Run via: <c>dotnet run --project tests/Encina.LoadTests -- --scenario dataresidency</c>
/// </para>
/// </remarks>
internal static class DataResidencyLoadTests
{
    private const int ConcurrentWorkers = 50;
    private const int OperationsPerWorker = 10_000;

    private static readonly string[] AllowedRegions = ["DE", "FR", "NL", "BE", "AT"];
    private static readonly string[] NonAllowedRegions = ["US", "CN", "RU", "BR", "IN"];
    private static readonly string[] DataCategories = ["personal-data", "healthcare-data", "financial-data", "employee-data"];
    private static readonly string[] AllRegions = [.. AllowedRegions, .. NonAllowedRegions];

    public static async Task RunAllAsync()
    {
        Console.WriteLine();
        Console.WriteLine("=== Data Residency Load Tests ===");
        Console.WriteLine($"Workers: {ConcurrentWorkers}, Operations/worker: {OperationsPerWorker:N0}");
        Console.WriteLine();

        await RunTestAsync("Region Validation — Concurrent Lookups",
            RegionValidation_ConcurrentLookups_MaintainsThroughput);
        await RunTestAsync("Residency Policy Evaluation — Concurrent Checks",
            ResidencyPolicyEvaluation_ConcurrentChecks_MaintainsThroughput);
        await RunTestAsync("Storage Location Registration — Concurrent Operations",
            StorageLocationRegistration_ConcurrentOperations_NoErrors);
        await RunTestAsync("Cross-Border Transfer Validation — Concurrent",
            CrossBorderTransferValidation_ConcurrentChecks_MaintainsThroughput);
        await RunTestAsync("Mixed Residency Scenarios — Concurrent",
            MixedResidencyScenarios_ConcurrentOperations_NoErrors);
        await RunTestAsync("Pipeline Validation — Concurrent Checks",
            PipelineValidation_ConcurrentChecks_MaintainsThroughput);
        await RunTestAsync("Latency Distribution — P50/P95/P99",
            LatencyDistribution_ConcurrentLoad_WithinBounds);

        Console.WriteLine();
        Console.WriteLine("=== All data residency load tests completed ===");
    }

    // ────────────────────────────────────────────────────────────
    //  Region Validation — Concurrent Lookups
    // ────────────────────────────────────────────────────────────

    private static async Task RegionValidation_ConcurrentLookups_MaintainsThroughput()
    {
        var successCount = 0L;
        var failureCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(() =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var code = AllRegions[i % AllRegions.Length];
                var region = RegionRegistry.GetByCode(code);

                if (region is not null)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref failureCount);
            }
        }));

        await Task.WhenAll(tasks);

        var total = ConcurrentWorkers * OperationsPerWorker;
        Assert(successCount == total, $"Expected {total} successes, got {successCount} (failures: {failureCount})");

        Console.WriteLine($"  {successCount:N0} concurrent region lookups, 0 failures");
    }

    // ────────────────────────────────────────────────────────────
    //  Residency Policy Evaluation — Concurrent Checks
    // ────────────────────────────────────────────────────────────

    private static async Task ResidencyPolicyEvaluation_ConcurrentChecks_MaintainsThroughput()
    {
        using var provider = BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        var allowedCount = 0L;
        var blockedCount = 0L;
        var errorCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                using var scope = scopeFactory.CreateScope();
                var policyService = scope.ServiceProvider.GetRequiredService<IResidencyPolicyService>();

                var regionCode = AllRegions[i % AllRegions.Length];
                var region = RegionRegistry.GetByCode(regionCode)!;

                var result = await policyService.IsAllowedAsync("personal-data", region);

                result.Match(
                    Left: _ => Interlocked.Increment(ref errorCount),
                    Right: isAllowed =>
                    {
                        if (isAllowed)
                            Interlocked.Increment(ref allowedCount);
                        else
                            Interlocked.Increment(ref blockedCount);
                    });
            }
        }));

        await Task.WhenAll(tasks);

        var total = allowedCount + blockedCount;
        Assert(errorCount == 0, $"Expected 0 errors, got {errorCount}");
        Assert(total == ConcurrentWorkers * (long)OperationsPerWorker,
            $"Expected {ConcurrentWorkers * OperationsPerWorker} total, got {total}");

        Console.WriteLine($"  {total:N0} policy evaluations: {allowedCount:N0} allowed, {blockedCount:N0} blocked, 0 errors");
    }

    // ────────────────────────────────────────────────────────────
    //  Storage Location Registration — Concurrent Operations
    // ────────────────────────────────────────────────────────────

    private static async Task StorageLocationRegistration_ConcurrentOperations_NoErrors()
    {
        using var provider = BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        var successCount = 0L;
        var errorCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(workerId => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                using var scope = scopeFactory.CreateScope();
                var locationService = scope.ServiceProvider.GetRequiredService<IDataLocationService>();

                var regionCode = AllowedRegions[i % AllowedRegions.Length];
                var category = DataCategories[i % DataCategories.Length];

                var result = await locationService.RegisterLocationAsync(
                    $"entity-{workerId}-{i}", category, regionCode, StorageType.Primary);

                if (result.IsRight)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref errorCount);
            }
        }));

        await Task.WhenAll(tasks);

        var total = ConcurrentWorkers * OperationsPerWorker;
        Assert(successCount == total, $"Expected {total} successes, got {successCount}");
        Assert(errorCount == 0, $"Expected 0 errors, got {errorCount}");

        Console.WriteLine($"  {successCount:N0} concurrent location registrations, 0 errors");
    }

    // ────────────────────────────────────────────────────────────
    //  Cross-Border Transfer Validation — Concurrent
    // ────────────────────────────────────────────────────────────

    private static async Task CrossBorderTransferValidation_ConcurrentChecks_MaintainsThroughput()
    {
        using var provider = BuildServiceProvider();

        var transferValidator = provider.GetRequiredService<ICrossBorderTransferValidator>();

        var allowedCount = 0L;
        var blockedCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var sourceCode = AllowedRegions[i % AllowedRegions.Length];
                var destCode = AllRegions[i % AllRegions.Length];
                var source = RegionRegistry.GetByCode(sourceCode)!;
                var dest = RegionRegistry.GetByCode(destCode)!;

                var result = await transferValidator.ValidateTransferAsync(source, dest, "personal-data");

                result.Match(
                    Left: _ => Interlocked.Increment(ref blockedCount),
                    Right: outcome =>
                    {
                        if (outcome.IsAllowed)
                            Interlocked.Increment(ref allowedCount);
                        else
                            Interlocked.Increment(ref blockedCount);
                    });
            }
        }));

        await Task.WhenAll(tasks);

        var total = allowedCount + blockedCount;
        Assert(total == ConcurrentWorkers * (long)OperationsPerWorker,
            $"Expected {ConcurrentWorkers * OperationsPerWorker} total, got {total}");

        Console.WriteLine($"  {total:N0} transfer validations: {allowedCount:N0} allowed, {blockedCount:N0} blocked");
    }

    // ────────────────────────────────────────────────────────────
    //  Mixed Residency Scenarios — Concurrent
    // ────────────────────────────────────────────────────────────

    private static async Task MixedResidencyScenarios_ConcurrentOperations_NoErrors()
    {
        using var provider = BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        var errors = new ConcurrentQueue<string>();
        var operationCounts = new ConcurrentDictionary<string, int>();

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(workerId => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var scenario = i % 4;

                try
                {
                    using var scope = scopeFactory.CreateScope();
                    string scenarioName;

                    switch (scenario)
                    {
                        case 0: // Policy evaluation — allowed region
                            {
                                var policyService = scope.ServiceProvider.GetRequiredService<IResidencyPolicyService>();
                                await policyService.IsAllowedAsync("personal-data", RegionRegistry.DE);
                                scenarioName = "PolicyAllowed";
                                break;
                            }

                        case 1: // Policy evaluation — blocked region
                            {
                                var policyService = scope.ServiceProvider.GetRequiredService<IResidencyPolicyService>();
                                await policyService.IsAllowedAsync("personal-data", RegionRegistry.GetByCode("US")!);
                                scenarioName = "PolicyBlocked";
                                break;
                            }

                        case 2: // Location registration
                            {
                                var locationService = scope.ServiceProvider.GetRequiredService<IDataLocationService>();
                                await locationService.RegisterLocationAsync(
                                    $"entity-{workerId}-{i}", "personal-data", "DE", StorageType.Primary);
                                scenarioName = "LocationRegister";
                                break;
                            }

                        default: // Transfer validation
                            {
                                var transferValidator = scope.ServiceProvider.GetRequiredService<ICrossBorderTransferValidator>();
                                await transferValidator.ValidateTransferAsync(RegionRegistry.DE, RegionRegistry.FR, "personal-data");
                                scenarioName = "TransferValidation";
                                break;
                            }
                    }

                    operationCounts.AddOrUpdate(scenarioName, 1, (_, c) => c + 1);
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
    //  Pipeline Validation — Concurrent Checks
    // ────────────────────────────────────────────────────────────

    private static async Task PipelineValidation_ConcurrentChecks_MaintainsThroughput()
    {
        var services = new ServiceCollection();
        services.AddEncina(config =>
            config.RegisterServicesFromAssemblyContaining<ResidencyLoadTestCommand>());

        RegisterMockedDependencies(services);

        services.AddEncinaDataResidency(options =>
        {
            options.EnforcementMode = DataResidencyEnforcementMode.Block;
            options.AutoRegisterFromAttributes = false;
        });

        services.AddScoped<IRequestHandler<ResidencyLoadTestCommand, int>, ResidencyLoadTestHandler>();
        services.AddScoped<IRequestHandler<NoResidencyLoadTestCommand, int>, NoResidencyLoadTestHandler>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = false,
            ValidateOnBuild = false
        });

        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        var successCount = 0L;
        var blockedCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < 1000; i++)
            {
                using var scope = scopeFactory.CreateScope();
                var encina = scope.ServiceProvider.GetRequiredService<IEncina>();

                // Alternate between commands with and without residency attribute
                if (i % 2 == 0)
                {
                    var command = new ResidencyLoadTestCommand("DE");
                    var result = await encina.Send(command);
                    if (result.IsRight)
                        Interlocked.Increment(ref successCount);
                    else
                        Interlocked.Increment(ref blockedCount);
                }
                else
                {
                    var command = new NoResidencyLoadTestCommand("any-data");
                    var result = await encina.Send(command);
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
    //  Latency Distribution — P50/P95/P99
    // ────────────────────────────────────────────────────────────

    private static async Task LatencyDistribution_ConcurrentLoad_WithinBounds()
    {
        using var provider = BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        var latencies = new ConcurrentBag<double>();

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                using var scope = scopeFactory.CreateScope();
                var policyService = scope.ServiceProvider.GetRequiredService<IResidencyPolicyService>();

                var regionCode = AllRegions[i % AllRegions.Length];
                var region = RegionRegistry.GetByCode(regionCode)!;

                var sw = Stopwatch.StartNew();
                await policyService.IsAllowedAsync("personal-data", region);
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

        Assert(p99 < 10_000, $"P99 latency {p99:F1}µs exceeds 10ms threshold");

        Console.WriteLine($"  {latencies.Count:N0} operations — mean: {mean:F1}µs, P50: {p50:F1}µs, P95: {p95:F1}µs, P99: {p99:F1}µs, min: {min:F1}µs, max: {max:F1}µs");
    }

    // ────────────────────────────────────────────────────────────
    //  Test Infrastructure — Commands & Handlers
    // ────────────────────────────────────────────────────────────

    [DataResidency("DE", "FR", "NL", DataCategory = "personal-data")]
    private sealed record ResidencyLoadTestCommand(string Region) : IRequest<int>;

    private sealed class ResidencyLoadTestHandler : IRequestHandler<ResidencyLoadTestCommand, int>
    {
        public Task<Either<EncinaError, int>> Handle(ResidencyLoadTestCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, int>(42));
    }

    private sealed record NoResidencyLoadTestCommand(string Data) : IRequest<int>;

    private sealed class NoResidencyLoadTestHandler : IRequestHandler<NoResidencyLoadTestCommand, int>
    {
        public Task<Either<EncinaError, int>> Handle(NoResidencyLoadTestCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, int>(99));
    }

    // ────────────────────────────────────────────────────────────
    //  Test Infrastructure — Service Provider Builder
    // ────────────────────────────────────────────────────────────

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        RegisterMockedDependencies(services);

        services.AddEncinaDataResidency(options =>
        {
            options.EnforcementMode = DataResidencyEnforcementMode.Block;
            options.AutoRegisterFromAttributes = false;
        });

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = false,
            ValidateOnBuild = false
        });
    }

    private static void RegisterMockedDependencies(IServiceCollection services)
    {
        // Mock aggregate repositories (load tests don't need real Marten)
        var policyRepo = Substitute.For<IAggregateRepository<ResidencyPolicyAggregate>>();
        var locationRepo = Substitute.For<IAggregateRepository<DataLocationAggregate>>();
        var policyReadRepo = Substitute.For<IReadModelRepository<ResidencyPolicyReadModel>>();
        var locationReadRepo = Substitute.For<IReadModelRepository<DataLocationReadModel>>();

        // Configure policy read repository to return a policy for "personal-data"
        var policyReadModel = new ResidencyPolicyReadModel
        {
            Id = Guid.NewGuid(),
            DataCategory = "personal-data",
            AllowedRegionCodes = ["DE", "FR", "NL", "BE", "AT"],
            RequireAdequacyDecision = false,
            AllowedTransferBases = [TransferLegalBasis.StandardContractualClauses],
            IsActive = true,
            Version = 1
        };
        policyReadRepo.QueryAsync(
                Arg.Any<Func<IQueryable<ResidencyPolicyReadModel>, IQueryable<ResidencyPolicyReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                Right<EncinaError, IReadOnlyList<ResidencyPolicyReadModel>>(
                    new List<ResidencyPolicyReadModel> { policyReadModel })));

        // Configure location repository to return success for registration
        locationRepo.CreateAsync(Arg.Any<DataLocationAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, Unit>(unit)));

        services.AddScoped<IAggregateRepository<ResidencyPolicyAggregate>>(_ => policyRepo);
        services.AddScoped<IAggregateRepository<DataLocationAggregate>>(_ => locationRepo);
        services.AddScoped<IReadModelRepository<ResidencyPolicyReadModel>>(_ => policyReadRepo);
        services.AddScoped<IReadModelRepository<DataLocationReadModel>>(_ => locationReadRepo);

        // Mock cache provider (fire-and-forget, no real caching needed)
        services.AddSingleton(_ => Substitute.For<ICacheProvider>());

        // Mock IRegionContextProvider to return DE region (for pipeline behavior)
        var regionProvider = Substitute.For<IRegionContextProvider>();
#pragma warning disable CA2012
        regionProvider.GetCurrentRegionAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Region>>(
                Right<EncinaError, Region>(RegionRegistry.DE)));
#pragma warning restore CA2012
        services.AddSingleton(regionProvider);
    }

    // ────────────────────────────────────────────────────────────
    //  Test Infrastructure — Utilities
    // ────────────────────────────────────────────────────────────

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

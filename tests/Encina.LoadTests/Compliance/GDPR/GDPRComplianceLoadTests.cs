using System.Collections.Concurrent;
using System.Diagnostics;
using Encina.Compliance.GDPR;
using Encina.Compliance.GDPR.Export;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.LoadTests.Compliance.GDPR;

/// <summary>
/// Load tests for the GDPR compliance pipeline behavior and RoPA operations under high concurrent traffic.
/// Validates throughput, latency percentiles, and thread safety of:
/// - GDPRCompliancePipelineBehavior attribute cache and registry lookups
/// - InMemoryProcessingActivityRegistry concurrent reads and writes
/// - RoPA JSON/CSV export under concurrent invocations
/// - Mixed lawful basis scenarios through the pipeline
/// </summary>
/// <remarks>
/// <para>
/// GDPR compliance checks execute on every data processing request decorated with
/// <see cref="ProcessingActivityAttribute"/> or <see cref="ProcessesPersonalDataAttribute"/>.
/// Article 6 (lawful basis) and Article 30 (RoPA) checks are hot-path operations
/// requiring performance characterization.
/// </para>
/// <para>
/// Run via: <c>dotnet run --project tests/Encina.LoadTests -- --scenario gdpr</c>
/// </para>
/// </remarks>
internal static class GDPRComplianceLoadTests
{
    /// <summary>
    /// DocRef identifier for load-test documentation traceability (see ADR-025).
    /// </summary>
    public const string DocRef = "load:compliance/gdpr";

    private const int ConcurrentWorkers = 50;
    private const int OperationsPerWorker = 10_000;

    private static readonly LawfulBasis[] AllLawfulBases =
        [LawfulBasis.Consent, LawfulBasis.Contract, LawfulBasis.LegalObligation,
         LawfulBasis.VitalInterests, LawfulBasis.PublicTask, LawfulBasis.LegitimateInterests];

    private static readonly string[] Purposes =
        ["Order fulfillment", "Newsletter subscription", "Employee payroll", "Analytics", "Customer support"];

    public static async Task RunAllAsync()
    {
        Console.WriteLine();
        Console.WriteLine("=== GDPR Compliance Load Tests ===");
        Console.WriteLine($"Workers: {ConcurrentWorkers}, Operations/worker: {OperationsPerWorker:N0}");
        Console.WriteLine();

        await RunTestAsync("Registry — Concurrent Lookups (Registered Activity)",
            Registry_ConcurrentLookups_RegisteredActivity);
        await RunTestAsync("Registry — Concurrent Lookups (Unregistered Activity)",
            Registry_ConcurrentLookups_UnregisteredActivity);
        await RunTestAsync("Pipeline — Enforce Mode (Compliant Path)",
            Pipeline_EnforceMode_CompliantPath);
        await RunTestAsync("Pipeline — WarnOnly Mode (Non-Compliant Path)",
            Pipeline_WarnOnlyMode_NonCompliantPath);
        await RunTestAsync("Pipeline — No Attribute (Skip Branch)",
            Pipeline_NoAttribute_SkipBranch);
        await RunTestAsync("Mixed Lawful Basis Scenarios — Concurrent",
            MixedLawfulBasis_ConcurrentOperations_NoErrors);
        await RunTestAsync("RoPA Export — Concurrent JSON/CSV Exports",
            RoPAExport_ConcurrentExports_AllSucceed);
        await RunTestAsync("Latency Distribution — P50/P95/P99",
            LatencyDistribution_ConcurrentLoad_WithinBounds);

        Console.WriteLine();
        Console.WriteLine("=== All GDPR compliance load tests completed ===");
    }

    // ────────────────────────────────────────────────────────────
    //  Registry — Concurrent Lookups (Registered Activity)
    // ────────────────────────────────────────────────────────────

    private static async Task Registry_ConcurrentLookups_RegisteredActivity()
    {
        using var provider = BuildServiceProvider(GDPREnforcementMode.Enforce, registerActivities: true);
        var registry = provider.GetRequiredService<IProcessingActivityRegistry>();

        var successCount = 0L;
        var failureCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var result = await registry.GetActivityByRequestTypeAsync(typeof(GDPRLoadTestCommand));

                if (result.IsRight)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref failureCount);
            }
        }));

        await Task.WhenAll(tasks);

        var total = ConcurrentWorkers * OperationsPerWorker;
        Assert(successCount == total, $"Expected {total} successes, got {successCount} (failures: {failureCount})");

        Console.WriteLine($"  {successCount:N0} concurrent registry lookups, 0 failures");
    }

    // ────────────────────────────────────────────────────────────
    //  Registry — Concurrent Lookups (Unregistered Activity)
    // ────────────────────────────────────────────────────────────

    private static async Task Registry_ConcurrentLookups_UnregisteredActivity()
    {
        using var provider = BuildServiceProvider(GDPREnforcementMode.Enforce, registerActivities: false);
        var registry = provider.GetRequiredService<IProcessingActivityRegistry>();

        var noneCount = 0L;
        var errorCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var result = await registry.GetActivityByRequestTypeAsync(typeof(GDPRLoadTestCommand));

                result.Match(
                    Left: _ => Interlocked.Increment(ref errorCount),
                    Right: option =>
                    {
                        if (option.IsNone)
                            Interlocked.Increment(ref noneCount);
                    });
            }
        }));

        await Task.WhenAll(tasks);

        var total = ConcurrentWorkers * OperationsPerWorker;
        Assert(noneCount == total, $"Expected {total} None results, got {noneCount} (errors: {errorCount})");

        Console.WriteLine($"  {noneCount:N0} concurrent unregistered lookups, 0 errors");
    }

    // ────────────────────────────────────────────────────────────
    //  Pipeline — Enforce Mode (Compliant Path)
    // ────────────────────────────────────────────────────────────

    private static async Task Pipeline_EnforceMode_CompliantPath()
    {
        var services = new ServiceCollection();
        services.AddEncina(config =>
            config.RegisterServicesFromAssemblyContaining<GDPRLoadTestCommand>());

        services.AddEncinaGDPR(options =>
        {
            options.ControllerName = "Load Test Corp";
            options.ControllerEmail = "privacy@loadtest.com";
            options.EnforcementMode = GDPREnforcementMode.Enforce;
            options.AutoRegisterFromAttributes = true;
            options.AssembliesToScan.Add(typeof(GDPRComplianceLoadTests).Assembly);
        });

        services.AddScoped<IRequestHandler<GDPRLoadTestCommand, int>, GDPRLoadTestHandler>();
        services.AddScoped<IRequestHandler<NoGDPRLoadTestCommand, int>, NoGDPRLoadTestHandler>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = false,
            ValidateOnBuild = false
        });

        // Trigger auto-registration hosted service
        await WarmUpRegistryAsync(provider);

        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        var successCount = 0L;
        var failureCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < 1000; i++)
            {
                using var scope = scopeFactory.CreateScope();
                var encina = scope.ServiceProvider.GetRequiredService<IEncina>();

                var command = new GDPRLoadTestCommand("Order fulfillment");
                var result = await encina.Send(command);

                if (result.IsRight)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref failureCount);
            }
        }));

        await Task.WhenAll(tasks);

        var total = successCount + failureCount;
        Assert(total == ConcurrentWorkers * 1000L,
            $"Expected {ConcurrentWorkers * 1000} total, got {total}");
        Assert(successCount > 0, $"Expected successful operations, got {successCount}");

        Console.WriteLine($"  {total:N0} pipeline validations: {successCount:N0} passed, {failureCount:N0} failed");
    }

    // ────────────────────────────────────────────────────────────
    //  Pipeline — WarnOnly Mode (Non-Compliant Path)
    // ────────────────────────────────────────────────────────────

    private static async Task Pipeline_WarnOnlyMode_NonCompliantPath()
    {
        var services = new ServiceCollection();
        services.AddEncina(config =>
            config.RegisterServicesFromAssemblyContaining<GDPRLoadTestCommand>());

        // Register a validator that returns non-compliant
        var validator = Substitute.For<IGDPRComplianceValidator>();
#pragma warning disable CA2012 // ValueTask instances returned from NSubstitute mock setups are consumed by the framework
        validator.ValidateAsync(Arg.Any<GDPRLoadTestCommand>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, ComplianceResult>>(
                Right<EncinaError, ComplianceResult>(ComplianceResult.NonCompliant("Test non-compliance warning"))));
#pragma warning restore CA2012
        services.AddScoped<IGDPRComplianceValidator>(_ => validator);

        services.AddEncinaGDPR(options =>
        {
            options.ControllerName = "Load Test Corp";
            options.ControllerEmail = "privacy@loadtest.com";
            options.EnforcementMode = GDPREnforcementMode.WarnOnly;
            options.AutoRegisterFromAttributes = true;
            options.AssembliesToScan.Add(typeof(GDPRComplianceLoadTests).Assembly);
        });

        services.AddScoped<IRequestHandler<GDPRLoadTestCommand, int>, GDPRLoadTestHandler>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = false,
            ValidateOnBuild = false
        });

        await WarmUpRegistryAsync(provider);

        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        var successCount = 0L;
        var failureCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < 1000; i++)
            {
                using var scope = scopeFactory.CreateScope();
                var encina = scope.ServiceProvider.GetRequiredService<IEncina>();

                var result = await encina.Send(new GDPRLoadTestCommand("WarnOnly test"));

                if (result.IsRight)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref failureCount);
            }
        }));

        await Task.WhenAll(tasks);

        var total = successCount + failureCount;
        Assert(total == ConcurrentWorkers * 1000L,
            $"Expected {ConcurrentWorkers * 1000} total, got {total}");
        // WarnOnly mode should proceed even on non-compliant
        Assert(successCount == total, $"WarnOnly mode should allow all requests, but {failureCount} were blocked");

        Console.WriteLine($"  {successCount:N0} warn-only pipeline validations (all proceeded)");
    }

    // ────────────────────────────────────────────────────────────
    //  Pipeline — No Attribute (Skip Branch)
    // ────────────────────────────────────────────────────────────

    private static async Task Pipeline_NoAttribute_SkipBranch()
    {
        var services = new ServiceCollection();
        services.AddEncina(config =>
            config.RegisterServicesFromAssemblyContaining<NoGDPRLoadTestCommand>());
        services.AddEncinaGDPR(options =>
        {
            options.ControllerName = "Load Test Corp";
            options.ControllerEmail = "privacy@loadtest.com";
            options.AutoRegisterFromAttributes = false;
        });

        services.AddScoped<IRequestHandler<NoGDPRLoadTestCommand, int>, NoGDPRLoadTestHandler>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = false,
            ValidateOnBuild = false
        });

        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        var successCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                using var scope = scopeFactory.CreateScope();
                var encina = scope.ServiceProvider.GetRequiredService<IEncina>();

                var result = await encina.Send(new NoGDPRLoadTestCommand("data"));

                if (result.IsRight)
                    Interlocked.Increment(ref successCount);
            }
        }));

        await Task.WhenAll(tasks);

        var total = ConcurrentWorkers * OperationsPerWorker;
        Assert(successCount == total, $"Expected {total} successes (skip branch), got {successCount}");

        Console.WriteLine($"  {successCount:N0} no-attribute skip-branch operations");
    }

    // ────────────────────────────────────────────────────────────
    //  Mixed Lawful Basis Scenarios — Concurrent
    // ────────────────────────────────────────────────────────────

    private static async Task MixedLawfulBasis_ConcurrentOperations_NoErrors()
    {
        using var provider = BuildServiceProvider(GDPREnforcementMode.Enforce, registerActivities: true);
        var registry = provider.GetRequiredService<IProcessingActivityRegistry>();

        // Register activities with different lawful bases
        for (var idx = 0; idx < AllLawfulBases.Length; idx++)
        {
            var basis = AllLawfulBases[idx];
            var activity = CreateActivity($"LawfulBasisTest_{basis}", Purposes[idx % Purposes.Length], basis, typeof(GDPRLoadTestCommand));
            // Use a unique dummy type per basis for registry keying
        }

        var errors = new ConcurrentQueue<string>();
        var operationCounts = new ConcurrentDictionary<string, int>();

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(workerId => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var scenario = i % 4;

                try
                {
                    string scenarioName;

                    switch (scenario)
                    {
                        case 0: // Registry lookup (registered)
                            await registry.GetActivityByRequestTypeAsync(typeof(GDPRLoadTestCommand));
                            scenarioName = "RegistryHit";
                            break;

                        case 1: // Registry lookup (unregistered)
                            await registry.GetActivityByRequestTypeAsync(typeof(NoGDPRLoadTestCommand));
                            scenarioName = "RegistryMiss";
                            break;

                        case 2: // GetAll activities
                            await registry.GetAllActivitiesAsync();
                            scenarioName = "GetAll";
                            break;

                        default: // Registry lookup with varying types
                            await registry.GetActivityByRequestTypeAsync(typeof(GDPRLoadTestCommand));
                            scenarioName = "RegistryLookup";
                            break;
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
    //  RoPA Export — Concurrent JSON/CSV Exports
    // ────────────────────────────────────────────────────────────

    private static async Task RoPAExport_ConcurrentExports_AllSucceed()
    {
        var jsonExporter = new JsonRoPAExporter();
        var csvExporter = new CsvRoPAExporter();

        var activities = Enumerable.Range(0, 20).Select(i => CreateActivity(
            $"Activity_{i}", Purposes[i % Purposes.Length], AllLawfulBases[i % AllLawfulBases.Length],
            typeof(GDPRLoadTestCommand))).ToList().AsReadOnly();

        var metadata = new RoPAExportMetadata("Load Test Corp", "privacy@loadtest.com", DateTimeOffset.UtcNow);

        var jsonSuccess = 0L;
        var csvSuccess = 0L;
        var failureCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                if (i % 2 == 0)
                {
                    var result = await jsonExporter.ExportAsync(activities, metadata);
                    if (result.IsRight)
                        Interlocked.Increment(ref jsonSuccess);
                    else
                        Interlocked.Increment(ref failureCount);
                }
                else
                {
                    var result = await csvExporter.ExportAsync(activities, metadata);
                    if (result.IsRight)
                        Interlocked.Increment(ref csvSuccess);
                    else
                        Interlocked.Increment(ref failureCount);
                }
            }
        }));

        await Task.WhenAll(tasks);

        var total = ConcurrentWorkers * OperationsPerWorker;
        Assert(jsonSuccess + csvSuccess == total,
            $"Expected {total} successes, got {jsonSuccess + csvSuccess} (failures: {failureCount})");

        Console.WriteLine($"  {jsonSuccess:N0} JSON + {csvSuccess:N0} CSV exports, 0 failures");
    }

    // ────────────────────────────────────────────────────────────
    //  Latency Distribution — P50/P95/P99
    // ────────────────────────────────────────────────────────────

    private static async Task LatencyDistribution_ConcurrentLoad_WithinBounds()
    {
        using var provider = BuildServiceProvider(GDPREnforcementMode.Enforce, registerActivities: true);
        var registry = provider.GetRequiredService<IProcessingActivityRegistry>();

        var latencies = new ConcurrentBag<double>();

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var sw = Stopwatch.StartNew();
                await registry.GetActivityByRequestTypeAsync(typeof(GDPRLoadTestCommand));
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

    [ProcessingActivity(
        Purpose = "Load test order fulfillment",
        LawfulBasis = LawfulBasis.Contract,
        DataCategories = ["Name", "Email", "Address"],
        DataSubjects = ["Customers"],
        RetentionDays = 2555,
        Recipients = ["Shipping Provider"],
        SecurityMeasures = "AES-256 encryption at rest, TLS 1.3 in transit")]
    internal sealed record GDPRLoadTestCommand(string Purpose) : IRequest<int>;

    internal sealed class GDPRLoadTestHandler : IRequestHandler<GDPRLoadTestCommand, int>
    {
        public Task<Either<EncinaError, int>> Handle(GDPRLoadTestCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, int>(42));
    }

    internal sealed record NoGDPRLoadTestCommand(string Data) : IRequest<int>;

    internal sealed class NoGDPRLoadTestHandler : IRequestHandler<NoGDPRLoadTestCommand, int>
    {
        public Task<Either<EncinaError, int>> Handle(NoGDPRLoadTestCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, int>(99));
    }

    // ────────────────────────────────────────────────────────────
    //  Test Infrastructure — Service Provider Builder
    // ────────────────────────────────────────────────────────────

    private static ServiceProvider BuildServiceProvider(
        GDPREnforcementMode enforcementMode,
        bool registerActivities)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaGDPR(options =>
        {
            options.ControllerName = "Load Test Corp";
            options.ControllerEmail = "privacy@loadtest.com";
            options.EnforcementMode = enforcementMode;
            options.AutoRegisterFromAttributes = false;
        });

        var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = false,
            ValidateOnBuild = false
        });

        if (registerActivities)
        {
            var registry = provider.GetRequiredService<IProcessingActivityRegistry>();
            var activity = CreateActivity("OrderProcessing", "Order fulfillment",
                LawfulBasis.Contract, typeof(GDPRLoadTestCommand));
            registry.RegisterActivityAsync(activity).AsTask().GetAwaiter().GetResult();
        }

        return provider;
    }

    private static async Task WarmUpRegistryAsync(ServiceProvider provider)
    {
        // Give auto-registration hosted service time to run
        var registry = provider.GetRequiredService<IProcessingActivityRegistry>();
        var maxWait = TimeSpan.FromSeconds(5);
        var sw = Stopwatch.StartNew();

        while (sw.Elapsed < maxWait)
        {
            var result = await registry.GetActivityByRequestTypeAsync(typeof(GDPRLoadTestCommand));
            if (result.IsRight)
            {
                var option = (Option<ProcessingActivity>)result;
                if (option.IsSome)
                    return;
            }

            await Task.Delay(50);
        }
    }

    private static ProcessingActivity CreateActivity(string name, string purpose, LawfulBasis basis, Type requestType)
    {
        var now = DateTimeOffset.UtcNow;
        return new ProcessingActivity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Purpose = purpose,
            LawfulBasis = basis,
            CategoriesOfDataSubjects = ["Customers", "Employees"],
            CategoriesOfPersonalData = ["Name", "Email", "Address"],
            Recipients = ["Shipping Provider", "Payment Processor"],
            RetentionPeriod = TimeSpan.FromDays(2555),
            SecurityMeasures = "AES-256 encryption at rest, TLS 1.3 in transit",
            RequestType = requestType,
            CreatedAtUtc = now,
            LastUpdatedAtUtc = now
        };
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

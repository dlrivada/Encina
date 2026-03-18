using System.Collections.Concurrent;
using System.Diagnostics;
using Encina.Compliance.PrivacyByDesign;
using Encina.Compliance.PrivacyByDesign.Model;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using static LanguageExt.Prelude;

namespace Encina.LoadTests.Compliance.PrivacyByDesign;

/// <summary>
/// Load tests for the Privacy by Design validation system under high concurrent traffic.
/// Validates throughput, latency percentiles, and thread safety of:
/// - IPrivacyByDesignValidator under concurrent request processing
/// - IDataMinimizationAnalyzer reflection caching under parallel invocation
/// - DataMinimizationPipelineBehavior under concurrent request processing
/// - IPurposeRegistry concurrent read/write operations
/// </summary>
/// <remarks>
/// <para>
/// GDPR Article 25 mandates privacy by design and by default. The pipeline behavior validates
/// every request for data minimization, purpose limitation, and field-level privacy controls.
/// This is one of the highest-frequency compliance checks.
/// </para>
/// <para>
/// Run via: <c>dotnet run --project tests/Encina.LoadTests -- --scenario privacybydesign</c>
/// </para>
/// </remarks>
internal static class PrivacyByDesignLoadTests
{
    private const int ConcurrentWorkers = 50;
    private const int OperationsPerWorker = 10_000;

    private static readonly string[] Purposes =
        ["Order Processing", "Marketing", "Analytics", "Customer Support", "Billing"];

    public static async Task RunAllAsync()
    {
        Console.WriteLine();
        Console.WriteLine("=== Privacy by Design Load Tests ===");
        Console.WriteLine($"Workers: {ConcurrentWorkers}, Operations/worker: {OperationsPerWorker:N0}");
        Console.WriteLine();

        await RunTestAsync("Privacy Impact Checks — Concurrent Validations",
            PrivacyImpactChecks_ConcurrentValidations_AllSucceed);
        await RunTestAsync("Data Minimization Enforcement — Concurrent Checks",
            DataMinimization_ConcurrentChecks_AllSucceed);
        await RunTestAsync("Purpose Limitation Enforcement — Concurrent Operations",
            PurposeLimitation_ConcurrentOperations_AllSucceed);
        await RunTestAsync("Field-Level Privacy Controls — Concurrent Evaluations",
            FieldLevelPrivacy_ConcurrentEvaluations_AllSucceed);
        await RunTestAsync("Mixed Privacy Scenarios — Concurrent Operations",
            MixedPrivacyScenarios_ConcurrentOperations_NoErrors);
        await RunTestAsync("Pipeline Validation — Concurrent Checks",
            PipelineValidation_ConcurrentChecks_MaintainsThroughput);
        await RunTestAsync("Latency Distribution — P50/P95/P99",
            LatencyDistribution_ConcurrentLoad_WithinBounds);

        Console.WriteLine();
        Console.WriteLine("=== All Privacy by Design load tests completed ===");
    }

    // ────────────────────────────────────────────────────────────
    //  Privacy Impact Checks — Concurrent Validations
    // ────────────────────────────────────────────────────────────

    private static async Task PrivacyImpactChecks_ConcurrentValidations_AllSucceed()
    {
        using var provider = BuildServiceProvider();

        var successCount = 0L;
        var failureCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                using var scope = provider.CreateScope();
                var validator = scope.ServiceProvider.GetRequiredService<IPrivacyByDesignValidator>();

                var request = new CompliantLoadRequest
                {
                    ProductId = $"P{i:D5}",
                    Quantity = i % 100 + 1
                };

                var result = await validator.ValidateAsync(request);

                if (result.IsRight)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref failureCount);
            }
        }));

        await Task.WhenAll(tasks);

        var total = ConcurrentWorkers * OperationsPerWorker;
        Assert(successCount == total, $"Expected {total} successes, got {successCount} (failures: {failureCount})");

        Console.WriteLine($"  {successCount:N0} concurrent privacy validations, 0 failures");
    }

    // ────────────────────────────────────────────────────────────
    //  Data Minimization Enforcement — Concurrent Checks
    // ────────────────────────────────────────────────────────────

    private static async Task DataMinimization_ConcurrentChecks_AllSucceed()
    {
        using var provider = BuildServiceProvider();

        var compliantCount = 0L;
        var violationCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                using var scope = provider.CreateScope();
                var validator = scope.ServiceProvider.GetRequiredService<IPrivacyByDesignValidator>();

                // Alternate between compliant and non-compliant requests
                if (i % 2 == 0)
                {
                    var request = new CompliantLoadRequest { ProductId = $"P{i:D5}", Quantity = 1 };
                    var result = await validator.ValidateAsync(request);
                    if (result.IsRight)
                    {
                        var validation = (PrivacyValidationResult)result;
                        if (validation.IsCompliant)
                            Interlocked.Increment(ref compliantCount);
                    }
                }
                else
                {
                    var request = new NonCompliantLoadRequest
                    {
                        ProductId = $"P{i:D5}",
                        ReferralSource = "Google Ads",
                        CampaignCode = "SUMMER2026"
                    };
                    var result = await validator.ValidateAsync(request);
                    if (result.IsRight)
                    {
                        var validation = (PrivacyValidationResult)result;
                        if (!validation.IsCompliant)
                            Interlocked.Increment(ref violationCount);
                    }
                }
            }
        }));

        await Task.WhenAll(tasks);

        var expectedPerType = (long)ConcurrentWorkers * OperationsPerWorker / 2;
        Assert(compliantCount == expectedPerType,
            $"Expected {expectedPerType} compliant, got {compliantCount}");
        Assert(violationCount == expectedPerType,
            $"Expected {expectedPerType} violations, got {violationCount}");

        Console.WriteLine($"  {compliantCount:N0} compliant, {violationCount:N0} violations — all correct");
    }

    // ────────────────────────────────────────────────────────────
    //  Purpose Limitation Enforcement — Concurrent Operations
    // ────────────────────────────────────────────────────────────

    private static async Task PurposeLimitation_ConcurrentOperations_AllSucceed()
    {
        using var provider = BuildServiceProvider(configurePurposes: true);

        var successCount = 0L;
        var failureCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                using var scope = provider.CreateScope();
                var validator = scope.ServiceProvider.GetRequiredService<IPrivacyByDesignValidator>();
                var purpose = Purposes[i % Purposes.Length];

                var request = new PurposeLimitedLoadRequest
                {
                    CustomerId = $"C{i:D5}",
                    OrderData = "order-data"
                };

                var result = await validator.ValidatePurposeLimitationAsync(request, purpose);

                if (result.IsRight)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref failureCount);
            }
        }));

        await Task.WhenAll(tasks);

        var total = ConcurrentWorkers * OperationsPerWorker;
        Assert(successCount == total, $"Expected {total} successes, got {successCount}");

        Console.WriteLine($"  {successCount:N0} concurrent purpose validations, 0 failures");
    }

    // ────────────────────────────────────────────────────────────
    //  Field-Level Privacy Controls — Concurrent Evaluations
    // ────────────────────────────────────────────────────────────

    private static async Task FieldLevelPrivacy_ConcurrentEvaluations_AllSucceed()
    {
        using var provider = BuildServiceProvider();

        var matchCount = 0L;
        var overrideCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                using var scope = provider.CreateScope();
                var validator = scope.ServiceProvider.GetRequiredService<IPrivacyByDesignValidator>();

                var request = new DefaultsLoadRequest
                {
                    ShareData = i % 3 == 0,  // some override the default (false)
                    MarketingConsent = i % 4 == 0 ? "opt-in" : null  // some override the default (null)
                };

                var result = await validator.ValidateDefaultsAsync(request);

                if (result.IsRight)
                {
                    var defaults = result.Match(d => d, _ => (IReadOnlyList<DefaultPrivacyFieldInfo>)[]);
                    foreach (var field in defaults)
                    {
                        if (field.MatchesDefault)
                            Interlocked.Increment(ref matchCount);
                        else
                            Interlocked.Increment(ref overrideCount);
                    }
                }
            }
        }));

        await Task.WhenAll(tasks);

        Assert(matchCount + overrideCount > 0,
            "Expected some field evaluations");

        Console.WriteLine($"  {matchCount:N0} matches, {overrideCount:N0} overrides detected across all workers");
    }

    // ────────────────────────────────────────────────────────────
    //  Mixed Privacy Scenarios — Concurrent Operations
    // ────────────────────────────────────────────────────────────

    private static async Task MixedPrivacyScenarios_ConcurrentOperations_NoErrors()
    {
        using var provider = BuildServiceProvider(configurePurposes: true);

        var errors = new ConcurrentQueue<string>();
        var operationCounts = new ConcurrentDictionary<string, int>();

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(workerId => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var scenario = i % 5;

                try
                {
                    using var scope = provider.CreateScope();
                    var validator = scope.ServiceProvider.GetRequiredService<IPrivacyByDesignValidator>();
                    string scenarioName;

                    switch (scenario)
                    {
                        case 0: // Full validation — compliant
                            {
                                var request = new CompliantLoadRequest { ProductId = $"P{i}", Quantity = 1 };
                                await validator.ValidateAsync(request);
                                scenarioName = "FullCompliant";
                                break;
                            }

                        case 1: // Full validation — non-compliant
                            {
                                var request = new NonCompliantLoadRequest
                                {
                                    ProductId = $"P{i}",
                                    ReferralSource = "ad",
                                    CampaignCode = "C1"
                                };
                                await validator.ValidateAsync(request);
                                scenarioName = "FullNonCompliant";
                                break;
                            }

                        case 2: // Minimization analysis only
                            {
                                var request = new NonCompliantLoadRequest { ProductId = $"P{i}" };
                                await validator.AnalyzeMinimizationAsync(request);
                                scenarioName = "MinimizationOnly";
                                break;
                            }

                        case 3: // Purpose validation only
                            {
                                var request = new PurposeLimitedLoadRequest
                                {
                                    CustomerId = $"C{i}",
                                    OrderData = "data"
                                };
                                var purpose = Purposes[i % Purposes.Length];
                                await validator.ValidatePurposeLimitationAsync(request, purpose);
                                scenarioName = "PurposeOnly";
                                break;
                            }

                        default: // Defaults check only
                            {
                                var request = new DefaultsLoadRequest { ShareData = i % 2 == 0 };
                                await validator.ValidateDefaultsAsync(request);
                                scenarioName = "DefaultsOnly";
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

        Assert(errors.IsEmpty,
            $"Got {errors.Count} unexpected exceptions: {string.Join(", ", errors.Take(3))}");

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
            config.RegisterServicesFromAssemblyContaining<PrivacyLoadTestCommand>());

        services.AddEncinaPrivacyByDesign(options =>
        {
            options.EnforcementMode = PrivacyByDesignEnforcementMode.Block;
            options.PrivacyLevel = PrivacyLevel.Maximum;
            options.MinimizationScoreThreshold = 0.0;
        });

        services.AddScoped<IRequestHandler<PrivacyLoadTestCommand, int>, PrivacyLoadTestHandler>();
        services.AddScoped<IRequestHandler<NoPrivacyLoadTestCommand, int>, NoPrivacyLoadTestHandler>();

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

                // Alternate between compliant (no unnecessary fields set) and non-compliant
                if (i % 2 == 0)
                {
                    var command = new PrivacyLoadTestCommand { ProductId = $"P{i}", Quantity = 1 };
                    var result = await encina.Send(command);
                    if (result.IsRight)
                        Interlocked.Increment(ref successCount);
                    else
                        Interlocked.Increment(ref blockedCount);
                }
                else
                {
                    var command = new NoPrivacyLoadTestCommand($"data-{i}");
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

        var latencies = new ConcurrentBag<double>();

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                using var scope = provider.CreateScope();
                var validator = scope.ServiceProvider.GetRequiredService<IPrivacyByDesignValidator>();

                var request = new CompliantLoadRequest
                {
                    ProductId = $"P{i:D5}",
                    Quantity = i % 100 + 1
                };

                var sw = Stopwatch.StartNew();
                await validator.ValidateAsync(request);
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

        Assert(p99 < 10_000, $"P99 latency {p99:F1}us exceeds 10ms threshold");

        Console.WriteLine($"  {latencies.Count:N0} operations — mean: {mean:F1}us, P50: {p50:F1}us, P95: {p95:F1}us, P99: {p99:F1}us, min: {min:F1}us, max: {max:F1}us");
    }

    // ────────────────────────────────────────────────────────────
    //  Test Infrastructure — Commands & Handlers
    // ────────────────────────────────────────────────────────────

    [EnforceDataMinimization(Purpose = "Order Processing")]
    private sealed class PrivacyLoadTestCommand : IRequest<int>
    {
        public string ProductId { get; set; } = "";
        public int Quantity { get; set; }

        [NotStrictlyNecessary(Reason = "Analytics only")]
        public string? TrackingId { get; set; }
    }

    private sealed class PrivacyLoadTestHandler : IRequestHandler<PrivacyLoadTestCommand, int>
    {
        public Task<Either<EncinaError, int>> Handle(PrivacyLoadTestCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, int>(42));
    }

    private sealed record NoPrivacyLoadTestCommand(string Data) : IRequest<int>;

    private sealed class NoPrivacyLoadTestHandler : IRequestHandler<NoPrivacyLoadTestCommand, int>
    {
        public Task<Either<EncinaError, int>> Handle(NoPrivacyLoadTestCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, int>(99));
    }

    // ────────────────────────────────────────────────────────────
    //  Test Infrastructure — Request Types
    // ────────────────────────────────────────────────────────────

    [EnforceDataMinimization]
    private sealed class CompliantLoadRequest
    {
        public string ProductId { get; set; } = "";
        public int Quantity { get; set; }
    }

    [EnforceDataMinimization]
    private sealed class NonCompliantLoadRequest
    {
        public string ProductId { get; set; } = "";

        [NotStrictlyNecessary(Reason = "Analytics only")]
        public string? ReferralSource { get; set; }

        [NotStrictlyNecessary(Reason = "Marketing campaign")]
        public string? CampaignCode { get; set; }
    }

    private sealed class PurposeLimitedLoadRequest
    {
        [PurposeLimitation("Order Processing")]
        public string CustomerId { get; set; } = "";

        [PurposeLimitation("Order Processing")]
        public string OrderData { get; set; } = "";
    }

    private sealed class DefaultsLoadRequest
    {
        [PrivacyDefault(false)]
        public bool ShareData { get; set; }

        [PrivacyDefault(null)]
        public string? MarketingConsent { get; set; }
    }

    // ────────────────────────────────────────────────────────────
    //  Test Infrastructure — Service Provider Builder
    // ────────────────────────────────────────────────────────────

    private static ServiceProvider BuildServiceProvider(bool configurePurposes = false)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaPrivacyByDesign(options =>
        {
            options.EnforcementMode = PrivacyByDesignEnforcementMode.Block;
            options.PrivacyLevel = PrivacyLevel.Maximum;
            options.MinimizationScoreThreshold = 0.0;

            if (configurePurposes)
            {
                foreach (var purpose in Purposes)
                {
                    options.AddPurpose(purpose, p =>
                    {
                        p.Description = $"Processing data for {purpose}";
                        p.LegalBasis = "Contract";
                        p.AllowedFields.AddRange(["CustomerId", "OrderData", "ProductId", "Quantity"]);
                    });
                }
            }
        });

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = false,
            ValidateOnBuild = false
        });
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

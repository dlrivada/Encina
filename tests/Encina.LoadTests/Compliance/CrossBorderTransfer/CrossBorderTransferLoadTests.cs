using System.Collections.Concurrent;
using System.Diagnostics;
using Encina.Compliance.CrossBorderTransfer;
using Encina.Compliance.CrossBorderTransfer.Abstractions;
using Encina.Compliance.CrossBorderTransfer.Attributes;
using Encina.Compliance.CrossBorderTransfer.Model;
using Encina.Compliance.CrossBorderTransfer.ReadModels;
using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.LoadTests.Compliance.CrossBorderTransfer;

/// <summary>
/// Load tests for the cross-border transfer validation system under high concurrent traffic.
/// Validates throughput, latency percentiles, and thread safety of:
/// - ITransferValidator cascading validation chain (adequacy → approved → SCC → TIA → block)
/// - TransferBlockingPipelineBehavior under concurrent request processing
/// - ITIARiskAssessor risk scoring under parallel invocation
/// </summary>
/// <remarks>
/// <para>
/// Cross-border transfer validation is legally mandatory (GDPR Chapter V, Articles 44-49).
/// Every request involving international data transfer MUST pass through the validation
/// pipeline — this makes it a hot-path operation requiring load testing.
/// </para>
/// <para>
/// Run via: <c>dotnet run --project tests/Encina.LoadTests -- --scenario crossborder</c>
/// </para>
/// </remarks>
internal static class CrossBorderTransferLoadTests
{
    private const int ConcurrentWorkers = 50;
    private const int OperationsPerWorker = 10_000;

    private static readonly string[] AdequateCountries = ["JP", "GB", "CH", "NZ", "KR"];
    private static readonly string[] ProcessorIds = ["processor-1", "processor-2", "processor-3"];
    private static readonly string[] RiskAssessorCountries = ["US", "CN", "RU", "BR", "IN", "JP", "GB", "SA", "KR", "AU"];
    private static readonly string[] DataCategories = ["personal-data", "health-data", "financial-data", "biometric-data"];
    private static readonly string[] LatencyCountries = ["JP", "US", "CN", "GB", "BR"];

    public static async Task RunAllAsync()
    {
        Console.WriteLine();
        Console.WriteLine("=== Cross-Border Transfer Validation Load Tests ===");
        Console.WriteLine($"Workers: {ConcurrentWorkers}, Operations/worker: {OperationsPerWorker:N0}");
        Console.WriteLine();

        await RunTestAsync("Validator — Adequacy Decision (Fast Path)",
            Validator_AdequacyDecision_ConcurrentReads_MaintainsThroughput);
        await RunTestAsync("Validator — SCC Validation (Mid Path)",
            Validator_SCCValidation_ConcurrentReads_MaintainsThroughput);
        await RunTestAsync("Validator — Full Cascade to Block",
            Validator_FullCascadeToBlock_ConcurrentReads_MaintainsThroughput);
        await RunTestAsync("Risk Assessor — Concurrent Assessments",
            RiskAssessor_ConcurrentAssessments_AllSucceed);
        await RunTestAsync("Mixed Validation Scenarios — Concurrent",
            MixedValidation_ConcurrentOperations_NoErrors);
        await RunTestAsync("Pipeline Validation — Concurrent Checks",
            PipelineValidation_ConcurrentChecks_MaintainsThroughput);
        await RunTestAsync("Latency Distribution — P50/P95/P99",
            LatencyDistribution_ConcurrentLoad_WithinBounds);

        Console.WriteLine();
        Console.WriteLine("=== All cross-border transfer load tests completed ===");
    }

    // ────────────────────────────────────────────────────────────
    //  Validator — Adequacy Decision (Fast Path)
    // ────────────────────────────────────────────────────────────

    private static async Task Validator_AdequacyDecision_ConcurrentReads_MaintainsThroughput()
    {
        using var provider = BuildServiceProvider(
            adequateCountries: ["JP", "GB", "CH", "NZ", "KR"]);
        var validator = provider.GetRequiredService<ITransferValidator>();

        var successCount = 0L;
        var failureCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var request = new TransferRequest
                {
                    SourceCountryCode = "DE",
                    DestinationCountryCode = AdequateCountries[i % AdequateCountries.Length],
                    DataCategory = "personal-data"
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

        Console.WriteLine($"  {successCount:N0} concurrent adequacy validations, 0 failures");
    }

    // ────────────────────────────────────────────────────────────
    //  Validator — SCC Validation (Mid Path)
    // ────────────────────────────────────────────────────────────

    private static async Task Validator_SCCValidation_ConcurrentReads_MaintainsThroughput()
    {
        using var provider = BuildServiceProvider(
            adequateCountries: [],
            sccProcessors: ["processor-1", "processor-2", "processor-3"]);
        var validator = provider.GetRequiredService<ITransferValidator>();

        var successCount = 0L;
        var failureCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var request = new TransferRequest
                {
                    SourceCountryCode = "DE",
                    DestinationCountryCode = "US",
                    DataCategory = "personal-data",
                    ProcessorId = ProcessorIds[i % ProcessorIds.Length]
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
        Assert(successCount == total, $"Expected {total} successes, got {successCount}");

        Console.WriteLine($"  {successCount:N0} concurrent SCC validations, 0 failures");
    }

    // ────────────────────────────────────────────────────────────
    //  Validator — Full Cascade to Block
    // ────────────────────────────────────────────────────────────

    private static async Task Validator_FullCascadeToBlock_ConcurrentReads_MaintainsThroughput()
    {
        using var provider = BuildServiceProvider(
            adequateCountries: [],
            approvedRoutes: [],
            sccProcessors: [],
            tiaRoutes: []);
        var validator = provider.GetRequiredService<ITransferValidator>();

        var blockedCount = 0L;
        var errorCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var request = new TransferRequest
                {
                    SourceCountryCode = "DE",
                    DestinationCountryCode = "CN",
                    DataCategory = "health-data"
                };

                var result = await validator.ValidateAsync(request);

                result.Match(
                    Left: _ => Interlocked.Increment(ref errorCount),
                    Right: outcome =>
                    {
                        if (!outcome.IsAllowed)
                            Interlocked.Increment(ref blockedCount);
                    });
            }
        }));

        await Task.WhenAll(tasks);

        var total = ConcurrentWorkers * OperationsPerWorker;
        Assert(blockedCount == total, $"Expected {total} blocked, got {blockedCount}");
        Assert(errorCount == 0, $"Expected 0 errors, got {errorCount}");

        Console.WriteLine($"  {blockedCount:N0} concurrent full-cascade blocks, 0 errors");
    }

    // ────────────────────────────────────────────────────────────
    //  Risk Assessor — Concurrent Assessments
    // ────────────────────────────────────────────────────────────

    private static async Task RiskAssessor_ConcurrentAssessments_AllSucceed()
    {
        using var provider = BuildServiceProvider();
        var assessor = provider.GetRequiredService<ITIARiskAssessor>();

        var successCount = 0L;
        var failureCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var country = RiskAssessorCountries[i % RiskAssessorCountries.Length];
                var category = DataCategories[i % DataCategories.Length];

                var result = await assessor.AssessRiskAsync(country, category);

                if (result.IsRight)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref failureCount);
            }
        }));

        await Task.WhenAll(tasks);

        var total = ConcurrentWorkers * OperationsPerWorker;
        Assert(successCount == total, $"Expected {total} successes, got {successCount}");

        Console.WriteLine($"  {successCount:N0} concurrent risk assessments, 0 failures");
    }

    // ────────────────────────────────────────────────────────────
    //  Mixed Validation Scenarios — Concurrent
    // ────────────────────────────────────────────────────────────

    private static async Task MixedValidation_ConcurrentOperations_NoErrors()
    {
        using var provider = BuildServiceProvider(
            adequateCountries: ["JP", "GB"],
            approvedRoutes: [("DE", "US", "personal-data")],
            sccProcessors: ["proc-1"],
            tiaRoutes: [("DE", "BR", "financial-data")]);
        var validator = provider.GetRequiredService<ITransferValidator>();

        var errors = new ConcurrentQueue<string>();
        var operationCounts = new ConcurrentDictionary<string, int>();

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(workerId => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var scenario = i % 5;

                try
                {
                    TransferRequest request;
                    string scenarioName;

                    switch (scenario)
                    {
                        case 0: // Adequacy fast-path
                            request = new TransferRequest
                            {
                                SourceCountryCode = "DE",
                                DestinationCountryCode = "JP",
                                DataCategory = "personal-data"
                            };
                            scenarioName = "Adequacy";
                            break;

                        case 1: // Approved transfer
                            request = new TransferRequest
                            {
                                SourceCountryCode = "DE",
                                DestinationCountryCode = "US",
                                DataCategory = "personal-data"
                            };
                            scenarioName = "Approved";
                            break;

                        case 2: // SCC path
                            request = new TransferRequest
                            {
                                SourceCountryCode = "DE",
                                DestinationCountryCode = "IN",
                                DataCategory = "personal-data",
                                ProcessorId = "proc-1"
                            };
                            scenarioName = "SCC";
                            break;

                        case 3: // TIA path
                            request = new TransferRequest
                            {
                                SourceCountryCode = "DE",
                                DestinationCountryCode = "BR",
                                DataCategory = "financial-data"
                            };
                            scenarioName = "TIA";
                            break;

                        default: // Block path
                            request = new TransferRequest
                            {
                                SourceCountryCode = "DE",
                                DestinationCountryCode = "CN",
                                DataCategory = "health-data"
                            };
                            scenarioName = "Block";
                            break;
                    }

                    await validator.ValidateAsync(request);
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
            config.RegisterServicesFromAssemblyContaining<TransferLoadTestCommand>());

        RegisterMockedServices(services,
            adequateCountries: ["JP", "GB"],
            approvedRoutes: [("DE", "US", "personal-data")]);

        services.AddEncinaCrossBorderTransfer(options =>
        {
            options.EnforcementMode = CrossBorderTransferEnforcementMode.Block;
        });

        services.AddScoped<IRequestHandler<TransferLoadTestCommand, int>, TransferLoadTestHandler>();
        services.AddScoped<IRequestHandler<NoTransferLoadTestCommand, int>, NoTransferLoadTestHandler>();

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

                var destination = i % 2 == 0 ? "JP" : "CN";
                var command = new TransferLoadTestCommand(destination);
                var result = await encina.Send(command);

                if (result.IsRight)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref blockedCount);
            }
        }));

        await Task.WhenAll(tasks);

        var total = successCount + blockedCount;
        Assert(total == ConcurrentWorkers * 1000L,
            $"Expected {ConcurrentWorkers * 1000} total operations, got {total}");
        Assert(successCount > 0, "Expected some successful operations (adequacy path)");
        Assert(blockedCount > 0, "Expected some blocked operations (non-adequate path)");

        Console.WriteLine($"  {total:N0} pipeline validations: {successCount:N0} passed, {blockedCount:N0} blocked");
    }

    // ────────────────────────────────────────────────────────────
    //  Latency Distribution — P50/P95/P99
    // ────────────────────────────────────────────────────────────

    private static async Task LatencyDistribution_ConcurrentLoad_WithinBounds()
    {
        using var provider = BuildServiceProvider(
            adequateCountries: ["JP", "GB", "CH"],
            approvedRoutes: [("DE", "US", "personal-data")]);
        var validator = provider.GetRequiredService<ITransferValidator>();

        var latencies = new ConcurrentBag<double>();

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var request = new TransferRequest
                {
                    SourceCountryCode = "DE",
                    DestinationCountryCode = LatencyCountries[i % LatencyCountries.Length],
                    DataCategory = "personal-data"
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

        Assert(p99 < 10_000, $"P99 latency {p99:F1}µs exceeds 10ms threshold");

        Console.WriteLine($"  {latencies.Count:N0} operations — mean: {mean:F1}µs, P50: {p50:F1}µs, P95: {p95:F1}µs, P99: {p99:F1}µs, min: {min:F1}µs, max: {max:F1}µs");
    }

    // ────────────────────────────────────────────────────────────
    //  Test Infrastructure — Commands & Handlers
    // ────────────────────────────────────────────────────────────

    [RequiresCrossBorderTransfer(DestinationProperty = nameof(DestinationCountryCode), DataCategory = "personal-data")]
    private sealed record TransferLoadTestCommand(string DestinationCountryCode) : IRequest<int>;

    private sealed class TransferLoadTestHandler : IRequestHandler<TransferLoadTestCommand, int>
    {
        public Task<Either<EncinaError, int>> Handle(TransferLoadTestCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, int>(42));
    }

    private sealed record NoTransferLoadTestCommand(string Data) : IRequest<int>;

    private sealed class NoTransferLoadTestHandler : IRequestHandler<NoTransferLoadTestCommand, int>
    {
        public Task<Either<EncinaError, int>> Handle(NoTransferLoadTestCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, int>(99));
    }

    // ────────────────────────────────────────────────────────────
    //  Test Infrastructure — Service Provider Builder
    // ────────────────────────────────────────────────────────────

    private static ServiceProvider BuildServiceProvider(
        string[]? adequateCountries = null,
        (string Source, string Dest, string Category)[]? approvedRoutes = null,
        string[]? sccProcessors = null,
        (string Source, string Dest, string Category)[]? tiaRoutes = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // Register mocked dependencies BEFORE AddEncinaCrossBorderTransfer (TryAdd respects pre-registration)
        RegisterMockedServices(services, adequateCountries, approvedRoutes, sccProcessors, tiaRoutes);

        services.AddEncinaCrossBorderTransfer(options =>
        {
            options.EnforcementMode = CrossBorderTransferEnforcementMode.Block;
            options.DefaultSourceCountryCode = "DE";
            options.TIARiskThreshold = 0.6;
        });

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = false,
            ValidateOnBuild = false
        });
    }

    private static void RegisterMockedServices(
        IServiceCollection services,
        string[]? adequateCountries = null,
        (string Source, string Dest, string Category)[]? approvedRoutes = null,
        string[]? sccProcessors = null,
        (string Source, string Dest, string Category)[]? tiaRoutes = null)
    {
        // IAdequacyDecisionProvider — synchronous check against known adequate countries
        var adequacyProvider = Substitute.For<IAdequacyDecisionProvider>();
        adequacyProvider.HasAdequacy(Arg.Any<Region>())
            .Returns(callInfo =>
            {
                var region = callInfo.Arg<Region>();
                return adequateCountries?.Contains(region.Code) == true;
            });
        services.AddSingleton(adequacyProvider);

        // IApprovedTransferService — checks if a transfer route is pre-approved
        var approvedService = Substitute.For<IApprovedTransferService>();
#pragma warning disable CA2012 // ValueTask instances returned from NSubstitute mock setups are consumed by the framework
        approvedService.IsTransferApprovedAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var source = callInfo.ArgAt<string>(0);
                var dest = callInfo.ArgAt<string>(1);
                var category = callInfo.ArgAt<string>(2);
                var isApproved = approvedRoutes?.Any(t =>
                    t.Source == source && t.Dest == dest && t.Category == category) == true;

                return new ValueTask<Either<EncinaError, bool>>(
                    Right<EncinaError, bool>(isApproved));
            });
        services.AddScoped<IApprovedTransferService>(_ => approvedService);

        // ISCCService — validates SCC agreement for a processor
        var sccService = Substitute.For<ISCCService>();
        sccService.ValidateAgreementAsync(
                Arg.Any<string>(), Arg.Any<SCCModule>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var processorId = callInfo.ArgAt<string>(0);
                var isValid = sccProcessors?.Contains(processorId) == true;

                return new ValueTask<Either<EncinaError, SCCValidationResult>>(
                    Right<EncinaError, SCCValidationResult>(new SCCValidationResult
                    {
                        IsValid = isValid,
                        AgreementId = isValid ? Guid.NewGuid() : null,
                        Module = isValid ? SCCModule.ControllerToProcessor : null,
                        Version = isValid ? "2021/914" : null,
                        MissingMeasures = [],
                        Issues = isValid ? [] : ["No SCC agreement found"]
                    }));
            });
        services.AddScoped<ISCCService>(_ => sccService);

        // ITIAService — checks for completed TIA by route
        var tiaService = Substitute.For<ITIAService>();
        tiaService.GetTIAByRouteAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var source = callInfo.ArgAt<string>(0);
                var dest = callInfo.ArgAt<string>(1);
                var category = callInfo.ArgAt<string>(2);
                var match = tiaRoutes?.Any(t =>
                    t.Source == source && t.Dest == dest && t.Category == category) == true;

                if (match)
                {
                    return new ValueTask<Either<EncinaError, TIAReadModel>>(
                        Right<EncinaError, TIAReadModel>(new TIAReadModel
                        {
                            Id = Guid.NewGuid(),
                            SourceCountryCode = source,
                            DestinationCountryCode = dest,
                            DataCategory = category,
                            Status = TIAStatus.Completed,
                            RiskScore = 0.45,
                            CreatedAtUtc = DateTimeOffset.UtcNow,
                            LastModifiedAtUtc = DateTimeOffset.UtcNow,
                            RequiredSupplementaryMeasures = []
                        }));
                }

                return new ValueTask<Either<EncinaError, TIAReadModel>>(
                    Left<EncinaError, TIAReadModel>(EncinaError.New("TIA not found")));
            });
#pragma warning restore CA2012
        services.AddScoped<ITIAService>(_ => tiaService);
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

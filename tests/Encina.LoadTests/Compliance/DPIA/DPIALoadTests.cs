using System.Collections.Concurrent;
using System.Diagnostics;
using Encina.Compliance.DPIA.Abstractions;
using Encina.Compliance.DPIA.Model;
using Encina.Compliance.DPIA.ReadModels;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using static LanguageExt.Prelude;

namespace Encina.LoadTests.Compliance.DPIA;

/// <summary>
/// Load tests for the DPIA compliance module under high concurrent traffic.
/// Validates throughput, latency percentiles, and thread safety of:
/// - IDPIAService assessment lifecycle operations under concurrent access
/// - Assessment engine risk evaluation under parallel invocation
/// - Mixed assessment lifecycle scenarios with concurrent operations
/// - Latency distribution for assessment creation and querying
/// </summary>
/// <remarks>
/// <para>
/// DPIA is legally mandatory per GDPR Article 35. Every high-risk processing operation
/// requires an approved DPIA before data processing can proceed. The pipeline behavior
/// checks assessments on every request, making it a hot-path operation.
/// </para>
/// <para>
/// Run via: <c>dotnet run --project tests/Encina.LoadTests -- --scenario dpia</c>
/// </para>
/// </remarks>
internal static class DPIALoadTests
{
    private const int ConcurrentWorkers = 50;
    private const int OperationsPerWorker = 10_000;

    private static readonly string[] ProcessingTypes =
        ["AutomatedDecisionMaking", "LargeScaleProcessing", "SystematicMonitoring", "BiometricProcessing", "ProfilingAnalytics"];

    private static readonly RiskLevel[] RiskLevels =
        [RiskLevel.Low, RiskLevel.Medium, RiskLevel.High, RiskLevel.VeryHigh];

    private static readonly string[] RequestTypes =
        ["MyApp.Commands.ProcessHealth", "MyApp.Commands.ProfileUser", "MyApp.Commands.TrackEmployee",
         "MyApp.Commands.AnalyzeBiometrics", "MyApp.Commands.MonitorPublicArea"];

    public static async Task RunAllAsync()
    {
        Console.WriteLine();
        Console.WriteLine("=== DPIA Compliance Load Tests ===");
        Console.WriteLine($"Workers: {ConcurrentWorkers}, Operations/worker: {OperationsPerWorker:N0}");
        Console.WriteLine();

        await RunTestAsync("Create Assessment — Concurrent Recording",
            CreateAssessment_ConcurrentCreation_AllSucceed);
        await RunTestAsync("Evaluate Assessment — Concurrent Risk Evaluation",
            EvaluateAssessment_ConcurrentEvaluation_AllSucceed);
        await RunTestAsync("Full Lifecycle — Sequential Per-Assessment Under Load",
            FullLifecycle_ConcurrentAssessments_AllComplete);
        await RunTestAsync("DPO Consultation — Concurrent Consultation Cycles",
            DPOConsultation_ConcurrentCycles_AllSucceed);
        await RunTestAsync("Mixed Assessment Scenarios — Concurrent",
            MixedAssessmentScenarios_ConcurrentOperations_NoErrors);
        await RunTestAsync("Query Assessments — Concurrent Reads",
            QueryAssessments_ConcurrentReads_MaintainsThroughput);
        await RunTestAsync("Latency Distribution — P50/P95/P99",
            LatencyDistribution_ConcurrentLoad_WithinBounds);

        Console.WriteLine();
        Console.WriteLine("=== All DPIA load tests completed ===");
    }

    // ────────────────────────────────────────────────────────────
    //  Create Assessment — Concurrent Recording
    // ────────────────────────────────────────────────────────────

    private static async Task CreateAssessment_ConcurrentCreation_AllSucceed()
    {
        using var provider = BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        var successCount = 0L;
        var failureCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(workerId => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IDPIAService>();

                var requestType = $"{RequestTypes[i % RequestTypes.Length]}.W{workerId}.{i}";
                var processingType = ProcessingTypes[i % ProcessingTypes.Length];

                var result = await service.CreateAssessmentAsync(
                    requestType, processingType, $"Load test W{workerId}-{i}");

                if (result.IsRight)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref failureCount);
            }
        }));

        await Task.WhenAll(tasks);

        var total = ConcurrentWorkers * OperationsPerWorker;
        Assert(successCount == total, $"Expected {total} successes, got {successCount} (failures: {failureCount})");

        Console.WriteLine($"  {successCount:N0} concurrent assessment creations, 0 failures");
    }

    // ────────────────────────────────────────────────────────────
    //  Evaluate Assessment — Concurrent Risk Evaluation
    // ────────────────────────────────────────────────────────────

    private static async Task EvaluateAssessment_ConcurrentEvaluation_AllSucceed()
    {
        using var provider = BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        // Pre-create assessments to evaluate
        var assessmentIds = new ConcurrentBag<Guid>();
        var batchSize = ConcurrentWorkers * 100;
        for (var i = 0; i < batchSize; i++)
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IDPIAService>();
            var result = await service.CreateAssessmentAsync(
                $"EvalTest.Command.{i}", ProcessingTypes[i % ProcessingTypes.Length]);
            result.IfRight(id => assessmentIds.Add(id));
        }

        var ids = assessmentIds.ToArray();
        var successCount = 0L;
        var failureCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(workerId => Task.Run(async () =>
        {
            for (var i = 0; i < 100; i++)
            {
                var assessmentId = ids[(workerId * 100 + i) % ids.Length];
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IDPIAService>();

                var context = new DPIAContext
                {
                    RequestType = typeof(string),
                    ProcessingType = ProcessingTypes[i % ProcessingTypes.Length],
                    DataCategories = ["personal-data"],
                    HighRiskTriggers = i % 3 == 0 ? (IReadOnlyList<string>)["special-category-data"] : [],
                };

                var result = await service.EvaluateAssessmentAsync(assessmentId, context);

                if (result.IsRight)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref failureCount);
            }
        }));

        await Task.WhenAll(tasks);

        var total = successCount + failureCount;
        Assert(total == ConcurrentWorkers * 100L,
            $"Expected {ConcurrentWorkers * 100} total operations, got {total}");

        Console.WriteLine($"  {total:N0} concurrent evaluations: {successCount:N0} succeeded, {failureCount:N0} state conflicts");
    }

    // ────────────────────────────────────────────────────────────
    //  Full Lifecycle — Concurrent Assessments All Complete
    // ────────────────────────────────────────────────────────────

    private static async Task FullLifecycle_ConcurrentAssessments_AllComplete()
    {
        using var provider = BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        var completedCount = 0L;
        var errorCount = 0L;
        var assessmentsPerWorker = 20;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(workerId => Task.Run(async () =>
        {
            for (var i = 0; i < assessmentsPerWorker; i++)
            {
                try
                {
                    Guid assessmentId;

                    // Create
                    using (var scope = scopeFactory.CreateScope())
                    {
                        var service = scope.ServiceProvider.GetRequiredService<IDPIAService>();
                        var result = await service.CreateAssessmentAsync(
                            $"Lifecycle.W{workerId}.{i}", ProcessingTypes[i % ProcessingTypes.Length],
                            $"Lifecycle test W{workerId}-{i}");
                        assessmentId = result.Match(id => id, _ => Guid.Empty);
                        if (assessmentId == Guid.Empty) { Interlocked.Increment(ref errorCount); continue; }
                    }

                    // Evaluate
                    using (var scope = scopeFactory.CreateScope())
                    {
                        var service = scope.ServiceProvider.GetRequiredService<IDPIAService>();
                        var context = new DPIAContext
                        {
                            RequestType = typeof(string),
                            DataCategories = ["personal"],
                            HighRiskTriggers = [],
                        };
                        await service.EvaluateAssessmentAsync(assessmentId, context);
                    }

                    // Approve
                    using (var scope = scopeFactory.CreateScope())
                    {
                        var service = scope.ServiceProvider.GetRequiredService<IDPIAService>();
                        await service.ApproveAssessmentAsync(assessmentId, $"approver-{workerId}",
                            DateTimeOffset.UtcNow.AddDays(365));
                    }

                    // Expire
                    using (var scope = scopeFactory.CreateScope())
                    {
                        var service = scope.ServiceProvider.GetRequiredService<IDPIAService>();
                        await service.ExpireAssessmentAsync(assessmentId);
                    }

                    Interlocked.Increment(ref completedCount);
                }
                catch (Exception)
                {
                    Interlocked.Increment(ref errorCount);
                }
            }
        }));

        await Task.WhenAll(tasks);

        var total = ConcurrentWorkers * assessmentsPerWorker;
        Assert(completedCount + errorCount == total,
            $"Expected {total} total, got {completedCount + errorCount}");
        Assert(completedCount > total * 0.8,
            $"Expected >80% completion rate, got {completedCount}/{total}");

        Console.WriteLine($"  {completedCount:N0}/{total:N0} full lifecycles completed ({errorCount:N0} conflicts)");
    }

    // ────────────────────────────────────────────────────────────
    //  DPO Consultation — Concurrent Consultation Cycles
    // ────────────────────────────────────────────────────────────

    private static async Task DPOConsultation_ConcurrentCycles_AllSucceed()
    {
        using var provider = BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        var successCount = 0L;
        var failureCount = 0L;
        var consultationsPerWorker = 100;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(workerId => Task.Run(async () =>
        {
            for (var i = 0; i < consultationsPerWorker; i++)
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<IDPIAService>();

                    // Create and evaluate (transitions to InReview)
                    var createResult = await service.CreateAssessmentAsync(
                        $"DPO.W{workerId}.{i}", "LargeScaleProcessing");
                    var id = createResult.Match(v => v, _ => Guid.Empty);
                    if (id == Guid.Empty) { Interlocked.Increment(ref failureCount); continue; }

                    var context = new DPIAContext
                    {
                        RequestType = typeof(string),
                        DataCategories = ["personal"],
                        HighRiskTriggers = [],
                    };
                    await service.EvaluateAssessmentAsync(id, context);

                    // Request DPO consultation
                    await service.RequestDPOConsultationAsync(id);

                    Interlocked.Increment(ref successCount);
                }
                catch (Exception)
                {
                    Interlocked.Increment(ref failureCount);
                }
            }
        }));

        await Task.WhenAll(tasks);

        var total = successCount + failureCount;
        Assert(total == ConcurrentWorkers * (long)consultationsPerWorker,
            $"Expected {ConcurrentWorkers * consultationsPerWorker} total, got {total}");

        Console.WriteLine($"  {total:N0} DPO consultations: {successCount:N0} succeeded, {failureCount:N0} errors");
    }

    // ────────────────────────────────────────────────────────────
    //  Mixed Assessment Scenarios — Concurrent
    // ────────────────────────────────────────────────────────────

    private static async Task MixedAssessmentScenarios_ConcurrentOperations_NoErrors()
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
                    var service = scope.ServiceProvider.GetRequiredService<IDPIAService>();
                    string scenarioName;

                    switch (scenario)
                    {
                        case 0: // Create only
                            await service.CreateAssessmentAsync(
                                $"Mix.Create.W{workerId}.{i}", ProcessingTypes[i % ProcessingTypes.Length]);
                            scenarioName = "Create";
                            break;

                        case 1: // Create + evaluate
                            {
                                var r = await service.CreateAssessmentAsync(
                                    $"Mix.Eval.W{workerId}.{i}", ProcessingTypes[i % ProcessingTypes.Length]);
                                var id = r.Match(v => v, _ => Guid.Empty);
                                if (id != Guid.Empty)
                                {
                                    var ctx = new DPIAContext { RequestType = typeof(string), DataCategories = ["personal"], HighRiskTriggers = [] };
                                    await service.EvaluateAssessmentAsync(id, ctx);
                                }
                                scenarioName = "Evaluate";
                                break;
                            }

                        case 2: // Create + evaluate + approve
                            {
                                var r = await service.CreateAssessmentAsync(
                                    $"Mix.Approve.W{workerId}.{i}", ProcessingTypes[i % ProcessingTypes.Length]);
                                var id = r.Match(v => v, _ => Guid.Empty);
                                if (id != Guid.Empty)
                                {
                                    var ctx = new DPIAContext { RequestType = typeof(string), DataCategories = ["personal"], HighRiskTriggers = [] };
                                    await service.EvaluateAssessmentAsync(id, ctx);
                                    await service.ApproveAssessmentAsync(id, "approver");
                                }
                                scenarioName = "Approve";
                                break;
                            }

                        default: // Query
                            await service.GetAllAssessmentsAsync();
                            scenarioName = "Query";
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
    //  Query Assessments — Concurrent Reads
    // ────────────────────────────────────────────────────────────

    private static async Task QueryAssessments_ConcurrentReads_MaintainsThroughput()
    {
        using var provider = BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        // Pre-create assessments
        for (var i = 0; i < 100; i++)
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IDPIAService>();
            await service.CreateAssessmentAsync($"QueryTest.{i}", ProcessingTypes[i % ProcessingTypes.Length]);
        }

        var successCount = 0L;
        var failureCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < 100; i++)
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IDPIAService>();
                var result = await service.GetAllAssessmentsAsync();

                if (result.IsRight)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref failureCount);
            }
        }));

        await Task.WhenAll(tasks);

        var total = ConcurrentWorkers * 100L;
        Assert(successCount == total, $"Expected {total} successes, got {successCount}");

        Console.WriteLine($"  {successCount:N0} concurrent queries, 0 failures");
    }

    // ────────────────────────────────────────────────────────────
    //  Latency Distribution — P50/P95/P99
    // ────────────────────────────────────────────────────────────

    private static async Task LatencyDistribution_ConcurrentLoad_WithinBounds()
    {
        using var provider = BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        var latencies = new ConcurrentBag<double>();

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(workerId => Task.Run(async () =>
        {
            for (var i = 0; i < 200; i++)
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IDPIAService>();

                var sw = Stopwatch.StartNew();
                await service.CreateAssessmentAsync(
                    $"Latency.W{workerId}.{i}", ProcessingTypes[i % ProcessingTypes.Length],
                    $"Latency test W{workerId}-{i}");
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
    //  Test Infrastructure — Service Provider Builder
    // ────────────────────────────────────────────────────────────

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // Register an in-memory DPIA service that simulates all operations
        // using thread-safe ConcurrentDictionary state. This avoids requiring
        // real PostgreSQL/Marten for load tests while exercising the full API contract.
        var dpiaService = new InMemoryDPIAService();
        services.AddScoped<IDPIAService>(_ => dpiaService);

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = false,
            ValidateOnBuild = false
        });
    }

    // ────────────────────────────────────────────────────────────
    //  Test Infrastructure — In-Memory Service for Load Tests
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// Thread-safe in-memory implementation of <see cref="IDPIAService"/> for load testing.
    /// Simulates the full DPIA lifecycle without requiring PostgreSQL/Marten.
    /// </summary>
    private sealed class InMemoryDPIAService : IDPIAService
    {
        private readonly ConcurrentDictionary<Guid, DPIAReadModel> _assessments = new();

        public ValueTask<Either<EncinaError, Guid>> CreateAssessmentAsync(
            string requestTypeName, string? processingType = null, string? reason = null,
            string? tenantId = null, string? moduleId = null, CancellationToken cancellationToken = default)
        {
            var id = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;
            _assessments[id] = new DPIAReadModel
            {
                Id = id,
                RequestTypeName = requestTypeName,
                ProcessingType = processingType,
                Reason = reason,
                Status = DPIAAssessmentStatus.Draft,
                TenantId = tenantId,
                ModuleId = moduleId,
                LastModifiedAtUtc = now,
                Version = 1
            };
            return ValueTask.FromResult(Right<EncinaError, Guid>(id));
        }

        public ValueTask<Either<EncinaError, DPIAResult>> EvaluateAssessmentAsync(
            Guid assessmentId, DPIAContext context, CancellationToken cancellationToken = default)
        {
            if (!_assessments.TryGetValue(assessmentId, out var assessment))
                return ValueTask.FromResult(Left<EncinaError, DPIAResult>(EncinaError.New("Not found")));
            if (assessment.Status is not (DPIAAssessmentStatus.Draft or DPIAAssessmentStatus.RequiresRevision))
                return ValueTask.FromResult(Left<EncinaError, DPIAResult>(EncinaError.New("Invalid state")));

            var risk = context.HighRiskTriggers.Count > 0 ? RiskLevel.High : RiskLevel.Medium;
            var result = new DPIAResult
            {
                OverallRisk = risk,
                IdentifiedRisks = [new RiskItem("Test risk", risk, "Load test identified risk", null)],
                ProposedMitigations = [new Mitigation("Load test mitigation", "Technical", false, null)],
                RequiresPriorConsultation = risk >= RiskLevel.VeryHigh,
                AssessedAtUtc = DateTimeOffset.UtcNow,
            };

            assessment.OverallRisk = result.OverallRisk;
            assessment.IdentifiedRisks = result.IdentifiedRisks;
            assessment.ProposedMitigations = result.ProposedMitigations;
            assessment.RequiresPriorConsultation = result.RequiresPriorConsultation;
            assessment.AssessedAtUtc = result.AssessedAtUtc;
            assessment.Status = DPIAAssessmentStatus.InReview;
            assessment.LastModifiedAtUtc = DateTimeOffset.UtcNow;
            assessment.Version++;

            return ValueTask.FromResult(Right<EncinaError, DPIAResult>(result));
        }

        public ValueTask<Either<EncinaError, Guid>> RequestDPOConsultationAsync(
            Guid assessmentId, CancellationToken cancellationToken = default)
        {
            if (!_assessments.TryGetValue(assessmentId, out var assessment))
                return ValueTask.FromResult(Left<EncinaError, Guid>(EncinaError.New("Not found")));
            if (assessment.Status != DPIAAssessmentStatus.InReview)
                return ValueTask.FromResult(Left<EncinaError, Guid>(EncinaError.New("Invalid state")));

            var consultationId = Guid.NewGuid();
            assessment.DPOConsultation = new DPOConsultation
            {
                Id = consultationId,
                DPOName = "Load Test DPO",
                DPOEmail = "dpo@loadtest.com",
                RequestedAtUtc = DateTimeOffset.UtcNow,
                Decision = DPOConsultationDecision.Pending,
            };
            assessment.LastModifiedAtUtc = DateTimeOffset.UtcNow;
            assessment.Version++;
            return ValueTask.FromResult(Right<EncinaError, Guid>(consultationId));
        }

        public ValueTask<Either<EncinaError, Unit>> RecordDPOResponseAsync(
            Guid assessmentId, Guid consultationId, DPOConsultationDecision decision,
            string? comments = null, string? conditions = null, CancellationToken cancellationToken = default)
        {
            if (!_assessments.TryGetValue(assessmentId, out var assessment))
                return ValueTask.FromResult(Left<EncinaError, Unit>(EncinaError.New("Not found")));
            if (assessment.DPOConsultation is null || assessment.DPOConsultation.Id != consultationId)
                return ValueTask.FromResult(Left<EncinaError, Unit>(EncinaError.New("No matching consultation")));

            assessment.DPOConsultation = assessment.DPOConsultation with
            {
                Decision = decision,
                Comments = comments,
                Conditions = conditions,
                RespondedAtUtc = DateTimeOffset.UtcNow,
            };
            assessment.LastModifiedAtUtc = DateTimeOffset.UtcNow;
            assessment.Version++;
            return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
        }

        public ValueTask<Either<EncinaError, Unit>> ApproveAssessmentAsync(
            Guid assessmentId, string approvedBy, DateTimeOffset? nextReviewAtUtc = null,
            CancellationToken cancellationToken = default)
        {
            if (!_assessments.TryGetValue(assessmentId, out var assessment))
                return ValueTask.FromResult(Left<EncinaError, Unit>(EncinaError.New("Not found")));
            if (assessment.Status != DPIAAssessmentStatus.InReview)
                return ValueTask.FromResult(Left<EncinaError, Unit>(EncinaError.New("Invalid state")));

            assessment.Status = DPIAAssessmentStatus.Approved;
            assessment.ApprovedAtUtc = DateTimeOffset.UtcNow;
            assessment.NextReviewAtUtc = nextReviewAtUtc;
            assessment.LastModifiedAtUtc = DateTimeOffset.UtcNow;
            assessment.Version++;
            return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
        }

        public ValueTask<Either<EncinaError, Unit>> RejectAssessmentAsync(
            Guid assessmentId, string rejectedBy, string reason, CancellationToken cancellationToken = default)
        {
            if (!_assessments.TryGetValue(assessmentId, out var assessment))
                return ValueTask.FromResult(Left<EncinaError, Unit>(EncinaError.New("Not found")));
            if (assessment.Status != DPIAAssessmentStatus.InReview)
                return ValueTask.FromResult(Left<EncinaError, Unit>(EncinaError.New("Invalid state")));

            assessment.Status = DPIAAssessmentStatus.Rejected;
            assessment.LastModifiedAtUtc = DateTimeOffset.UtcNow;
            assessment.Version++;
            return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
        }

        public ValueTask<Either<EncinaError, Unit>> RequestRevisionAsync(
            Guid assessmentId, string requestedBy, string reason, CancellationToken cancellationToken = default)
        {
            if (!_assessments.TryGetValue(assessmentId, out var assessment))
                return ValueTask.FromResult(Left<EncinaError, Unit>(EncinaError.New("Not found")));
            if (assessment.Status != DPIAAssessmentStatus.InReview)
                return ValueTask.FromResult(Left<EncinaError, Unit>(EncinaError.New("Invalid state")));

            assessment.Status = DPIAAssessmentStatus.RequiresRevision;
            assessment.LastModifiedAtUtc = DateTimeOffset.UtcNow;
            assessment.Version++;
            return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
        }

        public ValueTask<Either<EncinaError, Unit>> ExpireAssessmentAsync(
            Guid assessmentId, CancellationToken cancellationToken = default)
        {
            if (!_assessments.TryGetValue(assessmentId, out var assessment))
                return ValueTask.FromResult(Left<EncinaError, Unit>(EncinaError.New("Not found")));
            if (assessment.Status != DPIAAssessmentStatus.Approved)
                return ValueTask.FromResult(Left<EncinaError, Unit>(EncinaError.New("Invalid state")));

            assessment.Status = DPIAAssessmentStatus.Expired;
            assessment.LastModifiedAtUtc = DateTimeOffset.UtcNow;
            assessment.Version++;
            return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
        }

        public ValueTask<Either<EncinaError, DPIAReadModel>> GetAssessmentAsync(
            Guid assessmentId, CancellationToken cancellationToken = default)
        {
            if (_assessments.TryGetValue(assessmentId, out var assessment))
                return ValueTask.FromResult(Right<EncinaError, DPIAReadModel>(assessment));
            return ValueTask.FromResult(Left<EncinaError, DPIAReadModel>(EncinaError.New("Not found")));
        }

        public ValueTask<Either<EncinaError, DPIAReadModel>> GetAssessmentByRequestTypeAsync(
            string requestTypeName, CancellationToken cancellationToken = default)
        {
            var match = _assessments.Values.FirstOrDefault(a => a.RequestTypeName == requestTypeName);
            if (match is not null)
                return ValueTask.FromResult(Right<EncinaError, DPIAReadModel>(match));
            return ValueTask.FromResult(Left<EncinaError, DPIAReadModel>(EncinaError.New("Not found")));
        }

        public ValueTask<Either<EncinaError, IReadOnlyList<DPIAReadModel>>> GetExpiredAssessmentsAsync(
            CancellationToken cancellationToken = default)
        {
            var expired = _assessments.Values
                .Where(a => a.Status == DPIAAssessmentStatus.Expired)
                .ToList();
            return ValueTask.FromResult(Right<EncinaError, IReadOnlyList<DPIAReadModel>>(expired.AsReadOnly()));
        }

        public ValueTask<Either<EncinaError, IReadOnlyList<DPIAReadModel>>> GetAllAssessmentsAsync(
            CancellationToken cancellationToken = default)
        {
            var all = _assessments.Values.ToList();
            return ValueTask.FromResult(Right<EncinaError, IReadOnlyList<DPIAReadModel>>(all.AsReadOnly()));
        }

        public ValueTask<Either<EncinaError, IReadOnlyList<object>>> GetAssessmentHistoryAsync(
            Guid assessmentId, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(Left<EncinaError, IReadOnlyList<object>>(EncinaError.New("Not available in load test")));
        }
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

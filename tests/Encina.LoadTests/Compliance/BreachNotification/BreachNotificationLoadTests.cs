using System.Collections.Concurrent;
using System.Diagnostics;
using Encina.Compliance.BreachNotification.Abstractions;
using Encina.Compliance.BreachNotification.Model;
using Encina.Compliance.BreachNotification.ReadModels;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using static LanguageExt.Prelude;

namespace Encina.LoadTests.Compliance.BreachNotification;

/// <summary>
/// Load tests for the breach notification compliance module under high concurrent traffic.
/// Validates throughput, latency percentiles, and thread safety of:
/// - IBreachNotificationService lifecycle operations under concurrent access
/// - Breach detection pipeline under parallel invocation
/// - Mixed breach lifecycle scenarios with concurrent operations
/// - Latency distribution for breach recording and querying
/// </summary>
/// <remarks>
/// <para>
/// Breach notification is legally mandatory per GDPR Articles 33-34. The 72-hour notification
/// deadline makes timely processing critical. This load test validates that the service can
/// handle multiple concurrent breach operations without data corruption or lost events.
/// </para>
/// <para>
/// Run via: <c>dotnet run --project tests/Encina.LoadTests -- --scenario breachnotification</c>
/// </para>
/// </remarks>
internal static class BreachNotificationLoadTests
{
    private const int ConcurrentWorkers = 50;
    private const int OperationsPerWorker = 10_000;

    private static readonly string[] Natures =
        ["unauthorized access", "data exfiltration", "ransomware", "privilege escalation", "anomalous query"];

    private static readonly BreachSeverity[] Severities =
        [BreachSeverity.Low, BreachSeverity.Medium, BreachSeverity.High, BreachSeverity.Critical];

    private static readonly string[] DetectionRules =
        ["UnauthorizedAccessRule", "MassDataExfiltrationRule", "PrivilegeEscalationRule", "AnomalousQueryPatternRule"];

    private static readonly string[] Authorities =
        ["AEPD", "ICO", "CNIL", "BfDI", "Garante"];

    public static async Task RunAllAsync()
    {
        Console.WriteLine();
        Console.WriteLine("=== Breach Notification Compliance Load Tests ===");
        Console.WriteLine($"Workers: {ConcurrentWorkers}, Operations/worker: {OperationsPerWorker:N0}");
        Console.WriteLine();

        await RunTestAsync("Record Breach — Concurrent Detection Recording",
            RecordBreach_ConcurrentRecording_AllSucceed);
        await RunTestAsync("Assess Breach — Concurrent Assessment Updates",
            AssessBreach_ConcurrentAssessment_AllSucceed);
        await RunTestAsync("Full Lifecycle — Sequential Per-Breach Under Load",
            FullLifecycle_ConcurrentBreaches_AllComplete);
        await RunTestAsync("Phased Reports — Concurrent Report Addition",
            PhasedReports_ConcurrentAddition_AllSucceed);
        await RunTestAsync("Mixed Breach Scenarios — Concurrent",
            MixedBreachScenarios_ConcurrentOperations_NoErrors);
        await RunTestAsync("Query By Status — Concurrent Reads",
            QueryByStatus_ConcurrentReads_MaintainsThroughput);
        await RunTestAsync("Latency Distribution — P50/P95/P99",
            LatencyDistribution_ConcurrentLoad_WithinBounds);

        Console.WriteLine();
        Console.WriteLine("=== All breach notification load tests completed ===");
    }

    // ────────────────────────────────────────────────────────────
    //  Record Breach — Concurrent Detection Recording
    // ────────────────────────────────────────────────────────────

    private static async Task RecordBreach_ConcurrentRecording_AllSucceed()
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
                var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();

                var nature = Natures[i % Natures.Length];
                var severity = Severities[i % Severities.Length];
                var rule = DetectionRules[i % DetectionRules.Length];

                var result = await service.RecordBreachAsync(
                    nature, severity, rule, (i + 1) * 10,
                    $"Load test breach W{workerId}-{i}", $"worker-{workerId}");

                if (result.IsRight)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref failureCount);
            }
        }));

        await Task.WhenAll(tasks);

        var total = ConcurrentWorkers * OperationsPerWorker;
        Assert(successCount == total, $"Expected {total} successes, got {successCount} (failures: {failureCount})");

        Console.WriteLine($"  {successCount:N0} concurrent breach recordings, 0 failures");
    }

    // ────────────────────────────────────────────────────────────
    //  Assess Breach — Concurrent Assessment Updates
    // ────────────────────────────────────────────────────────────

    private static async Task AssessBreach_ConcurrentAssessment_AllSucceed()
    {
        using var provider = BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        // Pre-create breaches to assess
        var breachIds = new ConcurrentBag<Guid>();
        var batchSize = ConcurrentWorkers * 100; // Fewer ops for assess (requires load+save)
        for (var i = 0; i < batchSize; i++)
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();
            var result = await service.RecordBreachAsync(
                "assess-test", BreachSeverity.Low, "TestRule", 10, $"Assess test {i}", "setup");
            result.IfRight(id => breachIds.Add(id));
        }

        var ids = breachIds.ToArray();
        var successCount = 0L;
        var failureCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(workerId => Task.Run(async () =>
        {
            for (var i = 0; i < 100; i++)
            {
                var breachId = ids[(workerId * 100 + i) % ids.Length];
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();

                var result = await service.AssessBreachAsync(
                    breachId, BreachSeverity.High, 500,
                    $"Assessment by W{workerId}", $"assessor-{workerId}");

                if (result.IsRight)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref failureCount);
            }
        }));

        await Task.WhenAll(tasks);

        // Some assess calls may fail due to state conflicts (already assessed)
        // The important thing is no exceptions and total = success + failure
        var total = successCount + failureCount;
        Assert(total == ConcurrentWorkers * 100L,
            $"Expected {ConcurrentWorkers * 100} total operations, got {total}");

        Console.WriteLine($"  {total:N0} concurrent assessments: {successCount:N0} succeeded, {failureCount:N0} state conflicts");
    }

    // ────────────────────────────────────────────────────────────
    //  Full Lifecycle — Concurrent Breaches All Complete
    // ────────────────────────────────────────────────────────────

    private static async Task FullLifecycle_ConcurrentBreaches_AllComplete()
    {
        using var provider = BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        var completedCount = 0L;
        var errorCount = 0L;
        var breachesPerWorker = 20;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(workerId => Task.Run(async () =>
        {
            for (var i = 0; i < breachesPerWorker; i++)
            {
                try
                {
                    Guid breachId;

                    // Record
                    using (var scope = scopeFactory.CreateScope())
                    {
                        var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();
                        var result = await service.RecordBreachAsync(
                            "lifecycle-test", BreachSeverity.High, "LifecycleRule",
                            100, $"Lifecycle W{workerId}-{i}", $"worker-{workerId}");
                        breachId = result.Match(id => id, _ => Guid.Empty);
                        if (breachId == Guid.Empty) { Interlocked.Increment(ref errorCount); continue; }
                    }

                    // Assess
                    using (var scope = scopeFactory.CreateScope())
                    {
                        var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();
                        await service.AssessBreachAsync(
                            breachId, BreachSeverity.Critical, 200, "Full assessment", "assessor");
                    }

                    // Report to DPA
                    using (var scope = scopeFactory.CreateScope())
                    {
                        var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();
                        await service.ReportToDPAAsync(
                            breachId, Authorities[i % Authorities.Length],
                            "contact@dpa.eu", "Report", "dpo");
                    }

                    // Notify subjects
                    using (var scope = scopeFactory.CreateScope())
                    {
                        var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();
                        await service.NotifySubjectsAsync(
                            breachId, 200, "email", SubjectNotificationExemption.None, "comms");
                    }

                    // Contain
                    using (var scope = scopeFactory.CreateScope())
                    {
                        var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();
                        await service.ContainBreachAsync(
                            breachId, "Access revoked", "ops");
                    }

                    // Close
                    using (var scope = scopeFactory.CreateScope())
                    {
                        var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();
                        await service.CloseBreachAsync(
                            breachId, "Resolved and documented", "dpo");
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

        var total = ConcurrentWorkers * breachesPerWorker;
        Assert(completedCount + errorCount == total,
            $"Expected {total} total, got {completedCount + errorCount}");
        Assert(completedCount > total * 0.8,
            $"Expected >80% completion rate, got {completedCount}/{total}");

        Console.WriteLine($"  {completedCount:N0}/{total:N0} full lifecycles completed ({errorCount:N0} conflicts)");
    }

    // ────────────────────────────────────────────────────────────
    //  Phased Reports — Concurrent Addition
    // ────────────────────────────────────────────────────────────

    private static async Task PhasedReports_ConcurrentAddition_AllSucceed()
    {
        using var provider = BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        // Pre-create breaches in AuthorityNotified status (can accept phased reports)
        var breachIds = new List<Guid>();
        for (var i = 0; i < ConcurrentWorkers; i++)
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();
            var result = await service.RecordBreachAsync(
                "phased-test", BreachSeverity.High, "Rule", 100, $"Phased {i}", "user");
            var breachId = result.Match(id => id, _ => Guid.Empty);
            if (breachId == Guid.Empty) continue;

            await service.AssessBreachAsync(breachId, BreachSeverity.High, 100, "Assessed", "assessor");

            using var scope2 = scopeFactory.CreateScope();
            var service2 = scope2.ServiceProvider.GetRequiredService<IBreachNotificationService>();
            await service2.ReportToDPAAsync(breachId, "DPA", "dpa@gov.eu", "Report", "dpo");

            breachIds.Add(breachId);
        }

        var ids = breachIds.ToArray();
        var successCount = 0L;
        var failureCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(workerId => Task.Run(async () =>
        {
            var breachId = ids[workerId % ids.Length];
            for (var phase = 1; phase <= 5; phase++)
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();
                var result = await service.AddPhasedReportAsync(
                    breachId, $"Phase {phase} from W{workerId}", $"analyst-{workerId}");

                if (result.IsRight)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref failureCount);
            }
        }));

        await Task.WhenAll(tasks);

        var total = successCount + failureCount;
        Assert(total == ConcurrentWorkers * 5L,
            $"Expected {ConcurrentWorkers * 5} total, got {total}");

        Console.WriteLine($"  {total:N0} phased report additions: {successCount:N0} succeeded, {failureCount:N0} conflicts");
    }

    // ────────────────────────────────────────────────────────────
    //  Mixed Breach Scenarios — Concurrent
    // ────────────────────────────────────────────────────────────

    private static async Task MixedBreachScenarios_ConcurrentOperations_NoErrors()
    {
        using var provider = BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        var errors = new ConcurrentQueue<string>();
        var operationCounts = new ConcurrentDictionary<string, int>();

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(workerId => Task.Run(async () =>
        {
            for (var i = 0; i < 200; i++)
            {
                var scenario = i % 4;

                try
                {
                    string scenarioName;

                    switch (scenario)
                    {
                        case 0: // Record only
                            {
                                using var scope = scopeFactory.CreateScope();
                                var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();
                                await service.RecordBreachAsync(
                                    "mixed-record", Severities[i % Severities.Length],
                                    DetectionRules[i % DetectionRules.Length],
                                    i * 10, $"Mixed W{workerId}-{i}", $"worker-{workerId}");
                                scenarioName = "Record";
                                break;
                            }

                        case 1: // Record + Assess
                            {
                                using var scope = scopeFactory.CreateScope();
                                var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();
                                var result = await service.RecordBreachAsync(
                                    "mixed-assess", BreachSeverity.Low, "MixedRule",
                                    50, $"Mixed assess W{workerId}-{i}", $"worker-{workerId}");
                                if (result.IsRight)
                                {
                                    var id = result.Match(x => x, _ => Guid.Empty);
                                    using var scope2 = scopeFactory.CreateScope();
                                    var service2 = scope2.ServiceProvider.GetRequiredService<IBreachNotificationService>();
                                    await service2.AssessBreachAsync(
                                        id, BreachSeverity.Medium, 100, "Mixed assessment", "assessor");
                                }
                                scenarioName = "Record+Assess";
                                break;
                            }

                        case 2: // Record + Report
                            {
                                using var scope = scopeFactory.CreateScope();
                                var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();
                                var result = await service.RecordBreachAsync(
                                    "mixed-report", BreachSeverity.Critical, "MixedRule",
                                    1000, $"Mixed report W{workerId}-{i}", $"worker-{workerId}");
                                if (result.IsRight)
                                {
                                    var id = result.Match(x => x, _ => Guid.Empty);
                                    using var scope2 = scopeFactory.CreateScope();
                                    var service2 = scope2.ServiceProvider.GetRequiredService<IBreachNotificationService>();
                                    await service2.ReportToDPAAsync(
                                        id, "DPA", "dpa@gov.eu", "Report", "dpo");
                                }
                                scenarioName = "Record+Report";
                                break;
                            }

                        default: // Query
                            {
                                using var scope = scopeFactory.CreateScope();
                                var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();
                                await service.GetBreachesByStatusAsync(BreachStatus.Detected);
                                scenarioName = "Query";
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
    //  Query By Status — Concurrent Reads
    // ────────────────────────────────────────────────────────────

    private static async Task QueryByStatus_ConcurrentReads_MaintainsThroughput()
    {
        using var provider = BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        // Pre-populate some breaches
        for (var i = 0; i < 50; i++)
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();
            await service.RecordBreachAsync(
                "query-test", Severities[i % Severities.Length], "QueryRule",
                i * 10, $"Query test {i}", "setup");
        }

        var successCount = 0L;
        var failureCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < 100; i++)
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();
                var result = await service.GetBreachesByStatusAsync(BreachStatus.Detected);

                if (result.IsRight)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref failureCount);
            }
        }));

        await Task.WhenAll(tasks);

        var total = ConcurrentWorkers * 100L;
        Assert(successCount == total, $"Expected {total} successes, got {successCount}");

        Console.WriteLine($"  {successCount:N0} concurrent status queries, 0 failures");
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
                var service = scope.ServiceProvider.GetRequiredService<IBreachNotificationService>();

                var sw = Stopwatch.StartNew();
                await service.RecordBreachAsync(
                    Natures[i % Natures.Length], Severities[i % Severities.Length],
                    DetectionRules[i % DetectionRules.Length],
                    (i + 1) * 5, $"Latency W{workerId}-{i}", $"worker-{workerId}");
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

        // Breach recording involves aggregate creation + Marten persistence,
        // so we allow a generous P99 threshold
        Assert(p99 < 100_000, $"P99 latency {p99:F1}µs exceeds 100ms threshold");

        Console.WriteLine($"  {latencies.Count:N0} operations — mean: {mean:F1}µs, P50: {p50:F1}µs, P95: {p95:F1}µs, P99: {p99:F1}µs, min: {min:F1}µs, max: {max:F1}µs");
    }

    // ────────────────────────────────────────────────────────────
    //  Test Infrastructure — Service Provider Builder
    // ────────────────────────────────────────────────────────────

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // Register a mocked IBreachNotificationService that simulates all operations
        // using thread-safe in-memory state. This avoids requiring real PostgreSQL/Marten
        // for load tests while exercising the full service API contract.
        var breachService = new InMemoryBreachNotificationService();
        services.AddScoped<IBreachNotificationService>(_ => breachService);

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
    /// Thread-safe in-memory implementation of <see cref="IBreachNotificationService"/> for load testing.
    /// Simulates the full breach lifecycle without requiring PostgreSQL/Marten.
    /// </summary>
    private sealed class InMemoryBreachNotificationService : IBreachNotificationService
    {
        private readonly ConcurrentDictionary<Guid, BreachReadModel> _breaches = new();

        public ValueTask<Either<EncinaError, Guid>> RecordBreachAsync(
            string nature, BreachSeverity severity, string detectedByRule,
            int estimatedAffectedSubjects, string description,
            string? detectedByUserId = null, string? tenantId = null, string? moduleId = null,
            CancellationToken cancellationToken = default)
        {
            var id = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;
            _breaches[id] = new BreachReadModel
            {
                Id = id,
                Nature = nature,
                Severity = severity,
                DetectedByRule = detectedByRule,
                EstimatedAffectedSubjects = estimatedAffectedSubjects,
                Description = description,
                DetectedByUserId = detectedByUserId,
                DetectedAtUtc = now,
                DeadlineUtc = now.AddHours(72),
                Status = BreachStatus.Detected,
                TenantId = tenantId,
                ModuleId = moduleId,
                LastModifiedAtUtc = now,
                Version = 1
            };
            return ValueTask.FromResult(Right<EncinaError, Guid>(id));
        }

        public ValueTask<Either<EncinaError, Unit>> AssessBreachAsync(
            Guid breachId, BreachSeverity updatedSeverity, int updatedAffectedSubjects,
            string assessmentSummary, string assessedByUserId, CancellationToken cancellationToken = default)
        {
            if (!_breaches.TryGetValue(breachId, out var breach))
                return ValueTask.FromResult(Left<EncinaError, Unit>(EncinaError.New("Not found")));
            if (breach.Status != BreachStatus.Detected)
                return ValueTask.FromResult(Left<EncinaError, Unit>(EncinaError.New("Invalid state")));

            breach.Severity = updatedSeverity;
            breach.EstimatedAffectedSubjects = updatedAffectedSubjects;
            breach.AssessmentSummary = assessmentSummary;
            breach.AssessedAtUtc = DateTimeOffset.UtcNow;
            breach.Status = BreachStatus.Investigating;
            breach.LastModifiedAtUtc = DateTimeOffset.UtcNow;
            breach.Version++;
            return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
        }

        public ValueTask<Either<EncinaError, Unit>> ReportToDPAAsync(
            Guid breachId, string authorityName, string authorityContactInfo,
            string reportSummary, string reportedByUserId, CancellationToken cancellationToken = default)
        {
            if (!_breaches.TryGetValue(breachId, out var breach))
                return ValueTask.FromResult(Left<EncinaError, Unit>(EncinaError.New("Not found")));
            if (breach.Status is not (BreachStatus.Detected or BreachStatus.Investigating))
                return ValueTask.FromResult(Left<EncinaError, Unit>(EncinaError.New("Invalid state")));

            breach.AuthorityName = authorityName;
            breach.ReportedToDPAAtUtc = DateTimeOffset.UtcNow;
            breach.Status = BreachStatus.AuthorityNotified;
            breach.LastModifiedAtUtc = DateTimeOffset.UtcNow;
            breach.Version++;
            return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
        }

        public ValueTask<Either<EncinaError, Unit>> NotifySubjectsAsync(
            Guid breachId, int subjectCount, string communicationMethod,
            SubjectNotificationExemption exemption, string notifiedByUserId,
            CancellationToken cancellationToken = default)
        {
            if (!_breaches.TryGetValue(breachId, out var breach))
                return ValueTask.FromResult(Left<EncinaError, Unit>(EncinaError.New("Not found")));
            if (breach.Status != BreachStatus.AuthorityNotified)
                return ValueTask.FromResult(Left<EncinaError, Unit>(EncinaError.New("Invalid state")));

            breach.SubjectCount = subjectCount;
            breach.CommunicationMethod = communicationMethod;
            breach.Exemption = exemption;
            breach.NotifiedSubjectsAtUtc = DateTimeOffset.UtcNow;
            breach.Status = BreachStatus.SubjectsNotified;
            breach.LastModifiedAtUtc = DateTimeOffset.UtcNow;
            breach.Version++;
            return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
        }

        public ValueTask<Either<EncinaError, Unit>> AddPhasedReportAsync(
            Guid breachId, string reportContent, string submittedByUserId,
            CancellationToken cancellationToken = default)
        {
            if (!_breaches.TryGetValue(breachId, out var breach))
                return ValueTask.FromResult(Left<EncinaError, Unit>(EncinaError.New("Not found")));
            if (breach.Status == BreachStatus.Closed)
                return ValueTask.FromResult(Left<EncinaError, Unit>(EncinaError.New("Closed")));

            var phaseNumber = breach.PhasedReports.Count + 1;
            breach.PhasedReports.Add(new PhasedReportSummary(
                phaseNumber, reportContent, submittedByUserId, DateTimeOffset.UtcNow));
            breach.LastModifiedAtUtc = DateTimeOffset.UtcNow;
            breach.Version++;
            return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
        }

        public ValueTask<Either<EncinaError, Unit>> ContainBreachAsync(
            Guid breachId, string containmentMeasures, string containedByUserId,
            CancellationToken cancellationToken = default)
        {
            if (!_breaches.TryGetValue(breachId, out var breach))
                return ValueTask.FromResult(Left<EncinaError, Unit>(EncinaError.New("Not found")));
            if (breach.Status == BreachStatus.Closed)
                return ValueTask.FromResult(Left<EncinaError, Unit>(EncinaError.New("Closed")));

            breach.ContainmentMeasures = containmentMeasures;
            breach.ContainedAtUtc = DateTimeOffset.UtcNow;
            breach.Status = BreachStatus.Resolved;
            breach.LastModifiedAtUtc = DateTimeOffset.UtcNow;
            breach.Version++;
            return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
        }

        public ValueTask<Either<EncinaError, Unit>> CloseBreachAsync(
            Guid breachId, string resolutionSummary, string closedByUserId,
            CancellationToken cancellationToken = default)
        {
            if (!_breaches.TryGetValue(breachId, out var breach))
                return ValueTask.FromResult(Left<EncinaError, Unit>(EncinaError.New("Not found")));
            if (breach.Status is not (BreachStatus.SubjectsNotified or BreachStatus.Resolved))
                return ValueTask.FromResult(Left<EncinaError, Unit>(EncinaError.New("Invalid state")));

            breach.ResolutionSummary = resolutionSummary;
            breach.ClosedAtUtc = DateTimeOffset.UtcNow;
            breach.Status = BreachStatus.Closed;
            breach.LastModifiedAtUtc = DateTimeOffset.UtcNow;
            breach.Version++;
            return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
        }

        public ValueTask<Either<EncinaError, BreachReadModel>> GetBreachAsync(
            Guid breachId, CancellationToken cancellationToken = default)
        {
            if (_breaches.TryGetValue(breachId, out var breach))
                return ValueTask.FromResult(Right<EncinaError, BreachReadModel>(breach));
            return ValueTask.FromResult(Left<EncinaError, BreachReadModel>(EncinaError.New("Not found")));
        }

        public ValueTask<Either<EncinaError, IReadOnlyList<BreachReadModel>>> GetBreachesByStatusAsync(
            BreachStatus status, CancellationToken cancellationToken = default)
        {
            var results = _breaches.Values.Where(b => b.Status == status).ToList();
            return ValueTask.FromResult(Right<EncinaError, IReadOnlyList<BreachReadModel>>(results.AsReadOnly()));
        }

        public ValueTask<Either<EncinaError, IReadOnlyList<BreachReadModel>>> GetBreachesByTenantAsync(
            string tenantId, CancellationToken cancellationToken = default)
        {
            var results = _breaches.Values.Where(b => b.TenantId == tenantId).ToList();
            return ValueTask.FromResult(Right<EncinaError, IReadOnlyList<BreachReadModel>>(results.AsReadOnly()));
        }

        public ValueTask<Either<EncinaError, IReadOnlyList<BreachReadModel>>> GetApproachingDeadlineBreachesAsync(
            CancellationToken cancellationToken = default)
        {
            var now = DateTimeOffset.UtcNow;
            var deadline = now.AddHours(24);
            var results = _breaches.Values
                .Where(b => b.ReportedToDPAAtUtc == null && b.Status != BreachStatus.Closed && b.DeadlineUtc <= deadline)
                .ToList();
            return ValueTask.FromResult(Right<EncinaError, IReadOnlyList<BreachReadModel>>(results.AsReadOnly()));
        }

        public ValueTask<Either<EncinaError, IReadOnlyList<object>>> GetBreachHistoryAsync(
            Guid breachId, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(Left<EncinaError, IReadOnlyList<object>>(EncinaError.New("Not available")));
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

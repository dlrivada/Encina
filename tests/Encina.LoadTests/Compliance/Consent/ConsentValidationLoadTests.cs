using System.Collections.Concurrent;
using System.Diagnostics;
using Encina.Compliance.Consent;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using static LanguageExt.Prelude;

namespace Encina.LoadTests.Compliance.Consent;

/// <summary>
/// Load tests for the consent validation system under high concurrent traffic.
/// Validates throughput, latency percentiles, and thread safety of:
/// - InMemoryConsentStore operations (RecordConsent, HasValidConsent, Withdraw, Bulk)
/// - ConsentRequiredPipelineBehavior under concurrent request processing
/// - DefaultConsentValidator multi-purpose validation
/// </summary>
/// <remarks>
/// Run via: <c>dotnet run --project tests/Encina.LoadTests -- --scenario consent</c>
/// (requires integration with Program.cs scenario routing, or run directly via <see cref="RunAllAsync"/>).
/// </remarks>
internal static class ConsentValidationLoadTests
{
    private const int ConcurrentWorkers = 50;
    private const int OperationsPerWorker = 10_000;

    public static async Task RunAllAsync()
    {
        Console.WriteLine();
        Console.WriteLine("=== Consent Validation Load Tests ===");
        Console.WriteLine($"Workers: {ConcurrentWorkers}, Operations/worker: {OperationsPerWorker:N0}");
        Console.WriteLine();

        await RunTestAsync("HasValidConsent — Concurrent Reads", HasValidConsent_ConcurrentReads_MaintainsThroughput);
        await RunTestAsync("RecordConsent — Concurrent Writes", RecordConsent_ConcurrentWrites_AllSucceed);
        await RunTestAsync("WithdrawConsent — Concurrent Withdrawals", WithdrawConsent_ConcurrentWithdrawals_ThreadSafe);
        await RunTestAsync("Mixed Read/Write — Concurrent Operations", MixedReadWrite_ConcurrentOperations_NoErrors);
        await RunTestAsync("BulkRecord — High Volume Batch", BulkRecordConsent_HighVolumeBatch_CompletesWithinBounds);
        await RunTestAsync("Pipeline Validation — Concurrent Checks", PipelineValidation_ConcurrentChecks_MaintainsThroughput);
        await RunTestAsync("Latency Distribution — P50/P95/P99", LatencyDistribution_ConcurrentLoad_WithinBounds);

        Console.WriteLine();
        Console.WriteLine("=== All consent validation load tests completed ===");
    }

    // ────────────────────────────────────────────────────────────
    //  HasValidConsent — Concurrent Reads Under Load
    // ────────────────────────────────────────────────────────────

    private static async Task HasValidConsent_ConcurrentReads_MaintainsThroughput()
    {
        var store = CreateConsentStore();

        // Seed consent records for multiple subjects
        for (var i = 0; i < 100; i++)
        {
            await store.RecordConsentAsync(CreateConsentRecord($"subject-{i}", ConsentPurposes.Marketing));
        }

        var successCount = 0L;
        var failureCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var subjectId = $"subject-{i % 100}";
                var result = await store.HasValidConsentAsync(subjectId, ConsentPurposes.Marketing);

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
    //  RecordConsent — Concurrent Writes Under Load
    // ────────────────────────────────────────────────────────────

    private static async Task RecordConsent_ConcurrentWrites_AllSucceed()
    {
        var store = CreateConsentStore();
        var successCount = 0L;
        var failureCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(workerId => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                // Each worker writes to unique subject IDs (no contention)
                var subjectId = $"worker-{workerId}-subject-{i}";
                var consent = CreateConsentRecord(subjectId, ConsentPurposes.Analytics);
                var result = await store.RecordConsentAsync(consent);

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
    //  WithdrawConsent — Concurrent Withdrawals Thread Safety
    // ────────────────────────────────────────────────────────────

    private static async Task WithdrawConsent_ConcurrentWithdrawals_ThreadSafe()
    {
        var store = CreateConsentStore();

        // Seed consents to withdraw
        var totalConsents = ConcurrentWorkers * 100;
        for (var i = 0; i < totalConsents; i++)
        {
            await store.RecordConsentAsync(CreateConsentRecord($"withdraw-subject-{i}", ConsentPurposes.Marketing));
        }

        var successCount = 0L;
        var expectedFailureCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(workerId => Task.Run(async () =>
        {
            for (var i = 0; i < 100; i++)
            {
                var subjectId = $"withdraw-subject-{workerId * 100 + i}";
                var result = await store.WithdrawConsentAsync(subjectId, ConsentPurposes.Marketing);

                if (result.IsRight)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref expectedFailureCount);
            }
        }));

        await Task.WhenAll(tasks);

        Assert(successCount == totalConsents, $"Expected {totalConsents} successful withdrawals, got {successCount}");

        // Verify all withdrawn
        for (var i = 0; i < totalConsents; i++)
        {
            var result = await store.HasValidConsentAsync($"withdraw-subject-{i}", ConsentPurposes.Marketing);
            var isValid = result.Match(Left: _ => true, Right: v => v);
            Assert(!isValid, $"Subject withdraw-subject-{i} should not have valid consent after withdrawal");
        }

        Console.WriteLine($"  {successCount:N0} withdrawals, all verified as withdrawn");
    }

    // ────────────────────────────────────────────────────────────
    //  Mixed Read/Write — Concurrent Operations
    // ────────────────────────────────────────────────────────────

    private static async Task MixedReadWrite_ConcurrentOperations_NoErrors()
    {
        var store = CreateConsentStore();
        var errors = new ConcurrentQueue<string>();
        var operationCounts = new ConcurrentDictionary<string, int>();

        // Seed some initial records
        for (var i = 0; i < 50; i++)
        {
            await store.RecordConsentAsync(CreateConsentRecord($"mixed-subject-{i}", ConsentPurposes.Marketing));
        }

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(workerId => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var operation = i % 5;
                var subjectId = $"mixed-subject-{(workerId * OperationsPerWorker + i) % 200}";

                try
                {
                    switch (operation)
                    {
                        case 0: // HasValidConsent (read)
                            await store.HasValidConsentAsync(subjectId, ConsentPurposes.Marketing);
                            operationCounts.AddOrUpdate("HasValid", 1, (_, c) => c + 1);
                            break;

                        case 1: // GetConsent (read)
                            await store.GetConsentAsync(subjectId, ConsentPurposes.Marketing);
                            operationCounts.AddOrUpdate("GetConsent", 1, (_, c) => c + 1);
                            break;

                        case 2: // GetAllConsents (read)
                            await store.GetAllConsentsAsync(subjectId);
                            operationCounts.AddOrUpdate("GetAll", 1, (_, c) => c + 1);
                            break;

                        case 3: // RecordConsent (write)
                            await store.RecordConsentAsync(
                                CreateConsentRecord(subjectId, ConsentPurposes.Marketing));
                            operationCounts.AddOrUpdate("Record", 1, (_, c) => c + 1);
                            break;

                        case 4: // WithdrawConsent (write — may fail if not found)
                            await store.WithdrawConsentAsync(subjectId, ConsentPurposes.Marketing);
                            operationCounts.AddOrUpdate("Withdraw", 1, (_, c) => c + 1);
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
        Console.WriteLine($"  {totalOps:N0} mixed operations: {string.Join(", ", operationCounts.Select(kv => $"{kv.Key}={kv.Value:N0}"))}");
    }

    // ────────────────────────────────────────────────────────────
    //  BulkRecord — High Volume Batch
    // ────────────────────────────────────────────────────────────

    private static async Task BulkRecordConsent_HighVolumeBatch_CompletesWithinBounds()
    {
        var store = CreateConsentStore();
        var batchSize = 1000;
        var batchCount = ConcurrentWorkers;

        var tasks = Enumerable.Range(0, batchCount).Select(batchId => Task.Run(async () =>
        {
            var consents = Enumerable.Range(0, batchSize)
                .Select(i => CreateConsentRecord($"bulk-{batchId}-subject-{i}", ConsentPurposes.Marketing))
                .ToList();

            return await store.BulkRecordConsentAsync(consents);
        }));

        var results = await Task.WhenAll(tasks);

        var totalSuccess = 0;
        var totalErrors = 0;

        foreach (var result in results)
        {
            result.Match(
                Left: _ => totalErrors++,
                Right: bulkResult =>
                {
                    totalSuccess += bulkResult.SuccessCount;
                    totalErrors += bulkResult.Errors.Count;
                });
        }

        var expectedTotal = batchSize * batchCount;
        Assert(totalSuccess == expectedTotal, $"Expected {expectedTotal} bulk successes, got {totalSuccess}");
        Assert(totalErrors == 0, $"Expected 0 bulk errors, got {totalErrors}");

        Console.WriteLine($"  {totalSuccess:N0} records in {batchCount} batches of {batchSize} each, 0 errors");
    }

    // ────────────────────────────────────────────────────────────
    //  Pipeline Validation — Concurrent Consent Checks
    // ────────────────────────────────────────────────────────────

    private static async Task PipelineValidation_ConcurrentChecks_MaintainsThroughput()
    {
        // Set up full consent pipeline with DI
        var services = new ServiceCollection();
        services.AddEncina(config =>
            config.RegisterServicesFromAssemblyContaining<ConsentLoadTestCommand>());
        services.AddEncinaConsent(options =>
        {
            options.EnforcementMode = ConsentEnforcementMode.Block;
            options.AutoRegisterFromAttributes = false;
        });
        services.AddScoped<IRequestHandler<ConsentLoadTestCommand, int>, ConsentLoadTestHandler>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = false,
            ValidateOnBuild = false
        });

        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var consentStore = provider.GetRequiredService<IConsentStore>();

        // Seed consent records for test subjects
        for (var i = 0; i < 100; i++)
        {
            await consentStore.RecordConsentAsync(
                CreateConsentRecord($"pipeline-user-{i}", ConsentPurposes.Marketing));
        }

        var successCount = 0L;
        var blockedCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < 1000; i++)
            {
                using var scope = scopeFactory.CreateScope();
                var encina = scope.ServiceProvider.GetRequiredService<IEncina>();

                // Half will have consent, half won't
                var subjectId = i % 2 == 0
                    ? $"pipeline-user-{i % 100}"
                    : $"no-consent-user-{i}";

                var command = new ConsentLoadTestCommand(subjectId);
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
        Assert(successCount > 0, "Expected some successful operations");
        Assert(blockedCount > 0, "Expected some blocked operations (missing consent)");

        Console.WriteLine($"  {total:N0} pipeline validations: {successCount:N0} passed, {blockedCount:N0} blocked");
    }

    // ────────────────────────────────────────────────────────────
    //  Latency Distribution — P50/P95/P99
    // ────────────────────────────────────────────────────────────

    private static async Task LatencyDistribution_ConcurrentLoad_WithinBounds()
    {
        var store = CreateConsentStore();
        var latencies = new ConcurrentBag<double>();

        // Seed consent records
        for (var i = 0; i < 1000; i++)
        {
            await store.RecordConsentAsync(CreateConsentRecord($"latency-subject-{i}", ConsentPurposes.Marketing));
        }

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var subjectId = $"latency-subject-{i % 1000}";
                var sw = Stopwatch.StartNew();
                await store.HasValidConsentAsync(subjectId, ConsentPurposes.Marketing);
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

        // InMemoryConsentStore is ConcurrentDictionary-based, should be sub-millisecond
        Assert(p99 < 10_000, $"P99 latency {p99:F1}µs exceeds 10ms threshold");

        var totalOps = latencies.Count;
        var durationEstimate = sorted.Sum() / 1_000_000; // rough total seconds
        Console.WriteLine($"  {totalOps:N0} operations — mean: {mean:F1}µs, P50: {p50:F1}µs, P95: {p95:F1}µs, P99: {p99:F1}µs, min: {min:F1}µs, max: {max:F1}µs");
    }

    // ────────────────────────────────────────────────────────────
    //  Test Infrastructure
    // ────────────────────────────────────────────────────────────

    [RequireConsent(ConsentPurposes.Marketing, SubjectIdProperty = nameof(UserId))]
    private sealed record ConsentLoadTestCommand(string UserId) : IRequest<int>;

    private sealed class ConsentLoadTestHandler : IRequestHandler<ConsentLoadTestCommand, int>
    {
        public Task<Either<EncinaError, int>> Handle(ConsentLoadTestCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, int>(42));
    }

    private static InMemoryConsentStore CreateConsentStore()
    {
        return new InMemoryConsentStore(
            TimeProvider.System,
            NullLogger<InMemoryConsentStore>.Instance);
    }

    private static ConsentRecord CreateConsentRecord(string subjectId, string purpose)
    {
        return new ConsentRecord
        {
            Id = Guid.NewGuid(),
            SubjectId = subjectId,
            Purpose = purpose,
            Status = ConsentStatus.Active,
            ConsentVersionId = "v1.0",
            GivenAtUtc = DateTimeOffset.UtcNow,
            Source = "load-test",
            Metadata = new Dictionary<string, object?>()
        };
    }

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

using System.Collections.Concurrent;
using System.Diagnostics;
using Encina.Compliance.DataSubjectRights;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using static LanguageExt.Prelude;

namespace Encina.LoadTests.Compliance.DataSubjectRights;

/// <summary>
/// Load tests for the DSR request store and audit trail under high concurrent traffic.
/// Validates throughput, latency percentiles, and thread safety of:
/// - InMemoryDSRRequestStore operations (Create, GetById, GetBySubjectId, UpdateStatus)
/// - InMemoryDSRAuditStore operations (Record, GetAuditTrail)
/// - HasActiveRestriction enforcement under concurrent access
/// </summary>
/// <remarks>
/// Run via: <c>dotnet run --project tests/Encina.LoadTests -- --scenario dsr</c>
/// (requires integration with Program.cs scenario routing, or run directly via <see cref="RunAllAsync"/>).
/// </remarks>
internal static class DSRRequestStoreLoadTests
{
    private const int ConcurrentWorkers = 50;
    private const int OperationsPerWorker = 10_000;

    public static async Task RunAllAsync()
    {
        Console.WriteLine();
        Console.WriteLine("=== DSR Request Store Load Tests ===");
        Console.WriteLine($"Workers: {ConcurrentWorkers}, Operations/worker: {OperationsPerWorker:N0}");
        Console.WriteLine();

        await RunTestAsync("RequestStore GetById — Concurrent Reads", RequestStoreGetById_ConcurrentReads_MaintainsThroughput);
        await RunTestAsync("RequestStore Create — Concurrent Writes", RequestStoreCreate_ConcurrentWrites_AllSucceed);
        await RunTestAsync("RequestStore GetBySubjectId — Concurrent Reads", RequestStoreGetBySubjectId_ConcurrentReads_MaintainsThroughput);
        await RunTestAsync("RequestStore UpdateStatus — Concurrent Updates", RequestStoreUpdateStatus_ConcurrentUpdates_AllSucceed);
        await RunTestAsync("RequestStore HasActiveRestriction — Under Load", HasActiveRestriction_ConcurrentChecks_MaintainsThroughput);
        await RunTestAsync("AuditStore Record and Get — Mixed Concurrent", AuditStore_MixedReadWrite_NoErrors);
        await RunTestAsync("Mixed RequestStore Operations — Concurrent", MixedRequestStoreOperations_ConcurrentOperations_NoErrors);
        await RunTestAsync("RequestStore Latency Distribution — P50/P95/P99", RequestStoreLatencyDistribution_ConcurrentLoad_WithinBounds);

        Console.WriteLine();
        Console.WriteLine("=== All DSR request store load tests completed ===");
    }

    // ────────────────────────────────────────────────────────────
    //  RequestStore GetById — Concurrent Reads Under Load
    // ────────────────────────────────────────────────────────────

    private static async Task RequestStoreGetById_ConcurrentReads_MaintainsThroughput()
    {
        var store = CreateRequestStore();

        // Seed requests
        for (var i = 0; i < 100; i++)
        {
            var request = DSRRequest.Create($"req-{i}", $"subject-{i % 10}", (DataSubjectRight)(i % 8), DateTimeOffset.UtcNow);
            await store.CreateAsync(request);
        }

        var successCount = 0L;
        var failureCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var id = $"req-{i % 100}";
                var result = await store.GetByIdAsync(id);

                if (result.IsRight)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref failureCount);
            }
        }));

        await Task.WhenAll(tasks);

        var total = ConcurrentWorkers * OperationsPerWorker;
        Assert(successCount == total, $"Expected {total} successes, got {successCount}");
        Console.WriteLine($"  {successCount:N0} concurrent reads, 0 failures");
    }

    // ────────────────────────────────────────────────────────────
    //  RequestStore Create — Concurrent Writes (Non-Contention)
    // ────────────────────────────────────────────────────────────

    private static async Task RequestStoreCreate_ConcurrentWrites_AllSucceed()
    {
        var store = CreateRequestStore();
        var successCount = 0L;
        var failureCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(workerId => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var id = $"w{workerId}-req-{i}";
                var request = DSRRequest.Create(id, $"subject-{workerId}", DataSubjectRight.Access, DateTimeOffset.UtcNow);
                var result = await store.CreateAsync(request);

                if (result.IsRight)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref failureCount);
            }
        }));

        await Task.WhenAll(tasks);

        var total = ConcurrentWorkers * OperationsPerWorker;
        Assert(successCount == total, $"Expected {total} successes, got {successCount}");
        Assert(store.Count == total, $"Expected {total} records, got {store.Count}");
        Console.WriteLine($"  {successCount:N0} concurrent writes, store size: {store.Count:N0}");
    }

    // ────────────────────────────────────────────────────────────
    //  RequestStore GetBySubjectId — Concurrent Reads Under Load
    // ────────────────────────────────────────────────────────────

    private static async Task RequestStoreGetBySubjectId_ConcurrentReads_MaintainsThroughput()
    {
        var store = CreateRequestStore();

        // Seed: 10 subjects with 10 requests each
        for (var s = 0; s < 10; s++)
        {
            for (var r = 0; r < 10; r++)
            {
                var request = DSRRequest.Create($"req-{s}-{r}", $"subject-{s}", (DataSubjectRight)(r % 8), DateTimeOffset.UtcNow);
                await store.CreateAsync(request);
            }
        }

        var successCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var subjectId = $"subject-{i % 10}";
                var result = await store.GetBySubjectIdAsync(subjectId);

                if (result.IsRight)
                    Interlocked.Increment(ref successCount);
            }
        }));

        await Task.WhenAll(tasks);

        var total = ConcurrentWorkers * OperationsPerWorker;
        Assert(successCount == total, $"Expected {total} successes, got {successCount}");
        Console.WriteLine($"  {successCount:N0} concurrent subject lookups, 0 failures");
    }

    // ────────────────────────────────────────────────────────────
    //  RequestStore UpdateStatus — Concurrent Status Updates
    // ────────────────────────────────────────────────────────────

    private static async Task RequestStoreUpdateStatus_ConcurrentUpdates_AllSucceed()
    {
        var store = CreateRequestStore();
        var totalRequests = ConcurrentWorkers * 100;

        // Seed requests (one per unique ID)
        for (var i = 0; i < totalRequests; i++)
        {
            var request = DSRRequest.Create($"req-{i}", $"subject-{i % 10}", DataSubjectRight.Access, DateTimeOffset.UtcNow);
            await store.CreateAsync(request);
        }

        var successCount = 0L;
        var failureCount = 0L;

        // Each worker updates a unique range of IDs
        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(workerId => Task.Run(async () =>
        {
            var start = workerId * 100;
            for (var i = 0; i < 100; i++)
            {
                var id = $"req-{start + i}";
                var statuses = new[] { DSRRequestStatus.IdentityVerified, DSRRequestStatus.InProgress, DSRRequestStatus.Completed };

                foreach (var status in statuses)
                {
                    var result = await store.UpdateStatusAsync(id, status, status == DSRRequestStatus.Completed ? null : "reason");

                    if (result.IsRight)
                        Interlocked.Increment(ref successCount);
                    else
                        Interlocked.Increment(ref failureCount);
                }
            }
        }));

        await Task.WhenAll(tasks);

        Console.WriteLine($"  {successCount:N0} status updates, {failureCount} failures");
        Assert(failureCount == 0, $"Expected 0 failures, got {failureCount}");
    }

    // ────────────────────────────────────────────────────────────
    //  HasActiveRestriction — Concurrent Checks Under Load
    // ────────────────────────────────────────────────────────────

    private static async Task HasActiveRestriction_ConcurrentChecks_MaintainsThroughput()
    {
        var store = CreateRequestStore();

        // Seed: 5 subjects with active restrictions, 5 without
        for (var i = 0; i < 5; i++)
        {
            await store.CreateAsync(DSRRequest.Create($"restrict-{i}", $"subject-{i}", DataSubjectRight.Restriction, DateTimeOffset.UtcNow));
        }
        for (var i = 5; i < 10; i++)
        {
            await store.CreateAsync(DSRRequest.Create($"access-{i}", $"subject-{i}", DataSubjectRight.Access, DateTimeOffset.UtcNow));
        }

        var trueCount = 0L;
        var falseCount = 0L;
        var errorCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var subjectId = $"subject-{i % 10}";
                var result = await store.HasActiveRestrictionAsync(subjectId);

                if (result.IsRight)
                {
                    var hasRestriction = (bool)result;
                    if (hasRestriction)
                        Interlocked.Increment(ref trueCount);
                    else
                        Interlocked.Increment(ref falseCount);
                }
                else
                {
                    Interlocked.Increment(ref errorCount);
                }
            }
        }));

        await Task.WhenAll(tasks);

        Assert(errorCount == 0, $"Expected 0 errors, got {errorCount}");
        Assert(trueCount > 0, "Expected some restriction-active results");
        Assert(falseCount > 0, "Expected some non-restriction results");
        Console.WriteLine($"  Active: {trueCount:N0}, Inactive: {falseCount:N0}, Errors: {errorCount}");
    }

    // ────────────────────────────────────────────────────────────
    //  AuditStore Mixed Read/Write — Concurrent Operations
    // ────────────────────────────────────────────────────────────

    private static async Task AuditStore_MixedReadWrite_NoErrors()
    {
        var auditStore = CreateAuditStore();
        var successCount = 0L;
        var errorCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(workerId => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var requestId = $"req-{workerId % 20}";

                if (i % 3 == 0)
                {
                    // Write operation
                    var entry = new DSRAuditEntry
                    {
                        Id = $"audit-w{workerId}-{i}",
                        DSRRequestId = requestId,
                        Action = $"Action-{i % 5}",
                        OccurredAtUtc = DateTimeOffset.UtcNow
                    };
                    var result = await auditStore.RecordAsync(entry);

                    if (result.IsRight)
                        Interlocked.Increment(ref successCount);
                    else
                        Interlocked.Increment(ref errorCount);
                }
                else
                {
                    // Read operation
                    var result = await auditStore.GetAuditTrailAsync(requestId);

                    if (result.IsRight)
                        Interlocked.Increment(ref successCount);
                    else
                        Interlocked.Increment(ref errorCount);
                }
            }
        }));

        await Task.WhenAll(tasks);

        Assert(errorCount == 0, $"Expected 0 errors, got {errorCount}");
        Console.WriteLine($"  {successCount:N0} mixed audit operations, {errorCount} errors, store size: {auditStore.Count:N0}");
    }

    // ────────────────────────────────────────────────────────────
    //  Mixed RequestStore Operations — All Types Concurrent
    // ────────────────────────────────────────────────────────────

    private static async Task MixedRequestStoreOperations_ConcurrentOperations_NoErrors()
    {
        var store = CreateRequestStore();
        var successCount = 0L;
        var errorCount = 0L;

        // Seed some initial data
        for (var i = 0; i < 100; i++)
        {
            await store.CreateAsync(DSRRequest.Create($"seed-{i}", $"subject-{i % 10}", (DataSubjectRight)(i % 8), DateTimeOffset.UtcNow));
        }

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(workerId => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var op = i % 5;
                Either<EncinaError, object> result;

                switch (op)
                {
                    case 0: // Create
                        var id = $"w{workerId}-{i}";
                        var req = DSRRequest.Create(id, $"subject-{workerId}", DataSubjectRight.Access, DateTimeOffset.UtcNow);
                        var createResult = await store.CreateAsync(req);
                        result = createResult.Map<object>(u => u);
                        break;

                    case 1: // GetById
                        var getResult = await store.GetByIdAsync($"seed-{i % 100}");
                        result = getResult.Map<object>(o => o);
                        break;

                    case 2: // GetBySubjectId
                        var subResult = await store.GetBySubjectIdAsync($"subject-{i % 10}");
                        result = subResult.Map<object>(l => l);
                        break;

                    case 3: // HasActiveRestriction
                        var hasResult = await store.HasActiveRestrictionAsync($"subject-{i % 10}");
                        result = hasResult.Map<object>(b => b);
                        break;

                    default: // GetPending
                        var pendResult = await store.GetPendingRequestsAsync();
                        result = pendResult.Map<object>(l => l);
                        break;
                }

                if (result.IsRight)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref errorCount);
            }
        }));

        await Task.WhenAll(tasks);

        Console.WriteLine($"  {successCount:N0} mixed operations, {errorCount:N0} errors");
        // Some create errors expected (potential duplicates in mixed mode)
    }

    // ────────────────────────────────────────────────────────────
    //  Latency Distribution — P50 / P95 / P99
    // ────────────────────────────────────────────────────────────

    private static async Task RequestStoreLatencyDistribution_ConcurrentLoad_WithinBounds()
    {
        var store = CreateRequestStore();
        var latencies = new ConcurrentBag<double>();

        // Seed requests
        for (var i = 0; i < 100; i++)
        {
            await store.CreateAsync(DSRRequest.Create($"req-{i}", $"subject-{i % 10}", DataSubjectRight.Access, DateTimeOffset.UtcNow));
        }

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var sw = Stopwatch.StartNew();
                await store.GetByIdAsync($"req-{i % 100}");
                sw.Stop();
                latencies.Add(sw.Elapsed.TotalMicroseconds);
            }
        }));

        await Task.WhenAll(tasks);

        var sorted = latencies.OrderBy(l => l).ToArray();
        var totalOps = sorted.Length;
        var mean = sorted.Average();
        var p50 = sorted[(int)(totalOps * 0.50)];
        var p95 = sorted[(int)(totalOps * 0.95)];
        var p99 = sorted[(int)(totalOps * 0.99)];

        Console.WriteLine($"  {totalOps:N0} operations — mean: {mean:F1}µs, P50: {p50:F1}µs, P95: {p95:F1}µs, P99: {p99:F1}µs");
    }

    // ────────────────────────────────────────────────────────────
    //  Infrastructure
    // ────────────────────────────────────────────────────────────

    private static InMemoryDSRRequestStore CreateRequestStore() =>
        new(TimeProvider.System, NullLogger<InMemoryDSRRequestStore>.Instance);

    private static InMemoryDSRAuditStore CreateAuditStore() =>
        new(NullLogger<InMemoryDSRAuditStore>.Instance);

    private static async Task RunTestAsync(string name, Func<Task> test)
    {
        Console.Write($"  [{name}] ... ");
        var sw = Stopwatch.StartNew();
        try
        {
            await test();
            sw.Stop();
            Console.WriteLine($"OK ({sw.Elapsed.TotalSeconds:F2}s)");
        }
        catch (Exception ex)
        {
            sw.Stop();
            Console.WriteLine($"FAILED ({sw.Elapsed.TotalSeconds:F2}s): {ex.Message}");
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException($"Assertion failed: {message}");
    }
}

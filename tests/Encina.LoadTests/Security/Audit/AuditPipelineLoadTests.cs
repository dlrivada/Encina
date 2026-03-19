using System.Collections.Concurrent;
using System.Diagnostics;

using Encina.Security.Audit;

namespace Encina.LoadTests.Security.Audit;

/// <summary>
/// Load tests for the Audit pipeline — measures throughput, latency, memory impact and GC
/// pressure under concurrent audit recording, querying, burst, and retention scenarios.
/// </summary>
/// <remarks>
/// <para>
/// The audit pipeline is the hot path for every audited command in compliance-grade
/// applications (SOX, NIS2, GDPR). These tests validate that <see cref="InMemoryAuditStore"/>
/// can sustain high concurrency without contention or memory leaks.
/// </para>
/// <para>
/// Run from the LoadTests project:
/// <code>
/// dotnet run --project tests/Encina.LoadTests -- --scenario audit-pipeline --workers 8 --duration 30
/// </code>
/// </para>
/// <para>
/// Metrics reported per scenario:
/// <list type="bullet">
/// <item>Throughput: operations/sec at the configured concurrency level</item>
/// <item>Latency: P50, P95, P99 per operation</item>
/// <item>Memory: GC Gen0/Gen1/Gen2 collections, allocation delta</item>
/// <item>Errors: count of Left results from the store</item>
/// </list>
/// </para>
/// </remarks>
public static class AuditPipelineLoadTests
{
    /// <summary>
    /// Runs all audit pipeline load test scenarios sequentially.
    /// </summary>
    public static async Task RunAllAsync(int workerCount = 8, TimeSpan? duration = null, int payloadSizeBytes = 1024)
    {
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("  Audit Pipeline Load Tests");
        Console.WriteLine("========================================");
        Console.WriteLine();

        await RunConcurrentWritesAsync(workerCount, duration, payloadSizeBytes).ConfigureAwait(false);
        Console.WriteLine();

        await RunWriteAndQueryAsync(workerCount, duration, payloadSizeBytes).ConfigureAwait(false);
        Console.WriteLine();

        await RunBurstWritesAsync(payloadSizeBytes).ConfigureAwait(false);
        Console.WriteLine();

        await RunRetentionUnderLoadAsync(workerCount, duration, payloadSizeBytes).ConfigureAwait(false);
    }

    /// <summary>
    /// Scenario 1: Multiple workers writing audit entries simultaneously via
    /// <see cref="IAuditStore.RecordAsync"/>. Measures pure write throughput and
    /// contention on the underlying <see cref="ConcurrentDictionary{TKey,TValue}"/>.
    /// </summary>
    /// <param name="workerCount">Number of concurrent workers simulating parallel command execution.</param>
    /// <param name="duration">How long to run the test.</param>
    /// <param name="payloadSizeBytes">Size of the simulated request payload per entry.</param>
    public static async Task RunConcurrentWritesAsync(
        int workerCount = 8,
        TimeSpan? duration = null,
        int payloadSizeBytes = 1024)
    {
        var testDuration = duration ?? TimeSpan.FromSeconds(30);

        Console.WriteLine("=== Audit Pipeline Load Test ===");
        Console.WriteLine("Scenario: ConcurrentWrites");
        Console.WriteLine($"Workers: {workerCount} | Duration: {testDuration.TotalSeconds}s");
        Console.WriteLine();

        var store = new InMemoryAuditStore();
        var latencies = new ConcurrentBag<double>();
        long totalOperations = 0;
        long totalErrors = 0;
        var payload = new string('X', payloadSizeBytes);

        var gcGen0Before = GC.CollectionCount(0);
        var gcGen1Before = GC.CollectionCount(1);
        var gcGen2Before = GC.CollectionCount(2);
        var memBefore = GC.GetTotalMemory(true);

        using var cts = new CancellationTokenSource(testDuration);
        var sw = Stopwatch.StartNew();

        var workers = Enumerable
            .Range(0, workerCount)
            .Select(_ => Task.Run(async () =>
            {
                var localSw = new Stopwatch();
                while (!cts.IsCancellationRequested)
                {
                    var entry = CreateAuditEntry(payload);

                    localSw.Restart();
                    var result = await store.RecordAsync(entry, cts.Token).ConfigureAwait(false);
                    localSw.Stop();

                    if (result.IsRight)
                    {
                        Interlocked.Increment(ref totalOperations);
                        latencies.Add(localSw.Elapsed.TotalMilliseconds);
                    }
                    else
                    {
                        Interlocked.Increment(ref totalErrors);
                    }
                }
            }))
            .ToArray();

        await Task.WhenAll(workers).ConfigureAwait(false);
        sw.Stop();

        var memAfter = GC.GetTotalMemory(false);
        PrintResults(
            "ConcurrentWrites",
            workerCount,
            testDuration,
            totalOperations,
            totalErrors,
            sw,
            latencies,
            memBefore,
            memAfter,
            gcGen0Before,
            gcGen1Before,
            gcGen2Before);
    }

    /// <summary>
    /// Scenario 2: Mixed read/write workload — half the workers write entries while
    /// the other half query. Validates that reads don't block writes and vice versa.
    /// </summary>
    /// <param name="workerCount">Total number of workers (split evenly between read and write).</param>
    /// <param name="duration">How long to run the test.</param>
    /// <param name="payloadSizeBytes">Size of the simulated request payload per entry.</param>
    public static async Task RunWriteAndQueryAsync(
        int workerCount = 8,
        TimeSpan? duration = null,
        int payloadSizeBytes = 1024)
    {
        var testDuration = duration ?? TimeSpan.FromSeconds(30);
        var writeWorkers = Math.Max(1, workerCount / 2);
        var readWorkers = Math.Max(1, workerCount - writeWorkers);

        Console.WriteLine("=== Audit Pipeline Load Test ===");
        Console.WriteLine("Scenario: WriteAndQuery");
        Console.WriteLine($"Writers: {writeWorkers} | Readers: {readWorkers} | Duration: {testDuration.TotalSeconds}s");
        Console.WriteLine();

        var store = new InMemoryAuditStore();
        var writeLatencies = new ConcurrentBag<double>();
        var readLatencies = new ConcurrentBag<double>();
        long totalWrites = 0;
        long totalReads = 0;
        long totalErrors = 0;
        var payload = new string('X', payloadSizeBytes);

        // Pre-seed some data for queries
        for (var i = 0; i < 1000; i++)
        {
            await store.RecordAsync(CreateAuditEntry(payload), CancellationToken.None).ConfigureAwait(false);
        }

        var gcGen0Before = GC.CollectionCount(0);
        var gcGen1Before = GC.CollectionCount(1);
        var gcGen2Before = GC.CollectionCount(2);
        var memBefore = GC.GetTotalMemory(true);

        using var cts = new CancellationTokenSource(testDuration);
        var sw = Stopwatch.StartNew();

        var writers = Enumerable
            .Range(0, writeWorkers)
            .Select(_ => Task.Run(async () =>
            {
                var localSw = new Stopwatch();
                while (!cts.IsCancellationRequested)
                {
                    var entry = CreateAuditEntry(payload);

                    localSw.Restart();
                    var result = await store.RecordAsync(entry, cts.Token).ConfigureAwait(false);
                    localSw.Stop();

                    if (result.IsRight)
                    {
                        Interlocked.Increment(ref totalWrites);
                        writeLatencies.Add(localSw.Elapsed.TotalMilliseconds);
                    }
                    else
                    {
                        Interlocked.Increment(ref totalErrors);
                    }
                }
            }))
            .ToArray();

        var readers = Enumerable
            .Range(0, readWorkers)
            .Select(_ => Task.Run(async () =>
            {
                var localSw = new Stopwatch();
                while (!cts.IsCancellationRequested)
                {
                    var query = new AuditQuery
                    {
                        EntityType = "Order",
                        PageSize = 50
                    };

                    localSw.Restart();
                    var result = await store.QueryAsync(query, cts.Token).ConfigureAwait(false);
                    localSw.Stop();

                    if (result.IsRight)
                    {
                        Interlocked.Increment(ref totalReads);
                        readLatencies.Add(localSw.Elapsed.TotalMilliseconds);
                    }
                    else
                    {
                        Interlocked.Increment(ref totalErrors);
                    }
                }
            }))
            .ToArray();

        await Task.WhenAll(writers.Concat(readers)).ConfigureAwait(false);
        sw.Stop();

        var memAfter = GC.GetTotalMemory(false);

        Console.WriteLine("Results:");
        Console.WriteLine($"  Total Writes:       {totalWrites:N0}");
        Console.WriteLine($"  Total Reads:        {totalReads:N0}");
        Console.WriteLine($"  Total Operations:   {totalWrites + totalReads:N0}");
        Console.WriteLine($"  Write Throughput:   {totalWrites / sw.Elapsed.TotalSeconds:N1} ops/sec");
        Console.WriteLine($"  Read Throughput:    {totalReads / sw.Elapsed.TotalSeconds:N1} ops/sec");
        Console.WriteLine($"  Errors:             {totalErrors:N0}");

        PrintLatencyBlock("Write Latency", writeLatencies);
        PrintLatencyBlock("Read Latency", readLatencies);
        PrintMemoryBlock(memBefore, memAfter, gcGen0Before, gcGen1Before, gcGen2Before);
    }

    /// <summary>
    /// Scenario 3: Sudden spike of entries — a large batch is written as fast as possible
    /// to simulate a burst of audited commands (e.g., batch import, campaign launch).
    /// </summary>
    /// <param name="payloadSizeBytes">Size of the simulated request payload per entry.</param>
    /// <param name="burstSize">Total number of entries to write in the burst.</param>
    /// <param name="burstWorkers">Number of concurrent workers for the burst.</param>
    public static async Task RunBurstWritesAsync(
        int payloadSizeBytes = 1024,
        int burstSize = 50_000,
        int burstWorkers = 16)
    {
        Console.WriteLine("=== Audit Pipeline Load Test ===");
        Console.WriteLine("Scenario: BurstWrites");
        Console.WriteLine($"Burst size: {burstSize:N0} | Workers: {burstWorkers}");
        Console.WriteLine();

        var store = new InMemoryAuditStore();
        var latencies = new ConcurrentBag<double>();
        long totalOperations = 0;
        long totalErrors = 0;
        var payload = new string('X', payloadSizeBytes);
        long remaining = burstSize;

        var gcGen0Before = GC.CollectionCount(0);
        var gcGen1Before = GC.CollectionCount(1);
        var gcGen2Before = GC.CollectionCount(2);
        var memBefore = GC.GetTotalMemory(true);

        var sw = Stopwatch.StartNew();

        var workers = Enumerable
            .Range(0, burstWorkers)
            .Select(_ => Task.Run(async () =>
            {
                var localSw = new Stopwatch();
                while (Interlocked.Decrement(ref remaining) >= 0)
                {
                    var entry = CreateAuditEntry(payload);

                    localSw.Restart();
                    var result = await store.RecordAsync(entry, CancellationToken.None).ConfigureAwait(false);
                    localSw.Stop();

                    if (result.IsRight)
                    {
                        Interlocked.Increment(ref totalOperations);
                        latencies.Add(localSw.Elapsed.TotalMilliseconds);
                    }
                    else
                    {
                        Interlocked.Increment(ref totalErrors);
                    }
                }
            }))
            .ToArray();

        await Task.WhenAll(workers).ConfigureAwait(false);
        sw.Stop();

        var memAfter = GC.GetTotalMemory(false);
        PrintResults(
            "BurstWrites",
            burstWorkers,
            sw.Elapsed,
            totalOperations,
            totalErrors,
            sw,
            latencies,
            memBefore,
            memAfter,
            gcGen0Before,
            gcGen1Before,
            gcGen2Before);
    }

    /// <summary>
    /// Scenario 4: Purge entries while writes are happening — validates that
    /// <see cref="IAuditStore.PurgeEntriesAsync"/> does not block or corrupt concurrent writes.
    /// </summary>
    /// <param name="workerCount">Number of concurrent write workers.</param>
    /// <param name="duration">How long to run the test.</param>
    /// <param name="payloadSizeBytes">Size of the simulated request payload per entry.</param>
    public static async Task RunRetentionUnderLoadAsync(
        int workerCount = 8,
        TimeSpan? duration = null,
        int payloadSizeBytes = 1024)
    {
        var testDuration = duration ?? TimeSpan.FromSeconds(30);

        Console.WriteLine("=== Audit Pipeline Load Test ===");
        Console.WriteLine("Scenario: RetentionUnderLoad");
        Console.WriteLine($"Writers: {workerCount} | Purge interval: 2s | Duration: {testDuration.TotalSeconds}s");
        Console.WriteLine();

        var store = new InMemoryAuditStore();
        var writeLatencies = new ConcurrentBag<double>();
        var purgeLatencies = new ConcurrentBag<double>();
        long totalWrites = 0;
        long totalPurged = 0;
        long totalPurgeCycles = 0;
        long totalErrors = 0;
        var payload = new string('X', payloadSizeBytes);

        var gcGen0Before = GC.CollectionCount(0);
        var gcGen1Before = GC.CollectionCount(1);
        var gcGen2Before = GC.CollectionCount(2);
        var memBefore = GC.GetTotalMemory(true);

        using var cts = new CancellationTokenSource(testDuration);
        var sw = Stopwatch.StartNew();

        // Write workers
        var writers = Enumerable
            .Range(0, workerCount)
            .Select(_ => Task.Run(async () =>
            {
                var localSw = new Stopwatch();
                while (!cts.IsCancellationRequested)
                {
                    var entry = CreateAuditEntry(payload);

                    localSw.Restart();
                    var result = await store.RecordAsync(entry, cts.Token).ConfigureAwait(false);
                    localSw.Stop();

                    if (result.IsRight)
                    {
                        Interlocked.Increment(ref totalWrites);
                        writeLatencies.Add(localSw.Elapsed.TotalMilliseconds);
                    }
                    else
                    {
                        Interlocked.Increment(ref totalErrors);
                    }
                }
            }))
            .ToArray();

        // Purge worker — runs every 2 seconds, purging entries older than 5 seconds
        var purgeTask = Task.Run(async () =>
        {
            var localSw = new Stopwatch();
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cts.IsCancellationRequested)
                {
                    break;
                }

                var cutoff = DateTime.UtcNow.AddSeconds(-5);

                localSw.Restart();
                var result = await store.PurgeEntriesAsync(cutoff, cts.Token).ConfigureAwait(false);
                localSw.Stop();

                result.IfRight(purged =>
                {
                    Interlocked.Add(ref totalPurged, purged);
                    Interlocked.Increment(ref totalPurgeCycles);
                    purgeLatencies.Add(localSw.Elapsed.TotalMilliseconds);
                });
            }
        });

        await Task.WhenAll(writers.Append(purgeTask)).ConfigureAwait(false);
        sw.Stop();

        var memAfter = GC.GetTotalMemory(false);

        Console.WriteLine("Results:");
        Console.WriteLine($"  Total Writes:       {totalWrites:N0}");
        Console.WriteLine($"  Write Throughput:   {totalWrites / sw.Elapsed.TotalSeconds:N1} ops/sec");
        Console.WriteLine($"  Total Purged:       {totalPurged:N0}");
        Console.WriteLine($"  Purge Cycles:       {totalPurgeCycles:N0}");
        Console.WriteLine($"  Store Size (final): {store.Count:N0}");
        Console.WriteLine($"  Errors:             {totalErrors:N0}");

        PrintLatencyBlock("Write Latency", writeLatencies);
        PrintLatencyBlock("Purge Latency", purgeLatencies);
        PrintMemoryBlock(memBefore, memAfter, gcGen0Before, gcGen1Before, gcGen2Before);
    }

    private static void PrintResults(
        string scenario,
        int workers,
        TimeSpan duration,
        long totalOperations,
        long totalErrors,
        Stopwatch sw,
        ConcurrentBag<double> latencies,
        long memBefore,
        long memAfter,
        int gcGen0Before,
        int gcGen1Before,
        int gcGen2Before)
    {
        Console.WriteLine("Results:");
        Console.WriteLine($"  Total Operations:   {totalOperations:N0}");
        Console.WriteLine($"  Throughput:         {totalOperations / sw.Elapsed.TotalSeconds:N1} ops/sec");
        Console.WriteLine($"  Errors:             {totalErrors:N0}");

        PrintLatencyBlock("Latency", latencies);
        PrintMemoryBlock(memBefore, memAfter, gcGen0Before, gcGen1Before, gcGen2Before);
    }

    private static void PrintLatencyBlock(string label, ConcurrentBag<double> latencies)
    {
        var sorted = latencies.OrderBy(l => l).ToArray();
        if (sorted.Length == 0)
        {
            return;
        }

        Console.WriteLine($"  {label}:");
        Console.WriteLine($"    P50:  {Percentile(sorted, 50):F2} ms");
        Console.WriteLine($"    P95:  {Percentile(sorted, 95):F2} ms");
        Console.WriteLine($"    P99:  {Percentile(sorted, 99):F2} ms");
        Console.WriteLine($"    Max:  {sorted[^1]:F2} ms");
        Console.WriteLine($"    Avg:  {sorted.Average():F2} ms");
    }

    private static void PrintMemoryBlock(
        long memBefore,
        long memAfter,
        int gcGen0Before,
        int gcGen1Before,
        int gcGen2Before)
    {
        var deltaBytes = memAfter - memBefore;
        var deltaMb = deltaBytes / (1024.0 * 1024.0);
        Console.WriteLine($"  Memory Delta:       {(deltaMb >= 0 ? "+" : "")}{deltaMb:F1} MB");
        Console.WriteLine($"  GC Gen0: {GC.CollectionCount(0) - gcGen0Before} | Gen1: {GC.CollectionCount(1) - gcGen1Before} | Gen2: {GC.CollectionCount(2) - gcGen2Before}");
    }

    private static double Percentile(double[] sorted, double percentile)
    {
        var index = (int)Math.Ceiling(percentile / 100.0 * sorted.Length) - 1;
        return sorted[Math.Clamp(index, 0, sorted.Length - 1)];
    }

    private static AuditEntry CreateAuditEntry(string payload) => new()
    {
        Id = Guid.NewGuid(),
        CorrelationId = Guid.NewGuid().ToString("N"),
        UserId = $"user-{Random.Shared.Next(1, 100):D5}",
        TenantId = $"tenant-{Random.Shared.Next(1, 10)}",
        Action = Random.Shared.Next(4) switch
        {
            0 => "Create",
            1 => "Update",
            2 => "Delete",
            _ => "Get"
        },
        EntityType = "Order",
        EntityId = $"ORD-{Random.Shared.Next(1, 999_999)}",
        Outcome = AuditOutcome.Success,
        TimestampUtc = DateTime.UtcNow,
        StartedAtUtc = DateTimeOffset.UtcNow.AddMilliseconds(-Random.Shared.Next(1, 50)),
        CompletedAtUtc = DateTimeOffset.UtcNow,
        IpAddress = $"10.0.{Random.Shared.Next(0, 255)}.{Random.Shared.Next(1, 254)}",
        UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)",
        RequestPayloadHash = "sha256:loadtest",
        RequestPayload = payload,
        Metadata = new Dictionary<string, object?>
        {
            ["region"] = "eu-west-1",
            ["scenario"] = "load-test"
        }
    };
}

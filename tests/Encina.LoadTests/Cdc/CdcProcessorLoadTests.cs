using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using Encina.Cdc;
using Encina.Cdc.Abstractions;

using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.LoadTests.Cdc;

/// <summary>
/// Load tests for the CDC processing pipeline — measures throughput, latency, and memory impact
/// under high-volume change event streams. Simulates sustained throughput, burst processing,
/// and mixed-operation scenarios using mock CDC connectors and handlers.
/// </summary>
/// <remarks>
/// <para>
/// The CDC processor is a critical BackgroundService that must sustain high-throughput
/// event streams without backpressure or memory leaks. These tests validate the dispatch
/// hot path by bypassing the <c>CdcProcessor</c> BackgroundService loop and driving
/// the <see cref="ICdcDispatcher.DispatchAsync"/> equivalent directly.
/// </para>
/// <para>
/// Run from the LoadTests project:
/// <code>
/// dotnet run --project tests/Encina.LoadTests -- --scenario cdc-processor --workers 8 --duration 30
/// </code>
/// </para>
/// <para>
/// Metrics reported per scenario:
/// <list type="bullet">
/// <item>Throughput: events/sec processed</item>
/// <item>Latency: P50, P95, P99 per event dispatch</item>
/// <item>Memory: GC Gen0/Gen1/Gen2 collections, allocation delta</item>
/// <item>Errors: count of failed dispatches</item>
/// </list>
/// </para>
/// </remarks>
public static class CdcProcessorLoadTests
{
    /// <summary>
    /// DocRef identifier for load-test documentation traceability (see ADR-025).
    /// </summary>
    public const string DocRef = "load:cdc";

    /// <summary>
    /// Runs all CDC processor load test scenarios sequentially.
    /// </summary>
    public static async Task RunAllAsync(int workerCount = 8, TimeSpan? duration = null)
    {
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("  CDC Processor Load Tests");
        Console.WriteLine("========================================");
        Console.WriteLine();

        await RunHighThroughputAsync(workerCount, duration).ConfigureAwait(false);
        Console.WriteLine();

        await RunBurstProcessingAsync().ConfigureAwait(false);
        Console.WriteLine();

        await RunMixedOperationsAsync(workerCount, duration).ConfigureAwait(false);
    }

    /// <summary>
    /// Scenario 1: Sustained event stream — multiple workers consume change events
    /// from a mock connector and dispatch to a mock handler. Targets 10K+ events/sec.
    /// </summary>
    /// <param name="workerCount">Number of concurrent dispatch workers.</param>
    /// <param name="duration">How long to run the test.</param>
    public static async Task RunHighThroughputAsync(int workerCount = 8, TimeSpan? duration = null)
    {
        var testDuration = duration ?? TimeSpan.FromSeconds(30);

        Console.WriteLine("=== CDC Processor Load Test ===");
        Console.WriteLine("Scenario: HighThroughput");
        Console.WriteLine($"Workers: {workerCount} | Duration: {testDuration.TotalSeconds}s");
        Console.WriteLine();

        var handler = new LatencyTrackingHandler();
        var latencies = new ConcurrentBag<double>();
        long totalEvents = 0;
        long totalErrors = 0;

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
                    var changeEvent = CreateChangeEvent(ChangeOperation.Insert);
                    var context = CreateContext(changeEvent);

                    localSw.Restart();
                    var result = await handler.HandleInsertAsync(
                        changeEvent.After!, context).ConfigureAwait(false);
                    localSw.Stop();

                    if (result.IsRight)
                    {
                        Interlocked.Increment(ref totalEvents);
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
            "HighThroughput",
            workerCount,
            totalEvents,
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
    /// Scenario 2: Spike of 50K events processed as fast as possible to simulate
    /// an initial snapshot load or a sudden burst from a replication slot.
    /// </summary>
    /// <param name="burstSize">Total number of events in the burst.</param>
    /// <param name="burstWorkers">Number of concurrent workers for the burst.</param>
    public static async Task RunBurstProcessingAsync(int burstSize = 50_000, int burstWorkers = 16)
    {
        Console.WriteLine("=== CDC Processor Load Test ===");
        Console.WriteLine("Scenario: BurstProcessing");
        Console.WriteLine($"Burst size: {burstSize:N0} | Workers: {burstWorkers}");
        Console.WriteLine();

        var handler = new LatencyTrackingHandler();
        var latencies = new ConcurrentBag<double>();
        long totalEvents = 0;
        long totalErrors = 0;
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
                    var changeEvent = CreateChangeEvent(ChangeOperation.Insert);
                    var context = CreateContext(changeEvent);

                    localSw.Restart();
                    var result = await handler.HandleInsertAsync(
                        changeEvent.After!, context).ConfigureAwait(false);
                    localSw.Stop();

                    if (result.IsRight)
                    {
                        Interlocked.Increment(ref totalEvents);
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
            "BurstProcessing",
            burstWorkers,
            totalEvents,
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
    /// Scenario 3: Mixed Insert/Update/Delete operations — simulates a realistic
    /// production workload where changes are a mix of operation types.
    /// </summary>
    /// <param name="workerCount">Number of concurrent dispatch workers.</param>
    /// <param name="duration">How long to run the test.</param>
    public static async Task RunMixedOperationsAsync(int workerCount = 8, TimeSpan? duration = null)
    {
        var testDuration = duration ?? TimeSpan.FromSeconds(30);

        Console.WriteLine("=== CDC Processor Load Test ===");
        Console.WriteLine("Scenario: MixedOperations");
        Console.WriteLine($"Workers: {workerCount} | Duration: {testDuration.TotalSeconds}s");
        Console.WriteLine();

        var handler = new LatencyTrackingHandler();
        var latencies = new ConcurrentBag<double>();
        long totalInserts = 0;
        long totalUpdates = 0;
        long totalDeletes = 0;
        long totalErrors = 0;

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
                    // Realistic distribution: 40% Insert, 40% Update, 20% Delete
                    var opRoll = Random.Shared.Next(100);
                    var operation = opRoll switch
                    {
                        < 40 => ChangeOperation.Insert,
                        < 80 => ChangeOperation.Update,
                        _ => ChangeOperation.Delete
                    };

                    var changeEvent = CreateChangeEvent(operation);
                    var context = CreateContext(changeEvent);

                    localSw.Restart();
                    var result = operation switch
                    {
                        ChangeOperation.Insert => await handler.HandleInsertAsync(
                            changeEvent.After!, context).ConfigureAwait(false),
                        ChangeOperation.Update => await handler.HandleUpdateAsync(
                            changeEvent.Before!, changeEvent.After!, context).ConfigureAwait(false),
                        ChangeOperation.Delete => await handler.HandleDeleteAsync(
                            changeEvent.Before!, context).ConfigureAwait(false),
                        _ => Right<EncinaError, Unit>(unit)
                    };
                    localSw.Stop();

                    if (result.IsRight)
                    {
                        latencies.Add(localSw.Elapsed.TotalMilliseconds);
                        switch (operation)
                        {
                            case ChangeOperation.Insert:
                                Interlocked.Increment(ref totalInserts);
                                break;
                            case ChangeOperation.Update:
                                Interlocked.Increment(ref totalUpdates);
                                break;
                            case ChangeOperation.Delete:
                                Interlocked.Increment(ref totalDeletes);
                                break;
                        }
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

        var totalEvents = totalInserts + totalUpdates + totalDeletes;
        var memAfter = GC.GetTotalMemory(false);

        Console.WriteLine("Results:");
        Console.WriteLine($"  Total Events:       {totalEvents:N0}");
        Console.WriteLine($"  Throughput:         {totalEvents / sw.Elapsed.TotalSeconds:N1} events/sec");
        Console.WriteLine($"  Inserts:            {totalInserts:N0} ({100.0 * totalInserts / Math.Max(totalEvents, 1):F1}%)");
        Console.WriteLine($"  Updates:            {totalUpdates:N0} ({100.0 * totalUpdates / Math.Max(totalEvents, 1):F1}%)");
        Console.WriteLine($"  Deletes:            {totalDeletes:N0} ({100.0 * totalDeletes / Math.Max(totalEvents, 1):F1}%)");
        Console.WriteLine($"  Errors:             {totalErrors:N0}");

        PrintLatencyBlock("Latency", latencies);
        PrintMemoryBlock(memBefore, memAfter, gcGen0Before, gcGen1Before, gcGen2Before);
    }

    #region Helpers

    private static void PrintResults(
        string scenario,
        int workers,
        long totalEvents,
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
        Console.WriteLine($"  Total Events:       {totalEvents:N0}");
        Console.WriteLine($"  Throughput:         {totalEvents / sw.Elapsed.TotalSeconds:N1} events/sec");
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

    private static ChangeEvent CreateChangeEvent(ChangeOperation operation)
    {
        var entityId = Random.Shared.Next(1, 999_999);
        var entity = new LoadTestEntity(
            entityId,
            $"Entity-{entityId}",
            DateTime.UtcNow,
            Random.Shared.Next(1, 100) * 10.0m);

        var before = operation is ChangeOperation.Update or ChangeOperation.Delete
            ? entity with { Name = $"Entity-{entityId}-old", Amount = entity.Amount - 5.0m }
            : null;

        var after = operation is ChangeOperation.Insert or ChangeOperation.Update
            ? entity
            : null;

        return new ChangeEvent(
            TableName: "orders",
            Operation: operation,
            Before: before,
            After: after,
            Metadata: new ChangeMetadata(
                Position: new SequentialPosition(Interlocked.Increment(ref _positionCounter)),
                CapturedAtUtc: DateTime.UtcNow,
                TransactionId: Guid.NewGuid().ToString("N"),
                SourceDatabase: "loadtest_db",
                SourceSchema: "public"));
    }

    private static ChangeContext CreateContext(ChangeEvent changeEvent)
        => new(changeEvent.TableName, changeEvent.Metadata, CancellationToken.None);

    private static long _positionCounter;

    #endregion

    #region Test Infrastructure

    /// <summary>
    /// Lightweight entity used for CDC load testing.
    /// </summary>
    private sealed record LoadTestEntity(int Id, string Name, DateTime CreatedAtUtc, decimal Amount);

    /// <summary>
    /// Sequential position implementation for load testing.
    /// </summary>
    private sealed class SequentialPosition(long sequence) : CdcPosition
    {
        public long Sequence { get; } = sequence;

        public override int CompareTo(CdcPosition? other)
        {
            if (other is SequentialPosition otherSeq)
            {
                return Sequence.CompareTo(otherSeq.Sequence);
            }

            return 0;
        }

        public override byte[] ToBytes() => BitConverter.GetBytes(Sequence);
        public override string ToString() => $"seq:{Sequence}";
    }

    /// <summary>
    /// Mock handler that tracks processing latency without performing real work.
    /// Implements <see cref="IChangeEventHandler{TEntity}"/> for <see cref="object"/>
    /// to accept any entity type.
    /// </summary>
    private sealed class LatencyTrackingHandler : IChangeEventHandler<object>
    {
        private long _insertCount;
        private long _updateCount;
        private long _deleteCount;

        public long InsertCount => Interlocked.Read(ref _insertCount);
        public long UpdateCount => Interlocked.Read(ref _updateCount);
        public long DeleteCount => Interlocked.Read(ref _deleteCount);

        public ValueTask<Either<EncinaError, Unit>> HandleInsertAsync(
            object entity, ChangeContext context)
        {
            Interlocked.Increment(ref _insertCount);
            return new(Right<EncinaError, Unit>(unit));
        }

        public ValueTask<Either<EncinaError, Unit>> HandleUpdateAsync(
            object before, object after, ChangeContext context)
        {
            Interlocked.Increment(ref _updateCount);
            return new(Right<EncinaError, Unit>(unit));
        }

        public ValueTask<Either<EncinaError, Unit>> HandleDeleteAsync(
            object entity, ChangeContext context)
        {
            Interlocked.Increment(ref _deleteCount);
            return new(Right<EncinaError, Unit>(unit));
        }
    }

    #endregion
}

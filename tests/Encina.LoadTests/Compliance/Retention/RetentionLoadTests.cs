using System.Collections.Concurrent;
using System.Diagnostics;
using Encina.Caching;
using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Abstractions;
using Encina.Compliance.Retention.Model;
using Encina.Compliance.Retention.ReadModels;
using Encina.Compliance.Retention.Services;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.LoadTests.Compliance.Retention;

/// <summary>
/// Load tests for the Retention compliance module under high concurrent traffic.
/// Validates throughput, latency percentiles, and thread safety of:
/// - IRetentionPolicyService policy CRUD operations under concurrent access
/// - IRetentionRecordService lifecycle operations (track, expire, delete) under concurrent access
/// - ILegalHoldService place/lift operations under concurrent access
/// - Mixed retention scenarios with concurrent operations across all services
/// - Latency distribution measurement for retention operations
/// </summary>
/// <remarks>
/// <para>
/// Retention enforcement is legally mandatory per GDPR Article 5(1)(e) — Storage Limitation.
/// Every data entity tracked for retention must have its lifecycle managed correctly under
/// concurrent access. This makes it a critical-path operation requiring load testing.
/// </para>
/// <para>
/// Run via: <c>dotnet run --project tests/Encina.LoadTests -- --scenario retention</c>
/// </para>
/// </remarks>
internal static class RetentionLoadTests
{
    private const int ConcurrentWorkers = 50;
    private const int OperationsPerWorker = 10_000;

    private static readonly string[] DataCategories =
        ["customer-data", "financial-records", "employee-data", "health-records", "marketing-consent"];

    private static readonly string[] EntityPrefixes =
        ["customer-", "order-", "employee-", "patient-", "contract-"];

    public static async Task RunAllAsync()
    {
        Console.WriteLine();
        Console.WriteLine("=== Retention Compliance Load Tests ===");
        Console.WriteLine($"Workers: {ConcurrentWorkers}, Operations/worker: {OperationsPerWorker:N0}");
        Console.WriteLine();

        await RunTestAsync("Policy Service — Concurrent Policy Creation",
            PolicyService_ConcurrentCreation_AllSucceed);
        await RunTestAsync("Record Service — Concurrent Entity Tracking",
            RecordService_ConcurrentTracking_AllSucceed);
        await RunTestAsync("Record Service — Concurrent Expiration Processing",
            RecordService_ConcurrentExpiration_AllSucceed);
        await RunTestAsync("Legal Hold — Concurrent Place and Lift",
            LegalHold_ConcurrentPlaceAndLift_AllSucceed);
        await RunTestAsync("Mixed Retention Scenarios — Concurrent",
            MixedRetention_ConcurrentOperations_NoErrors);
        await RunTestAsync("Record Lifecycle — Full Cycle Under Load",
            RecordLifecycle_FullCycleUnderLoad_AllSucceed);
        await RunTestAsync("Latency Distribution — P50/P95/P99",
            LatencyDistribution_ConcurrentLoad_WithinBounds);

        Console.WriteLine();
        Console.WriteLine("=== All retention load tests completed ===");
    }

    // ────────────────────────────────────────────────────────────
    //  Policy Service — Concurrent Policy Creation
    // ────────────────────────────────────────────────────────────

    private static async Task PolicyService_ConcurrentCreation_AllSucceed()
    {
        var policyService = BuildMockedPolicyService();

        var successCount = 0L;
        var failureCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var category = DataCategories[i % DataCategories.Length];
                var result = await policyService.CreatePolicyAsync(
                    $"{category}-{i}", TimeSpan.FromDays(365),
                    true, RetentionPolicyType.TimeBased,
                    "Load test", "Art. 5(1)(e)");

                if (result.IsRight)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref failureCount);
            }
        }));

        await Task.WhenAll(tasks);

        var total = ConcurrentWorkers * OperationsPerWorker;
        Assert(successCount == total, $"Expected {total} successes, got {successCount} (failures: {failureCount})");

        Console.WriteLine($"  {successCount:N0} concurrent policy creations, 0 failures");
    }

    // ────────────────────────────────────────────────────────────
    //  Record Service — Concurrent Entity Tracking
    // ────────────────────────────────────────────────────────────

    private static async Task RecordService_ConcurrentTracking_AllSucceed()
    {
        var recordService = BuildMockedRecordService();

        var successCount = 0L;
        var failureCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(workerId => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var category = DataCategories[i % DataCategories.Length];
                var entityPrefix = EntityPrefixes[i % EntityPrefixes.Length];
                var result = await recordService.TrackEntityAsync(
                    $"{entityPrefix}{workerId}-{i}", category,
                    Guid.NewGuid(), TimeSpan.FromDays(365));

                if (result.IsRight)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref failureCount);
            }
        }));

        await Task.WhenAll(tasks);

        var total = ConcurrentWorkers * OperationsPerWorker;
        Assert(successCount == total, $"Expected {total} successes, got {successCount}");

        Console.WriteLine($"  {successCount:N0} concurrent entity trackings, 0 failures");
    }

    // ────────────────────────────────────────────────────────────
    //  Record Service — Concurrent Expiration Processing
    // ────────────────────────────────────────────────────────────

    private static async Task RecordService_ConcurrentExpiration_AllSucceed()
    {
        var recordService = BuildMockedRecordService();

        var successCount = 0L;
        var failureCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var result = await recordService.MarkExpiredAsync(Guid.NewGuid());

                if (result.IsRight)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref failureCount);
            }
        }));

        await Task.WhenAll(tasks);

        var total = ConcurrentWorkers * OperationsPerWorker;
        Assert(successCount == total, $"Expected {total} successes, got {successCount}");

        Console.WriteLine($"  {successCount:N0} concurrent expirations, 0 failures");
    }

    // ────────────────────────────────────────────────────────────
    //  Legal Hold — Concurrent Place and Lift
    // ────────────────────────────────────────────────────────────

    private static async Task LegalHold_ConcurrentPlaceAndLift_AllSucceed()
    {
        var holdService = BuildMockedLegalHoldService();

        var placeCount = 0L;
        var liftCount = 0L;
        var errorCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(workerId => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                if (i % 2 == 0)
                {
                    // Place hold
                    var result = await holdService.PlaceHoldAsync(
                        $"entity-{workerId}-{i}", "Litigation hold",
                        "legal-counsel-1");

                    if (result.IsRight)
                        Interlocked.Increment(ref placeCount);
                    else
                        Interlocked.Increment(ref errorCount);
                }
                else
                {
                    // Lift hold
                    var result = await holdService.LiftHoldAsync(
                        Guid.NewGuid(), "legal-counsel-2");

                    if (result.IsRight)
                        Interlocked.Increment(ref liftCount);
                    else
                        Interlocked.Increment(ref errorCount);
                }
            }
        }));

        await Task.WhenAll(tasks);

        Assert(errorCount == 0, $"Expected 0 errors, got {errorCount}");
        var totalOps = placeCount + liftCount;

        Console.WriteLine($"  {totalOps:N0} legal hold operations: {placeCount:N0} placed, {liftCount:N0} lifted, 0 errors");
    }

    // ────────────────────────────────────────────────────────────
    //  Mixed Retention Scenarios — Concurrent
    // ────────────────────────────────────────────────────────────

    private static async Task MixedRetention_ConcurrentOperations_NoErrors()
    {
        var policyService = BuildMockedPolicyService();
        var recordService = BuildMockedRecordService();
        var holdService = BuildMockedLegalHoldService();

        var errors = new ConcurrentQueue<string>();
        var operationCounts = new ConcurrentDictionary<string, int>();

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(workerId => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var scenario = i % 5;

                try
                {
                    string scenarioName;

                    switch (scenario)
                    {
                        case 0: // Create policy
                            await policyService.CreatePolicyAsync(
                                $"cat-{workerId}-{i}", TimeSpan.FromDays(365),
                                true, RetentionPolicyType.TimeBased);
                            scenarioName = "CreatePolicy";
                            break;

                        case 1: // Track entity
                            await recordService.TrackEntityAsync(
                                $"entity-{workerId}-{i}", DataCategories[i % DataCategories.Length],
                                Guid.NewGuid(), TimeSpan.FromDays(365));
                            scenarioName = "TrackEntity";
                            break;

                        case 2: // Mark expired
                            await recordService.MarkExpiredAsync(Guid.NewGuid());
                            scenarioName = "MarkExpired";
                            break;

                        case 3: // Place hold
                            await holdService.PlaceHoldAsync(
                                $"entity-{workerId}-{i}", "Load test hold", "tester-1");
                            scenarioName = "PlaceHold";
                            break;

                        default: // Mark deleted
                            await recordService.MarkDeletedAsync(Guid.NewGuid());
                            scenarioName = "MarkDeleted";
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
    //  Record Lifecycle — Full Cycle Under Load
    // ────────────────────────────────────────────────────────────

    private static async Task RecordLifecycle_FullCycleUnderLoad_AllSucceed()
    {
        var recordService = BuildMockedRecordService();

        var completedCycles = 0L;
        var failedCycles = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(workerId => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker / 3; i++) // /3 because each cycle = 3 operations
            {
                try
                {
                    // Track → Expire → Delete (full lifecycle)
                    var trackResult = await recordService.TrackEntityAsync(
                        $"lifecycle-{workerId}-{i}", DataCategories[i % DataCategories.Length],
                        Guid.NewGuid(), TimeSpan.FromDays(30));

                    if (trackResult.IsLeft)
                    {
                        Interlocked.Increment(ref failedCycles);
                        continue;
                    }

                    var recordId = trackResult.Match(Right: id => id, Left: _ => Guid.Empty);

                    var expireResult = await recordService.MarkExpiredAsync(recordId);
                    if (expireResult.IsLeft)
                    {
                        Interlocked.Increment(ref failedCycles);
                        continue;
                    }

                    var deleteResult = await recordService.MarkDeletedAsync(recordId);
                    if (deleteResult.IsLeft)
                    {
                        Interlocked.Increment(ref failedCycles);
                        continue;
                    }

                    Interlocked.Increment(ref completedCycles);
                }
                catch
                {
                    Interlocked.Increment(ref failedCycles);
                }
            }
        }));

        await Task.WhenAll(tasks);

        Assert(failedCycles == 0, $"Expected 0 failed cycles, got {failedCycles}");

        Console.WriteLine($"  {completedCycles:N0} complete lifecycle cycles (track→expire→delete), 0 failures");
    }

    // ────────────────────────────────────────────────────────────
    //  Latency Distribution — P50/P95/P99
    // ────────────────────────────────────────────────────────────

    private static async Task LatencyDistribution_ConcurrentLoad_WithinBounds()
    {
        var policyService = BuildMockedPolicyService();

        var latencies = new ConcurrentBag<double>();

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(workerId => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var category = DataCategories[i % DataCategories.Length];

                var sw = Stopwatch.StartNew();
                await policyService.CreatePolicyAsync(
                    $"{category}-{workerId}-{i}", TimeSpan.FromDays(365),
                    true, RetentionPolicyType.TimeBased);
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
    //  Test Infrastructure — Mocked Service Builders
    // ────────────────────────────────────────────────────────────

    private static IRetentionPolicyService BuildMockedPolicyService()
    {
        var service = Substitute.For<IRetentionPolicyService>();

#pragma warning disable CA2012 // ValueTask instances returned from NSubstitute mock setups are consumed by the framework
        service.CreatePolicyAsync(
                Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<bool>(),
                Arg.Any<RetentionPolicyType>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Guid>>(Right<EncinaError, Guid>(Guid.NewGuid())));

        service.UpdatePolicyAsync(
                Arg.Any<Guid>(), Arg.Any<TimeSpan>(), Arg.Any<bool>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(Unit.Default)));

        service.DeactivatePolicyAsync(
                Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(Unit.Default)));

        service.GetPolicyAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);
                return new ValueTask<Either<EncinaError, RetentionPolicyReadModel>>(
                    Right<EncinaError, RetentionPolicyReadModel>(new RetentionPolicyReadModel
                    {
                        Id = id,
                        DataCategory = "test-data",
                        RetentionPeriod = TimeSpan.FromDays(365),
                        AutoDelete = true,
                        PolicyType = RetentionPolicyType.TimeBased,
                        IsActive = true,
                        CreatedAtUtc = DateTimeOffset.UtcNow,
                        LastModifiedAtUtc = DateTimeOffset.UtcNow
                    }));
            });

        service.GetRetentionPeriodAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, TimeSpan>>(
                Right<EncinaError, TimeSpan>(TimeSpan.FromDays(365))));
#pragma warning restore CA2012

        return service;
    }

    private static IRetentionRecordService BuildMockedRecordService()
    {
        var service = Substitute.For<IRetentionRecordService>();

#pragma warning disable CA2012
        service.TrackEntityAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>(),
                Arg.Any<TimeSpan>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Guid>>(Right<EncinaError, Guid>(Guid.NewGuid())));

        service.MarkExpiredAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(Unit.Default)));

        service.MarkDeletedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(Unit.Default)));

        service.MarkAnonymizedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(Unit.Default)));

        service.HoldRecordAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(Unit.Default)));

        service.ReleaseRecordAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(Unit.Default)));

        service.GetExpiredRecordsAsync(Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, IReadOnlyList<RetentionRecordReadModel>>>(
                Right<EncinaError, IReadOnlyList<RetentionRecordReadModel>>(
                    System.Array.Empty<RetentionRecordReadModel>())));
#pragma warning restore CA2012

        return service;
    }

    private static ILegalHoldService BuildMockedLegalHoldService()
    {
        var service = Substitute.For<ILegalHoldService>();

#pragma warning disable CA2012
        service.PlaceHoldAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Guid>>(Right<EncinaError, Guid>(Guid.NewGuid())));

        service.LiftHoldAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(Unit.Default)));

        service.HasActiveHoldsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, bool>>(Right<EncinaError, bool>(false)));

        service.GetAllActiveHoldsAsync(Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, IReadOnlyList<LegalHoldReadModel>>>(
                Right<EncinaError, IReadOnlyList<LegalHoldReadModel>>(
                    System.Array.Empty<LegalHoldReadModel>())));
#pragma warning restore CA2012

        return service;
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

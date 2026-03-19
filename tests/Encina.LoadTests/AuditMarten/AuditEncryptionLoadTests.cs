using System.Collections.Concurrent;
using System.Diagnostics;

using Encina.Audit.Marten;
using Encina.Audit.Marten.Crypto;
using Encina.Audit.Marten.Events;
using Encina.Security.Audit;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.LoadTests.AuditMarten;

/// <summary>
/// Load tests for Marten audit encryption — measures throughput, latency, CPU and memory impact
/// under concurrent audit entry encryption. This simulates the hot path of every command
/// in a compliance-grade European application (SOX, NIS2, GDPR).
/// </summary>
/// <remarks>
/// <para>
/// Run from the LoadTests project:
/// <code>
/// dotnet run --project tests/Encina.LoadTests -- --scenario audit-encrypt --workers 8 --duration 30
/// </code>
/// </para>
/// <para>
/// Metrics reported:
/// <list type="bullet">
/// <item>Throughput: entries/sec at various concurrency levels</item>
/// <item>Latency: P50, P95, P99 per entry (encrypt + key lookup)</item>
/// <item>Memory: GC collections, allocation rate</item>
/// <item>CPU: wall-clock time vs. thread time</item>
/// </list>
/// </para>
/// </remarks>
public static class AuditEncryptionLoadTests
{
    /// <summary>
    /// Runs the concurrent audit encryption load test with the specified parameters.
    /// </summary>
    /// <param name="workerCount">Number of concurrent workers simulating parallel command execution.</param>
    /// <param name="duration">How long to run the test.</param>
    /// <param name="payloadSizeBytes">Size of the simulated request payload per entry.</param>
    public static async Task RunAsync(int workerCount = 8, TimeSpan? duration = null, int payloadSizeBytes = 2048)
    {
        var testDuration = duration ?? TimeSpan.FromSeconds(30);

        Console.WriteLine("=== Audit Encryption Load Test ===");
        Console.WriteLine($"Workers: {workerCount}");
        Console.WriteLine($"Duration: {testDuration.TotalSeconds}s");
        Console.WriteLine($"Payload size: {payloadSizeBytes} bytes");
        Console.WriteLine();

        var keyProvider = new InMemoryTemporalKeyProvider(
            TimeProvider.System,
            NullLogger<InMemoryTemporalKeyProvider>.Instance);

        var options = Options.Create(new MartenAuditOptions
        {
            TemporalGranularity = TemporalKeyGranularity.Monthly
        });

        var encryptor = new AuditEventEncryptor(
            keyProvider,
            options,
            NullLogger<AuditEventEncryptor>.Instance);

        var latencies = new ConcurrentBag<double>();
        long totalEntries = 0;
        long totalErrors = 0;
        var payload = new string('X', payloadSizeBytes);

        var gcBefore = GC.CollectionCount(0);
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
                    var result = await encryptor.EncryptAuditEntryAsync(entry, cts.Token)
                        .ConfigureAwait(false);
                    localSw.Stop();

                    if (result.IsRight)
                    {
                        Interlocked.Increment(ref totalEntries);
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

        var gcAfter = GC.CollectionCount(0);
        var memAfter = GC.GetTotalMemory(false);
        var allLatencies = latencies.OrderBy(l => l).ToArray();

        Console.WriteLine("=== Results ===");
        Console.WriteLine($"Total entries encrypted: {totalEntries:N0}");
        Console.WriteLine($"Total errors:           {totalErrors:N0}");
        Console.WriteLine($"Elapsed:                {sw.Elapsed.TotalSeconds:F2}s");
        Console.WriteLine($"Throughput:             {totalEntries / sw.Elapsed.TotalSeconds:F0} entries/sec");
        Console.WriteLine();

        if (allLatencies.Length > 0)
        {
            Console.WriteLine("=== Latency (ms per entry) ===");
            Console.WriteLine($"  P50:  {Percentile(allLatencies, 50):F3}");
            Console.WriteLine($"  P90:  {Percentile(allLatencies, 90):F3}");
            Console.WriteLine($"  P95:  {Percentile(allLatencies, 95):F3}");
            Console.WriteLine($"  P99:  {Percentile(allLatencies, 99):F3}");
            Console.WriteLine($"  Max:  {allLatencies[^1]:F3}");
            Console.WriteLine($"  Avg:  {allLatencies.Average():F3}");
        }

        Console.WriteLine();
        Console.WriteLine("=== Memory & GC ===");
        Console.WriteLine($"  GC Gen0 collections: {gcAfter - gcBefore}");
        Console.WriteLine($"  Memory delta:        {(memAfter - memBefore) / 1024.0:F0} KB");
        Console.WriteLine($"  Memory per entry:    {(memAfter - memBefore) / Math.Max(totalEntries, 1):F0} bytes");
        Console.WriteLine();

        keyProvider.Clear();
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
        UserId = "user-12345678",
        TenantId = "tenant-eu-west",
        Action = "Create",
        EntityType = "Order",
        EntityId = $"ORD-{Random.Shared.Next(1, 999999)}",
        Outcome = AuditOutcome.Success,
        TimestampUtc = DateTime.UtcNow,
        StartedAtUtc = DateTimeOffset.UtcNow.AddMilliseconds(-10),
        CompletedAtUtc = DateTimeOffset.UtcNow,
        IpAddress = "10.0.0.42",
        UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)",
        RequestPayloadHash = "sha256:benchmark",
        RequestPayload = payload,
        Metadata = new Dictionary<string, object?>
        {
            ["region"] = "eu-west-1",
            ["compliance"] = "NIS2"
        }
    };
}

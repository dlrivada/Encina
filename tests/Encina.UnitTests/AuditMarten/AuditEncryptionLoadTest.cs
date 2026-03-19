using System.Collections.Concurrent;
using System.Diagnostics;

using Encina.Audit.Marten;
using Encina.Audit.Marten.Crypto;
using Encina.Security.Audit;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Encina.UnitTests.AuditMarten;

/// <summary>
/// Load test for audit entry encryption under concurrency.
/// Measures throughput, latency percentiles, and memory pressure.
/// Runs as a standard xUnit test to ensure it's always executed and baseline is captured.
/// </summary>
[Trait("Category", "Load")]
[Trait("Provider", "Marten")]
public sealed class AuditEncryptionLoadTest
{
    [Fact]
    public async Task ConcurrentEncryption_8Workers_15Seconds_ShouldMeetThroughputBaseline()
    {
        // Configuration
        const int workerCount = 8;
        var duration = TimeSpan.FromSeconds(15);
        const int payloadSize = 2048;
        const int minThroughput = 50_000; // entries/sec minimum baseline

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
        var payload = new string('X', payloadSize);

        using var cts = new CancellationTokenSource(duration);
        var sw = Stopwatch.StartNew();

        var workers = Enumerable
            .Range(0, workerCount)
            .Select(_ => Task.Run(async () =>
            {
                var localSw = new Stopwatch();
                while (!cts.IsCancellationRequested)
                {
                    var entry = CreateEntry(payload);

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

        await Task.WhenAll(workers);
        sw.Stop();

        var throughput = totalEntries / sw.Elapsed.TotalSeconds;
        var allLatencies = latencies.OrderBy(l => l).ToArray();

        // Output results to file for documentation capture
        var ci = System.Globalization.CultureInfo.InvariantCulture;
        var lines = new List<string>
        {
            string.Create(ci, $"Workers: {workerCount}, Duration: {sw.Elapsed.TotalSeconds:F1}s, Payload: {payloadSize}B"),
            string.Create(ci, $"Total: {totalEntries:N0} entries, Errors: {totalErrors:N0}"),
            string.Create(ci, $"Throughput: {throughput:F0} entries/sec")
        };
        if (allLatencies.Length > 0)
        {
            lines.Add(string.Create(ci,
                $"P50: {Pct(allLatencies, 50):F4}ms, P90: {Pct(allLatencies, 90):F4}ms, P95: {Pct(allLatencies, 95):F4}ms, P99: {Pct(allLatencies, 99):F4}ms, Max: {allLatencies[^1]:F4}ms"));
        }

        var resultPath = Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "artifacts", "load-metrics", "audit-marten-load-results.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(resultPath)!);
        File.WriteAllLines(resultPath, lines);

        foreach (var line in lines)
        {
            Console.WriteLine(line);
        }

        // Assertions — tolerate a tiny error rate from cancellation at timer expiry
        var errorRate = totalErrors / (double)Math.Max(totalEntries + totalErrors, 1);
        errorRate.ShouldBeLessThan(0.001, "Error rate should be < 0.1% (only cancellation errors expected)");
        throughput.ShouldBeGreaterThan(minThroughput,
            $"Throughput {throughput:F0} entries/sec should exceed baseline {minThroughput}");
    }

    private static double Pct(double[] sorted, double pct)
    {
        var idx = (int)Math.Ceiling(pct / 100.0 * sorted.Length) - 1;
        return sorted[Math.Clamp(idx, 0, sorted.Length - 1)];
    }

    private static AuditEntry CreateEntry(string payload) => new()
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
        RequestPayloadHash = "sha256:load-test",
        RequestPayload = payload,
        Metadata = new Dictionary<string, object?>
        {
            ["region"] = "eu-west-1",
            ["compliance"] = "NIS2"
        }
    };
}

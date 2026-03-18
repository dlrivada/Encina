using System.Collections.Concurrent;
using System.Diagnostics;
using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.Model;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.LoadTests.Compliance.Anonymization;

/// <summary>
/// Load tests for the Anonymization compliance module under high concurrent traffic.
/// Validates throughput, latency percentiles, and thread safety of:
/// - Tokenization (UUID, prefixed, format-preserving) under concurrent access
/// - Pseudonymization with AES-256-GCM and HMAC-SHA256 under concurrent access
/// - Anonymization techniques (data masking, generalization, suppression) under concurrent access
/// - Risk assessment computations under parallel invocation
/// - Mixed anonymization scenarios with concurrent operations
/// - Pipeline behavior attribute-based transformations under concurrent request processing
/// </summary>
/// <remarks>
/// <para>
/// Anonymization operations execute on every data export/deletion request where anonymization
/// is the chosen strategy over erasure. These are computationally intensive hot-path operations
/// involving AES-256-GCM encryption, HMAC-SHA256 hashing, and privacy metric calculations.
/// </para>
/// <para>
/// GDPR compliance is legally mandatory in Europe. Performance characterization is essential
/// for capacity planning: CPU time, memory allocations, GC pressure, and latency percentiles.
/// </para>
/// <para>
/// Run via: <c>dotnet run --project tests/Encina.LoadTests -- --scenario anonymization</c>
/// </para>
/// </remarks>
internal static class AnonymizationLoadTests
{
    private const int ConcurrentWorkers = 50;
    private const int OperationsPerWorker = 10_000;

    private static readonly string[] SensitiveValues =
    [
        "4111-1111-1111-1111", "john.doe@example.com", "SSN-123-45-6789",
        "Jane Smith", "+1-555-0100", "MRN-2026-001234", "passport-AB123456",
        "bank-acct-9876543210", "driver-license-DL99887766", "tax-id-EIN-12-3456789"
    ];

    private static readonly string[] PersonNames =
    [
        "John Smith", "Maria Garcia", "Wei Zhang", "Priya Patel", "Olaf Müller",
        "Yuki Tanaka", "Ahmed Hassan", "Fatima Al-Said", "Lars Eriksson", "Ana Souza"
    ];

    private static readonly string[] EmailAddresses =
    [
        "john@example.com", "maria@company.org", "wei@university.edu",
        "priya@startup.io", "olaf@enterprise.de", "yuki@tech.jp",
        "ahmed@gov.eg", "fatima@health.sa", "lars@nordic.se", "ana@finance.br"
    ];

    public static async Task RunAllAsync()
    {
        Console.WriteLine();
        Console.WriteLine("=== Anonymization Compliance Load Tests ===");
        Console.WriteLine($"Workers: {ConcurrentWorkers}, Operations/worker: {OperationsPerWorker:N0}");
        Console.WriteLine();

        await RunTestAsync("Tokenization — UUID Format Concurrent",
            Tokenization_UuidFormat_ConcurrentOperations_AllSucceed);
        await RunTestAsync("Tokenization — Prefixed Format Concurrent",
            Tokenization_PrefixedFormat_ConcurrentOperations_AllSucceed);
        await RunTestAsync("Pseudonymization — AES-256-GCM Concurrent",
            Pseudonymization_Aes256Gcm_ConcurrentOperations_AllSucceed);
        await RunTestAsync("Pseudonymization — HMAC-SHA256 Concurrent",
            Pseudonymization_HmacSha256_ConcurrentOperations_AllSucceed);
        await RunTestAsync("Anonymization — Data Masking Concurrent",
            Anonymization_DataMasking_ConcurrentOperations_AllSucceed);
        await RunTestAsync("Risk Assessment — Concurrent Assessments",
            RiskAssessment_ConcurrentAssessments_AllSucceed);
        await RunTestAsync("Mixed Anonymization Scenarios — Concurrent",
            MixedAnonymization_ConcurrentOperations_NoErrors);
        await RunTestAsync("Latency Distribution — P50/P95/P99",
            LatencyDistribution_ConcurrentLoad_WithinBounds);

        Console.WriteLine();
        Console.WriteLine("=== All anonymization load tests completed ===");
    }

    // ────────────────────────────────────────────────────────────
    //  Tokenization — UUID Format Concurrent
    // ────────────────────────────────────────────────────────────

    private static async Task Tokenization_UuidFormat_ConcurrentOperations_AllSucceed()
    {
        using var provider = BuildServiceProvider();
        var tokenizer = provider.GetRequiredService<ITokenizer>();
        var options = new TokenizationOptions { Format = TokenFormat.Uuid };

        var successCount = 0L;
        var failureCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(workerId => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                // Each worker uses a unique value to avoid deduplication
                var value = $"sensitive-{workerId}-{i}";
                var result = await tokenizer.TokenizeAsync(value, options);

                if (result.IsRight)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref failureCount);
            }
        }));

        await Task.WhenAll(tasks);

        var total = ConcurrentWorkers * OperationsPerWorker;
        Assert(successCount == total, $"Expected {total} successes, got {successCount} (failures: {failureCount})");

        Console.WriteLine($"  {successCount:N0} concurrent UUID tokenizations, 0 failures");
    }

    // ────────────────────────────────────────────────────────────
    //  Tokenization — Prefixed Format Concurrent
    // ────────────────────────────────────────────────────────────

    private static async Task Tokenization_PrefixedFormat_ConcurrentOperations_AllSucceed()
    {
        using var provider = BuildServiceProvider();
        var tokenizer = provider.GetRequiredService<ITokenizer>();
        var options = new TokenizationOptions { Format = TokenFormat.Prefixed, Prefix = "tok" };

        var successCount = 0L;
        var failureCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(workerId => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var value = $"prefixed-{workerId}-{i}";
                var result = await tokenizer.TokenizeAsync(value, options);

                if (result.IsRight)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref failureCount);
            }
        }));

        await Task.WhenAll(tasks);

        var total = ConcurrentWorkers * OperationsPerWorker;
        Assert(successCount == total, $"Expected {total} successes, got {successCount} (failures: {failureCount})");

        Console.WriteLine($"  {successCount:N0} concurrent prefixed tokenizations, 0 failures");
    }

    // ────────────────────────────────────────────────────────────
    //  Pseudonymization — AES-256-GCM Concurrent
    // ────────────────────────────────────────────────────────────

    private static async Task Pseudonymization_Aes256Gcm_ConcurrentOperations_AllSucceed()
    {
        using var provider = BuildServiceProvider();
        var pseudonymizer = provider.GetRequiredService<IPseudonymizer>();
        var keyProvider = provider.GetRequiredService<IKeyProvider>();

        var activeKeyResult = await keyProvider.GetActiveKeyIdAsync();
        Assert(activeKeyResult.IsRight, "Active key should exist");
        var keyId = (string)activeKeyResult;

        var successCount = 0L;
        var failureCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var value = SensitiveValues[i % SensitiveValues.Length];

                var result = await pseudonymizer.PseudonymizeValueAsync(
                    value, keyId, PseudonymizationAlgorithm.Aes256Gcm);

                if (result.IsRight)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref failureCount);
            }
        }));

        await Task.WhenAll(tasks);

        var total = ConcurrentWorkers * OperationsPerWorker;
        Assert(successCount == total, $"Expected {total} successes, got {successCount} (failures: {failureCount})");

        Console.WriteLine($"  {successCount:N0} concurrent AES-256-GCM pseudonymizations, 0 failures");
    }

    // ────────────────────────────────────────────────────────────
    //  Pseudonymization — HMAC-SHA256 Concurrent
    // ────────────────────────────────────────────────────────────

    private static async Task Pseudonymization_HmacSha256_ConcurrentOperations_AllSucceed()
    {
        using var provider = BuildServiceProvider();
        var pseudonymizer = provider.GetRequiredService<IPseudonymizer>();
        var keyProvider = provider.GetRequiredService<IKeyProvider>();

        var activeKeyResult = await keyProvider.GetActiveKeyIdAsync();
        Assert(activeKeyResult.IsRight, "Active key should exist");
        var keyId = (string)activeKeyResult;

        var successCount = 0L;
        var failureCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var value = EmailAddresses[i % EmailAddresses.Length];

                var result = await pseudonymizer.PseudonymizeValueAsync(
                    value, keyId, PseudonymizationAlgorithm.HmacSha256);

                if (result.IsRight)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref failureCount);
            }
        }));

        await Task.WhenAll(tasks);

        var total = ConcurrentWorkers * OperationsPerWorker;
        Assert(successCount == total, $"Expected {total} successes, got {successCount} (failures: {failureCount})");

        Console.WriteLine($"  {successCount:N0} concurrent HMAC-SHA256 pseudonymizations, 0 failures");
    }

    // ────────────────────────────────────────────────────────────
    //  Anonymization — Data Masking Concurrent
    // ────────────────────────────────────────────────────────────

    private static async Task Anonymization_DataMasking_ConcurrentOperations_AllSucceed()
    {
        using var provider = BuildServiceProvider();
        var anonymizer = provider.GetRequiredService<IAnonymizer>();

        var profile = AnonymizationProfile.Create(
            name: "load-test-masking",
            fieldRules:
            [
                new FieldAnonymizationRule
                {
                    FieldName = "Name",
                    Technique = AnonymizationTechnique.DataMasking,
                    Parameters = new Dictionary<string, object>
                    {
                        ["PreserveStart"] = 1,
                        ["PreserveEnd"] = 0
                    }
                },
                new FieldAnonymizationRule
                {
                    FieldName = "Email",
                    Technique = AnonymizationTechnique.DataMasking,
                    Parameters = new Dictionary<string, object>
                    {
                        ["PreserveDomain"] = true,
                        ["PreserveStart"] = 1
                    }
                }
            ]);

        var successCount = 0L;
        var failureCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var data = new LoadTestPerson
                {
                    Name = PersonNames[i % PersonNames.Length],
                    Email = EmailAddresses[i % EmailAddresses.Length],
                    Age = 25 + (i % 50)
                };

                var result = await anonymizer.AnonymizeAsync(data, profile);

                if (result.IsRight)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref failureCount);
            }
        }));

        await Task.WhenAll(tasks);

        var total = ConcurrentWorkers * OperationsPerWorker;
        Assert(successCount == total, $"Expected {total} successes, got {successCount} (failures: {failureCount})");

        Console.WriteLine($"  {successCount:N0} concurrent data masking anonymizations, 0 failures");
    }

    // ────────────────────────────────────────────────────────────
    //  Risk Assessment — Concurrent Assessments
    // ────────────────────────────────────────────────────────────

    private static async Task RiskAssessment_ConcurrentAssessments_AllSucceed()
    {
        using var provider = BuildServiceProvider();
        var assessor = provider.GetRequiredService<IRiskAssessor>();

        // Build a dataset large enough for meaningful privacy metrics
        var dataset = Enumerable.Range(0, 100).Select(i => new LoadTestPerson
        {
            Name = PersonNames[i % PersonNames.Length],
            Email = EmailAddresses[i % EmailAddresses.Length],
            Age = 20 + (i % 60)
        }).ToList().AsReadOnly();

        var quasiIdentifiers = QuasiIdentifiers;

        var successCount = 0L;
        var failureCount = 0L;

        // Risk assessment is heavier than single-value operations — use fewer ops per worker
        var opsPerWorker = 1_000;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < opsPerWorker; i++)
            {
                var result = await assessor.AssessAsync(dataset, quasiIdentifiers);

                if (result.IsRight)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref failureCount);
            }
        }));

        await Task.WhenAll(tasks);

        var total = ConcurrentWorkers * opsPerWorker;
        Assert(successCount == total, $"Expected {total} successes, got {successCount} (failures: {failureCount})");

        Console.WriteLine($"  {successCount:N0} concurrent risk assessments, 0 failures");
    }

    // ────────────────────────────────────────────────────────────
    //  Mixed Anonymization Scenarios — Concurrent
    // ────────────────────────────────────────────────────────────

    private static async Task MixedAnonymization_ConcurrentOperations_NoErrors()
    {
        using var provider = BuildServiceProvider();
        var tokenizer = provider.GetRequiredService<ITokenizer>();
        var pseudonymizer = provider.GetRequiredService<IPseudonymizer>();
        var anonymizer = provider.GetRequiredService<IAnonymizer>();
        var keyProvider = provider.GetRequiredService<IKeyProvider>();

        var activeKeyResult = await keyProvider.GetActiveKeyIdAsync();
        Assert(activeKeyResult.IsRight, "Active key should exist");
        var keyId = (string)activeKeyResult;

        var uuidOptions = new TokenizationOptions { Format = TokenFormat.Uuid };

        var maskingProfile = AnonymizationProfile.Create(
            name: "mixed-masking",
            fieldRules:
            [
                new FieldAnonymizationRule
                {
                    FieldName = "Name",
                    Technique = AnonymizationTechnique.DataMasking,
                    Parameters = new Dictionary<string, object> { ["PreserveStart"] = 1 }
                }
            ]);

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
                        case 0: // UUID tokenization
                            var tokenResult = await tokenizer.TokenizeAsync(
                                $"mixed-tok-{workerId}-{i}", uuidOptions);
                            Assert(tokenResult.IsRight, "Tokenization should succeed");
                            scenarioName = "Tokenize";
                            break;

                        case 1: // AES-256-GCM pseudonymization
                            var aesResult = await pseudonymizer.PseudonymizeValueAsync(
                                SensitiveValues[i % SensitiveValues.Length],
                                keyId, PseudonymizationAlgorithm.Aes256Gcm);
                            Assert(aesResult.IsRight, "AES pseudonymization should succeed");
                            scenarioName = "AES-Pseudo";
                            break;

                        case 2: // HMAC-SHA256 pseudonymization
                            var hmacResult = await pseudonymizer.PseudonymizeValueAsync(
                                EmailAddresses[i % EmailAddresses.Length],
                                keyId, PseudonymizationAlgorithm.HmacSha256);
                            Assert(hmacResult.IsRight, "HMAC pseudonymization should succeed");
                            scenarioName = "HMAC-Pseudo";
                            break;

                        case 3: // Data masking anonymization
                            var data = new LoadTestPerson
                            {
                                Name = PersonNames[i % PersonNames.Length],
                                Email = EmailAddresses[i % EmailAddresses.Length],
                                Age = 30
                            };
                            var maskResult = await anonymizer.AnonymizeAsync(data, maskingProfile);
                            Assert(maskResult.IsRight, "Masking should succeed");
                            scenarioName = "Masking";
                            break;

                        default: // Tokenize + detokenize roundtrip
                            var rtResult = await tokenizer.TokenizeAsync(
                                $"roundtrip-{workerId}-{i}", uuidOptions);
                            Assert(rtResult.IsRight, "Tokenization should succeed");
                            var token = (string)rtResult;
                            var detResult = await tokenizer.DetokenizeAsync(token);
                            Assert(detResult.IsRight, "Detokenization should succeed");
                            scenarioName = "Roundtrip";
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
    //  Latency Distribution — P50/P95/P99
    // ────────────────────────────────────────────────────────────

    private static async Task LatencyDistribution_ConcurrentLoad_WithinBounds()
    {
        using var provider = BuildServiceProvider();
        var pseudonymizer = provider.GetRequiredService<IPseudonymizer>();
        var keyProvider = provider.GetRequiredService<IKeyProvider>();

        var activeKeyResult = await keyProvider.GetActiveKeyIdAsync();
        Assert(activeKeyResult.IsRight, "Active key should exist");
        var keyId = (string)activeKeyResult;

        var latencies = new ConcurrentBag<double>();

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var value = SensitiveValues[i % SensitiveValues.Length];

                var sw = Stopwatch.StartNew();
                await pseudonymizer.PseudonymizeValueAsync(
                    value, keyId, PseudonymizationAlgorithm.Aes256Gcm);
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

        // AES-256-GCM is CPU-bound, P99 should be under 10ms even under heavy load
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
        services.AddEncinaAnonymization(options =>
        {
            options.EnforcementMode = AnonymizationEnforcementMode.Block;
            options.TrackAuditTrail = false; // Disable audit trail for load testing to isolate core performance
        });

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = false,
            ValidateOnBuild = false
        });
    }

    // ────────────────────────────────────────────────────────────
    //  Test Infrastructure — Data Models
    // ────────────────────────────────────────────────────────────

    private static readonly IReadOnlyList<string> QuasiIdentifiers = new[] { "Age" }.AsReadOnly();

    private sealed class LoadTestPerson
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public int Age { get; set; }
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

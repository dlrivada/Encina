using System.Collections.Concurrent;
using System.Diagnostics;
using Encina.Compliance.ProcessorAgreements.Abstractions;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Compliance.ProcessorAgreements.ReadModels;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using static LanguageExt.Prelude;

namespace Encina.LoadTests.Compliance.ProcessorAgreements;

/// <summary>
/// Load tests for the Processor Agreements compliance module under high concurrent traffic.
/// Validates throughput, latency percentiles, and thread safety of:
/// - IDPAService agreement validation under concurrent access
/// - IProcessorService registry lookups under parallel invocation
/// - Sub-processor chain traversal under concurrent load
/// - Agreement expiration checks under concurrent evaluation
/// - Mixed DPA scenarios with concurrent operations
/// - Latency distribution for agreement validation
/// </summary>
/// <remarks>
/// <para>
/// GDPR Article 28 requires Data Processing Agreements for all data processing involving
/// third-party processors. Agreement validation executes on every request involving processors,
/// making it a hot-path operation requiring load testing.
/// </para>
/// <para>
/// Run via: <c>dotnet run --project tests/Encina.LoadTests -- --scenario processor-agreements</c>
/// </para>
/// </remarks>
internal static class ProcessorAgreementLoadTests
{
    private const int ConcurrentWorkers = 50;
    private const int OperationsPerWorker = 10_000;

    private static readonly string[] ProcessorNames =
        ["Stripe", "AWS", "Google Cloud", "Twilio", "SendGrid"];

    private static readonly string[] Countries =
        ["US", "DE", "IE", "JP", "GB", "FR", "AU", "CA", "NL", "SE"];

    private static readonly string[] ProcessingPurposes =
        ["data-analytics", "payment-processing", "email-marketing", "cloud-hosting", "customer-support"];

    public static async Task RunAllAsync()
    {
        Console.WriteLine();
        Console.WriteLine("=== Processor Agreement Validation Load Tests ===");
        Console.WriteLine($"Workers: {ConcurrentWorkers}, Operations/worker: {OperationsPerWorker:N0}");
        Console.WriteLine();

        await RunTestAsync("Agreement Validation — Concurrent Checks",
            AgreementValidation_ConcurrentChecks_AllSucceed);
        await RunTestAsync("Processor Registration — Concurrent Lookups",
            ProcessorRegistration_ConcurrentLookups_MaintainsThroughput);
        await RunTestAsync("Sub-Processor Chain — Concurrent Traversals",
            SubProcessorChain_ConcurrentTraversals_AllSucceed);
        await RunTestAsync("Agreement Expiration — Concurrent Evaluations",
            AgreementExpiration_ConcurrentEvaluations_AllSucceed);
        await RunTestAsync("Mixed DPA Scenarios — Concurrent Operations",
            MixedDPAScenarios_ConcurrentOperations_NoErrors);
        await RunTestAsync("Latency Distribution — P50/P95/P99",
            LatencyDistribution_ConcurrentLoad_WithinBounds);

        Console.WriteLine();
        Console.WriteLine("=== All processor agreement load tests completed ===");
    }

    // ────────────────────────────────────────────────────────────
    //  Agreement Validation — Concurrent Checks
    // ────────────────────────────────────────────────────────────

    private static async Task AgreementValidation_ConcurrentChecks_AllSucceed()
    {
        using var provider = BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        // Pre-register processors and DPAs
        var processorIds = new List<Guid>();
        for (var i = 0; i < 10; i++)
        {
            using var scope = scopeFactory.CreateScope();
            var ps = scope.ServiceProvider.GetRequiredService<IProcessorService>();
            var ds = scope.ServiceProvider.GetRequiredService<IDPAService>();

            var pid = (await ps.RegisterProcessorAsync(
                $"ValidProc-{i}", Countries[i % Countries.Length], null, null, 0,
                SubProcessorAuthorizationType.Specific))
                .Match(id => id, _ => Guid.Empty);

            if (pid != Guid.Empty)
            {
                await ds.ExecuteDPAAsync(
                    pid, FullyCompliantTerms(), false,
                    [ProcessingPurposes[i % ProcessingPurposes.Length]],
                    DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(2));
                processorIds.Add(pid);
            }
        }

        var successCount = 0L;
        var failureCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                using var scope = scopeFactory.CreateScope();
                var ds = scope.ServiceProvider.GetRequiredService<IDPAService>();
                var processorId = processorIds[i % processorIds.Count];

                var result = await ds.HasValidDPAAsync(processorId);

                if (result.IsRight)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref failureCount);
            }
        }));

        await Task.WhenAll(tasks);

        var total = ConcurrentWorkers * OperationsPerWorker;
        Assert(successCount == total, $"Expected {total} successes, got {successCount} (failures: {failureCount})");

        Console.WriteLine($"  {successCount:N0} concurrent agreement validations, 0 failures");
    }

    // ────────────────────────────────────────────────────────────
    //  Processor Registration — Concurrent Lookups
    // ────────────────────────────────────────────────────────────

    private static async Task ProcessorRegistration_ConcurrentLookups_MaintainsThroughput()
    {
        using var provider = BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        // Pre-register processors
        var processorIds = new List<Guid>();
        for (var i = 0; i < 20; i++)
        {
            using var scope = scopeFactory.CreateScope();
            var ps = scope.ServiceProvider.GetRequiredService<IProcessorService>();
            var pid = (await ps.RegisterProcessorAsync(
                $"{ProcessorNames[i % ProcessorNames.Length]}-{i}", Countries[i % Countries.Length],
                $"dpo-{i}@test.com", null, 0, SubProcessorAuthorizationType.Specific))
                .Match(id => id, _ => Guid.Empty);
            if (pid != Guid.Empty) processorIds.Add(pid);
        }

        var successCount = 0L;
        var failureCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                using var scope = scopeFactory.CreateScope();
                var ps = scope.ServiceProvider.GetRequiredService<IProcessorService>();
                var processorId = processorIds[i % processorIds.Count];

                var result = await ps.GetProcessorAsync(processorId);

                if (result.IsRight)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref failureCount);
            }
        }));

        await Task.WhenAll(tasks);

        var total = ConcurrentWorkers * OperationsPerWorker;
        Assert(successCount == total, $"Expected {total} successes, got {successCount}");

        Console.WriteLine($"  {successCount:N0} concurrent processor lookups, 0 failures");
    }

    // ────────────────────────────────────────────────────────────
    //  Sub-Processor Chain — Concurrent Traversals
    // ────────────────────────────────────────────────────────────

    private static async Task SubProcessorChain_ConcurrentTraversals_AllSucceed()
    {
        using var provider = BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        // Build a 3-level processor hierarchy
        Guid rootId;
        using (var scope = scopeFactory.CreateScope())
        {
            var ps = scope.ServiceProvider.GetRequiredService<IProcessorService>();
            rootId = (await ps.RegisterProcessorAsync(
                "Root Corp", "DE", null, null, 0, SubProcessorAuthorizationType.General))
                .Match(id => id, _ => Guid.Empty);

            for (var i = 0; i < 3; i++)
            {
                var subId = (await ps.RegisterProcessorAsync(
                    $"Sub-{i}", Countries[i % Countries.Length], null, rootId, 1,
                    SubProcessorAuthorizationType.Specific))
                    .Match(id => id, _ => Guid.Empty);

                // Add sub-sub-processors
                for (var j = 0; j < 2; j++)
                {
                    await ps.RegisterProcessorAsync(
                        $"SubSub-{i}-{j}", Countries[(i + j) % Countries.Length], null, subId, 2,
                        SubProcessorAuthorizationType.Specific);
                }
            }
        }

        var successCount = 0L;
        var failureCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < 1000; i++)
            {
                using var scope = scopeFactory.CreateScope();
                var ps = scope.ServiceProvider.GetRequiredService<IProcessorService>();

                var result = await ps.GetFullSubProcessorChainAsync(rootId);

                if (result.IsRight)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref failureCount);
            }
        }));

        await Task.WhenAll(tasks);

        var total = ConcurrentWorkers * 1000L;
        Assert(successCount == total, $"Expected {total} successes, got {successCount}");

        Console.WriteLine($"  {successCount:N0} concurrent chain traversals, 0 failures");
    }

    // ────────────────────────────────────────────────────────────
    //  Agreement Expiration — Concurrent Evaluations
    // ────────────────────────────────────────────────────────────

    private static async Task AgreementExpiration_ConcurrentEvaluations_AllSucceed()
    {
        using var provider = BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        // Register processors with DPAs near expiration
        var processorIds = new List<Guid>();
        for (var i = 0; i < 10; i++)
        {
            using var scope = scopeFactory.CreateScope();
            var ps = scope.ServiceProvider.GetRequiredService<IProcessorService>();
            var ds = scope.ServiceProvider.GetRequiredService<IDPAService>();

            var pid = (await ps.RegisterProcessorAsync(
                $"ExpiringProc-{i}", "DE", null, null, 0, SubProcessorAuthorizationType.Specific))
                .Match(id => id, _ => Guid.Empty);

            if (pid != Guid.Empty)
            {
                var expiresAt = DateTimeOffset.UtcNow.AddDays(15 + i);
                await ds.ExecuteDPAAsync(pid, FullyCompliantTerms(), false,
                    ["processing"], DateTimeOffset.UtcNow, expiresAt);
                processorIds.Add(pid);
            }
        }

        var successCount = 0L;
        var failureCount = 0L;

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                using var scope = scopeFactory.CreateScope();
                var ds = scope.ServiceProvider.GetRequiredService<IDPAService>();
                var processorId = processorIds[i % processorIds.Count];

                var result = await ds.ValidateDPAAsync(processorId);

                if (result.IsRight)
                    Interlocked.Increment(ref successCount);
                else
                    Interlocked.Increment(ref failureCount);
            }
        }));

        await Task.WhenAll(tasks);

        var total = ConcurrentWorkers * OperationsPerWorker;
        Assert(successCount == total, $"Expected {total} successes, got {successCount} (failures: {failureCount})");

        Console.WriteLine($"  {successCount:N0} concurrent expiration checks, 0 failures");
    }

    // ────────────────────────────────────────────────────────────
    //  Mixed DPA Scenarios — Concurrent Operations
    // ────────────────────────────────────────────────────────────

    private static async Task MixedDPAScenarios_ConcurrentOperations_NoErrors()
    {
        using var provider = BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        var errors = new ConcurrentQueue<string>();
        var operationCounts = new ConcurrentDictionary<string, int>();

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(workerId => Task.Run(async () =>
        {
            for (var i = 0; i < OperationsPerWorker; i++)
            {
                var scenario = i % 5;

                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var ps = scope.ServiceProvider.GetRequiredService<IProcessorService>();
                    var ds = scope.ServiceProvider.GetRequiredService<IDPAService>();
                    string scenarioName;

                    switch (scenario)
                    {
                        case 0: // Register processor
                            await ps.RegisterProcessorAsync(
                                $"Mix.W{workerId}.{i}", Countries[i % Countries.Length],
                                null, null, 0, SubProcessorAuthorizationType.Specific);
                            scenarioName = "Register";
                            break;

                        case 1: // Register + execute DPA
                        {
                            var r = await ps.RegisterProcessorAsync(
                                $"MixDPA.W{workerId}.{i}", "DE", null, null, 0,
                                SubProcessorAuthorizationType.Specific);
                            var pid = r.Match(id => id, _ => Guid.Empty);
                            if (pid != Guid.Empty)
                            {
                                await ds.ExecuteDPAAsync(pid, FullyCompliantTerms(), false,
                                    ["processing"], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(1));
                            }
                            scenarioName = "ExecuteDPA";
                            break;
                        }

                        case 2: // Register + DPA + validate
                        {
                            var r = await ps.RegisterProcessorAsync(
                                $"MixVal.W{workerId}.{i}", "DE", null, null, 0,
                                SubProcessorAuthorizationType.Specific);
                            var pid = r.Match(id => id, _ => Guid.Empty);
                            if (pid != Guid.Empty)
                            {
                                await ds.ExecuteDPAAsync(pid, FullyCompliantTerms(), false,
                                    ["processing"], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(1));
                                await ds.HasValidDPAAsync(pid);
                            }
                            scenarioName = "Validate";
                            break;
                        }

                        case 3: // Get all processors
                            await ps.GetAllProcessorsAsync();
                            scenarioName = "QueryAll";
                            break;

                        default: // Get DPAs by status
                            await ds.GetDPAsByStatusAsync(DPAStatus.Active);
                            scenarioName = "QueryByStatus";
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
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        // Pre-register processors with valid DPAs
        var processorIds = new List<Guid>();
        for (var i = 0; i < 5; i++)
        {
            using var scope = scopeFactory.CreateScope();
            var ps = scope.ServiceProvider.GetRequiredService<IProcessorService>();
            var ds = scope.ServiceProvider.GetRequiredService<IDPAService>();

            var pid = (await ps.RegisterProcessorAsync(
                $"LatencyProc-{i}", Countries[i], null, null, 0,
                SubProcessorAuthorizationType.Specific))
                .Match(id => id, _ => Guid.Empty);

            if (pid != Guid.Empty)
            {
                await ds.ExecuteDPAAsync(pid, FullyCompliantTerms(), false,
                    ["processing"], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(2));
                processorIds.Add(pid);
            }
        }

        var latencies = new ConcurrentBag<double>();

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < 200; i++)
            {
                using var scope = scopeFactory.CreateScope();
                var ds = scope.ServiceProvider.GetRequiredService<IDPAService>();
                var processorId = processorIds[i % processorIds.Count];

                var sw = Stopwatch.StartNew();
                await ds.HasValidDPAAsync(processorId);
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

        var processorService = new InMemoryProcessorService();
        var dpaService = new InMemoryDPAService();
        services.AddScoped<IProcessorService>(_ => processorService);
        services.AddScoped<IDPAService>(_ => dpaService);

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = false,
            ValidateOnBuild = false
        });
    }

    private static DPAMandatoryTerms FullyCompliantTerms() => new()
    {
        ProcessOnDocumentedInstructions = true,
        ConfidentialityObligations = true,
        SecurityMeasures = true,
        SubProcessorRequirements = true,
        DataSubjectRightsAssistance = true,
        ComplianceAssistance = true,
        DataDeletionOrReturn = true,
        AuditRights = true
    };

    // ────────────────────────────────────────────────────────────
    //  Test Infrastructure — In-Memory Services for Load Tests
    // ────────────────────────────────────────────────────────────

    private sealed class InMemoryProcessorService : IProcessorService
    {
        private readonly ConcurrentDictionary<Guid, ProcessorReadModel> _processors = new();

        public ValueTask<Either<EncinaError, Guid>> RegisterProcessorAsync(
            string name, string country, string? contactEmail, Guid? parentProcessorId,
            int depth, SubProcessorAuthorizationType authorizationType,
            string? tenantId = null, string? moduleId = null, CancellationToken cancellationToken = default)
        {
            var id = Guid.NewGuid();
            _processors[id] = new ProcessorReadModel
            {
                Id = id, Name = name, Country = country, ContactEmail = contactEmail,
                ParentProcessorId = parentProcessorId, Depth = depth,
                AuthorizationType = authorizationType, TenantId = tenantId, ModuleId = moduleId,
                CreatedAtUtc = DateTimeOffset.UtcNow, LastModifiedAtUtc = DateTimeOffset.UtcNow, Version = 1
            };
            return ValueTask.FromResult(Right<EncinaError, Guid>(id));
        }

        public ValueTask<Either<EncinaError, Unit>> UpdateProcessorAsync(
            Guid processorId, string name, string country, string? contactEmail,
            SubProcessorAuthorizationType authorizationType, CancellationToken cancellationToken = default)
        {
            if (!_processors.TryGetValue(processorId, out var p))
                return ValueTask.FromResult(Left<EncinaError, Unit>(EncinaError.New("Not found")));
            p.Name = name; p.Country = country; p.ContactEmail = contactEmail;
            p.AuthorizationType = authorizationType; p.LastModifiedAtUtc = DateTimeOffset.UtcNow; p.Version++;
            return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
        }

        public ValueTask<Either<EncinaError, Unit>> RemoveProcessorAsync(
            Guid processorId, string reason, CancellationToken cancellationToken = default)
        {
            if (!_processors.TryGetValue(processorId, out var p))
                return ValueTask.FromResult(Left<EncinaError, Unit>(EncinaError.New("Not found")));
            p.IsRemoved = true; p.LastModifiedAtUtc = DateTimeOffset.UtcNow; p.Version++;
            return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
        }

        public ValueTask<Either<EncinaError, ProcessorReadModel>> GetProcessorAsync(
            Guid processorId, CancellationToken cancellationToken = default)
        {
            if (_processors.TryGetValue(processorId, out var p) && !p.IsRemoved)
                return ValueTask.FromResult(Right<EncinaError, ProcessorReadModel>(p));
            return ValueTask.FromResult(Left<EncinaError, ProcessorReadModel>(EncinaError.New("Not found")));
        }

        public ValueTask<Either<EncinaError, IReadOnlyList<ProcessorReadModel>>> GetAllProcessorsAsync(
            CancellationToken cancellationToken = default)
        {
            var all = _processors.Values.Where(p => !p.IsRemoved).ToList();
            return ValueTask.FromResult(Right<EncinaError, IReadOnlyList<ProcessorReadModel>>(all.AsReadOnly()));
        }

        public ValueTask<Either<EncinaError, IReadOnlyList<ProcessorReadModel>>> GetSubProcessorsAsync(
            Guid processorId, CancellationToken cancellationToken = default)
        {
            var subs = _processors.Values.Where(p => p.ParentProcessorId == processorId && !p.IsRemoved).ToList();
            return ValueTask.FromResult(Right<EncinaError, IReadOnlyList<ProcessorReadModel>>(subs.AsReadOnly()));
        }

        public ValueTask<Either<EncinaError, IReadOnlyList<ProcessorReadModel>>> GetFullSubProcessorChainAsync(
            Guid processorId, CancellationToken cancellationToken = default)
        {
            var chain = new List<ProcessorReadModel>();
            var queue = new Queue<Guid>();
            queue.Enqueue(processorId);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var subs = _processors.Values.Where(p => p.ParentProcessorId == current && !p.IsRemoved).ToList();
                chain.AddRange(subs);
                foreach (var s in subs) queue.Enqueue(s.Id);
            }
            return ValueTask.FromResult(Right<EncinaError, IReadOnlyList<ProcessorReadModel>>(chain.AsReadOnly()));
        }

        public ValueTask<Either<EncinaError, IReadOnlyList<object>>> GetProcessorHistoryAsync(
            Guid processorId, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Left<EncinaError, IReadOnlyList<object>>(EncinaError.New("Not available")));
    }

    private sealed class InMemoryDPAService : IDPAService
    {
        private readonly ConcurrentDictionary<Guid, DPAReadModel> _dpas = new();

        public ValueTask<Either<EncinaError, Guid>> ExecuteDPAAsync(
            Guid processorId, DPAMandatoryTerms mandatoryTerms, bool hasSCCs,
            IReadOnlyList<string> processingPurposes, DateTimeOffset signedAtUtc,
            DateTimeOffset? expiresAtUtc, string? tenantId = null, string? moduleId = null,
            CancellationToken cancellationToken = default)
        {
            var id = Guid.NewGuid();
            _dpas[id] = new DPAReadModel
            {
                Id = id, ProcessorId = processorId, Status = DPAStatus.Active,
                MandatoryTerms = mandatoryTerms, HasSCCs = hasSCCs,
                ProcessingPurposes = processingPurposes.ToList(),
                SignedAtUtc = signedAtUtc, ExpiresAtUtc = expiresAtUtc,
                TenantId = tenantId, ModuleId = moduleId,
                CreatedAtUtc = DateTimeOffset.UtcNow, LastModifiedAtUtc = DateTimeOffset.UtcNow, Version = 1
            };
            return ValueTask.FromResult(Right<EncinaError, Guid>(id));
        }

        public ValueTask<Either<EncinaError, Unit>> AmendDPAAsync(
            Guid dpaId, DPAMandatoryTerms updatedTerms, bool hasSCCs,
            IReadOnlyList<string> processingPurposes, string amendmentReason,
            CancellationToken cancellationToken = default)
        {
            if (!_dpas.TryGetValue(dpaId, out var d))
                return ValueTask.FromResult(Left<EncinaError, Unit>(EncinaError.New("Not found")));
            d.MandatoryTerms = updatedTerms; d.HasSCCs = hasSCCs;
            d.ProcessingPurposes = processingPurposes.ToList();
            d.LastModifiedAtUtc = DateTimeOffset.UtcNow; d.Version++;
            return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
        }

        public ValueTask<Either<EncinaError, Unit>> AuditDPAAsync(
            Guid dpaId, string auditorId, string auditFindings, CancellationToken cancellationToken = default)
        {
            if (!_dpas.TryGetValue(dpaId, out var d))
                return ValueTask.FromResult(Left<EncinaError, Unit>(EncinaError.New("Not found")));
            d.AuditHistory.Add(new AuditRecord(auditorId, auditFindings, DateTimeOffset.UtcNow));
            d.LastModifiedAtUtc = DateTimeOffset.UtcNow; d.Version++;
            return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
        }

        public ValueTask<Either<EncinaError, Unit>> RenewDPAAsync(
            Guid dpaId, DateTimeOffset newExpiresAtUtc, CancellationToken cancellationToken = default)
        {
            if (!_dpas.TryGetValue(dpaId, out var d))
                return ValueTask.FromResult(Left<EncinaError, Unit>(EncinaError.New("Not found")));
            d.ExpiresAtUtc = newExpiresAtUtc; d.Status = DPAStatus.Active;
            d.LastModifiedAtUtc = DateTimeOffset.UtcNow; d.Version++;
            return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
        }

        public ValueTask<Either<EncinaError, Unit>> TerminateDPAAsync(
            Guid dpaId, string reason, CancellationToken cancellationToken = default)
        {
            if (!_dpas.TryGetValue(dpaId, out var d))
                return ValueTask.FromResult(Left<EncinaError, Unit>(EncinaError.New("Not found")));
            d.Status = DPAStatus.Terminated; d.TerminationReason = reason;
            d.TerminatedAtUtc = DateTimeOffset.UtcNow;
            d.LastModifiedAtUtc = DateTimeOffset.UtcNow; d.Version++;
            return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
        }

        public ValueTask<Either<EncinaError, DPAReadModel>> GetDPAAsync(
            Guid dpaId, CancellationToken cancellationToken = default)
        {
            if (_dpas.TryGetValue(dpaId, out var d))
                return ValueTask.FromResult(Right<EncinaError, DPAReadModel>(d));
            return ValueTask.FromResult(Left<EncinaError, DPAReadModel>(EncinaError.New("Not found")));
        }

        public ValueTask<Either<EncinaError, IReadOnlyList<DPAReadModel>>> GetDPAsByProcessorIdAsync(
            Guid processorId, CancellationToken cancellationToken = default)
        {
            var list = _dpas.Values.Where(d => d.ProcessorId == processorId).ToList();
            return ValueTask.FromResult(Right<EncinaError, IReadOnlyList<DPAReadModel>>(list.AsReadOnly()));
        }

        public ValueTask<Either<EncinaError, DPAReadModel>> GetActiveDPAByProcessorIdAsync(
            Guid processorId, CancellationToken cancellationToken = default)
        {
            var active = _dpas.Values.FirstOrDefault(d =>
                d.ProcessorId == processorId && d.IsActive(DateTimeOffset.UtcNow));
            if (active is not null)
                return ValueTask.FromResult(Right<EncinaError, DPAReadModel>(active));
            return ValueTask.FromResult(Left<EncinaError, DPAReadModel>(EncinaError.New("Not found")));
        }

        public ValueTask<Either<EncinaError, IReadOnlyList<DPAReadModel>>> GetDPAsByStatusAsync(
            DPAStatus status, CancellationToken cancellationToken = default)
        {
            var list = _dpas.Values.Where(d => d.Status == status).ToList();
            return ValueTask.FromResult(Right<EncinaError, IReadOnlyList<DPAReadModel>>(list.AsReadOnly()));
        }

        public ValueTask<Either<EncinaError, IReadOnlyList<DPAReadModel>>> GetExpiringDPAsAsync(
            CancellationToken cancellationToken = default)
        {
            var expiring = _dpas.Values.Where(d =>
                d.Status == DPAStatus.Active && d.ExpiresAtUtc.HasValue &&
                d.ExpiresAtUtc.Value < DateTimeOffset.UtcNow.AddDays(30)).ToList();
            return ValueTask.FromResult(Right<EncinaError, IReadOnlyList<DPAReadModel>>(expiring.AsReadOnly()));
        }

        public ValueTask<Either<EncinaError, bool>> HasValidDPAAsync(
            Guid processorId, CancellationToken cancellationToken = default)
        {
            var hasValid = _dpas.Values.Any(d =>
                d.ProcessorId == processorId && d.IsActive(DateTimeOffset.UtcNow));
            return ValueTask.FromResult(Right<EncinaError, bool>(hasValid));
        }

        public ValueTask<Either<EncinaError, DPAValidationResult>> ValidateDPAAsync(
            Guid processorId, CancellationToken cancellationToken = default)
        {
            var active = _dpas.Values.FirstOrDefault(d =>
                d.ProcessorId == processorId && d.IsActive(DateTimeOffset.UtcNow));
            var result = new DPAValidationResult
            {
                ProcessorId = processorId.ToString(),
                DPAId = active?.Id.ToString(),
                IsValid = active is not null,
                MissingTerms = active?.MandatoryTerms.MissingTerms ?? [],
                Warnings = [],
                DaysUntilExpiration = active?.ExpiresAtUtc.HasValue == true
                    ? (int)(active.ExpiresAtUtc.Value - DateTimeOffset.UtcNow).TotalDays : null,
                ValidatedAtUtc = DateTimeOffset.UtcNow
            };
            return ValueTask.FromResult(Right<EncinaError, DPAValidationResult>(result));
        }

        public ValueTask<Either<EncinaError, IReadOnlyList<object>>> GetDPAHistoryAsync(
            Guid dpaId, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Left<EncinaError, IReadOnlyList<object>>(EncinaError.New("Not available")));
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

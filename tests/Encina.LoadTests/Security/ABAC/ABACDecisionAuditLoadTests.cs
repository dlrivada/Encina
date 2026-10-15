using System.Collections.Concurrent;
using System.Diagnostics;

using Encina.Security.ABAC;
using Encina.Security.Audit;
using Encina.Testing.Identity;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;

using static LanguageExt.Prelude;

namespace Encina.LoadTests.Security.ABAC;

/// <summary>
/// Load tests for the ABAC decision audit (#751 Phase 7): the Policy Enforcement Point evaluates
/// requests concurrently with the audit on, against the real Policy Decision Point, the real
/// recorder and an operation audit store.
/// </summary>
/// <remarks>
/// <para>
/// The scenarios check invariants, not only speed. An invariant that breaks throws, so the run
/// exits with a failure:
/// </para>
/// <list type="bullet">
/// <item>ConcurrentEvaluations: permits and denials from many workers write exactly one record each;
/// the handler runs for permits only.</item>
/// <item>DenialsUnderStoreFailure: when the store returns <c>Left</c> or throws, the fail-closed
/// default denies every request with <c>abac.decision_audit_failed</c> and the handler never runs.</item>
/// <item>BoundedWrites: a store that ignores its token and never answers cannot hold requests longer
/// than the configured write timeout.</item>
/// </list>
/// <para>
/// Run from the LoadTests project:
/// <code>
/// dotnet run --project tests/Encina.LoadTests -- --scenario abac-decision-audit --send-workers 8 --duration 00:00:10
/// </code>
/// </para>
/// </remarks>
public static class ABACDecisionAuditLoadTests
{
    /// <summary>DocRef identifier for load-test documentation traceability (see ADR-025).</summary>
    public const string DocRef = "load:abac/decision-audit";

    private const string PermitPolicy = "load-permit";
    private const string DenyPolicy = "load-deny";

    [RequirePolicy(PermitPolicy)]
    private sealed record PermitRequest : IRequest<string>;

    [RequirePolicy(DenyPolicy)]
    private sealed record DenyRequest : IRequest<string>;

    /// <summary>Runs every scenario in sequence.</summary>
    public static async Task RunAllAsync(int workerCount = 8, TimeSpan? duration = null)
    {
        var testDuration = duration ?? TimeSpan.FromSeconds(10);

        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("  ABAC Decision Audit Load Tests");
        Console.WriteLine("========================================");

        await RunConcurrentEvaluationsAsync(workerCount, testDuration).ConfigureAwait(false);
        await RunDenialsUnderStoreFailureAsync(workerCount, testDuration, StoreFault.ReturnsLeft).ConfigureAwait(false);
        await RunDenialsUnderStoreFailureAsync(workerCount, testDuration, StoreFault.Throws).ConfigureAwait(false);
        await RunBoundedWritesAsync(workerCount).ConfigureAwait(false);
    }

    /// <summary>
    /// Scenario 1: permits and denials from many workers with the audit on. Exactly one record is
    /// written per evaluated request, and the handler runs for permits only.
    /// </summary>
    public static async Task RunConcurrentEvaluationsAsync(int workerCount, TimeSpan duration)
    {
        Console.WriteLine();
        Console.WriteLine("Scenario: ConcurrentEvaluations (audit on, in-memory store)");

        var inner = new InMemoryOperationAuditStore();
        await using var provider = await BuildProviderAsync(inner, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        var counters = new Counters();

        var result = await RunWorkersAsync(provider, workerCount, duration, counters, alternate: true).ConfigureAwait(false);

        Require(counters.Errors == 0, $"{counters.Errors} evaluations failed unexpectedly.");
        Require(counters.Permits > 0 && counters.Denials > 0, "The run produced no permit or no denial.");
        Require(inner.Count == result.Operations, $"{result.Operations} evaluations wrote {inner.Count} records.");
        Require(counters.HandlerCalls == counters.Permits, "The handler ran for something other than a permit.");

        result.Print(counters, inner.Count);
    }

    /// <summary>
    /// Scenario 2: the store fails (a <c>Left</c> or an exception). Under the fail-closed default every
    /// request, permit or deny, is denied with <c>abac.decision_audit_failed</c>; the handler never runs.
    /// </summary>
    public static async Task RunDenialsUnderStoreFailureAsync(int workerCount, TimeSpan duration, StoreFault fault)
    {
        Console.WriteLine();
        Console.WriteLine($"Scenario: DenialsUnderStoreFailure ({fault})");

        var inner = new InMemoryOperationAuditStore();
        var store = new FaultyOperationAuditStore(inner, fault);
        await using var provider = await BuildProviderAsync(store, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        var counters = new Counters();

        var result = await RunWorkersAsync(provider, workerCount, duration, counters, alternate: true).ConfigureAwait(false);

        Require(result.Operations > 0, "The run produced no evaluation.");
        Require(counters.HandlerCalls == 0, "The handler ran although the decision record could not be written.");
        Require(counters.Permits == 0, "A request proceeded without its decision record.");
        Require(inner.Count == 0, "The failing store kept a record.");

        // A permit is denied for the audit failure; a deny keeps its own denial, which is also recorded in the Left.
        Require(counters.AuditFailures > 0, "No request was denied with abac.decision_audit_failed.");
        Require(counters.AuditFailures + counters.Denials + counters.Errors == result.Operations, "Some request had no outcome.");

        result.Print(counters, inner.Count);
    }

    /// <summary>
    /// Scenario 3: a store that ignores its token and never answers. Each request ends at the write
    /// timeout, denied, never hanging.
    /// </summary>
    public static async Task RunBoundedWritesAsync(int workerCount)
    {
        Console.WriteLine();
        Console.WriteLine("Scenario: BoundedWrites (store ignores its token)");

        var timeout = TimeSpan.FromMilliseconds(100);
        var store = new FaultyOperationAuditStore(new InMemoryOperationAuditStore(), StoreFault.NeverAnswers);
        await using var provider = await BuildProviderAsync(store, timeout).ConfigureAwait(false);
        var counters = new Counters();

        const int requestsPerWorker = 5;
        var latencies = new ConcurrentBag<double>();
        var sw = Stopwatch.StartNew();

        await Task.WhenAll(Enumerable.Range(0, workerCount).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < requestsPerWorker; i++)
            {
                var local = Stopwatch.StartNew();
                await EvaluateAsync<PermitRequest>(provider, counters, CancellationToken.None).ConfigureAwait(false);
                latencies.Add(local.Elapsed.TotalMilliseconds);
            }
        }))).ConfigureAwait(false);

        sw.Stop();
        var total = workerCount * requestsPerWorker;
        var max = latencies.Max();

        Require(counters.HandlerCalls == 0, "The handler ran although the write timed out.");
        Require(counters.AuditFailures + counters.Errors == total, "Some request had no failed outcome.");

        // Two bounds (the write, then the lookup) plus scheduling headroom; far below a hang.
        Require(max < timeout.TotalMilliseconds * 2 + 2_000, $"A request took {max:F0} ms; the write bound is {timeout.TotalMilliseconds:F0} ms.");

        Console.WriteLine($"Workers: {workerCount} | Requests: {total} | Elapsed: {sw.Elapsed.TotalSeconds:F2}s | Max latency: {max:F0} ms (write bound {timeout.TotalMilliseconds:F0} ms)");
        Console.WriteLine($"Denied by audit failure: {counters.AuditFailures} | Thrown: {counters.Errors} | Handler calls: {counters.HandlerCalls}");
    }

    // ── Harness ──────────────────────────────────────────────────────

    private static async Task<ServiceProvider> BuildProviderAsync(IOperationAuditStore store, TimeSpan writeTimeout)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IOperationAuditStore>(_ => store);
        services.AddEncinaABAC(options =>
        {
            options.EnforcementMode = ABACEnforcementMode.Block;
            options.DecisionAudit.Enabled = true;
            options.DecisionAudit.WriteTimeout = writeTimeout;
        });

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = false, ValidateScopes = true });

        var pap = provider.GetRequiredService<IPolicyAdministrationPoint>();
        await pap.AddPolicyAsync(OnePolicy(PermitPolicy, Effect.Permit), null).ConfigureAwait(false);
        await pap.AddPolicyAsync(OnePolicy(DenyPolicy, Effect.Deny), null).ConfigureAwait(false);
        return provider;
    }

    private static Policy OnePolicy(string id, Effect effect) => new()
    {
        Id = id,
        IsEnabled = true,
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Target = null,
        Rules =
        [
            new Rule
            {
                Id = id + "-rule",
                Effect = effect,
                Target = null,
                Condition = null,
                Description = null,
                Obligations = [],
                Advice = []
            }
        ],
        Obligations = [],
        Advice = [],
        VariableDefinitions = []
    };

    private static async Task<RunResult> RunWorkersAsync(
        ServiceProvider provider, int workerCount, TimeSpan duration, Counters counters, bool alternate)
    {
        var latencies = new ConcurrentBag<double>();
        long operations = 0;
        using var cts = new CancellationTokenSource(duration);
        var sw = Stopwatch.StartNew();

        await Task.WhenAll(Enumerable.Range(0, workerCount).Select(_ => Task.Run(async () =>
        {
            var local = new Stopwatch();
            var flip = false;
            while (!cts.IsCancellationRequested)
            {
                local.Restart();

                // The token of the run is not passed to the request: a request in flight at the end finishes
                // and is counted, so the record count and the operation count can be compared exactly.
                if (alternate && (flip = !flip))
                {
                    await EvaluateAsync<DenyRequest>(provider, counters, CancellationToken.None).ConfigureAwait(false);
                }
                else
                {
                    await EvaluateAsync<PermitRequest>(provider, counters, CancellationToken.None).ConfigureAwait(false);
                }

                latencies.Add(local.Elapsed.TotalMilliseconds);
                Interlocked.Increment(ref operations);
            }
        }))).ConfigureAwait(false);

        sw.Stop();
        return new RunResult(operations, sw.Elapsed, latencies.OrderBy(l => l).ToArray(), workerCount);
    }

    private static async Task EvaluateAsync<TRequest>(ServiceProvider provider, Counters counters, CancellationToken cancellationToken)
        where TRequest : IRequest<string>, new()
    {
        await using var scope = provider.CreateAsyncScope();
        var behavior = scope.ServiceProvider.GetServices<IPipelineBehavior<TRequest, string>>().Single();
        var context = TestRequestContext.For(TestIdentity.User("load-user"), correlationId: $"load-{Guid.NewGuid():N}");

        try
        {
            var result = await behavior.Handle(
                new TRequest(),
                context,
                () =>
                {
                    Interlocked.Increment(ref counters.HandlerCalls);
                    return ValueTask.FromResult(Right<EncinaError, string>("ok"));
                },
                cancellationToken).ConfigureAwait(false);

            if (result.IsRight)
            {
                Interlocked.Increment(ref counters.Permits);
            }
            else if (result.LeftToSeq().Any(error => error.GetCode().IfNone(string.Empty) == ABACErrors.DecisionAuditFailedCode))
            {
                Interlocked.Increment(ref counters.AuditFailures);
            }
            else
            {
                Interlocked.Increment(ref counters.Denials);
            }
        }
        catch (Exception)
        {
            // A store that throws surfaces as the recorder's exception, which the PEP converts to the audit failure.
            Interlocked.Increment(ref counters.Errors);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException("Load invariant broken: " + message);
        }
    }

    private sealed class Counters
    {
        public long Permits;
        public long Denials;
        public long AuditFailures;
        public long Errors;
        public long HandlerCalls;
    }

    private sealed record RunResult(long Operations, TimeSpan Elapsed, double[] SortedLatencies, int Workers)
    {
        public void Print(Counters counters, int records)
        {
            double P(double q) => SortedLatencies.Length == 0 ? 0 : SortedLatencies[(int)Math.Min(SortedLatencies.Length - 1, q * SortedLatencies.Length)];

            Console.WriteLine($"Workers: {Workers} | Elapsed: {Elapsed.TotalSeconds:F2}s | Evaluations: {Operations} ({Operations / Elapsed.TotalSeconds:F0}/s)");
            Console.WriteLine($"Latency ms: P50 {P(0.50):F3} | P95 {P(0.95):F3} | P99 {P(0.99):F3}");
            Console.WriteLine($"Permits: {counters.Permits} | Denials: {counters.Denials} | Denied by audit failure: {counters.AuditFailures} | Thrown: {counters.Errors} | Handler calls: {counters.HandlerCalls} | Records: {records}");
        }
    }

    /// <summary>How the faulty store fails.</summary>
    public enum StoreFault
    {
        /// <summary>Every write returns a <c>Left</c>.</summary>
        ReturnsLeft,

        /// <summary>Every write throws.</summary>
        Throws,

        /// <summary>Every write waits forever, ignoring its token.</summary>
        NeverAnswers
    }

    private sealed class FaultyOperationAuditStore(IOperationAuditStore inner, StoreFault fault) : IOperationAuditStore
    {
        public ValueTask<Either<EncinaError, Unit>> RecordAsync(OperationAuditEntry entry, CancellationToken cancellationToken = default) =>
            fault switch
            {
                StoreFault.ReturnsLeft => ValueTask.FromResult(Left<EncinaError, Unit>(EncinaErrors.Create(code: "load.store_failed", message: "The store is down."))),
                StoreFault.Throws => throw new InvalidOperationException("The store is down."),
                _ => new ValueTask<Either<EncinaError, Unit>>(new TaskCompletionSource<Either<EncinaError, Unit>>().Task)
            };

        public ValueTask<Either<EncinaError, IReadOnlyList<OperationAuditEntry>>> GetByEntityAsync(string entityType, string? entityId, CancellationToken cancellationToken = default) =>
            inner.GetByEntityAsync(entityType, entityId, cancellationToken);

        public ValueTask<Either<EncinaError, IReadOnlyList<OperationAuditEntry>>> GetByUserAsync(string userId, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default) =>
            inner.GetByUserAsync(userId, fromUtc, toUtc, cancellationToken);

        public ValueTask<Either<EncinaError, IReadOnlyList<OperationAuditEntry>>> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default) =>
            inner.GetByCorrelationIdAsync(correlationId, cancellationToken);

        public ValueTask<Either<EncinaError, PagedResult<OperationAuditEntry>>> QueryAsync(OperationAuditQuery query, CancellationToken cancellationToken = default) =>
            inner.QueryAsync(query, cancellationToken);

        public ValueTask<Either<EncinaError, int>> PurgeEntriesAsync(DateTime olderThanUtc, CancellationToken cancellationToken = default) =>
            inner.PurgeEntriesAsync(olderThanUtc, cancellationToken);
    }
}

using BenchmarkDotNet.Attributes;

using Encina.Security.ABAC;
using Encina.Security.Audit;
using Encina.Testing.Identity;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;

using static LanguageExt.Prelude;

namespace Encina.Benchmarks.Security.ABAC;

/// <summary>
/// Benchmarks for the ABAC decision audit (#751 Phase 7): what the Policy Enforcement Point costs
/// with the audit off and on, and what the evaluation trace costs the Policy Decision Point.
/// </summary>
/// <remarks>
/// <para>
/// <b>PEP</b>: the same permitted request goes through <see cref="ABACPipelineBehavior{TRequest, TResponse}"/>
/// with <see cref="ABACDecisionAuditOptions.Enabled"/> off and on. The audited run writes through the
/// real recorder to a store that discards the entry, so the numbers show the audit pipeline (record,
/// mapper, scope, write) and not the speed of a particular database.
/// </para>
/// <para>
/// <b>PDP</b>: the same policy (five rules) is evaluated without and with the evaluation trace that the
/// audit asks for by default.
/// </para>
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
[GroupBenchmarksBy(BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ABACDecisionAuditBenchmarks
{
    private const string PolicyId = "bench-policy";

    [RequirePolicy(PolicyId)]
    private sealed record BenchRequest : IRequest<string>;

    private ServiceProvider _auditOff = default!;
    private ServiceProvider _auditOn = default!;
    private AsyncServiceScope _offScope;
    private AsyncServiceScope _onScope;
    private IPipelineBehavior<BenchRequest, string> _pepOff = default!;
    private IPipelineBehavior<BenchRequest, string> _pepOn = default!;
    private IPolicyDecisionPoint _pdp = default!;
    private IRequestContext _context = default!;
    private PolicyEvaluationContext _plainContext = default!;
    private PolicyEvaluationContext _tracedContext = default!;

    [GlobalSetup]
    public async Task Setup()
    {
        _auditOff = await BuildAsync(auditEnabled: false).ConfigureAwait(false);
        _auditOn = await BuildAsync(auditEnabled: true).ConfigureAwait(false);

        _offScope = _auditOff.CreateAsyncScope();
        _onScope = _auditOn.CreateAsyncScope();
        _pepOff = _offScope.ServiceProvider.GetServices<IPipelineBehavior<BenchRequest, string>>().Single();
        _pepOn = _onScope.ServiceProvider.GetServices<IPipelineBehavior<BenchRequest, string>>().Single();
        _pdp = _auditOff.GetRequiredService<IPolicyDecisionPoint>();
        _context = TestRequestContext.For(TestIdentity.User("bench-user"), correlationId: "bench-correlation");

        _plainContext = CreateEvaluationContext(trace: false);
        _tracedContext = CreateEvaluationContext(trace: true);

        // Each benchmark must time the path it names: a permit through the PEP and a Permit from the PDP.
        if (!await Pep_AuditOff().ConfigureAwait(false) || !await Pep_AuditOn().ConfigureAwait(false))
        {
            throw new InvalidOperationException("The PEP did not permit the benchmark request.");
        }

        if (await Pdp_WithoutTrace().ConfigureAwait(false) != Effect.Permit || await Pdp_WithTrace().ConfigureAwait(false) != Effect.Permit)
        {
            throw new InvalidOperationException("The PDP did not permit the benchmark policy.");
        }
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _offScope.DisposeAsync().ConfigureAwait(false);
        await _onScope.DisposeAsync().ConfigureAwait(false);
        await _auditOff.DisposeAsync().ConfigureAwait(false);
        await _auditOn.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>PEP with the decision audit off (the baseline).</summary>
    [BenchmarkCategory("PEP")]
    [Benchmark(Baseline = true)]
    public async ValueTask<bool> Pep_AuditOff() =>
        (await _pepOff.Handle(new BenchRequest(), _context, Ok, CancellationToken.None).ConfigureAwait(false)).IsRight;

    /// <summary>PEP with the decision audit on: record, map and write before the handler runs.</summary>
    [BenchmarkCategory("PEP")]
    [Benchmark]
    public async ValueTask<bool> Pep_AuditOn() =>
        (await _pepOn.Handle(new BenchRequest(), _context, Ok, CancellationToken.None).ConfigureAwait(false)).IsRight;

    /// <summary>PDP evaluation without a trace (the baseline of the PDP pair).</summary>
    [BenchmarkCategory("PDP")]
    [Benchmark(Baseline = true)]
    public async ValueTask<Effect> Pdp_WithoutTrace() =>
        (await _pdp.EvaluatePolicyAsync(PolicyId, _plainContext, CancellationToken.None).ConfigureAwait(false))
            .Match(Right: decision => decision.Effect, Left: _ => Effect.Indeterminate);

    /// <summary>PDP evaluation with the evaluation trace.</summary>
    [BenchmarkCategory("PDP")]
    [Benchmark]
    public async ValueTask<Effect> Pdp_WithTrace() =>
        (await _pdp.EvaluatePolicyAsync(PolicyId, _tracedContext, CancellationToken.None).ConfigureAwait(false))
            .Match(Right: decision => decision.Effect, Left: _ => Effect.Indeterminate);

    private static ValueTask<Either<EncinaError, string>> Ok() => ValueTask.FromResult(Right<EncinaError, string>("ok"));

    private static async Task<ServiceProvider> BuildAsync(bool auditEnabled)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IOperationAuditStore, DiscardingOperationAuditStore>();
        services.AddEncinaABAC(options =>
        {
            options.EnforcementMode = ABACEnforcementMode.Block;
            options.DecisionAudit.Enabled = auditEnabled;
        });

        var provider = services.BuildServiceProvider();
        await provider.GetRequiredService<IPolicyAdministrationPoint>().AddPolicyAsync(Policy(), null).ConfigureAwait(false);
        return provider;
    }

    // Five rules: a target-less deny-overrides policy whose rules all permit, so every rule is evaluated and traced.
    private static Policy Policy() => new()
    {
        Id = PolicyId,
        IsEnabled = true,
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Target = null,
        Rules = Enumerable.Range(1, 5)
            .Select(index => new Rule
            {
                Id = $"rule-{index}",
                Effect = Effect.Permit,
                Target = null,
                Condition = null,
                Description = null,
                Obligations = [],
                Advice = []
            })
            .ToList(),
        Obligations = [],
        Advice = [],
        VariableDefinitions = []
    };

    private static PolicyEvaluationContext CreateEvaluationContext(bool trace) => new()
    {
        SubjectAttributes = new Dictionary<string, AttributeBag>
        {
            ["role"] = AttributeBag.Of(new AttributeValue { DataType = XACMLDataTypes.String, Value = "admin" })
        },
        ResourceAttributes = new Dictionary<string, AttributeBag>(),
        ActionAttributes = new Dictionary<string, AttributeBag>
        {
            ["name"] = AttributeBag.Of(new AttributeValue { DataType = XACMLDataTypes.String, Value = nameof(BenchRequest) })
        },
        EnvironmentAttributes = new Dictionary<string, AttributeBag>(),
        RequestType = typeof(BenchRequest),
        IncludeEvaluationTrace = trace
    };

    /// <summary>A store that accepts every entry and keeps nothing, so a run measures the audit pipeline only.</summary>
    private sealed class DiscardingOperationAuditStore : IOperationAuditStore
    {
        private static readonly Either<EncinaError, IReadOnlyList<OperationAuditEntry>> NoEntries =
            Right<EncinaError, IReadOnlyList<OperationAuditEntry>>([]);

        public ValueTask<Either<EncinaError, Unit>> RecordAsync(OperationAuditEntry entry, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));

        public ValueTask<Either<EncinaError, IReadOnlyList<OperationAuditEntry>>> GetByEntityAsync(string entityType, string? entityId, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(NoEntries);

        public ValueTask<Either<EncinaError, IReadOnlyList<OperationAuditEntry>>> GetByUserAsync(string userId, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(NoEntries);

        public ValueTask<Either<EncinaError, IReadOnlyList<OperationAuditEntry>>> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(NoEntries);

        public ValueTask<Either<EncinaError, PagedResult<OperationAuditEntry>>> QueryAsync(OperationAuditQuery query, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Right<EncinaError, PagedResult<OperationAuditEntry>>(PagedResult<OperationAuditEntry>.Empty()));

        public ValueTask<Either<EncinaError, int>> PurgeEntriesAsync(DateTime olderThanUtc, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Right<EncinaError, int>(0));
    }
}

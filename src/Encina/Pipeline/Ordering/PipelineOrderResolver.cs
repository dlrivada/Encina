using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Encina;

/// <summary>
/// Position of one step: stage, order inside the stage, and where they came from.
/// </summary>
internal readonly record struct ResolvedPosition(PipelineStage Stage, int Order, PipelineStepSource Source);

/// <summary>
/// Computes, once per closed pipeline, the execution order of the resolved steps.
/// </summary>
/// <remarks>
/// <para>
/// The order key is (stage, order, processors before behaviors, then <see cref="Type.FullName"/> for processors and the
/// registration index for behaviors). Registration order therefore only breaks ties between behaviors that declare the
/// same stage and order. The position of a step comes from, in this precedence, the <see cref="PipelineStageRegistry"/>,
/// the <see cref="PipelineStageAttribute"/>, and the default of its kind (behaviors <c>Application</c>/0;
/// pre, post and error processors <c>Application</c>/-100; post-commit processors <c>Transaction</c>/-100).
/// </para>
/// <para>
/// Steps with the same identity (same step type and kind) are collapsed to the one with the lowest registration index.
/// The permutation is cached per closed pipeline type and recomputed only when the sequence of step types differs, which
/// costs one pass over the candidates per request. The first computation of a pipeline runs exactly once even when
/// several requests build it concurrently.
/// </para>
/// </remarks>
internal sealed class PipelineOrderResolver
{
    private static readonly Comparer<SortKey> KeyComparer = Comparer<SortKey>.Create(CompareKeys);

    private readonly PipelineStageRegistry _registry;
    private readonly ILogger<PipelineOrderResolver> _logger;
    private readonly ConcurrentDictionary<Type, Slot> _slots = new();
    private int _computations;

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineOrderResolver"/> class.
    /// </summary>
    /// <param name="registry">Registration-time overrides.</param>
    /// <param name="logger">Logger for the resolver diagnostics.</param>
    public PipelineOrderResolver(PipelineStageRegistry registry, ILogger<PipelineOrderResolver> logger)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Gets the logger of the resolver (collapse and placement diagnostics arrive with the observability phase).</summary>
    internal ILogger Logger => _logger;

    /// <summary>Gets how many permutations were computed (a cache hit does not count).</summary>
    internal int Computations => Volatile.Read(ref _computations);

    /// <summary>
    /// Returns the execution order of the candidates, outermost first.
    /// </summary>
    /// <param name="pipelineKey">The closed pipeline type the candidates belong to.</param>
    /// <param name="candidates">The resolved steps, in registration order.</param>
    /// <returns>
    /// Indexes into <paramref name="candidates"/>, outermost first; collapsed duplicates are omitted.
    /// The array is shared with the cache and must not be modified.
    /// </returns>
    public int[] Resolve(Type pipelineKey, IReadOnlyList<PipelineStepCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(pipelineKey);
        ArgumentNullException.ThrowIfNull(candidates);

        var slot = _slots.GetOrAdd(pipelineKey, static _ => new Slot());
        var current = slot.Current;
        return current is not null && current.Matches(candidates)
            ? current.Order
            : Refresh(slot, candidates);
    }

    /// <summary>
    /// Resolves the position of one step.
    /// </summary>
    /// <param name="candidate">The step.</param>
    internal ResolvedPosition ResolvePosition(in PipelineStepCandidate candidate)
    {
        if (_registry.TryGet(candidate.StepType, out var overridden))
        {
            return new ResolvedPosition(overridden.Stage, overridden.Order, PipelineStepSource.Registration);
        }

        return TryReadDeclared(candidate, out var declared)
            ? new ResolvedPosition(declared.Stage, declared.Order, PipelineStepSource.Attribute)
            : DefaultFor(candidate.Kind);
    }

    private int[] Refresh(Slot slot, IReadOnlyList<PipelineStepCandidate> candidates)
    {
        lock (slot)
        {
            var current = slot.Current;
            if (current is not null && current.Matches(candidates))
            {
                return current.Order;
            }

            var created = new CachedOrder(Signature(candidates), Compute(candidates));
            slot.Current = created;
            Interlocked.Increment(ref _computations);
            return created.Order;
        }
    }

    private int[] Compute(IReadOnlyList<PipelineStepCandidate> candidates)
    {
        var kept = CollapseDuplicates(candidates);
        var keys = new SortKey[kept.Length];
        for (var i = 0; i < kept.Length; i++)
        {
            keys[i] = BuildKey(candidates[kept[i]]);
        }

        Array.Sort(keys, kept, KeyComparer);
        return kept;
    }

    private static int[] CollapseDuplicates(IReadOnlyList<PipelineStepCandidate> candidates)
    {
        var seen = new HashSet<(Type, PipelineStepKind)>();
        var kept = new List<int>(candidates.Count);
        for (var i = 0; i < candidates.Count; i++)
        {
            if (seen.Add((candidates[i].StepType, candidates[i].Kind)))
            {
                kept.Add(i);
            }
        }

        return [.. kept];
    }

    private SortKey BuildKey(in PipelineStepCandidate candidate)
    {
        var position = ResolvePosition(candidate);
        var processor = IsProcessor(candidate.Kind);
        return new SortKey(
            position.Stage,
            position.Order,
            processor ? 0 : 1,
            processor ? candidate.StepType.FullName ?? candidate.StepType.Name : null,
            candidate.RegistrationIndex);
    }

    private static int CompareKeys(SortKey left, SortKey right)
    {
        var result = ((int)left.Stage).CompareTo((int)right.Stage);
        if (result == 0)
        {
            result = left.Order.CompareTo(right.Order);
        }

        if (result == 0)
        {
            result = left.Rank.CompareTo(right.Rank);
        }

        if (result == 0)
        {
            result = string.CompareOrdinal(left.Name, right.Name);
        }

        return result != 0 ? result : left.Index.CompareTo(right.Index);
    }

    private static bool TryReadDeclared(in PipelineStepCandidate candidate, out (PipelineStage Stage, int Order) declared)
    {
        if (candidate.Identity is { } identity)
        {
            declared = (identity.DeclaredStage.GetValueOrDefault(), identity.DeclaredOrder.GetValueOrDefault());
            return identity.DeclaredStage.HasValue;
        }

        var attribute = PipelineStageLookup.Find(candidate.RuntimeType);
        declared = attribute is null ? default : (attribute.Stage, attribute.Order);
        return attribute is not null;
    }

    // crap-exempt: single-question switch — maps a step kind to its default position
    private static ResolvedPosition DefaultFor(PipelineStepKind kind) => kind switch
    {
        PipelineStepKind.PostCommitProcessor => new ResolvedPosition(PipelineStage.Transaction, -100, PipelineStepSource.Default),
        PipelineStepKind.PreProcessor or PipelineStepKind.PostProcessor or PipelineStepKind.ErrorProcessor
            => new ResolvedPosition(PipelineStage.Application, -100, PipelineStepSource.Default),
        _ => new ResolvedPosition(PipelineStage.Application, 0, PipelineStepSource.Default)
    };

    private static bool IsProcessor(PipelineStepKind kind)
        => kind is PipelineStepKind.PreProcessor or PipelineStepKind.PostProcessor
            or PipelineStepKind.ErrorProcessor or PipelineStepKind.PostCommitProcessor;

    private static (Type, PipelineStepKind)[] Signature(IReadOnlyList<PipelineStepCandidate> candidates)
    {
        var signature = new (Type, PipelineStepKind)[candidates.Count];
        for (var i = 0; i < signature.Length; i++)
        {
            signature[i] = (candidates[i].StepType, candidates[i].Kind);
        }

        return signature;
    }

    private readonly record struct SortKey(PipelineStage Stage, int Order, int Rank, string? Name, int Index);

    private sealed class Slot
    {
        public volatile CachedOrder? Current;
    }

    private sealed class CachedOrder((Type, PipelineStepKind)[] signature, int[] order)
    {
        public int[] Order { get; } = order;

        public bool Matches(IReadOnlyList<PipelineStepCandidate> candidates)
        {
            if (candidates.Count != signature.Length)
            {
                return false;
            }

            for (var i = 0; i < signature.Length; i++)
            {
                if (signature[i].Item1 != candidates[i].StepType || signature[i].Item2 != candidates[i].Kind)
                {
                    return false;
                }
            }

            return true;
        }
    }
}

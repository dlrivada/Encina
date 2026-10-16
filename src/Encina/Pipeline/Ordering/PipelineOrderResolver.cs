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
/// The order key is (stage, order, kind rank, then <see cref="Type.FullName"/> for processors and the registration index
/// for behaviors). The kind rank is pre-processor, error processor, post-processor, post-commit processor, behavior, so at
/// equal position the nesting is fixed (an error processor also observes the <c>Left</c> of a post-processor) and the type
/// name only breaks ties inside one kind. Registration order only breaks ties between behaviors that declare the
/// same stage and order. The position of a step comes from, in this precedence, the <see cref="PipelineStageRegistry"/>,
/// the <see cref="PipelineStageAttribute"/>, and the default of its kind (behaviors <c>Application</c>/0;
/// pre, post and error processors <c>Application</c>/-100; post-commit processors <c>Transaction</c>/-100).
/// </para>
/// <para>
/// Steps with the same identity (same step type, kind and module) are collapsed to the one with the lowest registration index.
/// The permutation is cached per closed pipeline type and recomputed only when the sequence of step types differs, which
/// costs one pass over the candidates per request. The first computation of a pipeline runs exactly once even when
/// several requests build it concurrently.
/// </para>
/// </remarks>
internal sealed class PipelineOrderResolver
{
    private const int BehaviorRank = 4;

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
    /// Returns the execution order of the steps, outermost first.
    /// </summary>
    /// <param name="pipelineKey">The closed pipeline type the steps belong to.</param>
    /// <param name="steps">
    /// The resolved behaviors and processor steps, in registration order. A cache hit reads them in place and allocates nothing.
    /// </param>
    /// <returns>
    /// Indexes into <paramref name="steps"/>, outermost first; collapsed duplicates are omitted.
    /// The array is shared with the cache and must not be modified.
    /// </returns>
    public int[] Resolve(Type pipelineKey, IReadOnlyList<object> steps)
    {
        ArgumentNullException.ThrowIfNull(pipelineKey);
        ArgumentNullException.ThrowIfNull(steps);

        var slot = _slots.GetOrAdd(pipelineKey, static _ => new Slot());
        var current = slot.Current;
        return current is not null && current.Matches(steps)
            ? current.Order
            : Refresh(slot, steps);
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

    private int[] Refresh(Slot slot, IReadOnlyList<object> steps)
    {
        lock (slot)
        {
            var current = slot.Current;
            if (current is not null && current.Matches(steps))
            {
                return current.Order;
            }

            // A cached pipeline would not see a later override: from the first computation on the registry is closed
            _registry.Seal();
            var candidates = Describe(steps);
            var created = new CachedOrder(Signature(candidates), Compute(candidates));
            slot.Current = created;
            Interlocked.Increment(ref _computations);
            return created.Order;
        }
    }

    private static PipelineStepCandidate[] Describe(IReadOnlyList<object> steps)
    {
        var candidates = new PipelineStepCandidate[steps.Count];
        for (var i = 0; i < candidates.Length; i++)
        {
            candidates[i] = PipelineStepCandidate.For(steps[i], i);
        }

        return candidates;
    }

    private int[] Compute(PipelineStepCandidate[] candidates)
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

    private static int[] CollapseDuplicates(PipelineStepCandidate[] candidates)
    {
        var seen = new HashSet<(Type, PipelineStepKind, Type?)>();
        var kept = new List<int>(candidates.Length);
        for (var i = 0; i < candidates.Length; i++)
        {
            if (seen.Add((candidates[i].StepType, candidates[i].Kind, candidates[i].ModuleType)))
            {
                kept.Add(i);
            }
        }

        return [.. kept];
    }

    private SortKey BuildKey(in PipelineStepCandidate candidate)
    {
        var position = ResolvePosition(candidate);
        var rank = RankOf(candidate.Kind);
        return new SortKey(
            position.Stage,
            position.Order,
            rank,
            rank < BehaviorRank ? candidate.StepType.FullName ?? candidate.StepType.Name : null,
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

    // Rank at equal (stage, order): the kind decides before the type name, so the nesting is fixed:
    // pre (outermost), error, post, post-commit, then behaviors. An error processor therefore also observes the
    // Left of a post-processor, and a pre-processor aborts before either runs.
    // crap-exempt: single-question switch — maps a step kind to its rank at equal position
    private static int RankOf(PipelineStepKind kind) => kind switch
    {
        PipelineStepKind.PreProcessor => 0,
        PipelineStepKind.ErrorProcessor => 1,
        PipelineStepKind.PostProcessor => 2,
        PipelineStepKind.PostCommitProcessor => 3,
        _ => BehaviorRank
    };

    private static (Type, PipelineStepKind, Type?)[] Signature(PipelineStepCandidate[] candidates)
    {
        var signature = new (Type, PipelineStepKind, Type?)[candidates.Length];
        for (var i = 0; i < signature.Length; i++)
        {
            signature[i] = (candidates[i].StepType, candidates[i].Kind, candidates[i].ModuleType);
        }

        return signature;
    }

    private readonly record struct SortKey(PipelineStage Stage, int Order, int Rank, string? Name, int Index);

    private sealed class Slot
    {
        public volatile CachedOrder? Current;
    }

    private sealed class CachedOrder((Type, PipelineStepKind, Type?)[] signature, int[] order)
    {
        public int[] Order { get; } = order;

        public bool Matches(IReadOnlyList<object> steps)
        {
            if (steps.Count != signature.Length)
            {
                return false;
            }

            for (var i = 0; i < signature.Length; i++)
            {
                if (!IsSame(signature[i], PipelineStepCandidate.For(steps[i], i)))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsSame((Type StepType, PipelineStepKind Kind, Type? ModuleType) expected, in PipelineStepCandidate actual)
            => expected.StepType == actual.StepType && expected.Kind == actual.Kind && expected.ModuleType == actual.ModuleType;
    }
}

using System.Collections.Concurrent;

namespace Encina;

/// <summary>
/// Holds the registration-time <c>(stage, order)</c> overrides of pipeline steps.
/// </summary>
/// <remarks>
/// An override takes precedence over the <see cref="PipelineStageAttribute"/> of the type. Generic types are keyed by
/// their open generic definition, so one override covers every closed pipeline of the type. Overrides are expected to be
/// recorded while the host builds its service collection. The order resolver seals the registry when it computes its first
/// pipeline, and a later <see cref="Set"/> throws, because a cached pipeline would not see the override.
/// </remarks>
internal sealed class PipelineStageRegistry
{
    private readonly ConcurrentDictionary<Type, (PipelineStage Stage, int Order)> _overrides = new();
    private readonly List<PipelineStageConflict> _conflicts = [];
    private readonly object _gate = new();
    private bool _sealed;

    /// <summary>
    /// Gets the declarations that placed one type at two different positions, in recording order (rule R6 of the
    /// pipeline order validation). The later declaration wins in the registry; the conflict is what makes startup fail.
    /// </summary>
    public IReadOnlyList<PipelineStageConflict> Conflicts
    {
        get
        {
            lock (_gate)
            {
                return [.. _conflicts];
            }
        }
    }

    /// <summary>
    /// Records (or replaces) the override of a step type.
    /// </summary>
    /// <param name="stepType">Behavior or processor type; a closed generic type is keyed by its definition.</param>
    /// <param name="stage">Stage in which the type runs.</param>
    /// <param name="order">Order inside the stage; lower is further out.</param>
    /// <exception cref="InvalidOperationException">A pipeline was already resolved, so the override would be ignored by it.</exception>
    public void Set(Type stepType, PipelineStage stage, int order)
    {
        ArgumentNullException.ThrowIfNull(stepType);
        lock (_gate)
        {
            if (_sealed)
            {
                throw new InvalidOperationException(
                    $"The pipeline stage override for '{stepType.Name}' was recorded after the first pipeline was built. " +
                    "Record every stage override while configuring services, before the first request runs.");
            }

            var key = Normalize(stepType);
            if (_overrides.TryGetValue(key, out var previous) && previous != (stage, order))
            {
                _conflicts.Add(new PipelineStageConflict(key, previous.Stage, previous.Order, stage, order));
            }

            _overrides[key] = (stage, order);
        }
    }

    /// <summary>
    /// Rejects every later <see cref="Set"/>. Called by the order resolver when it computes its first pipeline,
    /// because a cached pipeline would not see a later override.
    /// </summary>
    public void Seal()
    {
        lock (_gate)
        {
            _sealed = true;
        }
    }

    /// <summary>
    /// Looks up the override of a step type.
    /// </summary>
    /// <param name="stepType">Behavior or processor type; a closed generic type is keyed by its definition.</param>
    /// <param name="position">The override when found.</param>
    /// <returns><see langword="true"/> when an override was recorded for the type.</returns>
    public bool TryGet(Type stepType, out (PipelineStage Stage, int Order) position)
    {
        ArgumentNullException.ThrowIfNull(stepType);
        return _overrides.TryGetValue(Normalize(stepType), out position);
    }

    private static Type Normalize(Type stepType)
        => stepType.IsGenericType ? stepType.GetGenericTypeDefinition() : stepType;
}

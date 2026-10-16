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
    private bool _sealed;

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
        if (Volatile.Read(ref _sealed))
        {
            throw new InvalidOperationException(
                $"The pipeline stage override for '{stepType.Name}' was recorded after the first pipeline was built. " +
                "Record every stage override while configuring services, before the first request runs.");
        }

        _overrides[Normalize(stepType)] = (stage, order);
    }

    /// <summary>
    /// Rejects every later <see cref="Set"/>. Called by the order resolver when it computes its first pipeline,
    /// because a cached pipeline would not see a later override.
    /// </summary>
    public void Seal() => Volatile.Write(ref _sealed, true);

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

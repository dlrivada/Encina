using System.Collections.Concurrent;

namespace Encina;

/// <summary>
/// Holds the registration-time <c>(stage, order)</c> overrides of pipeline steps.
/// </summary>
/// <remarks>
/// An override takes precedence over the <see cref="PipelineStageAttribute"/> of the type. Generic types are keyed by
/// their open generic definition, so one override covers every closed pipeline of the type. Overrides are expected to be
/// recorded while the host builds its service collection; the order resolver caches a resolved pipeline until its set of
/// steps changes.
/// </remarks>
internal sealed class PipelineStageRegistry
{
    private readonly ConcurrentDictionary<Type, (PipelineStage Stage, int Order)> _overrides = new();

    /// <summary>
    /// Records (or replaces) the override of a step type.
    /// </summary>
    /// <param name="stepType">Behavior or processor type; a closed generic type is keyed by its definition.</param>
    /// <param name="stage">Stage in which the type runs.</param>
    /// <param name="order">Order inside the stage; lower is further out.</param>
    public void Set(Type stepType, PipelineStage stage, int order)
    {
        ArgumentNullException.ThrowIfNull(stepType);
        _overrides[Normalize(stepType)] = (stage, order);
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

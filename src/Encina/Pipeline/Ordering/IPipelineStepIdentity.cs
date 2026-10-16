namespace Encina;

/// <summary>
/// Implemented by pipeline steps that wrap another type (a module behavior or a processor), so the order resolver
/// places and de-duplicates the wrapped type instead of the wrapper.
/// </summary>
internal interface IPipelineStepIdentity
{
    /// <summary>Gets the wrapped behavior or processor type that the step stands for.</summary>
    Type StepType { get; }

    /// <summary>Gets the kind of the step.</summary>
    PipelineStepKind Kind { get; }

    /// <summary>
    /// Gets the module the step is scoped to, or <see langword="null"/> when it is not module-scoped. Two steps of the
    /// same <see cref="StepType"/> for different modules behave differently, so they are not duplicates.
    /// </summary>
    Type? ModuleType { get; }

    /// <summary>Gets the stage declared by a <see cref="PipelineStageAttribute"/> on <see cref="StepType"/>, if any.</summary>
    PipelineStage? DeclaredStage { get; }

    /// <summary>Gets the order declared by a <see cref="PipelineStageAttribute"/> on <see cref="StepType"/>, if any.</summary>
    int? DeclaredOrder { get; }
}

/// <summary>
/// Reads the <see cref="PipelineStageAttribute"/> of a type.
/// </summary>
internal static class PipelineStageLookup
{
    /// <summary>Returns the attribute declared on <paramref name="type"/> itself, or <see langword="null"/>.</summary>
    public static PipelineStageAttribute? Find(Type type)
        => Attribute.GetCustomAttribute(type, typeof(PipelineStageAttribute), inherit: false) as PipelineStageAttribute;
}

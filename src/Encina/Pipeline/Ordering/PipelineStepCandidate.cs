namespace Encina;

/// <summary>
/// One resolved step instance offered to the <see cref="PipelineOrderResolver"/>.
/// </summary>
/// <param name="RuntimeType">Runtime type of the instance in the pipeline (a wrapper for adapted steps).</param>
/// <param name="Identity">Step identity when the instance wraps another type; <see langword="null"/> for a plain behavior.</param>
/// <param name="Kind">Kind of the step.</param>
/// <param name="RegistrationIndex">Position of the instance in the resolved registration sequence.</param>
internal readonly record struct PipelineStepCandidate(
    Type RuntimeType,
    IPipelineStepIdentity? Identity,
    PipelineStepKind Kind,
    int RegistrationIndex)
{
    /// <summary>Gets the type the step stands for: the wrapped type of an adapted step, otherwise the runtime type.</summary>
    public Type StepType => Identity?.StepType ?? RuntimeType;

    /// <summary>Gets the module the step is scoped to, or <see langword="null"/>.</summary>
    public Type? ModuleType => Identity?.ModuleType;

    /// <summary>
    /// Creates the candidate of a resolved behavior instance.
    /// </summary>
    /// <param name="behavior">Behavior or adapted step instance.</param>
    /// <param name="registrationIndex">Position of the instance in the resolved registration sequence.</param>
    public static PipelineStepCandidate For(object behavior, int registrationIndex)
    {
        var identity = behavior as IPipelineStepIdentity;
        return new PipelineStepCandidate(
            behavior.GetType(),
            identity,
            identity?.Kind ?? PipelineStepKind.Behavior,
            registrationIndex);
    }
}

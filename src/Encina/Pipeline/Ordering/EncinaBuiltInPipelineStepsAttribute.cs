namespace Encina;

/// <summary>
/// Marks an assembly as shipped from the Encina repository, so the pipeline order validation requires every
/// behavior and processor it declares to state its <see cref="PipelineStage"/>.
/// </summary>
/// <remarks>
/// <para>
/// The validation recognizes a built-in step by its assembly: the assembly is either in the fixed list of the
/// packages built from <c>src/</c>, or carries this attribute. The assembly name prefix <c>Encina.</c> is not used,
/// because it also matches test projects and applications.
/// </para>
/// <para>
/// A built-in step that declares no stage fails startup (rule R3), because an undeclared step would silently run in
/// the <see cref="PipelineStage.Application"/> stage, inside every gate.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [assembly: EncinaBuiltInPipelineSteps]
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
public sealed class EncinaBuiltInPipelineStepsAttribute : Attribute
{
}

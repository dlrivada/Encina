namespace Encina;

/// <summary>
/// Kind of a pipeline step.
/// </summary>
/// <example>
/// <code>
/// var processors = inspector.Describe&lt;MyCommand, Unit&gt;()
///     .Where(step =&gt; step.Kind == PipelineStepKind.PreProcessor);
/// </code>
/// </example>
public enum PipelineStepKind
{
    /// <summary>A global <c>IPipelineBehavior</c> (or stream behavior).</summary>
    Behavior,

    /// <summary>A module-scoped behavior adapted into the pipeline.</summary>
    ModuleBehavior,

    /// <summary>An <c>IRequestPreProcessor</c> adapted into a step.</summary>
    PreProcessor,

    /// <summary>An <c>IRequestPostProcessor</c> adapted into a step.</summary>
    PostProcessor,

    /// <summary>An <c>IRequestErrorProcessor</c> adapted into a step.</summary>
    ErrorProcessor,

    /// <summary>An <c>IRequestPostCommitProcessor</c> adapted into a step.</summary>
    PostCommitProcessor
}

/// <summary>
/// Where the position of a pipeline step came from.
/// </summary>
/// <example>
/// <code>
/// var overridden = inspector.Describe&lt;MyCommand, Unit&gt;()
///     .Where(step =&gt; step.Source == PipelineStepSource.Registration);
/// </code>
/// </example>
public enum PipelineStepSource
{
    /// <summary>The position comes from a <see cref="PipelineStageAttribute"/> on the type.</summary>
    Attribute,

    /// <summary>The position was set at registration time and overrides the attribute.</summary>
    Registration,

    /// <summary>The type declares no stage; the default for its kind applies.</summary>
    Default
}

/// <summary>
/// Describes one step of a resolved pipeline: what it is and where, and why, it runs.
/// </summary>
/// <param name="StepType">Runtime type of the behavior or processor.</param>
/// <param name="Kind">Kind of step.</param>
/// <param name="Stage">Stage in which the step runs.</param>
/// <param name="Order">Order inside <paramref name="Stage"/>; lower is further out.</param>
/// <param name="Source">Where <paramref name="Stage"/> and <paramref name="Order"/> came from.</param>
/// <example>
/// <code>
/// foreach (var step in inspector.Describe&lt;PlaceOrder, OrderId&gt;())
/// {
///     Console.WriteLine($"{step.Stage} {step.Order} {step.StepType.Name} ({step.Source})");
/// }
/// </code>
/// </example>
public sealed record PipelineStepDescription(
    Type StepType,
    PipelineStepKind Kind,
    PipelineStage Stage,
    int Order,
    PipelineStepSource Source);

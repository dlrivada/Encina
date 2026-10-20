namespace Encina;

/// <summary>
/// Declares the <see cref="PipelineStage"/> (and optionally the order inside it) of a pipeline behavior
/// or request processor.
/// </summary>
/// <remarks>
/// <para>
/// Steps run in ascending (<see cref="Stage"/>, <see cref="Order"/>) order: lower values are further out.
/// <see cref="Order"/> is relative to the other steps of the same stage; negative values are allowed to place a
/// step before the default order of 0. The attribute is not inherited.
/// </para>
/// <para>
/// The pipeline builders read the attribute once per closed pipeline. A registration-time override, when present,
/// takes precedence over the attribute; a type without the attribute runs in <see cref="PipelineStage.Application"/>.
/// </para>
/// </remarks>
/// <param name="stage">Stage in which the annotated type runs.</param>
/// <example>
/// <code>
/// [PipelineStage(PipelineStage.Validation, Order = -10)]
/// public sealed class PayloadShapeBehavior&lt;TRequest, TResponse&gt; : IPipelineBehavior&lt;TRequest, TResponse&gt;
///     where TRequest : IRequest&lt;TResponse&gt;
/// {
///     public ValueTask&lt;Either&lt;EncinaError, TResponse&gt;&gt; Handle(
///         TRequest request,
///         IRequestContext context,
///         RequestHandlerCallback&lt;TResponse&gt; nextStep,
///         CancellationToken cancellationToken) => nextStep();
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class PipelineStageAttribute(PipelineStage stage) : Attribute
{
    /// <summary>
    /// Gets the stage in which the annotated type runs.
    /// </summary>
    public PipelineStage Stage { get; } = stage;

    /// <summary>
    /// Gets the order inside <see cref="Stage"/>. Lower is further out; negative values are allowed.
    /// </summary>
    public int Order { get; init; }
}

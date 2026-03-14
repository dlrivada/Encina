namespace Encina.Compliance.PrivacyByDesign;

/// <summary>
/// Declares that a request type is subject to data minimization enforcement
/// via the Privacy by Design pipeline behavior.
/// </summary>
/// <remarks>
/// <para>
/// This attribute is used by the <c>DataMinimizationPipelineBehavior</c> in the Encina
/// request pipeline. When a request decorated with this attribute is dispatched, the pipeline
/// analyzes its fields for minimization compliance before the handler executes.
/// </para>
/// <para>
/// Per GDPR Article 25(2), "the controller shall implement appropriate technical and
/// organisational measures for ensuring that, by default, only personal data which are
/// necessary for each specific purpose of the processing are processed. That obligation
/// applies to the amount of personal data collected, the extent of their processing, the
/// period of their storage and their accessibility."
/// </para>
/// <para>
/// Individual fields within the request are annotated with <see cref="NotStrictlyNecessaryAttribute"/>
/// to indicate fields that are not required for the processing operation. The pipeline behavior
/// produces a <see cref="Model.MinimizationReport"/> and optionally blocks the request if the
/// minimization score exceeds the configured threshold.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [EnforceDataMinimization(Purpose = "Order Processing")]
/// public sealed record CreateOrderCommand(
///     string ProductId,
///     int Quantity,
///     [property: NotStrictlyNecessary(Reason = "Analytics only")]
///     string? ReferralSource) : ICommand&lt;OrderId&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class EnforceDataMinimizationAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the declared processing purpose for this request type.
    /// </summary>
    /// <remarks>
    /// When specified, purpose limitation validation is also performed against the
    /// <see cref="Model.PurposeDefinition"/> registered with this purpose identifier.
    /// When <see langword="null"/>, only data minimization checks are applied.
    /// </remarks>
    public string? Purpose { get; set; }
}

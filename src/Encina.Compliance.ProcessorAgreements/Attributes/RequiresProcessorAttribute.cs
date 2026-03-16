namespace Encina.Compliance.ProcessorAgreements;

/// <summary>
/// Declares that a request type requires a valid Data Processing Agreement
/// with a specific processor before it can be executed.
/// </summary>
/// <remarks>
/// <para>
/// This attribute is used by the <c>ProcessorValidationPipelineBehavior</c> in the Encina
/// request pipeline. When a request decorated with this attribute is dispatched, the pipeline
/// verifies that the referenced processor has a current, active DPA before the handler executes.
/// </para>
/// <para>
/// Per GDPR Article 28(3), "processing by a processor shall be governed by a contract or other
/// legal act [...] that is binding on the processor." This attribute provides declarative
/// enforcement of the DPA requirement at the request type level.
/// </para>
/// <para>
/// The behavior depends on the configured
/// <see cref="Model.ProcessorAgreementEnforcementMode"/>:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="Model.ProcessorAgreementEnforcementMode.Block"/>:
/// Blocks execution if no valid DPA exists for the processor.</description></item>
/// <item><description><see cref="Model.ProcessorAgreementEnforcementMode.Warn"/>:
/// Logs a warning but allows execution to proceed.</description></item>
/// <item><description><see cref="Model.ProcessorAgreementEnforcementMode.Disabled"/>:
/// No enforcement (attribute is ignored).</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Basic usage — ProcessorId identifies which processor's DPA to validate
/// [RequiresProcessor(ProcessorId = "stripe-payments")]
/// public sealed record ProcessPaymentCommand(decimal Amount) : ICommand&lt;PaymentResult&gt;;
///
/// // Sub-processor scenario — validates DPA for a specific sub-processor
/// [RequiresProcessor(ProcessorId = "aws-s3-storage")]
/// public sealed record StoreDocumentCommand(byte[] Content) : ICommand&lt;DocumentId&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class RequiresProcessorAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the identifier of the processor that must have a valid DPA
    /// for the decorated request type to proceed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This value must match a <see cref="Model.Processor.Id"/> registered via
    /// <see cref="Abstractions.IProcessorService"/>. The <c>ProcessorValidationPipelineBehavior</c>
    /// uses this identifier to look up the processor and verify its DPA status via
    /// <see cref="Abstractions.IDPAService.HasValidDPAAsync"/>.
    /// </para>
    /// <para>
    /// When the processor is not found in the registry, the pipeline returns a
    /// <c>processor.not_found</c> error. When found but without a valid DPA,
    /// the error depends on the DPA status (e.g., <c>processor.dpa_missing</c>,
    /// <c>processor.dpa_expired</c>).
    /// </para>
    /// </remarks>
    public required string ProcessorId { get; set; }
}

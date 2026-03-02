namespace Encina.Compliance.DataResidency.Attributes;

/// <summary>
/// Declares that data processed by this request must remain in the current processing region
/// and must not be transferred across borders.
/// </summary>
/// <remarks>
/// <para>
/// When applied to a request type, the <see cref="DataResidencyPipelineBehavior{TRequest, TResponse}"/>
/// records the no-cross-border constraint in the residency audit trail and ensures that data
/// processed by this request stays within the current deployment region.
/// </para>
/// <para>
/// This attribute is useful for highly sensitive data categories (e.g., national security,
/// health records in certain jurisdictions) where ANY cross-border transfer is prohibited
/// regardless of adequacy decisions, SCCs, or other transfer mechanisms.
/// </para>
/// <para>
/// Per GDPR Article 49(1), derogations from the general cross-border transfer rules
/// may apply in specific situations. This attribute enforces a blanket prohibition,
/// providing the strictest level of data sovereignty enforcement.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Strict no-transfer policy for classified data
/// [NoCrossBorderTransfer(
///     DataCategory = "classified-records",
///     Reason = "National security regulation prohibits any cross-border transfer")]
/// public record ProcessClassifiedDocumentCommand(string DocumentId) : ICommand;
///
/// // Simple no-transfer constraint
/// [NoCrossBorderTransfer]
/// public record UpdateLocalHealthRecordCommand(string PatientId) : ICommand;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class NoCrossBorderTransferAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the data category for policy association.
    /// </summary>
    /// <remarks>
    /// If not set, the pipeline behavior derives the category from the request type name.
    /// </remarks>
    public string? DataCategory { get; set; }

    /// <summary>
    /// Gets or sets the reason for the no-cross-border restriction.
    /// </summary>
    /// <remarks>
    /// Documents the legal or business justification for prohibiting cross-border transfers.
    /// Per GDPR Article 5(2) (accountability), controllers should document the rationale
    /// behind data processing restrictions.
    /// </remarks>
    public string? Reason { get; set; }
}

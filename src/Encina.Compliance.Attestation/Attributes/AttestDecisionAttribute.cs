namespace Encina.Compliance.Attestation.Attributes;

/// <summary>
/// Marks a command or request type as requiring attestation of the decision outcome.
/// When the attestation pipeline behavior is registered, requests decorated with
/// this attribute will have their results attested via the configured
/// <see cref="Abstractions.IAuditAttestationProvider"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class AttestDecisionAttribute : Attribute
{
    /// <summary>
    /// Gets or sets a human-readable reason for why attestation is required.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the record type discriminator used in the <see cref="Model.AuditRecord"/>.
    /// Defaults to the request type name if not specified.
    /// </summary>
    public string? RecordType { get; set; }
}

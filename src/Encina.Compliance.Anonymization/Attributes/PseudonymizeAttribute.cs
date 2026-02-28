using Encina.Compliance.Anonymization.Model;

namespace Encina.Compliance.Anonymization;

/// <summary>
/// Marks a response property for automatic pseudonymization by the
/// <see cref="AnonymizationPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
/// <remarks>
/// <para>
/// When the pipeline behavior detects this attribute on a <c>TResponse</c> property,
/// it replaces the property value with a pseudonym using the specified
/// <see cref="PseudonymizationAlgorithm"/>. The handler works with real data;
/// pseudonymization occurs on the way out (response-side transformation per
/// GDPR Article 4(5) — definition of pseudonymisation).
/// </para>
/// <para>
/// Pseudonymization is <b>reversible</b> when using <see cref="PseudonymizationAlgorithm.Aes256Gcm"/>
/// (the default), meaning the original value can be recovered with the correct key.
/// <see cref="PseudonymizationAlgorithm.HmacSha256"/> produces a deterministic but
/// irreversible pseudonym (one-way keyed hash).
/// </para>
/// <para>
/// The <see cref="KeyId"/> property specifies which cryptographic key from the
/// <see cref="IKeyProvider"/> to use. This enables per-field key isolation and
/// supports key rotation scenarios (EDPB Guidelines 01/2025, §5.3).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public record PatientResponse
/// {
///     public Guid Id { get; init; }
///
///     [Pseudonymize(KeyId = "patient-names")]
///     public string FullName { get; init; } = string.Empty;
///
///     [Pseudonymize(KeyId = "patient-emails", Algorithm = PseudonymizationAlgorithm.HmacSha256)]
///     public string Email { get; init; } = string.Empty;
///
///     [Pseudonymize(KeyId = "patient-ssn")]
///     public string SocialSecurityNumber { get; init; } = string.Empty;
///
///     // Non-decorated properties pass through unmodified
///     public string DiagnosisCode { get; init; } = string.Empty;
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class PseudonymizeAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the identifier of the cryptographic key to use for pseudonymization.
    /// </summary>
    /// <remarks>
    /// <para>
    /// References a key registered in the <see cref="IKeyProvider"/>. Different fields
    /// can use different keys to achieve per-field key isolation, limiting the blast radius
    /// if a single key is compromised (EDPB Guidelines 01/2025, §5.3).
    /// </para>
    /// <para>
    /// This property is required. When <c>null</c> or empty, the pipeline behavior will
    /// use the active key from <see cref="IKeyProvider.GetActiveKeyIdAsync"/> as a fallback.
    /// </para>
    /// </remarks>
    public string? KeyId { get; set; }

    /// <summary>
    /// Gets or sets the pseudonymization algorithm to use.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="PseudonymizationAlgorithm.Aes256Gcm"/> (default): Authenticated encryption
    /// producing a random, reversible pseudonym. Same input yields different ciphertext each time.
    /// Supports depseudonymization with the correct key.
    /// </para>
    /// <para>
    /// <see cref="PseudonymizationAlgorithm.HmacSha256"/>: Deterministic keyed hash producing
    /// a consistent pseudonym for the same input and key. Irreversible — the original value
    /// cannot be recovered. Useful when pseudonyms must be comparable across records.
    /// </para>
    /// </remarks>
    public PseudonymizationAlgorithm Algorithm { get; set; } = PseudonymizationAlgorithm.Aes256Gcm;
}

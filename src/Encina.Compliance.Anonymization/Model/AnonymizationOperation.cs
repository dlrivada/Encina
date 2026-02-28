namespace Encina.Compliance.Anonymization.Model;

/// <summary>
/// Types of operations recorded in the anonymization audit trail.
/// </summary>
/// <remarks>
/// Each anonymization, pseudonymization, or tokenization action is auditable.
/// The operation type identifies what was performed, enabling compliance
/// reporting and forensic analysis of data protection measures applied
/// under GDPR Articles 25, 32, and 89.
/// </remarks>
public enum AnonymizationOperation
{
    /// <summary>
    /// Data was irreversibly anonymized using one or more anonymization techniques.
    /// </summary>
    /// <remarks>
    /// Once anonymized, the data falls outside GDPR scope (Recital 26).
    /// The original data cannot be recovered from the anonymized output.
    /// </remarks>
    Anonymized = 0,

    /// <summary>
    /// Data was pseudonymized using a cryptographic algorithm with a managed key.
    /// </summary>
    /// <remarks>
    /// Pseudonymized data remains personal data under GDPR Article 4(5).
    /// The original data can be recovered using the corresponding key via depseudonymization.
    /// </remarks>
    Pseudonymized = 1,

    /// <summary>
    /// Previously pseudonymized data was reversed to its original form.
    /// </summary>
    /// <remarks>
    /// Depseudonymization requires access to the key used during pseudonymization.
    /// This operation should be restricted to authorized personnel and audited.
    /// </remarks>
    Depseudonymized = 2,

    /// <summary>
    /// A sensitive value was replaced with a non-sensitive token.
    /// </summary>
    /// <remarks>
    /// The token-to-value mapping is stored in the <c>ITokenMappingStore</c>.
    /// Tokens have no mathematical relationship to the original value.
    /// </remarks>
    Tokenized = 3,

    /// <summary>
    /// A token was resolved back to its original sensitive value.
    /// </summary>
    /// <remarks>
    /// Detokenization requires access to the <c>ITokenMappingStore</c>.
    /// This operation should be restricted to authorized personnel and audited.
    /// </remarks>
    Detokenized = 4,

    /// <summary>
    /// A cryptographic key was rotated (new key generated, old key retired).
    /// </summary>
    /// <remarks>
    /// Key rotation is a security best practice recommended by EDPB Guidelines 01/2025
    /// Section 4.3. All data pseudonymized with the old key should be re-encrypted
    /// with the new key.
    /// </remarks>
    KeyRotated = 5,

    /// <summary>
    /// A re-identification risk assessment was performed on a dataset.
    /// </summary>
    /// <remarks>
    /// Risk assessments evaluate the effectiveness of anonymization measures
    /// by calculating metrics such as k-anonymity, l-diversity, and t-closeness.
    /// Required for demonstrating GDPR Article 89 safeguards.
    /// </remarks>
    RiskAssessed = 6
}

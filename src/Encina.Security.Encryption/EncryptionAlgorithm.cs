namespace Encina.Security.Encryption;

/// <summary>
/// Supported encryption algorithms for field-level encryption.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Aes256Gcm"/> is the recommended default for new applications. It provides
/// authenticated encryption with associated data (AEAD), ensuring both confidentiality
/// and integrity of encrypted values.
/// </para>
/// <para>
/// Additional algorithms may be added in future versions for specific compliance
/// requirements or hardware optimization scenarios.
/// </para>
/// </remarks>
public enum EncryptionAlgorithm
{
    /// <summary>
    /// AES-256 with Galois/Counter Mode (GCM).
    /// </summary>
    /// <remarks>
    /// Authenticated encryption providing confidentiality and integrity.
    /// Recommended for all new applications. NIST-approved (SP 800-38D).
    /// </remarks>
    Aes256Gcm = 0
}

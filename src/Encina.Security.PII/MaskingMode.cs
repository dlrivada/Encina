namespace Encina.Security.PII;

/// <summary>
/// Defines the masking strategy to apply when redacting PII values.
/// </summary>
/// <remarks>
/// <para>
/// The masking mode determines the level of data transformation applied:
/// <list type="bullet">
/// <item><see cref="Partial"/> preserves some characters for identification</item>
/// <item><see cref="Full"/> replaces the entire value with a mask character</item>
/// <item><see cref="Hash"/> produces a deterministic hash for correlation without revealing the value</item>
/// <item><see cref="Tokenize"/> replaces the value with a reversible token</item>
/// <item><see cref="Redact"/> removes the value entirely</item>
/// </list>
/// </para>
/// </remarks>
public enum MaskingMode
{
    /// <summary>
    /// Partially masks the value, preserving some characters for identification.
    /// </summary>
    /// <remarks>
    /// The number of visible characters depends on the <see cref="PIIType"/> and
    /// <see cref="MaskingOptions"/>. For example, an email might show <c>j***@example.com</c>
    /// and a credit card might show <c>****-****-****-1234</c>.
    /// </remarks>
    Partial = 0,

    /// <summary>
    /// Replaces the entire value with mask characters.
    /// </summary>
    /// <remarks>
    /// The value is completely replaced (e.g., <c>***</c>). No part of the original
    /// value is preserved. Use when even partial data could be a compliance risk.
    /// </remarks>
    Full,

    /// <summary>
    /// Produces a deterministic SHA-256 hash of the value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Useful for correlation analysis without exposing the original value.
    /// The same input always produces the same hash, enabling joins and grouping
    /// in analytics without PII exposure.
    /// </para>
    /// <para>
    /// <b>Note:</b> While hashing is irreversible, short or low-entropy values
    /// (e.g., phone numbers) may be vulnerable to brute-force reversal.
    /// Consider adding a salt via <see cref="MaskingOptions"/> for added protection.
    /// </para>
    /// </remarks>
    Hash,

    /// <summary>
    /// Replaces the value with a reversible token.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Tokenization substitutes the original value with a non-sensitive placeholder
    /// while maintaining a secure mapping for authorized de-tokenization.
    /// </para>
    /// <para>
    /// This mode requires an <c>ITokenVault</c> implementation to store and retrieve
    /// the original-to-token mappings securely.
    /// </para>
    /// </remarks>
    Tokenize,

    /// <summary>
    /// Removes the value entirely, replacing it with <c>[REDACTED]</c>.
    /// </summary>
    /// <remarks>
    /// The most aggressive masking mode. The original value is completely removed
    /// and replaced with a fixed placeholder. Use for highly sensitive data where
    /// no trace of the original value should remain.
    /// </remarks>
    Redact
}

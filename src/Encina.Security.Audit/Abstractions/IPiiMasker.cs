namespace Encina.Security.Audit;

/// <summary>
/// Abstraction for masking personally identifiable information (PII) before audit logging.
/// </summary>
/// <remarks>
/// <para>
/// This interface serves as an extension point for integrating PII masking functionality
/// into the audit trail system. Implementations can sanitize sensitive data before
/// it is hashed or logged.
/// </para>
/// <para>
/// The default implementation (<see cref="NullPiiMasker"/>) performs no masking.
/// When the <c>Encina.Security.PII</c> package is available, register a custom
/// implementation that performs actual PII detection and masking.
/// </para>
/// <para>
/// <b>Common PII to mask:</b>
/// <list type="bullet">
/// <item>Social Security Numbers</item>
/// <item>Credit card numbers</item>
/// <item>Email addresses</item>
/// <item>Phone numbers</item>
/// <item>Physical addresses</item>
/// <item>Names (depending on compliance requirements)</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Custom PII masker implementation
/// public class RegexPiiMasker : IPiiMasker
/// {
///     public T MaskForAudit&lt;T&gt;(T request) where T : notnull
///     {
///         // Clone and mask PII fields
///         // Return sanitized copy
///     }
/// }
///
/// // Registration
/// services.AddSingleton&lt;IPiiMasker, RegexPiiMasker&gt;();
/// </code>
/// </example>
public interface IPiiMasker
{
    /// <summary>
    /// Masks PII in a request object for safe audit logging.
    /// </summary>
    /// <typeparam name="T">The type of the request object.</typeparam>
    /// <param name="request">The request object potentially containing PII.</param>
    /// <returns>
    /// A sanitized copy of the request with PII masked, or the original object if no masking is needed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The returned object is used for:
    /// <list type="bullet">
    /// <item>Computing the <see cref="AuditEntry.RequestPayloadHash"/></item>
    /// <item>Any optional payload logging (if enabled)</item>
    /// </list>
    /// </para>
    /// <para>
    /// Implementations should:
    /// <list type="bullet">
    /// <item>Return a copy to avoid modifying the original request</item>
    /// <item>Use consistent masking patterns (e.g., "***" or "[REDACTED]")</item>
    /// <item>Handle nested objects and collections</item>
    /// <item>Be performant as it runs on every audited request</item>
    /// </list>
    /// </para>
    /// </remarks>
    T MaskForAudit<T>(T request) where T : notnull;

    /// <summary>
    /// Masks PII in a request object for safe audit logging.
    /// </summary>
    /// <param name="request">The request object potentially containing PII.</param>
    /// <returns>
    /// A sanitized copy of the request with PII masked, or the original object if no masking is needed.
    /// </returns>
    /// <remarks>
    /// Non-generic overload for scenarios where the type is not known at compile time.
    /// </remarks>
    object MaskForAudit(object request);
}

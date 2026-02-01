namespace Encina.Security.Audit;

/// <summary>
/// No-op implementation of <see cref="IPiiMasker"/> that performs no masking.
/// </summary>
/// <remarks>
/// <para>
/// This is the default implementation when no custom PII masker is registered.
/// It simply returns the input unchanged, providing pass-through behavior.
/// </para>
/// <para>
/// <b>Important</b>: This implementation does NOT protect PII. For production systems
/// handling sensitive data, register a custom <see cref="IPiiMasker"/> implementation
/// that performs actual PII detection and masking.
/// </para>
/// <para>
/// Consider using this implementation only when:
/// <list type="bullet">
/// <item>Requests do not contain PII</item>
/// <item>Payload hashing is disabled via <c>AuditOptions.IncludePayloadHash = false</c></item>
/// <item>PII is handled at a different layer (e.g., API gateway)</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Default registration (happens automatically via AddEncinaAudit)
/// services.TryAddSingleton&lt;IPiiMasker, NullPiiMasker&gt;();
///
/// // Override with custom implementation
/// services.AddSingleton&lt;IPiiMasker, MyCustomPiiMasker&gt;();
/// </code>
/// </example>
public sealed class NullPiiMasker : IPiiMasker
{
    /// <inheritdoc/>
    /// <remarks>
    /// Returns the input unchanged without any masking.
    /// </remarks>
    public T MaskForAudit<T>(T request) where T : notnull
    {
        return request;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Returns the input unchanged without any masking.
    /// </remarks>
    public object MaskForAudit(object request)
    {
        return request;
    }
}

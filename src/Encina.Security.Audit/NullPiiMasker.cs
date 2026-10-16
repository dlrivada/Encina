namespace Encina.Security.Audit;

/// <summary>
/// No-op implementation of <see cref="IPiiMasker"/> that performs no masking.
/// </summary>
/// <remarks>
/// <para>
/// This is the explicit opt-out of PII masking: <c>AddEncinaAudit</c> registers it only when
/// <see cref="OperationAuditOptions.AllowUnmaskedPayloads"/> is <c>true</c>, and the host then logs a warning at
/// every start. It simply returns the input unchanged, providing pass-through behavior.
/// </para>
/// <para>
/// <b>Important</b>: This implementation does NOT protect PII. The audit module refuses to start with it
/// (or with no masker) unless the opt-out is set. For production systems
/// handling sensitive data, register a real <see cref="IPiiMasker"/> implementation
/// that performs actual PII detection and masking.
/// </para>
/// <para>
/// Consider using this implementation only when:
/// <list type="bullet">
/// <item>Requests do not contain PII</item>
/// <item>Payload hashing is disabled via <c>OperationAuditOptions.IncludePayloadHash = false</c></item>
/// <item>PII is handled at a different layer (e.g., API gateway)</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registered by AddEncinaAudit only with the explicit opt-out
/// services.AddEncinaAudit(options => options.AllowUnmaskedPayloads = true);
///
/// // Override with a real implementation
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

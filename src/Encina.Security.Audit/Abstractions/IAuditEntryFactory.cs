namespace Encina.Security.Audit;

/// <summary>
/// Factory abstraction for creating <see cref="AuditEntry"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// The factory is responsible for:
/// <list type="bullet">
/// <item>Extracting entity type and action from request type names or attributes</item>
/// <item>Extracting entity IDs from request properties</item>
/// <item>Computing SHA-256 payload hashes for tamper detection</item>
/// <item>Populating context information (user, tenant, correlation ID, timestamps)</item>
/// <item>Extracting HTTP context (IP address, User-Agent) from request metadata</item>
/// </list>
/// </para>
/// <para>
/// Custom implementations can override metadata extraction logic or add
/// domain-specific fields to the <see cref="AuditEntry.Metadata"/> dictionary.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class CustomAuditEntryFactory : IAuditEntryFactory
/// {
///     public AuditEntry Create&lt;TRequest&gt;(
///         TRequest request,
///         IRequestContext context,
///         AuditOutcome outcome,
///         string? errorMessage)
///     {
///         // Custom implementation with domain-specific logic
///     }
/// }
/// </code>
/// </example>
public interface IAuditEntryFactory
{
    /// <summary>
    /// Creates an <see cref="AuditEntry"/> from a request and its execution context.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request being audited.</typeparam>
    /// <param name="request">The request instance.</param>
    /// <param name="context">The request context containing correlation, user, and tenant information.</param>
    /// <param name="outcome">The outcome of the operation.</param>
    /// <param name="errorMessage">The error message if <paramref name="outcome"/> is not <see cref="AuditOutcome.Success"/>.</param>
    /// <returns>A fully populated <see cref="AuditEntry"/>.</returns>
    /// <remarks>
    /// <para>
    /// The factory should:
    /// <list type="number">
    /// <item>Check for <c>[Auditable]</c> attribute overrides on the request type</item>
    /// <item>Extract entity type and action from type name conventions (e.g., <c>CreateOrderCommand</c>)</item>
    /// <item>Extract entity ID from common properties (<c>Id</c>, <c>EntityId</c>, <c>[Entity]Id</c>)</item>
    /// <item>Apply PII masking before computing payload hash (via <see cref="IPiiMasker"/>)</item>
    /// <item>Extract IP address and User-Agent from context metadata</item>
    /// </list>
    /// </para>
    /// </remarks>
    AuditEntry Create<TRequest>(
        TRequest request,
        IRequestContext context,
        AuditOutcome outcome,
        string? errorMessage);

    /// <summary>
    /// Creates an <see cref="AuditEntry"/> from a request with full timing and response capture.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request being audited.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="request">The request instance.</param>
    /// <param name="response">The response instance, or <c>default</c> if the operation failed.</param>
    /// <param name="context">The request context containing correlation, user, and tenant information.</param>
    /// <param name="outcome">The outcome of the operation.</param>
    /// <param name="errorMessage">The error message if <paramref name="outcome"/> is not <see cref="AuditOutcome.Success"/>.</param>
    /// <param name="startedAtUtc">When the operation started.</param>
    /// <param name="completedAtUtc">When the operation completed.</param>
    /// <returns>A fully populated <see cref="AuditEntry"/> with timing and payload information.</returns>
    /// <remarks>
    /// <para>
    /// This overload captures additional information:
    /// <list type="bullet">
    /// <item>Start and completion timestamps for duration tracking</item>
    /// <item>Request and response payloads (when enabled in <see cref="AuditOptions"/>)</item>
    /// <item>Sensitive field redaction based on <see cref="AuditableAttribute.SensitiveFields"/></item>
    /// </list>
    /// </para>
    /// </remarks>
    AuditEntry Create<TRequest, TResponse>(
        TRequest request,
        TResponse? response,
        IRequestContext context,
        AuditOutcome outcome,
        string? errorMessage,
        DateTimeOffset startedAtUtc,
        DateTimeOffset completedAtUtc);
}

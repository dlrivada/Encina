namespace Encina;

/// <summary>
/// Carries ambient context metadata through the mediator pipeline.
/// </summary>
/// <remarks>
/// <para>
/// Context is created once per request and flows through all behaviors, processors, and handlers.
/// It provides access to cross-cutting concerns like correlation IDs, user identity, idempotency keys,
/// and tenant information without polluting request types.
/// </para>
/// <para>
/// Immutable design: use <c>With*</c> methods to create modified copies.
/// </para>
/// <para><b>Common Use Cases:</b></para>
/// <list type="bullet">
/// <item>Distributed tracing with correlation IDs</item>
/// <item>Multi-tenant applications (tenant isolation)</item>
/// <item>Idempotency checking (duplicate request detection)</item>
/// <item>User context propagation (audit logs, authorization)</item>
/// <item>Custom metadata for extensibility</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Creating context
/// var context = RequestContext.Create()
///     .WithUserId("user-123")
///     .WithTenantId("tenant-abc")
///     .WithIdempotencyKey("idempotency-xyz");
///
/// // Accessing in behaviors
/// public async ValueTask&lt;Either&lt;MediatorError, TResponse&gt;&gt; Handle(
///     TRequest request,
///     IRequestContext context,
///     RequestHandlerCallback&lt;TResponse&gt; nextStep,
///     CancellationToken cancellationToken)
/// {
///     _logger.LogInformation(
///         "Handling {Request} for user {UserId} (correlation: {CorrelationId})",
///         typeof(TRequest).Name,
///         context.UserId,
///         context.CorrelationId);
///
///     return await nextStep();
/// }
/// </code>
/// </example>
public interface IRequestContext
{
    /// <summary>
    /// Correlation ID for distributed tracing.
    /// </summary>
    /// <remarks>
    /// Always present - auto-generated from <see cref="System.Diagnostics.Activity.Current"/> or <see cref="System.Guid"/> if not provided.
    /// Use this for linking logs, traces, and metrics across services.
    /// </remarks>
    string CorrelationId { get; }

    /// <summary>
    /// User ID initiating the request.
    /// </summary>
    /// <remarks>
    /// <c>null</c> if the request is unauthenticated.
    /// Typically extracted from claims principal in ASP.NET Core applications.
    /// </remarks>
    string? UserId { get; }

    /// <summary>
    /// Idempotency key for duplicate detection.
    /// </summary>
    /// <remarks>
    /// <c>null</c> if idempotency is not applicable for this request.
    /// Used by idempotency behaviors to prevent duplicate processing of the same logical request.
    /// Typically extracted from HTTP headers (e.g., <c>Idempotency-Key</c>).
    /// </remarks>
    string? IdempotencyKey { get; }

    /// <summary>
    /// Tenant ID for multi-tenant applications.
    /// </summary>
    /// <remarks>
    /// <c>null</c> if the application is not multi-tenant or tenant cannot be determined.
    /// Used for data isolation and tenant-specific logic.
    /// </remarks>
    string? TenantId { get; }

    /// <summary>
    /// Request timestamp (UTC).
    /// </summary>
    /// <remarks>
    /// Captured when the context is created, represents the start of request processing.
    /// Useful for time-based logic, audit trails, and latency measurements.
    /// </remarks>
    DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Custom metadata for extensibility.
    /// </summary>
    /// <remarks>
    /// Allows behaviors and processors to attach additional context that doesn't fit standard properties.
    /// Use <see cref="WithMetadata"/> to add entries.
    /// </remarks>
    IReadOnlyDictionary<string, object?> Metadata { get; }

    /// <summary>
    /// Creates a new context with additional metadata.
    /// </summary>
    /// <param name="key">Metadata key.</param>
    /// <param name="value">Metadata value.</param>
    /// <returns>New context instance with the metadata added.</returns>
    /// <remarks>
    /// Follows immutable pattern - original context is not modified.
    /// </remarks>
    IRequestContext WithMetadata(string key, object? value);

    /// <summary>
    /// Creates a new context with updated user ID.
    /// </summary>
    /// <param name="userId">User ID to set.</param>
    /// <returns>New context instance with the user ID updated.</returns>
    /// <remarks>
    /// Follows immutable pattern - original context is not modified.
    /// Useful in pre-processors that extract user identity.
    /// </remarks>
    IRequestContext WithUserId(string? userId);

    /// <summary>
    /// Creates a new context with updated idempotency key.
    /// </summary>
    /// <param name="idempotencyKey">Idempotency key to set.</param>
    /// <returns>New context instance with the idempotency key updated.</returns>
    /// <remarks>
    /// Follows immutable pattern - original context is not modified.
    /// Useful in pre-processors that extract idempotency headers.
    /// </remarks>
    IRequestContext WithIdempotencyKey(string? idempotencyKey);

    /// <summary>
    /// Creates a new context with updated tenant ID.
    /// </summary>
    /// <param name="tenantId">Tenant ID to set.</param>
    /// <returns>New context instance with the tenant ID updated.</returns>
    /// <remarks>
    /// Follows immutable pattern - original context is not modified.
    /// Useful in pre-processors that determine tenant from request data or claims.
    /// </remarks>
    IRequestContext WithTenantId(string? tenantId);
}

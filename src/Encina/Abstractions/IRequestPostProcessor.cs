using LanguageExt;

namespace Encina;

/// <summary>
/// Executes logic after the main handler for a given request.
/// </summary>
/// <typeparam name="TRequest">Observed request type.</typeparam>
/// <typeparam name="TResponse">Response type emitted by the handler.</typeparam>
/// <remarks>
/// Useful for emitting notifications, cleaning resources, or persisting additional results. Runs
/// even when the handler returned a functional error; the implementation decides how to react.
/// </remarks>
/// <example>
/// <code>
/// public sealed class AuditLogPostProcessor&lt;TRequest, TResponse&gt; : IRequestPostProcessor&lt;TRequest, TResponse&gt;
/// {
///     private readonly IAuditLogger _auditLogger;
///
///     public Task Process(
///         TRequest request,
///         IRequestContext context,
///         Either&lt;MediatorError, TResponse&gt; response,
///         CancellationToken cancellationToken)
///     {
///         await _auditLogger.LogAsync(new AuditEntry
///         {
///             UserId = context.UserId,
///             Action = typeof(TRequest).Name,
///             Timestamp = context.Timestamp,
///             Success = response.IsRight
///         });
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
public interface IRequestPostProcessor<in TRequest, TResponse>
{
    /// <summary>
    /// Executes the post-processing logic using the request and the final response.
    /// </summary>
    /// <param name="request">Original request.</param>
    /// <param name="context">Request context with correlation ID, user info, etc.</param>
    /// <param name="response">Response returned by the pipeline.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// Post-processors have read-only access to context.
    /// Use context for audit logging, metrics tagging, or conditional side effects.
    /// </remarks>
    Task Process(
        TRequest request,
        IRequestContext context,
        Either<MediatorError, TResponse> response,
        CancellationToken cancellationToken);
}

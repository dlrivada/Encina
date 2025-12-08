using System.Threading;
using System.Threading.Tasks;
using LanguageExt;

namespace SimpleMediator;

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
/// public sealed class PublishEmailOnSuccess : IRequestPostProcessor&lt;SendEmailCommand, Unit&gt;
/// {
///     public Task Process(SendEmailCommand request, Either&lt;MediatorError, Unit&gt; response, CancellationToken cancellationToken)
///     {
///         if (response.IsRight)
///         {
///             metrics.Increment("email.sent");
///         }
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
    /// <param name="response">Response returned by the pipeline.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task Process(TRequest request, Either<Error, TResponse> response, CancellationToken cancellationToken);
}

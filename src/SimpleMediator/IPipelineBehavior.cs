using System.Threading;
using System.Threading.Tasks;
using LanguageExt;

namespace SimpleMediator;

/// <summary>
/// Intercepts handler execution to apply cross-cutting logic.
/// </summary>
/// <typeparam name="TRequest">Request type traversing the pipeline.</typeparam>
/// <typeparam name="TResponse">Response type returned by the final handler.</typeparam>
/// <remarks>
/// Behaviors are chained in reverse registration order. Each one decides whether to invoke
/// <paramref name="next"/> or short-circuit the flow with its own response.
/// </remarks>
/// <example>
/// <code>
/// public sealed class LoggingBehavior&lt;TRequest, TResponse&gt; : IPipelineBehavior&lt;TRequest, TResponse&gt;
///     where TRequest : IRequest&lt;TResponse&gt;
/// {
///     public async Task&lt;Either&lt;MediatorError, TResponse&gt;&gt; Handle(
///         TRequest request,
///         CancellationToken cancellationToken,
///         RequestHandlerDelegate&lt;TResponse&gt; next)
///     {
///         logger.LogInformation("Handling {Request}", typeof(TRequest).Name);
///         var response = await next().ConfigureAwait(false);
///         logger.LogInformation("Handled {Request}", typeof(TRequest).Name);
///         return response;
///     }
/// }
/// </code>
/// </example>
public interface IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Executes the behavior logic around the next pipeline element.
    /// </summary>
    /// <param name="request">Request being processed.</param>
    /// <param name="cancellationToken">Token to cancel the flow.</param>
    /// <param name="next">Delegate to the next behavior or handler.</param>
    /// <returns>Final result or the modified response from the behavior.</returns>
    Task<Either<Error, TResponse>> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next);
}

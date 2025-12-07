using System.Threading;
using System.Threading.Tasks;
using LanguageExt;

namespace SimpleMediator;

/// <summary>
/// Interceptor que rodea la ejecución del handler, permitiendo lógica transversal.
/// </summary>
/// <typeparam name="TRequest">Tipo de solicitud que atraviesa el pipeline.</typeparam>
/// <typeparam name="TResponse">Tipo devuelto por el manejador final.</typeparam>
/// <remarks>
/// Los behaviors se encadenan en orden inverso al registro. Cada uno decide si invoca a
/// <paramref name="next"/> o corta el flujo devolviendo su propia respuesta.
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
    /// Ejecuta la lógica del behavior en torno al siguiente elemento del pipeline.
    /// </summary>
    /// <param name="request">Solicitud procesada.</param>
    /// <param name="cancellationToken">Token para cancelar el flujo.</param>
    /// <param name="next">Delegado al siguiente behavior o handler.</param>
    /// <returns>Resultado final o alterado por el behavior.</returns>
    Task<Either<Error, TResponse>> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next);
}

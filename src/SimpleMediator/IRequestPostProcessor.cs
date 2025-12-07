using System.Threading;
using System.Threading.Tasks;
using LanguageExt;

namespace SimpleMediator;

/// <summary>
/// Ejecuta lógica posterior al handler principal para una solicitud dada.
/// </summary>
/// <typeparam name="TRequest">Tipo de solicitud observada.</typeparam>
/// <typeparam name="TResponse">Tipo de respuesta emitida por el handler.</typeparam>
/// <remarks>
/// Útil para emitir notificaciones, limpiar recursos o persistir resultados adicionales. Se
/// ejecuta incluso cuando el handler devolvió un resultado funcional de error; la implementación
/// decide cómo actuar ante ese escenario.
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
    /// Ejecuta la lógica posterior utilizando el request y la respuesta final.
    /// </summary>
    /// <param name="request">Solicitud original.</param>
    /// <param name="response">Respuesta devuelta por el pipeline.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task Process(TRequest request, Either<Error, TResponse> response, CancellationToken cancellationToken);
}

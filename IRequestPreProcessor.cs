using System.Threading;
using System.Threading.Tasks;

namespace SimpleMediator;

/// <summary>
/// Ejecuta lógica previa al pipeline principal para una solicitud.
/// </summary>
/// <typeparam name="TRequest">Tipo de solicitud procesada.</typeparam>
/// <remarks>
/// Se ejecuta antes de cualquier behavior. Ideal para normalizar datos, enriquecer contexto o
/// aplicar políticas de auditoría ligeras.
/// </remarks>
/// <example>
/// <code>
/// public sealed class EnsureCorrelationId&lt;TRequest&gt; : IRequestPreProcessor&lt;TRequest&gt;
/// {
///     public Task Process(TRequest request, CancellationToken cancellationToken)
///     {
///         CorrelationContext.Ensure();
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
public interface IRequestPreProcessor<in TRequest>
{
    /// <summary>
    /// Ejecuta la lógica previa usando el request recibido.
    /// </summary>
    /// <param name="request">Solicitud original.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task Process(TRequest request, CancellationToken cancellationToken);
}

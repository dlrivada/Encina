using System.Threading;
using System.Threading.Tasks;

namespace SimpleMediator;

/// <summary>
/// Ejecuta la lógica asociada a una solicitud concreta.
/// </summary>
/// <typeparam name="TRequest">Tipo de solicitud atendida.</typeparam>
/// <typeparam name="TResponse">Tipo producido al finalizar.</typeparam>
/// <remarks>
/// Los handlers deben ser livianos y delegar la orquestación a servicios especializados. El
/// mediator gestiona su ciclo de vida según el ámbito configurado en el contenedor.
/// </remarks>
/// <example>
/// <code>
/// public sealed class RefundPaymentHandler : IRequestHandler&lt;RefundPayment, Unit&gt;
/// {
///     public async Task&lt;Unit&gt; Handle(RefundPayment request, CancellationToken cancellationToken)
///     {
///         await paymentGateway.RefundAsync(request.PaymentId, cancellationToken);
///         await auditTrail.RecordAsync(request.PaymentId, cancellationToken);
///         return Unit.Default;
///     }
/// }
/// </code>
/// </example>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Procesa la solicitud recibida y devuelve el resultado correspondiente.
    /// </summary>
    /// <param name="request">Solicitud a resolver.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Resultado final según el contrato del request.</returns>
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

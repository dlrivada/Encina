namespace SimpleMediator;

/// <summary>
/// Executes the logic associated with a specific request.
/// </summary>
/// <typeparam name="TRequest">Handled request type.</typeparam>
/// <typeparam name="TResponse">Response type returned on completion.</typeparam>
/// <remarks>
/// Handlers should stay lightweight and delegate orchestration to specialized services. The
/// mediator manages their lifetime according to the container configuration.
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
    /// Processes the incoming request and returns the corresponding result.
    /// </summary>
    /// <param name="request">Request to handle.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Final result as defined by the request contract.</returns>
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

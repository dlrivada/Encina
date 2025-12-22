using LanguageExt;

namespace Encina;

/// <summary>
/// Executes the logic associated with a specific request using Railway Oriented Programming.
/// </summary>
/// <typeparam name="TRequest">Handled request type.</typeparam>
/// <typeparam name="TResponse">Response type returned on completion.</typeparam>
/// <remarks>
/// <para>
/// Handlers should stay lightweight and delegate orchestration to specialized services. The
/// mediator manages their lifetime according to the container configuration.
/// </para>
/// <para>
/// Handlers return <see cref="Either{L,R}"/> to enable explicit error handling without exceptions.
/// Return <c>Right(value)</c> for success or <c>Left(error)</c> for functional failures.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class RefundPaymentHandler : IRequestHandler&lt;RefundPayment, Unit&gt;
/// {
///     public async Task&lt;Either&lt;MediatorError, Unit&gt;&gt; Handle(RefundPayment request, CancellationToken cancellationToken)
///     {
///         var payment = await _paymentGateway.FindAsync(request.PaymentId, cancellationToken);
///         if (payment is null)
///             return Left(MediatorErrors.NotFound("Payment not found"));
///
///         if (!payment.CanRefund)
///             return Left(MediatorErrors.ValidationFailed("Payment cannot be refunded"));
///
///         await _paymentGateway.RefundAsync(request.PaymentId, cancellationToken);
///         await _auditTrail.RecordAsync(request.PaymentId, cancellationToken);
///
///         return Right(Unit.Default);
///     }
/// }
/// </code>
/// </example>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Processes the incoming request and returns either an error or the expected response.
    /// </summary>
    /// <param name="request">Request to handle.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Either a <see cref="MediatorError"/> (Left) representing a functional failure,
    /// or the expected response (Right) on success.
    /// </returns>
    /// <remarks>
    /// Use <c>static LanguageExt.Prelude</c> to access <c>Left</c> and <c>Right</c> factory methods.
    /// The mediator pipeline will short-circuit on the first Left value encountered.
    /// </remarks>
    Task<Either<MediatorError, TResponse>> Handle(TRequest request, CancellationToken cancellationToken);
}

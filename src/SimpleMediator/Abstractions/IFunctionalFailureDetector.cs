namespace SimpleMediator;

/// <summary>
/// Allows the application to identify functional failures in mediator responses.
/// </summary>
/// <remarks>
/// SimpleMediator relies on this abstraction to avoid binding to specific domain types.
/// Implementations can inspect <c>Either</c>, discriminated unions, or custom objects to extract
/// codes and messages.
/// </remarks>
/// <example>
/// <code>
/// public sealed class PaymentOutcomeFailureDetector : IFunctionalFailureDetector
/// {
///     public bool TryExtractFailure(object? response, out string reason, out object? capturedFailure)
///     {
///         if (response is PaymentOutcome outcome &amp;&amp; outcome.TryGetError(out var paymentError))
///         {
///             reason = paymentError.Code ?? "payment.failure";
///             capturedFailure = paymentError;
///             return true;
///         }
///
///         reason = string.Empty;
///         capturedFailure = null;
///         return false;
///     }
///
///     public string? TryGetErrorCode(object? capturedFailure)
///         => (capturedFailure as PaymentError)?.Code;
///
///     public string? TryGetErrorMessage(object? capturedFailure)
///         => (capturedFailure as PaymentError)?.Message;
/// }
/// </code>
/// </example>
public interface IFunctionalFailureDetector
{
    /// <summary>
    /// Attempts to determine whether the response represents a functional failure.
    /// </summary>
    /// <param name="response">Object returned by the handler.</param>
    /// <param name="reason">Code or description of the failure when detected.</param>
    /// <param name="capturedFailure">Captured error instance for further inspection.</param>
    /// <returns><c>true</c> when a functional failure was identified; otherwise <c>false</c>.</returns>
    bool TryExtractFailure(object? response, out string reason, out object? capturedFailure);

    /// <summary>
    /// Gets a standardized error code from the captured object.
    /// </summary>
    /// <param name="capturedFailure">Instance previously returned by <see cref="TryExtractFailure"/>.</param>
    /// <returns>The interpreted code or <c>null</c> when not applicable.</returns>
    string? TryGetErrorCode(object? capturedFailure);

    /// <summary>
    /// Gets a human-friendly message or detail from the captured error.
    /// </summary>
    /// <param name="capturedFailure">Instance returned by <see cref="TryExtractFailure"/>.</param>
    /// <returns>Message to display or <c>null</c> when unavailable.</returns>
    string? TryGetErrorMessage(object? capturedFailure);
}

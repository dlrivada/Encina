using System;

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
///     public bool TryExtractFailure(object? response, out string reason, out object? error)
///     {
///         if (response is PaymentOutcome outcome && outcome.TryGetError(out var paymentError))
///         {
///             reason = paymentError.Code ?? "payment.failure";
///             error = paymentError;
///             return true;
///         }
///
///         reason = string.Empty;
///         error = null;
///         return false;
///     }
///
///     public string? TryGetErrorCode(object? error)
///         => (error as PaymentError)?.Code;
///
///     public string? TryGetErrorMessage(object? error)
///         => (error as PaymentError)?.Message;
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
    /// <param name="error">Captured error instance for further inspection.</param>
    /// <returns><c>true</c> when a functional failure was identified; otherwise <c>false</c>.</returns>
    bool TryExtractFailure(object? response, out string reason, out object? error);

    /// <summary>
    /// Gets a standardized error code from the captured object.
    /// </summary>
    /// <param name="error">Instance previously returned by <see cref="TryExtractFailure"/>.</param>
    /// <returns>The interpreted code or <c>null</c> when not applicable.</returns>
    string? TryGetErrorCode(object? error);

    /// <summary>
    /// Gets a human-friendly message or detail from the captured error.
    /// </summary>
    /// <param name="error">Instance returned by <see cref="TryExtractFailure"/>.</param>
    /// <returns>Message to display or <c>null</c> when unavailable.</returns>
    string? TryGetErrorMessage(object? error);
}

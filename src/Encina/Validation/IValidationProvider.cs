namespace Encina.Validation;

/// <summary>
/// Defines a provider that performs validation on requests.
/// </summary>
/// <remarks>
/// <para>
/// This interface abstracts the validation mechanism, allowing different validation libraries
/// (FluentValidation, DataAnnotations, MiniValidator) to be used interchangeably.
/// </para>
/// <para>
/// Implementations should be thread-safe and reusable across requests.
/// </para>
/// </remarks>
public interface IValidationProvider
{
    /// <summary>
    /// Validates the specified request.
    /// </summary>
    /// <typeparam name="TRequest">The type of request to validate.</typeparam>
    /// <param name="request">The request instance to validate.</param>
    /// <param name="context">The request context containing metadata like CorrelationId, UserId, TenantId.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValidationResult"/> containing the validation outcome.
    /// If validation passes, <see cref="ValidationResult.IsValid"/> is <c>true</c>.
    /// If validation fails, <see cref="ValidationResult.Errors"/> contains the validation errors.
    /// </returns>
    ValueTask<ValidationResult> ValidateAsync<TRequest>(
        TRequest request,
        IRequestContext context,
        CancellationToken cancellationToken);
}

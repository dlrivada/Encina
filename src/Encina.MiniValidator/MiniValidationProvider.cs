using Encina.Validation;

namespace Encina.MiniValidator;

/// <summary>
/// Validation provider that uses MiniValidation to validate requests.
/// </summary>
/// <remarks>
/// <para>
/// This provider integrates MiniValidation with Encina's validation orchestration system.
/// MiniValidation is a minimalist validation library that uses Data Annotations under the hood,
/// designed specifically for Minimal APIs and lightweight scenarios.
/// </para>
/// <para>
/// <b>Lightweight Alternative</b>: MiniValidation is perfect for Minimal APIs where you want validation
/// without the overhead of FluentValidation or the ceremony of Data Annotations.
/// </para>
/// </remarks>
public sealed class MiniValidationProvider : IValidationProvider
{
    /// <inheritdoc />
    public ValueTask<ValidationResult> ValidateAsync<TRequest>(
        TRequest request,
        IRequestContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        // Validate using MiniValidation
        var isValid = MiniValidation.MiniValidator.TryValidate(request, out var errors);

        if (isValid || errors.Count == 0)
        {
            return ValueTask.FromResult(ValidationResult.Success);
        }

        var validationErrors = errors
            .SelectMany(kvp => kvp.Value.Select(msg => new ValidationError(kvp.Key, msg)))
            .ToList();

        return ValueTask.FromResult(ValidationResult.Failure(validationErrors));
    }
}

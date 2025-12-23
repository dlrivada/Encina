using System.Collections.Immutable;

namespace Encina.Validation;

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
/// <remarks>
/// This is an immutable value type that encapsulates validation success or failure
/// in a provider-agnostic way, allowing different validation libraries to return
/// consistent results.
/// </remarks>
public sealed class ValidationResult
{
    /// <summary>
    /// Gets a successful validation result with no errors.
    /// </summary>
    public static ValidationResult Success { get; } = new(ImmutableArray<ValidationError>.Empty);

    /// <summary>
    /// Gets the collection of validation errors.
    /// </summary>
    public ImmutableArray<ValidationError> Errors { get; }

    /// <summary>
    /// Gets a value indicating whether the validation passed.
    /// </summary>
    public bool IsValid => Errors.IsEmpty;

    /// <summary>
    /// Gets a value indicating whether the validation failed.
    /// </summary>
    public bool IsInvalid => !IsValid;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationResult"/> class.
    /// </summary>
    /// <param name="errors">The collection of validation errors.</param>
    private ValidationResult(ImmutableArray<ValidationError> errors)
    {
        Errors = errors;
    }

    /// <summary>
    /// Creates a failed validation result with the specified errors.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <returns>A <see cref="ValidationResult"/> representing failure.</returns>
    public static ValidationResult Failure(IEnumerable<ValidationError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        var errorArray = errors.ToImmutableArray();
        return errorArray.IsEmpty ? Success : new ValidationResult(errorArray);
    }

    /// <summary>
    /// Creates a failed validation result with a single error.
    /// </summary>
    /// <param name="propertyName">The name of the property that failed validation.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A <see cref="ValidationResult"/> representing failure.</returns>
    public static ValidationResult Failure(string propertyName, string errorMessage)
    {
        return new ValidationResult(ImmutableArray.Create(new ValidationError(propertyName, errorMessage)));
    }

    /// <summary>
    /// Creates a formatted error message containing all validation errors.
    /// </summary>
    /// <param name="requestTypeName">The name of the request type being validated.</param>
    /// <returns>A formatted error message string.</returns>
    public string ToErrorMessage(string requestTypeName)
    {
        if (IsValid)
        {
            return string.Empty;
        }

        var errorMessages = Errors.Select(e => e.PropertyName is not null
            ? $"{e.PropertyName}: {e.ErrorMessage}"
            : e.ErrorMessage);

        return $"Validation failed for {requestTypeName} with {Errors.Length} error(s): {string.Join(", ", errorMessages)}";
    }
}

/// <summary>
/// Represents a single validation error.
/// </summary>
/// <param name="PropertyName">The name of the property that failed validation, or null for object-level errors.</param>
/// <param name="ErrorMessage">The error message describing the validation failure.</param>
public sealed record ValidationError(string? PropertyName, string ErrorMessage);

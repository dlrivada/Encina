using System.ComponentModel.DataAnnotations;
using Encina.Validation;
using ValidationResult = Encina.Validation.ValidationResult;

namespace Encina.DataAnnotations;

/// <summary>
/// Validation provider that uses System.ComponentModel.DataAnnotations to validate requests.
/// </summary>
/// <remarks>
/// <para>
/// This provider integrates Data Annotations with Encina's validation orchestration system.
/// It validates requests decorated with attributes like <c>[Required]</c>, <c>[EmailAddress]</c>, etc.
/// </para>
/// <para>
/// <b>Zero External Dependencies</b>: Uses built-in .NET validation, no additional packages required.
/// </para>
/// </remarks>
public sealed class DataAnnotationsValidationProvider : IValidationProvider
{
    /// <inheritdoc />
    public ValueTask<ValidationResult> ValidateAsync<TRequest>(
        TRequest request,
        IRequestContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        // Create validation context with Encina metadata
        var validationContext = new ValidationContext(request, serviceProvider: null, items: null);
        validationContext.Items["CorrelationId"] = context.CorrelationId;

        if (context.UserId is not null)
        {
            validationContext.Items["UserId"] = context.UserId;
        }

        if (context.TenantId is not null)
        {
            validationContext.Items["TenantId"] = context.TenantId;
        }

        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(
            request,
            validationContext,
            validationResults,
            validateAllProperties: true);

        if (isValid)
        {
            return ValueTask.FromResult(ValidationResult.Success);
        }

        var errors = validationResults
            .Select(vr => new ValidationError(
                vr.MemberNames.FirstOrDefault(),
                vr.ErrorMessage ?? "Validation error"))
            .ToList();

        return ValueTask.FromResult(ValidationResult.Failure(errors));
    }
}

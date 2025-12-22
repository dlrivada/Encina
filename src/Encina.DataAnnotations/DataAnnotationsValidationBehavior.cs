using System.ComponentModel.DataAnnotations;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.DataAnnotations;

/// <summary>
/// Pipeline behavior that validates requests using Data Annotations before handler execution.
/// </summary>
/// <typeparam name="TRequest">The request type to validate.</typeparam>
/// <typeparam name="TResponse">The response type returned by the handler.</typeparam>
/// <remarks>
/// <para>
/// This behavior integrates System.ComponentModel.DataAnnotations with Encina's Railway Oriented Programming (ROP) model.
/// It validates requests decorated with validation attributes like <c>[Required]</c>, <c>[EmailAddress]</c>, etc., before handler execution
/// and short-circuits the pipeline with a validation error if validation fails.
/// </para>
/// <para>
/// Validation failures are returned as <c>Left&lt;EncinaError&gt;</c> containing a <see cref="ValidationException"/>
/// with all validation errors. This allows downstream code to inspect and handle validation failures functionally.
/// </para>
/// <para>
/// <b>Zero Dependencies</b>: Uses built-in .NET validation, no external packages required.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register with DI
/// services.AddEncina(cfg =>
/// {
///     cfg.AddDataAnnotationsValidation();
/// }, typeof(CreateUser).Assembly);
///
/// // Define request with attributes
/// public record CreateUser(
///     [Required]
///     [EmailAddress]
///     string Email,
///
///     [Required]
///     [MinLength(8)]
///     string Password
/// ) : ICommand&lt;UserId&gt;;
///
/// // Handler receives only valid requests
/// public class CreateUserHandler : ICommandHandler&lt;CreateUser, UserId&gt;
/// {
///     public Task&lt;Either&lt;EncinaError, UserId&gt;&gt; Handle(CreateUser request, CancellationToken ct)
///     {
///         // request is guaranteed to be valid here
///         return Task.FromResult(Right&lt;EncinaError, UserId&gt;(UserId.New()));
///     }
/// }
/// </code>
/// </example>
public sealed class DataAnnotationsValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextStep);

        if (cancellationToken.IsCancellationRequested)
        {
            return Left<EncinaError, TResponse>(
                EncinaError.New("Operation was cancelled before validation."));
        }

        // Validate using Data Annotations
        var validationContext = new ValidationContext(request, serviceProvider: null, items: null);

        // Enrich validation context with Encina metadata
        validationContext.Items["CorrelationId"] = context.CorrelationId;

        if (context.UserId is not null)
        {
            validationContext.Items["UserId"] = context.UserId;
        }

        if (context.TenantId is not null)
        {
            validationContext.Items["TenantId"] = context.TenantId;
        }

        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(
            request,
            validationContext,
            validationResults,
            validateAllProperties: true);

        if (!isValid)
        {
            // Create error message with all validation failures
            var errorMessages = validationResults
                .Select(vr => vr.ErrorMessage ?? "Validation error")
                .ToList();

            var validationException = new ValidationException(
                $"Validation failed for {typeof(TRequest).Name} with {validationResults.Count} error(s): {string.Join(", ", errorMessages)}");

            // Store validation results in Data property for inspection
            validationException.Data["ValidationResults"] = validationResults;

            return Left<EncinaError, TResponse>(
                EncinaError.New(
                    validationException,
                    $"Validation failed for {typeof(TRequest).Name} with {validationResults.Count} error(s)."));
        }

        // Validation passed, proceed to next step
        return await nextStep().ConfigureAwait(false);
    }
}

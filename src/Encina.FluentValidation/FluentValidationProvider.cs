using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using EncinaValidation = global::Encina.Validation;

namespace Encina.FluentValidation;

/// <summary>
/// Validation provider that uses FluentValidation to validate requests.
/// </summary>
/// <remarks>
/// <para>
/// This provider integrates FluentValidation with Encina's validation orchestration system.
/// It resolves validators from the DI container and executes them in parallel.
/// </para>
/// <para>
/// Validators are resolved using <see cref="IServiceProvider"/> to support scoped validators
/// that depend on services like DbContext.
/// </para>
/// </remarks>
public sealed class FluentValidationProvider : EncinaValidation.IValidationProvider
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="FluentValidationProvider"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve validators from.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="serviceProvider"/> is null.</exception>
    public FluentValidationProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc />
    public async ValueTask<EncinaValidation.ValidationResult> ValidateAsync<TRequest>(
        TRequest request,
        IRequestContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        // Resolve all validators for this request type
        var validators = _serviceProvider.GetServices<IValidator<TRequest>>().ToList();

        if (validators.Count == 0)
        {
            return EncinaValidation.ValidationResult.Success;
        }

        // Create validation context with Encina metadata
        var validationContext = new ValidationContext<TRequest>(request);
        validationContext.RootContextData["CorrelationId"] = context.CorrelationId;

        if (context.UserId is not null)
        {
            validationContext.RootContextData["UserId"] = context.UserId;
        }

        if (context.TenantId is not null)
        {
            validationContext.RootContextData["TenantId"] = context.TenantId;
        }

        // Run all validators in parallel
        var validationTasks = validators
            .Select(v => v.ValidateAsync(validationContext, cancellationToken))
            .ToArray();

        var validationResults = await Task.WhenAll(validationTasks).ConfigureAwait(false);

        // Aggregate all validation failures
        var errors = validationResults
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .Select(failure => new EncinaValidation.ValidationError(failure.PropertyName, failure.ErrorMessage))
            .ToList();

        return errors.Count == 0
            ? EncinaValidation.ValidationResult.Success
            : EncinaValidation.ValidationResult.Failure(errors);
    }
}

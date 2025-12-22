using FluentValidation;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.FluentValidation;

/// <summary>
/// Pipeline behavior that validates requests using FluentValidation before handler execution.
/// </summary>
/// <typeparam name="TRequest">The request type to validate.</typeparam>
/// <typeparam name="TResponse">The response type returned by the handler.</typeparam>
/// <remarks>
/// <para>
/// This behavior integrates FluentValidation with Encina's Railway Oriented Programming (ROP) model.
/// It validates requests before handler execution and short-circuits the pipeline with a <see cref="ValidationException"/>
/// wrapped in <see cref="MediatorError"/> if validation fails.
/// </para>
/// <para>
/// Validation failures are returned as <c>Left&lt;MediatorError&gt;</c> containing a <see cref="ValidationException"/>
/// with all validation errors. This allows downstream code to inspect and handle validation failures functionally.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register with DI
/// services.AddEncina(cfg =>
/// {
///     cfg.AddFluentValidation(typeof(CreateUserValidator).Assembly);
/// }, typeof(CreateUser).Assembly);
///
/// // Define validator
/// public class CreateUserValidator : AbstractValidator&lt;CreateUser&gt;
/// {
///     public CreateUserValidator()
///     {
///         RuleFor(x => x.Email).NotEmpty().EmailAddress();
///         RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
///     }
/// }
///
/// // Handler receives only valid requests
/// public class CreateUserHandler : ICommandHandler&lt;CreateUser, UserId&gt;
/// {
///     public Task&lt;Either&lt;MediatorError, UserId&gt;&gt; Handle(CreateUser request, CancellationToken ct)
///     {
///         // request is guaranteed to be valid here
///         return Task.FromResult(Right&lt;MediatorError, UserId&gt;(UserId.New()));
///     }
/// }
/// </code>
/// </example>
public sealed class ValidationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="validators">All registered validators for the request type.</param>
    public ValidationPipelineBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators ?? Enumerable.Empty<IValidator<TRequest>>();
    }

    /// <inheritdoc />
    public async ValueTask<Either<MediatorError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        // Skip validation if no validators are registered
        if (!_validators.Any())
        {
            return await nextStep().ConfigureAwait(false);
        }

        // Create validation context with custom properties from IRequestContext
        var validationContext = new ValidationContext<TRequest>(request);

        // Enrich validation context with mediator metadata
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
        var validationTasks = _validators
            .Select(v => v.ValidateAsync(validationContext, cancellationToken))
            .ToArray();

        try
        {
            var validationResults = await Task.WhenAll(validationTasks).ConfigureAwait(false);

            // Aggregate all validation failures
            var failures = validationResults
                .SelectMany(result => result.Errors)
                .Where(failure => failure != null)
                .ToList();

            if (failures.Count != 0)
            {
                // Create ValidationException with all failures
                var validationException = new ValidationException(failures);

                // Return as MediatorError following ROP pattern
                return Left<MediatorError, TResponse>(
                    MediatorError.New(
                        validationException,
                        $"Validation failed for {typeof(TRequest).Name} with {failures.Count} error(s)."));
            }

            // Validation passed, proceed to next step
            return await nextStep().ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            return Left<MediatorError, TResponse>(
                MediatorError.New(ex, $"Validation cancelled for {typeof(TRequest).Name}."));
        }
    }
}

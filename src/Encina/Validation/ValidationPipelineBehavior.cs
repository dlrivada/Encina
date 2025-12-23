using LanguageExt;

namespace Encina.Validation;

/// <summary>
/// Pipeline behavior that validates requests before handler execution.
/// </summary>
/// <typeparam name="TRequest">The request type to validate.</typeparam>
/// <typeparam name="TResponse">The response type returned by the handler.</typeparam>
/// <remarks>
/// <para>
/// This behavior uses the <see cref="ValidationOrchestrator"/> to validate requests
/// before passing them to the next step in the pipeline. If validation fails,
/// the pipeline is short-circuited with an <see cref="EncinaError"/>.
/// </para>
/// <para>
/// This centralized behavior works with any <see cref="IValidationProvider"/> implementation:
/// <list type="bullet">
/// <item>FluentValidation via <c>FluentValidationProvider</c></item>
/// <item>DataAnnotations via <c>DataAnnotationsValidationProvider</c></item>
/// <item>MiniValidator via <c>MiniValidationProvider</c></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registration is automatic when using any validation package
/// services.AddEncinaFluentValidation(typeof(MyValidator).Assembly);
/// // or
/// services.AddDataAnnotationsValidation();
/// // or
/// services.AddMiniValidation();
/// </code>
/// </example>
public sealed class ValidationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ValidationOrchestrator _orchestrator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="orchestrator">The validation orchestrator.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="orchestrator"/> is null.</exception>
    public ValidationPipelineBehavior(ValidationOrchestrator orchestrator)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
    }

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

        var validationResult = await _orchestrator.ValidateAsync<TRequest, TResponse>(
            request, context, cancellationToken).ConfigureAwait(false);

        return await validationResult.Match(
            Left: error => ValueTask.FromResult(Either<EncinaError, TResponse>.Left(error)),
            Right: _ => nextStep()).ConfigureAwait(false);
    }
}

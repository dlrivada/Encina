using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Validation;

/// <summary>
/// Orchestrates validation operations using registered validation providers.
/// </summary>
/// <remarks>
/// <para>
/// This orchestrator centralizes validation logic, coordinating with provider-specific
/// implementations (FluentValidation, DataAnnotations, MiniValidator) through the
/// <see cref="IValidationProvider"/> interface.
/// </para>
/// <para>
/// The orchestrator handles:
/// <list type="bullet">
/// <item>Delegating validation to the registered provider</item>
/// <item>Converting validation results to ROP <see cref="Either{L,R}"/> format</item>
/// <item>Handling cancellation gracefully</item>
/// </list>
/// </para>
/// </remarks>
public sealed class ValidationOrchestrator
{
    private readonly IValidationProvider _validationProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationOrchestrator"/> class.
    /// </summary>
    /// <param name="validationProvider">The validation provider to use.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="validationProvider"/> is null.</exception>
    public ValidationOrchestrator(IValidationProvider validationProvider)
    {
        _validationProvider = validationProvider ?? throw new ArgumentNullException(nameof(validationProvider));
    }

    /// <summary>
    /// Validates the specified request using the registered validation provider.
    /// </summary>
    /// <typeparam name="TRequest">The type of request to validate.</typeparam>
    /// <typeparam name="TResponse">The type of response expected from the handler.</typeparam>
    /// <param name="request">The request to validate.</param>
    /// <param name="context">The request context.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right&lt;Unit&gt;</c> if validation passes, or <c>Left&lt;EncinaError&gt;</c> if validation fails.
    /// </returns>
    public async ValueTask<Either<EncinaError, Unit>> ValidateAsync<TRequest, TResponse>(
        TRequest request,
        IRequestContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        if (cancellationToken.IsCancellationRequested)
        {
            return Left<EncinaError, Unit>( // NOSONAR S6966: LanguageExt Left is a pure function
                EncinaError.New("Operation was cancelled before validation."));
        }

        try
        {
            var result = await _validationProvider.ValidateAsync(request, context, cancellationToken)
                .ConfigureAwait(false);

            if (result.IsInvalid)
            {
                var errorMessage = result.ToErrorMessage(typeof(TRequest).Name);
                return Left<EncinaError, Unit>(EncinaError.New(errorMessage)); // NOSONAR S6966: LanguageExt Left is a pure function
            }

            return Right<EncinaError, Unit>(Unit.Default); // NOSONAR S6966: LanguageExt Right is a pure function
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            return Left<EncinaError, Unit>( // NOSONAR S6966: LanguageExt Left is a pure function
                EncinaError.New(ex, $"Validation cancelled for {typeof(TRequest).Name}."));
        }
    }
}

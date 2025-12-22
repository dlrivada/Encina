using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.MiniValidator;

/// <summary>
/// Pipeline behavior that validates requests using MiniValidation before handler execution.
/// </summary>
/// <typeparam name="TRequest">The request type to validate.</typeparam>
/// <typeparam name="TResponse">The response type returned by the handler.</typeparam>
/// <remarks>
/// <para>
/// This behavior integrates MiniValidation with Encina's Railway Oriented Programming (ROP) model.
/// MiniValidation is a minimalist validation library that uses Data Annotations under the hood,
/// designed specifically for Minimal APIs and lightweight scenarios.
/// </para>
/// <para>
/// Validation failures are returned as <c>Left&lt;EncinaError&gt;</c> containing validation errors
/// in a structured format. This allows downstream code to inspect and handle validation failures functionally.
/// </para>
/// <para>
/// <b>Lightweight Alternative</b>: MiniValidation is perfect for Minimal APIs where you want validation
/// without the overhead of FluentValidation or the ceremony of Data Annotations.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register with DI
/// services.AddEncina(cfg =>
/// {
///     cfg.AddMiniValidation();
/// }, typeof(CreateUser).Assembly);
///
/// // Define request with Data Annotations
/// public record CreateUser : ICommand&lt;UserId&gt;
/// {
///     [Required]
///     [EmailAddress]
///     public string Email { get; init; } = string.Empty;
///
///     [Required]
///     [MinLength(8)]
///     public string Password { get; init; } = string.Empty;
/// }
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
public sealed class MiniValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
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

        // Validate using MiniValidation
        var isValid = MiniValidation.MiniValidator.TryValidate(request, out var errors);

        if (!isValid && errors.Count > 0)
        {
            // Create error message with all validation failures
            var errorMessages = errors
                .SelectMany(kvp => kvp.Value.Select(msg => $"{kvp.Key}: {msg}"))
                .ToList();

            var errorMessage = $"Validation failed for {typeof(TRequest).Name} with {errorMessages.Count} error(s): {string.Join(", ", errorMessages)}";

            return Left<EncinaError, TResponse>(
                EncinaError.New(errorMessage));
        }

        // Validation passed, proceed to next step
        return await nextStep().ConfigureAwait(false);
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SimpleMediator.MiniValidator;

/// <summary>
/// Extension methods for configuring MiniValidation with SimpleMediator.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds MiniValidation pipeline behavior to SimpleMediator.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers <see cref="MiniValidationBehavior{TRequest, TResponse}"/> as an open generic pipeline behavior
    /// that automatically validates all requests using MiniValidation before handler execution.
    /// </para>
    /// <para>
    /// <b>MiniValidation</b> is a minimalist validation library that uses Data Annotations under the hood,
    /// designed specifically for Minimal APIs. It's lightweight (~20KB) and perfect for scenarios where
    /// FluentValidation feels like overkill.
    /// </para>
    /// <para>
    /// The behavior validates requests using <c>MiniValidator.TryValidate</c>, which validates all properties
    /// decorated with validation attributes like <c>[Required]</c>, <c>[EmailAddress]</c>, etc.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register MiniValidation
    /// services.AddSimpleMediator(cfg =>
    /// {
    ///     // Configuration if needed
    /// }, typeof(CreateUser).Assembly);
    ///
    /// services.AddMiniValidation();
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
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddMiniValidation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register the validation pipeline behavior as an open generic
        services.TryAddTransient(typeof(IPipelineBehavior<,>), typeof(MiniValidationBehavior<,>));

        return services;
    }
}

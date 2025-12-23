using Encina.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.DataAnnotations;

/// <summary>
/// Extension methods for configuring Data Annotations validation with Encina.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Data Annotations validation pipeline behavior to Encina.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method performs the following operations:
    /// <list type="number">
    /// <item>Registers <see cref="DataAnnotationsValidationProvider"/> as the <see cref="IValidationProvider"/></item>
    /// <item>Registers <see cref="ValidationOrchestrator"/> for coordinating validation</item>
    /// <item>Registers <see cref="ValidationPipelineBehavior{TRequest, TResponse}"/> as an open generic pipeline behavior</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Zero External Dependencies</b>: Uses built-in System.ComponentModel.DataAnnotations, no additional packages required.
    /// </para>
    /// <para>
    /// The behavior validates requests using <c>Validator.TryValidateObject</c> with <c>validateAllProperties: true</c>,
    /// which validates all properties decorated with validation attributes like:
    /// <list type="bullet">
    /// <item><c>[Required]</c> - Property must have a value</item>
    /// <item><c>[EmailAddress]</c> - Property must be a valid email</item>
    /// <item><c>[MinLength]</c> / <c>[MaxLength]</c> - String length constraints</item>
    /// <item><c>[Range]</c> - Numeric range validation</item>
    /// <item><c>[RegularExpression]</c> - Pattern matching</item>
    /// <item><c>[Compare]</c> - Compare with another property</item>
    /// <item>Custom <c>ValidationAttribute</c> implementations</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register Data Annotations validation
    /// services.AddEncina(cfg =>
    /// {
    ///     // Configuration if needed
    /// }, typeof(CreateUser).Assembly);
    ///
    /// services.AddDataAnnotationsValidation();
    ///
    /// // Define request with attributes
    /// public record CreateUser(
    ///     [Required]
    ///     [EmailAddress]
    ///     string Email,
    ///
    ///     [Required]
    ///     [MinLength(8)]
    ///     [RegularExpression(@"^(?=.*[A-Z])(?=.*\d).+$", ErrorMessage = "Password must contain uppercase and number")]
    ///     string Password
    /// ) : ICommand&lt;UserId&gt;;
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddDataAnnotationsValidation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register the validation infrastructure
        services.TryAddSingleton<IValidationProvider, DataAnnotationsValidationProvider>();
        services.TryAddSingleton<ValidationOrchestrator>();
        services.TryAddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehavior<,>));

        return services;
    }
}

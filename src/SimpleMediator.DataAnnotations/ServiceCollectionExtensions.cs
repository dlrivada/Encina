using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SimpleMediator.DataAnnotations;

/// <summary>
/// Extension methods for configuring Data Annotations validation with SimpleMediator.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Data Annotations validation pipeline behavior to SimpleMediator.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers <see cref="DataAnnotationsValidationBehavior{TRequest, TResponse}"/> as an open generic pipeline behavior
    /// that automatically validates all requests decorated with Data Annotations attributes before handler execution.
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
    /// services.AddSimpleMediator(cfg =>
    /// {
    ///     // DataAnnotationsValidationBehavior is automatically registered
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

        // Register the validation pipeline behavior as an open generic
        services.TryAddTransient(typeof(IPipelineBehavior<,>), typeof(DataAnnotationsValidationBehavior<,>));

        return services;
    }
}

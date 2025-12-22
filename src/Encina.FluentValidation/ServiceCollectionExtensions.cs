using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.FluentValidation;

/// <summary>
/// Extension methods for configuring FluentValidation integration with Encina.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds FluentValidation pipeline behavior and registers all validators from the specified assemblies.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="assemblies">Assemblies to scan for <see cref="IValidator{T}"/> implementations.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method performs two operations:
    /// <list type="number">
    /// <item>Registers <see cref="ValidationPipelineBehavior{TRequest, TResponse}"/> as an open generic pipeline behavior</item>
    /// <item>Scans the provided assemblies and registers all <see cref="IValidator{T}"/> implementations with <see cref="ServiceLifetime.Singleton"/> lifetime</item>
    /// </list>
    /// </para>
    /// <para>
    /// Validators are registered as singletons because they are typically stateless and thread-safe.
    /// If you need scoped or transient validators, register them manually after calling this method.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register FluentValidation with validators from specific assemblies
    /// services.AddEncinaFluentValidation(
    ///     typeof(CreateUserValidator).Assembly,
    ///     typeof(UpdateOrderValidator).Assembly);
    ///
    /// // Or use with assembly scanning pattern
    /// services.AddEncinaFluentValidation(
    ///     AppDomain.CurrentDomain.GetAssemblies()
    ///         .Where(a => a.FullName?.StartsWith("MyApp") ?? false)
    ///         .ToArray());
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="assemblies"/> is null.</exception>
    public static IServiceCollection AddEncinaFluentValidation(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (assemblies is null || assemblies.Length == 0)
        {
            throw new ArgumentNullException(nameof(assemblies), "At least one assembly must be provided to scan for validators.");
        }

        // Register the validation pipeline behavior as an open generic
        services.TryAddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehavior<,>));

        // Scan and register all validators from the provided assemblies
        RegisterValidators(services, assemblies);

        return services;
    }

    /// <summary>
    /// Adds FluentValidation pipeline behavior and registers all validators from the specified assemblies with a specified lifetime.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="lifetime">The lifetime for registered validators (Singleton, Scoped, or Transient).</param>
    /// <param name="assemblies">Assemblies to scan for <see cref="IValidator{T}"/> implementations.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Use this overload when you need validators with a specific lifetime (e.g., Scoped for validators that depend on EF DbContext).
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register validators with Scoped lifetime for database-dependent validators
    /// services.AddEncinaFluentValidation(
    ///     ServiceLifetime.Scoped,
    ///     typeof(CreateUserValidator).Assembly);
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="assemblies"/> is null.</exception>
    public static IServiceCollection AddEncinaFluentValidation(
        this IServiceCollection services,
        ServiceLifetime lifetime,
        params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (assemblies is null || assemblies.Length == 0)
        {
            throw new ArgumentNullException(nameof(assemblies), "At least one assembly must be provided to scan for validators.");
        }

        // Register the validation pipeline behavior as an open generic
        services.TryAddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehavior<,>));

        // Scan and register all validators with the specified lifetime
        RegisterValidators(services, assemblies, lifetime);

        return services;
    }

    private static void RegisterValidators(IServiceCollection services, Assembly[] assemblies, ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        // Find all types that implement IValidator<T>
        var validatorType = typeof(IValidator<>);

        var validatorTypes = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type =>
                type is { IsClass: true, IsAbstract: false } &&
                type.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == validatorType))
            .ToList();

        foreach (var validator in validatorTypes)
        {
            // Get all IValidator<T> interfaces implemented by this validator
            var interfaces = validator.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == validatorType)
                .ToList();

            foreach (var @interface in interfaces)
            {
                // Register the validator for each IValidator<T> it implements
                var descriptor = new ServiceDescriptor(@interface, validator, lifetime);
                services.TryAddEnumerable(descriptor);
            }
        }
    }
}

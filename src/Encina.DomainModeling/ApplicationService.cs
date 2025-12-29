using System.Reflection;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.DomainModeling;

/// <summary>
/// Marker interface for application services.
/// Application services orchestrate use cases, coordinating domain services,
/// repositories, and infrastructure concerns.
/// </summary>
/// <remarks>
/// <para>
/// Application services are distinct from domain services:
/// <list type="bullet">
///   <item><description><strong>Domain Services</strong>: Contain domain logic that doesn't fit in entities.</description></item>
///   <item><description><strong>Application Services</strong>: Orchestrate use cases, manage transactions, publish events.</description></item>
/// </list>
/// </para>
/// <para>
/// Application services typically:
/// <list type="bullet">
///   <item><description>Load data from repositories.</description></item>
///   <item><description>Execute domain logic via domain services or aggregates.</description></item>
///   <item><description>Persist changes.</description></item>
///   <item><description>Publish domain events as integration events.</description></item>
///   <item><description>Map results to DTOs.</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IApplicationService { }

/// <summary>
/// Typed application service for a specific use case.
/// </summary>
/// <typeparam name="TInput">The input type for the use case.</typeparam>
/// <typeparam name="TOutput">The output type of the use case.</typeparam>
/// <remarks>
/// <para>
/// Use this interface for use cases that require input and produce output.
/// The return type is <see cref="Either{L,R}"/> to support Railway-Oriented Programming.
/// </para>
/// <example>
/// <code>
/// public class CreateOrderApplicationService : IApplicationService&lt;CreateOrderInput, OrderDto&gt;
/// {
///     public async Task&lt;Either&lt;ApplicationServiceError, OrderDto&gt;&gt; ExecuteAsync(
///         CreateOrderInput input,
///         CancellationToken ct)
///     {
///         // 1. Load customer
///         // 2. Execute domain logic
///         // 3. Persist order
///         // 4. Publish events
///         // 5. Return DTO
///     }
/// }
/// </code>
/// </example>
/// </remarks>
public interface IApplicationService<in TInput, TOutput> : IApplicationService
{
    /// <summary>
    /// Executes the use case with the given input.
    /// </summary>
    /// <param name="input">The input for the use case.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Either an error or the output.</returns>
    Task<Either<ApplicationServiceError, TOutput>> ExecuteAsync(
        TInput input,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Application service without input (e.g., scheduled tasks, system operations).
/// </summary>
/// <typeparam name="TOutput">The output type of the use case.</typeparam>
public interface IApplicationService<TOutput> : IApplicationService
{
    /// <summary>
    /// Executes the use case without input.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Either an error or the output.</returns>
    Task<Either<ApplicationServiceError, TOutput>> ExecuteAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Application service that returns Unit (no meaningful output).
/// </summary>
/// <typeparam name="TInput">The input type for the use case.</typeparam>
public interface IVoidApplicationService<in TInput> : IApplicationService
{
    /// <summary>
    /// Executes the use case with the given input.
    /// </summary>
    /// <param name="input">The input for the use case.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    Task<Either<ApplicationServiceError, Unit>> ExecuteAsync(
        TInput input,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Error type for application service failures.
/// </summary>
/// <param name="Message">Description of the failure.</param>
/// <param name="ErrorCode">Machine-readable error code.</param>
/// <param name="ServiceType">The application service type that failed.</param>
/// <param name="InnerException">Optional inner exception.</param>
public sealed record ApplicationServiceError(
    string Message,
    string ErrorCode,
    Type ServiceType,
    Exception? InnerException = null)
{
    /// <summary>
    /// Creates an error for when an entity is not found.
    /// </summary>
    public static ApplicationServiceError NotFound<TService>(
        string entityType,
        string entityId)
        where TService : IApplicationService =>
        new(
            $"{entityType} with ID '{entityId}' not found",
            "APP_SERVICE_NOT_FOUND",
            typeof(TService));

    /// <summary>
    /// Creates an error for when validation fails.
    /// </summary>
    public static ApplicationServiceError ValidationFailed<TService>(string reason)
        where TService : IApplicationService =>
        new(
            $"Validation failed: {reason}",
            "APP_SERVICE_VALIDATION_FAILED",
            typeof(TService));

    /// <summary>
    /// Creates an error for when a business rule is violated.
    /// </summary>
    public static ApplicationServiceError BusinessRuleViolation<TService>(string rule)
        where TService : IApplicationService =>
        new(
            $"Business rule violated: {rule}",
            "APP_SERVICE_BUSINESS_RULE_VIOLATION",
            typeof(TService));

    /// <summary>
    /// Creates an error for when a concurrency conflict occurs.
    /// </summary>
    public static ApplicationServiceError ConcurrencyConflict<TService>(
        string entityType,
        string entityId)
        where TService : IApplicationService =>
        new(
            $"Concurrency conflict for {entityType} with ID '{entityId}'",
            "APP_SERVICE_CONCURRENCY_CONFLICT",
            typeof(TService));

    /// <summary>
    /// Creates an error for when an infrastructure operation fails.
    /// </summary>
    public static ApplicationServiceError InfrastructureFailure<TService>(
        string operation,
        Exception exception)
        where TService : IApplicationService =>
        new(
            $"Infrastructure operation '{operation}' failed: {exception.Message}",
            "APP_SERVICE_INFRASTRUCTURE_FAILURE",
            typeof(TService),
            exception);

    /// <summary>
    /// Creates an error for when authorization fails.
    /// </summary>
    public static ApplicationServiceError Unauthorized<TService>(string reason)
        where TService : IApplicationService =>
        new(
            $"Unauthorized: {reason}",
            "APP_SERVICE_UNAUTHORIZED",
            typeof(TService));

    /// <summary>
    /// Creates an error from an adapter error.
    /// </summary>
    public static ApplicationServiceError FromAdapterError<TService>(AdapterError adapterError)
        where TService : IApplicationService =>
        new(
            adapterError.Message,
            $"APP_SERVICE_ADAPTER_{adapterError.ErrorCode}",
            typeof(TService),
            adapterError.InnerException);

    /// <summary>
    /// Creates an error from a mapping error.
    /// </summary>
    public static ApplicationServiceError FromMappingError<TService>(MappingError mappingError)
        where TService : IApplicationService =>
        new(
            mappingError.Message,
            $"APP_SERVICE_MAPPING_{mappingError.ErrorCode}",
            typeof(TService),
            mappingError.InnerException);

    /// <summary>
    /// Creates an error from a repository error.
    /// </summary>
    public static ApplicationServiceError FromRepositoryError<TService>(RepositoryError repositoryError)
        where TService : IApplicationService =>
        new(
            repositoryError.Message,
            $"APP_SERVICE_REPOSITORY_{repositoryError.ErrorCode}",
            typeof(TService),
            repositoryError.InnerException);
}

/// <summary>
/// Extension methods for application services.
/// </summary>
public static class ApplicationServiceExtensions
{
    /// <summary>
    /// Converts an adapter error to an application service error.
    /// </summary>
    public static Either<ApplicationServiceError, T> ToApplicationServiceError<TService, T>(
        this Either<AdapterError, T> result)
        where TService : IApplicationService
    {
        return result.MapLeft(error => ApplicationServiceError.FromAdapterError<TService>(error));
    }

    /// <summary>
    /// Converts a mapping error to an application service error.
    /// </summary>
    public static Either<ApplicationServiceError, T> ToApplicationServiceError<TService, T>(
        this Either<MappingError, T> result)
        where TService : IApplicationService
    {
        return result.MapLeft(error => ApplicationServiceError.FromMappingError<TService>(error));
    }

    /// <summary>
    /// Converts a repository error to an application service error.
    /// </summary>
    public static Either<ApplicationServiceError, T> ToApplicationServiceError<TService, T>(
        this Either<RepositoryError, T> result)
        where TService : IApplicationService
    {
        return result.MapLeft(error => ApplicationServiceError.FromRepositoryError<TService>(error));
    }
}

/// <summary>
/// Extension methods for registering application services with dependency injection.
/// </summary>
public static class ApplicationServiceRegistrationExtensions
{
    /// <summary>
    /// Registers an application service.
    /// </summary>
    /// <typeparam name="TService">The application service type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">The service lifetime. Defaults to Scoped.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApplicationService<TService>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TService : class, IApplicationService
    {
        ArgumentNullException.ThrowIfNull(services);

        var serviceType = typeof(TService);
        var interfaces = serviceType.GetInterfaces()
            .Where(i => i.IsGenericType &&
                (i.GetGenericTypeDefinition() == typeof(IApplicationService<,>) ||
                 i.GetGenericTypeDefinition() == typeof(IApplicationService<>) ||
                 i.GetGenericTypeDefinition() == typeof(IVoidApplicationService<>)))
            .ToList();

        foreach (var interfaceType in interfaces)
        {
            services.Add(new ServiceDescriptor(interfaceType, serviceType, lifetime));
        }

        // Also register the concrete type
        services.Add(new ServiceDescriptor(serviceType, serviceType, lifetime));

        return services;
    }

    /// <summary>
    /// Scans an assembly for all application services and registers them.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assembly">The assembly to scan.</param>
    /// <param name="lifetime">The service lifetime. Defaults to Scoped.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApplicationServicesFromAssembly(
        this IServiceCollection services,
        Assembly assembly,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        var applicationServiceTypes = assembly.GetTypes()
            .Where(t => t.IsClass &&
                !t.IsAbstract &&
                typeof(IApplicationService).IsAssignableFrom(t));

        foreach (var serviceType in applicationServiceTypes)
        {
            var interfaces = serviceType.GetInterfaces()
                .Where(i => i.IsGenericType &&
                    (i.GetGenericTypeDefinition() == typeof(IApplicationService<,>) ||
                     i.GetGenericTypeDefinition() == typeof(IApplicationService<>) ||
                     i.GetGenericTypeDefinition() == typeof(IVoidApplicationService<>)))
                .ToList();

            foreach (var interfaceType in interfaces)
            {
                services.Add(new ServiceDescriptor(interfaceType, serviceType, lifetime));
            }

            // Also register the concrete type
            services.Add(new ServiceDescriptor(serviceType, serviceType, lifetime));
        }

        return services;
    }
}

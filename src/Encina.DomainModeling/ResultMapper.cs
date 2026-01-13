using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.DomainModeling;

/// <summary>
/// Maps domain models to DTOs with Railway-Oriented Programming semantics.
/// </summary>
/// <typeparam name="TDomain">The domain model type.</typeparam>
/// <typeparam name="TDto">The DTO type.</typeparam>
/// <remarks>
/// <para>
/// Result mappers ensure that mapping operations can fail gracefully,
/// returning an <see cref="Either{L,R}"/> instead of throwing exceptions.
/// </para>
/// <example>
/// <code>
/// public class OrderToOrderDtoMapper : IResultMapper&lt;Order, OrderDto&gt;
/// {
///     public Either&lt;MappingError, OrderDto&gt; Map(Order domain)
///     {
///         if (domain.Items.Count == 0)
///             return MappingError.ValidationFailed&lt;Order, OrderDto&gt;("Order has no items");
///
///         return new OrderDto { Id = domain.Id.Value, ... };
///     }
/// }
/// </code>
/// </example>
/// </remarks>
public interface IResultMapper<in TDomain, TDto>
{
    /// <summary>
    /// Maps a domain model to a DTO.
    /// </summary>
    /// <param name="domain">The domain model to map.</param>
    /// <returns>Either an error or the mapped DTO.</returns>
    Either<MappingError, TDto> Map(TDomain domain);
}

/// <summary>
/// Async variant for mappings that require async operations.
/// </summary>
/// <typeparam name="TDomain">The domain model type.</typeparam>
/// <typeparam name="TDto">The DTO type.</typeparam>
/// <remarks>
/// <para>
/// Use this interface when the mapping requires async operations such as:
/// <list type="bullet">
///   <item><description>Loading related data from a database.</description></item>
///   <item><description>Calling external services for enrichment.</description></item>
///   <item><description>Fetching configuration from remote sources.</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IAsyncResultMapper<in TDomain, TDto>
{
    /// <summary>
    /// Asynchronously maps a domain model to a DTO.
    /// </summary>
    /// <param name="domain">The domain model to map.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing either an error or the mapped DTO.</returns>
    Task<Either<MappingError, TDto>> MapAsync(TDomain domain, CancellationToken cancellationToken = default);
}

/// <summary>
/// Bidirectional mapper for commands that need to map DTOs back to domain models.
/// </summary>
/// <typeparam name="TDomain">The domain model type.</typeparam>
/// <typeparam name="TDto">The DTO type.</typeparam>
public interface IBidirectionalMapper<TDomain, TDto> : IResultMapper<TDomain, TDto>
{
    /// <summary>
    /// Maps a DTO back to a domain model.
    /// </summary>
    /// <param name="dto">The DTO to map.</param>
    /// <returns>Either an error or the domain model.</returns>
    Either<MappingError, TDomain> MapToDomain(TDto dto);
}

/// <summary>
/// Async bidirectional mapper for async operations in both directions.
/// </summary>
/// <typeparam name="TDomain">The domain model type.</typeparam>
/// <typeparam name="TDto">The DTO type.</typeparam>
public interface IAsyncBidirectionalMapper<TDomain, TDto> : IAsyncResultMapper<TDomain, TDto>
{
    /// <summary>
    /// Asynchronously maps a DTO back to a domain model.
    /// </summary>
    /// <param name="dto">The DTO to map.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing either an error or the domain model.</returns>
    Task<Either<MappingError, TDomain>> MapToDomainAsync(TDto dto, CancellationToken cancellationToken = default);
}

/// <summary>
/// Error type for mapping operation failures.
/// </summary>
/// <param name="Message">Description of the mapping failure.</param>
/// <param name="ErrorCode">Machine-readable error code.</param>
/// <param name="SourceType">The source type of the mapping.</param>
/// <param name="TargetType">The target type of the mapping.</param>
/// <param name="PropertyName">Optional property that caused the error.</param>
/// <param name="InnerException">Optional inner exception.</param>
public sealed record MappingError(
    string Message,
    string ErrorCode,
    Type SourceType,
    Type TargetType,
    string? PropertyName = null,
    Exception? InnerException = null)
{
    /// <summary>
    /// Creates an error for when a required property is null.
    /// </summary>
    public static MappingError NullProperty<TSource, TTarget>(string propertyName) =>
        new(
            $"Required property '{propertyName}' is null",
            "MAPPING_NULL_PROPERTY",
            typeof(TSource),
            typeof(TTarget),
            propertyName);

    /// <summary>
    /// Creates an error for when a validation fails during mapping.
    /// </summary>
    public static MappingError ValidationFailed<TSource, TTarget>(string reason) =>
        new(
            $"Mapping validation failed: {reason}",
            "MAPPING_VALIDATION_FAILED",
            typeof(TSource),
            typeof(TTarget));

    /// <summary>
    /// Creates an error for when a type conversion fails.
    /// </summary>
    public static MappingError ConversionFailed<TSource, TTarget>(
        string propertyName,
        Exception? exception = null) =>
        new(
            $"Failed to convert property '{propertyName}'",
            "MAPPING_CONVERSION_FAILED",
            typeof(TSource),
            typeof(TTarget),
            propertyName,
            exception);

    /// <summary>
    /// Creates an error for when a collection is empty but expected to have items.
    /// </summary>
    public static MappingError EmptyCollection<TSource, TTarget>(string propertyName) =>
        new(
            $"Collection property '{propertyName}' is empty but expected to have items",
            "MAPPING_EMPTY_COLLECTION",
            typeof(TSource),
            typeof(TTarget),
            propertyName);

    /// <summary>
    /// Creates an error for when the mapping operation fails unexpectedly.
    /// </summary>
    public static MappingError OperationFailed<TSource, TTarget>(Exception exception) =>
        new(
            $"Mapping operation failed: {exception.Message}",
            "MAPPING_OPERATION_FAILED",
            typeof(TSource),
            typeof(TTarget),
            InnerException: exception);
}

/// <summary>
/// Extension methods for result mappers.
/// </summary>
public static class ResultMapperExtensions
{
    /// <summary>
    /// Maps a collection of domain models to DTOs, returning the first error encountered.
    /// </summary>
    /// <typeparam name="TDomain">The domain model type.</typeparam>
    /// <typeparam name="TDto">The DTO type.</typeparam>
    /// <param name="mapper">The mapper to use.</param>
    /// <param name="domains">The domain models to map.</param>
    /// <returns>Either an error or the list of mapped DTOs.</returns>
    public static Either<MappingError, IReadOnlyList<TDto>> MapAll<TDomain, TDto>(
        this IResultMapper<TDomain, TDto> mapper,
        IEnumerable<TDomain> domains)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        ArgumentNullException.ThrowIfNull(domains);

        var results = new List<TDto>();

        foreach (var domain in domains)
        {
            var result = mapper.Map(domain);

            if (result.IsLeft)
            {
                return result.Match(
                    Right: _ => throw new InvalidOperationException("Expected Left"),
                    Left: error => error);
            }

            result.IfRight(dto => results.Add(dto));
        }

        return results.AsReadOnly();
    }

    /// <summary>
    /// Maps a collection of domain models to DTOs, collecting all errors.
    /// </summary>
    /// <typeparam name="TDomain">The domain model type.</typeparam>
    /// <typeparam name="TDto">The DTO type.</typeparam>
    /// <param name="mapper">The mapper to use.</param>
    /// <param name="domains">The domain models to map.</param>
    /// <returns>Either a list of errors or the list of mapped DTOs.</returns>
    public static Either<IReadOnlyList<MappingError>, IReadOnlyList<TDto>> MapAllCollectErrors<TDomain, TDto>(
        this IResultMapper<TDomain, TDto> mapper,
        IEnumerable<TDomain> domains)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        ArgumentNullException.ThrowIfNull(domains);

        var results = new List<TDto>();
        var errors = new List<MappingError>();

        foreach (var domain in domains)
        {
            var result = mapper.Map(domain);

            result.Match(
                Right: dto => results.Add(dto),
                Left: error => errors.Add(error));
        }

        if (errors.Count > 0)
        {
            return errors.AsReadOnly();
        }

        return results.AsReadOnly();
    }

    /// <summary>
    /// Asynchronously maps a collection of domain models to DTOs.
    /// </summary>
    /// <typeparam name="TDomain">The domain model type.</typeparam>
    /// <typeparam name="TDto">The DTO type.</typeparam>
    /// <param name="mapper">The async mapper to use.</param>
    /// <param name="domains">The domain models to map.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Either an error or the list of mapped DTOs.</returns>
    public static async Task<Either<MappingError, IReadOnlyList<TDto>>> MapAllAsync<TDomain, TDto>(
        this IAsyncResultMapper<TDomain, TDto> mapper,
        IEnumerable<TDomain> domains,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        ArgumentNullException.ThrowIfNull(domains);

        var results = new List<TDto>();

        foreach (var domain in domains)
        {
            var result = await mapper.MapAsync(domain, cancellationToken).ConfigureAwait(false);

            if (result.IsLeft)
            {
                return result.Match(
                    Right: _ => throw new InvalidOperationException("Expected Left"),
                    Left: error => error);
            }

            result.IfRight(dto => results.Add(dto));
        }

        return results.AsReadOnly();
    }

    /// <summary>
    /// Creates a mapper that combines two mappers in sequence.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TIntermediate">The intermediate type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="first">The first mapper.</param>
    /// <param name="second">The second mapper.</param>
    /// <returns>A composed mapper.</returns>
    public static IResultMapper<TSource, TTarget> Compose<TSource, TIntermediate, TTarget>(
        this IResultMapper<TSource, TIntermediate> first,
        IResultMapper<TIntermediate, TTarget> second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return new ComposedResultMapper<TSource, TIntermediate, TTarget>(first, second);
    }

    /// <summary>
    /// Tries to map a domain model, returning None if mapping fails.
    /// </summary>
    /// <typeparam name="TDomain">The domain model type.</typeparam>
    /// <typeparam name="TDto">The DTO type.</typeparam>
    /// <param name="mapper">The mapper to use.</param>
    /// <param name="domain">The domain model to map.</param>
    /// <returns>Some(DTO) on success, None on failure.</returns>
    public static Option<TDto> TryMap<TDomain, TDto>(
        this IResultMapper<TDomain, TDto> mapper,
        TDomain domain)
    {
        if (mapper is null || domain is null)
        {
            return Option<TDto>.None;
        }

        try
        {
            var result = mapper.Map(domain);
            return result.Match(
                Right: dto => Option<TDto>.Some(dto),
                Left: _ => Option<TDto>.None);
        }
        catch
        {
            return Option<TDto>.None;
        }
    }

    [SuppressMessage("SonarQube", "S2436:Classes and methods should not have too many generic parameters",
        Justification = "Three generic parameters are required for mapper composition: source, intermediate, and target types.")]
    private sealed class ComposedResultMapper<TSource, TIntermediate, TTarget>(
        IResultMapper<TSource, TIntermediate> first,
        IResultMapper<TIntermediate, TTarget> second)
        : IResultMapper<TSource, TTarget>
    {
        public Either<MappingError, TTarget> Map(TSource domain)
        {
            var intermediate = first.Map(domain);

            return intermediate.Match(
                Right: i => second.Map(i),
                Left: error => error);
        }
    }
}

/// <summary>
/// Extension methods for registering result mappers with dependency injection.
/// </summary>
public static class ResultMapperRegistrationExtensions
{
    /// <summary>
    /// Registers a result mapper.
    /// </summary>
    /// <typeparam name="TDomain">The domain model type.</typeparam>
    /// <typeparam name="TDto">The DTO type.</typeparam>
    /// <typeparam name="TMapper">The mapper implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">The service lifetime. Defaults to Scoped.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddResultMapper<TDomain, TDto, TMapper>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TMapper : class, IResultMapper<TDomain, TDto>
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Add(new ServiceDescriptor(
            typeof(IResultMapper<TDomain, TDto>),
            typeof(TMapper),
            lifetime));

        return services;
    }

    /// <summary>
    /// Registers an async result mapper.
    /// </summary>
    /// <typeparam name="TDomain">The domain model type.</typeparam>
    /// <typeparam name="TDto">The DTO type.</typeparam>
    /// <typeparam name="TMapper">The mapper implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">The service lifetime. Defaults to Scoped.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAsyncResultMapper<TDomain, TDto, TMapper>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TMapper : class, IAsyncResultMapper<TDomain, TDto>
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Add(new ServiceDescriptor(
            typeof(IAsyncResultMapper<TDomain, TDto>),
            typeof(TMapper),
            lifetime));

        return services;
    }

    /// <summary>
    /// Scans an assembly for all result mappers and registers them.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assembly">The assembly to scan.</param>
    /// <param name="lifetime">The service lifetime. Defaults to Scoped.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddResultMappersFromAssembly(
        this IServiceCollection services,
        Assembly assembly,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        var mapperInterface = typeof(IResultMapper<,>);
        var asyncMapperInterface = typeof(IAsyncResultMapper<,>);

        var mapperTypes = GetLoadableTypes(assembly)
            .Where(t => t.IsClass && !t.IsAbstract)
            .Select(t => new
            {
                ImplementationType = t,
                Interfaces = t.GetInterfaces()
                    .Where(i => i.IsGenericType &&
                        (i.GetGenericTypeDefinition() == mapperInterface ||
                         i.GetGenericTypeDefinition() == asyncMapperInterface))
                    .ToList()
            })
            .Where(x => x.Interfaces.Count > 0);

        foreach (var mapper in mapperTypes)
        {
            foreach (var mapperInterfaceType in mapper.Interfaces)
            {
                services.Add(new ServiceDescriptor(
                    mapperInterfaceType,
                    mapper.ImplementationType,
                    lifetime));
            }
        }

        return services;
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
    }
}

using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Encina.DomainModeling;

/// <summary>
/// Represents a vertical slice (feature) that groups related functionality.
/// </summary>
/// <remarks>
/// <para>
/// A feature slice organizes code by feature rather than by layer.
/// Each slice contains all the code needed for a specific feature:
/// commands, queries, handlers, validators, and domain logic.
/// </para>
/// <para>
/// This pattern combines well with Hexagonal Architecture by keeping
/// ports and adapters at the slice level, enabling better cohesion
/// and easier testing.
/// </para>
/// <example>
/// <code>
/// public sealed class OrdersSlice : FeatureSlice
/// {
///     public override string FeatureName => "Orders";
///
///     public override void ConfigureServices(IServiceCollection services)
///     {
///         // Register slice-specific services
///         services.AddScoped&lt;IOrderRepository, OrderRepository&gt;();
///         services.AddPort&lt;IInventoryPort, InventoryAdapter&gt;();
///     }
/// }
/// </code>
/// </example>
/// </remarks>
public abstract class FeatureSlice
{
    /// <summary>
    /// Gets the name of this feature slice.
    /// </summary>
    public abstract string FeatureName { get; }

    /// <summary>
    /// Gets an optional description of this feature.
    /// </summary>
    public virtual string? Description => null;

    /// <summary>
    /// Gets the API route prefix for this slice (e.g., "/api/orders").
    /// </summary>
    public virtual string? RoutePrefix => null;

    /// <summary>
    /// Configures the services for this feature slice.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public abstract void ConfigureServices(IServiceCollection services);
}

/// <summary>
/// Interface for feature slices with endpoint configuration.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface when your slice exposes HTTP endpoints.
/// The endpoint configuration is separated to allow slices to work
/// without ASP.NET Core dependencies.
/// </para>
/// </remarks>
public interface IFeatureSliceWithEndpoints
{
    /// <summary>
    /// Gets the name of this feature slice.
    /// </summary>
    string FeatureName { get; }

    /// <summary>
    /// Configures the services for this feature slice.
    /// </summary>
    /// <param name="services">The service collection.</param>
    void ConfigureServices(IServiceCollection services);

    /// <summary>
    /// Gets the types of endpoint configurators for this slice.
    /// </summary>
    /// <remarks>
    /// Return endpoint configurator types that implement your endpoint
    /// configuration interface. These will be resolved from DI.
    /// </remarks>
    IEnumerable<Type> EndpointConfiguratorTypes { get; }
}

/// <summary>
/// Represents a slice dependency on another slice.
/// </summary>
/// <param name="SliceName">The name of the required slice.</param>
/// <param name="IsOptional">Whether this dependency is optional.</param>
public sealed record SliceDependency(string SliceName, bool IsOptional = false);

/// <summary>
/// Interface for feature slices with explicit dependencies.
/// </summary>
public interface IFeatureSliceWithDependencies
{
    /// <summary>
    /// Gets the slices this slice depends on.
    /// </summary>
    IEnumerable<SliceDependency> Dependencies { get; }
}

/// <summary>
/// Error type for feature slice failures.
/// </summary>
/// <param name="Message">Description of the failure.</param>
/// <param name="ErrorCode">Machine-readable error code.</param>
/// <param name="SliceName">The slice where the error occurred.</param>
public sealed record FeatureSliceError(
    string Message,
    string ErrorCode,
    string? SliceName = null)
{
    /// <summary>
    /// Creates an error for a missing dependency.
    /// </summary>
    public static FeatureSliceError MissingDependency(string sliceName, string dependencyName) =>
        new(
            $"Slice '{sliceName}' depends on '{dependencyName}' which is not registered",
            "SLICE_MISSING_DEPENDENCY",
            sliceName);

    /// <summary>
    /// Creates an error for circular dependencies.
    /// </summary>
    public static FeatureSliceError CircularDependency(IReadOnlyList<string> cycle) =>
        new(
            $"Circular dependency detected: {string.Join(" -> ", cycle)}",
            "SLICE_CIRCULAR_DEPENDENCY");

    /// <summary>
    /// Creates an error for registration failure.
    /// </summary>
    public static FeatureSliceError RegistrationFailed(string sliceName, Exception exception) =>
        new(
            $"Failed to register slice '{sliceName}': {exception.Message}",
            "SLICE_REGISTRATION_FAILED",
            sliceName);
}

/// <summary>
/// Configuration for feature slice registration.
/// </summary>
public sealed class FeatureSliceConfiguration
{
    private readonly List<FeatureSlice> _slices = [];
    private readonly List<Type> _sliceTypes = [];

    /// <summary>
    /// Gets the registered slices.
    /// </summary>
    public IReadOnlyList<FeatureSlice> Slices => _slices.AsReadOnly();

    /// <summary>
    /// Gets the registered slice types.
    /// </summary>
    public IReadOnlyList<Type> SliceTypes => _sliceTypes.AsReadOnly();

    /// <summary>
    /// Gets or sets whether to validate slice dependencies at registration.
    /// </summary>
    public bool ValidateDependencies { get; set; } = true;

    /// <summary>
    /// Adds a feature slice.
    /// </summary>
    /// <typeparam name="TSlice">The slice type.</typeparam>
    /// <returns>This configuration for fluent chaining.</returns>
    public FeatureSliceConfiguration AddSlice<TSlice>()
        where TSlice : FeatureSlice, new()
    {
        _slices.Add(new TSlice());
        _sliceTypes.Add(typeof(TSlice));
        return this;
    }

    /// <summary>
    /// Adds a feature slice instance.
    /// </summary>
    /// <param name="slice">The slice to add.</param>
    /// <returns>This configuration for fluent chaining.</returns>
    public FeatureSliceConfiguration AddSlice(FeatureSlice slice)
    {
        ArgumentNullException.ThrowIfNull(slice);
        _slices.Add(slice);
        _sliceTypes.Add(slice.GetType());
        return this;
    }

    /// <summary>
    /// Scans an assembly for feature slices and adds them.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>This configuration for fluent chaining.</returns>
    public FeatureSliceConfiguration AddSlicesFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var sliceTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => typeof(FeatureSlice).IsAssignableFrom(t));

        foreach (var sliceType in sliceTypes)
        {
            var slice = (FeatureSlice)Activator.CreateInstance(sliceType)!;
            _slices.Add(slice);
            _sliceTypes.Add(sliceType);
        }

        return this;
    }
}

/// <summary>
/// Extension methods for feature slice registration.
/// </summary>
public static class FeatureSliceExtensions
{
    /// <summary>
    /// Adds feature slices to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddFeatureSlices(
        this IServiceCollection services,
        Action<FeatureSliceConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var config = new FeatureSliceConfiguration();
        configure(config);

        // Validate dependencies if enabled
        if (config.ValidateDependencies)
        {
            ValidateSliceDependencies(config.Slices);
        }

        // Register each slice
        foreach (var slice in config.Slices)
        {
            slice.ConfigureServices(services);
            services.AddSingleton(slice);
            services.AddSingleton<FeatureSlice>(slice);
        }

        // Register the configuration
        services.AddSingleton(config);

        return services;
    }

    /// <summary>
    /// Adds a single feature slice.
    /// </summary>
    /// <typeparam name="TSlice">The slice type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddFeatureSlice<TSlice>(this IServiceCollection services)
        where TSlice : FeatureSlice, new()
    {
        ArgumentNullException.ThrowIfNull(services);

        var slice = new TSlice();
        slice.ConfigureServices(services);
        services.AddSingleton(slice);
        services.AddSingleton<FeatureSlice>(slice);

        return services;
    }

    /// <summary>
    /// Adds feature slices from an assembly.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddFeatureSlicesFromAssembly(
        this IServiceCollection services,
        Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        var sliceTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => typeof(FeatureSlice).IsAssignableFrom(t));

        foreach (var sliceType in sliceTypes)
        {
            var slice = (FeatureSlice)Activator.CreateInstance(sliceType)!;
            slice.ConfigureServices(services);
            services.AddSingleton(slice);
            services.AddSingleton<FeatureSlice>(slice);
        }

        return services;
    }

    /// <summary>
    /// Gets all registered feature slices.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <returns>All registered feature slices.</returns>
    public static IEnumerable<FeatureSlice> GetFeatureSlices(this IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        return serviceProvider.GetServices<FeatureSlice>();
    }

    /// <summary>
    /// Gets a feature slice by name.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="featureName">The feature name.</param>
    /// <returns>The slice if found, otherwise null.</returns>
    public static FeatureSlice? GetFeatureSlice(
        this IServiceProvider serviceProvider,
        string featureName)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentException.ThrowIfNullOrWhiteSpace(featureName);

        return serviceProvider.GetFeatureSlices()
            .FirstOrDefault(s => string.Equals(s.FeatureName, featureName, StringComparison.Ordinal));
    }

    private static void ValidateSliceDependencies(IReadOnlyList<FeatureSlice> slices)
    {
        var sliceNames = new HashSet<string>(slices.Select(s => s.FeatureName), StringComparer.Ordinal);

        foreach (var slice in slices)
        {
            if (slice is IFeatureSliceWithDependencies withDeps)
            {
                foreach (var dep in withDeps.Dependencies)
                {
                    if (!dep.IsOptional && !sliceNames.Contains(dep.SliceName))
                    {
                        throw new InvalidOperationException(
                            $"Slice '{slice.FeatureName}' depends on '{dep.SliceName}' which is not registered");
                    }
                }
            }
        }
    }
}

/// <summary>
/// Marker interface for use case handlers within a feature slice.
/// </summary>
/// <remarks>
/// <para>
/// Use case handlers implement the application logic for a specific
/// use case within a feature slice. They coordinate domain objects
/// and infrastructure services.
/// </para>
/// </remarks>
public interface IUseCaseHandler
{
}

/// <summary>
/// Use case handler with input and output.
/// </summary>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TOutput">The output type.</typeparam>
public interface IUseCaseHandler<in TInput, TOutput> : IUseCaseHandler
{
    /// <summary>
    /// Handles the use case.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing the output.</returns>
    Task<TOutput> HandleAsync(TInput input, CancellationToken cancellationToken = default);
}

/// <summary>
/// Use case handler with only input (command pattern).
/// </summary>
/// <typeparam name="TInput">The input type.</typeparam>
public interface IUseCaseHandler<in TInput> : IUseCaseHandler
{
    /// <summary>
    /// Handles the use case.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task HandleAsync(TInput input, CancellationToken cancellationToken = default);
}

/// <summary>
/// Extension methods for registering use case handlers.
/// </summary>
public static class UseCaseHandlerExtensions
{
    /// <summary>
    /// Registers all use case handlers from an assembly.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assembly">The assembly to scan.</param>
    /// <param name="lifetime">The service lifetime. Defaults to Scoped.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddUseCaseHandlersFromAssembly(
        this IServiceCollection services,
        Assembly assembly,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        var handlerInterface = typeof(IUseCaseHandler);

        var handlerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => handlerInterface.IsAssignableFrom(t));

        foreach (var handlerType in handlerTypes)
        {
            var interfaces = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType)
                .Where(i =>
                    i.GetGenericTypeDefinition() == typeof(IUseCaseHandler<,>) ||
                    i.GetGenericTypeDefinition() == typeof(IUseCaseHandler<>));

            foreach (var interfaceType in interfaces)
            {
                services.Add(new ServiceDescriptor(interfaceType, handlerType, lifetime));
            }
        }

        return services;
    }

    /// <summary>
    /// Registers a specific use case handler.
    /// </summary>
    /// <typeparam name="THandler">The handler implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">The service lifetime. Defaults to Scoped.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddUseCaseHandler<THandler>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where THandler : class, IUseCaseHandler
    {
        ArgumentNullException.ThrowIfNull(services);

        var handlerType = typeof(THandler);
        var interfaces = handlerType.GetInterfaces()
            .Where(i => i.IsGenericType)
            .Where(i =>
                i.GetGenericTypeDefinition() == typeof(IUseCaseHandler<,>) ||
                i.GetGenericTypeDefinition() == typeof(IUseCaseHandler<>));

        foreach (var interfaceType in interfaces)
        {
            services.Add(new ServiceDescriptor(interfaceType, handlerType, lifetime));
        }

        return services;
    }
}

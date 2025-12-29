using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Encina.DomainModeling;

/// <summary>
/// Marker interface for all ports.
/// A port defines a boundary between the application core and external systems.
/// </summary>
/// <remarks>
/// <para>
/// Ports are the interfaces that define how the application interacts with
/// the outside world. They are part of the Hexagonal (Ports &amp; Adapters) Architecture.
/// </para>
/// <para>
/// There are two types of ports:
/// <list type="bullet">
///   <item><description><see cref="IInboundPort"/> - Driven by external actors.</description></item>
///   <item><description><see cref="IOutboundPort"/> - Drives external systems.</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IPort { }

/// <summary>
/// Inbound port - driven by external actors (e.g., HTTP controllers, message handlers).
/// These ports define how external systems can interact with the application.
/// </summary>
/// <remarks>
/// <para>
/// Inbound ports are typically implemented as use cases or application services.
/// They are called by primary adapters (controllers, CLI, etc.).
/// </para>
/// <example>
/// <code>
/// public interface IOrderUseCasesPort : IInboundPort
/// {
///     Task&lt;Either&lt;EncinaError, OrderDto&gt;&gt; CreateOrderAsync(CreateOrderInput input, CancellationToken ct);
///     Task&lt;Either&lt;EncinaError, OrderDto&gt;&gt; GetOrderAsync(OrderId orderId, CancellationToken ct);
/// }
/// </code>
/// </example>
/// </remarks>
public interface IInboundPort : IPort { }

/// <summary>
/// Outbound port - drives external systems (e.g., repositories, external APIs).
/// These ports define how the application interacts with external systems.
/// </summary>
/// <remarks>
/// <para>
/// Outbound ports are implemented by secondary adapters (repositories, external services).
/// The application core depends on these interfaces, not the implementations.
/// </para>
/// <example>
/// <code>
/// public interface IOrderRepositoryPort : IOutboundPort
/// {
///     Task&lt;Option&lt;Order&gt;&gt; GetByIdAsync(OrderId id, CancellationToken ct);
///     Task AddAsync(Order order, CancellationToken ct);
/// }
/// </code>
/// </example>
/// </remarks>
public interface IOutboundPort : IPort { }

/// <summary>
/// Marker interface for adapters that implement ports.
/// </summary>
/// <typeparam name="TPort">The port type this adapter implements.</typeparam>
/// <remarks>
/// <para>
/// Adapters are the implementations of ports. They translate between
/// the application's internal language and external systems.
/// </para>
/// </remarks>
public interface IAdapter<TPort> where TPort : IPort { }

/// <summary>
/// Base class for adapters with common functionality like logging and error handling.
/// </summary>
/// <typeparam name="TPort">The port type this adapter implements.</typeparam>
public abstract class AdapterBase<TPort> : IAdapter<TPort> where TPort : IPort
{
    /// <summary>
    /// Gets the logger for this adapter.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AdapterBase{TPort}"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    protected AdapterBase(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        Logger = logger;
    }

    /// <summary>
    /// Wraps a synchronous operation with logging and error handling.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="operationName">The name of the operation for logging.</param>
    /// <param name="operation">The operation to execute.</param>
    /// <returns>The result of the operation or an error.</returns>
    protected LanguageExt.Either<AdapterError, T> Execute<T>(
        string operationName,
        Func<T> operation)
    {
        ArgumentNullException.ThrowIfNull(operationName);
        ArgumentNullException.ThrowIfNull(operation);

        try
        {
            Logger.LogDebug("Executing adapter operation: {Operation}", operationName);
            var result = operation();
            Logger.LogDebug("Completed adapter operation: {Operation}", operationName);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed adapter operation: {Operation}", operationName);
            return AdapterError.OperationFailed<TPort>(operationName, ex);
        }
    }

    /// <summary>
    /// Wraps an asynchronous operation with logging and error handling.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="operationName">The name of the operation for logging.</param>
    /// <param name="operation">The async operation to execute.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing the result or an error.</returns>
    protected async Task<LanguageExt.Either<AdapterError, T>> ExecuteAsync<T>(
        string operationName,
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operationName);
        ArgumentNullException.ThrowIfNull(operation);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            Logger.LogDebug("Executing async adapter operation: {Operation}", operationName);
            var result = await operation().ConfigureAwait(false);
            Logger.LogDebug("Completed async adapter operation: {Operation}", operationName);
            return result;
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Cancelled adapter operation: {Operation}", operationName);
            return AdapterError.Cancelled<TPort>(operationName);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed async adapter operation: {Operation}", operationName);
            return AdapterError.OperationFailed<TPort>(operationName, ex);
        }
    }
}

/// <summary>
/// Error type for adapter operation failures.
/// </summary>
/// <param name="Message">Description of the adapter failure.</param>
/// <param name="ErrorCode">Machine-readable error code.</param>
/// <param name="PortType">The port type associated with the failure.</param>
/// <param name="OperationName">The name of the failed operation.</param>
/// <param name="InnerException">Optional inner exception.</param>
public sealed record AdapterError(
    string Message,
    string ErrorCode,
    Type PortType,
    string? OperationName = null,
    Exception? InnerException = null)
{
    /// <summary>
    /// Creates an error for when an adapter operation fails.
    /// </summary>
    public static AdapterError OperationFailed<TPort>(
        string operationName,
        Exception exception)
        where TPort : IPort =>
        new(
            $"Adapter operation '{operationName}' failed: {exception.Message}",
            "ADAPTER_OPERATION_FAILED",
            typeof(TPort),
            operationName,
            exception);

    /// <summary>
    /// Creates an error for when an adapter operation is cancelled.
    /// </summary>
    public static AdapterError Cancelled<TPort>(string operationName)
        where TPort : IPort =>
        new(
            $"Adapter operation '{operationName}' was cancelled",
            "ADAPTER_OPERATION_CANCELLED",
            typeof(TPort),
            operationName);

    /// <summary>
    /// Creates an error for when an external resource is not found.
    /// </summary>
    public static AdapterError NotFound<TPort>(string resourceName)
        where TPort : IPort =>
        new(
            $"External resource '{resourceName}' not found",
            "ADAPTER_NOT_FOUND",
            typeof(TPort));

    /// <summary>
    /// Creates an error for when communication with external system fails.
    /// </summary>
    public static AdapterError CommunicationFailed<TPort>(
        string systemName,
        Exception? exception = null)
        where TPort : IPort =>
        new(
            $"Communication with external system '{systemName}' failed",
            "ADAPTER_COMMUNICATION_FAILED",
            typeof(TPort),
            InnerException: exception);

    /// <summary>
    /// Creates an error for when external system returns an error.
    /// </summary>
    public static AdapterError ExternalError<TPort>(
        string systemName,
        string errorMessage)
        where TPort : IPort =>
        new(
            $"External system '{systemName}' returned error: {errorMessage}",
            "ADAPTER_EXTERNAL_ERROR",
            typeof(TPort));
}

/// <summary>
/// Extension methods for registering ports and adapters.
/// </summary>
public static class PortRegistrationExtensions
{
    /// <summary>
    /// Registers an adapter for a port.
    /// </summary>
    /// <typeparam name="TPort">The port interface type.</typeparam>
    /// <typeparam name="TAdapter">The adapter implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">The service lifetime. Defaults to Scoped.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPort<TPort, TAdapter>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TPort : class, IPort
        where TAdapter : class, TPort
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Add(new ServiceDescriptor(typeof(TPort), typeof(TAdapter), lifetime));
        return services;
    }

    /// <summary>
    /// Registers a port with a factory function.
    /// </summary>
    /// <typeparam name="TPort">The port interface type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="factory">The factory function to create the adapter.</param>
    /// <param name="lifetime">The service lifetime. Defaults to Scoped.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPort<TPort>(
        this IServiceCollection services,
        Func<IServiceProvider, TPort> factory,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TPort : class, IPort
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(factory);

        services.Add(new ServiceDescriptor(typeof(TPort), factory, lifetime));
        return services;
    }

    /// <summary>
    /// Scans an assembly for all port/adapter pairs and registers them.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assembly">The assembly to scan.</param>
    /// <param name="lifetime">The service lifetime. Defaults to Scoped.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method discovers all classes implementing <see cref="IPort"/> interfaces
    /// and registers them automatically. The adapter must be a concrete class
    /// implementing exactly one port interface.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddPortsFromAssembly(
        this IServiceCollection services,
        Assembly assembly,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        var portType = typeof(IPort);

        // Find all concrete classes implementing IPort interfaces
        var adapterTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Select(t => new
            {
                AdapterType = t,
                PortInterfaces = t.GetInterfaces()
                    .Where(i => i != portType
                        && i != typeof(IInboundPort)
                        && i != typeof(IOutboundPort)
                        && portType.IsAssignableFrom(i)
                        && i.IsInterface)
                    .ToList()
            })
            .Where(x => x.PortInterfaces.Count > 0);

        foreach (var adapter in adapterTypes)
        {
            foreach (var portInterface in adapter.PortInterfaces)
            {
                services.Add(new ServiceDescriptor(
                    portInterface,
                    adapter.AdapterType,
                    lifetime));
            }
        }

        return services;
    }

    /// <summary>
    /// Scans multiple assemblies for port/adapter pairs.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <param name="lifetime">The service lifetime. Defaults to Scoped.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPortsFromAssemblies(
        this IServiceCollection services,
        IEnumerable<Assembly> assemblies,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        foreach (var assembly in assemblies)
        {
            services.AddPortsFromAssembly(assembly, lifetime);
        }

        return services;
    }
}

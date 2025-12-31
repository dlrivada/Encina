using System.Collections.Concurrent;
using System.Reflection;
using DotNet.Testcontainers.Containers;
using Xunit;

namespace Encina.Testing.Testcontainers;

/// <summary>
/// Generic container fixture for custom-configured containers.
/// </summary>
/// <typeparam name="TContainer">The type of container.</typeparam>
/// <remarks>
/// <para>
/// This fixture is used when creating containers with custom configuration
/// via the <see cref="EncinaContainers"/> factory methods that accept
/// configuration actions.
/// </para>
/// <para>
/// For pre-configured defaults, use the specific fixture classes like
/// <see cref="SqlServerContainerFixture"/> or <see cref="PostgreSqlContainerFixture"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var fixture = EncinaContainers.PostgreSql(builder => builder
///     .WithImage("postgres:15-alpine")
///     .WithDatabase("custom_db"));
/// await fixture.InitializeAsync();
/// var connectionString = fixture.ConnectionString;
/// </code>
/// </example>
public class ConfiguredContainerFixture<TContainer> : IAsyncLifetime
    where TContainer : IContainer
{
    /// <summary>
    /// Cache for GetConnectionString MethodInfo by container type to avoid reflection overhead.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, MethodInfo?> GetConnectionStringMethodCache = new();

    private readonly TContainer _container;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfiguredContainerFixture{TContainer}"/> class.
    /// </summary>
    /// <param name="container">The pre-configured container instance.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="container"/> is null.
    /// </exception>
    public ConfiguredContainerFixture(TContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);
        _container = container;
    }

    /// <summary>
    /// Gets the underlying Testcontainers container instance.
    /// </summary>
    public TContainer Container => _container;

    /// <summary>
    /// Attempts to get the connection string for the container with validation.
    /// </summary>
    /// <param name="connectionString">The connection string if successful; null otherwise.</param>
    /// <param name="errorMessage">Detailed error message if retrieval fails; null if successful.</param>
    /// <remarks>
    /// This method validates that the container is initialized and running before
    /// attempting to retrieve the connection string. It uses reflection to call the
    /// GetConnectionString method on the container, with MethodInfo cached
    /// per container type to avoid reflection overhead.
    /// Returns false if container is not running, type lacks GetConnectionString method,
    /// method returns null or non-string, or reflection invocation throws.
    /// </remarks>
    /// <returns>True if connection string retrieved successfully; false otherwise.</returns>
    public bool TryGetConnectionString(out string? connectionString, out string? errorMessage)
    {
        connectionString = null;
        errorMessage = null;

        // Validate container is initialized and running
        if (!IsRunning)
        {
            errorMessage = "Container not initialized or not running. Call await InitializeAsync() first.";
            return false;
        }

        var containerType = typeof(TContainer);
        var method = GetConnectionStringMethodCache.GetOrAdd(containerType, type =>
            type.GetMethod("GetConnectionString", Type.EmptyTypes));

        if (method is null)
        {
            errorMessage = $"Container type '{containerType.FullName}' does not expose a parameterless GetConnectionString() method.";
            return false;
        }

        try
        {
            var result = method.Invoke(_container, null);
            if (result is not string cs)
            {
                errorMessage = $"GetConnectionString() on '{containerType.FullName}' returned null or non-string value.";
                return false;
            }

            connectionString = cs;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Error invoking GetConnectionString() on '{containerType.FullName}': {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// Gets the connection string for the container.
    /// </summary>
    /// <remarks>
    /// This property uses reflection to call the GetConnectionString method
    /// on the container. The MethodInfo is cached per container type to avoid
    /// reflection overhead. For error-safe retrieval, use TryGetConnectionString
    /// instead, which validates container state and returns false on error rather than throwing.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the container is not running, lacks GetConnectionString method,
    /// method returns null, or reflection invocation fails.
    /// </exception>
    public string ConnectionString
    {
        get
        {
            if (!TryGetConnectionString(out var cs, out var error))
            {
                throw new InvalidOperationException(error ?? "Failed to retrieve connection string.");
            }

            return cs!;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the container is running.
    /// </summary>
    /// <remarks>
    /// Returns true only when the container is in the Running state.
    /// </remarks>
    public bool IsRunning => _container.State == TestcontainersStates.Running;

    /// <summary>
    /// Starts the container.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InitializeAsync()
    {
        await InitializeAsync(CancellationToken.None);
    }

    /// <summary>
    /// Starts the container with an optional cancellation token.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while starting the container.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await _container.StartAsync(cancellationToken);
    }

    /// <summary>
    /// Stops and disposes the container.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method safely disposes the container even if it was never started
    /// or had errors during startup. It will only attempt to stop the container
    /// if it is currently running, and always calls DisposeAsync regardless of
    /// the container state. Any exceptions during stop are caught and logged
    /// to prevent teardown failures from masking test failures.
    /// </para>
    /// </remarks>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DisposeAsync()
    {
        if (_container is null)
        {
            return;
        }

        try
        {
            // Only attempt to stop if the container is running to avoid exceptions
            // from containers that were never started or already stopped
            if (_container.State == TestcontainersStates.Running)
            {
                await _container.StopAsync();
            }
        }
        catch (Exception ex)
        {
            // Log with full details to ensure visibility in CI logs and Release builds.
            // We suppress the rethrow because we always want to proceed with disposal;
            // if both stop and dispose fail, we still attempt cleanup. Test teardown
            // failures are logged here and won't hide the primary test failure.
            Console.Error.WriteLine($"Error stopping container: {ex.GetType().Name}");
            Console.Error.WriteLine(ex.ToString());
        }

        // Always dispose, even if stop failed
        try
        {
            await _container.DisposeAsync();
        }
        catch (Exception ex)
        {
            // Log with full details to ensure visibility in CI logs and Release builds.
            // We suppress the rethrow because we've already attempted stop above.
            // Suppression is safe here: disposal errors don't affect test results,
            // and we want the container cleanup to complete even if disposal fails.
            Console.Error.WriteLine($"Error disposing container: {ex.GetType().Name}");
            Console.Error.WriteLine(ex.ToString());
        }
    }
}

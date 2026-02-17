using DotNet.Testcontainers.Containers;
using Xunit;

namespace Encina.Testing.Testcontainers;

/// <summary>
/// Base class for container fixtures that manages the lifecycle of containers.
/// </summary>
/// <typeparam name="TContainer">The type of container.</typeparam>
/// <remarks>
/// <para>
/// This abstract base class provides common functionality for all container fixtures:
/// <list type="bullet">
///   <item><description>Container lifecycle management (start, stop, dispose)</description></item>
///   <item><description>State tracking via <see cref="IsRunning"/></description></item>
///   <item><description>Connection string access</description></item>
///   <item><description>Cancellation token support for startup operations</description></item>
/// </list>
/// </para>
/// <para>
/// Derived classes must override <see cref="BuildContainer"/> to configure and build
/// the specific container type with appropriate settings (image, environment, etc.).
/// </para>
/// </remarks>
public abstract class ContainerFixtureBase<TContainer> : IAsyncLifetime
    where TContainer : IContainer
{
    /// <summary>
    /// The underlying container instance.
    /// </summary>
    private TContainer? _container;

    /// <summary>
    /// Gets the underlying container instance (for derived classes).
    /// </summary>
    protected TContainer? ContainerInstance => _container;

    /// <summary>
    /// Gets the underlying Testcontainers container instance.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when accessed before <see cref="InitializeAsync()"/> is called.
    /// </exception>
    public TContainer Container => _container
        ?? throw new InvalidOperationException("Container not initialized. Call InitializeAsync first.");

    /// <summary>
    /// Gets the connection string for the container.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property assumes the container type has a <c>GetConnectionString()</c> method.
    /// If your container type does not expose this method, override this property in the derived class.
    /// </para>
    /// <para>
    /// The default implementation uses reflection to call GetConnectionString() dynamically
    /// to avoid requiring all container types to implement an interface.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when accessed before <see cref="InitializeAsync()"/> is called, or when the
    /// container does not expose a GetConnectionString method.
    /// </exception>
    public virtual string ConnectionString
    {
        get
        {
            var container = Container; // Ensure it's initialized
            var method = typeof(TContainer).GetMethod("GetConnectionString", System.Type.EmptyTypes);
            if (method is null)
            {
                throw new InvalidOperationException(
                    $"Container type '{typeof(TContainer).FullName}' does not expose a GetConnectionString() method. "
                    + "Override the ConnectionString property in the derived class to provide an alternative.");
            }

            var result = method.Invoke(container, null);
            if (result is not string connectionString)
            {
                throw new InvalidOperationException(
                    $"GetConnectionString() on '{typeof(TContainer).FullName}' returned null or non-string value.");
            }

            return connectionString;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the container is running.
    /// </summary>
    /// <remarks>
    /// Returns true only when the container exists and is in the Running state.
    /// </remarks>
    public bool IsRunning => _container is not null && _container.State == TestcontainersStates.Running;

    /// <summary>
    /// Builds and configures the container.
    /// </summary>
    /// <remarks>
    /// This method is called during initialization to create the container instance
    /// with type-specific configuration. Derived classes must override this method to
    /// return a properly configured container.
    /// </remarks>
    /// <returns>A configured container instance ready to start.</returns>
    protected abstract TContainer BuildContainer();

    /// <summary>
    /// Initializes and starts the container.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async ValueTask InitializeAsync()
    {
        await InitializeAsync(CancellationToken.None);
    }

    /// <summary>
    /// Initializes and starts the container with an optional cancellation token.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while starting the container.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        _container = BuildContainer();
        await _container.StartAsync(cancellationToken);
    }

    /// <summary>
    /// Stops and disposes the container.
    /// </summary>
    /// <remarks>
    /// This method safely disposes the container even if it was never started
    /// or had errors during startup. It will only attempt to stop the container
    /// if it is currently running, and always calls DisposeAsync regardless of
    /// the container state. Any exceptions during stop are caught and logged
    /// to prevent teardown failures from masking test failures.
    /// </remarks>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

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

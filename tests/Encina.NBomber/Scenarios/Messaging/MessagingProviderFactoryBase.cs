using Encina.InMemory;

namespace Encina.NBomber.Scenarios.Messaging;

/// <summary>
/// Base class for messaging provider factories providing common functionality.
/// </summary>
public abstract class MessagingProviderFactoryBase : IMessagingProviderFactory
{
    private bool _initialized;
    private bool _disposed;

    /// <inheritdoc />
    public abstract string ProviderName { get; }

    /// <inheritdoc />
    public abstract MessagingProviderCategory Category { get; }

    /// <inheritdoc />
    public MessagingProviderOptions Options { get; } = new();

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            return;
        }

        await InitializeCoreAsync(cancellationToken).ConfigureAwait(false);
        _initialized = true;
    }

    /// <summary>
    /// Core initialization logic to be implemented by derived classes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected abstract Task InitializeCoreAsync(CancellationToken cancellationToken);

    /// <inheritdoc />
    public abstract IInMemoryMessageBus? CreateMessageBus();

    /// <inheritdoc />
    public abstract IServiceProvider CreateServiceProvider();

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await DisposeCoreAsync().ConfigureAwait(false);
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Core disposal logic to be implemented by derived classes.
    /// </summary>
    protected abstract ValueTask DisposeCoreAsync();

    /// <summary>
    /// Throws if not initialized.
    /// </summary>
    protected void EnsureInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException($"Provider '{ProviderName}' has not been initialized. Call InitializeAsync() first.");
        }
    }
}

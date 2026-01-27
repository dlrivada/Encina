using Encina.Caching;

namespace Encina.NBomber.Scenarios.Caching;

/// <summary>
/// Base class for cache provider factories providing common initialization and disposal logic.
/// </summary>
public abstract class CacheProviderFactoryBase : ICacheProviderFactory
{
    private bool _isInitialized;
    private bool _isDisposed;

    /// <inheritdoc/>
    public abstract string ProviderName { get; }

    /// <inheritdoc/>
    public abstract CacheProviderCategory Category { get; }

    /// <inheritdoc/>
    public CacheProviderOptions Options { get; }

    /// <inheritdoc/>
    public virtual bool IsAvailable => _isInitialized && !_isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheProviderFactoryBase"/> class.
    /// </summary>
    /// <param name="configureOptions">Optional delegate to configure options.</param>
    protected CacheProviderFactoryBase(Action<CacheProviderOptions>? configureOptions = null)
    {
        Options = new CacheProviderOptions();
        configureOptions?.Invoke(Options);
    }

    /// <inheritdoc/>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
        {
            return;
        }

        await InitializeCoreAsync(cancellationToken).ConfigureAwait(false);
        _isInitialized = true;
    }

    /// <summary>
    /// When overridden in a derived class, performs provider-specific initialization.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected abstract Task InitializeCoreAsync(CancellationToken cancellationToken);

    /// <inheritdoc/>
    public abstract ICacheProvider CreateCacheProvider();

    /// <summary>
    /// Ensures the factory has been initialized before use.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the factory has not been initialized.</exception>
    protected void EnsureInitialized()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException(
                $"The {ProviderName} provider factory has not been initialized. Call InitializeAsync first.");
        }

        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        await DisposeCoreAsync().ConfigureAwait(false);
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// When overridden in a derived class, performs provider-specific cleanup.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual Task DisposeCoreAsync() => Task.CompletedTask;
}

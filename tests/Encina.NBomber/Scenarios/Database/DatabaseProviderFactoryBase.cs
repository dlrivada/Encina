using System.Data;
using Encina.Tenancy;

namespace Encina.NBomber.Scenarios.Database;

/// <summary>
/// Base class for database provider factories providing common functionality.
/// </summary>
public abstract class DatabaseProviderFactoryBase : IDatabaseProviderFactory
{
    private bool _initialized;
    private bool _disposed;

    /// <inheritdoc />
    public abstract string ProviderName { get; }

    /// <inheritdoc />
    public abstract ProviderCategory Category { get; }

    /// <inheritdoc />
    public abstract DatabaseType DatabaseType { get; }

    /// <inheritdoc />
    public virtual bool SupportsReadWriteSeparation => DatabaseType != DatabaseType.MongoDB;

    /// <inheritdoc />
    public abstract string ConnectionString { get; }

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
    public abstract IDbConnection CreateConnection();

    /// <inheritdoc />
    public abstract object? CreateUnitOfWork();

    /// <inheritdoc />
    public abstract ITenantProvider CreateTenantProvider();

    /// <inheritdoc />
    public abstract object? CreateReadWriteSelector();

    /// <inheritdoc />
    public abstract Task ClearDataAsync(CancellationToken cancellationToken = default);

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

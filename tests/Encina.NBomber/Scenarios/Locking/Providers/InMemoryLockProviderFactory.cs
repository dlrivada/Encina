using Encina.DistributedLock;
using Encina.DistributedLock.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Encina.NBomber.Scenarios.Locking.Providers;

/// <summary>
/// Factory for creating in-memory lock providers for load testing.
/// Useful for baseline performance comparison.
/// </summary>
public sealed class InMemoryLockProviderFactory : LockProviderFactoryBase
{
    private ServiceProvider? _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryLockProviderFactory"/> class.
    /// </summary>
    /// <param name="configureOptions">Optional delegate to configure options.</param>
    public InMemoryLockProviderFactory(Action<LockProviderOptions>? configureOptions = null)
        : base(configureOptions)
    {
    }

    /// <inheritdoc/>
    public override string ProviderName => "inmemory";

    /// <inheritdoc/>
    public override LockProviderCategory Category => LockProviderCategory.InMemory;

    /// <inheritdoc/>
    protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        services.Configure<InMemoryLockOptions>(opt =>
        {
            opt.KeyPrefix = Options.KeyPrefix;
            opt.DefaultExpiry = Options.DefaultExpiry;
            opt.DefaultWait = Options.DefaultWaitTimeout;
            opt.DefaultRetry = Options.DefaultRetryInterval;
            opt.WarnOnUse = false; // Suppress warnings for load testing
        });
        services.AddSingleton<IDistributedLockProvider, InMemoryDistributedLockProvider>();

        _serviceProvider = services.BuildServiceProvider();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override IDistributedLockProvider CreateLockProvider()
    {
        EnsureInitialized();

        if (_serviceProvider is null)
        {
            throw new InvalidOperationException("Service provider is not available.");
        }

        return _serviceProvider.GetRequiredService<IDistributedLockProvider>();
    }

    /// <inheritdoc/>
    protected override async Task DisposeCoreAsync()
    {
        if (_serviceProvider is not null)
        {
            await _serviceProvider.DisposeAsync().ConfigureAwait(false);
        }
    }
}

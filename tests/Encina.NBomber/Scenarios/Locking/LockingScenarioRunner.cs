using Encina.NBomber.Scenarios.Locking.Providers;
using NBomber.Contracts;

namespace Encina.NBomber.Scenarios.Locking;

/// <summary>
/// Runner for distributed locking load test scenarios.
/// Creates and executes InMemory, Redis, and SQL Server lock scenarios.
/// </summary>
public sealed class LockingScenarioRunner : IAsyncDisposable
{
    private readonly LockingFeature _feature;
    private readonly string _providerName;
    private ILockProviderFactory? _providerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="LockingScenarioRunner"/> class.
    /// </summary>
    /// <param name="feature">The locking feature to test.</param>
    /// <param name="providerName">Optional provider name (for Redis variants).</param>
    public LockingScenarioRunner(LockingFeature feature, string? providerName = null)
    {
        _feature = feature;
        _providerName = providerName ?? GetDefaultProviderName(feature);
    }

    /// <summary>
    /// Creates scenarios for the specified locking feature.
    /// </summary>
    /// <returns>A collection of scenario props to run.</returns>
    public async Task<ScenarioProps[]> CreateScenariosAsync()
    {
        var scenarios = new List<ScenarioProps>();

        _providerFactory = LockProviderRegistry.CreateFactory(_providerName, options =>
        {
            options.DefaultExpiry = TimeSpan.FromSeconds(30);
            options.DefaultWaitTimeout = TimeSpan.FromSeconds(10);
            options.DefaultRetryInterval = TimeSpan.FromMilliseconds(100);
            options.ContentionBuckets = 10;
            options.KeyPrefix = "nbomber:lock";
        });

        await _providerFactory.InitializeAsync().ConfigureAwait(false);

        if (!_providerFactory.IsAvailable)
        {
            Console.WriteLine($"Lock provider '{_providerName}' is not available (Docker may be required).");
            return [];
        }

        var context = new LockScenarioContext(_providerFactory, _providerName);

        if (_feature is LockingFeature.InMemory or LockingFeature.All)
        {
            if (_providerFactory.Category == LockProviderCategory.InMemory)
            {
                var inMemoryFactory = new InMemoryLockScenarioFactory(context);
                scenarios.AddRange(inMemoryFactory.CreateScenarios());
            }
        }

        if (_feature is LockingFeature.Redis or LockingFeature.All)
        {
            if (_providerFactory.Category == LockProviderCategory.Redis)
            {
                var redisFactory = new RedisLockScenarioFactory(context);
                scenarios.AddRange(redisFactory.CreateScenarios());
            }
        }

        if (_feature is LockingFeature.SqlServer or LockingFeature.All)
        {
            if (_providerFactory.Category == LockProviderCategory.SqlServer)
            {
                var sqlServerFactory = new SqlServerLockScenarioFactory(context);
                scenarios.AddRange(sqlServerFactory.CreateScenarios());
            }
        }

        return scenarios.ToArray();
    }

    /// <summary>
    /// Creates scenarios for all available providers.
    /// </summary>
    /// <returns>A collection of scenario props to run.</returns>
    public static async Task<ScenarioProps[]> CreateAllProvidersAsync()
    {
        var allScenarios = new List<ScenarioProps>();

        // InMemory scenarios
        var inMemoryRunner = new LockingScenarioRunner(LockingFeature.InMemory, LockProviderRegistry.ProviderNames.InMemory);
        allScenarios.AddRange(await inMemoryRunner.CreateScenariosAsync().ConfigureAwait(false));
        await inMemoryRunner.DisposeAsync().ConfigureAwait(false);

        // Redis scenarios
        var redisRunner = new LockingScenarioRunner(LockingFeature.Redis, LockProviderRegistry.ProviderNames.Redis);
        allScenarios.AddRange(await redisRunner.CreateScenariosAsync().ConfigureAwait(false));
        await redisRunner.DisposeAsync().ConfigureAwait(false);

        // SQL Server scenarios
        var sqlServerRunner = new LockingScenarioRunner(LockingFeature.SqlServer, LockProviderRegistry.ProviderNames.SqlServer);
        allScenarios.AddRange(await sqlServerRunner.CreateScenariosAsync().ConfigureAwait(false));
        await sqlServerRunner.DisposeAsync().ConfigureAwait(false);

        return allScenarios.ToArray();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_providerFactory is not null)
        {
            await _providerFactory.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static string GetDefaultProviderName(LockingFeature feature)
    {
        return feature switch
        {
            LockingFeature.InMemory => LockProviderRegistry.ProviderNames.InMemory,
            LockingFeature.Redis => LockProviderRegistry.ProviderNames.Redis,
            LockingFeature.SqlServer => LockProviderRegistry.ProviderNames.SqlServer,
            _ => LockProviderRegistry.ProviderNames.InMemory
        };
    }
}

/// <summary>
/// Feature categories for distributed locking load testing.
/// </summary>
public enum LockingFeature
{
    /// <summary>In-memory locking (single process baseline).</summary>
    InMemory,

    /// <summary>Redis-based distributed locking.</summary>
    Redis,

    /// <summary>SQL Server-based distributed locking (sp_getapplock).</summary>
    SqlServer,

    /// <summary>All locking features.</summary>
    All
}

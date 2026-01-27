using Encina.DistributedLock;
using Encina.DistributedLock.SqlServer;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace Encina.NBomber.Scenarios.Locking.Providers;

/// <summary>
/// Factory for creating SQL Server-based lock providers for load testing.
/// Uses sp_getapplock for distributed locking.
/// </summary>
public sealed class SqlServerLockProviderFactory : LockProviderFactoryBase
{
    private MsSqlContainer? _container;
    private string? _connectionString;
    private ServiceProvider? _serviceProvider;
    private bool _containerInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerLockProviderFactory"/> class.
    /// </summary>
    /// <param name="configureOptions">Optional delegate to configure options.</param>
    public SqlServerLockProviderFactory(Action<LockProviderOptions>? configureOptions = null)
        : base(configureOptions)
    {
    }

    /// <inheritdoc/>
    public override string ProviderName => "sqlserver";

    /// <inheritdoc/>
    public override LockProviderCategory Category => LockProviderCategory.SqlServer;

    /// <inheritdoc/>
    public override bool IsAvailable => base.IsAvailable && _containerInitialized;

    /// <summary>
    /// Gets the SQL Server connection string.
    /// </summary>
    public string? ConnectionString => _connectionString;

    /// <summary>
    /// Creates a new SQL connection for direct operations.
    /// </summary>
    /// <returns>A new SQL connection.</returns>
    public SqlConnection? CreateConnection()
    {
        return _connectionString is not null ? new SqlConnection(_connectionString) : null;
    }

    /// <inheritdoc/>
    protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!string.IsNullOrEmpty(Options.SqlServerConnectionString))
            {
                _connectionString = Options.SqlServerConnectionString;
            }
            else
            {
                _container = new MsSqlBuilder(Options.SqlServerImage)
                    .WithCleanUp(true)
                    .Build();

                await _container.StartAsync(cancellationToken).ConfigureAwait(false);
                _connectionString = _container.GetConnectionString();
            }

            // Build service provider with lock provider
            var services = new ServiceCollection();

            services.AddLogging();
            services.Configure<SqlServerLockOptions>(opt =>
            {
                opt.ConnectionString = _connectionString;
                opt.KeyPrefix = Options.KeyPrefix;
                opt.DefaultExpiry = Options.DefaultExpiry;
                opt.DefaultWait = Options.DefaultWaitTimeout;
                opt.DefaultRetry = Options.DefaultRetryInterval;
            });
            services.AddSingleton<IDistributedLockProvider, SqlServerDistributedLockProvider>();

            _serviceProvider = services.BuildServiceProvider();
            _containerInitialized = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize SQL Server lock provider: {ex.Message}");
            _containerInitialized = false;
        }
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

        if (_container is not null)
        {
            await _container.StopAsync().ConfigureAwait(false);
            await _container.DisposeAsync().ConfigureAwait(false);
        }
    }
}

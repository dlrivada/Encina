using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using Testcontainers.Nats;

namespace Encina.NBomber.Scenarios.Brokers.Providers;

/// <summary>
/// Factory for creating NATS broker providers for load testing.
/// </summary>
public sealed class NATSProviderFactory : BrokerProviderFactoryBase
{
    private NatsContainer? _container;
    private NatsConnection? _connection;
    private string? _connectionString;
    private bool _containerInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="NATSProviderFactory"/> class.
    /// </summary>
    /// <param name="configureOptions">Optional delegate to configure options.</param>
    public NATSProviderFactory(Action<BrokerProviderOptions>? configureOptions = null)
        : base(configureOptions)
    {
    }

    /// <inheritdoc/>
    public override string ProviderName => "nats";

    /// <inheritdoc/>
    public override BrokerProviderCategory Category => BrokerProviderCategory.NATS;

    /// <inheritdoc/>
    public override bool IsAvailable => base.IsAvailable && _containerInitialized;

    /// <summary>
    /// Gets the NATS connection.
    /// </summary>
    public NatsConnection? Connection => _connection;

    /// <summary>
    /// Gets the NATS connection string.
    /// </summary>
    public string? ConnectionString => _connectionString;

    /// <summary>
    /// Creates a new NATS connection.
    /// </summary>
    /// <returns>A new NATS connection.</returns>
    public async Task<NatsConnection> CreateConnectionAsync()
    {
        EnsureInitialized();
        var opts = new NatsOpts { Url = _connectionString! };
        var connection = new NatsConnection(opts);
        await connection.ConnectAsync().ConfigureAwait(false);
        return connection;
    }

    /// <summary>
    /// Creates a JetStream context.
    /// </summary>
    /// <returns>A JetStream context, or null if not initialized.</returns>
    public INatsJSContext? CreateJetStreamContext()
    {
        EnsureInitialized();
        return _connection is not null ? new NatsJSContext(_connection) : null;
    }

    /// <summary>
    /// Creates a JetStream stream for testing.
    /// </summary>
    /// <param name="streamName">The stream name.</param>
    /// <param name="subjects">The subjects to capture.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CreateStreamAsync(string streamName, params string[] subjects)
    {
        EnsureInitialized();
        if (_connection is null) return;

        var js = new NatsJSContext(_connection);
        var config = new StreamConfig(streamName, subjects)
        {
            Storage = StreamConfigStorage.Memory,
            Retention = StreamConfigRetention.Limits,
            MaxMsgs = 100000
        };

        try
        {
            await js.CreateStreamAsync(config).ConfigureAwait(false);
        }
        catch (NatsJSApiException ex) when (ex.Error.Code == 400)
        {
            // Stream already exists, ignore
        }
    }

    /// <inheritdoc/>
    protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!string.IsNullOrEmpty(Options.NATSConnectionString))
            {
                _connectionString = Options.NATSConnectionString;
            }
            else
            {
                _container = new NatsBuilder(Options.NATSImage)
                    .WithCommand("--jetstream")
                    .WithCleanUp(true)
                    .Build();

                await _container.StartAsync(cancellationToken).ConfigureAwait(false);
                _connectionString = _container.GetConnectionString();
            }

            var opts = new NatsOpts { Url = _connectionString };
            _connection = new NatsConnection(opts);
            await _connection.ConnectAsync().ConfigureAwait(false);
            _containerInitialized = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize NATS: {ex.Message}");
            _containerInitialized = false;
        }
    }

    /// <inheritdoc/>
    protected override async Task DisposeCoreAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
        }

        if (_container is not null)
        {
            await _container.StopAsync().ConfigureAwait(false);
            await _container.DisposeAsync().ConfigureAwait(false);
        }
    }
}

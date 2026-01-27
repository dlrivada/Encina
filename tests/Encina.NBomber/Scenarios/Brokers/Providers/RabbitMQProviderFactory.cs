using RabbitMQ.Client;
using Testcontainers.RabbitMq;

namespace Encina.NBomber.Scenarios.Brokers.Providers;

/// <summary>
/// Factory for creating RabbitMQ broker providers for load testing.
/// </summary>
public sealed class RabbitMQProviderFactory : BrokerProviderFactoryBase
{
    private RabbitMqContainer? _container;
    private ConnectionFactory? _connectionFactory;
    private IConnection? _connection;
    private bool _containerInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMQProviderFactory"/> class.
    /// </summary>
    /// <param name="configureOptions">Optional delegate to configure options.</param>
    public RabbitMQProviderFactory(Action<BrokerProviderOptions>? configureOptions = null)
        : base(configureOptions)
    {
    }

    /// <inheritdoc/>
    public override string ProviderName => "rabbitmq";

    /// <inheritdoc/>
    public override BrokerProviderCategory Category => BrokerProviderCategory.RabbitMQ;

    /// <inheritdoc/>
    public override bool IsAvailable => base.IsAvailable && _containerInitialized;

    /// <summary>
    /// Gets the RabbitMQ connection factory.
    /// </summary>
    public ConnectionFactory? ConnectionFactory => _connectionFactory;

    /// <summary>
    /// Gets or creates a shared connection.
    /// </summary>
    public IConnection? Connection => _connection;

    /// <summary>
    /// Creates a new RabbitMQ channel.
    /// </summary>
    /// <returns>A new channel, or null if not initialized.</returns>
    public IChannel? CreateChannel()
    {
        EnsureInitialized();
        return _connection?.CreateChannelAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Creates a new RabbitMQ channel asynchronously.
    /// </summary>
    /// <returns>A new channel, or null if not initialized.</returns>
    public async Task<IChannel?> CreateChannelAsync()
    {
        EnsureInitialized();
        return _connection is not null
            ? await _connection.CreateChannelAsync().ConfigureAwait(false)
            : null;
    }

    /// <inheritdoc/>
    protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!string.IsNullOrEmpty(Options.RabbitMQConnectionString))
            {
                _connectionFactory = new ConnectionFactory
                {
                    Uri = new Uri(Options.RabbitMQConnectionString)
                };
            }
            else
            {
                _container = new RabbitMqBuilder(Options.RabbitMQImage)
                    .WithCleanUp(true)
                    .Build();

                await _container.StartAsync(cancellationToken).ConfigureAwait(false);

                _connectionFactory = new ConnectionFactory
                {
                    Uri = new Uri(_container.GetConnectionString())
                };
            }

            _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
            _containerInitialized = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize RabbitMQ: {ex.Message}");
            _containerInitialized = false;
        }
    }

    /// <inheritdoc/>
    protected override async Task DisposeCoreAsync()
    {
        if (_connection is not null)
        {
            await _connection.CloseAsync().ConfigureAwait(false);
            _connection.Dispose();
        }

        if (_container is not null)
        {
            await _container.StopAsync().ConfigureAwait(false);
            await _container.DisposeAsync().ConfigureAwait(false);
        }
    }
}

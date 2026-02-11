using NATS.Client.Core;
using Testcontainers.Nats;
using Xunit;

namespace Encina.TestInfrastructure.Fixtures;

/// <summary>
/// NATS fixture using Testcontainers.
/// Provides a throwaway NATS instance for integration tests.
/// </summary>
public sealed class NatsFixture : IAsyncLifetime
{
    private NatsContainer? _container;

    /// <summary>
    /// Gets the NATS connection.
    /// </summary>
    public INatsConnection? Connection { get; private set; }

    /// <summary>
    /// Gets the connection string for the NATS container.
    /// </summary>
    public string ConnectionString => _container?.GetConnectionString() ?? string.Empty;

    /// <summary>
    /// Gets a value indicating whether the NATS container is available.
    /// </summary>
    public bool IsAvailable => _container is not null && Connection is not null;

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        _container = new NatsBuilder()
            .WithImage("nats:2-alpine")
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();

        var options = new NatsOpts
        {
            Url = ConnectionString
        };
        Connection = new NatsConnection(options);
        await Connection.ConnectAsync();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (Connection is not null)
        {
            await Connection.DisposeAsync();
        }

        if (_container is not null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }
}

/// <summary>
/// Collection definition for NATS integration tests.
/// </summary>
[CollectionDefinition(Name)]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public class NatsCollection : ICollectionFixture<NatsFixture>
#pragma warning restore CA1711
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public const string Name = "NATS";
}

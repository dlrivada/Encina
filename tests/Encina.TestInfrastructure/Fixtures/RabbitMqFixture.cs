using RabbitMQ.Client;
using Testcontainers.RabbitMq;
using Xunit;

namespace Encina.TestInfrastructure.Fixtures;

/// <summary>
/// RabbitMQ fixture using Testcontainers.
/// Provides a throwaway RabbitMQ instance for integration tests.
/// </summary>
public sealed class RabbitMqFixture : IAsyncLifetime
{
    private RabbitMqContainer? _container;

    /// <summary>
    /// Gets the RabbitMQ connection factory.
    /// </summary>
    public ConnectionFactory? ConnectionFactory { get; private set; }

    /// <summary>
    /// Gets the connection string for the RabbitMQ container.
    /// </summary>
    public string ConnectionString => _container?.GetConnectionString() ?? string.Empty;

    /// <summary>
    /// Gets a value indicating whether the RabbitMQ container is available.
    /// </summary>
    public bool IsAvailable => _container is not null && ConnectionFactory is not null;

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        _container = new RabbitMqBuilder()
            .WithImage("rabbitmq:3-management-alpine")
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();

        ConnectionFactory = new ConnectionFactory
        {
            Uri = new Uri(ConnectionString)
        };
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }
}

/// <summary>
/// Collection definition for RabbitMQ integration tests.
/// </summary>
[CollectionDefinition(Name)]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public class RabbitMqCollection : ICollectionFixture<RabbitMqFixture>
#pragma warning restore CA1711
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public const string Name = "RabbitMQ";
}

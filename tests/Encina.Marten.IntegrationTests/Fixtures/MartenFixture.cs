using Marten;
using Testcontainers.PostgreSql;
using Xunit;

namespace Encina.Marten.IntegrationTests.Fixtures;

/// <summary>
/// Marten/PostgreSQL fixture using Testcontainers.
/// Provides a throwaway PostgreSQL instance with Marten for integration tests.
/// </summary>
public sealed class MartenFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    /// <summary>
    /// Gets the Marten document store.
    /// </summary>
    public IDocumentStore? Store { get; private set; }

    /// <summary>
    /// Gets the connection string for the PostgreSQL container.
    /// </summary>
    public string ConnectionString => _container?.GetConnectionString() ?? string.Empty;

    /// <summary>
    /// Gets a value indicating whether the fixture is available.
    /// </summary>
    public bool IsAvailable => _container is not null && Store is not null;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:17-alpine")
                .WithDatabase("encina_test")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .WithCleanUp(true)
                .Build();

            await _container.StartAsync();

            Store = DocumentStore.For(ConnectionString);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start PostgreSQL container: {ex.Message}");
            // Container might not be available in CI without Docker
        }
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        if (Store is not null)
        {
            Store.Dispose();
        }

        if (_container is not null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }
}

/// <summary>
/// Collection definition for Marten integration tests.
/// </summary>
[CollectionDefinition(Name)]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public class MartenCollection : ICollectionFixture<MartenFixture>
#pragma warning restore CA1711
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public const string Name = "Marten";
}

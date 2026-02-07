using Encina.IntegrationTests.Infrastructure.Marten.Snapshots;
using Encina.Marten.Snapshots;
using Marten;
using Testcontainers.PostgreSql;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.Marten.Fixtures;

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
        _container = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("encina_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();

        Store = DocumentStore.For(opts =>
        {
            opts.Connection(ConnectionString);

            // Enable event metadata tracking for correlation/causation IDs and headers
            opts.Events.MetadataConfig.CorrelationIdEnabled = true;
            opts.Events.MetadataConfig.CausationIdEnabled = true;
            opts.Events.MetadataConfig.HeadersEnabled = true;
        });

        // Initialize schema by storing and deleting a test document
        // This ensures the table exists before running tests that query empty tables
        await InitializeSnapshotSchemaAsync();
    }

    /// <summary>
    /// Initializes the schema for snapshot documents by storing and deleting a test document.
    /// This ensures the table exists before running tests that query empty tables.
    /// </summary>
    private async Task InitializeSnapshotSchemaAsync()
    {
        if (Store is null)
        {
            return;
        }

        await using var session = Store.LightweightSession();

        // Create a temporary envelope to trigger schema creation
        var tempEnvelope = new SnapshotEnvelope<TestSnapshotableAggregate>
        {
            Id = "init:schema:0",
            AggregateId = Guid.Empty,
            Version = 0,
            State = null,
            CreatedAtUtc = DateTime.UtcNow,
            AggregateType = typeof(TestSnapshotableAggregate).FullName ?? typeof(TestSnapshotableAggregate).Name
        };

        session.Store(tempEnvelope);
        await session.SaveChangesAsync();

        // Delete the temporary document
        session.Delete(tempEnvelope);
        await session.SaveChangesAsync();
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

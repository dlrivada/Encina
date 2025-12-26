using System.Data;
using Testcontainers.MsSql;
using Testcontainers.MySql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Encina.TestInfrastructure.Fixtures;

/// <summary>
/// Abstract base class for database fixtures using Testcontainers.
/// Implements IAsyncLifetime for xUnit integration.
/// </summary>
/// <typeparam name="TContainer">The Testcontainers container type.</typeparam>
public abstract class DatabaseFixture<TContainer> : IAsyncLifetime
    where TContainer : class
{
    /// <summary>
    /// Gets or sets the Testcontainers container instance.
    /// </summary>
    protected TContainer? Container { get; set; }

    /// <summary>
    /// Gets a value indicating whether the database container is available.
    /// </summary>
    public bool IsAvailable => Container is not null && !string.IsNullOrEmpty(ConnectionString);

    /// <summary>
    /// Gets the connection string for the test database.
    /// </summary>
    public abstract string ConnectionString { get; }

    /// <summary>
    /// Gets the database provider name (e.g., "SqlServer", "PostgreSQL").
    /// </summary>
    public abstract string ProviderName { get; }

    /// <summary>
    /// Creates and configures the Testcontainers container.
    /// </summary>
    protected abstract Task<TContainer> CreateContainerAsync();

    /// <summary>
    /// Creates the database schema (Outbox, Inbox, Sagas, Scheduling).
    /// </summary>
    protected abstract Task CreateSchemaAsync(IDbConnection connection);

    /// <summary>
    /// Drops the database schema (cleanup).
    /// </summary>
    protected abstract Task DropSchemaAsync(IDbConnection connection);

    /// <summary>
    /// Creates a new database connection for the test.
    /// </summary>
    public abstract IDbConnection CreateConnection();

    /// <summary>
    /// xUnit lifecycle: Initialize the container and schema.
    /// </summary>
    public virtual async Task InitializeAsync()
    {
        Container = await CreateContainerAsync();

        // Start container (if applicable - SQLite doesn't need this)
        if (Container is MsSqlContainer sqlServer)
        {
            await sqlServer.StartAsync();
        }
        else if (Container is PostgreSqlContainer postgres)
        {
            await postgres.StartAsync();
        }
        else if (Container is MySqlContainer mysql)
        {
            await mysql.StartAsync();
        }

        // Create schema
        using var connection = CreateConnection();
        await CreateSchemaAsync(connection);
    }

    /// <summary>
    /// xUnit lifecycle: Cleanup schema and stop container.
    /// </summary>
    public virtual async Task DisposeAsync()
    {
        // Drop schema
        try
        {
            using var connection = CreateConnection();
            await DropSchemaAsync(connection);
        }
        catch
        {
            // Best effort cleanup
        }

        // Stop and dispose container
        if (Container is MsSqlContainer sqlServer)
        {
            await sqlServer.StopAsync();
            await sqlServer.DisposeAsync();
        }
        else if (Container is PostgreSqlContainer postgres)
        {
            await postgres.StopAsync();
            await postgres.DisposeAsync();
        }
        else if (Container is MySqlContainer mysql)
        {
            await mysql.StopAsync();
            await mysql.DisposeAsync();
        }
        else if (Container is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }
    }
}

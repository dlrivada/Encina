using System.Data;
using Encina.TestInfrastructure.Fixtures;
using Encina.TestInfrastructure.Schemas;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.Repository;

/// <summary>
/// Fixture for repository integration tests.
/// Creates the test schema and provides DbContext instances.
/// </summary>
public sealed class RepositoryFixture : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlServerFixture = new();

    public string ConnectionString => _sqlServerFixture.ConnectionString;

    public RepositoryTestDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<RepositoryTestDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new RepositoryTestDbContext(options);
    }

    public async ValueTask InitializeAsync()
    {
        await _sqlServerFixture.InitializeAsync();

        // Create the repository test schema
        using var connection = _sqlServerFixture.CreateConnection();
        if (connection is SqlConnection sqlConnection)
        {
            await SqlServerSchema.CreateRepositoryTestSchemaAsync(sqlConnection);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _sqlServerFixture.DisposeAsync();
    }

    /// <summary>
    /// Clears all data from the TestEntities table.
    /// </summary>
    public async Task ClearDataAsync()
    {
        using var connection = _sqlServerFixture.CreateConnection();
        if (connection is SqlConnection sqlConnection)
        {
            await using var command = new SqlCommand("DELETE FROM TestEntities", sqlConnection);
            await command.ExecuteNonQueryAsync();
        }
    }
}

/// <summary>
/// Collection definition for repository integration tests.
/// </summary>
[CollectionDefinition("RepositoryTests")]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "xUnit convention requires 'Collection' suffix for collection definitions")]
public class RepositoryTestsCollection : ICollectionFixture<RepositoryFixture>
{
}

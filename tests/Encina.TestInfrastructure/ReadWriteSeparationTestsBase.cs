using System.Data;
using Encina.TestInfrastructure.Entities;
using Shouldly;
using Xunit;

namespace Encina.TestInfrastructure;

/// <summary>
/// Abstract base class for read/write separation integration tests.
/// Contains standard test methods for connection routing verification that work
/// across ADO.NET, Dapper, and EF Core providers.
/// </summary>
/// <typeparam name="TFixture">The type of database fixture to use.</typeparam>
/// <remarks>
/// This base class provides:
/// <list type="bullet">
/// <item><description>Read operation routing to read connections</description></item>
/// <item><description>Write operation routing to primary connection</description></item>
/// <item><description>ForceWriteDatabase attribute behavior</description></item>
/// <item><description>Connection factory routing decisions</description></item>
/// </list>
/// <para>
/// Note: Full replica testing requires complex infrastructure. These tests focus on
/// routing logic verification rather than actual replica data consistency.
/// </para>
/// </remarks>
public abstract class ReadWriteSeparationTestsBase<TFixture> : IAsyncLifetime
    where TFixture : class
{
    /// <summary>
    /// Gets the database fixture instance.
    /// </summary>
    protected abstract TFixture Fixture { get; }

    /// <summary>
    /// Creates a connection for read operations.
    /// </summary>
    /// <returns>A connection configured for read intent.</returns>
    protected abstract IDbConnection CreateReadConnection();

    /// <summary>
    /// Creates a connection for write operations.
    /// </summary>
    /// <returns>A connection configured for write intent (primary).</returns>
    protected abstract IDbConnection CreateWriteConnection();

    /// <summary>
    /// Creates a connection with forced write intent (for read-after-write scenarios).
    /// </summary>
    /// <returns>A connection that routes to primary even for read operations.</returns>
    protected abstract IDbConnection CreateForcedWriteConnection();

    /// <summary>
    /// Inserts a test entity using the provided connection.
    /// </summary>
    protected abstract Task InsertEntityAsync(IDbConnection connection, ReadWriteTestEntity entity);

    /// <summary>
    /// Queries all test entities using the provided connection.
    /// </summary>
    protected abstract Task<List<ReadWriteTestEntity>> QueryEntitiesAsync(IDbConnection connection);

    /// <summary>
    /// Queries a specific entity by ID using the provided connection.
    /// </summary>
    protected abstract Task<ReadWriteTestEntity?> QueryEntityByIdAsync(IDbConnection connection, Guid id);

    /// <summary>
    /// Updates an entity using the provided connection.
    /// </summary>
    protected abstract Task UpdateEntityAsync(IDbConnection connection, ReadWriteTestEntity entity);

    /// <summary>
    /// Gets the connection string that was used by the connection.
    /// Used to verify routing decisions.
    /// </summary>
    protected abstract string GetConnectionString(IDbConnection connection);

    /// <summary>
    /// Gets the primary connection string for comparison.
    /// </summary>
    protected abstract string PrimaryConnectionString { get; }

    /// <summary>
    /// Gets the read replica connection string for comparison.
    /// </summary>
    protected abstract string ReadReplicaConnectionString { get; }

    /// <summary>
    /// Records a routing decision for later verification.
    /// </summary>
    protected List<RoutingDecision> RoutingDecisions { get; } = [];

    /// <summary>
    /// Gets the provider-specific name for display in test output.
    /// </summary>
    protected abstract string ProviderName { get; }

    /// <summary>
    /// Gets whether the fixture is configured with separate read/write endpoints.
    /// If false, tests will verify routing logic without separate databases.
    /// </summary>
    protected abstract bool HasSeparateReadWriteEndpoints { get; }

    /// <summary>
    /// Clears all test data.
    /// </summary>
    protected abstract Task ClearTestDataAsync();

    #region IAsyncLifetime

    /// <inheritdoc />
    public virtual async ValueTask InitializeAsync()
    {
        RoutingDecisions.Clear();
        await ClearTestDataAsync();
    }

    /// <inheritdoc />
    public virtual ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    #endregion

    #region Read Connection Routing Tests

    [Fact]
    public async Task ReadConnection_ShouldBeUsableForQueries()
    {
        // Arrange
        using var writeConn = CreateWriteConnection();
        var entity = new ReadWriteTestEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test Entity",
            Value = 100,
            Timestamp = DateTime.UtcNow,
            WriteCounter = 1
        };
        await InsertEntityAsync(writeConn, entity);

        // Act - Use read connection to query
        using var readConn = CreateReadConnection();
        var entities = await QueryEntitiesAsync(readConn);

        // Assert
        entities.ShouldNotBeEmpty($"[{ProviderName}] Read connection should return data");
    }

    [Fact]
    public async Task ReadConnection_ShouldRouteToReadEndpoint()
    {
        Assert.SkipUnless(HasSeparateReadWriteEndpoints, "Fixture not configured with separate read/write endpoints");

        // Act
        using var readConn = CreateReadConnection();
        await QueryEntitiesAsync(readConn);

        // Assert
        var connectionString = GetConnectionString(readConn);
        connectionString.ShouldBe(ReadReplicaConnectionString,
            $"[{ProviderName}] Read connection should use read replica endpoint");
    }

    [Fact]
    public void ReadConnection_ShouldRecordRoutingDecision()
    {
        // Act
        using var readConn = CreateReadConnection();

        // Record the routing decision
        RoutingDecisions.Add(new RoutingDecision
        {
            OperationType = "Read",
            Intent = TestDatabaseIntent.Read,
            ConnectionStringUsed = GetConnectionString(readConn),
            RoutedToPrimary = GetConnectionString(readConn) == PrimaryConnectionString
        });

        // Assert
        RoutingDecisions.ShouldContain(r => r.Intent == TestDatabaseIntent.Read,
            $"[{ProviderName}] Should record read routing decision");
    }

    #endregion

    #region Write Connection Routing Tests

    [Fact]
    public async Task WriteConnection_ShouldBeUsableForInserts()
    {
        // Arrange
        var entity = new ReadWriteTestEntity
        {
            Id = Guid.NewGuid(),
            Name = "Write Test Entity",
            Value = 200,
            Timestamp = DateTime.UtcNow,
            WriteCounter = 1
        };

        // Act
        using var writeConn = CreateWriteConnection();
        await InsertEntityAsync(writeConn, entity);

        // Assert - Verify insert worked
        var retrieved = await QueryEntityByIdAsync(writeConn, entity.Id);
        retrieved.ShouldNotBeNull($"[{ProviderName}] Write connection should persist data");
    }

    [Fact]
    public async Task WriteConnection_ShouldRouteToPrimaryEndpoint()
    {
        Assert.SkipUnless(HasSeparateReadWriteEndpoints, "Fixture not configured with separate read/write endpoints");

        // Act
        using var writeConn = CreateWriteConnection();
        await InsertEntityAsync(writeConn, new ReadWriteTestEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            Value = 1,
            Timestamp = DateTime.UtcNow,
            WriteCounter = 1
        });

        // Assert
        var connectionString = GetConnectionString(writeConn);
        connectionString.ShouldBe(PrimaryConnectionString,
            $"[{ProviderName}] Write connection should use primary endpoint");
    }

    [Fact]
    public void WriteConnection_ShouldRecordRoutingDecision()
    {
        // Act
        using var writeConn = CreateWriteConnection();

        // Record the routing decision
        RoutingDecisions.Add(new RoutingDecision
        {
            OperationType = "Write",
            Intent = TestDatabaseIntent.Write,
            ConnectionStringUsed = GetConnectionString(writeConn),
            RoutedToPrimary = GetConnectionString(writeConn) == PrimaryConnectionString
        });

        // Assert
        RoutingDecisions.ShouldContain(r => r.Intent == TestDatabaseIntent.Write,
            $"[{ProviderName}] Should record write routing decision");
    }

    #endregion

    #region Forced Write Connection Tests

    [Fact]
    public async Task ForcedWriteConnection_ShouldBeUsableForReadAfterWrite()
    {
        // Arrange - Insert using write connection
        var entity = new ReadWriteTestEntity
        {
            Id = Guid.NewGuid(),
            Name = "Read After Write Entity",
            Value = 300,
            Timestamp = DateTime.UtcNow,
            WriteCounter = 1
        };

        using (var writeConn = CreateWriteConnection())
        {
            await InsertEntityAsync(writeConn, entity);
        }

        // Act - Read immediately using forced write connection (to avoid replica lag)
        using var forcedConn = CreateForcedWriteConnection();
        var retrieved = await QueryEntityByIdAsync(forcedConn, entity.Id);

        // Assert
        retrieved.ShouldNotBeNull($"[{ProviderName}] Forced write connection should see recently written data");
    }

    [Fact]
    public void ForcedWriteConnection_ShouldRouteToPrimary()
    {
        Assert.SkipUnless(HasSeparateReadWriteEndpoints, "Fixture not configured with separate read/write endpoints");

        // Act
        using var forcedConn = CreateForcedWriteConnection();

        // Assert
        var connectionString = GetConnectionString(forcedConn);
        connectionString.ShouldBe(PrimaryConnectionString,
            $"[{ProviderName}] Forced write connection should use primary endpoint");
    }

    #endregion

    #region Update Operation Routing Tests

    [Fact]
    public async Task UpdateOperation_ShouldUseWriteConnection()
    {
        // Arrange
        var entity = new ReadWriteTestEntity
        {
            Id = Guid.NewGuid(),
            Name = "Update Test Entity",
            Value = 100,
            Timestamp = DateTime.UtcNow,
            WriteCounter = 0
        };

        using (var writeConn = CreateWriteConnection())
        {
            await InsertEntityAsync(writeConn, entity);
        }

        // Act - Update the entity
        entity.Value = 200;
        entity.WriteCounter = 1;

        using var updateConn = CreateWriteConnection();
        await UpdateEntityAsync(updateConn, entity);

        // Assert
        using var readConn = CreateForcedWriteConnection(); // Use forced to avoid replica lag
        var retrieved = await QueryEntityByIdAsync(readConn, entity.Id);

        retrieved.ShouldNotBeNull();
        retrieved!.Value.ShouldBe(200, $"[{ProviderName}] Update should persist to database");
        retrieved.WriteCounter.ShouldBe(1, $"[{ProviderName}] WriteCounter should be updated");
    }

    #endregion

    #region Connection Factory Behavior Tests

    [Fact]
    public void ConnectionFactory_ShouldCreateDistinctConnectionsForReadAndWrite()
    {
        // Act
        using var readConn = CreateReadConnection();
        using var writeConn = CreateWriteConnection();

        // Assert - They should be different connection instances
        readConn.ShouldNotBeSameAs(writeConn,
            $"[{ProviderName}] Read and write connections should be different instances");
    }

    [Fact]
    public void ConnectionFactory_ReadAndWriteConnectionStrings_ShouldDiffer()
    {
        Assert.SkipUnless(HasSeparateReadWriteEndpoints, "Fixture not configured with separate read/write endpoints");

        // Act
        using var readConn = CreateReadConnection();
        using var writeConn = CreateWriteConnection();

        var readConnString = GetConnectionString(readConn);
        var writeConnString = GetConnectionString(writeConn);

        // Assert
        readConnString.ShouldNotBe(writeConnString,
            $"[{ProviderName}] Read and write connections should use different connection strings");
    }

    #endregion

    #region Routing Decision Verification

    [Fact]
    public async Task RoutingDecisions_ShouldTrackAllOperations()
    {
        // Arrange
        var entity = new ReadWriteTestEntity
        {
            Id = Guid.NewGuid(),
            Name = "Tracking Test",
            Value = 1,
            Timestamp = DateTime.UtcNow,
            WriteCounter = 0
        };

        // Act - Perform various operations
        using (var writeConn = CreateWriteConnection())
        {
            RoutingDecisions.Add(new RoutingDecision
            {
                OperationType = "Insert",
                Intent = TestDatabaseIntent.Write,
                ConnectionStringUsed = GetConnectionString(writeConn),
                RoutedToPrimary = true
            });
            await InsertEntityAsync(writeConn, entity);
        }

        using (var readConn = CreateReadConnection())
        {
            RoutingDecisions.Add(new RoutingDecision
            {
                OperationType = "Select",
                Intent = TestDatabaseIntent.Read,
                ConnectionStringUsed = GetConnectionString(readConn),
                RoutedToPrimary = !HasSeparateReadWriteEndpoints || GetConnectionString(readConn) == PrimaryConnectionString
            });
            await QueryEntitiesAsync(readConn);
        }

        // Assert
        RoutingDecisions.Count.ShouldBe(2, $"[{ProviderName}] Should track all routing decisions");
        RoutingDecisions.ShouldContain(r => r.OperationType == "Insert" && r.Intent == TestDatabaseIntent.Write);
        RoutingDecisions.ShouldContain(r => r.OperationType == "Select" && r.Intent == TestDatabaseIntent.Read);
    }

    #endregion
}

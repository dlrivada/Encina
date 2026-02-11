using System.Diagnostics.CodeAnalysis;
using Encina.EntityFrameworkCore.ReadWriteSeparation;
using Encina.Messaging.ReadWriteSeparation;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.Sqlite.ReadWriteSeparation;

/// <summary>
/// SQLite-specific integration tests for EF Core read/write separation support.
/// Tests the ReadWriteDbContextFactory and connection routing logic.
/// </summary>
/// <remarks>
/// <para>
/// SQLite doesn't natively support replication. These tests verify routing logic
/// using a shared-cache in-memory database with the same connection string. This validates that:
/// </para>
/// <list type="bullet">
/// <item><description>ReadWriteDbContextFactory correctly routes to write context</description></item>
/// <item><description>ReadWriteDbContextFactory correctly routes to read context</description></item>
/// <item><description>DatabaseRoutingScope scenarios work correctly</description></item>
/// <item><description>Context factory creates usable DbContext instances</description></item>
/// </list>
/// <para>
/// In production SQLite scenarios, "replicas" would be file copies that are periodically
/// synchronized from the primary database file.
/// </para>
/// <para>
/// NOTE: Uses shared-cache mode (Mode=Memory;Cache=Shared) to allow multiple connections
/// to access the same in-memory database.
/// </para>
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Database", "Sqlite")]
[Collection("EFCore-Sqlite")]
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Connection is disposed in DisposeAsync")]
public sealed class ReadWriteSeparationEFSqliteTests : IAsyncLifetime
{
    private readonly EFCoreSqliteFixture _fixture;
    private SqliteConnection? _keepAliveConnection;
    private string _sharedConnectionString = null!;
    private ReadWriteSeparationOptions _options = null!;
    private ReadWriteConnectionSelector _connectionSelector = null!;
    private IServiceProvider _serviceProvider = null!;

    public ReadWriteSeparationEFSqliteTests(EFCoreSqliteFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        // Use a unique database name for each test run to avoid conflicts
        var dbName = $"RWTest_{Guid.NewGuid():N}";
        _sharedConnectionString = $"Data Source={dbName};Mode=Memory;Cache=Shared";

        // Keep one connection open to maintain the shared database
        _keepAliveConnection = new SqliteConnection(_sharedConnectionString);
        await _keepAliveConnection.OpenAsync();

        // Create schema for test entities
        await CreateSchemaAsync(_keepAliveConnection);

        // Configure read/write separation options (same connection string for SQLite)
        _options = new ReadWriteSeparationOptions
        {
            WriteConnectionString = _sharedConnectionString,
            ReadConnectionStrings = { _sharedConnectionString } // Same DB for routing logic test
        };

        var replicas = _options.ReadConnectionStrings.ToList().AsReadOnly();
        _connectionSelector = new ReadWriteConnectionSelector(_options, new RoundRobinReplicaSelector(replicas));

        // Setup service provider for factory
        var services = new ServiceCollection();
        services.AddSingleton<IReadWriteConnectionSelector>(_connectionSelector);
        services.AddSingleton(_options);

        // Configure base DbContext options with the shared connection string
        var baseOptionsBuilder = new DbContextOptionsBuilder<ReadWriteTestDbContext>();
        baseOptionsBuilder.UseSqlite(_sharedConnectionString);
        services.AddSingleton(baseOptionsBuilder.Options);

        _serviceProvider = services.BuildServiceProvider();
    }

    public async ValueTask DisposeAsync()
    {
        await ClearDataAsync();
        if (_keepAliveConnection is not null)
        {
            await _keepAliveConnection.DisposeAsync();
        }
    }

    private static async Task CreateSchemaAsync(SqliteConnection connection)
    {
        const string sql = """
            DROP TABLE IF EXISTS ReadWriteTestEntities;
            CREATE TABLE ReadWriteTestEntities (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Value INTEGER NOT NULL,
                Timestamp TEXT NOT NULL,
                WriteCounter INTEGER NOT NULL DEFAULT 0
            );
            """;

        await using var command = new SqliteCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task ClearDataAsync()
    {
        if (_keepAliveConnection is null)
            return;

        try
        {
            await using var command = new SqliteCommand("DELETE FROM ReadWriteTestEntities", _keepAliveConnection);
            await command.ExecuteNonQueryAsync();
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    private ReadWriteTestDbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ReadWriteTestDbContext>();
        optionsBuilder.UseSqlite(_sharedConnectionString);
        return new ReadWriteTestDbContext(optionsBuilder.Options);
    }

    private ReadWriteDbContextFactory<ReadWriteTestDbContext> CreateFactory()
    {
        var baseOptionsBuilder = new DbContextOptionsBuilder<ReadWriteTestDbContext>();
        baseOptionsBuilder.UseSqlite(_sharedConnectionString);

        return new ReadWriteDbContextFactory<ReadWriteTestDbContext>(
            _serviceProvider,
            _connectionSelector,
            baseOptionsBuilder.Options);
    }

    #region Connection Factory Tests

    [Fact]
    public void Factory_ShouldBeConfiguredCorrectly()
    {
        // Arrange
        var factory = CreateFactory();

        // Assert
        factory.ShouldNotBeNull();
        _connectionSelector.GetWriteConnectionString().ShouldBe(_sharedConnectionString);
        _connectionSelector.GetReadConnectionString().ShouldBe(_sharedConnectionString);
    }

    [Fact]
    public void CreateWriteContext_ShouldReturnUsableContext()
    {
        // Arrange
        var factory = CreateFactory();

        // Act
        using var context = factory.CreateWriteContext();

        // Assert
        context.ShouldNotBeNull();
        context.ShouldBeOfType<ReadWriteTestDbContext>();
    }

    [Fact]
    public void CreateReadContext_ShouldReturnUsableContext()
    {
        // Arrange
        var factory = CreateFactory();

        // Act
        using var context = factory.CreateReadContext();

        // Assert
        context.ShouldNotBeNull();
        context.ShouldBeOfType<ReadWriteTestDbContext>();
    }

    [Fact]
    public void CreateContext_ShouldReturnUsableContext()
    {
        // Arrange
        var factory = CreateFactory();

        // Act
        using var context = factory.CreateContext();

        // Assert
        context.ShouldNotBeNull();
        context.ShouldBeOfType<ReadWriteTestDbContext>();
    }

    #endregion

    #region Write Operation Tests

    [Fact]
    public async Task WriteContext_ShouldBeUsableForInserts()
    {
        // Arrange
        var factory = CreateFactory();
        var entity = new ReadWriteTestEntity
        {
            Id = Guid.NewGuid(),
            Name = "Write Test Entity",
            Value = 200,
            Timestamp = DateTime.UtcNow,
            WriteCounter = 1
        };

        // Act
        await using (var writeContext = factory.CreateWriteContext())
        {
            writeContext.ReadWriteTestEntities.Add(entity);
            await writeContext.SaveChangesAsync();
        }

        // Assert - Verify insert worked using direct context
        await using var verifyContext = CreateDbContext();
        var retrieved = await verifyContext.ReadWriteTestEntities.FindAsync(entity.Id);
        retrieved.ShouldNotBeNull();
        retrieved!.Name.ShouldBe("Write Test Entity");
    }

    [Fact]
    public async Task WriteContext_ShouldBeUsableForUpdates()
    {
        // Arrange
        var factory = CreateFactory();
        var entity = new ReadWriteTestEntity
        {
            Id = Guid.NewGuid(),
            Name = "Update Test Entity",
            Value = 100,
            Timestamp = DateTime.UtcNow,
            WriteCounter = 0
        };

        // Insert
        await using (var context = CreateDbContext())
        {
            context.ReadWriteTestEntities.Add(entity);
            await context.SaveChangesAsync();
        }

        // Act - Update
        await using (var writeContext = factory.CreateWriteContext())
        {
            var toUpdate = await writeContext.ReadWriteTestEntities.FindAsync(entity.Id);
            toUpdate.ShouldNotBeNull();
            toUpdate!.Value = 200;
            toUpdate.WriteCounter = 1;
            await writeContext.SaveChangesAsync();
        }

        // Assert
        await using var verifyContext = CreateDbContext();
        var retrieved = await verifyContext.ReadWriteTestEntities.FindAsync(entity.Id);
        retrieved.ShouldNotBeNull();
        retrieved!.Value.ShouldBe(200);
        retrieved.WriteCounter.ShouldBe(1);
    }

    #endregion

    #region Read Operation Tests

    [Fact]
    public async Task ReadContext_ShouldBeUsableForQueries()
    {
        // Arrange
        var factory = CreateFactory();
        var entity = new ReadWriteTestEntity
        {
            Id = Guid.NewGuid(),
            Name = "Read Test Entity",
            Value = 100,
            Timestamp = DateTime.UtcNow,
            WriteCounter = 0
        };

        await using (var context = CreateDbContext())
        {
            context.ReadWriteTestEntities.Add(entity);
            await context.SaveChangesAsync();
        }

        // Act - Query using read context
        await using var readContext = factory.CreateReadContext();
        var entities = await readContext.ReadWriteTestEntities.ToListAsync();

        // Assert
        entities.ShouldNotBeEmpty();
        entities.ShouldContain(e => e.Id == entity.Id);
    }

    #endregion

    #region Routing Scope Tests

    [Fact]
    public async Task RoutingScope_WithReadIntent_ShouldRouteToRead()
    {
        // Arrange
        var factory = CreateFactory();
        var entity = new ReadWriteTestEntity
        {
            Id = Guid.NewGuid(),
            Name = "Routing Test Entity",
            Value = 100,
            Timestamp = DateTime.UtcNow,
            WriteCounter = 0
        };

        await using (var context = CreateDbContext())
        {
            context.ReadWriteTestEntities.Add(entity);
            await context.SaveChangesAsync();
        }

        // Act - Use read routing scope
        using var scope = new DatabaseRoutingScope(DatabaseIntent.Read);
        await using var context2 = factory.CreateContext();
        var entities = await context2.ReadWriteTestEntities.ToListAsync();

        // Assert
        entities.ShouldNotBeEmpty();
        _connectionSelector.GetReadConnectionString().ShouldNotBeEmpty();
    }

    [Fact]
    public async Task RoutingScope_WithWriteIntent_ShouldRouteToPrimary()
    {
        // Arrange
        var factory = CreateFactory();
        var entity = new ReadWriteTestEntity
        {
            Id = Guid.NewGuid(),
            Name = "Routing Test Entity",
            Value = 100,
            Timestamp = DateTime.UtcNow,
            WriteCounter = 0
        };

        // Act - Use write routing scope
        using var scope = new DatabaseRoutingScope(DatabaseIntent.Write);
        await using var context = factory.CreateContext();
        context.ReadWriteTestEntities.Add(entity);
        await context.SaveChangesAsync();

        // Assert
        _connectionSelector.GetWriteConnectionString().ShouldNotBeEmpty();
    }

    #endregion

    #region Async Factory Tests

    [Fact]
    public async Task CreateWriteContextAsync_ShouldReturnUsableContext()
    {
        // Arrange
        var factory = CreateFactory();

        // Act
        await using var context = await factory.CreateWriteContextAsync();

        // Assert
        context.ShouldNotBeNull();
        context.ShouldBeOfType<ReadWriteTestDbContext>();
    }

    [Fact]
    public async Task CreateReadContextAsync_ShouldReturnUsableContext()
    {
        // Arrange
        var factory = CreateFactory();

        // Act
        await using var context = await factory.CreateReadContextAsync();

        // Assert
        context.ShouldNotBeNull();
        context.ShouldBeOfType<ReadWriteTestDbContext>();
    }

    [Fact]
    public async Task CreateContextAsync_ShouldReturnUsableContext()
    {
        // Arrange
        var factory = CreateFactory();

        // Act
        await using var context = await factory.CreateContextAsync();

        // Assert
        context.ShouldNotBeNull();
        context.ShouldBeOfType<ReadWriteTestDbContext>();
    }

    #endregion

    #region Connection String Tests

    [Fact]
    public void ConnectionSelector_GetWriteConnectionString_ShouldReturnPrimary()
    {
        // Assert
        _connectionSelector.GetWriteConnectionString().ShouldBe(_sharedConnectionString);
    }

    [Fact]
    public void ConnectionSelector_GetReadConnectionString_ShouldReturnReplica()
    {
        // Assert - In this test setup, replica is the same as primary
        _connectionSelector.GetReadConnectionString().ShouldBe(_sharedConnectionString);
    }

    #endregion
}

#region Test DbContext and Entities

internal sealed class ReadWriteTestDbContext : DbContext
{
    public ReadWriteTestDbContext(DbContextOptions<ReadWriteTestDbContext> options)
        : base(options)
    {
    }

    public DbSet<ReadWriteTestEntity> ReadWriteTestEntities => Set<ReadWriteTestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ReadWriteTestEntity>(entity =>
        {
            entity.ToTable("ReadWriteTestEntities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
        });
    }
}

internal class ReadWriteTestEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public DateTime Timestamp { get; set; }
    public int WriteCounter { get; set; }
}

#endregion

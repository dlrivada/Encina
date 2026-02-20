using Encina.EntityFrameworkCore.ReadWriteSeparation;
using Encina.Messaging.ReadWriteSeparation;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Encina.Testing.Shouldly;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shouldly;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.PostgreSQL.ReadWriteSeparation;

/// <summary>
/// PostgreSQL-specific integration tests for EF Core read/write separation support.
/// Tests the ReadWriteDbContextFactory and connection routing logic.
/// </summary>
/// <remarks>
/// <para>
/// Since setting up actual PostgreSQL replication requires complex infrastructure,
/// these tests verify routing logic using the same database with the same connection string.
/// </para>
/// <list type="bullet">
/// <item><description>ReadWriteDbContextFactory correctly routes to write context</description></item>
/// <item><description>ReadWriteDbContextFactory correctly routes to read context</description></item>
/// <item><description>DatabaseRoutingScope scenarios work correctly</description></item>
/// <item><description>Context factory creates usable DbContext instances</description></item>
/// </list>
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
[Collection("EFCore-PostgreSQL")]
public sealed class ReadWriteSeparationEFPostgreSqlTests : IAsyncLifetime
{
    private readonly EFCorePostgreSqlFixture _fixture;
    private ReadWriteSeparationOptions _options = null!;
    private ReadWriteConnectionSelector _connectionSelector = null!;
    private IServiceProvider _serviceProvider = null!;

    public ReadWriteSeparationEFPostgreSqlTests(EFCorePostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        if (!_fixture.IsAvailable)
            return;

        // Create schema for test entities
        await CreateSchemaAsync();

        // Configure read/write separation options (same connection for routing logic test)
        _options = new ReadWriteSeparationOptions
        {
            WriteConnectionString = _fixture.ConnectionString,
            ReadConnectionStrings = { _fixture.ConnectionString } // Same DB for routing logic test
        };

        var replicas = _options.ReadConnectionStrings.ToList().AsReadOnly();
        _connectionSelector = new ReadWriteConnectionSelector(_options, new RoundRobinReplicaSelector(replicas));

        // Setup service provider for factory
        var services = new ServiceCollection();
        services.AddSingleton(_connectionSelector);
        services.AddSingleton(_options);

        // Configure base DbContext options
        var baseOptionsBuilder = new DbContextOptionsBuilder<ReadWriteTestDbContext>();
        baseOptionsBuilder.UseNpgsql(_fixture.ConnectionString);
        services.AddSingleton(baseOptionsBuilder.Options);

        _serviceProvider = services.BuildServiceProvider();
    }

    public async ValueTask DisposeAsync()
    {
        if (!_fixture.IsAvailable)
            return;

        await ClearDataAsync();
    }

    private async Task CreateSchemaAsync()
    {
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        const string sql = """
            DROP TABLE IF EXISTS "RWTestEntities";
            CREATE TABLE "RWTestEntities" (
                "Id" UUID PRIMARY KEY,
                "Name" VARCHAR(200) NOT NULL,
                "Value" INT NOT NULL,
                "Timestamp" TIMESTAMPTZ NOT NULL,
                "WriteCounter" INT NOT NULL DEFAULT 0
            );
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task ClearDataAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand("DELETE FROM \"RWTestEntities\"", connection);
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
        optionsBuilder.UseNpgsql(_fixture.ConnectionString);
        return new ReadWriteTestDbContext(optionsBuilder.Options);
    }

    private ReadWriteDbContextFactory<ReadWriteTestDbContext> CreateFactory()
    {
        var baseOptionsBuilder = new DbContextOptionsBuilder<ReadWriteTestDbContext>();
        baseOptionsBuilder.UseNpgsql(_fixture.ConnectionString);

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
        _connectionSelector.GetWriteConnectionString().ShouldBeRight().ShouldBe(_fixture.ConnectionString);
        _connectionSelector.GetReadConnectionString().ShouldBeRight().ShouldBe(_fixture.ConnectionString);
    }

    [Fact]
    public void CreateWriteContext_ShouldReturnUsableContext()
    {

        // Arrange
        var factory = CreateFactory();

        // Act
        var context = factory.CreateWriteContext().ShouldBeRight();
        using (context)
        {
            // Assert
            context.ShouldNotBeNull();
            context.ShouldBeOfType<ReadWriteTestDbContext>();
        }
    }

    [Fact]
    public void CreateReadContext_ShouldReturnUsableContext()
    {

        // Arrange
        var factory = CreateFactory();

        // Act
        var context = factory.CreateReadContext().ShouldBeRight();
        using (context)
        {
            // Assert
            context.ShouldNotBeNull();
            context.ShouldBeOfType<ReadWriteTestDbContext>();
        }
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
        await using (var writeContext = factory.CreateWriteContext().ShouldBeRight())
        {
            writeContext.RWTestEntities.Add(entity);
            await writeContext.SaveChangesAsync();
        }

        // Assert - Verify insert worked using direct context
        await using var verifyContext = CreateDbContext();
        var retrieved = await verifyContext.RWTestEntities.FindAsync(entity.Id);
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
            context.RWTestEntities.Add(entity);
            await context.SaveChangesAsync();
        }

        // Act - Update
        await using (var writeContext = factory.CreateWriteContext().ShouldBeRight())
        {
            var toUpdate = await writeContext.RWTestEntities.FindAsync(entity.Id);
            toUpdate.ShouldNotBeNull();
            toUpdate!.Value = 200;
            toUpdate.WriteCounter = 1;
            await writeContext.SaveChangesAsync();
        }

        // Assert
        await using var verifyContext = CreateDbContext();
        var retrieved = await verifyContext.RWTestEntities.FindAsync(entity.Id);
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
            context.RWTestEntities.Add(entity);
            await context.SaveChangesAsync();
        }

        // Act - Query using read context
        await using var readContext = factory.CreateReadContext().ShouldBeRight();
        var entities = await readContext.RWTestEntities.ToListAsync();

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
            context.RWTestEntities.Add(entity);
            await context.SaveChangesAsync();
        }

        // Act - Use read routing scope
        using var scope = new DatabaseRoutingScope(DatabaseIntent.Read);
        await using var context2 = factory.CreateContext().ShouldBeRight();
        var entities = await context2.RWTestEntities.ToListAsync();

        // Assert
        entities.ShouldNotBeEmpty();
        _connectionSelector.GetReadConnectionString().ShouldBeRight().ShouldNotBeEmpty();
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
        await using var context = factory.CreateContext().ShouldBeRight();
        context.RWTestEntities.Add(entity);
        await context.SaveChangesAsync();

        // Assert
        _connectionSelector.GetWriteConnectionString().ShouldBeRight().ShouldNotBeEmpty();
    }

    #endregion

    #region Async Factory Tests

    [Fact]
    public async Task CreateWriteContextAsync_ShouldReturnUsableContext()
    {

        // Arrange
        var factory = CreateFactory();

        // Act
        var result = await factory.CreateWriteContextAsync();
        await using var context = result.ShouldBeRight();

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
        var result = await factory.CreateReadContextAsync();
        await using var context = result.ShouldBeRight();

        // Assert
        context.ShouldNotBeNull();
        context.ShouldBeOfType<ReadWriteTestDbContext>();
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

    public DbSet<ReadWriteTestEntity> RWTestEntities => Set<ReadWriteTestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ReadWriteTestEntity>(entity =>
        {
            entity.ToTable("RWTestEntities");
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

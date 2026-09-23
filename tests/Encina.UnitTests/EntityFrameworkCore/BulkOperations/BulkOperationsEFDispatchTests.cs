using Encina.EntityFrameworkCore.BulkOperations;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Unit tests for <see cref="BulkOperationsEF{TEntity}"/> provider dispatch logic.
/// </summary>
/// <remarks>
/// Oracle and SQLite are out of scope for Encina 1.0 (ADR-009, ADR-024): the dispatcher no
/// longer selects a provider-specific implementation for them, so connecting through SQLite
/// now falls into the unsupported-provider branch instead of <c>BulkOperationsEFSqlite</c>.
/// MySQL dispatch is not exercised here because Pomelo.EntityFrameworkCore.MySql does not yet
/// support EF Core 10 (see the MySQL integration tests, which are skipped for the same reason).
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Provider", "EntityFrameworkCore")]
public sealed class BulkOperationsEFDispatchTests
{
    [Fact]
    public void Constructor_SqlServerConnection_DispatchesSuccessfully()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<DispatchTestDbContext>()
            .UseSqlServer("Server=fake;Database=fake;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;
        using var dbContext = new DispatchTestDbContext(options);

        // Act
        var bulkOps = new BulkOperationsEF<DispatchTestEntity>(dbContext);

        // Assert
        bulkOps.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_PostgreSqlConnection_DispatchesSuccessfully()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<DispatchTestDbContext>()
            .UseNpgsql("Host=fake;Database=fake;")
            .Options;
        using var dbContext = new DispatchTestDbContext(options);

        // Act
        var bulkOps = new BulkOperationsEF<DispatchTestEntity>(dbContext);

        // Assert
        bulkOps.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_UnsupportedConnectionType_ThrowsNotSupportedExceptionListingSupportedProviders()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<DispatchTestDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        using var dbContext = new DispatchTestDbContext(options);

        // Act
        var ex = Should.Throw<NotSupportedException>(() => new BulkOperationsEF<DispatchTestEntity>(dbContext));

        // Assert
        ex.Message.ShouldContain("SqliteConnection");
        ex.Message.ShouldContain("Supported providers: SQL Server, PostgreSQL, MySQL.");
        ex.Message.ShouldNotContain("Oracle");
    }

    private sealed class DispatchTestEntity
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    private sealed class DispatchTestDbContext(DbContextOptions<DispatchTestDbContext> options) : DbContext(options)
    {
        public DbSet<DispatchTestEntity> DispatchTestEntities => Set<DispatchTestEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DispatchTestEntity>(entity =>
            {
                entity.ToTable("DispatchTestEntities");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            });
        }
    }
}

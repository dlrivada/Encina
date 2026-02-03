using Encina.DomainModeling;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.SoftDelete;

/// <summary>
/// EF Core fixture for soft delete integration tests.
/// Provides a throwaway SQL Server instance for testing soft delete operations.
/// </summary>
public sealed class SoftDeleteEFFixture : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlServerFixture = new();

    public SoftDeleteTestDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SoftDeleteTestDbContext>()
            .UseSqlServer(_sqlServerFixture.ConnectionString)
            .Options;

        return new SoftDeleteTestDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await _sqlServerFixture.InitializeAsync();
        await CreateSoftDeleteSchemaAsync();
    }

    public async Task DisposeAsync()
    {
        await _sqlServerFixture.DisposeAsync();
    }

    /// <summary>
    /// Clears all data from all tables (but preserves schema).
    /// Use this between tests to ensure clean state.
    /// </summary>
    public async Task ClearAllDataAsync()
    {
        using var connection = new SqlConnection(_sqlServerFixture.ConnectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM SoftDeleteTestEntities";
        await command.ExecuteNonQueryAsync();
    }

    private async Task CreateSoftDeleteSchemaAsync()
    {
        using var connection = new SqlConnection(_sqlServerFixture.ConnectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = """
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SoftDeleteTestEntities' AND xtype='U')
            BEGIN
                CREATE TABLE SoftDeleteTestEntities (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    Name NVARCHAR(200) NOT NULL,
                    Amount DECIMAL(18,2) NOT NULL,
                    IsDeleted BIT NOT NULL DEFAULT 0,
                    DeletedAtUtc DATETIME2 NULL,
                    DeletedBy NVARCHAR(200) NULL,
                    CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                    CreatedBy NVARCHAR(200) NULL,
                    ModifiedAtUtc DATETIME2 NULL,
                    ModifiedBy NVARCHAR(200) NULL
                );

                CREATE INDEX IX_SoftDeleteTestEntities_IsDeleted
                    ON SoftDeleteTestEntities(IsDeleted);
            END
            """;

        await command.ExecuteNonQueryAsync();
    }
}

/// <summary>
/// Test DbContext for soft delete integration tests.
/// </summary>
public sealed class SoftDeleteTestDbContext : DbContext
{
    public SoftDeleteTestDbContext(DbContextOptions<SoftDeleteTestDbContext> options)
        : base(options)
    {
    }

    public DbSet<SoftDeleteTestEntity> SoftDeleteTestEntities => Set<SoftDeleteTestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SoftDeleteTestEntity>(entity =>
        {
            entity.ToTable("SoftDeleteTestEntities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Amount).HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.IsDeleted).IsRequired();
            entity.Property(e => e.DeletedAtUtc);
            entity.Property(e => e.DeletedBy).HasMaxLength(200);
            entity.Property(e => e.CreatedAtUtc).IsRequired();
            entity.Property(e => e.CreatedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedAtUtc);
            entity.Property(e => e.ModifiedBy).HasMaxLength(200);

            // Apply global query filter for soft delete
            entity.HasQueryFilter(e => !e.IsDeleted);
        });
    }
}

/// <summary>
/// Test entity for soft delete integration tests.
/// Implements both ISoftDeletable and ISoftDeletableEntity for full compatibility.
/// </summary>
public sealed class SoftDeleteTestEntity : IEntity<Guid>, ISoftDeletable, ISoftDeletableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
}

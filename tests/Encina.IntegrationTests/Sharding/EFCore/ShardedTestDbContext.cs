using Encina.IntegrationTests.Sharding;

using Microsoft.EntityFrameworkCore;

namespace Encina.IntegrationTests.Sharding.EFCore;

/// <summary>
/// Test DbContext for EF Core sharded repository integration tests.
/// Uses PascalCase column names matching SQLite, SQL Server, and MySQL table schemas.
/// </summary>
public sealed class ShardedTestDbContext : DbContext
{
    public ShardedTestDbContext(DbContextOptions<ShardedTestDbContext> options)
        : base(options)
    {
    }

    public DbSet<ShardedTestEntity> ShardedEntities => Set<ShardedTestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShardedTestEntity>(entity =>
        {
            entity.ToTable("ShardedEntities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ShardKey).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Value);
            entity.Property(e => e.CreatedAtUtc).IsRequired();
        });
    }
}

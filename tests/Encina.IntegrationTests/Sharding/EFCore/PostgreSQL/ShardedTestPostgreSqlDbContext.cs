using Encina.IntegrationTests.Sharding;

using Microsoft.EntityFrameworkCore;

namespace Encina.IntegrationTests.Sharding.EFCore.PostgreSQL;

/// <summary>
/// PostgreSQL-specific test DbContext for EF Core sharded repository integration tests.
/// Uses snake_case column names and table name matching the PostgreSQL fixture schema.
/// </summary>
public sealed class ShardedTestPostgreSqlDbContext : DbContext
{
    public ShardedTestPostgreSqlDbContext(DbContextOptions<ShardedTestPostgreSqlDbContext> options)
        : base(options)
    {
    }

    public DbSet<ShardedTestEntity> ShardedEntities => Set<ShardedTestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShardedTestEntity>(entity =>
        {
            entity.ToTable("sharded_entities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasMaxLength(100).IsRequired();
            entity.Property(e => e.ShardKey).HasColumnName("shard_key").HasMaxLength(200).IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(500).IsRequired();
            entity.Property(e => e.Value).HasColumnName("value");
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        });
    }
}

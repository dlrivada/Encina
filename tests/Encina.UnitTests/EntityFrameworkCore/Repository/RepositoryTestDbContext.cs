using Microsoft.EntityFrameworkCore;

namespace Encina.UnitTests.EntityFrameworkCore.Repository;

/// <summary>
/// Test DbContext for repository tests.
/// </summary>
public class RepositoryTestDbContext : DbContext
{
    public RepositoryTestDbContext(DbContextOptions<RepositoryTestDbContext> options) : base(options)
    {
    }

    public DbSet<TestEntity> TestEntities => Set<TestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Amount).HasPrecision(18, 2);
        });
    }
}

/// <summary>
/// Test DbContext for IHasId entity tests.
/// </summary>
public class HasIdTestDbContext : DbContext
{
    public HasIdTestDbContext(DbContextOptions<HasIdTestDbContext> options) : base(options)
    {
    }

    public DbSet<TestEntityWithHasId> TestEntities => Set<TestEntityWithHasId>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestEntityWithHasId>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
        });
    }
}

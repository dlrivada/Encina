using Encina.DomainModeling;
using Microsoft.EntityFrameworkCore;

namespace Encina.UnitTests.EntityFrameworkCore.SoftDelete;

/// <summary>
/// Test DbContext for soft delete functionality.
/// </summary>
public class SoftDeleteTestDbContext : DbContext
{
    public SoftDeleteTestDbContext(DbContextOptions<SoftDeleteTestDbContext> options) : base(options)
    {
    }

    public DbSet<TestSoftDeletableOrder> Orders => Set<TestSoftDeletableOrder>();
    public DbSet<TestSoftDeletableOrderEntity> OrderEntities => Set<TestSoftDeletableOrderEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestSoftDeletableOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CustomerName).HasMaxLength(200);
            entity.Property(e => e.Total).HasPrecision(18, 2);
            entity.Property(e => e.CreatedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedBy).HasMaxLength(200);
            entity.Property(e => e.DeletedBy).HasMaxLength(200);

            // Apply global query filter for soft delete
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<TestSoftDeletableOrderEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CustomerName).HasMaxLength(200);
            entity.Property(e => e.Total).HasPrecision(18, 2);
            entity.Property(e => e.CreatedBy).HasMaxLength(200);
            entity.Property(e => e.ModifiedBy).HasMaxLength(200);
            entity.Property(e => e.DeletedBy).HasMaxLength(200);

            // Apply global query filter for soft delete
            entity.HasQueryFilter(e => !e.IsDeleted);
        });
    }
}

/// <summary>
/// Test entity implementing ISoftDeletableEntity for interceptor-based soft delete.
/// </summary>
public sealed class TestSoftDeletableOrder : IEntity<Guid>, ISoftDeletableEntity
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Total { get; set; }

    // IAuditableEntity properties
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }

    // ISoftDeletableEntity properties
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
}

/// <summary>
/// Test entity for soft delete repository tests.
/// Implements both ISoftDeletable (for repository constraint) and ISoftDeletableEntity (for interceptor compatibility).
/// </summary>
public sealed class TestSoftDeletableOrderEntity : IEntity<Guid>, ISoftDeletable, ISoftDeletableEntity
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
}

/// <summary>
/// Specification to retrieve all orders.
/// </summary>
public sealed class AllOrdersSpecification : Specification<TestSoftDeletableOrderEntity>
{
    public override System.Linq.Expressions.Expression<Func<TestSoftDeletableOrderEntity, bool>> ToExpression()
    {
        return o => true;
    }
}

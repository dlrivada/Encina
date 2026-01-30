using Encina.DomainModeling;
using Microsoft.EntityFrameworkCore;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.Repository;

/// <summary>
/// Test DbContext for repository integration tests.
/// </summary>
public sealed class RepositoryTestDbContext : DbContext
{
    public RepositoryTestDbContext(DbContextOptions<RepositoryTestDbContext> options)
        : base(options)
    {
    }

    public DbSet<TestEntity> TestEntities => Set<TestEntity>();
    public DbSet<TestImmutableAggregate> ImmutableAggregates => Set<TestImmutableAggregate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestEntity>(entity =>
        {
            entity.ToTable("TestEntities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Amount).HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CreatedAtUtc).IsRequired();
            entity.HasIndex(e => e.IsActive);
        });

        modelBuilder.Entity<TestImmutableAggregate>(entity =>
        {
            entity.ToTable("ImmutableAggregates");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Amount).HasPrecision(18, 2).IsRequired();
            entity.Ignore(e => e.DomainEvents);
            entity.Ignore(e => e.RowVersion);
        });
    }
}

/// <summary>
/// Test entity for repository integration tests.
/// </summary>
public class TestEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// Test aggregate root for immutable record update integration tests.
/// Uses C# record-like semantics with init properties.
/// </summary>
public sealed class TestImmutableAggregate : AggregateRoot<Guid>
{
    /// <summary>
    /// Gets the name of the aggregate.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the amount.
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Creates a new aggregate with the specified id.
    /// </summary>
    public TestImmutableAggregate(Guid id) : base(id) { }

    /// <summary>
    /// Raises a domain event for testing purposes.
    /// </summary>
    public void RaiseTestEvent(IDomainEvent domainEvent) => RaiseDomainEvent(domainEvent);
}

/// <summary>
/// Test domain event for immutable aggregate tests.
/// </summary>
public sealed record TestImmutableEvent(string Message) : DomainEvent;

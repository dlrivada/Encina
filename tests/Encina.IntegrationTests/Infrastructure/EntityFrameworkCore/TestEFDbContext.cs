using Encina.EntityFrameworkCore.Inbox;
using Encina.EntityFrameworkCore.Outbox;
using Encina.EntityFrameworkCore.Sagas;
using Encina.EntityFrameworkCore.Scheduling;
using Encina.TestInfrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore;

/// <summary>
/// Test DbContext for EF Core integration tests with real databases.
/// Supports all messaging entities and test entities for repository tests.
/// </summary>
public sealed class TestEFDbContext : DbContext
{
    public TestEFDbContext(DbContextOptions<TestEFDbContext> options)
        : base(options)
    {
    }

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<SagaState> SagaStates => Set<SagaState>();
    public DbSet<ScheduledMessage> ScheduledMessages => Set<ScheduledMessage>();
    public DbSet<TestRepositoryEntity> TestRepositoryEntities => Set<TestRepositoryEntity>();
    public DbSet<TestImmutableOrder> Orders => Set<TestImmutableOrder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Outbox configuration
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("OutboxMessages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NotificationType).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.CreatedAtUtc).IsRequired();
            entity.Property(e => e.RetryCount).IsRequired();
            entity.HasIndex(e => new { e.ProcessedAtUtc, e.NextRetryAtUtc });
        });

        // Inbox configuration
        modelBuilder.Entity<InboxMessage>(entity =>
        {
            entity.ToTable("InboxMessages");
            entity.HasKey(e => e.MessageId);
            entity.Property(e => e.MessageId).HasMaxLength(256).IsRequired();
            entity.Property(e => e.RequestType).HasMaxLength(500).IsRequired();
            entity.Property(e => e.ReceivedAtUtc).IsRequired();
            entity.Property(e => e.ExpiresAtUtc).IsRequired();
            entity.Property(e => e.RetryCount).IsRequired();
            entity.HasIndex(e => e.ExpiresAtUtc);
        });

        // Saga configuration
        modelBuilder.Entity<SagaState>(entity =>
        {
            entity.ToTable("SagaStates");
            entity.HasKey(e => e.SagaId);
            entity.Property(e => e.SagaType).HasMaxLength(500).IsRequired();
            entity.Property(e => e.CurrentStep).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Data).IsRequired();
            entity.Property(e => e.StartedAtUtc).IsRequired();
            entity.Property(e => e.LastUpdatedAtUtc).IsRequired();
            entity.HasIndex(e => new { e.Status, e.LastUpdatedAtUtc });
        });

        // Scheduling configuration
        modelBuilder.Entity<ScheduledMessage>(entity =>
        {
            entity.ToTable("ScheduledMessages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RequestType).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.ScheduledAtUtc).IsRequired();
            entity.Property(e => e.RetryCount).IsRequired();
            entity.Property(e => e.CronExpression).HasMaxLength(200);
            entity.Property(e => e.IsRecurring).IsRequired();
            entity.HasIndex(e => new { e.ScheduledAtUtc, e.ProcessedAtUtc });
        });

        // TestRepositoryEntity for repository integration tests
        modelBuilder.Entity<TestRepositoryEntity>(entity =>
        {
            entity.ToTable("TestRepositoryEntities");
            entity.HasKey(e => e.Id);
            // Prevent EF Core from auto-generating the Id - bulk operations provide their own IDs
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Amount).HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CreatedAtUtc).IsRequired();
            entity.HasIndex(e => e.IsActive);
        });

        // TestImmutableOrder for immutable update integration tests
        modelBuilder.Entity<TestImmutableOrder>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CustomerName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.Ignore(e => e.DomainEvents);
            entity.Ignore(e => e.RowVersion);
        });
    }
}

/// <summary>
/// Test entity for repository integration tests across all database providers.
/// </summary>
public class TestRepositoryEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

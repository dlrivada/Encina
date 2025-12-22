using Microsoft.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Inbox;
using Encina.EntityFrameworkCore.Outbox;
using Encina.EntityFrameworkCore.Sagas;
using Encina.EntityFrameworkCore.Scheduling;

namespace Encina.EntityFrameworkCore.IntegrationTests;

/// <summary>
/// Test DbContext for EF Core integration tests with real SQL Server.
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
            entity.Property(e => e.Status).IsRequired();
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
    }
}

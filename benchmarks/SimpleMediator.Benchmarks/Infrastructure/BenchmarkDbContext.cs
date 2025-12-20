using Microsoft.EntityFrameworkCore;
using SimpleMediator.EntityFrameworkCore.Outbox;
using SimpleMediator.EntityFrameworkCore.Inbox;
using SimpleMediator.EntityFrameworkCore.Sagas;
using SimpleMediator.EntityFrameworkCore.Scheduling;

namespace SimpleMediator.Benchmarks.Infrastructure;

/// <summary>
/// Shared DbContext for EF Core benchmarks.
/// Configures all messaging entities (Outbox, Inbox, Saga, Scheduling).
/// </summary>
public sealed class BenchmarkDbContext(DbContextOptions<BenchmarkDbContext> options) : DbContext(options)
{
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
            entity.Property(e => e.NotificationType).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.CreatedAtUtc).IsRequired();
            entity.Property(e => e.RetryCount).HasDefaultValue(0);
        });

        // Inbox configuration
        modelBuilder.Entity<InboxMessage>(entity =>
        {
            entity.ToTable("InboxMessages");
            entity.HasKey(e => e.MessageId);
            entity.Property(e => e.RequestType).IsRequired();
            entity.Property(e => e.ReceivedAtUtc).IsRequired();
            entity.Property(e => e.ExpiresAtUtc).IsRequired();
            entity.Property(e => e.RetryCount).HasDefaultValue(0);
        });

        // Saga configuration
        modelBuilder.Entity<SagaState>(entity =>
        {
            entity.ToTable("SagaStates");
            entity.HasKey(e => e.SagaId);
            entity.Property(e => e.SagaType).IsRequired();
            entity.Property(e => e.Data).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasConversion<string>();
            entity.Property(e => e.StartedAtUtc).IsRequired();
            entity.Property(e => e.LastUpdatedAtUtc).IsRequired();
            entity.Property(e => e.CurrentStep).HasDefaultValue(0);
        });

        // Scheduling configuration
        modelBuilder.Entity<ScheduledMessage>(entity =>
        {
            entity.ToTable("ScheduledMessages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RequestType).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.ScheduledAtUtc).IsRequired();
            entity.Property(e => e.CreatedAtUtc).IsRequired();
            entity.Property(e => e.RetryCount).HasDefaultValue(0);
            entity.Property(e => e.IsRecurring).HasDefaultValue(false);
        });
    }
}

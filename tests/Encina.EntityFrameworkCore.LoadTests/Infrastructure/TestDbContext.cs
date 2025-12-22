using Microsoft.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Inbox;
using Encina.EntityFrameworkCore.Outbox;
using Encina.EntityFrameworkCore.Sagas;
using Encina.EntityFrameworkCore.Scheduling;

namespace Encina.EntityFrameworkCore.LoadTests;

/// <summary>
/// Test database context for load testing EntityFrameworkCore stores.
/// Configures in-memory database with all messaging entities.
/// </summary>
internal sealed class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Outbox
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NotificationType).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.HasIndex(e => new { e.ProcessedAtUtc, e.NextRetryAtUtc });
        });

        // Configure Inbox
        modelBuilder.Entity<InboxMessage>(entity =>
        {
            entity.HasKey(e => e.MessageId);
            entity.Property(e => e.RequestType).IsRequired();
            entity.HasIndex(e => e.ExpiresAtUtc);
            entity.HasIndex(e => new { e.ProcessedAtUtc, e.NextRetryAtUtc });
        });

        // Configure Sagas
        modelBuilder.Entity<SagaState>(entity =>
        {
            entity.HasKey(e => e.SagaId);
            entity.Property(e => e.SagaType).IsRequired();
            entity.Property(e => e.Data).IsRequired();
            entity.HasIndex(e => new { e.Status, e.LastUpdatedAtUtc });
        });

        // Configure Scheduling
        modelBuilder.Entity<ScheduledMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RequestType).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.HasIndex(e => new { e.ScheduledAtUtc, e.ProcessedAtUtc });
            entity.HasIndex(e => new { e.ProcessedAtUtc, e.NextRetryAtUtc });
        });
    }
}

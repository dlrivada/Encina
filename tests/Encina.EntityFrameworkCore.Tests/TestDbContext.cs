using Microsoft.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Inbox;
using Encina.EntityFrameworkCore.Outbox;
using Encina.EntityFrameworkCore.Sagas;
using Encina.EntityFrameworkCore.Scheduling;

namespace Encina.EntityFrameworkCore.Tests;

/// <summary>
/// Test DbContext with all messaging patterns configured.
/// </summary>
public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<SagaState> SagaStates => Set<SagaState>();
    public DbSet<ScheduledMessage> ScheduledMessages => Set<ScheduledMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new SagaStateConfiguration());
        modelBuilder.ApplyConfiguration(new ScheduledMessageConfiguration());
    }
}

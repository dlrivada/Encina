using Microsoft.EntityFrameworkCore;
using SimpleMediator.EntityFrameworkCore.Inbox;
using SimpleMediator.EntityFrameworkCore.Outbox;
using SimpleMediator.EntityFrameworkCore.Sagas;
using SimpleMediator.EntityFrameworkCore.Scheduling;

namespace SimpleMediator.EntityFrameworkCore.Tests;

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

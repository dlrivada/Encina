using Encina.EntityFrameworkCore.Inbox;
using Encina.EntityFrameworkCore.Outbox;
using Encina.EntityFrameworkCore.Sagas;
using Encina.EntityFrameworkCore.Scheduling;
using Microsoft.EntityFrameworkCore;

namespace Encina.ContractTests.EntityFrameworkCore;

/// <summary>
/// Shared DbContext for EF Core contract tests, configuring all messaging entity types.
/// </summary>
public sealed class ContractTestDbContext : DbContext
{
    public ContractTestDbContext(DbContextOptions<ContractTestDbContext> options) : base(options)
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

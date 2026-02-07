using Encina.EntityFrameworkCore.Inbox;
using Encina.EntityFrameworkCore.Outbox;
using Encina.EntityFrameworkCore.Sagas;
using Encina.EntityFrameworkCore.Scheduling;
using Encina.TestInfrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.PostgreSQL;

/// <summary>
/// PostgreSQL-specific test DbContext that uses lowercase table names.
/// </summary>
/// <remarks>
/// PostgreSQL folds unquoted identifiers to lowercase. To ensure consistency between
/// EF Core (which quotes identifiers) and Dapper (which uses unquoted identifiers),
/// we configure all table and column names to be lowercase.
/// </remarks>
public sealed class TestPostgreSqlDbContext : DbContext
{
    public TestPostgreSqlDbContext(DbContextOptions<TestPostgreSqlDbContext> options)
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
        // Outbox configuration - lowercase for PostgreSQL
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outboxmessages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.NotificationType).HasColumnName("notificationtype").HasMaxLength(500).IsRequired();
            entity.Property(e => e.Content).HasColumnName("content").IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("createdatutc").IsRequired();
            entity.Property(e => e.ProcessedAtUtc).HasColumnName("processedatutc");
            entity.Property(e => e.ErrorMessage).HasColumnName("errormessage");
            entity.Property(e => e.RetryCount).HasColumnName("retrycount").IsRequired();
            entity.Property(e => e.NextRetryAtUtc).HasColumnName("nextretryatutc");
            entity.HasIndex(e => new { e.ProcessedAtUtc, e.NextRetryAtUtc })
                .HasDatabaseName("ix_outboxmessages_processedatutc_nextretryatutc");
        });

        // Inbox configuration - lowercase for PostgreSQL
        modelBuilder.Entity<InboxMessage>(entity =>
        {
            entity.ToTable("inboxmessages");
            entity.HasKey(e => e.MessageId);
            entity.Property(e => e.MessageId).HasColumnName("messageid").HasMaxLength(256).IsRequired();
            entity.Property(e => e.RequestType).HasColumnName("requesttype").HasMaxLength(500).IsRequired();
            entity.Property(e => e.ReceivedAtUtc).HasColumnName("receivedatutc").IsRequired();
            entity.Property(e => e.ProcessedAtUtc).HasColumnName("processedatutc");
            entity.Property(e => e.Response).HasColumnName("response");
            entity.Property(e => e.ErrorMessage).HasColumnName("errormessage");
            entity.Property(e => e.RetryCount).HasColumnName("retrycount").IsRequired();
            entity.Property(e => e.NextRetryAtUtc).HasColumnName("nextretryatutc");
            entity.Property(e => e.ExpiresAtUtc).HasColumnName("expiresatutc").IsRequired();
            entity.Property(e => e.Metadata).HasColumnName("metadata");
            entity.HasIndex(e => e.ExpiresAtUtc).HasDatabaseName("ix_inboxmessages_expiresatutc");
        });

        // Saga configuration - lowercase for PostgreSQL
        modelBuilder.Entity<SagaState>(entity =>
        {
            entity.ToTable("sagastates");
            entity.HasKey(e => e.SagaId);
            entity.Property(e => e.SagaId).HasColumnName("sagaid");
            entity.Property(e => e.SagaType).HasColumnName("sagatype").HasMaxLength(500).IsRequired();
            entity.Property(e => e.CurrentStep).HasColumnName("currentstep").IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").IsRequired().HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Data).HasColumnName("data").IsRequired();
            entity.Property(e => e.StartedAtUtc).HasColumnName("startedatutc").IsRequired();
            entity.Property(e => e.LastUpdatedAtUtc).HasColumnName("lastupdatedatutc").IsRequired();
            entity.Property(e => e.CompletedAtUtc).HasColumnName("completedatutc");
            entity.Property(e => e.ErrorMessage).HasColumnName("errormessage");
            entity.Property(e => e.TimeoutAtUtc).HasColumnName("timeoutatutc");
            entity.Property(e => e.CorrelationId).HasColumnName("correlationid");
            entity.Property(e => e.Metadata).HasColumnName("metadata");
            entity.HasIndex(e => new { e.Status, e.LastUpdatedAtUtc })
                .HasDatabaseName("ix_sagastates_status_lastupdatedatutc");
        });

        // Scheduling configuration - lowercase for PostgreSQL
        modelBuilder.Entity<ScheduledMessage>(entity =>
        {
            entity.ToTable("scheduledmessages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RequestType).HasColumnName("requesttype").HasMaxLength(500).IsRequired();
            entity.Property(e => e.Content).HasColumnName("content").IsRequired();
            entity.Property(e => e.ScheduledAtUtc).HasColumnName("scheduledatutc").IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("createdatutc").IsRequired();
            entity.Property(e => e.ProcessedAtUtc).HasColumnName("processedatutc");
            entity.Property(e => e.LastExecutedAtUtc).HasColumnName("lastexecutedatutc");
            entity.Property(e => e.ErrorMessage).HasColumnName("errormessage");
            entity.Property(e => e.RetryCount).HasColumnName("retrycount").IsRequired();
            entity.Property(e => e.NextRetryAtUtc).HasColumnName("nextretryatutc");
            entity.Property(e => e.CorrelationId).HasColumnName("correlationid");
            entity.Property(e => e.Metadata).HasColumnName("metadata");
            entity.Property(e => e.IsRecurring).HasColumnName("isrecurring").IsRequired();
            entity.Property(e => e.CronExpression).HasColumnName("cronexpression").HasMaxLength(200);
            entity.HasIndex(e => new { e.ScheduledAtUtc, e.ProcessedAtUtc })
                .HasDatabaseName("ix_scheduledmessages_scheduledatutc_processedatutc");
        });

        // TestRepositoryEntity - lowercase for PostgreSQL
        modelBuilder.Entity<TestRepositoryEntity>(entity =>
        {
            entity.ToTable("testrepositoryentities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.Amount).HasColumnName("amount").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.IsActive).HasColumnName("isactive").IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("createdatutc").IsRequired();
            entity.HasIndex(e => e.IsActive).HasDatabaseName("ix_testrepositoryentities_isactive");
        });

        // TestImmutableOrder - lowercase for PostgreSQL
        modelBuilder.Entity<TestImmutableOrder>(entity =>
        {
            entity.ToTable("orders");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.CustomerName).HasColumnName("customername").HasMaxLength(200).IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
            entity.Ignore(e => e.DomainEvents);
            entity.Ignore(e => e.RowVersion);
        });
    }
}

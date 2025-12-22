using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Encina.EntityFrameworkCore.Scheduling;

/// <summary>
/// Entity Framework Core configuration for <see cref="ScheduledMessage"/>.
/// </summary>
public sealed class ScheduledMessageConfiguration : IEntityTypeConfiguration<ScheduledMessage>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<ScheduledMessage> builder)
    {
        builder.ToTable("ScheduledMessages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RequestType)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Content)
            .IsRequired();

        builder.Property(x => x.ScheduledAtUtc)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.ProcessedAtUtc)
            .IsRequired(false);

        builder.Property(x => x.ErrorMessage)
            .IsRequired(false);

        builder.Property(x => x.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.NextRetryAtUtc)
            .IsRequired(false);

        builder.Property(x => x.CorrelationId)
            .IsRequired(false)
            .HasMaxLength(255);

        builder.Property(x => x.Metadata)
            .IsRequired(false);

        builder.Property(x => x.IsRecurring)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.CronExpression)
            .IsRequired(false)
            .HasMaxLength(255);

        builder.Property(x => x.LastExecutedAtUtc)
            .IsRequired(false);

        // Index for efficiently finding messages due for execution
        builder.HasIndex(x => new { x.ProcessedAtUtc, x.ScheduledAtUtc })
            .HasDatabaseName("IX_ScheduledMessages_Execution");

        // Index for correlation ID lookups
        builder.HasIndex(x => x.CorrelationId)
            .HasFilter("CorrelationId IS NOT NULL")
            .HasDatabaseName("IX_ScheduledMessages_CorrelationId");

        // Index for finding recurring messages
        builder.HasIndex(x => x.IsRecurring)
            .HasFilter("IsRecurring = 1")
            .HasDatabaseName("IX_ScheduledMessages_Recurring");
    }
}

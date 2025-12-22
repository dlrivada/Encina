using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Encina.EntityFrameworkCore.Outbox;

/// <summary>
/// Entity Framework Core configuration for <see cref="OutboxMessage"/>.
/// </summary>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.NotificationType)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Content)
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

        // Index for efficient outbox processing queries
        builder.HasIndex(x => new { x.ProcessedAtUtc, x.NextRetryAtUtc })
            .HasDatabaseName("IX_OutboxMessages_Processing");

        // Index for finding failed messages
        builder.HasIndex(x => new { x.RetryCount, x.ErrorMessage })
            .HasDatabaseName("IX_OutboxMessages_Failed");
    }
}

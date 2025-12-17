using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SimpleMediator.EntityFrameworkCore.Inbox;

/// <summary>
/// Entity Framework Core configuration for <see cref="InboxMessage"/>.
/// </summary>
public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("InboxMessages");

        builder.HasKey(x => x.MessageId);

        builder.Property(x => x.MessageId)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.RequestType)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.ReceivedAtUtc)
            .IsRequired();

        builder.Property(x => x.ProcessedAtUtc)
            .IsRequired(false);

        builder.Property(x => x.Response)
            .IsRequired(false);

        builder.Property(x => x.ErrorMessage)
            .IsRequired(false);

        builder.Property(x => x.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.NextRetryAtUtc)
            .IsRequired(false);

        builder.Property(x => x.Metadata)
            .IsRequired(false);

        builder.Property(x => x.ExpiresAtUtc)
            .IsRequired();

        // Index for efficient inbox lookups by message ID
        builder.HasIndex(x => x.MessageId)
            .IsUnique()
            .HasDatabaseName("IX_InboxMessages_MessageId");

        // Index for finding messages to process
        builder.HasIndex(x => new { x.ProcessedAtUtc, x.ReceivedAtUtc })
            .HasDatabaseName("IX_InboxMessages_Processing");

        // Index for purging expired messages
        builder.HasIndex(x => x.ExpiresAtUtc)
            .HasFilter("ExpiresAtUtc IS NOT NULL")
            .HasDatabaseName("IX_InboxMessages_Expiration");
    }
}

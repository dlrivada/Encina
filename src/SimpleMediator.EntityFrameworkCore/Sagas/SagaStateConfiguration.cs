using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SimpleMediator.EntityFrameworkCore.Sagas;

/// <summary>
/// Entity Framework Core configuration for <see cref="SagaState"/>.
/// </summary>
public sealed class SagaStateConfiguration : IEntityTypeConfiguration<SagaState>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<SagaState> builder)
    {
        builder.ToTable("SagaStates");

        builder.HasKey(x => x.SagaId);

        builder.Property(x => x.SagaType)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Data)
            .IsRequired();

        builder.Property(x => x.CurrentStep)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.StartedAtUtc)
            .IsRequired();

        builder.Property(x => x.LastUpdatedAtUtc)
            .IsRequired();

        builder.Property(x => x.CompletedAtUtc)
            .IsRequired(false);

        builder.Property(x => x.ErrorMessage)
            .IsRequired(false);

        builder.Property(x => x.CorrelationId)
            .IsRequired(false)
            .HasMaxLength(255);

        builder.Property(x => x.TimeoutAtUtc)
            .IsRequired(false);

        builder.Property(x => x.Metadata)
            .IsRequired(false);

        // Index for finding running sagas
        builder.HasIndex(x => new { x.Status, x.LastUpdatedAtUtc })
            .HasDatabaseName("IX_SagaStates_Status");

        // Index for timeout monitoring
        builder.HasIndex(x => x.TimeoutAtUtc)
            .HasFilter("TimeoutAtUtc IS NOT NULL AND Status = 'Running'")
            .HasDatabaseName("IX_SagaStates_Timeout");

        // Index for correlation ID lookups
        builder.HasIndex(x => x.CorrelationId)
            .HasFilter("CorrelationId IS NOT NULL")
            .HasDatabaseName("IX_SagaStates_CorrelationId");
    }
}

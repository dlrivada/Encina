using Encina.Compliance.DataSubjectRights;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Encina.EntityFrameworkCore.DataSubjectRights;

/// <summary>
/// EF Core entity configuration for <see cref="DSRAuditEntryEntity"/>.
/// </summary>
public sealed class DSRAuditEntryEntityConfiguration : IEntityTypeConfiguration<DSRAuditEntryEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DSRAuditEntryEntity> builder)
    {
        builder.ToTable("DSRAuditEntries");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).IsRequired().HasMaxLength(36);
        builder.Property(x => x.DSRRequestId).IsRequired().HasMaxLength(36);
        builder.Property(x => x.Action).IsRequired().HasMaxLength(256);
        builder.Property(x => x.Detail).HasMaxLength(2048);
        builder.Property(x => x.PerformedByUserId).HasMaxLength(256);
        builder.Property(x => x.OccurredAtUtc).IsRequired();

        builder.HasIndex(x => x.DSRRequestId);
        builder.HasIndex(x => x.OccurredAtUtc);
    }
}

using Encina.Compliance.DataSubjectRights;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Encina.EntityFrameworkCore.DataSubjectRights;

/// <summary>
/// EF Core entity configuration for <see cref="DSRRequestEntity"/>.
/// </summary>
public sealed class DSRRequestEntityConfiguration : IEntityTypeConfiguration<DSRRequestEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DSRRequestEntity> builder)
    {
        builder.ToTable("DSRRequests");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).IsRequired().HasMaxLength(36);
        builder.Property(x => x.SubjectId).IsRequired().HasMaxLength(256);
        builder.Property(x => x.RightTypeValue).IsRequired();
        builder.Property(x => x.StatusValue).IsRequired();
        builder.Property(x => x.ReceivedAtUtc).IsRequired();
        builder.Property(x => x.DeadlineAtUtc).IsRequired();
        builder.Property(x => x.ExtensionReason).HasMaxLength(1024);
        builder.Property(x => x.RejectionReason).HasMaxLength(1024);
        builder.Property(x => x.ProcessedByUserId).HasMaxLength(256);

        builder.HasIndex(x => x.SubjectId);
        builder.HasIndex(x => x.StatusValue);
        builder.HasIndex(x => x.RightTypeValue);
        builder.HasIndex(x => x.DeadlineAtUtc);
    }
}

using Encina.Compliance.ProcessorAgreements;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Encina.EntityFrameworkCore.ProcessorAgreements;

/// <summary>
/// EF Core configuration for <see cref="ProcessorEntity"/>.
/// </summary>
/// <remarks>
/// <para>
/// Maps to the <c>Processors</c> table with indexes on <c>ParentProcessorId</c>
/// and <c>TenantId</c> to support efficient hierarchy traversal and multi-tenancy queries.
/// </para>
/// <para>
/// The <see cref="ProcessorEntity.SubProcessorAuthorizationTypeValue"/> column stores the
/// <see cref="Model.SubProcessorAuthorizationType"/> enum as an integer for cross-provider compatibility.
/// </para>
/// </remarks>
internal sealed class ProcessorEntityConfiguration : IEntityTypeConfiguration<ProcessorEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<ProcessorEntity> builder)
    {
        builder.ToTable("Processors");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(x => x.Country)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.ContactEmail)
            .IsRequired(false)
            .HasMaxLength(256);

        builder.Property(x => x.ParentProcessorId)
            .IsRequired(false)
            .HasMaxLength(256);

        builder.Property(x => x.Depth)
            .IsRequired();

        builder.Property(x => x.SubProcessorAuthorizationTypeValue)
            .IsRequired();

        builder.Property(x => x.TenantId)
            .IsRequired(false)
            .HasMaxLength(256);

        builder.Property(x => x.ModuleId)
            .IsRequired(false)
            .HasMaxLength(256);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.LastUpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.ParentProcessorId)
            .HasDatabaseName("IX_Processors_ParentProcessorId");

        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("IX_Processors_TenantId");
    }
}

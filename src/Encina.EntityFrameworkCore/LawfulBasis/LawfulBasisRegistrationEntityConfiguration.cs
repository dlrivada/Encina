using Encina.Compliance.GDPR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Encina.EntityFrameworkCore.LawfulBasis;

/// <summary>
/// EF Core configuration for <see cref="LawfulBasisRegistrationEntity"/>.
/// </summary>
public sealed class LawfulBasisRegistrationEntityConfiguration : IEntityTypeConfiguration<LawfulBasisRegistrationEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<LawfulBasisRegistrationEntity> builder)
    {
        builder.ToTable("LawfulBasisRegistrations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired()
            .HasMaxLength(36);

        builder.Property(x => x.RequestTypeName)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(x => x.BasisValue)
            .IsRequired();

        builder.Property(x => x.Purpose)
            .IsRequired(false)
            .HasMaxLength(1024);

        builder.Property(x => x.LIAReference)
            .IsRequired(false)
            .HasMaxLength(256);

        builder.Property(x => x.LegalReference)
            .IsRequired(false)
            .HasMaxLength(256);

        builder.Property(x => x.ContractReference)
            .IsRequired(false)
            .HasMaxLength(256);

        builder.Property(x => x.RegisteredAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.RequestTypeName)
            .IsUnique()
            .HasDatabaseName("IX_LawfulBasisRegistrations_RequestTypeName");
    }
}

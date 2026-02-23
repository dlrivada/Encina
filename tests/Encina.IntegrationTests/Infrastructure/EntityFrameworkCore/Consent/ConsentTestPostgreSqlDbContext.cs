using Encina.Compliance.Consent;
using Encina.EntityFrameworkCore.Consent;
using Microsoft.EntityFrameworkCore;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.Consent;

/// <summary>
/// PostgreSQL-specific test DbContext for consent integration tests.
/// </summary>
/// <remarks>
/// PostgreSQL folds unquoted identifiers to lowercase. To ensure consistency between
/// EF Core (which quotes identifiers) and Dapper/ADO (which use unquoted identifiers),
/// we configure all table and column names to be lowercase.
/// This follows the same pattern as <see cref="PostgreSQL.TestPostgreSqlDbContext"/>.
/// </remarks>
public sealed class ConsentTestPostgreSqlDbContext : DbContext
{
    public ConsentTestPostgreSqlDbContext(DbContextOptions<ConsentTestPostgreSqlDbContext> options)
        : base(options)
    {
    }

    public DbSet<ConsentRecordEntity> ConsentRecords => Set<ConsentRecordEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConsentRecordEntity>(entity =>
        {
            entity.ToTable("consentrecords");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SubjectId).HasColumnName("subjectid").IsRequired().HasMaxLength(256);
            entity.Property(e => e.Purpose).HasColumnName("purpose").IsRequired().HasMaxLength(256);
            entity.Property(e => e.Status).HasColumnName("status").IsRequired();
            entity.Property(e => e.ConsentVersionId).HasColumnName("consentversionid").IsRequired().HasMaxLength(256);
            entity.Property(e => e.GivenAtUtc).HasColumnName("givenatutc").IsRequired();
            entity.Property(e => e.WithdrawnAtUtc).HasColumnName("withdrawnatutc").IsRequired(false);
            entity.Property(e => e.ExpiresAtUtc).HasColumnName("expiresatutc").IsRequired(false);
            entity.Property(e => e.Source).HasColumnName("source").IsRequired().HasMaxLength(256);
            entity.Property(e => e.IpAddress).HasColumnName("ipaddress").IsRequired(false).HasMaxLength(45);
            entity.Property(e => e.ProofOfConsent).HasColumnName("proofofconsent").IsRequired(false);
            entity.Property(e => e.Metadata).HasColumnName("metadata").IsRequired(false);

            entity.HasIndex(e => e.SubjectId).HasDatabaseName("ix_consentrecords_subjectid");
            entity.HasIndex(e => new { e.SubjectId, e.Purpose }).HasDatabaseName("ix_consentrecords_subjectid_purpose");
            entity.HasIndex(e => e.Status).HasDatabaseName("ix_consentrecords_status");
        });
    }
}

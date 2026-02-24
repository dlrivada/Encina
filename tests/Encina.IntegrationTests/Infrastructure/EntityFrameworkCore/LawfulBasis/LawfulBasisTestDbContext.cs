using Encina.Compliance.GDPR;
using Encina.EntityFrameworkCore.LawfulBasis;
using Microsoft.EntityFrameworkCore;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.LawfulBasis;

/// <summary>
/// Test DbContext for EF Core lawful basis integration tests.
/// Includes both lawful basis registration and LIA record entities.
/// </summary>
public sealed class LawfulBasisTestDbContext : DbContext
{
    public LawfulBasisTestDbContext(DbContextOptions<LawfulBasisTestDbContext> options)
        : base(options)
    {
    }

    public DbSet<LawfulBasisRegistrationEntity> LawfulBasisRegistrations => Set<LawfulBasisRegistrationEntity>();

    public DbSet<LIARecordEntity> LIARecords => Set<LIARecordEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyLawfulBasisConfiguration();
    }
}

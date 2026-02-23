using Encina.EntityFrameworkCore.Consent;
using Microsoft.EntityFrameworkCore;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.Consent;

/// <summary>
/// Test DbContext for EF Core consent integration tests.
/// Includes only consent-related entities for focused testing.
/// </summary>
public sealed class ConsentTestDbContext : DbContext
{
    public ConsentTestDbContext(DbContextOptions<ConsentTestDbContext> options)
        : base(options)
    {
    }

    public DbSet<ConsentRecordEntity> ConsentRecords => Set<ConsentRecordEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConsentConfiguration();
    }
}

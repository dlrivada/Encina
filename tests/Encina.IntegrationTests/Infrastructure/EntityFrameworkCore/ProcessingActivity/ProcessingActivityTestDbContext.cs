using Encina.Compliance.GDPR;
using Encina.EntityFrameworkCore.ProcessingActivity;
using Microsoft.EntityFrameworkCore;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.ProcessingActivity;

/// <summary>
/// Test <see cref="DbContext"/> for processing activity EF Core integration tests.
/// </summary>
public sealed class ProcessingActivityTestDbContext : DbContext
{
    public ProcessingActivityTestDbContext(DbContextOptions<ProcessingActivityTestDbContext> options)
        : base(options)
    {
    }

    public DbSet<ProcessingActivityEntity> ProcessingActivities => Set<ProcessingActivityEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyProcessingActivityConfiguration();
    }
}

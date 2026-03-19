using Encina.EntityFrameworkCore.Auditing;
using Encina.Security.Audit;
using Microsoft.EntityFrameworkCore;

namespace Encina.IntegrationTests.Security.Audit.EFCore;

/// <summary>
/// Test DbContext for EF Core audit store integration tests.
/// Includes AuditEntryEntity and ReadAuditEntryEntity for both store types.
/// </summary>
public sealed class AuditTestDbContext : DbContext
{
    public AuditTestDbContext(DbContextOptions<AuditTestDbContext> options)
        : base(options)
    {
    }

    public DbSet<AuditEntryEntity> SecurityAuditEntries => Set<AuditEntryEntity>();
    public DbSet<ReadAuditEntryEntity> ReadAuditEntries => Set<ReadAuditEntryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AuditEntryEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ReadAuditEntryEntityConfiguration());
    }
}

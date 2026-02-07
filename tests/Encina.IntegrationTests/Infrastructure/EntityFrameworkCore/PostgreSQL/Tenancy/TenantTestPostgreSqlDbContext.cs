using Encina.EntityFrameworkCore.Tenancy;
using Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.Tenancy;
using Encina.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.PostgreSQL.Tenancy;

/// <summary>
/// PostgreSQL-specific test DbContext with multi-tenancy support.
/// Uses lowercase table and column names to match PostgreSQL identifier folding behavior.
/// </summary>
public sealed class TenantTestPostgreSqlDbContext : TenantDbContext
{
    public TenantTestPostgreSqlDbContext(
        DbContextOptions<TenantTestPostgreSqlDbContext> options,
        ITenantProvider tenantProvider,
        IOptions<EfCoreTenancyOptions> tenancyOptions,
        IOptions<TenancyOptions> coreOptions)
        : base(options, tenantProvider, tenancyOptions, coreOptions)
    {
    }

    public DbSet<TenantTestEntity> TenantTestEntities => Set<TenantTestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // Apply tenant filters

        modelBuilder.Entity<TenantTestEntity>(entity =>
        {
            entity.ToTable("tenanttestentities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.TenantId).HasColumnName("tenantid").HasMaxLength(128).IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(1000);
            entity.Property(e => e.Amount).HasColumnName("amount").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.IsActive).HasColumnName("isactive").IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("createdatutc").IsRequired();
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.IsActive });
        });
    }
}

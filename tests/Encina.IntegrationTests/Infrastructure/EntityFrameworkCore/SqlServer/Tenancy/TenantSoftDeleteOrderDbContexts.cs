using Encina.EntityFrameworkCore.Configuration;
using Encina.EntityFrameworkCore.Tenancy;
using Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.Tenancy;
using Encina.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.SqlServer.Tenancy;

/// <summary>
/// Applies the tenant filter first (via <c>base.OnModelCreating</c>, the normal
/// <see cref="TenantDbContext"/> flow) and the soft-delete filter afterward. This mirrors the
/// order described in the #1268 repro steps.
/// </summary>
public sealed class TenantThenSoftDeleteSqlServerDbContext : TenantDbContext
{
    public TenantThenSoftDeleteSqlServerDbContext(
        DbContextOptions<TenantThenSoftDeleteSqlServerDbContext> options,
        ITenantProvider tenantProvider,
        IOptions<EfCoreTenancyOptions> tenancyOptions,
        IOptions<TenancyOptions> coreOptions)
        : base(options, tenantProvider, tenancyOptions, coreOptions)
    {
    }

    public DbSet<TenantSoftDeleteTestEntity> TenantSoftDeleteTestEntities => Set<TenantSoftDeleteTestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // Applies the "Encina.Tenancy" named filter.

        modelBuilder.Entity<TenantSoftDeleteTestEntity>(entity =>
        {
            entity.ToTable("TenantSoftDeleteTestEntities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.TenantId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.IsDeleted).IsRequired();
            entity.Property(e => e.DeletedAtUtc);
            entity.Property(e => e.DeletedBy).HasMaxLength(128);
        });

        modelBuilder.ApplySoftDeleteQueryFilters(); // Applies the "Encina.SoftDelete" named filter.
    }
}

/// <summary>
/// Applies the soft-delete filter first and the tenant filter afterward — the reverse order of
/// <see cref="TenantThenSoftDeleteSqlServerDbContext"/> — to prove that the two named filters
/// coexist regardless of application order.
/// </summary>
public sealed class SoftDeleteThenTenantSqlServerDbContext : TenantDbContext
{
    public SoftDeleteThenTenantSqlServerDbContext(
        DbContextOptions<SoftDeleteThenTenantSqlServerDbContext> options,
        ITenantProvider tenantProvider,
        IOptions<EfCoreTenancyOptions> tenancyOptions,
        IOptions<TenancyOptions> coreOptions)
        : base(options, tenantProvider, tenancyOptions, coreOptions)
    {
    }

    public DbSet<TenantSoftDeleteTestEntity> TenantSoftDeleteTestEntities => Set<TenantSoftDeleteTestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantSoftDeleteTestEntity>(entity =>
        {
            entity.ToTable("TenantSoftDeleteTestEntities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.TenantId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.IsDeleted).IsRequired();
            entity.Property(e => e.DeletedAtUtc);
            entity.Property(e => e.DeletedBy).HasMaxLength(128);
        });

        modelBuilder.ApplySoftDeleteQueryFilters(); // Applies the "Encina.SoftDelete" named filter first.

        base.OnModelCreating(modelBuilder); // Applies the "Encina.Tenancy" named filter afterward.
    }
}

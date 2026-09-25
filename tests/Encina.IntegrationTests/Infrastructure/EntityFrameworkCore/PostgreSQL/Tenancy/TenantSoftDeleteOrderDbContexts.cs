using Encina.EntityFrameworkCore.Configuration;
using Encina.EntityFrameworkCore.Tenancy;
using Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.Tenancy;
using Encina.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.PostgreSQL.Tenancy;

/// <summary>
/// Applies the tenant filter first (via <c>base.OnModelCreating</c>, the normal
/// <see cref="TenantDbContext"/> flow) and the soft-delete filter afterward. This mirrors the
/// order described in the #1268 repro steps.
/// Uses lowercase table and column names to match PostgreSQL identifier folding behavior.
/// </summary>
public sealed class TenantThenSoftDeletePostgreSqlDbContext : TenantDbContext
{
    public TenantThenSoftDeletePostgreSqlDbContext(
        DbContextOptions<TenantThenSoftDeletePostgreSqlDbContext> options,
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

        ConfigureEntity(modelBuilder);

        modelBuilder.ApplySoftDeleteQueryFilters(); // Applies the "Encina.SoftDelete" named filter.
    }

    private static void ConfigureEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantSoftDeleteTestEntity>(entity =>
        {
            entity.ToTable("tenantsoftdeletetestentities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.TenantId).HasColumnName("tenantid").HasMaxLength(128).IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.IsDeleted).HasColumnName("isdeleted").IsRequired();
            entity.Property(e => e.DeletedAtUtc).HasColumnName("deletedatutc");
            entity.Property(e => e.DeletedBy).HasColumnName("deletedby").HasMaxLength(128);
        });
    }
}

/// <summary>
/// Applies the soft-delete filter first and the tenant filter afterward — the reverse order of
/// <see cref="TenantThenSoftDeletePostgreSqlDbContext"/> — to prove that the two named filters
/// coexist regardless of application order.
/// Uses lowercase table and column names to match PostgreSQL identifier folding behavior.
/// </summary>
public sealed class SoftDeleteThenTenantPostgreSqlDbContext : TenantDbContext
{
    public SoftDeleteThenTenantPostgreSqlDbContext(
        DbContextOptions<SoftDeleteThenTenantPostgreSqlDbContext> options,
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
            entity.ToTable("tenantsoftdeletetestentities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.TenantId).HasColumnName("tenantid").HasMaxLength(128).IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.IsDeleted).HasColumnName("isdeleted").IsRequired();
            entity.Property(e => e.DeletedAtUtc).HasColumnName("deletedatutc");
            entity.Property(e => e.DeletedBy).HasColumnName("deletedby").HasMaxLength(128);
        });

        modelBuilder.ApplySoftDeleteQueryFilters(); // Applies the "Encina.SoftDelete" named filter first.

        base.OnModelCreating(modelBuilder); // Applies the "Encina.Tenancy" named filter afterward.
    }
}

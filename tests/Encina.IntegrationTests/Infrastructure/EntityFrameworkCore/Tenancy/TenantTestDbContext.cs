using Encina.EntityFrameworkCore.Tenancy;
using Encina.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.Tenancy;

/// <summary>
/// Test DbContext with multi-tenancy support for EF Core integration tests.
/// Inherits from TenantDbContext to get automatic query filters and tenant assignment.
/// </summary>
public sealed class TenantTestDbContext : TenantDbContext
{
    public TenantTestDbContext(
        DbContextOptions<TenantTestDbContext> options,
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
            entity.ToTable("TenantTestEntities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.TenantId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Amount).HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CreatedAtUtc).IsRequired();
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.IsActive });
        });
    }
}

/// <summary>
/// Test entity for multi-tenancy integration tests.
/// Implements ITenantEntity for automatic tenant filtering and assignment.
/// </summary>
public class TenantTestEntity : ITenantEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// Simple tenant provider for testing purposes.
/// Allows setting the current tenant ID programmatically.
/// </summary>
public sealed class TestTenantProvider : ITenantProvider
{
    private string? _currentTenantId;
    private TenantInfo? _currentTenantInfo;

    public void SetTenant(string tenantId)
    {
        _currentTenantId = tenantId;
        _currentTenantInfo = new TenantInfo(
            TenantId: tenantId,
            Name: $"Tenant {tenantId}",
            Strategy: TenantIsolationStrategy.SharedSchema);
    }

    public void ClearTenant()
    {
        _currentTenantId = null;
        _currentTenantInfo = null;
    }

    public string? GetCurrentTenantId() => _currentTenantId;

    public ValueTask<TenantInfo?> GetCurrentTenantAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(_currentTenantInfo);

    /// <summary>
    /// Gets tenant information by tenant ID. Uses the current tenant if it matches.
    /// </summary>
    public ValueTask<TenantInfo?> GetTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(tenantId))
            return ValueTask.FromResult<TenantInfo?>(null);

        // Return the cached tenant info if it matches the requested ID
        if (_currentTenantId == tenantId && _currentTenantInfo is not null)
            return ValueTask.FromResult<TenantInfo?>(_currentTenantInfo);

        return ValueTask.FromResult<TenantInfo?>(new TenantInfo(
            TenantId: tenantId,
            Name: $"Tenant {tenantId}",
            Strategy: TenantIsolationStrategy.SharedSchema));
    }
}

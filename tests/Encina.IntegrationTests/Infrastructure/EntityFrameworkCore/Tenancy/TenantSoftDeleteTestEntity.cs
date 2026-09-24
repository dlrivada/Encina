using Encina.DomainModeling;
using Encina.Tenancy;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.Tenancy;

/// <summary>
/// Test entity implementing both <see cref="ITenantEntity"/> and <see cref="ISoftDeletable"/>,
/// used by the #1268 integration tests to verify that <c>TenantDbContext.ApplyTenantQueryFilters</c>
/// and <c>EntityConfigurationExtensions.ApplySoftDeleteQueryFilters</c> apply as two independent
/// named EF Core query filters instead of one overwriting the other.
/// </summary>
public sealed class TenantSoftDeleteTestEntity : ITenantEntity, ISoftDeletable
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
}

using Encina.DomainModeling;
using Encina.Tenancy;

namespace Encina.TestInfrastructure.Entities;

/// <summary>
/// Test entity for multi-tenancy integration tests.
/// Implements both <see cref="IEntity{TId}"/> and <see cref="ITenantEntity"/>
/// to enable tenant-aware repository testing across all providers.
/// </summary>
/// <remarks>
/// This entity is used in integration tests to verify:
/// <list type="bullet">
/// <item><description>Automatic tenant filter injection in queries</description></item>
/// <item><description>Cross-tenant data isolation</description></item>
/// <item><description>Tenant context switching behavior</description></item>
/// <item><description>Tenant-aware bulk operations</description></item>
/// </list>
/// </remarks>
public sealed class TenantTestEntity : IEntity<Guid>, ITenantEntity
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the tenant identifier for this entity.
    /// </summary>
    public string TenantId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the entity name.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the numeric amount for testing decimal/money operations.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets whether this entity is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the last modification timestamp.
    /// </summary>
    public DateTime? UpdatedAtUtc { get; set; }
}

using Encina.TestInfrastructure.Entities;

namespace Encina.TestInfrastructure.Builders;

/// <summary>
/// Builder for creating <see cref="TenantTestEntity"/> test data.
/// Provides fluent API for constructing test entities with sensible defaults.
/// </summary>
public sealed class TenantTestEntityBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _tenantId = "default-tenant";
    private string _name = "Test Entity";
    private string? _description;
    private decimal _amount;
    private bool _isActive = true;
    private DateTime _createdAtUtc = DateTime.UtcNow;
    private DateTime? _updatedAtUtc;

    /// <summary>
    /// Sets the entity ID.
    /// </summary>
    public TenantTestEntityBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    /// <summary>
    /// Sets the tenant ID.
    /// </summary>
    public TenantTestEntityBuilder WithTenantId(string tenantId)
    {
        _tenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
        return this;
    }

    /// <summary>
    /// Sets the entity name.
    /// </summary>
    public TenantTestEntityBuilder WithName(string name)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        return this;
    }

    /// <summary>
    /// Sets the entity description.
    /// </summary>
    public TenantTestEntityBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Sets the amount value.
    /// </summary>
    public TenantTestEntityBuilder WithAmount(decimal amount)
    {
        _amount = amount;
        return this;
    }

    /// <summary>
    /// Sets the active status.
    /// </summary>
    public TenantTestEntityBuilder WithIsActive(bool isActive)
    {
        _isActive = isActive;
        return this;
    }

    /// <summary>
    /// Marks the entity as active.
    /// </summary>
    public TenantTestEntityBuilder AsActive()
    {
        _isActive = true;
        return this;
    }

    /// <summary>
    /// Marks the entity as inactive.
    /// </summary>
    public TenantTestEntityBuilder AsInactive()
    {
        _isActive = false;
        return this;
    }

    /// <summary>
    /// Sets the creation timestamp.
    /// </summary>
    public TenantTestEntityBuilder WithCreatedAtUtc(DateTime createdAtUtc)
    {
        _createdAtUtc = createdAtUtc;
        return this;
    }

    /// <summary>
    /// Sets the last updated timestamp.
    /// </summary>
    public TenantTestEntityBuilder WithUpdatedAtUtc(DateTime? updatedAtUtc)
    {
        _updatedAtUtc = updatedAtUtc;
        return this;
    }

    /// <summary>
    /// Marks the entity as having been updated.
    /// </summary>
    public TenantTestEntityBuilder AsUpdated(DateTime? updatedAtUtc = null)
    {
        _updatedAtUtc = updatedAtUtc ?? DateTime.UtcNow;
        return this;
    }

    /// <summary>
    /// Builds the <see cref="TenantTestEntity"/> instance.
    /// </summary>
    public TenantTestEntity Build() => new()
    {
        Id = _id,
        TenantId = _tenantId,
        Name = _name,
        Description = _description,
        Amount = _amount,
        IsActive = _isActive,
        CreatedAtUtc = _createdAtUtc,
        UpdatedAtUtc = _updatedAtUtc
    };

    /// <summary>
    /// Creates a new builder instance.
    /// </summary>
    public static TenantTestEntityBuilder Create() => new();

    /// <summary>
    /// Creates a new builder instance with the specified tenant ID.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    public static TenantTestEntityBuilder ForTenant(string tenantId)
        => new TenantTestEntityBuilder().WithTenantId(tenantId);
}

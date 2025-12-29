namespace Encina.DomainModeling;

/// <summary>
/// Interface for entities that track creation and modification audit information.
/// </summary>
/// <remarks>
/// <para>
/// Implementing this interface allows automatic population of audit fields via
/// EF Core SaveChanges interceptors or similar mechanisms.
/// </para>
/// </remarks>
public interface IAuditable
{
    /// <summary>
    /// Gets the timestamp when this entity was created (UTC).
    /// </summary>
    DateTime CreatedAtUtc { get; }

    /// <summary>
    /// Gets the identifier of the user who created this entity.
    /// </summary>
    string? CreatedBy { get; }

    /// <summary>
    /// Gets the timestamp when this entity was last modified (UTC).
    /// </summary>
    DateTime? ModifiedAtUtc { get; }

    /// <summary>
    /// Gets the identifier of the user who last modified this entity.
    /// </summary>
    string? ModifiedBy { get; }
}

/// <summary>
/// Interface for entities that support soft delete.
/// </summary>
/// <remarks>
/// <para>
/// Soft delete keeps the record in the database but marks it as deleted.
/// This is useful for audit trails, data recovery, and GDPR compliance.
/// </para>
/// <para>
/// Query filters should be applied to exclude soft-deleted entities from normal queries.
/// </para>
/// </remarks>
public interface ISoftDeletable
{
    /// <summary>
    /// Gets a value indicating whether this entity has been soft-deleted.
    /// </summary>
    bool IsDeleted { get; }

    /// <summary>
    /// Gets the timestamp when this entity was deleted (UTC).
    /// </summary>
    DateTime? DeletedAtUtc { get; }

    /// <summary>
    /// Gets the identifier of the user who deleted this entity.
    /// </summary>
    string? DeletedBy { get; }
}

/// <summary>
/// Interface for entities that support optimistic concurrency.
/// </summary>
/// <remarks>
/// <para>
/// Optimistic concurrency uses a version token (row version, ETag) to detect conflicts
/// when multiple clients try to update the same record simultaneously.
/// </para>
/// </remarks>
public interface IConcurrencyAware
{
    /// <summary>
    /// Gets the concurrency token (row version) for optimistic concurrency control.
    /// </summary>
    byte[]? RowVersion { get; }
}

/// <summary>
/// Interface for entities that track version numbers.
/// </summary>
/// <remarks>
/// <para>
/// Version numbers are useful for event sourcing, optimistic concurrency with integer versions,
/// and tracking the number of modifications to an entity.
/// </para>
/// </remarks>
public interface IVersioned
{
    /// <summary>
    /// Gets the version number of this entity.
    /// </summary>
    long Version { get; }
}

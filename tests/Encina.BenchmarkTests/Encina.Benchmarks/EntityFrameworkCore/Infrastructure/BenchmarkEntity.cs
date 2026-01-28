using Encina.DomainModeling;

namespace Encina.Benchmarks.EntityFrameworkCore.Infrastructure;

/// <summary>
/// Simple test entity for EntityFrameworkCore benchmarks.
/// Implements <see cref="IEntity{TId}"/> for repository benchmarks.
/// </summary>
/// <remarks>
/// This entity is intentionally simple to focus benchmarks on framework overhead
/// rather than domain complexity. It provides the minimal properties needed to
/// test CRUD operations, tracking behavior, and specification queries.
/// </remarks>
public sealed class BenchmarkEntity : IEntity<Guid>
{
    /// <summary>
    /// Gets or sets the unique identifier for this entity.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the entity.
    /// Used for testing text-based queries and indexes.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a numeric value for the entity.
    /// Used for testing range queries and aggregations.
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// Used for testing date-based queries and ordering.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets an optional category for the entity.
    /// Used for testing nullable fields and filtering.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets whether the entity is active.
    /// Used for testing boolean filters and soft delete patterns.
    /// </summary>
    public bool IsActive { get; set; } = true;
}

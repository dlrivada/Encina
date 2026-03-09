namespace Encina.Security.ABAC.Persistence;

/// <summary>
/// Database entity representing a persisted standalone <see cref="Policy"/> in the <c>abac_policies</c> table.
/// </summary>
/// <remarks>
/// <para>
/// This is a simple POCO used by relational providers (ADO.NET, Dapper, EF Core) to map
/// between database rows and the domain model. The full <see cref="Policy"/> graph —
/// including rules, targets, obligations, advice, variable definitions, and expression trees —
/// is stored as serialized JSON in the <see cref="PolicyJson"/> column.
/// </para>
/// <para>
/// Only standalone policies (those not embedded in a <see cref="PolicySet"/>) are stored
/// in this table. Policies nested within a policy set are serialized as part of the
/// parent <see cref="PolicySetEntity.PolicyJson"/>.
/// </para>
/// <para>
/// Metadata columns (<see cref="Id"/>, <see cref="Version"/>, <see cref="Description"/>,
/// <see cref="IsEnabled"/>, <see cref="Priority"/>) are extracted from the domain model
/// to enable SQL-level filtering and indexing without deserializing the full policy graph.
/// </para>
/// <para>
/// MongoDB implementations use native document storage and do not use this entity.
/// </para>
/// </remarks>
public sealed class PolicyEntity
{
    /// <summary>
    /// The unique identifier of the policy. Maps to <see cref="Policy.Id"/>.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// The optional version of the policy. Maps to <see cref="Policy.Version"/>.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// The optional description of the policy. Maps to <see cref="Policy.Description"/>.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The serialized JSON representation of the complete <see cref="Policy"/> graph,
    /// including all rules, targets, obligations, advice, variable definitions, and expression trees.
    /// </summary>
    public required string PolicyJson { get; set; }

    /// <summary>
    /// Whether the policy is enabled. Maps to <see cref="Policy.IsEnabled"/>.
    /// Extracted for SQL-level filtering.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// The priority of the policy. Maps to <see cref="Policy.Priority"/>.
    /// Extracted for SQL-level ordering.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// The UTC timestamp when the entity was first persisted.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// The UTC timestamp when the entity was last updated.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; }
}

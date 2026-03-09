using Encina.Security.ABAC;
using MongoDB.Bson.Serialization.Attributes;

namespace Encina.MongoDB.ABAC;

/// <summary>
/// MongoDB document wrapper for standalone <see cref="Policy"/> storage.
/// </summary>
/// <remarks>
/// <para>
/// Stores the full <see cref="Policy"/> domain model as a native BSON subdocument,
/// bypassing JSON serialization entirely. Only standalone policies (those not embedded
/// in a <see cref="PolicySet"/>) are stored in this collection.
/// </para>
/// <para>
/// Metadata fields (Id, IsEnabled, Priority, timestamps) are extracted at the document
/// root for efficient MongoDB queries and indexing, while the complete policy graph
/// (rules, targets, conditions, obligations, advice, variables, and expression trees)
/// is stored as native BSON types via registered class maps.
/// </para>
/// </remarks>
internal sealed class PolicyDocument
{
    /// <summary>
    /// The unique identifier of the policy. Maps to <see cref="Policy.Id"/>.
    /// </summary>
    [BsonId]
    [BsonElement("_id")]
    public required string Id { get; set; }

    /// <summary>
    /// Whether the policy is enabled. Extracted for query-level filtering.
    /// </summary>
    [BsonElement("isEnabled")]
    public bool IsEnabled { get; set; }

    /// <summary>
    /// The priority of the policy. Extracted for query-level ordering.
    /// </summary>
    [BsonElement("priority")]
    public int Priority { get; set; }

    /// <summary>
    /// The UTC timestamp when the document was first persisted.
    /// </summary>
    [BsonElement("createdAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// The UTC timestamp when the document was last updated.
    /// </summary>
    [BsonElement("updatedAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>
    /// The complete <see cref="Policy"/> domain model stored as a native BSON subdocument.
    /// </summary>
    [BsonElement("policy")]
    public required Policy Policy { get; set; }
}

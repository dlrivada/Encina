using Encina.Security.ABAC;
using MongoDB.Bson.Serialization.Attributes;

namespace Encina.MongoDB.ABAC;

/// <summary>
/// MongoDB document wrapper for <see cref="PolicySet"/> storage.
/// </summary>
/// <remarks>
/// <para>
/// Stores the full <see cref="PolicySet"/> domain model as a native BSON subdocument,
/// bypassing JSON serialization entirely. Metadata fields (Id, IsEnabled, Priority,
/// timestamps) are extracted at the document root for efficient MongoDB queries and indexing.
/// </para>
/// <para>
/// The <see cref="PolicySet"/> property contains the complete policy graph including
/// nested policies, rules, targets, obligations, advice, and expression trees — all
/// stored as native BSON types via registered class maps.
/// </para>
/// </remarks>
internal sealed class PolicySetDocument
{
    /// <summary>
    /// The unique identifier of the policy set. Maps to <see cref="PolicySet.Id"/>.
    /// </summary>
    [BsonId]
    [BsonElement("_id")]
    public required string Id { get; set; }

    /// <summary>
    /// Whether the policy set is enabled. Extracted for query-level filtering.
    /// </summary>
    [BsonElement("isEnabled")]
    public bool IsEnabled { get; set; }

    /// <summary>
    /// The priority of the policy set. Extracted for query-level ordering.
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
    /// The complete <see cref="PolicySet"/> domain model stored as a native BSON subdocument.
    /// </summary>
    [BsonElement("policySet")]
    public required PolicySet PolicySet { get; set; }
}

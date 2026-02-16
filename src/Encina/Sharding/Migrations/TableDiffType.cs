namespace Encina.Sharding.Migrations;

/// <summary>
/// Describes how a table in one shard differs from the baseline shard.
/// </summary>
public enum TableDiffType
{
    /// <summary>The table exists in the baseline shard but is missing from the compared shard.</summary>
    Missing,

    /// <summary>The table exists in the compared shard but not in the baseline shard.</summary>
    Extra,

    /// <summary>The table exists in both shards but its schema differs (columns, types, constraints).</summary>
    Modified
}

namespace Encina.Sharding.Migrations;

/// <summary>
/// Represents the outcome of a migration operation on an individual shard.
/// </summary>
public enum MigrationOutcome
{
    /// <summary>The migration has not yet started on this shard.</summary>
    Pending,

    /// <summary>The migration is currently being applied to this shard.</summary>
    InProgress,

    /// <summary>The migration was applied successfully on this shard.</summary>
    Succeeded,

    /// <summary>The migration failed on this shard.</summary>
    Failed,

    /// <summary>The migration was rolled back on this shard after a failure.</summary>
    RolledBack
}

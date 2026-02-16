using LanguageExt;

namespace Encina.Sharding.Migrations;

/// <summary>
/// Reads schema metadata from a shard's database for drift detection.
/// </summary>
/// <remarks>
/// <para>
/// Provider-specific implementations query the appropriate system catalog
/// (<c>sqlite_master</c>, <c>INFORMATION_SCHEMA</c>, <c>pg_catalog</c>, etc.)
/// to retrieve the current schema and compare it against a baseline.
/// </para>
/// </remarks>
public interface ISchemaIntrospector
{
    /// <summary>
    /// Compares the schema of a shard against a baseline shard.
    /// </summary>
    /// <param name="shard">The shard to compare.</param>
    /// <param name="baselineShard">The baseline shard to compare against.</param>
    /// <param name="includeColumnDiffs">Whether to include column-level details.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// Right with a <see cref="ShardSchemaDiff"/> describing the differences;
    /// Left with an <see cref="EncinaError"/> if the comparison fails.
    /// </returns>
    Task<Either<EncinaError, ShardSchemaDiff>> CompareAsync(
        ShardInfo shard,
        ShardInfo baselineShard,
        bool includeColumnDiffs,
        CancellationToken cancellationToken = default);
}

using System.Collections.Immutable;
using System.Text.Json;

namespace Encina.DomainModeling.Concurrency;

/// <summary>
/// Contains information about a concurrency conflict, including the entity states involved.
/// </summary>
/// <typeparam name="TEntity">The type of the entity involved in the conflict.</typeparam>
/// <remarks>
/// <para>
/// This record captures the three entity states relevant to a concurrency conflict:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       <b>CurrentEntity</b>: The entity state when it was originally loaded from the database
///       (before any modifications were made in the current operation).
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>ProposedEntity</b>: The entity state that the current operation is trying to save
///       (including all modifications made during this operation).
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>DatabaseEntity</b>: The current state in the database at the time the conflict was detected.
///       This may be <c>null</c> if the entity was deleted by another process.
///     </description>
///   </item>
/// </list>
/// <para>
/// Use <see cref="ToDictionary"/> to serialize the conflict information for inclusion in
/// <c>RepositoryErrors.ConcurrencyConflict</c> error details.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // When a conflict is detected in a repository:
/// var conflictInfo = new ConcurrencyConflictInfo&lt;Order&gt;(
///     CurrentEntity: originalOrder,
///     ProposedEntity: modifiedOrder,
///     DatabaseEntity: currentDbOrder
/// );
///
/// return RepositoryErrors.ConcurrencyConflict(conflictInfo);
/// </code>
/// </example>
public sealed record ConcurrencyConflictInfo<TEntity>(
    TEntity CurrentEntity,
    TEntity ProposedEntity,
    TEntity? DatabaseEntity)
    where TEntity : class
{
    /// <summary>
    /// The dictionary key used to store the current entity state in error details.
    /// </summary>
    public const string CurrentEntityKey = "CurrentEntity";

    /// <summary>
    /// The dictionary key used to store the proposed entity state in error details.
    /// </summary>
    public const string ProposedEntityKey = "ProposedEntity";

    /// <summary>
    /// The dictionary key used to store the database entity state in error details.
    /// </summary>
    public const string DatabaseEntityKey = "DatabaseEntity";

    /// <summary>
    /// The dictionary key used to store the entity type name in error details.
    /// </summary>
    public const string EntityTypeKey = "EntityType";

    /// <summary>
    /// Converts the conflict information to a dictionary suitable for inclusion in error details.
    /// </summary>
    /// <param name="serializerOptions">
    /// Optional JSON serializer options for entity serialization.
    /// If <c>null</c>, entities are stored as their original objects.
    /// </param>
    /// <returns>
    /// An immutable dictionary containing the entity type and serialized entity states.
    /// </returns>
    /// <remarks>
    /// <para>
    /// When <paramref name="serializerOptions"/> is provided, entities are serialized to JSON strings.
    /// This is useful for logging or when the error details need to be serialized.
    /// </para>
    /// <para>
    /// When <paramref name="serializerOptions"/> is <c>null</c>, entities are stored as objects.
    /// This preserves type information but may not be serializable.
    /// </para>
    /// </remarks>
    public ImmutableDictionary<string, object?> ToDictionary(JsonSerializerOptions? serializerOptions = null)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, object?>();
        builder.Add(EntityTypeKey, typeof(TEntity).Name);

        if (serializerOptions is not null)
        {
            builder.Add(CurrentEntityKey, JsonSerializer.Serialize(CurrentEntity, serializerOptions));
            builder.Add(ProposedEntityKey, JsonSerializer.Serialize(ProposedEntity, serializerOptions));
            builder.Add(DatabaseEntityKey, DatabaseEntity is not null
                ? JsonSerializer.Serialize(DatabaseEntity, serializerOptions)
                : null);
        }
        else
        {
            builder.Add(CurrentEntityKey, CurrentEntity);
            builder.Add(ProposedEntityKey, ProposedEntity);
            builder.Add(DatabaseEntityKey, DatabaseEntity);
        }

        return builder.ToImmutable();
    }

    /// <summary>
    /// Gets whether the entity was deleted by another process (database entity is null).
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="DatabaseEntity"/> is <c>null</c>, indicating the entity
    /// was deleted between when it was loaded and when the update was attempted;
    /// otherwise, <c>false</c>.
    /// </value>
    public bool WasDeleted => DatabaseEntity is null;
}

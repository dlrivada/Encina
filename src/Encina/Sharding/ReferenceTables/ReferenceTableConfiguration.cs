namespace Encina.Sharding.ReferenceTables;

/// <summary>
/// Captures the complete configuration for a single reference table registration,
/// binding the entity type to its options.
/// </summary>
/// <param name="EntityType">The CLR type of the reference table entity.</param>
/// <param name="Options">The per-table configuration options.</param>
/// <remarks>
/// <para>
/// This record is created during service registration and stored in the
/// <see cref="IReferenceTableRegistry"/>. It is immutable after construction.
/// </para>
/// <para>
/// The <see cref="EntityType"/> is used as the lookup key in the registry,
/// while <see cref="Options"/> provides the runtime configuration for replication.
/// </para>
/// </remarks>
public sealed record ReferenceTableConfiguration(
    Type EntityType,
    ReferenceTableOptions Options);

namespace Encina.Cdc;

/// <summary>
/// Represents a single change event captured from the database.
/// Contains the table name, operation type, before/after state, and metadata.
/// </summary>
/// <param name="TableName">The name of the database table where the change occurred.</param>
/// <param name="Operation">The type of change operation (Insert, Update, Delete, Snapshot).</param>
/// <param name="Before">The state of the row before the change, or <c>null</c> for Insert/Snapshot operations.</param>
/// <param name="After">The state of the row after the change, or <c>null</c> for Delete operations.</param>
/// <param name="Metadata">Metadata associated with the change, including position and timestamp.</param>
public sealed record ChangeEvent(
    string TableName,
    ChangeOperation Operation,
    object? Before,
    object? After,
    ChangeMetadata Metadata);

namespace Encina.Cdc;

/// <summary>
/// Represents the type of change captured from the database.
/// </summary>
public enum ChangeOperation
{
    /// <summary>A new row was inserted.</summary>
    Insert = 0,

    /// <summary>An existing row was updated.</summary>
    Update = 1,

    /// <summary>An existing row was deleted.</summary>
    Delete = 2,

    /// <summary>A snapshot of the current state (initial load).</summary>
    Snapshot = 3,
}

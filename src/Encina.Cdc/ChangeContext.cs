namespace Encina.Cdc;

/// <summary>
/// Context provided to change event handlers, containing the source table name,
/// metadata about the change, and a cancellation token for cooperative cancellation.
/// </summary>
/// <param name="TableName">The name of the database table where the change occurred.</param>
/// <param name="Metadata">Metadata associated with the change event being processed.</param>
/// <param name="CancellationToken">Cancellation token for cooperative cancellation of handler execution.</param>
public sealed record ChangeContext(
    string TableName,
    ChangeMetadata Metadata,
    CancellationToken CancellationToken);

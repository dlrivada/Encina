namespace Encina.Cdc.Errors;

/// <summary>
/// Error codes for the CDC subsystem, following the <c>encina.cdc.*</c> convention.
/// </summary>
public static class CdcErrorCodes
{
    /// <summary>Failed to establish a connection to the CDC source.</summary>
    public const string ConnectionFailed = "encina.cdc.connection_failed";

    /// <summary>The stored CDC position is invalid or corrupted.</summary>
    public const string PositionInvalid = "encina.cdc.position_invalid";

    /// <summary>The change stream was interrupted unexpectedly.</summary>
    public const string StreamInterrupted = "encina.cdc.stream_interrupted";

    /// <summary>A change event handler threw an exception.</summary>
    public const string HandlerFailed = "encina.cdc.handler_failed";

    /// <summary>Failed to deserialize a change event payload.</summary>
    public const string DeserializationFailed = "encina.cdc.deserialization_failed";

    /// <summary>Failed to read or write the position store.</summary>
    public const string PositionStoreFailed = "encina.cdc.position_store_failed";

    /// <summary>A referenced shard was not found in the sharded CDC connector.</summary>
    public const string ShardNotFound = "encina.cdc.shard_not_found";

    /// <summary>A per-shard CDC stream failed during sharded aggregation.</summary>
    public const string ShardStreamFailed = "encina.cdc.shard_stream_failed";

    /// <summary>Failed to persist a CDC event to the dead letter store.</summary>
    public const string DeadLetterStoreFailed = "encina.cdc.dead_letter_store_failed";

    /// <summary>A dead letter entry with the specified identifier was not found.</summary>
    public const string DeadLetterNotFound = "encina.cdc.dead_letter_not_found";

    /// <summary>The dead letter entry has already been resolved (replayed or discarded).</summary>
    public const string DeadLetterAlreadyResolved = "encina.cdc.dead_letter_already_resolved";

    /// <summary>The specified dead letter resolution is not valid for the current entry state.</summary>
    public const string DeadLetterInvalidResolution = "encina.cdc.dead_letter_invalid_resolution";
}

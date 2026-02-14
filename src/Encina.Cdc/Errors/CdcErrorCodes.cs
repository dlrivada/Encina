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
}

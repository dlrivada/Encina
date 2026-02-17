namespace Encina.Sharding.Resharding;

/// <summary>
/// Specifies the verification strategy used to validate data consistency
/// between source and target shards during resharding.
/// </summary>
public enum VerificationMode
{
    /// <summary>
    /// Verify only that row counts match between source and target shards.
    /// Fastest but least precise — does not detect corrupted or mismatched row values.
    /// </summary>
    Count,

    /// <summary>
    /// Verify data integrity using checksum comparison between source and target shards.
    /// More thorough than count-only but slower due to hash computation.
    /// </summary>
    Checksum,

    /// <summary>
    /// Verify both row counts and checksums. Provides the strongest consistency guarantee.
    /// </summary>
    CountAndChecksum
}

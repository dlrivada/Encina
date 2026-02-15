namespace Encina.Sharding.TimeBased;

/// <summary>
/// Configuration options for archiving a time-based shard to external storage.
/// </summary>
/// <remarks>
/// <para>
/// Archive options are passed to <see cref="IShardArchiver.ArchiveShardAsync"/> to control
/// where and how the shard data is exported. The actual archival mechanism is
/// provider-specific (e.g., export to Azure Blob, S3, or a compressed backup file).
/// </para>
/// </remarks>
/// <param name="Destination">
/// The target location for archived data (e.g., a storage URI, file path, or bucket name).
/// </param>
/// <param name="CompressData">
/// Whether to compress the archived data. Defaults to <see langword="true"/>.
/// </param>
/// <param name="DeleteAfterArchive">
/// Whether to delete the original shard data after successful archival.
/// Defaults to <see langword="false"/> for safety.
/// </param>
/// <param name="CompressionFormat">
/// The compression format to use when <see cref="CompressData"/> is <see langword="true"/>.
/// Defaults to <c>"gzip"</c>.
/// </param>
public sealed record ArchiveOptions(
    string Destination,
    bool CompressData = true,
    bool DeleteAfterArchive = false,
    string CompressionFormat = "gzip")
{
    /// <summary>
    /// Gets the target location for archived data.
    /// </summary>
    public string Destination { get; } = !string.IsNullOrWhiteSpace(Destination)
        ? Destination
        : throw new ArgumentException("Destination cannot be null or whitespace.", nameof(Destination));
}

using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Logging;

namespace Encina.Audit.Marten.Diagnostics;

/// <summary>
/// High-performance structured log messages for Marten event-sourced audit operations
/// using LoggerMessage source generators.
/// </summary>
/// <remarks>
/// <para>
/// Event IDs are allocated in the 2550-2599 range reserved for Marten audit
/// (see <c>EventIdRanges.AuditMarten</c>).
/// </para>
/// <para>
/// Allocation blocks:
/// <list type="table">
/// <item><term>2550-2559</term><description>Record operations (append encrypted events)</description></item>
/// <item><term>2560-2569</term><description>Query operations (read from projections)</description></item>
/// <item><term>2570-2579</term><description>Crypto-shredding / purge operations</description></item>
/// <item><term>2580-2589</term><description>Key management and retention service</description></item>
/// </list>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
internal static partial class MartenAuditLog
{
    // ========================================================================
    // Record operations (2550-2554)
    // ========================================================================

    /// <summary>Audit entry encrypted and appended to event stream.</summary>
    [LoggerMessage(
        EventId = 2550,
        Level = LogLevel.Debug,
        Message = "Audit entry {EntryId} encrypted and appended to stream {StreamId} using temporal period {Period}")]
    internal static partial void EntryRecorded(
        ILogger logger,
        Guid entryId,
        string streamId,
        string period);

    /// <summary>Read audit entry encrypted and appended to event stream.</summary>
    [LoggerMessage(
        EventId = 2551,
        Level = LogLevel.Debug,
        Message = "Read audit entry {EntryId} encrypted and appended to stream {StreamId} using temporal period {Period}")]
    internal static partial void ReadEntryRecorded(
        ILogger logger,
        Guid entryId,
        string streamId,
        string period);

    /// <summary>Failed to record audit entry.</summary>
    [LoggerMessage(
        EventId = 2552,
        Level = LogLevel.Error,
        Message = "Failed to record audit entry {EntryId}: {ErrorMessage}")]
    internal static partial void RecordFailed(
        ILogger logger,
        Guid entryId,
        string errorMessage,
        Exception? exception);

    /// <summary>Encryption of PII fields succeeded.</summary>
    [LoggerMessage(
        EventId = 2553,
        Level = LogLevel.Debug,
        Message = "Encrypted PII fields for audit entry {EntryId} with temporal key {KeyId}")]
    internal static partial void EncryptionSucceeded(
        ILogger logger,
        Guid entryId,
        string keyId);

    /// <summary>Encryption of PII fields failed.</summary>
    [LoggerMessage(
        EventId = 2554,
        Level = LogLevel.Error,
        Message = "Failed to encrypt PII fields for audit entry {EntryId} in period {Period}")]
    internal static partial void EncryptionFailed(
        ILogger logger,
        Guid entryId,
        string period,
        Exception exception);

    // ========================================================================
    // Query operations (2560-2563)
    // ========================================================================

    /// <summary>Audit query started.</summary>
    [LoggerMessage(
        EventId = 2560,
        Level = LogLevel.Debug,
        Message = "Marten audit query started. QueryType={QueryType}")]
    internal static partial void QueryStarted(
        ILogger logger,
        string queryType);

    /// <summary>Audit query completed successfully.</summary>
    [LoggerMessage(
        EventId = 2561,
        Level = LogLevel.Debug,
        Message = "Marten audit query completed. QueryType={QueryType}, ResultCount={ResultCount}")]
    internal static partial void QueryCompleted(
        ILogger logger,
        string queryType,
        int resultCount);

    /// <summary>Audit query failed.</summary>
    [LoggerMessage(
        EventId = 2562,
        Level = LogLevel.Warning,
        Message = "Marten audit query failed. QueryType={QueryType}, ErrorMessage={ErrorMessage}")]
    internal static partial void QueryFailed(
        ILogger logger,
        string queryType,
        string errorMessage);

    /// <summary>Shredded entries detected in query results.</summary>
    [LoggerMessage(
        EventId = 2563,
        Level = LogLevel.Debug,
        Message = "Query returned {ShreddedCount} shredded entries out of {TotalCount} total")]
    internal static partial void ShreddedEntriesInResults(
        ILogger logger,
        int shreddedCount,
        int totalCount);

    // ========================================================================
    // Crypto-shredding / purge operations (2570-2574)
    // ========================================================================

    /// <summary>Crypto-shredding operation started.</summary>
    [LoggerMessage(
        EventId = 2570,
        Level = LogLevel.Information,
        Message = "Crypto-shredding started. Destroying temporal keys older than {CutoffUtc} with granularity {Granularity}")]
    internal static partial void CryptoShreddingStarted(
        ILogger logger,
        DateTime cutoffUtc,
        string granularity);

    /// <summary>Crypto-shredding operation completed successfully.</summary>
    [LoggerMessage(
        EventId = 2571,
        Level = LogLevel.Information,
        Message = "Crypto-shredding completed. Destroyed {DestroyedCount} temporal key periods older than {CutoffUtc}")]
    internal static partial void CryptoShreddingCompleted(
        ILogger logger,
        int destroyedCount,
        DateTime cutoffUtc);

    /// <summary>Crypto-shredding operation failed.</summary>
    [LoggerMessage(
        EventId = 2572,
        Level = LogLevel.Error,
        Message = "Crypto-shredding failed for entries older than {CutoffUtc}")]
    internal static partial void CryptoShreddingFailed(
        ILogger logger,
        DateTime cutoffUtc,
        Exception exception);

    /// <summary>No temporal key periods to shred.</summary>
    [LoggerMessage(
        EventId = 2573,
        Level = LogLevel.Debug,
        Message = "No temporal key periods to destroy older than {CutoffUtc}")]
    internal static partial void NothingToShred(
        ILogger logger,
        DateTime cutoffUtc);

    /// <summary>Temporal key period destroyed.</summary>
    [LoggerMessage(
        EventId = 2574,
        Level = LogLevel.Information,
        Message = "Destroyed temporal key for period {Period}")]
    internal static partial void PeriodKeyDestroyed(
        ILogger logger,
        string period);

    // ========================================================================
    // Key management and retention service (2580-2586)
    // ========================================================================

    /// <summary>Retention service started.</summary>
    [LoggerMessage(
        EventId = 2580,
        Level = LogLevel.Information,
        Message = "Marten audit retention service started. IntervalHours={IntervalHours}, RetentionDays={RetentionDays}")]
    internal static partial void RetentionServiceStarted(
        ILogger logger,
        int intervalHours,
        int retentionDays);

    /// <summary>Retention service stopped.</summary>
    [LoggerMessage(
        EventId = 2581,
        Level = LogLevel.Information,
        Message = "Marten audit retention service stopped")]
    internal static partial void RetentionServiceStopped(
        ILogger logger);

    /// <summary>Retention service purge cycle completed.</summary>
    [LoggerMessage(
        EventId = 2582,
        Level = LogLevel.Debug,
        Message = "Retention purge cycle completed. DestroyedPeriods={DestroyedPeriods}")]
    internal static partial void RetentionCycleCompleted(
        ILogger logger,
        int destroyedPeriods);

    /// <summary>Retention service purge cycle failed.</summary>
    [LoggerMessage(
        EventId = 2583,
        Level = LogLevel.Error,
        Message = "Retention purge cycle failed")]
    internal static partial void RetentionCycleFailed(
        ILogger logger,
        Exception exception);

    /// <summary>Temporal key created for a new period.</summary>
    [LoggerMessage(
        EventId = 2584,
        Level = LogLevel.Debug,
        Message = "Temporal key created for period {Period} (KeyId={KeyId})")]
    internal static partial void TemporalKeyCreated(
        ILogger logger,
        string period,
        string keyId);

    /// <summary>Temporal key retrieved for decryption.</summary>
    [LoggerMessage(
        EventId = 2585,
        Level = LogLevel.Trace,
        Message = "Temporal key retrieved for period {Period} (Version={Version})")]
    internal static partial void TemporalKeyRetrieved(
        ILogger logger,
        string period,
        int version);

    /// <summary>Temporal key not found (period may have been shredded).</summary>
    [LoggerMessage(
        EventId = 2586,
        Level = LogLevel.Debug,
        Message = "Temporal key not found for period {Period} — may have been crypto-shredded")]
    internal static partial void TemporalKeyNotFound(
        ILogger logger,
        string period);
}

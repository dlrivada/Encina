using System.Text.Json;

using Encina.Audit.Marten.Crypto;
using Encina.Audit.Marten.Events;
using Encina.Security.Audit;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static LanguageExt.Prelude;

namespace Encina.Audit.Marten;

/// <summary>
/// Maps <see cref="AuditEntry"/> and <see cref="ReadAuditEntry"/> domain models to their
/// corresponding encrypted event-sourced events using temporal encryption keys.
/// </summary>
/// <remarks>
/// <para>
/// This utility handles the encryption of PII-sensitive fields during the mapping process.
/// Based on the configured <see cref="AuditEncryptionScope"/>, it encrypts either only PII
/// fields or all fields in the audit entry.
/// </para>
/// <para>
/// The encryptor resolves the correct temporal key for the entry's timestamp using
/// <see cref="ITemporalKeyProvider"/> and the configured <see cref="TemporalKeyGranularity"/>.
/// </para>
/// <para>
/// If the temporal key for the entry's period has been destroyed (crypto-shredded),
/// the mapping returns <c>Left&lt;EncinaError&gt;</c> — this should not happen during
/// normal operation, as new entries should always target the current (active) time period.
/// </para>
/// </remarks>
public sealed class AuditEventEncryptor
{
    private static readonly JsonSerializerOptions MetadataJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly ITemporalKeyProvider _keyProvider;
    private readonly MartenAuditOptions _options;
    private readonly ILogger<AuditEventEncryptor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditEventEncryptor"/> class.
    /// </summary>
    /// <param name="keyProvider">The temporal key provider for key retrieval/creation.</param>
    /// <param name="options">The Marten audit configuration options.</param>
    /// <param name="logger">Logger for structured diagnostic logging.</param>
    public AuditEventEncryptor(
        ITemporalKeyProvider keyProvider,
        IOptions<MartenAuditOptions> options,
        ILogger<AuditEventEncryptor> logger)
    {
        ArgumentNullException.ThrowIfNull(keyProvider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _keyProvider = keyProvider;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Maps an <see cref="AuditEntry"/> to an <see cref="AuditEntryRecordedEvent"/> with
    /// encrypted PII fields.
    /// </summary>
    /// <param name="entry">The audit entry to encrypt and map.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right&lt;AuditEntryRecordedEvent&gt;</c> with the encrypted event on success, or
    /// <c>Left&lt;EncinaError&gt;</c> if encryption fails (e.g., temporal key destroyed).
    /// </returns>
    public async ValueTask<Either<EncinaError, AuditEntryRecordedEvent>> EncryptAuditEntryAsync(
        AuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var period = TemporalPeriodHelper.GetPeriod(entry.TimestampUtc, _options.TemporalGranularity);

        var keyResult = await _keyProvider.GetOrCreateKeyAsync(period, cancellationToken)
            .ConfigureAwait(false);

        return keyResult.Match<Either<EncinaError, AuditEntryRecordedEvent>>(
            Right: keyInfo =>
            {
                try
                {
                    var keyMaterial = keyInfo.KeyMaterial;
                    var keyId = keyInfo.KeyId;

                    var encryptedEvent = new AuditEntryRecordedEvent
                    {
                        // Plaintext structural fields
                        Id = entry.Id,
                        CorrelationId = entry.CorrelationId,
                        Action = entry.Action,
                        EntityType = entry.EntityType,
                        EntityId = entry.EntityId,
                        Outcome = (int)entry.Outcome,
                        ErrorMessage = entry.ErrorMessage,
                        TimestampUtc = entry.TimestampUtc,
                        StartedAtUtc = entry.StartedAtUtc,
                        CompletedAtUtc = entry.CompletedAtUtc,
                        RequestPayloadHash = entry.RequestPayloadHash,
                        TenantId = entry.TenantId,

                        // Encrypted PII fields
                        EncryptedUserId = EncryptNullable(entry.UserId, keyMaterial, keyId),
                        EncryptedIpAddress = EncryptNullable(entry.IpAddress, keyMaterial, keyId),
                        EncryptedUserAgent = EncryptNullable(entry.UserAgent, keyMaterial, keyId),
                        EncryptedRequestPayload = EncryptNullable(entry.RequestPayload, keyMaterial, keyId),
                        EncryptedResponsePayload = EncryptNullable(entry.ResponsePayload, keyMaterial, keyId),
                        EncryptedMetadata = EncryptMetadata(entry.Metadata, keyMaterial, keyId),

                        // Key tracking
                        TemporalKeyPeriod = period
                    };

                    _logger.LogDebug(
                        "Encrypted audit entry {EntryId} with temporal key period {Period}",
                        entry.Id,
                        period);

                    return Right(encryptedEvent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to encrypt audit entry {EntryId} for period {Period}",
                        entry.Id,
                        period);
                    return Left(MartenAuditErrors.EncryptionFailed(entry.Id, period, ex));
                }
            },
            Left: error => Left(error));
    }

    /// <summary>
    /// Maps a <see cref="ReadAuditEntry"/> to a <see cref="ReadAuditEntryRecordedEvent"/> with
    /// encrypted PII fields.
    /// </summary>
    /// <param name="entry">The read audit entry to encrypt and map.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right&lt;ReadAuditEntryRecordedEvent&gt;</c> with the encrypted event on success, or
    /// <c>Left&lt;EncinaError&gt;</c> if encryption fails.
    /// </returns>
    public async ValueTask<Either<EncinaError, ReadAuditEntryRecordedEvent>> EncryptReadAuditEntryAsync(
        ReadAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var period = TemporalPeriodHelper.GetPeriod(entry.AccessedAtUtc, _options.TemporalGranularity);

        var keyResult = await _keyProvider.GetOrCreateKeyAsync(period, cancellationToken)
            .ConfigureAwait(false);

        return keyResult.Match<Either<EncinaError, ReadAuditEntryRecordedEvent>>(
            Right: keyInfo =>
            {
                try
                {
                    var keyMaterial = keyInfo.KeyMaterial;
                    var keyId = keyInfo.KeyId;

                    var encryptedEvent = new ReadAuditEntryRecordedEvent
                    {
                        // Plaintext structural fields
                        Id = entry.Id,
                        EntityType = entry.EntityType,
                        EntityId = entry.EntityId,
                        AccessedAtUtc = entry.AccessedAtUtc,
                        AccessMethod = (int)entry.AccessMethod,
                        EntityCount = entry.EntityCount,
                        CorrelationId = entry.CorrelationId,
                        TenantId = entry.TenantId,

                        // Encrypted PII fields
                        EncryptedUserId = EncryptNullable(entry.UserId, keyMaterial, keyId),
                        EncryptedPurpose = EncryptNullable(entry.Purpose, keyMaterial, keyId),
                        EncryptedMetadata = EncryptMetadata(entry.Metadata, keyMaterial, keyId),

                        // Key tracking
                        TemporalKeyPeriod = period
                    };

                    _logger.LogDebug(
                        "Encrypted read audit entry {EntryId} with temporal key period {Period}",
                        entry.Id,
                        period);

                    return Right(encryptedEvent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to encrypt read audit entry {EntryId} for period {Period}",
                        entry.Id,
                        period);
                    return Left(MartenAuditErrors.EncryptionFailed(entry.Id, period, ex));
                }
            },
            Left: error => Left(error));
    }

    /// <summary>
    /// Encrypts a nullable string value, returning <c>null</c> if the input is <c>null</c>.
    /// </summary>
    private static EncryptedField? EncryptNullable(string? value, byte[] keyMaterial, string keyId)
    {
        if (value is null)
        {
            return null;
        }

        return EncryptedField.Encrypt(value, keyMaterial, keyId);
    }

    /// <summary>
    /// Encrypts a metadata dictionary by serializing it to JSON first, then encrypting.
    /// </summary>
    private static EncryptedField? EncryptMetadata(
        IReadOnlyDictionary<string, object?> metadata,
        byte[] keyMaterial,
        string keyId)
    {
        if (metadata.Count == 0)
        {
            return null;
        }

        var json = JsonSerializer.Serialize(metadata, MetadataJsonOptions);
        return EncryptedField.Encrypt(json, keyMaterial, keyId);
    }
}

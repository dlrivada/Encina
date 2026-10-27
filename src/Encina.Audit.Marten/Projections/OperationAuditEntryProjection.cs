using System.Diagnostics.CodeAnalysis;

using Encina.Audit.Marten.Crypto;
using Encina.Audit.Marten.Events;
using Encina.Security.Audit;

using Marten;
using Marten.Events.Projections;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.Audit.Marten.Projections;

/// <summary>
/// Marten async event projection that transforms <see cref="OperationAuditEntryRecordedEvent"/> events
/// into <see cref="OperationAuditEntryReadModel"/> documents with transparent PII decryption.
/// </summary>
/// <remarks>
/// <para>
/// This projection is registered as <b>async</b> via Marten's <c>AsyncProjectionDaemon</c>,
/// providing eventual consistency with independent lifecycle management (retries, high-water
/// mark tracking, rebuild capability).
/// </para>
/// <para>
/// During processing, the projection:
/// <list type="number">
/// <item>Receives <see cref="OperationAuditEntryRecordedEvent"/> from the event stream</item>
/// <item>Uses the projection-scoped <see cref="IDocumentOperations"/> to load the
/// <see cref="TemporalKeyDocument"/> for the entry's period</item>
/// <item>Decrypts PII fields using the temporal key material</item>
/// <item>If the key has been destroyed (crypto-shredded), substitutes the configured
/// shredded placeholder and sets <see cref="OperationAuditEntryReadModel.IsShredded"/> to <c>true</c></item>
/// <item>Stores the resulting <see cref="OperationAuditEntryReadModel"/> as a Marten document</item>
/// </list>
/// </para>
/// <para>
/// <b>Note:</b> Marten's projection validation only supports a fixed set of parameter types
/// on <c>Create</c> methods (<see cref="CancellationToken"/>, <see cref="IDocumentOperations"/>,
/// the event type, and <see cref="JasperFx.Events.IEvent"/>). <see cref="IServiceProvider"/>
/// is <b>not</b> supported, so all dependencies are injected via the constructor and temporal
/// keys are loaded through the supplied <see cref="IDocumentOperations"/>.
/// </para>
/// </remarks>
public sealed partial class OperationAuditEntryProjection : EventProjection
{
    private readonly string _shreddedPlaceholder;
    private readonly string _expiredPlaceholder;
    private readonly ILogger<OperationAuditEntryProjection> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationAuditEntryProjection"/> class
    /// using the default shredded placeholder (<see cref="MartenOperationAuditOptions.DefaultShreddedPlaceholder"/>)
    /// and a null logger.
    /// </summary>
    public OperationAuditEntryProjection()
        : this(
            MartenOperationAuditOptions.DefaultShreddedPlaceholder,
            MartenOperationAuditOptions.DefaultExpiredPlaceholder,
            NullLogger<OperationAuditEntryProjection>.Instance)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationAuditEntryProjection"/> class.
    /// </summary>
    /// <param name="shreddedPlaceholder">
    /// The placeholder substituted for PII fields when the temporal key was destroyed by a recorded erasure request.
    /// </param>
    /// <param name="expiredPlaceholder">
    /// The placeholder substituted for PII fields when the temporal key was destroyed by a recorded retention execution.
    /// </param>
    /// <param name="logger">Logger used for projection diagnostics.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when a placeholder is <c>null</c>, empty, or whitespace.
    /// An empty or whitespace placeholder would project PII fields as blank strings instead
    /// of masking them, which defeats the purpose of crypto-shredding.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/> is <c>null</c>.
    /// </exception>
    public OperationAuditEntryProjection(string shreddedPlaceholder, string expiredPlaceholder, ILogger<OperationAuditEntryProjection> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shreddedPlaceholder);
        ArgumentException.ThrowIfNullOrWhiteSpace(expiredPlaceholder);
        ArgumentNullException.ThrowIfNull(logger);

        Name = "OperationAuditEntryProjection";
        _shreddedPlaceholder = shreddedPlaceholder;
        _expiredPlaceholder = expiredPlaceholder;
        _logger = logger;
    }

    /// <summary>
    /// Creates an <see cref="OperationAuditEntryReadModel"/> document from an <see cref="OperationAuditEntryRecordedEvent"/>.
    /// </summary>
    /// <param name="event">The event containing the encrypted audit entry data.</param>
    /// <param name="operations">
    /// The Marten document operations used to load the temporal key for the entry's period.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The projected read model document with decrypted (or shredded) PII fields.</returns>
    /// <remarks>
    /// Marten invokes this method for each <see cref="OperationAuditEntryRecordedEvent"/> in the event stream.
    /// The method shape is constrained by Marten's projection validator — only
    /// <see cref="IDocumentOperations"/>, <see cref="CancellationToken"/>, and event parameter
    /// types are permitted.
    /// </remarks>
    [SuppressMessage("Performance", "CA1822:Mark members as static",
        Justification = "Marten's EventProjection convention requires instance methods for Create/Apply.")]
    public async Task<OperationAuditEntryReadModel> Create(
        OperationAuditEntryRecordedEvent @event,
        IDocumentOperations operations,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(operations);

        var (keyMaterial, isShredded, isExpired) = await LoadTemporalKeyAsync(
            operations, @event.TemporalKeyPeriod, cancellationToken).ConfigureAwait(false);

        return MapToReadModel(@event, keyMaterial, isShredded, isExpired);
    }

    /// <summary>
    /// Loads the active temporal key material for a given period via Marten's
    /// <see cref="IQuerySession"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a thin I/O wrapper: it issues the two Marten queries (active key, destroyed
    /// marker) and delegates the actual decision to <see cref="ClassifyTemporalKeyLookup"/>,
    /// which is pure and fully unit-tested. The Marten LINQ pipeline
    /// (<c>IMartenQueryable.FirstOrDefaultAsync</c>) cannot be reliably faked at the unit-test
    /// level, so this wrapper itself is exercised end-to-end by integration tests (see #951).
    /// </para>
    /// <para>
    /// Lookup strategy, ordered for the common case:
    /// </para>
    /// <list type="number">
    /// <item>
    /// Fetch the latest active key in a single query via
    /// <c>OrderByDescending(Version).FirstOrDefaultAsync</c>, pushing ordering and limit down
    /// to PostgreSQL. Common case: one query per event, no in-memory sorting.
    /// </item>
    /// <item>
    /// If no active key is found, check for an explicit
    /// <see cref="TemporalKeyDestroyedMarker"/>. If present with only an erasure reference, the period was
    /// crypto-shredded for an erasure request and the projection substitutes the shredded placeholder
    /// (<c>IsShredded = true</c>). If present with only a retention reference, the keys expired under a retention
    /// policy and the projection substitutes the expired placeholder (<c>IsExpired = true</c>) and keeps running.
    /// If present with neither reference, or with both (an error, tampering, and every marker written before the
    /// references existed), the projection fails with <see cref="EncryptedFieldDecryptionException"/> and the async
    /// daemon pauses the shard.
    /// </item>
    /// <item>
    /// If neither an active key nor a destroyed marker exists, the lookup throws
    /// <see cref="KeyNotFoundException"/>. The absence of both documents is not a recorded erasure, so the
    /// projection never persists <c>IsShredded = true</c> for it: that would permanently corrupt the read model.
    /// <b>The daemon does not retry this exception.</b> With the fail-closed error policy that
    /// <see cref="ConfigureMartenOperationAuditProjections"/> sets, Marten pauses the shard (verified by
    /// <c>AuditProjectionUnreadableEventIntegrationTests</c>, issue #2770); without that policy it would skip the
    /// event silently. Either way the operator restores the key (or the marker) and rebuilds the projection.
    /// </item>
    /// </list>
    /// </remarks>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when neither an active key nor a destroyed marker exists for the given period,
    /// indicating an inconsistency the operator must resolve. The Marten async projection daemon pauses the shard.
    /// </exception>
    internal static async Task<(byte[]? KeyMaterial, bool IsShredded, bool IsExpired)> LoadTemporalKeyAsync(
        IQuerySession session,
        string period,
        CancellationToken cancellationToken)
    {
        // Common case: fetch the latest active key in a single query.
        var activeKey = await session.Query<TemporalKeyDocument>()
            .Where(d => d.Period == period && d.Status == TemporalKeyStatus.Active)
            .OrderByDescending(d => d.Version)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        // Only fetch the destroyed marker if the active-key lookup came up empty,
        // keeping the hot path to a single indexed query.
        TemporalKeyDestroyedMarker? destroyedDoc = null;
        if (activeKey is null)
        {
            destroyedDoc = await session.LoadAsync<TemporalKeyDestroyedMarker>(
                TemporalPeriodHelper.FormatDestroyedMarkerId(period),
                cancellationToken).ConfigureAwait(false);
        }

        return ClassifyTemporalKeyLookup(activeKey, destroyedDoc, period);
    }

    /// <summary>
    /// Pure classification logic that maps an (activeKey, destroyedMarker) lookup result
    /// into the tuple consumed by <see cref="MapToReadModel"/>.
    /// </summary>
    /// <param name="activeKey">
    /// The latest active temporal key for the period, or <c>null</c> if none exists.
    /// </param>
    /// <param name="destroyedMarker">
    /// The destruction marker for the period, or <c>null</c> if the period has not been
    /// crypto-shredded. Only consulted when <paramref name="activeKey"/> is <c>null</c>.
    /// </param>
    /// <param name="period">The temporal period identifier (used in the error message).</param>
    /// <returns>
    /// A tuple where:
    /// <list type="bullet">
    /// <item><c>(keyMaterial, false, false)</c> — an active key was found</item>
    /// <item><c>(null, true, false)</c> — no active key, destroyed marker present with only an erasure reference (crypto-shredded)</item>
    /// <item><c>(null, false, true)</c> — no active key, destroyed marker present with only a retention reference (expired)</item>
    /// </list>
    /// </returns>
    /// <exception cref="EncryptedFieldDecryptionException">
    /// Thrown (reason <see cref="EncryptedFieldDecryptionFailureReason.KeyDestroyedWithoutErasureRecord"/>) when the
    /// destroyed marker carries neither a complete erasure nor a complete retention reference, or carries both.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when both <paramref name="activeKey"/> and <paramref name="destroyedMarker"/>
    /// are <c>null</c>. This indicates an inconsistency the daemon does not retry — see
    /// <see cref="LoadTemporalKeyAsync"/> for the full rationale.
    /// </exception>
    /// <remarks>
    /// This function is extracted and exposed as <c>internal</c> so the three decision paths
    /// can be unit-tested exhaustively without needing to mock Marten's LINQ pipeline.
    /// </remarks>
    internal static (byte[]? KeyMaterial, bool IsShredded, bool IsExpired) ClassifyTemporalKeyLookup(
        TemporalKeyDocument? activeKey,
        TemporalKeyDestroyedMarker? destroyedMarker,
        string period)
    {
        if (activeKey is not null)
        {
            return (activeKey.KeyMaterial, false, false);
        }

        if (destroyedMarker is not null)
        {
            // [SHREDDED] only for a destruction recorded as an erasure request, [EXPIRED] only for one recorded as a
            // retention execution (Q3). Any other destruction (neither reference, or both) is not a lawful event:
            // fail typed so the daemon pauses the shard for the operator (issue #2770, D15/Q1).
            return destroyedMarker.Classify() switch
            {
                KeyDestructionKind.Erasure => (null, true, false),
                KeyDestructionKind.Retention => (null, false, true),
                _ => throw new EncryptedFieldDecryptionException(EncryptedFieldDecryptionFailureReason.KeyDestroyedWithoutErasureRecord)
            };
        }

        throw new KeyNotFoundException(
            $"Temporal key for period '{period}' not found: no active key and no destroyed marker. " +
            "The key may not be written yet (replication lag, pending write) or may be lost. " +
            "The Marten async projection daemon does not retry: it pauses the shard until the operator restores the key.");
    }

    /// <summary>
    /// Maps an <see cref="OperationAuditEntryRecordedEvent"/> to an <see cref="OperationAuditEntryReadModel"/>,
    /// decrypting PII fields with the supplied key material or substituting the shredded
    /// placeholder when <paramref name="isShredded"/> is <c>true</c>, or the expired placeholder when
    /// <paramref name="isExpired"/> is <c>true</c>.
    /// </summary>
    /// <remarks>
    /// Exposed as <c>internal</c> so unit tests can validate the mapping logic independently
    /// of Marten's projection pipeline, which cannot be booted from unit tests.
    /// </remarks>
    internal OperationAuditEntryReadModel MapToReadModel(
        OperationAuditEntryRecordedEvent @event,
        byte[]? keyMaterial,
        bool isShredded,
        bool isExpired = false)
    {
        if (isShredded || isExpired)
        {
            _logger.LogDebug(
                "Temporal key for period {Period} destroyed — marking audit entry {EntryId} as {Kind}",
                @event.TemporalKeyPeriod,
                @event.Id,
                isShredded ? "shredded" : "expired");
        }

        var placeholder = isExpired ? _expiredPlaceholder : _shreddedPlaceholder;

        return new OperationAuditEntryReadModel
        {
            // Identity
            Id = @event.Id,

            // Structural fields (always plaintext)
            CorrelationId = @event.CorrelationId,
            Action = @event.Action,
            EntityType = @event.EntityType,
            EntityId = @event.EntityId,
            Outcome = (AuditOutcome)@event.Outcome,
            ErrorMessage = @event.ErrorMessage,
            TimestampUtc = @event.TimestampUtc.ToUtcOffset(),
            StartedAtUtc = @event.StartedAtUtc,
            CompletedAtUtc = @event.CompletedAtUtc,
            RequestPayloadHash = @event.RequestPayloadHash,
            TenantId = @event.TenantId,

            // PII fields (decrypted or shredded)
            UserId = Decrypt(@event.EncryptedUserId, keyMaterial, placeholder),
            IpAddress = Decrypt(@event.EncryptedIpAddress, keyMaterial, placeholder),
            UserAgent = Decrypt(@event.EncryptedUserAgent, keyMaterial, placeholder),
            RequestPayload = Decrypt(@event.EncryptedRequestPayload, keyMaterial, placeholder),
            ResponsePayload = Decrypt(@event.EncryptedResponsePayload, keyMaterial, placeholder),
            MetadataJson = Decrypt(@event.EncryptedMetadata, keyMaterial, placeholder),

            // Crypto-shredding tracking
            IsShredded = isShredded,
            IsExpired = isExpired,
            TemporalKeyPeriod = @event.TemporalKeyPeriod
        };
    }

    private static string? Decrypt(EncryptedField? field, byte[]? keyMaterial, string placeholder) =>
        field?.DecryptOrPlaceholder(keyMaterial, placeholder);
}

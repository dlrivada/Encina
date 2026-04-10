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
/// Marten async event projection that transforms <see cref="AuditEntryRecordedEvent"/> events
/// into <see cref="AuditEntryReadModel"/> documents with transparent PII decryption.
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
/// <item>Receives <see cref="AuditEntryRecordedEvent"/> from the event stream</item>
/// <item>Uses the projection-scoped <see cref="IDocumentOperations"/> to load the
/// <see cref="TemporalKeyDocument"/> for the entry's period</item>
/// <item>Decrypts PII fields using the temporal key material</item>
/// <item>If the key has been destroyed (crypto-shredded), substitutes the configured
/// shredded placeholder and sets <see cref="AuditEntryReadModel.IsShredded"/> to <c>true</c></item>
/// <item>Stores the resulting <see cref="AuditEntryReadModel"/> as a Marten document</item>
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
public sealed class AuditEntryProjection : EventProjection
{
    private readonly string _shreddedPlaceholder;
    private readonly ILogger<AuditEntryProjection> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditEntryProjection"/> class
    /// using the default shredded placeholder (<see cref="MartenAuditOptions.DefaultShreddedPlaceholder"/>)
    /// and a null logger.
    /// </summary>
    public AuditEntryProjection()
        : this(MartenAuditOptions.DefaultShreddedPlaceholder, NullLogger<AuditEntryProjection>.Instance)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditEntryProjection"/> class.
    /// </summary>
    /// <param name="shreddedPlaceholder">
    /// The placeholder substituted for PII fields when the temporal key has been destroyed.
    /// </param>
    /// <param name="logger">Logger used for projection diagnostics.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="shreddedPlaceholder"/> or <paramref name="logger"/> is <c>null</c>.
    /// </exception>
    public AuditEntryProjection(string shreddedPlaceholder, ILogger<AuditEntryProjection> logger)
    {
        ArgumentNullException.ThrowIfNull(shreddedPlaceholder);
        ArgumentNullException.ThrowIfNull(logger);

        Name = "AuditEntryProjection";
        _shreddedPlaceholder = shreddedPlaceholder;
        _logger = logger;
    }

    /// <summary>
    /// Creates an <see cref="AuditEntryReadModel"/> document from an <see cref="AuditEntryRecordedEvent"/>.
    /// </summary>
    /// <param name="event">The event containing the encrypted audit entry data.</param>
    /// <param name="operations">
    /// The Marten document operations used to load the temporal key for the entry's period.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The projected read model document with decrypted (or shredded) PII fields.</returns>
    /// <remarks>
    /// Marten invokes this method for each <see cref="AuditEntryRecordedEvent"/> in the event stream.
    /// The method shape is constrained by Marten's projection validator — only
    /// <see cref="IDocumentOperations"/>, <see cref="CancellationToken"/>, and event parameter
    /// types are permitted.
    /// </remarks>
    [SuppressMessage("Performance", "CA1822:Mark members as static",
        Justification = "Marten's EventProjection convention requires instance methods for Create/Apply.")]
    public async Task<AuditEntryReadModel> Create(
        AuditEntryRecordedEvent @event,
        IDocumentOperations operations,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(operations);

        var (keyMaterial, isShredded) = await LoadTemporalKeyAsync(
            operations, @event.TemporalKeyPeriod, cancellationToken).ConfigureAwait(false);

        if (isShredded)
        {
            _logger.LogDebug(
                "Temporal key for period {Period} not found — marking audit entry {EntryId} as shredded",
                @event.TemporalKeyPeriod,
                @event.Id);
        }

        return MapToReadModel(@event, keyMaterial, isShredded);
    }

    /// <summary>
    /// Loads the active temporal key material for a given period via Marten's
    /// <see cref="IQuerySession"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Optimized to a single database round-trip: the latest active key is fetched directly
    /// via <c>OrderByDescending(Version).FirstOrDefaultAsync</c>, pushing the ordering and
    /// limit down to PostgreSQL instead of materializing all versions into memory. This keeps
    /// the hot path of the async projection daemon as cheap as possible (one query per event).
    /// </para>
    /// <para>
    /// For projection purposes, a missing active key is equivalent to crypto-shredding:
    /// the PII fields become unrecoverable either way, so the projection substitutes the
    /// configured placeholder. The <see cref="TemporalKeyDestroyedMarker"/> document is
    /// therefore <b>not</b> consulted here — it only exists to serve the
    /// <c>MartenTemporalKeyProvider</c> public API, which needs to distinguish
    /// destroyed-vs-never-existed for its callers.
    /// </para>
    /// <para>
    /// This is exposed as <c>internal</c> so unit tests can verify the key-lookup contract
    /// against an in-memory Marten session or a stub without invoking the full projection pipeline.
    /// </para>
    /// </remarks>
    internal static async Task<(byte[]? KeyMaterial, bool IsShredded)> LoadTemporalKeyAsync(
        IQuerySession session,
        string period,
        CancellationToken cancellationToken)
    {
        var activeKey = await session.Query<TemporalKeyDocument>()
            .Where(d => d.Period == period && d.Status == TemporalKeyStatus.Active)
            .OrderByDescending(d => d.Version)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        return activeKey is not null
            ? (activeKey.KeyMaterial, false)
            : (null, true);
    }

    /// <summary>
    /// Maps an <see cref="AuditEntryRecordedEvent"/> to an <see cref="AuditEntryReadModel"/>,
    /// decrypting PII fields with the supplied key material or substituting the shredded
    /// placeholder when <paramref name="isShredded"/> is <c>true</c>.
    /// </summary>
    /// <remarks>
    /// Exposed as <c>internal</c> so unit tests can validate the mapping logic independently
    /// of Marten's projection pipeline, which cannot be booted from unit tests.
    /// </remarks>
    internal AuditEntryReadModel MapToReadModel(
        AuditEntryRecordedEvent @event,
        byte[]? keyMaterial,
        bool isShredded)
    {
        var placeholder = _shreddedPlaceholder;

        return new AuditEntryReadModel
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
            TimestampUtc = @event.TimestampUtc,
            StartedAtUtc = @event.StartedAtUtc,
            CompletedAtUtc = @event.CompletedAtUtc,
            RequestPayloadHash = @event.RequestPayloadHash,
            TenantId = @event.TenantId,

            // PII fields (decrypted or shredded)
            UserId = @event.EncryptedUserId?.DecryptOrPlaceholder(keyMaterial, placeholder),
            IpAddress = @event.EncryptedIpAddress?.DecryptOrPlaceholder(keyMaterial, placeholder),
            UserAgent = @event.EncryptedUserAgent?.DecryptOrPlaceholder(keyMaterial, placeholder),
            RequestPayload = @event.EncryptedRequestPayload?.DecryptOrPlaceholder(keyMaterial, placeholder),
            ResponsePayload = @event.EncryptedResponsePayload?.DecryptOrPlaceholder(keyMaterial, placeholder),
            MetadataJson = @event.EncryptedMetadata?.DecryptOrPlaceholder(keyMaterial, placeholder),

            // Crypto-shredding tracking
            IsShredded = isShredded,
            TemporalKeyPeriod = @event.TemporalKeyPeriod
        };
    }
}

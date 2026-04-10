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
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="shreddedPlaceholder"/> is <c>null</c>, empty, or whitespace.
    /// An empty or whitespace placeholder would project PII fields as blank strings instead
    /// of masking them, which defeats the purpose of crypto-shredding.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/> is <c>null</c>.
    /// </exception>
    public AuditEntryProjection(string shreddedPlaceholder, ILogger<AuditEntryProjection> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shreddedPlaceholder);
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

        return MapToReadModel(@event, keyMaterial, isShredded);
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
    /// <see cref="TemporalKeyDestroyedMarker"/>. If present, the period was crypto-shredded
    /// and the projection substitutes the placeholder (<c>IsShredded = true</c>).
    /// </item>
    /// <item>
    /// If neither an active key nor a destroyed marker exists, the lookup throws
    /// <see cref="KeyNotFoundException"/>. This is <b>deliberately retryable</b>: the absence
    /// of both documents can be caused by replication lag, a transient read-your-writes delay,
    /// or a pending write on the primary. Persisting <c>IsShredded = true</c> in that scenario
    /// would advance the async projection daemon's high-water mark and permanently corrupt
    /// the read model. Throwing causes Marten to retry the projection on the next pass.
    /// </item>
    /// </list>
    /// </remarks>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when neither an active key nor a destroyed marker exists for the given period,
    /// indicating transient inconsistency. The Marten async projection daemon will retry.
    /// </exception>
    internal static async Task<(byte[]? KeyMaterial, bool IsShredded)> LoadTemporalKeyAsync(
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
    /// <item><c>(keyMaterial, false)</c> — an active key was found</item>
    /// <item><c>(null, true)</c> — no active key, destroyed marker present (crypto-shredded)</item>
    /// </list>
    /// </returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when both <paramref name="activeKey"/> and <paramref name="destroyedMarker"/>
    /// are <c>null</c>. This indicates transient inconsistency — see
    /// <see cref="LoadTemporalKeyAsync"/> for the full rationale.
    /// </exception>
    /// <remarks>
    /// This function is extracted and exposed as <c>internal</c> so the three decision paths
    /// can be unit-tested exhaustively without needing to mock Marten's LINQ pipeline.
    /// </remarks>
    internal static (byte[]? KeyMaterial, bool IsShredded) ClassifyTemporalKeyLookup(
        TemporalKeyDocument? activeKey,
        TemporalKeyDestroyedMarker? destroyedMarker,
        string period)
    {
        if (activeKey is not null)
        {
            return (activeKey.KeyMaterial, false);
        }

        if (destroyedMarker is not null)
        {
            return (null, true);
        }

        throw new KeyNotFoundException(
            $"Temporal key for period '{period}' not found: no active key and no destroyed marker. " +
            "This indicates transient inconsistency (replication lag, pending write, or similar). " +
            "The Marten async projection daemon will retry on the next pass.");
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
        if (isShredded)
        {
            _logger.LogDebug(
                "Temporal key for period {Period} not found — marking audit entry {EntryId} as shredded",
                @event.TemporalKeyPeriod,
                @event.Id);
        }

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

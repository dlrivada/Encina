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
/// Marten async event projection that transforms <see cref="ReadAuditEntryRecordedEvent"/> events
/// into <see cref="ReadAuditEntryReadModel"/> documents with transparent PII decryption.
/// </summary>
/// <remarks>
/// <para>
/// This projection is registered as <b>async</b> via Marten's <c>AsyncProjectionDaemon</c>,
/// providing eventual consistency with independent lifecycle management.
/// </para>
/// <para>
/// During processing, the projection decrypts PII fields (UserId, Purpose, Metadata)
/// using the temporal key for the entry's period, loaded through the projection-scoped
/// <see cref="IDocumentOperations"/>. If the key has been destroyed, PII fields are replaced
/// with the configured shredded placeholder.
/// </para>
/// <para>
/// <b>Note:</b> Marten's projection validation only supports a fixed set of parameter types
/// on <c>Create</c> methods. <see cref="IServiceProvider"/> is not supported, so dependencies
/// are injected via the constructor.
/// </para>
/// </remarks>
public sealed class ReadAuditEntryProjection : EventProjection
{
    private readonly string _shreddedPlaceholder;
    private readonly ILogger<ReadAuditEntryProjection> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadAuditEntryProjection"/> class
    /// using the default shredded placeholder and a null logger.
    /// </summary>
    public ReadAuditEntryProjection()
        : this(MartenAuditOptions.DefaultShreddedPlaceholder, NullLogger<ReadAuditEntryProjection>.Instance)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadAuditEntryProjection"/> class.
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
    public ReadAuditEntryProjection(string shreddedPlaceholder, ILogger<ReadAuditEntryProjection> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shreddedPlaceholder);
        ArgumentNullException.ThrowIfNull(logger);

        Name = "ReadAuditEntryProjection";
        _shreddedPlaceholder = shreddedPlaceholder;
        _logger = logger;
    }

    /// <summary>
    /// Creates a <see cref="ReadAuditEntryReadModel"/> document from a
    /// <see cref="ReadAuditEntryRecordedEvent"/>.
    /// </summary>
    /// <param name="event">The event containing the encrypted read audit entry data.</param>
    /// <param name="operations">
    /// The Marten document operations used to load the temporal key for the entry's period.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The projected read model document with decrypted (or shredded) PII fields.</returns>
    [SuppressMessage("Performance", "CA1822:Mark members as static",
        Justification = "Marten's EventProjection convention requires instance methods for Create/Apply.")]
    public async Task<ReadAuditEntryReadModel> Create(
        ReadAuditEntryRecordedEvent @event,
        IDocumentOperations operations,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(operations);

        var (keyMaterial, isShredded) = await AuditEntryProjection.LoadTemporalKeyAsync(
            operations, @event.TemporalKeyPeriod, cancellationToken).ConfigureAwait(false);

        return MapToReadModel(@event, keyMaterial, isShredded);
    }

    /// <summary>
    /// Maps a <see cref="ReadAuditEntryRecordedEvent"/> to a <see cref="ReadAuditEntryReadModel"/>,
    /// decrypting PII fields with the supplied key material or substituting the shredded
    /// placeholder when <paramref name="isShredded"/> is <c>true</c>.
    /// </summary>
    /// <remarks>
    /// Exposed as <c>internal</c> so unit tests can validate the mapping logic independently
    /// of Marten's projection pipeline.
    /// </remarks>
    internal ReadAuditEntryReadModel MapToReadModel(
        ReadAuditEntryRecordedEvent @event,
        byte[]? keyMaterial,
        bool isShredded)
    {
        if (isShredded)
        {
            _logger.LogDebug(
                "Temporal key for period {Period} not found — marking read audit entry {EntryId} as shredded",
                @event.TemporalKeyPeriod,
                @event.Id);
        }

        var placeholder = _shreddedPlaceholder;

        return new ReadAuditEntryReadModel
        {
            // Identity
            Id = @event.Id,

            // Structural fields (always plaintext)
            EntityType = @event.EntityType,
            EntityId = @event.EntityId,
            AccessedAtUtc = @event.AccessedAtUtc,
            AccessMethod = (ReadAccessMethod)@event.AccessMethod,
            EntityCount = @event.EntityCount,
            CorrelationId = @event.CorrelationId,
            TenantId = @event.TenantId,

            // PII fields (decrypted or shredded)
            UserId = @event.EncryptedUserId?.DecryptOrPlaceholder(keyMaterial, placeholder),
            Purpose = @event.EncryptedPurpose?.DecryptOrPlaceholder(keyMaterial, placeholder),
            MetadataJson = @event.EncryptedMetadata?.DecryptOrPlaceholder(keyMaterial, placeholder),

            // Crypto-shredding tracking
            IsShredded = isShredded,
            TemporalKeyPeriod = @event.TemporalKeyPeriod
        };
    }
}

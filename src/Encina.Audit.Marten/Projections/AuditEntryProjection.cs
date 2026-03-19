using System.Diagnostics.CodeAnalysis;

using Encina.Audit.Marten.Crypto;
using Encina.Audit.Marten.Events;
using Encina.Security.Audit;

using Marten.Events.Projections;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
/// <item>Resolves <see cref="ITemporalKeyProvider"/> from DI to retrieve decryption keys</item>
/// <item>Decrypts PII fields using the temporal key for the entry's period</item>
/// <item>If the key has been destroyed (crypto-shredded), substitutes <c>[SHREDDED]</c>
/// and sets <see cref="AuditEntryReadModel.IsShredded"/> to <c>true</c></item>
/// <item>Stores the resulting <see cref="AuditEntryReadModel"/> as a Marten document</item>
/// </list>
/// </para>
/// </remarks>
public sealed class AuditEntryProjection : EventProjection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuditEntryProjection"/> class.
    /// </summary>
    public AuditEntryProjection()
    {
        Name = "AuditEntryProjection";
    }

    /// <summary>
    /// Creates an <see cref="AuditEntryReadModel"/> document from an <see cref="AuditEntryRecordedEvent"/>.
    /// </summary>
    /// <param name="event">The event containing the encrypted audit entry data.</param>
    /// <param name="services">The scoped service provider for resolving dependencies.</param>
    /// <returns>The projected read model document with decrypted (or shredded) PII fields.</returns>
    /// <remarks>
    /// Marten invokes this method for each <see cref="AuditEntryRecordedEvent"/> in the event stream.
    /// The <paramref name="services"/> parameter provides access to the DI container for resolving
    /// <see cref="ITemporalKeyProvider"/> and <see cref="MartenAuditOptions"/>.
    /// </remarks>
    [SuppressMessage("Performance", "CA1822:Mark members as static",
        Justification = "Marten's EventProjection convention requires instance methods for Create/Apply.")]
    public async Task<AuditEntryReadModel> Create(
        AuditEntryRecordedEvent @event,
        IServiceProvider services)
    {
        var evt = @event;
        var keyProvider = services.GetRequiredService<ITemporalKeyProvider>();
        var options = services.GetRequiredService<IOptions<MartenAuditOptions>>().Value;
        var logger = services.GetRequiredService<ILogger<AuditEntryProjection>>();

        // Attempt to retrieve the temporal key for decryption
        byte[]? keyMaterial = null;
        var isShredded = false;

        var keyResult = await keyProvider.GetKeyAsync(evt.TemporalKeyPeriod)
            .ConfigureAwait(false);

        keyResult.Match(
            Right: keyInfo => keyMaterial = keyInfo.KeyMaterial,
            Left: _ =>
            {
                isShredded = true;
                logger.LogDebug(
                    "Temporal key for period {Period} not found — marking audit entry {EntryId} as shredded",
                    evt.TemporalKeyPeriod,
                    evt.Id);
            });

        var placeholder = options.ShreddedPlaceholder;

        return new AuditEntryReadModel
        {
            // Identity
            Id = evt.Id,

            // Structural fields (always plaintext)
            CorrelationId = evt.CorrelationId,
            Action = evt.Action,
            EntityType = evt.EntityType,
            EntityId = evt.EntityId,
            Outcome = (AuditOutcome)evt.Outcome,
            ErrorMessage = evt.ErrorMessage,
            TimestampUtc = evt.TimestampUtc,
            StartedAtUtc = evt.StartedAtUtc,
            CompletedAtUtc = evt.CompletedAtUtc,
            RequestPayloadHash = evt.RequestPayloadHash,
            TenantId = evt.TenantId,

            // PII fields (decrypted or shredded)
            UserId = evt.EncryptedUserId?.DecryptOrPlaceholder(keyMaterial, placeholder),
            IpAddress = evt.EncryptedIpAddress?.DecryptOrPlaceholder(keyMaterial, placeholder),
            UserAgent = evt.EncryptedUserAgent?.DecryptOrPlaceholder(keyMaterial, placeholder),
            RequestPayload = evt.EncryptedRequestPayload?.DecryptOrPlaceholder(keyMaterial, placeholder),
            ResponsePayload = evt.EncryptedResponsePayload?.DecryptOrPlaceholder(keyMaterial, placeholder),
            MetadataJson = evt.EncryptedMetadata?.DecryptOrPlaceholder(keyMaterial, placeholder),

            // Crypto-shredding tracking
            IsShredded = isShredded,
            TemporalKeyPeriod = evt.TemporalKeyPeriod
        };
    }
}

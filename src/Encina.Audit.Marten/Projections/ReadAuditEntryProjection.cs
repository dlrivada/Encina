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
/// using the temporal key for the entry's period. If the key has been destroyed,
/// PII fields are replaced with <c>[SHREDDED]</c> placeholders.
/// </para>
/// </remarks>
public sealed class ReadAuditEntryProjection : EventProjection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReadAuditEntryProjection"/> class.
    /// </summary>
    public ReadAuditEntryProjection()
    {
        Name = "ReadAuditEntryProjection";
    }

    /// <summary>
    /// Creates a <see cref="ReadAuditEntryReadModel"/> document from a
    /// <see cref="ReadAuditEntryRecordedEvent"/>.
    /// </summary>
    /// <param name="event">The event containing the encrypted read audit entry data.</param>
    /// <param name="services">The scoped service provider for resolving dependencies.</param>
    /// <returns>The projected read model document with decrypted (or shredded) PII fields.</returns>
    [SuppressMessage("Performance", "CA1822:Mark members as static",
        Justification = "Marten's EventProjection convention requires instance methods for Create/Apply.")]
    public async Task<ReadAuditEntryReadModel> Create(
        ReadAuditEntryRecordedEvent @event,
        IServiceProvider services)
    {
        var evt = @event;
        var keyProvider = services.GetRequiredService<ITemporalKeyProvider>();
        var options = services.GetRequiredService<IOptions<MartenAuditOptions>>().Value;
        var logger = services.GetRequiredService<ILogger<ReadAuditEntryProjection>>();

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
                    "Temporal key for period {Period} not found — marking read audit entry {EntryId} as shredded",
                    evt.TemporalKeyPeriod,
                    evt.Id);
            });

        var placeholder = options.ShreddedPlaceholder;

        return new ReadAuditEntryReadModel
        {
            // Identity
            Id = evt.Id,

            // Structural fields (always plaintext)
            EntityType = evt.EntityType,
            EntityId = evt.EntityId,
            AccessedAtUtc = evt.AccessedAtUtc,
            AccessMethod = (ReadAccessMethod)evt.AccessMethod,
            EntityCount = evt.EntityCount,
            CorrelationId = evt.CorrelationId,
            TenantId = evt.TenantId,

            // PII fields (decrypted or shredded)
            UserId = evt.EncryptedUserId?.DecryptOrPlaceholder(keyMaterial, placeholder),
            Purpose = evt.EncryptedPurpose?.DecryptOrPlaceholder(keyMaterial, placeholder),
            MetadataJson = evt.EncryptedMetadata?.DecryptOrPlaceholder(keyMaterial, placeholder),

            // Crypto-shredding tracking
            IsShredded = isShredded,
            TemporalKeyPeriod = evt.TemporalKeyPeriod
        };
    }
}

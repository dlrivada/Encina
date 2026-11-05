using System.Security.Cryptography;

using Encina.Audit.Marten;
using Encina.Audit.Marten.Events;
using Encina.Audit.Marten.Projections;

using FsCheck;
using FsCheck.Xunit;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.PropertyTests;

/// <summary>
/// Issue #3049 (D16): whatever the key outcome and whatever the metadata, a read model with
/// <c>MetadataJsonMasked</c> set always holds a placeholder in <c>MetadataJson</c>, and the flag is set exactly when
/// the metadata was substituted (key destroyed and metadata present).
/// </summary>
[Trait("Category", "Property")]
[Trait("Provider", "Marten")]
public sealed class AuditMartenMetadataMaskedProperties
{
    private const string Shredded = "<S-PLACEHOLDER>";
    private const string Expired = "<E-PLACEHOLDER>";
    private static readonly byte[] Key = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();

    [Property(MaxTest = 200)]
    public bool OperationProjection_MaskedImpliesPlaceholder_AndFlagMeansSubstituted(
        bool shredded, bool expired, bool hasMetadata, NonEmptyString metadata)
    {
        var projection = new OperationAuditEntryProjection(Shredded, Expired, NullLogger<OperationAuditEntryProjection>.Instance);
        var evt = new OperationAuditEntryRecordedEvent
        {
            Id = Guid.NewGuid(),
            CorrelationId = "c",
            Action = "a",
            EntityType = "E",
            Outcome = 0,
            TimestampUtc = DateTime.UtcNow,
            StartedAtUtc = DateTimeOffset.UtcNow,
            CompletedAtUtc = DateTimeOffset.UtcNow,
            EncryptedMetadata = hasMetadata ? EncryptedField.Encrypt(metadata.Get, Key, "kid-1") : null,
            TemporalKeyPeriod = "2099-12"
        };
        var destroyed = shredded || expired;

        var model = projection.MapToReadModel(evt, destroyed ? null : Key, shredded, expired);

        return Holds(model.MetadataJsonMasked, model.MetadataJson, expired, destroyed, hasMetadata);
    }

    [Property(MaxTest = 200)]
    public bool ReadProjection_MaskedImpliesPlaceholder_AndFlagMeansSubstituted(
        bool shredded, bool expired, bool hasMetadata, NonEmptyString metadata)
    {
        var projection = new ReadAuditEntryProjection(Shredded, Expired, NullLogger<ReadAuditEntryProjection>.Instance);
        var evt = new ReadAuditEntryRecordedEvent
        {
            Id = Guid.NewGuid(),
            EntityType = "E",
            AccessedAtUtc = DateTimeOffset.UtcNow,
            AccessMethod = 0,
            EntityCount = 1,
            EncryptedMetadata = hasMetadata ? EncryptedField.Encrypt(metadata.Get, Key, "kid-1") : null,
            TemporalKeyPeriod = "2099-12"
        };
        var destroyed = shredded || expired;

        var model = projection.MapToReadModel(evt, destroyed ? null : Key, shredded, expired);

        return Holds(model.MetadataJsonMasked, model.MetadataJson, expired, destroyed, hasMetadata);
    }

    private static bool Holds(bool masked, string? metadataJson, bool expired, bool destroyed, bool hasMetadata)
    {
        var placeholder = expired ? Expired : Shredded;
        var impliesPlaceholder = !masked || metadataJson == placeholder;
        var flagMeansSubstituted = masked == (destroyed && hasMetadata);
        return impliesPlaceholder && flagMeansSubstituted;
    }
}

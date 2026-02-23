using Encina.Compliance.Consent;

namespace Encina.EntityFrameworkCore.Consent;

/// <summary>
/// Entity Framework Core entity for <see cref="ConsentRecord"/>.
/// </summary>
/// <remarks>
/// Maps to the ConsentRecords database table. Stores consent metadata as JSON text.
/// DateTimeOffset values are used to match the domain model; EF Core handles
/// provider-specific storage format conversion.
/// </remarks>
public sealed class ConsentRecordEntity
{
    /// <summary>Unique identifier for this consent record.</summary>
    public required Guid Id { get; set; }

    /// <summary>Identifier of the data subject who gave consent.</summary>
    public required string SubjectId { get; set; }

    /// <summary>The specific processing purpose for which consent was given.</summary>
    public required string Purpose { get; set; }

    /// <summary>The current status of this consent record.</summary>
    public ConsentStatus Status { get; set; }

    /// <summary>Identifier of the consent version the data subject agreed to.</summary>
    public required string ConsentVersionId { get; set; }

    /// <summary>Timestamp when the data subject gave consent (UTC).</summary>
    public DateTimeOffset GivenAtUtc { get; set; }

    /// <summary>Timestamp when the data subject withdrew consent (UTC).</summary>
    public DateTimeOffset? WithdrawnAtUtc { get; set; }

    /// <summary>Timestamp when this consent expires (UTC).</summary>
    public DateTimeOffset? ExpiresAtUtc { get; set; }

    /// <summary>The source or channel through which consent was collected.</summary>
    public required string Source { get; set; }

    /// <summary>The IP address of the data subject at the time consent was given.</summary>
    public string? IpAddress { get; set; }

    /// <summary>Hash or reference to the consent form shown to the data subject.</summary>
    public string? ProofOfConsent { get; set; }

    /// <summary>Additional metadata serialized as JSON.</summary>
    public string? Metadata { get; set; }
}

using Encina.Compliance.Consent;

namespace Encina.EntityFrameworkCore.Consent;

/// <summary>
/// Entity Framework Core entity for <see cref="ConsentAuditEntry"/>.
/// </summary>
/// <remarks>
/// Maps to the ConsentAuditEntries database table. Provides an immutable audit trail
/// for consent-related actions as required by GDPR Article 7(1).
/// </remarks>
public sealed class ConsentAuditEntryEntity
{
    /// <summary>Unique identifier for this audit entry.</summary>
    public required Guid Id { get; set; }

    /// <summary>Identifier of the data subject whose consent was affected.</summary>
    public required string SubjectId { get; set; }

    /// <summary>The processing purpose associated with this consent action.</summary>
    public required string Purpose { get; set; }

    /// <summary>The type of consent action that was performed.</summary>
    public ConsentAuditAction Action { get; set; }

    /// <summary>Timestamp when the action occurred (UTC).</summary>
    public DateTimeOffset OccurredAtUtc { get; set; }

    /// <summary>Identifier of the actor who performed or triggered the action.</summary>
    public required string PerformedBy { get; set; }

    /// <summary>The IP address of the actor at the time of the action.</summary>
    public string? IpAddress { get; set; }

    /// <summary>Additional metadata serialized as JSON.</summary>
    public string? Metadata { get; set; }
}

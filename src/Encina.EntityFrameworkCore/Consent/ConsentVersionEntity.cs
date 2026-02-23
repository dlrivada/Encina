using Encina.Compliance.Consent;

namespace Encina.EntityFrameworkCore.Consent;

/// <summary>
/// Entity Framework Core entity for <see cref="ConsentVersion"/>.
/// </summary>
/// <remarks>
/// Maps to the ConsentVersions database table. Tracks versions of consent terms
/// for specific processing purposes, enabling reconsent detection when terms change.
/// </remarks>
public sealed class ConsentVersionEntity
{
    /// <summary>Unique identifier for this consent version.</summary>
    public required string VersionId { get; set; }

    /// <summary>The processing purpose this version applies to.</summary>
    public required string Purpose { get; set; }

    /// <summary>Timestamp from which this version of the consent terms is effective (UTC).</summary>
    public DateTimeOffset EffectiveFromUtc { get; set; }

    /// <summary>Human-readable description of what changed in this version.</summary>
    public required string Description { get; set; }

    /// <summary>Whether existing consents under previous versions must be explicitly renewed.</summary>
    public bool RequiresExplicitReconsent { get; set; }
}

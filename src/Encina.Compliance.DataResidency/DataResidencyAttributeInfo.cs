namespace Encina.Compliance.DataResidency;

/// <summary>
/// Cached attribute information for a request type's data residency declarations.
/// </summary>
/// <param name="AllowedRegionCodes">The region codes where data processing is permitted.</param>
/// <param name="DataCategory">The data category for policy resolution, or <c>null</c> to use the request type name.</param>
/// <param name="RequireAdequacyDecision">Whether an EU adequacy decision is required for the processing region.</param>
internal sealed record DataResidencyAttributeInfo(
    string[] AllowedRegionCodes,
    string? DataCategory,
    bool RequireAdequacyDecision);

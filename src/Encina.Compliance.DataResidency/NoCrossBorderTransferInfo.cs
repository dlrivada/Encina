namespace Encina.Compliance.DataResidency;

/// <summary>
/// Cached attribute information for a request type's no-cross-border transfer declaration.
/// </summary>
/// <param name="DataCategory">The data category for policy association, or <c>null</c> to use the request type name.</param>
/// <param name="Reason">The documented reason for the no-cross-border restriction.</param>
internal sealed record NoCrossBorderTransferInfo(
    string? DataCategory,
    string? Reason);

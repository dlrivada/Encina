using System.Globalization;

namespace Encina.Audit.Marten.Resolution;

/// <summary>
/// Builds the deterministic <see cref="AuditResolution"/> document id and knows which fields belong to which trail.
/// </summary>
internal static class AuditResolutionIds
{
    private const string Prefix = "audit-resolution:";

    /// <summary>
    /// Returns the document id <c>"audit-resolution:{trail}:{eventId}:{field}"</c> (event id in the <c>D</c> format).
    /// </summary>
    internal static string Format(AuditResolutionTrail trail, Guid eventId, AuditResolutionField field) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{Prefix}{trail}:{eventId:D}:{field}");

    /// <summary>
    /// Returns whether <paramref name="field"/> may be resolved on <paramref name="trail"/>:
    /// <see cref="AuditResolutionField.All"/> on either, otherwise only the fields of that trail's read model.
    /// </summary>
    internal static bool IsValid(AuditResolutionTrail trail, AuditResolutionField field) => (trail, field) switch
    {
        (_, AuditResolutionField.All) => true,
        (_, AuditResolutionField.UserId or AuditResolutionField.MetadataJson) => Enum.IsDefined(trail),
        (AuditResolutionTrail.Operation, AuditResolutionField.IpAddress
            or AuditResolutionField.UserAgent
            or AuditResolutionField.RequestPayload
            or AuditResolutionField.ResponsePayload) => true,
        (AuditResolutionTrail.Read, AuditResolutionField.Purpose) => true,
        _ => false
    };
}

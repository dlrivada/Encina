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
    internal static bool IsValid(AuditResolutionTrail trail, AuditResolutionField field) =>
        ValidFields.TryGetValue(trail, out var fields) && fields.Contains(field);

    private static readonly HashSet<AuditResolutionField> OperationFields =
    [
        AuditResolutionField.All,
        AuditResolutionField.UserId,
        AuditResolutionField.IpAddress,
        AuditResolutionField.UserAgent,
        AuditResolutionField.RequestPayload,
        AuditResolutionField.ResponsePayload,
        AuditResolutionField.MetadataJson
    ];

    private static readonly HashSet<AuditResolutionField> ReadFields =
    [
        AuditResolutionField.All,
        AuditResolutionField.UserId,
        AuditResolutionField.Purpose,
        AuditResolutionField.MetadataJson
    ];

    // Declared after the sets it references: static initialisers run in textual order.
    private static readonly Dictionary<AuditResolutionTrail, HashSet<AuditResolutionField>> ValidFields = new()
    {
        [AuditResolutionTrail.Operation] = OperationFields,
        [AuditResolutionTrail.Read] = ReadFields
    };
}

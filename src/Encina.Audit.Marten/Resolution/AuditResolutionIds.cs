namespace Encina.Audit.Marten.Resolution;

/// <summary>
/// Creates <see cref="AuditResolution"/> ids and knows which fields belong to which trail.
/// </summary>
internal static class AuditResolutionIds
{
    /// <summary>
    /// Creates a new resolution id: a UUID version 7 whose timestamp comes from <paramref name="timeProvider"/>.
    /// Ids created in different milliseconds sort by creation time; ids of the same millisecond are not ordered
    /// (<see cref="AuditResolution.Sequence"/> decides).
    /// </summary>
    internal static Guid NewId(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        return Guid.CreateVersion7(timeProvider.GetUtcNow());
    }

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

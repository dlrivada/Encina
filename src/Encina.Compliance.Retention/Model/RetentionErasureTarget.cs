namespace Encina.Compliance.Retention.Model;

/// <summary>
/// Identifies the data that one expired retention record governs: the data of one
/// <see cref="DataCategory"/> held by one entity.
/// </summary>
/// <remarks>
/// <para>
/// A retention record is keyed by <see cref="EntityId"/> and <see cref="DataCategory"/>, so one entity can
/// be tracked by several records with different retention periods (for example a patient's contact data
/// kept for one year and their clinical record kept for five). When one of those records expires, only
/// the data of that record's category may be erased; the data of the other categories is still within its
/// retention period and must be left untouched.
/// </para>
/// <para>
/// <see cref="EntityId"/> is the identifier of the tracked entity (the value captured by
/// <c>IRetentionRecordService.TrackEntityAsync</c>), not the identifier of a data subject. An entity may
/// hold personal data of one subject, of several, or of none.
/// </para>
/// </remarks>
public sealed record RetentionErasureTarget
{
    /// <summary>
    /// The identifier of the expired retention record.
    /// </summary>
    /// <remarks>
    /// Stable across retries: when a cycle fails after erasing but before the record is marked deleted,
    /// the next cycle asks the eraser for the same record again.
    /// </remarks>
    public required Guid RecordId { get; init; }

    /// <summary>
    /// The identifier of the entity whose data is erased.
    /// </summary>
    public required string EntityId { get; init; }

    /// <summary>
    /// The retention data category whose period expired. Only data of this category is erased.
    /// </summary>
    public required string DataCategory { get; init; }

    /// <summary>
    /// The UTC timestamp at which the retention period of this record expired.
    /// </summary>
    public required DateTimeOffset ExpiresAtUtc { get; init; }

    /// <summary>
    /// The tenant the record belongs to, or <c>null</c> when multi-tenancy is not used.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// The module the record belongs to, or <c>null</c> when module isolation is not used.
    /// </summary>
    public string? ModuleId { get; init; }
}

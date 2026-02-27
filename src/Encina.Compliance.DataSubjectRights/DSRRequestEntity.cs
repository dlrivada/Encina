namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Persistence entity for <see cref="DSRRequest"/>.
/// </summary>
/// <remarks>
/// <para>
/// This entity provides a database-agnostic representation of a DSR request,
/// using primitive types suitable for any storage provider (ADO.NET, Dapper, EF Core, MongoDB).
/// </para>
/// <para>
/// Enum values (<see cref="DataSubjectRight"/> and <see cref="DSRRequestStatus"/>) are stored
/// as integers for cross-provider compatibility. Timestamps use <see cref="DateTimeOffset"/>
/// for UTC precision across all providers.
/// </para>
/// <para>
/// Use <see cref="DSRRequestMapper"/> to convert between this entity and <see cref="DSRRequest"/>.
/// </para>
/// </remarks>
public sealed class DSRRequestEntity
{
    /// <summary>
    /// Unique identifier for this DSR request record (GUID as string).
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Identifier of the data subject who submitted the request.
    /// </summary>
    public required string SubjectId { get; set; }

    /// <summary>
    /// Integer value of the <see cref="DataSubjectRight"/> enum.
    /// </summary>
    public required int RightTypeValue { get; set; }

    /// <summary>
    /// Integer value of the <see cref="DSRRequestStatus"/> enum.
    /// </summary>
    public required int StatusValue { get; set; }

    /// <summary>
    /// Timestamp when the request was received (UTC).
    /// </summary>
    public DateTimeOffset ReceivedAtUtc { get; set; }

    /// <summary>
    /// Deadline by which the request must be completed (UTC).
    /// </summary>
    public DateTimeOffset DeadlineAtUtc { get; set; }

    /// <summary>
    /// Timestamp when the request was completed or rejected (UTC), if applicable.
    /// </summary>
    public DateTimeOffset? CompletedAtUtc { get; set; }

    /// <summary>
    /// Reason for extending the response deadline, if applicable.
    /// </summary>
    public string? ExtensionReason { get; set; }

    /// <summary>
    /// Extended deadline when additional time is granted (UTC), if applicable.
    /// </summary>
    public DateTimeOffset? ExtendedDeadlineAtUtc { get; set; }

    /// <summary>
    /// Reason for rejecting the request, if applicable.
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Additional details or context provided with the request.
    /// </summary>
    public string? RequestDetails { get; set; }

    /// <summary>
    /// Timestamp when the data subject's identity was verified (UTC), if applicable.
    /// </summary>
    public DateTimeOffset? VerifiedAtUtc { get; set; }

    /// <summary>
    /// Identifier of the user or system that processed the request, if applicable.
    /// </summary>
    public string? ProcessedByUserId { get; set; }
}

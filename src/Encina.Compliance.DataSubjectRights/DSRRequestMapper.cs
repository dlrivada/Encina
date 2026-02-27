namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Maps between <see cref="DSRRequest"/> domain records and
/// <see cref="DSRRequestEntity"/> persistence entities.
/// </summary>
/// <remarks>
/// <para>
/// This mapper handles the conversion of <see cref="DataSubjectRight"/> and
/// <see cref="DSRRequestStatus"/> enum values to/from integers for cross-provider persistence.
/// </para>
/// <para>
/// Used by store implementations (ADO.NET, Dapper, EF Core, MongoDB) to persist and
/// retrieve DSR requests without coupling to the domain model.
/// </para>
/// </remarks>
public static class DSRRequestMapper
{
    /// <summary>
    /// Converts a domain <see cref="DSRRequest"/> to a persistence entity.
    /// </summary>
    /// <param name="request">The domain request to convert.</param>
    /// <returns>A <see cref="DSRRequestEntity"/> suitable for persistence.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is <c>null</c>.</exception>
    /// <remarks>
    /// A new GUID is generated for the entity's <see cref="DSRRequestEntity.Id"/> to ensure
    /// unique persistence-layer identifiers.
    /// </remarks>
    public static DSRRequestEntity ToEntity(DSRRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new DSRRequestEntity
        {
            Id = Guid.NewGuid().ToString("D"),
            SubjectId = request.SubjectId,
            RightTypeValue = (int)request.RightType,
            StatusValue = (int)request.Status,
            ReceivedAtUtc = request.ReceivedAtUtc,
            DeadlineAtUtc = request.DeadlineAtUtc,
            CompletedAtUtc = request.CompletedAtUtc,
            ExtensionReason = request.ExtensionReason,
            ExtendedDeadlineAtUtc = request.ExtendedDeadlineAtUtc,
            RejectionReason = request.RejectionReason,
            RequestDetails = request.RequestDetails,
            VerifiedAtUtc = request.VerifiedAtUtc,
            ProcessedByUserId = request.ProcessedByUserId
        };
    }

    /// <summary>
    /// Converts a persistence entity back to a domain <see cref="DSRRequest"/>.
    /// </summary>
    /// <param name="entity">The persistence entity to convert.</param>
    /// <returns>
    /// A <see cref="DSRRequest"/> if the entity state is valid (enum values are defined),
    /// or <c>null</c> if the entity contains invalid enum values.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is <c>null</c>.</exception>
    public static DSRRequest? ToDomain(DSRRequestEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (!Enum.IsDefined(typeof(DataSubjectRight), entity.RightTypeValue) ||
            !Enum.IsDefined(typeof(DSRRequestStatus), entity.StatusValue))
        {
            return null;
        }

        return new DSRRequest
        {
            Id = entity.Id,
            SubjectId = entity.SubjectId,
            RightType = (DataSubjectRight)entity.RightTypeValue,
            Status = (DSRRequestStatus)entity.StatusValue,
            ReceivedAtUtc = entity.ReceivedAtUtc,
            DeadlineAtUtc = entity.DeadlineAtUtc,
            CompletedAtUtc = entity.CompletedAtUtc,
            ExtensionReason = entity.ExtensionReason,
            ExtendedDeadlineAtUtc = entity.ExtendedDeadlineAtUtc,
            RejectionReason = entity.RejectionReason,
            RequestDetails = entity.RequestDetails,
            VerifiedAtUtc = entity.VerifiedAtUtc,
            ProcessedByUserId = entity.ProcessedByUserId
        };
    }
}

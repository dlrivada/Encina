using Encina.Compliance.Retention.Model;

namespace Encina.Compliance.Retention;

/// <summary>
/// Maps between <see cref="RetentionRecord"/> domain records and
/// <see cref="RetentionRecordEntity"/> persistence entities.
/// </summary>
/// <remarks>
/// <para>
/// This mapper handles the conversion of <see cref="RetentionStatus"/> enum values
/// to/from integers for cross-provider persistence. Invalid enum values in a
/// persistence entity result in a <c>null</c> return from <see cref="ToDomain"/>.
/// </para>
/// <para>
/// Used by store implementations (ADO.NET, Dapper, EF Core, MongoDB) to persist and
/// retrieve retention records without coupling to the domain model.
/// </para>
/// </remarks>
public static class RetentionRecordMapper
{
    /// <summary>
    /// Converts a domain <see cref="RetentionRecord"/> to a persistence entity.
    /// </summary>
    /// <param name="record">The domain record to convert.</param>
    /// <returns>A <see cref="RetentionRecordEntity"/> suitable for persistence.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="record"/> is <c>null</c>.</exception>
    public static RetentionRecordEntity ToEntity(RetentionRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new RetentionRecordEntity
        {
            Id = record.Id,
            EntityId = record.EntityId,
            DataCategory = record.DataCategory,
            PolicyId = record.PolicyId,
            CreatedAtUtc = record.CreatedAtUtc,
            ExpiresAtUtc = record.ExpiresAtUtc,
            StatusValue = (int)record.Status,
            DeletedAtUtc = record.DeletedAtUtc,
            LegalHoldId = record.LegalHoldId
        };
    }

    /// <summary>
    /// Converts a persistence entity back to a domain <see cref="RetentionRecord"/>.
    /// </summary>
    /// <param name="entity">The persistence entity to convert.</param>
    /// <returns>
    /// A <see cref="RetentionRecord"/> if the entity state is valid (enum values are defined),
    /// or <c>null</c> if the entity contains invalid enum values.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is <c>null</c>.</exception>
    public static RetentionRecord? ToDomain(RetentionRecordEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (!Enum.IsDefined(typeof(RetentionStatus), entity.StatusValue))
        {
            return null;
        }

        return new RetentionRecord
        {
            Id = entity.Id,
            EntityId = entity.EntityId,
            DataCategory = entity.DataCategory,
            PolicyId = entity.PolicyId,
            CreatedAtUtc = entity.CreatedAtUtc,
            ExpiresAtUtc = entity.ExpiresAtUtc,
            Status = (RetentionStatus)entity.StatusValue,
            DeletedAtUtc = entity.DeletedAtUtc,
            LegalHoldId = entity.LegalHoldId
        };
    }
}

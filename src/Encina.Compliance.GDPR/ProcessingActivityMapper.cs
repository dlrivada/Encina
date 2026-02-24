using System.Text.Json;

namespace Encina.Compliance.GDPR;

/// <summary>
/// Maps between <see cref="ProcessingActivity"/> domain records and
/// <see cref="ProcessingActivityEntity"/> persistence entities.
/// </summary>
/// <remarks>
/// <para>
/// This mapper handles the conversion of <see cref="Type"/> references to/from
/// assembly-qualified name strings, <see cref="LawfulBasis"/> enum values to/from integers,
/// <see cref="TimeSpan"/> to/from ticks, and <c>IReadOnlyList&lt;string&gt;</c> collections
/// to/from JSON strings.
/// </para>
/// <para>
/// Used by store implementations (ADO.NET, Dapper, EF Core, MongoDB) to persist and
/// retrieve processing activities without coupling to the domain model.
/// </para>
/// </remarks>
public static class ProcessingActivityMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Converts a domain <see cref="ProcessingActivity"/> to a persistence entity.
    /// </summary>
    /// <param name="activity">The domain activity to convert.</param>
    /// <returns>A <see cref="ProcessingActivityEntity"/> suitable for persistence.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="activity"/> is <c>null</c>.</exception>
    public static ProcessingActivityEntity ToEntity(ProcessingActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        return new ProcessingActivityEntity
        {
            Id = activity.Id.ToString("D"),
            RequestTypeName = activity.RequestType.AssemblyQualifiedName!,
            Name = activity.Name,
            Purpose = activity.Purpose,
            LawfulBasisValue = (int)activity.LawfulBasis,
            CategoriesOfDataSubjectsJson = JsonSerializer.Serialize(activity.CategoriesOfDataSubjects, JsonOptions),
            CategoriesOfPersonalDataJson = JsonSerializer.Serialize(activity.CategoriesOfPersonalData, JsonOptions),
            RecipientsJson = JsonSerializer.Serialize(activity.Recipients, JsonOptions),
            ThirdCountryTransfers = activity.ThirdCountryTransfers,
            Safeguards = activity.Safeguards,
            RetentionPeriodTicks = activity.RetentionPeriod.Ticks,
            SecurityMeasures = activity.SecurityMeasures,
            CreatedAtUtc = activity.CreatedAtUtc,
            LastUpdatedAtUtc = activity.LastUpdatedAtUtc
        };
    }

    /// <summary>
    /// Converts a persistence entity back to a domain <see cref="ProcessingActivity"/>.
    /// </summary>
    /// <param name="entity">The persistence entity to convert.</param>
    /// <returns>
    /// A <see cref="ProcessingActivity"/> if the <see cref="ProcessingActivityEntity.RequestTypeName"/>
    /// can be resolved to a <see cref="Type"/>, or <c>null</c> if the type cannot be found.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is <c>null</c>.</exception>
    public static ProcessingActivity? ToDomain(ProcessingActivityEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var requestType = Type.GetType(entity.RequestTypeName);
        if (requestType is null)
        {
            return null;
        }

        return new ProcessingActivity
        {
            Id = Guid.Parse(entity.Id),
            RequestType = requestType,
            Name = entity.Name,
            Purpose = entity.Purpose,
            LawfulBasis = (LawfulBasis)entity.LawfulBasisValue,
            CategoriesOfDataSubjects = JsonSerializer.Deserialize<List<string>>(entity.CategoriesOfDataSubjectsJson, JsonOptions) ?? [],
            CategoriesOfPersonalData = JsonSerializer.Deserialize<List<string>>(entity.CategoriesOfPersonalDataJson, JsonOptions) ?? [],
            Recipients = JsonSerializer.Deserialize<List<string>>(entity.RecipientsJson, JsonOptions) ?? [],
            ThirdCountryTransfers = entity.ThirdCountryTransfers,
            Safeguards = entity.Safeguards,
            RetentionPeriod = TimeSpan.FromTicks(entity.RetentionPeriodTicks),
            SecurityMeasures = entity.SecurityMeasures,
            CreatedAtUtc = entity.CreatedAtUtc,
            LastUpdatedAtUtc = entity.LastUpdatedAtUtc
        };
    }
}

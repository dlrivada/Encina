namespace Encina.Compliance.GDPR;

/// <summary>
/// Persistence entity for <see cref="ProcessingActivity"/>.
/// </summary>
/// <remarks>
/// <para>
/// This entity provides a database-agnostic representation of a processing activity record,
/// using primitive types suitable for any storage provider (ADO.NET, Dapper, EF Core, MongoDB).
/// </para>
/// <para>
/// The three JSON fields (<see cref="CategoriesOfDataSubjectsJson"/>, <see cref="CategoriesOfPersonalDataJson"/>,
/// <see cref="RecipientsJson"/>) store <c>IReadOnlyList&lt;string&gt;</c> collections serialized with
/// <see cref="System.Text.Json.JsonNamingPolicy.CamelCase"/>.
/// </para>
/// <para>
/// Use <see cref="ProcessingActivityMapper"/> to convert between this entity and
/// <see cref="ProcessingActivity"/>.
/// </para>
/// </remarks>
public sealed class ProcessingActivityEntity
{
    /// <summary>
    /// Unique identifier for this processing activity record (GUID as string, format "D").
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Assembly-qualified name of the request type this activity applies to.
    /// </summary>
    public required string RequestTypeName { get; set; }

    /// <summary>
    /// Human-readable name describing this processing activity.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Purpose of the processing as required by Article 30(1)(b).
    /// </summary>
    public required string Purpose { get; set; }

    /// <summary>
    /// Integer value of the <see cref="LawfulBasis"/> enum.
    /// </summary>
    public required int LawfulBasisValue { get; set; }

    /// <summary>
    /// JSON-serialized array of data subject categories (Article 30(1)(c)).
    /// </summary>
    public required string CategoriesOfDataSubjectsJson { get; set; }

    /// <summary>
    /// JSON-serialized array of personal data categories (Article 30(1)(c)).
    /// </summary>
    public required string CategoriesOfPersonalDataJson { get; set; }

    /// <summary>
    /// JSON-serialized array of recipient categories (Article 30(1)(d)).
    /// </summary>
    public required string RecipientsJson { get; set; }

    /// <summary>
    /// Description of third-country transfers (Article 30(1)(e)), or <c>null</c> if none.
    /// </summary>
    public string? ThirdCountryTransfers { get; set; }

    /// <summary>
    /// Safeguards for third-country transfers (Article 46), or <c>null</c> if none.
    /// </summary>
    public string? Safeguards { get; set; }

    /// <summary>
    /// Retention period stored as <see cref="TimeSpan.Ticks"/> for lossless round-tripping.
    /// </summary>
    public required long RetentionPeriodTicks { get; set; }

    /// <summary>
    /// Technical and organizational security measures (Article 30(1)(g)).
    /// </summary>
    public required string SecurityMeasures { get; set; }

    /// <summary>
    /// Timestamp when this record was created (UTC).
    /// </summary>
    public required DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>
    /// Timestamp when this record was last updated (UTC).
    /// </summary>
    public required DateTimeOffset LastUpdatedAtUtc { get; set; }
}

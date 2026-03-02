namespace Encina.Compliance.Retention;

/// <summary>
/// Persistence entity for <see cref="Model.RetentionPolicy"/>.
/// </summary>
/// <remarks>
/// <para>
/// This entity provides a database-agnostic representation of a retention policy,
/// using primitive types suitable for any storage provider (ADO.NET, Dapper, EF Core, MongoDB).
/// </para>
/// <para>
/// Key type transformations:
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="Model.RetentionPolicy.RetentionPeriod"/> (<see cref="TimeSpan"/>) is stored
/// as <see cref="RetentionPeriodTicks"/> (<see cref="long"/>) for database portability.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="Model.RetentionPolicyType"/> is stored as <see cref="PolicyTypeValue"/>
/// (<see cref="int"/>) for cross-provider compatibility.
/// </description>
/// </item>
/// </list>
/// </para>
/// <para>
/// Use <see cref="RetentionPolicyMapper"/> to convert between this entity and
/// <see cref="Model.RetentionPolicy"/>.
/// </para>
/// </remarks>
public sealed class RetentionPolicyEntity
{
    /// <summary>
    /// Unique identifier for this retention policy.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// The data category this policy applies to.
    /// </summary>
    /// <remarks>
    /// A UNIQUE INDEX should be created on this column to enforce one policy per category.
    /// </remarks>
    public required string DataCategory { get; set; }

    /// <summary>
    /// The retention period stored as ticks (<see cref="TimeSpan.Ticks"/>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="TimeSpan"/> is not universally supported across database providers.
    /// Ticks (a <see cref="long"/> value) provide lossless, portable storage.
    /// </para>
    /// <para>
    /// Convert to/from <see cref="TimeSpan"/>: <c>new TimeSpan(ticks)</c> / <c>timeSpan.Ticks</c>.
    /// </para>
    /// </remarks>
    public long RetentionPeriodTicks { get; set; }

    /// <summary>
    /// Whether expired data should be automatically deleted by the enforcement service.
    /// </summary>
    public bool AutoDelete { get; set; }

    /// <summary>
    /// Human-readable reason for this retention period.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// The GDPR lawful basis or legal reference requiring this retention period.
    /// </summary>
    public string? LegalBasis { get; set; }

    /// <summary>
    /// Integer value of the <see cref="Model.RetentionPolicyType"/> enum.
    /// </summary>
    /// <remarks>
    /// Values: TimeBased=0, EventBased=1, ConsentBased=2.
    /// </remarks>
    public required int PolicyTypeValue { get; set; }

    /// <summary>
    /// Timestamp when this policy was created (UTC).
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>
    /// Timestamp when this policy was last modified (UTC), if applicable.
    /// </summary>
    public DateTimeOffset? LastModifiedAtUtc { get; set; }
}

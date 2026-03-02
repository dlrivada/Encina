namespace Encina.Compliance.Retention.Model;

/// <summary>
/// Classification of the trigger mechanism for a retention policy.
/// </summary>
/// <remarks>
/// <para>
/// Different data categories have different retention triggers. Financial records
/// are typically time-based (e.g., 7 years from creation). Marketing consent data
/// is consent-based (retained until consent is withdrawn). Event-based policies
/// trigger retention from a specific business event (e.g., contract termination).
/// </para>
/// <para>
/// Per GDPR Article 5(1)(e), data shall be kept in a form which permits identification
/// of data subjects for no longer than is necessary for the purposes for which the
/// personal data are processed. The policy type determines how "necessary" is evaluated.
/// </para>
/// </remarks>
public enum RetentionPolicyType
{
    /// <summary>
    /// Retention period is measured from the data creation timestamp.
    /// </summary>
    /// <remarks>
    /// The most common type. Expiration is calculated as
    /// <c>CreatedAtUtc + RetentionPeriod</c>. Suitable for records with a
    /// fixed legal retention requirement (e.g., tax records: 7 years).
    /// </remarks>
    TimeBased = 0,

    /// <summary>
    /// Retention period starts from a specific business event.
    /// </summary>
    /// <remarks>
    /// Examples: retention starts when a contract terminates, when an employee
    /// leaves the organization, or when a service subscription ends.
    /// The event timestamp must be supplied when creating the retention record.
    /// </remarks>
    EventBased = 1,

    /// <summary>
    /// Data is retained until consent is withdrawn.
    /// </summary>
    /// <remarks>
    /// Per GDPR Article 7(3), the data subject shall have the right to withdraw
    /// consent at any time. When consent is the lawful basis for processing,
    /// data must be deleted upon withdrawal unless another legal basis applies.
    /// </remarks>
    ConsentBased = 2
}

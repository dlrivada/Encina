namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Categories of personal data for classification and grouping purposes.
/// </summary>
/// <remarks>
/// <para>
/// Categories enable granular control over data subject rights operations.
/// For example, a portability request may target only <see cref="Contact"/> data,
/// or an erasure request may exclude <see cref="Financial"/> data due to legal retention.
/// </para>
/// <para>
/// These categories align with common data classification schemes used in
/// Data Protection Impact Assessments (DPIAs) and Records of Processing Activities (RoPA).
/// </para>
/// </remarks>
public enum PersonalDataCategory
{
    /// <summary>
    /// Identity data — name, date of birth, national ID, passport number.
    /// </summary>
    Identity,

    /// <summary>
    /// Contact data — email address, phone number, postal address.
    /// </summary>
    Contact,

    /// <summary>
    /// Financial data — bank account, payment information, salary, tax records.
    /// </summary>
    Financial,

    /// <summary>
    /// Health data — medical records, health conditions, prescriptions.
    /// </summary>
    /// <remarks>Special category data under Article 9. Requires additional safeguards.</remarks>
    Health,

    /// <summary>
    /// Biometric data — fingerprints, facial recognition data, voice patterns.
    /// </summary>
    /// <remarks>Special category data under Article 9 when used for identification purposes.</remarks>
    Biometric,

    /// <summary>
    /// Genetic data — DNA sequences, genetic test results, hereditary information.
    /// </summary>
    /// <remarks>Special category data under Article 9. Subject to heightened protection.</remarks>
    Genetic,

    /// <summary>
    /// Location data — GPS coordinates, travel history, geofencing data.
    /// </summary>
    Location,

    /// <summary>
    /// Online identifiers and activity — IP addresses, cookies, browsing history, device IDs.
    /// </summary>
    /// <remarks>Recital 30 recognizes online identifiers as personal data.</remarks>
    Online,

    /// <summary>
    /// Employment data — job title, employment history, performance reviews, work schedules.
    /// </summary>
    Employment,

    /// <summary>
    /// Education data — academic records, qualifications, training history.
    /// </summary>
    Education,

    /// <summary>
    /// Other data that does not fit into the predefined categories.
    /// </summary>
    Other
}

namespace Encina.Compliance.AIAct.Model;

/// <summary>
/// Severity levels for data gaps identified during training data quality assessment.
/// </summary>
/// <remarks>
/// <para>
/// Article 10(2)(g) requires identification of relevant data gaps or shortcomings
/// that prevent compliance with the data governance requirements, and how those gaps
/// and shortcomings can be addressed.
/// </para>
/// <para>
/// The severity level helps prioritise remediation efforts and determines whether
/// the dataset meets the AI Act requirements for market placement.
/// </para>
/// </remarks>
public enum DataGapSeverity
{
    /// <summary>
    /// The data gap has negligible impact on model quality or fairness.
    /// </summary>
    /// <remarks>May be acceptable without remediation for most use cases.</remarks>
    Low = 0,

    /// <summary>
    /// The data gap may affect model quality in specific scenarios.
    /// </summary>
    /// <remarks>Should be addressed before deployment but does not block development.</remarks>
    Medium = 1,

    /// <summary>
    /// The data gap materially affects model quality, fairness, or representativeness.
    /// </summary>
    /// <remarks>Must be addressed before the AI system can be placed on the market or put into service.</remarks>
    High = 2,

    /// <summary>
    /// The data gap renders the dataset fundamentally unsuitable for its intended purpose.
    /// </summary>
    /// <remarks>The dataset cannot be used for training, validation, or testing until the gap is resolved.</remarks>
    Critical = 3
}

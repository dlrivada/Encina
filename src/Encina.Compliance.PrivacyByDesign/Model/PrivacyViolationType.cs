namespace Encina.Compliance.PrivacyByDesign.Model;

/// <summary>
/// Identifies the category of Privacy by Design violation detected during request validation.
/// </summary>
/// <remarks>
/// <para>
/// Each violation type corresponds to a distinct obligation under GDPR Article 25:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="DataMinimization"/>: Article 25(2) — only necessary data.</description></item>
/// <item><description><see cref="PurposeLimitation"/>: Article 25(1) — processing limited to declared purpose.</description></item>
/// <item><description><see cref="DefaultPrivacy"/>: Article 25(2) — privacy-respecting defaults.</description></item>
/// </list>
/// </remarks>
public enum PrivacyViolationType
{
    /// <summary>
    /// A data minimization violation: the request collects or processes personal data
    /// that is not strictly necessary for the declared purpose.
    /// </summary>
    /// <remarks>
    /// Per GDPR Article 25(2), "the controller shall implement appropriate technical and
    /// organisational measures for ensuring that, by default, only personal data which are
    /// necessary for each specific purpose of the processing are processed."
    /// </remarks>
    DataMinimization = 0,

    /// <summary>
    /// A purpose limitation violation: a field is used outside its declared processing purpose.
    /// </summary>
    /// <remarks>
    /// Per GDPR Article 25(1), measures shall be designed to implement data-protection principles
    /// "such as data minimisation" in an effective manner. Purpose limitation is a core principle
    /// under Article 5(1)(b).
    /// </remarks>
    PurposeLimitation = 1,

    /// <summary>
    /// A default privacy violation: a field's value does not match the privacy-respecting default.
    /// </summary>
    /// <remarks>
    /// Per GDPR Article 25(2), "the controller shall implement appropriate technical and
    /// organisational measures for ensuring that, by default, personal data are not made
    /// accessible without the individual's intervention to an indefinite number of natural persons."
    /// </remarks>
    DefaultPrivacy = 2
}

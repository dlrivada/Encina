using LanguageExt;

namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Locates all personal data associated with a data subject across the system.
/// </summary>
/// <remarks>
/// <para>
/// This interface is the foundation for data subject rights operations. It discovers all
/// personal data locations (entities, fields, and values) for a given data subject by
/// scanning properties marked with <see cref="PersonalDataAttribute"/>.
/// </para>
/// <para>
/// Implementations typically scan registered entity types for <see cref="PersonalDataAttribute"/>-decorated
/// properties and query the relevant data stores to build a complete inventory of the subject's
/// personal data.
/// </para>
/// <para>
/// Used by <see cref="IDataSubjectRightsHandler"/> to fulfill:
/// </para>
/// <list type="bullet">
/// <item><b>Access requests (Article 15)</b>: Locate all data to include in the response</item>
/// <item><b>Erasure requests (Article 17)</b>: Identify which fields to erase</item>
/// <item><b>Portability requests (Article 20)</b>: Determine which data to export</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public class EfCorePersonalDataLocator : IPersonalDataLocator
/// {
///     public async ValueTask&lt;Either&lt;EncinaError, IReadOnlyList&lt;PersonalDataLocation&gt;&gt;&gt;
///         LocateAllDataAsync(string subjectId, CancellationToken cancellationToken)
///     {
///         // Scan all registered entities for [PersonalData] properties
///         // Query each entity's store for the subject's records
///         // Return a flat list of all personal data locations
///     }
/// }
/// </code>
/// </example>
public interface IPersonalDataLocator
{
    /// <summary>
    /// Locates all personal data associated with the specified data subject.
    /// </summary>
    /// <param name="subjectId">The identifier of the data subject.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of <see cref="PersonalDataLocation"/> entries representing every
    /// personal data field found for the subject, or an <see cref="EncinaError"/> on failure.
    /// Returns an empty list if no personal data is found for the subject.
    /// </returns>
    ValueTask<Either<EncinaError, IReadOnlyList<PersonalDataLocation>>> LocateAllDataAsync(
        string subjectId,
        CancellationToken cancellationToken = default);
}

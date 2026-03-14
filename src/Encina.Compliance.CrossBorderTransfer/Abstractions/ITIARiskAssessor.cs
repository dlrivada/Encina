using Encina.Compliance.CrossBorderTransfer.Model;
using LanguageExt;

namespace Encina.Compliance.CrossBorderTransfer.Abstractions;

/// <summary>
/// Strategy interface for pluggable risk assessment of destination countries.
/// </summary>
/// <remarks>
/// <para>
/// Implementations evaluate the level of protection in a destination country's legal framework,
/// considering factors such as government surveillance laws, data protection authority effectiveness,
/// judicial redress mechanisms, and rule of law indicators.
/// </para>
/// <para>
/// The EDPB Recommendations 01/2020 outline the steps for conducting a Transfer Impact Assessment.
/// This interface provides a pluggable mechanism for automating or assisting with the risk
/// assessment step, allowing organizations to integrate their own assessment methodologies
/// or third-party risk databases.
/// </para>
/// <para>
/// A default no-op implementation is registered via <c>TryAdd</c>, allowing consumers to
/// provide their own implementation when needed.
/// </para>
/// </remarks>
public interface ITIARiskAssessor
{
    /// <summary>
    /// Assesses the risk of transferring personal data to the specified destination country.
    /// </summary>
    /// <param name="destinationCountryCode">ISO 3166-1 alpha-2 country code of the data importer.</param>
    /// <param name="dataCategory">Category of personal data being transferred.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the risk assessment result.</returns>
    ValueTask<Either<EncinaError, TIARiskAssessment>> AssessRiskAsync(
        string destinationCountryCode,
        string dataCategory,
        CancellationToken cancellationToken = default);
}

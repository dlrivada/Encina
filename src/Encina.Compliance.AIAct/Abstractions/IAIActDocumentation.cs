using Encina.Compliance.AIAct.Model;

using LanguageExt;

namespace Encina.Compliance.AIAct.Abstractions;

/// <summary>
/// Manages technical documentation for high-risk AI systems as required by
/// Article 11 and Annex IV of the EU AI Act.
/// </summary>
/// <remarks>
/// <para>
/// Article 11(1) requires that the technical documentation of a high-risk AI system
/// be drawn up before that system is placed on the market or put into service and shall
/// be kept up to date. The documentation must demonstrate that the system complies with
/// the requirements set out in Chapter III, Section 2.
/// </para>
/// <para>
/// Annex IV specifies the required content, including: general description, design
/// specifications, data governance practices, risk management measures, accuracy and
/// robustness metrics, and human oversight mechanisms.
/// </para>
/// <para>
/// The default implementation provides a basic documentation scaffold from the
/// <see cref="IAISystemRegistry"/> metadata. Full documentation generation with
/// template support is implemented in child issue #840 ("AI Act Technical
/// Documentation Generation").
/// </para>
/// </remarks>
public interface IAIActDocumentation
{
    /// <summary>
    /// Generates technical documentation for a registered AI system based on its
    /// registry metadata and associated compliance data.
    /// </summary>
    /// <param name="systemId">The unique identifier of the AI system.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="TechnicalDocumentation"/> record populated with available information,
    /// or an <see cref="EncinaError"/> if the system is not registered or generation fails.
    /// </returns>
    /// <remarks>
    /// The generated documentation may contain placeholder sections for information
    /// that must be completed by the provider (e.g., accuracy metrics from testing).
    /// </remarks>
    ValueTask<Either<EncinaError, TechnicalDocumentation>> GenerateDocumentationAsync(
        string systemId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the technical documentation for a registered AI system.
    /// </summary>
    /// <param name="systemId">The unique identifier of the AI system.</param>
    /// <param name="documentation">The updated technical documentation.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the system
    /// is not registered or the update fails.
    /// </returns>
    /// <remarks>
    /// Article 11(1) requires that documentation be kept up to date throughout the
    /// AI system's lifecycle. This method enables incremental updates as new information
    /// becomes available (e.g., after testing, deployment, or periodic review).
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> UpdateDocumentationAsync(
        string systemId,
        TechnicalDocumentation documentation,
        CancellationToken cancellationToken = default);
}

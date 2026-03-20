using Encina.Compliance.NIS2.Model;

using LanguageExt;

namespace Encina.Compliance.NIS2.Abstractions;

/// <summary>
/// Validates the overall NIS2 compliance posture by evaluating all 10 mandatory
/// cybersecurity risk-management measures defined in Article 21(2).
/// </summary>
/// <remarks>
/// <para>
/// The validator aggregates results from all registered <see cref="INIS2MeasureEvaluator"/>
/// implementations, one per <see cref="NIS2Measure"/>, and produces a comprehensive
/// <see cref="NIS2ComplianceResult"/> indicating which measures are satisfied and which
/// require remediation.
/// </para>
/// <para>
/// Per Art. 21(1), essential and important entities must take "appropriate and proportionate
/// technical, operational and organisational measures to manage the risks posed to the security
/// of network and information systems which those entities use for their operations or for the
/// provision of their services, and to prevent or minimise the impact of incidents on recipients
/// of their services and on other services."
/// </para>
/// </remarks>
public interface INIS2ComplianceValidator
{
    /// <summary>
    /// Validates the entity's compliance against all 10 mandatory NIS2 measures (Art. 21(2)).
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="NIS2ComplianceResult"/> containing per-measure evaluation results,
    /// the overall compliance status, and recommendations for missing measures;
    /// or an <see cref="EncinaError"/> if the validation could not be performed.
    /// </returns>
    ValueTask<Either<EncinaError, NIS2ComplianceResult>> ValidateAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the list of NIS2 measures that are not currently satisfied.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A list of <see cref="NIS2Measure"/> values representing unsatisfied measures;
    /// or an <see cref="EncinaError"/> if the evaluation could not be performed.
    /// An empty list indicates full compliance.
    /// </returns>
    ValueTask<Either<EncinaError, IReadOnlyList<NIS2Measure>>> GetMissingRequirementsAsync(
        CancellationToken cancellationToken = default);
}

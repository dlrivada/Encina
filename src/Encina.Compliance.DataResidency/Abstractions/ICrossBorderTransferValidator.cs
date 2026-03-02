using Encina.Compliance.DataResidency.Model;

using LanguageExt;

namespace Encina.Compliance.DataResidency;

/// <summary>
/// Validator for cross-border data transfers per GDPR Chapter V.
/// </summary>
/// <remarks>
/// <para>
/// The cross-border transfer validator evaluates whether a data transfer between two regions
/// is permitted under GDPR Chapter V rules. It checks adequacy decisions (Art. 45),
/// appropriate safeguards (Art. 46), and derogations (Art. 49), and produces a detailed
/// <see cref="TransferValidationResult"/> indicating whether the transfer is allowed,
/// the applicable legal basis, required safeguards, and any warnings.
/// </para>
/// <para>
/// Per GDPR Article 44, any transfer of personal data which are undergoing processing
/// or are intended for processing after transfer to a third country or to an international
/// organisation shall take place only if the conditions laid down in Chapter V are complied with.
/// </para>
/// <para>
/// The validation hierarchy follows GDPR preference order:
/// 1. Adequacy decision (Art. 45) — no additional safeguards needed
/// 2. Appropriate safeguards (Art. 46) — SCCs, BCRs, codes of conduct
/// 3. Derogations (Art. 49) — explicit consent, public interest, vital interests
/// </para>
/// <para>
/// All methods follow Railway Oriented Programming (ROP) using <c>Either&lt;EncinaError, T&gt;</c>
/// to provide explicit error handling without exceptions for business logic.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Validate a transfer from Germany to the United States
/// var result = await validator.ValidateTransferAsync(
///     RegionRegistry.Germany,
///     RegionRegistry.UnitedStates,
///     "personal-data",
///     cancellationToken);
///
/// if (result.IsRight)
/// {
///     var validation = result.Match(r => r, _ => default!);
///     if (validation.IsAllowed)
///         Console.WriteLine($"Transfer allowed via {validation.LegalBasis}");
///     else
///         Console.WriteLine($"Transfer blocked: {validation.DenialReason}");
/// }
/// </code>
/// </example>
public interface ICrossBorderTransferValidator
{
    /// <summary>
    /// Validates whether a cross-border data transfer is permitted.
    /// </summary>
    /// <param name="source">The source region from which data is being transferred.</param>
    /// <param name="destination">The destination region to which data would be transferred.</param>
    /// <param name="dataCategory">The data category being transferred (e.g., "personal-data", "healthcare-data").</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="TransferValidationResult"/> indicating whether the transfer is allowed,
    /// the legal basis, required safeguards, and any warnings or denial reasons,
    /// or an <see cref="EncinaError"/> if validation could not be performed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The validator checks the following in order:
    /// 1. If source and destination are the same region, the transfer is allowed (no cross-border).
    /// 2. If both regions are within the EEA, the transfer is allowed under GDPR's single market.
    /// 3. If the destination has an EU adequacy decision (Art. 45), the transfer is allowed.
    /// 4. If appropriate safeguards are configured (SCCs, BCRs — Art. 46), the transfer
    ///    is allowed with required safeguards noted.
    /// 5. Otherwise, the transfer is denied unless a derogation (Art. 49) applies.
    /// </para>
    /// <para>
    /// The <see cref="ResidencyPolicyDescriptor.RequireAdequacyDecision"/> and
    /// <see cref="ResidencyPolicyDescriptor.AllowedTransferBases"/> settings for the
    /// data category further constrain the validation.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, TransferValidationResult>> ValidateTransferAsync(
        Region source,
        Region destination,
        string dataCategory,
        CancellationToken cancellationToken = default);
}

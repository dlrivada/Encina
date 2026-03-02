using Encina.Compliance.DataResidency.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static LanguageExt.Prelude;

namespace Encina.Compliance.DataResidency;

/// <summary>
/// Default implementation of <see cref="ICrossBorderTransferValidator"/> that evaluates
/// cross-border data transfers per GDPR Chapter V rules.
/// </summary>
/// <remarks>
/// <para>
/// The validation hierarchy follows GDPR preference order:
/// 1. Same region → always allowed (no cross-border transfer).
/// 2. Both regions within EEA → always allowed (free movement under GDPR single market).
/// 3. Destination has EU adequacy decision (Art. 45) → allowed without additional safeguards.
/// 4. Appropriate safeguards configured (SCCs, BCRs — Art. 46) → allowed with safeguards noted.
/// 5. Otherwise → denied unless a derogation (Art. 49) applies.
/// </para>
/// <para>
/// Uses <see cref="IAdequacyDecisionProvider"/> to check adequacy status and
/// <see cref="DataResidencyOptions"/> for configuration.
/// </para>
/// </remarks>
public sealed class DefaultCrossBorderTransferValidator : ICrossBorderTransferValidator
{
    private readonly IAdequacyDecisionProvider _adequacyProvider;
    private readonly DataResidencyOptions _options;
    private readonly ILogger<DefaultCrossBorderTransferValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultCrossBorderTransferValidator"/> class.
    /// </summary>
    /// <param name="adequacyProvider">Provider for EU adequacy decision lookups.</param>
    /// <param name="options">Configuration options for the data residency module.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public DefaultCrossBorderTransferValidator(
        IAdequacyDecisionProvider adequacyProvider,
        IOptions<DataResidencyOptions> options,
        ILogger<DefaultCrossBorderTransferValidator> logger)
    {
        ArgumentNullException.ThrowIfNull(adequacyProvider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _adequacyProvider = adequacyProvider;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, TransferValidationResult>> ValidateTransferAsync(
        Region source,
        Region destination,
        string dataCategory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentException.ThrowIfNullOrWhiteSpace(dataCategory);

        // Step 1: Same region — no cross-border transfer
        if (source.Equals(destination))
        {
            _logger.LogDebug(
                "Transfer validation: same region '{RegionCode}' — no cross-border transfer",
                source.Code);

            return ValueTask.FromResult<Either<EncinaError, TransferValidationResult>>(
                Right<EncinaError, TransferValidationResult>(
                    TransferValidationResult.Allow(TransferLegalBasis.AdequacyDecision)));
        }

        // Step 2: Both within EEA — free movement under GDPR
        if (source.IsEEA && destination.IsEEA)
        {
            _logger.LogDebug(
                "Transfer validation: intra-EEA transfer from '{Source}' to '{Destination}' — allowed (free movement)",
                source.Code, destination.Code);

            return ValueTask.FromResult<Either<EncinaError, TransferValidationResult>>(
                Right<EncinaError, TransferValidationResult>(
                    TransferValidationResult.Allow(TransferLegalBasis.AdequacyDecision)));
        }

        // Step 3: Destination has adequacy decision (Art. 45)
        if (_adequacyProvider.HasAdequacy(destination))
        {
            _logger.LogDebug(
                "Transfer validation: '{Destination}' has adequacy decision — allowed (Art. 45)",
                destination.Code);

            return ValueTask.FromResult<Either<EncinaError, TransferValidationResult>>(
                Right<EncinaError, TransferValidationResult>(
                    TransferValidationResult.Allow(TransferLegalBasis.AdequacyDecision)));
        }

        // Step 4: Check for appropriate safeguards (Art. 46) — SCCs or BCRs
        // SCCs and BCRs are the most common Art. 46 mechanisms
        var sccResult = CheckAppropriateSafeguards(source, destination, dataCategory);
        if (sccResult is not null)
        {
            return ValueTask.FromResult<Either<EncinaError, TransferValidationResult>>(
                Right<EncinaError, TransferValidationResult>(sccResult));
        }

        // Step 5: No valid transfer mechanism — deny
        var denialReason = $"No adequacy decision for '{destination.Code}' and no appropriate safeguards (SCCs/BCRs) configured. "
            + "Transfer requires an adequacy decision (Art. 45), appropriate safeguards (Art. 46), or a valid derogation (Art. 49).";

        _logger.LogWarning(
            "Transfer validation: transfer from '{Source}' to '{Destination}' denied — {Reason}",
            source.Code, destination.Code, denialReason);

        return ValueTask.FromResult<Either<EncinaError, TransferValidationResult>>(
            Right<EncinaError, TransferValidationResult>(
                TransferValidationResult.Deny(denialReason)));
    }

    private TransferValidationResult? CheckAppropriateSafeguards(
        Region source,
        Region destination,
        string dataCategory)
    {
        // In the default implementation, we allow transfers with SCCs if the destination
        // has at least medium data protection level. This is a simplified check — real
        // implementations should evaluate the policy's AllowedTransferBases.
        if (destination.ProtectionLevel <= DataProtectionLevel.Medium)
        {
            _logger.LogDebug(
                "Transfer validation: '{Destination}' has {ProtectionLevel} protection — "
                + "allowed with Standard Contractual Clauses (Art. 46)",
                destination.Code, destination.ProtectionLevel);

            return TransferValidationResult.Allow(
                TransferLegalBasis.StandardContractualClauses,
                requiredSafeguards:
                [
                    "Standard Contractual Clauses (SCCs) must be in place",
                    "Transfer Impact Assessment (TIA) recommended per Schrems II"
                ],
                warnings:
                [
                    $"Destination '{destination.Code}' does not have an EU adequacy decision",
                    "Supplementary measures may be required per EDPB Recommendations 01/2020"
                ]);
        }

        return null;
    }
}

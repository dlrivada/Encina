using Encina.Compliance.CrossBorderTransfer.Abstractions;
using Encina.Compliance.CrossBorderTransfer.Diagnostics;
using Encina.Compliance.CrossBorderTransfer.Model;
using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Compliance.CrossBorderTransfer.Services;

/// <summary>
/// Default implementation of <see cref="ITransferValidator"/> that orchestrates the full
/// GDPR Chapter V cascading validation for international data transfers.
/// </summary>
/// <remarks>
/// <para>
/// The validation chain follows this priority order:
/// (1) adequacy decision check → (2) approved transfer check → (3) SCC agreement check →
/// (4) TIA requirement check → (5) block.
/// </para>
/// <para>
/// The first matching mechanism determines the <see cref="TransferValidationOutcome.Basis"/>.
/// If no mechanism applies, the transfer is blocked with <see cref="TransferBasis.Blocked"/>.
/// </para>
/// <para>
/// Per GDPR Article 44, any transfer of personal data which are undergoing processing or
/// are intended for processing after transfer to a third country or to an international
/// organisation shall take place only if the conditions laid down in Chapter V are complied with.
/// </para>
/// </remarks>
internal sealed class DefaultTransferValidator : ITransferValidator
{
    private readonly IAdequacyDecisionProvider _adequacyProvider;
    private readonly IApprovedTransferService _transferService;
    private readonly ISCCService _sccService;
    private readonly ITIAService _tiaService;
    private readonly ILogger<DefaultTransferValidator> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultTransferValidator"/>.
    /// </summary>
    /// <param name="adequacyProvider">Provider for EU adequacy decision checks.</param>
    /// <param name="transferService">Service for approved transfer lookups.</param>
    /// <param name="sccService">Service for SCC agreement validation.</param>
    /// <param name="tiaService">Service for TIA lookups.</param>
    /// <param name="logger">The logger instance.</param>
    public DefaultTransferValidator(
        IAdequacyDecisionProvider adequacyProvider,
        IApprovedTransferService transferService,
        ISCCService sccService,
        ITIAService tiaService,
        ILogger<DefaultTransferValidator> logger)
    {
        ArgumentNullException.ThrowIfNull(adequacyProvider);
        ArgumentNullException.ThrowIfNull(transferService);
        ArgumentNullException.ThrowIfNull(sccService);
        ArgumentNullException.ThrowIfNull(tiaService);
        ArgumentNullException.ThrowIfNull(logger);

        _adequacyProvider = adequacyProvider;
        _transferService = transferService;
        _sccService = sccService;
        _tiaService = tiaService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, TransferValidationOutcome>> ValidateAsync(
        TransferRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.ValidationChainStarted(request.SourceCountryCode, request.DestinationCountryCode, request.DataCategory);

        // Step 1: Check adequacy decision (Art. 45)
        var adequacyOption = CheckAdequacyDecision(request);
        if (adequacyOption.IsSome)
        {
            _logger.TransferAllowedByAdequacy(request.SourceCountryCode, request.DestinationCountryCode);
            return adequacyOption.Match(Some: o => o, None: () => default!);
        }

        // Step 2: Check existing approved transfer
        var approvedResult = await CheckApprovedTransferAsync(request, cancellationToken);
        if (approvedResult.IsLeft)
        {
            return approvedResult.Match<Either<EncinaError, TransferValidationOutcome>>(Right: _ => default!, Left: error => error);
        }

        var approvedOption = approvedResult.Match(Right: r => r, Left: _ => Option<TransferValidationOutcome>.None);
        if (approvedOption.IsSome)
        {
            _logger.TransferAllowedBySCC(request.SourceCountryCode, request.DestinationCountryCode);
            return approvedOption.Match(Some: o => o, None: () => default!);
        }

        // Step 3: Check SCC agreement (Art. 46(2)(c))
        var sccResult = await CheckSCCAgreementAsync(request, cancellationToken);
        if (sccResult.IsLeft)
        {
            return sccResult.Match<Either<EncinaError, TransferValidationOutcome>>(Right: _ => default!, Left: error => error);
        }

        var sccOption = sccResult.Match(Right: r => r, Left: _ => Option<TransferValidationOutcome>.None);
        if (sccOption.IsSome)
        {
            _logger.TransferAllowedBySCC(request.SourceCountryCode, request.DestinationCountryCode);
            return sccOption.Match(Some: o => o, None: () => default!);
        }

        // Step 4: Check TIA (Schrems II)
        var tiaResult = await CheckTIAAsync(request, cancellationToken);
        if (tiaResult.IsLeft)
        {
            return tiaResult.Match<Either<EncinaError, TransferValidationOutcome>>(Right: _ => default!, Left: error => error);
        }

        var tiaOption = tiaResult.Match(Right: r => r, Left: _ => Option<TransferValidationOutcome>.None);
        if (tiaOption.IsSome)
        {
            _logger.TransferRequiresTIA(request.SourceCountryCode, request.DestinationCountryCode, request.DataCategory);
            return tiaOption.Match(Some: o => o, None: () => default!);
        }

        // Step 5: Block — no valid mechanism found
        _logger.ValidationChainBlocked(request.SourceCountryCode, request.DestinationCountryCode);

        return TransferValidationOutcome.Block(
            $"No valid transfer mechanism found for route {request.SourceCountryCode} → {request.DestinationCountryCode} " +
            $"with data category '{request.DataCategory}'. An adequacy decision, SCC agreement, or TIA is required.");
    }

    private Option<TransferValidationOutcome> CheckAdequacyDecision(TransferRequest request)
    {
        // Create a Region from the destination country code to check adequacy
        var destinationRegion = Region.Create(request.DestinationCountryCode, request.DestinationCountryCode);

        if (_adequacyProvider.HasAdequacy(destinationRegion))
        {
            return TransferValidationOutcome.Allow(TransferBasis.AdequacyDecision);
        }

        return Option<TransferValidationOutcome>.None;
    }

    private async ValueTask<Either<EncinaError, Option<TransferValidationOutcome>>> CheckApprovedTransferAsync(
        TransferRequest request,
        CancellationToken cancellationToken)
    {
        var isApprovedResult = await _transferService.IsTransferApprovedAsync(
            request.SourceCountryCode, request.DestinationCountryCode, request.DataCategory, cancellationToken);

        return isApprovedResult.Match<Either<EncinaError, Option<TransferValidationOutcome>>>(
            Right: isApproved =>
            {
                if (isApproved)
                {
                    return Option<TransferValidationOutcome>.Some(TransferValidationOutcome.Allow(TransferBasis.SCCs));
                }

                return Option<TransferValidationOutcome>.None;
            },
            Left: error => error);
    }

    private async ValueTask<Either<EncinaError, Option<TransferValidationOutcome>>> CheckSCCAgreementAsync(
        TransferRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ProcessorId))
        {
            return Option<TransferValidationOutcome>.None;
        }

        // Default to ControllerToProcessor module if not further specified
        var sccResult = await _sccService.ValidateAgreementAsync(
            request.ProcessorId, SCCModule.ControllerToProcessor, cancellationToken);

        return sccResult.Match<Either<EncinaError, Option<TransferValidationOutcome>>>(
            Right: validation =>
            {
                if (validation.IsValid)
                {
                    var warnings = new List<string>();
                    if (validation.MissingMeasures.Count > 0)
                    {
                        warnings.Add($"SCC agreement has {validation.MissingMeasures.Count} missing supplementary measure(s).");
                    }

                    warnings.AddRange(validation.Issues);

                    return Option<TransferValidationOutcome>.Some(TransferValidationOutcome.Allow(
                        TransferBasis.SCCs,
                        supplementaryMeasuresRequired: validation.MissingMeasures,
                        tiaRequired: true,
                        sccModuleRequired: validation.Module,
                        warnings: warnings.Count > 0 ? warnings : null));
                }

                return Option<TransferValidationOutcome>.None;
            },
            Left: error => error);
    }

    private async ValueTask<Either<EncinaError, Option<TransferValidationOutcome>>> CheckTIAAsync(
        TransferRequest request,
        CancellationToken cancellationToken)
    {
        var tiaResult = await _tiaService.GetTIAByRouteAsync(
            request.SourceCountryCode, request.DestinationCountryCode, request.DataCategory, cancellationToken);

        return tiaResult.Match<Either<EncinaError, Option<TransferValidationOutcome>>>(
            Right: tia =>
            {
                if (tia.Status == TIAStatus.Completed)
                {
                    var warnings = new List<string>();
                    var pendingMeasures = tia.RequiredSupplementaryMeasures
                        .Where(m => !m.IsImplemented)
                        .Select(m => m.Description)
                        .ToList();

                    if (pendingMeasures.Count > 0)
                    {
                        warnings.Add($"TIA has {pendingMeasures.Count} supplementary measure(s) pending implementation.");
                    }

                    return Option<TransferValidationOutcome>.Some(TransferValidationOutcome.Allow(
                        TransferBasis.SCCs,
                        supplementaryMeasuresRequired: pendingMeasures,
                        tiaRequired: false,
                        warnings: warnings.Count > 0 ? warnings : null));
                }

                return Option<TransferValidationOutcome>.None;
            },
            Left: _ =>
            {
                // TIA not found is not an infrastructure error — it means no TIA exists for this route
                return Option<TransferValidationOutcome>.None;
            });
    }
}

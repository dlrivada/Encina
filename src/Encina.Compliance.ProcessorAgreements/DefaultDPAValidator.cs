using Encina.Compliance.ProcessorAgreements.Diagnostics;
using Encina.Compliance.ProcessorAgreements.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;

namespace Encina.Compliance.ProcessorAgreements;

/// <summary>
/// Default implementation of <see cref="IDPAValidator"/> that queries both the
/// <see cref="IProcessorRegistry"/> and <see cref="IDPAStore"/> to validate
/// Data Processing Agreement compliance.
/// </summary>
/// <remarks>
/// <para>
/// The validator performs the following checks:
/// </para>
/// <list type="number">
/// <item><description>Processor exists in <see cref="IProcessorRegistry"/>.</description></item>
/// <item><description>An active DPA exists in <see cref="IDPAStore"/>.</description></item>
/// <item><description>The DPA has not expired (<see cref="DataProcessingAgreement.IsActive"/>).</description></item>
/// <item><description>All mandatory terms are met (<see cref="DPAMandatoryTerms.IsFullyCompliant"/>).</description></item>
/// <item><description>SCCs are present when required for cross-border transfers.</description></item>
/// </list>
/// <para>
/// <see cref="HasValidDPAAsync"/> provides a lightweight boolean check optimized for the
/// <c>ProcessorValidationPipelineBehavior</c> hot path. <see cref="ValidateAsync"/> provides
/// detailed results for compliance dashboards and regulatory audits.
/// </para>
/// </remarks>
public sealed class DefaultDPAValidator : IDPAValidator
{
    private readonly IProcessorRegistry _registry;
    private readonly IDPAStore _dpaStore;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DefaultDPAValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultDPAValidator"/> class.
    /// </summary>
    /// <param name="registry">The processor registry for identity lookups.</param>
    /// <param name="dpaStore">The DPA store for contractual state lookups.</param>
    /// <param name="timeProvider">The time provider for deterministic time-dependent checks.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public DefaultDPAValidator(
        IProcessorRegistry registry,
        IDPAStore dpaStore,
        TimeProvider timeProvider,
        ILogger<DefaultDPAValidator> logger)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(dpaStore);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _registry = registry;
        _dpaStore = dpaStore;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, DPAValidationResult>> ValidateAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processorId);

        _logger.ValidationStarted(processorId);

        var nowUtc = _timeProvider.GetUtcNow();

        // 1. Verify processor exists.
        var processorResult = await _registry.GetProcessorAsync(processorId, cancellationToken);

        Either<EncinaError, DPAValidationResult> outcome;

        try
        {
            outcome = await processorResult.MatchAsync(
                RightAsync: async processorOption =>
                {
                    if (processorOption.IsNone)
                    {
                        return BuildNotFoundResult(processorId, nowUtc);
                    }

                    // 2. Get active DPA.
                    var dpaResult = await _dpaStore.GetActiveByProcessorIdAsync(processorId, cancellationToken);

                    return dpaResult.Match(
                        Right: dpaOption => dpaOption.Match(
                            Some: dpa => BuildValidationResult(processorId, dpa, nowUtc),
                            None: () => BuildMissingDPAResult(processorId, nowUtc)),
                        Left: error => error);
                },
                Left: error => error);
        }
        catch (Exception ex)
        {
            _logger.ValidationError(processorId, ex);
            throw;
        }

        outcome.Match(
            Right: result =>
            {
                if (result.IsValid)
                {
                    _logger.ValidationPassed(processorId);
                }
                else
                {
                    var reason = result.Warnings.Count > 0
                        ? string.Join("; ", result.Warnings)
                        : "validation_failed";
                    _logger.ValidationFailed(processorId, reason);
                }
            },
            Left: _ => { });

        return outcome;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, bool>> HasValidDPAAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processorId);

        var nowUtc = _timeProvider.GetUtcNow();

        // 1. Verify processor exists.
        var processorResult = await _registry.GetProcessorAsync(processorId, cancellationToken);

        return await processorResult.MatchAsync(
            RightAsync: async processorOption =>
            {
                if (processorOption.IsNone)
                {
                    return Either<EncinaError, bool>.Right(false);
                }

                // 2. Get active DPA.
                var dpaResult = await _dpaStore.GetActiveByProcessorIdAsync(processorId, cancellationToken);

                return dpaResult.Match(
                    Right: dpaOption => dpaOption.Match(
                        Some: dpa => Either<EncinaError, bool>.Right(
                            dpa.IsActive(nowUtc) && dpa.MandatoryTerms.IsFullyCompliant),
                        None: () => Either<EncinaError, bool>.Right(false)),
                    Left: error => Either<EncinaError, bool>.Left(error));
            },
            Left: error => Either<EncinaError, bool>.Left(error));
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DPAValidationResult>>> ValidateAllAsync(
        CancellationToken cancellationToken = default)
    {
        var processorsResult = await _registry.GetAllProcessorsAsync(cancellationToken);

        return await processorsResult.MatchAsync(
            RightAsync: async processors =>
            {
                var results = new List<DPAValidationResult>(processors.Count);

                foreach (var processor in processors)
                {
                    var validationResult = await ValidateAsync(processor.Id, cancellationToken);

                    validationResult.Match(
                        Right: result => results.Add(result),
                        Left: _ => { /* Skip processors that fail to validate due to store errors. */ });
                }

                return Either<EncinaError, IReadOnlyList<DPAValidationResult>>.Right(results);
            },
            Left: error => Either<EncinaError, IReadOnlyList<DPAValidationResult>>.Left(error));
    }

    // ── Private Helpers ──────────────────────────────────────────────────

    private static Either<EncinaError, DPAValidationResult> BuildNotFoundResult(
        string processorId,
        DateTimeOffset nowUtc) =>
        new DPAValidationResult
        {
            ProcessorId = processorId,
            IsValid = false,
            Status = null,
            DPAId = null,
            DaysUntilExpiration = null,
            MissingTerms = [],
            Warnings = ["Processor not found in registry."],
            ValidatedAtUtc = nowUtc
        };

    private static Either<EncinaError, DPAValidationResult> BuildMissingDPAResult(
        string processorId,
        DateTimeOffset nowUtc) =>
        new DPAValidationResult
        {
            ProcessorId = processorId,
            IsValid = false,
            Status = null,
            DPAId = null,
            DaysUntilExpiration = null,
            MissingTerms = [],
            Warnings = ["No active Data Processing Agreement exists for this processor."],
            ValidatedAtUtc = nowUtc
        };

    private static Either<EncinaError, DPAValidationResult> BuildValidationResult(
        string processorId,
        DataProcessingAgreement dpa,
        DateTimeOffset nowUtc)
    {
        var isActive = dpa.IsActive(nowUtc);
        var isFullyCompliant = dpa.MandatoryTerms.IsFullyCompliant;
        var missingTerms = dpa.MandatoryTerms.MissingTerms;

        int? daysUntilExpiration = dpa.ExpiresAtUtc.HasValue
            ? (int)(dpa.ExpiresAtUtc.Value - nowUtc).TotalDays
            : null;

        var warnings = new List<string>();

        if (!isActive)
        {
            warnings.Add($"DPA '{dpa.Id}' is not active (Status={dpa.Status}).");
        }

        if (!isFullyCompliant)
        {
            warnings.Add($"DPA '{dpa.Id}' is missing {missingTerms.Count} mandatory term(s) per Article 28(3).");
        }

        if (daysUntilExpiration is not null && daysUntilExpiration <= 30 && daysUntilExpiration > 0)
        {
            warnings.Add($"DPA '{dpa.Id}' expires in {daysUntilExpiration} day(s).");
        }

        if (!dpa.HasSCCs)
        {
            warnings.Add($"DPA '{dpa.Id}' does not include Standard Contractual Clauses.");
        }

        return new DPAValidationResult
        {
            ProcessorId = processorId,
            IsValid = isActive && isFullyCompliant,
            Status = dpa.Status,
            DPAId = dpa.Id,
            DaysUntilExpiration = daysUntilExpiration,
            MissingTerms = missingTerms,
            Warnings = warnings,
            ValidatedAtUtc = nowUtc
        };
    }
}

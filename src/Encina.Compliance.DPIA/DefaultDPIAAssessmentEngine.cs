using System.Diagnostics;

using Encina.Compliance.DPIA.Diagnostics;
using Encina.Compliance.DPIA.Model;
using Encina.Compliance.GDPR;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.DPIA;

/// <summary>
/// Default implementation of <see cref="IDPIAAssessmentEngine"/> that orchestrates risk criteria
/// evaluation, template-based mitigation generation, and DPO consultation lifecycle.
/// </summary>
/// <remarks>
/// <para>
/// The engine follows a composite evaluator pattern: it iterates all registered
/// <see cref="IRiskCriterion"/> instances against a <see cref="DPIAContext"/>, collects
/// identified risks, and aggregates them into a <see cref="DPIAResult"/>.
/// </para>
/// <para>
/// Risk aggregation uses a conservative approach: the <see cref="DPIAResult.OverallRisk"/>
/// is the maximum of all individual <see cref="RiskItem.Level"/> values. If no criteria trigger,
/// the overall risk defaults to <see cref="RiskLevel.Low"/>.
/// </para>
/// <para>
/// Prior consultation with the supervisory authority (Article 36) is required when:
/// <see cref="DPIAResult.OverallRisk"/> is <see cref="RiskLevel.VeryHigh"/> AND not all
/// proposed mitigations are implemented. This ensures that fully mitigated high-risk
/// processing does not unnecessarily require regulatory consultation.
/// </para>
/// <para>
/// Individual criterion failures are isolated: if a criterion throws an exception, the engine
/// logs a warning and continues evaluating the remaining criteria, ensuring robustness.
/// </para>
/// </remarks>
public sealed class DefaultDPIAAssessmentEngine : IDPIAAssessmentEngine
{
    private readonly List<IRiskCriterion> _criteria;
    private readonly IDPIAStore _store;
    private readonly IDPIAAuditStore _auditStore;
    private readonly IDPIATemplateProvider _templateProvider;
    private readonly DPIAOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly IDataProtectionOfficer? _dpo;
    private readonly ILogger<DefaultDPIAAssessmentEngine> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultDPIAAssessmentEngine"/> class.
    /// </summary>
    /// <param name="criteria">The risk criteria to evaluate during assessment.</param>
    /// <param name="store">The DPIA assessment persistence store.</param>
    /// <param name="auditStore">The audit trail store for recording DPIA operations.</param>
    /// <param name="templateProvider">The template provider for generating assessment structures.</param>
    /// <param name="options">Configuration options for the DPIA module.</param>
    /// <param name="timeProvider">The time provider for deterministic timestamp generation.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="dpo">
    /// Optional DPO from the GDPR module. When <see cref="DPIAOptions.DPOEmail"/> is not configured,
    /// DPO contact information is resolved from this service if registered. This is a soft dependency —
    /// the engine works identically if no <see cref="IDataProtectionOfficer"/> is registered.
    /// </param>
    public DefaultDPIAAssessmentEngine(
        IEnumerable<IRiskCriterion> criteria,
        IDPIAStore store,
        IDPIAAuditStore auditStore,
        IDPIATemplateProvider templateProvider,
        IOptions<DPIAOptions> options,
        TimeProvider timeProvider,
        ILogger<DefaultDPIAAssessmentEngine> logger,
        IDataProtectionOfficer? dpo = null)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(auditStore);
        ArgumentNullException.ThrowIfNull(templateProvider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _criteria = criteria.ToList();
        _store = store;
        _auditStore = auditStore;
        _templateProvider = templateProvider;
        _options = options.Value;
        _timeProvider = timeProvider;
        _dpo = dpo;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, DPIAResult>> AssessAsync(
        DPIAContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var requestTypeName = context.RequestType.FullName ?? context.RequestType.Name;
        using var activity = DPIADiagnostics.StartAssessment(requestTypeName);
        var startedAt = Stopwatch.GetTimestamp();

        _logger.AssessmentStarted(requestTypeName, _criteria.Count);

        try
        {
            // 1. Resolve template if not already provided on the context.
            var template = context.Template;
            if (template is null && context.ProcessingType is not null)
            {
                var templateResult = await _templateProvider.GetTemplateAsync(
                    context.ProcessingType, cancellationToken);

                if (templateResult.IsRight)
                {
                    template = (DPIATemplate)templateResult;
                    _logger.TemplateResolved(context.ProcessingType!, template.Name);
                }
                // Template not found is non-fatal — proceed without template.
            }

            // 2. Evaluate all criteria with fault isolation.
            var identifiedRisks = await EvaluateCriteriaAsync(context, cancellationToken);

            // 3. Compute overall risk (conservative: maximum of all identified risk levels).
            var overallRisk = identifiedRisks.Count > 0
                ? identifiedRisks.Max(r => r.Level)
                : RiskLevel.Low;

            // 4. Build proposed mitigations from template and individual criteria suggestions.
            var mitigations = BuildProposedMitigations(identifiedRisks, template);

            // 5. Determine prior consultation requirement (Art. 36).
            var requiresPriorConsultation = overallRisk >= RiskLevel.VeryHigh
                && !mitigations.All(m => m.IsImplemented);

            if (requiresPriorConsultation)
            {
                _logger.PriorConsultationRequired(requestTypeName, overallRisk.ToString());
            }

            // 6. Build the result.
            var nowUtc = _timeProvider.GetUtcNow();
            var result = new DPIAResult
            {
                OverallRisk = overallRisk,
                IdentifiedRisks = identifiedRisks,
                ProposedMitigations = mitigations,
                RequiresPriorConsultation = requiresPriorConsultation,
                AssessedAtUtc = nowUtc,
            };

            _logger.AssessmentCompleted(
                requestTypeName, overallRisk.ToString(),
                identifiedRisks.Count, mitigations.Count, requiresPriorConsultation);

            // 7. Record metrics and activity.
            var elapsedMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
            var tags = new TagList
            {
                { DPIADiagnostics.TagRequestType, requestTypeName },
                { DPIADiagnostics.TagRiskLevel, overallRisk.ToString() },
            };

            DPIADiagnostics.AssessmentTotal.Add(1, tags);
            DPIADiagnostics.AssessmentDuration.Record(elapsedMs, tags);
            DPIADiagnostics.RecordAssessmentCompleted(activity, overallRisk.ToString());

            // 8. Record audit trail for the assessment completion.
            if (_options.TrackAuditTrail)
            {
                await RecordAssessmentAuditAsync(
                    requestTypeName,
                    result,
                    nowUtc,
                    cancellationToken);
            }

            return result;
        }
        catch (Exception ex)
        {
            var elapsedMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
            DPIADiagnostics.AssessmentDuration.Record(elapsedMs);
            DPIADiagnostics.RecordAssessmentFailed(activity, ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, bool>> RequiresDPIAAsync(
        Type requestType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestType);

        var attribute = Attribute.GetCustomAttribute(requestType, typeof(RequiresDPIAAttribute));

        return ValueTask.FromResult<Either<EncinaError, bool>>(attribute is not null);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, DPOConsultation>> RequestDPOConsultationAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        using var activity = DPIADiagnostics.StartDPOConsultation(assessmentId);
        _logger.DPOConsultationStarted(assessmentId);
        DPIADiagnostics.DPOConsultationTotal.Add(1);

        // 1. Validate DPO configuration — try options first, then GDPR module fallback.
        var (dpoName, dpoEmail) = ResolveDPOContact();
        if (string.IsNullOrWhiteSpace(dpoEmail))
        {
            _logger.DPOConsultationNoDPO(assessmentId);
            return DPIAErrors.DPOConsultationRequired(assessmentId);
        }

        // 2. Load the assessment from the store.
        var assessmentResult = await _store.GetAssessmentByIdAsync(assessmentId, cancellationToken);

        return await assessmentResult.Match(
            Right: assessmentOption => CreateConsultationAsync(
                assessmentOption.IsSome ? (DPIAAssessment?)assessmentOption.Case : null,
                assessmentId,
                cancellationToken),
            Left: error => ValueTask.FromResult<Either<EncinaError, DPOConsultation>>(error));
    }

    // ── Private Helpers ──────────────────────────────────────────────────

    private async ValueTask<List<RiskItem>> EvaluateCriteriaAsync(
        DPIAContext context,
        CancellationToken cancellationToken)
    {
        var identifiedRisks = new List<RiskItem>();

        foreach (var criterion in _criteria)
        {
            try
            {
                var riskItem = await criterion.EvaluateAsync(context, cancellationToken);
                if (riskItem is not null)
                {
                    identifiedRisks.Add(riskItem);
                    _logger.CriterionTriggered(criterion.Name, riskItem.Category, riskItem.Level.ToString());
                }
            }
            catch (Exception ex)
            {
                // Fault isolation: individual criterion failures do not abort the assessment.
                _logger.CriterionFailed(criterion.Name, ex);
            }
        }

        return identifiedRisks;
    }

    private static List<Mitigation> BuildProposedMitigations(
        List<RiskItem> identifiedRisks,
        DPIATemplate? template)
    {
        var mitigations = new List<Mitigation>();

        // Mitigations from the template's suggested mitigations.
        if (template is not null)
        {
            foreach (var suggested in template.SuggestedMitigations)
            {
                mitigations.Add(new Mitigation(
                    Description: suggested,
                    Category: "Template",
                    IsImplemented: false,
                    ImplementedAtUtc: null));
            }
        }

        // Mitigations from individual risk criterion suggestions.
        foreach (var risk in identifiedRisks)
        {
            if (!string.IsNullOrWhiteSpace(risk.MitigationSuggestion))
            {
                mitigations.Add(new Mitigation(
                    Description: risk.MitigationSuggestion,
                    Category: risk.Category,
                    IsImplemented: false,
                    ImplementedAtUtc: null));
            }
        }

        return mitigations;
    }

    private async ValueTask<Either<EncinaError, DPOConsultation>> CreateConsultationAsync(
        DPIAAssessment? assessment,
        Guid assessmentId,
        CancellationToken cancellationToken)
    {
        if (assessment is null)
        {
            return DPIAErrors.DPOConsultationRequired(assessmentId);
        }

        var nowUtc = _timeProvider.GetUtcNow();
        var (dpoName, dpoEmail) = ResolveDPOContact();

        var consultation = new DPOConsultation
        {
            Id = Guid.NewGuid(),
            DPOName = dpoName ?? "Data Protection Officer",
            DPOEmail = dpoEmail!,
            RequestedAtUtc = nowUtc,
            Decision = DPOConsultationDecision.Pending,
        };

        // DPIAAssessment is immutable (sealed record with init properties);
        // create an updated copy with the consultation attached.
        var updatedAssessment = assessment with { DPOConsultation = consultation };

        var saveResult = await _store.SaveAssessmentAsync(updatedAssessment, cancellationToken);

        return await saveResult.Match(
            Right: _ => RecordConsultationAuditAsync(consultation, assessmentId, nowUtc, cancellationToken),
            Left: error => ValueTask.FromResult<Either<EncinaError, DPOConsultation>>(error));
    }

    private async ValueTask<Either<EncinaError, DPOConsultation>> RecordConsultationAuditAsync(
        DPOConsultation consultation,
        Guid assessmentId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        if (_options.TrackAuditTrail)
        {
            var auditEntry = new DPIAAuditEntry
            {
                Id = Guid.NewGuid(),
                AssessmentId = assessmentId,
                Action = "DPOConsultationRequested",
                PerformedBy = "System",
                OccurredAtUtc = nowUtc,
                Details = $"DPO consultation requested. DPO: {consultation.DPOEmail}.",
            };

            try
            {
                await _auditStore.RecordAuditEntryAsync(auditEntry, cancellationToken);
                _logger.AuditEntryRecorded(assessmentId, "DPOConsultationRequested", "System");
            }
            catch (Exception ex)
            {
                _logger.AuditEntryFailed(assessmentId, "DPOConsultationRequested", ex);
            }
        }

        _logger.DPOConsultationCreated(assessmentId, consultation.Id, consultation.DPOEmail);

        return consultation;
    }

    /// <summary>
    /// Records an audit entry when a risk assessment completes.
    /// </summary>
    private async ValueTask RecordAssessmentAuditAsync(
        string requestTypeName,
        DPIAResult result,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        var auditEntry = new DPIAAuditEntry
        {
            Id = Guid.NewGuid(),
            AssessmentId = Guid.Empty, // Assessment ID is not yet known at this point
            Action = "AssessmentCompleted",
            PerformedBy = "System",
            OccurredAtUtc = nowUtc,
            Details = $"Risk assessment completed for '{requestTypeName}'. "
                + $"OverallRisk={result.OverallRisk}, "
                + $"Risks={result.IdentifiedRisks.Count}, "
                + $"Mitigations={result.ProposedMitigations.Count}, "
                + $"RequiresPriorConsultation={result.RequiresPriorConsultation}.",
        };

        try
        {
            await _auditStore.RecordAuditEntryAsync(auditEntry, cancellationToken);
            _logger.AuditEntryRecorded(Guid.Empty, "AssessmentCompleted", "System");
        }
        catch (Exception ex)
        {
            _logger.AuditEntryFailed(Guid.Empty, "AssessmentCompleted", ex);
        }
    }

    /// <summary>
    /// Resolves DPO contact information from options or the GDPR module as a fallback.
    /// </summary>
    /// <returns>A tuple of (name, email) for the DPO.</returns>
    private (string? Name, string? Email) ResolveDPOContact()
    {
        // 1. Options take priority — explicit configuration always wins.
        if (!string.IsNullOrWhiteSpace(_options.DPOEmail))
        {
            _logger.DPOContactResolved("DPIAOptions", _options.DPOName, _options.DPOEmail);
            return (_options.DPOName, _options.DPOEmail);
        }

        // 2. Fallback to GDPR module's IDataProtectionOfficer if registered.
        if (_dpo is not null)
        {
            _logger.DPOContactResolved("IDataProtectionOfficer", _dpo.Name, _dpo.Email);
            return (_dpo.Name, _dpo.Email);
        }

        // 3. Neither configured — DPO consultation will fail with a descriptive error.
        return (null, null);
    }
}

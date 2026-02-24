using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;

using Encina.Compliance.GDPR.Diagnostics;


using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static LanguageExt.Prelude;

namespace Encina.Compliance.GDPR;

/// <summary>
/// Pipeline behavior that validates lawful basis declarations for GDPR Article 6(1) compliance.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// <para>
/// This behavior complements <see cref="GDPRCompliancePipelineBehavior{TRequest, TResponse}"/>
/// by performing targeted Article 6 validation:
/// </para>
/// <list type="number">
/// <item><description>Detects lawful basis from <see cref="LawfulBasisAttribute"/> or <see cref="ProcessingActivityAttribute"/>.</description></item>
/// <item><description>Detects attribute conflicts when both attributes declare different bases.</description></item>
/// <item><description>For <see cref="LawfulBasis.Consent"/>: validates active consent via <see cref="IConsentStatusProvider"/>.</description></item>
/// <item><description>For <see cref="LawfulBasis.LegitimateInterests"/>: validates LIA approval via <see cref="ILegitimateInterestAssessment"/>.</description></item>
/// <item><description>For other bases: verifies declaration exists and proceeds.</description></item>
/// </list>
/// <para>
/// The enforcement mode is controlled by <see cref="LawfulBasisOptions.EnforcementMode"/>:
/// <see cref="LawfulBasisEnforcementMode.Block"/> returns an error for violations,
/// <see cref="LawfulBasisEnforcementMode.Warn"/> logs a warning but allows processing,
/// <see cref="LawfulBasisEnforcementMode.Disabled"/> skips validation entirely.
/// </para>
/// <para>
/// <b>Attribute resolution:</b> Each closed generic type resolves its attribute info exactly once
/// via a <c>static readonly</c> field. <see cref="LawfulBasisAttribute"/> takes priority over
/// <see cref="ProcessingActivityAttribute"/> when both are present. Conflicts are logged at
/// Warning level with EventId 8207.
/// </para>
/// <para>
/// <b>Registry fallback:</b> When a request is marked with <see cref="ProcessesPersonalDataAttribute"/>
/// but has no lawful basis attribute, the behavior queries <see cref="ILawfulBasisRegistry"/> for
/// programmatically registered bases before applying enforcement.
/// </para>
/// <para>
/// <b>Observability:</b> Emits OpenTelemetry traces via <c>Encina.Compliance.GDPR</c> ActivitySource,
/// metrics via <c>Encina.Compliance.GDPR</c> Meter, and structured log messages with
/// EventIds in the 8200-8210 range.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Consent-based processing — pipeline validates consent status
/// [LawfulBasis(LawfulBasis.Consent, Purpose = "Marketing newsletters")]
/// public record SendNewsletterCommand(string CustomerId) : ICommand;
///
/// // Legitimate interests — pipeline validates LIA approval
/// [LawfulBasis(LawfulBasis.LegitimateInterests,
///     Purpose = "Fraud detection",
///     LIAReference = "LIA-2024-FRAUD-001")]
/// public record AnalyzeTransactionCommand(TransactionData Data) : ICommand&lt;FraudScore&gt;;
///
/// // Contract basis — proceeds with declared basis
/// [LawfulBasis(LawfulBasis.Contract,
///     Purpose = "Order fulfillment",
///     ContractReference = "Terms of Service v2.1")]
/// public record CreateOrderCommand(OrderData Data) : ICommand&lt;OrderId&gt;;
/// </code>
/// </example>
public sealed class LawfulBasisValidationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Static per-generic-type attribute info. Each closed generic type (e.g.,
    /// <c>LawfulBasisValidationPipelineBehavior&lt;CreateOrderCommand, OrderId&gt;</c>)
    /// resolves its own attribute info exactly once via the CLR's static field guarantee.
    /// </summary>
    private static readonly LawfulBasisAttributeInfo? CachedAttributeInfo = ResolveAttributeInfo();

    private readonly ILawfulBasisRegistry _registry;
    private readonly ILegitimateInterestAssessment _liaAssessment;
    private readonly IConsentStatusProvider? _consentProvider;
    private readonly ILawfulBasisSubjectIdExtractor _subjectIdExtractor;
    private readonly LawfulBasisOptions _options;
    private readonly ILogger<LawfulBasisValidationPipelineBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LawfulBasisValidationPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="registry">The lawful basis registry for programmatic registration fallback.</param>
    /// <param name="liaAssessment">The LIA validation service for legitimate interests basis.</param>
    /// <param name="subjectIdExtractor">The subject ID extractor for consent-based validation.</param>
    /// <param name="options">Lawful basis configuration options.</param>
    /// <param name="logger">Logger for structured lawful basis validation logging.</param>
    /// <param name="consentProvider">
    /// Optional consent status provider. When <c>null</c> and a request declares
    /// <see cref="LawfulBasis.Consent"/>, enforcement mode rules apply.
    /// </param>
    public LawfulBasisValidationPipelineBehavior(
        ILawfulBasisRegistry registry,
        ILegitimateInterestAssessment liaAssessment,
        ILawfulBasisSubjectIdExtractor subjectIdExtractor,
        IOptions<LawfulBasisOptions> options,
        ILogger<LawfulBasisValidationPipelineBehavior<TRequest, TResponse>> logger,
        IConsentStatusProvider? consentProvider = null)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(liaAssessment);
        ArgumentNullException.ThrowIfNull(subjectIdExtractor);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _registry = registry;
        _liaAssessment = liaAssessment;
        _subjectIdExtractor = subjectIdExtractor;
        _options = options.Value;
        _logger = logger;
        _consentProvider = consentProvider;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Step 1: Disabled mode — no-op, no logging, no metrics
        if (_options.EnforcementMode == LawfulBasisEnforcementMode.Disabled)
        {
            return await nextStep().ConfigureAwait(false);
        }

        var requestType = typeof(TRequest);
        var attrInfo = CachedAttributeInfo;

        // Step 2: No GDPR attributes at all — skip entirely
        if (attrInfo is null)
        {
            _logger.ValidationSkipped(requestType);
            return await nextStep().ConfigureAwait(false);
        }

        // Step 3: Start tracing
        using var activity = LawfulBasisDiagnostics.StartValidation(requestType);
        _logger.ValidationStarted(requestType);

        // Step 4: No lawful basis declared (only ProcessesPersonalData marker)
        if (attrInfo.Basis is null)
        {
            return await HandleNoBasisDeclaredAsync(
                requestType, activity, nextStep, cancellationToken)
                .ConfigureAwait(false);
        }

        var basis = attrInfo.Basis.Value;

        // Step 5: Log attribute conflict if detected
        if (attrInfo.HasAttributeConflict)
        {
            _logger.AttributeConflictDetected(requestType);
        }

        // Step 6: Set OpenTelemetry tags
        LawfulBasisDiagnostics.SetBasis(activity, basis);

        // Step 7: Basis-specific validation
        var validationResult = basis switch
        {
            LawfulBasis.Consent when _options.ValidateConsentForConsentBasis
                => await ValidateConsentAsync(
                        request, context, attrInfo, requestType, cancellationToken)
                    .ConfigureAwait(false),

            LawfulBasis.LegitimateInterests when _options.ValidateLIAForLegitimateInterests
                => await ValidateLIAAsync(attrInfo, requestType, cancellationToken)
                    .ConfigureAwait(false),

            _ => Right<EncinaError, bool>(true)
        };

        // Step 8: Handle validation failure
        if (validationResult.IsLeft)
        {
            RecordFailed(activity, requestType, basis, "validation_failed");
            return Left<EncinaError, TResponse>((EncinaError)validationResult);
        }

        // Step 9: Record success and proceed
        RecordPassed(activity, requestType, basis);
        _logger.ValidationPassed(requestType, basis);
        return await nextStep().ConfigureAwait(false);
    }

    // ================================================================
    // No basis declared — registry fallback
    // ================================================================

    private async ValueTask<Either<EncinaError, TResponse>> HandleNoBasisDeclaredAsync(
        Type requestType,
        Activity? activity,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        if (!_options.RequireDeclaredBasis)
        {
            RecordPassed(activity, requestType, null);
            return await nextStep().ConfigureAwait(false);
        }

        // Fallback: check the registry for programmatically registered bases
        var registryLookup = await _registry
            .GetByRequestTypeAsync(requestType, cancellationToken)
            .ConfigureAwait(false);

        var registryBasis = registryLookup.MatchUnsafe(
            Right: opt => opt.MatchUnsafe(
                Some: reg => (LawfulBasis?)reg.Basis,
                None: () => (LawfulBasis?)null),
            Left: _ => (LawfulBasis?)null);

        if (registryBasis is not null)
        {
            // Registry has a basis — proceed
            LawfulBasisDiagnostics.SetBasis(activity, registryBasis.Value);
            RecordPassed(activity, requestType, registryBasis.Value);
            _logger.ValidationPassed(requestType, registryBasis.Value);
            return await nextStep().ConfigureAwait(false);
        }

        // No basis from attributes or registry — enforce
        var error = GDPRErrors.LawfulBasisNotDeclared(requestType);
        _logger.BasisNotDeclared(requestType);

        var enforcementResult = ApplyEnforcement(error, requestType);
        if (enforcementResult.IsLeft)
        {
            RecordFailed(activity, requestType, null, GDPRErrors.LawfulBasisNotDeclaredCode);
            return Left<EncinaError, TResponse>((EncinaError)enforcementResult);
        }

        // Warn mode — continue despite violation
        RecordPassed(activity, requestType, null);
        return await nextStep().ConfigureAwait(false);
    }

    // ================================================================
    // Consent validation
    // ================================================================

    private async ValueTask<Either<EncinaError, bool>> ValidateConsentAsync(
        TRequest request,
        IRequestContext context,
        LawfulBasisAttributeInfo attrInfo,
        Type requestType,
        CancellationToken cancellationToken)
    {
        // Verify consent provider is registered
        if (_consentProvider is null)
        {
            var error = GDPRErrors.ConsentProviderNotRegistered(requestType);
            _logger.ProviderNotRegistered(requestType);
            return ApplyEnforcement(error, requestType);
        }

        // Extract subject ID
        var subjectId = _subjectIdExtractor.ExtractSubjectId(request, context);
        if (string.IsNullOrWhiteSpace(subjectId))
        {
            var error = GDPRErrors.ConsentNotFound(requestType);
            _logger.ConsentCheckFailed(requestType, "(no subject ID)", "Subject ID not extractable");
            LawfulBasisDiagnostics.ConsentChecksTotal.Add(1,
                new TagList { { LawfulBasisDiagnostics.TagOutcome, "failed" } });
            return ApplyEnforcement(error, requestType);
        }

        // Log consent check start
        _logger.ConsentCheckStarted(requestType, subjectId);

        // Check consent status
        var consentResult = await _consentProvider
            .CheckConsentAsync(subjectId, attrInfo.Purposes, cancellationToken)
            .ConfigureAwait(false);

        return consentResult.Match(
            Right: result =>
            {
                if (result.HasValidConsent)
                {
                    _logger.ConsentCheckPassed(requestType, subjectId);
                    LawfulBasisDiagnostics.ConsentChecksTotal.Add(1,
                        new TagList { { LawfulBasisDiagnostics.TagOutcome, "passed" } });
                    return Right<EncinaError, bool>(true);
                }

                var consentError = GDPRErrors.ConsentNotFound(requestType, subjectId);
                _logger.ConsentCheckFailed(requestType, subjectId, "No active consent found");
                LawfulBasisDiagnostics.ConsentChecksTotal.Add(1,
                    new TagList { { LawfulBasisDiagnostics.TagOutcome, "failed" } });
                return ApplyEnforcement(consentError, requestType);
            },
            Left: providerError =>
            {
                _logger.ConsentCheckFailed(requestType, subjectId, providerError.Message);
                LawfulBasisDiagnostics.ConsentChecksTotal.Add(1,
                    new TagList { { LawfulBasisDiagnostics.TagOutcome, "failed" } });
                return ApplyEnforcement(providerError, requestType);
            });
    }

    // ================================================================
    // LIA validation
    // ================================================================

    private async ValueTask<Either<EncinaError, bool>> ValidateLIAAsync(
        LawfulBasisAttributeInfo attrInfo,
        Type requestType,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(attrInfo.LIAReference))
        {
            var error = GDPRErrors.LIANotFound(requestType);
            LawfulBasisDiagnostics.LiaChecksTotal.Add(1,
                new TagList { { LawfulBasisDiagnostics.TagOutcome, "failed" } });
            return ApplyEnforcement(error, requestType);
        }

        _logger.LIACheckStarted(requestType, attrInfo.LIAReference);

        var liaResult = await _liaAssessment
            .ValidateAsync(attrInfo.LIAReference, cancellationToken)
            .ConfigureAwait(false);

        return liaResult.Match(
            Right: result =>
            {
                if (result.IsValid)
                {
                    _logger.LIACheckPassed(requestType, attrInfo.LIAReference);
                    LawfulBasisDiagnostics.LiaChecksTotal.Add(1,
                        new TagList { { LawfulBasisDiagnostics.TagOutcome, "passed" } });
                    return Right<EncinaError, bool>(true);
                }

                var notApprovedError = GDPRErrors.LIANotApproved(requestType, attrInfo.LIAReference);
                _logger.LIACheckFailed(requestType, attrInfo.LIAReference, "LIA not approved");
                LawfulBasisDiagnostics.LiaChecksTotal.Add(1,
                    new TagList { { LawfulBasisDiagnostics.TagOutcome, "failed" } });
                return ApplyEnforcement(notApprovedError, requestType);
            },
            Left: error =>
            {
                _logger.LIACheckFailed(requestType, attrInfo.LIAReference, error.Message);
                LawfulBasisDiagnostics.LiaChecksTotal.Add(1,
                    new TagList { { LawfulBasisDiagnostics.TagOutcome, "failed" } });
                return ApplyEnforcement(error, requestType);
            });
    }

    // ================================================================
    // Enforcement
    // ================================================================

    private Either<EncinaError, bool> ApplyEnforcement(EncinaError error, Type requestType)
    {
        if (_options.EnforcementMode == LawfulBasisEnforcementMode.Block)
        {
            return Left<EncinaError, bool>(error);
        }

        // Warn mode — log warning but allow processing
        _logger.EnforcementWarning(requestType, error.Message);
        return Right<EncinaError, bool>(true);
    }

    // ================================================================
    // Observability
    // ================================================================

    private static void RecordPassed(Activity? activity, Type requestType, LawfulBasis? basis)
    {
        LawfulBasisDiagnostics.ValidationsTotal.Add(1, new TagList
        {
            { LawfulBasisDiagnostics.TagBasis, basis?.ToString() ?? "none" },
            { LawfulBasisDiagnostics.TagOutcome, "passed" }
        });
        LawfulBasisDiagnostics.CompleteValidation(activity, true);
    }

    private void RecordFailed(Activity? activity, Type requestType, LawfulBasis? basis, string failureReason)
    {
        LawfulBasisDiagnostics.ValidationsTotal.Add(1, new TagList
        {
            { LawfulBasisDiagnostics.TagBasis, basis?.ToString() ?? "none" },
            { LawfulBasisDiagnostics.TagOutcome, "failed" }
        });
        LawfulBasisDiagnostics.CompleteValidation(activity, false, failureReason);
        _logger.ValidationFailed(requestType, failureReason);
    }

    // ================================================================
    // Attribute resolution
    // ================================================================

    private static LawfulBasisAttributeInfo? ResolveAttributeInfo()
    {
        var requestType = typeof(TRequest);
        var lawfulBasisAttr = requestType.GetCustomAttribute<LawfulBasisAttribute>();
        var processingAttr = requestType.GetCustomAttribute<ProcessingActivityAttribute>();
        var processesPersonalData = requestType.GetCustomAttribute<ProcessesPersonalDataAttribute>() is not null;

        // No GDPR attributes at all — null signals "skip entirely"
        if (lawfulBasisAttr is null && processingAttr is null && !processesPersonalData)
        {
            return null;
        }

        // Has ProcessesPersonalData marker but no lawful basis declaration
        if (lawfulBasisAttr is null && processingAttr is null)
        {
            return new LawfulBasisAttributeInfo(
                Basis: null,
                Purpose: null,
                LIAReference: null,
                Source: "ProcessesPersonalDataAttribute",
                HasAttributeConflict: false,
                Purposes: []);
        }

        // Detect conflict: both attributes present with different bases
        var hasConflict = lawfulBasisAttr is not null
            && processingAttr is not null
            && lawfulBasisAttr.Basis != processingAttr.LawfulBasis;

        // LawfulBasisAttribute takes priority
        if (lawfulBasisAttr is not null)
        {
            var source = hasConflict
                ? "LawfulBasisAttribute (conflict with ProcessingActivityAttribute)"
                : "LawfulBasisAttribute";

            return new LawfulBasisAttributeInfo(
                Basis: lawfulBasisAttr.Basis,
                Purpose: lawfulBasisAttr.Purpose,
                LIAReference: lawfulBasisAttr.LIAReference,
                Source: source,
                HasAttributeConflict: hasConflict,
                Purposes: lawfulBasisAttr.Purpose is not null ? [lawfulBasisAttr.Purpose] : []);
        }

        // ProcessingActivityAttribute only
        return new LawfulBasisAttributeInfo(
            Basis: processingAttr!.LawfulBasis,
            Purpose: processingAttr.Purpose,
            LIAReference: null,
            Source: "ProcessingActivityAttribute",
            HasAttributeConflict: false,
            Purposes: [processingAttr.Purpose]);
    }

    /// <summary>
    /// Cached attribute information for a request type's lawful basis declarations.
    /// </summary>
    /// <param name="Basis">
    /// The lawful basis, or <c>null</c> when the request only has <see cref="ProcessesPersonalDataAttribute"/>
    /// without an explicit basis declaration.
    /// </param>
    /// <param name="Purpose">The processing purpose, if declared.</param>
    /// <param name="LIAReference">The LIA reference for legitimate interests basis.</param>
    /// <param name="Source">Describes the attribute source (for diagnostics and conflict reporting).</param>
    /// <param name="HasAttributeConflict">
    /// <c>true</c> when both <see cref="LawfulBasisAttribute"/> and <see cref="ProcessingActivityAttribute"/>
    /// are present and declare different lawful bases.
    /// </param>
    /// <param name="Purposes">
    /// The processing purposes for consent validation. Derived from the declared purpose.
    /// </param>
    private sealed record LawfulBasisAttributeInfo(
        LawfulBasis? Basis,
        string? Purpose,
        string? LIAReference,
        string Source,
        bool HasAttributeConflict,
        IReadOnlyList<string> Purposes);
}

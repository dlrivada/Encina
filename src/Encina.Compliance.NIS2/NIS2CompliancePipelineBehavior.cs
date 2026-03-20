using System.Diagnostics;

using Encina.Compliance.NIS2.Abstractions;
using Encina.Compliance.NIS2.Diagnostics;
using Encina.Compliance.NIS2.Model;
using Encina.Security.Audit;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static LanguageExt.Prelude;

namespace Encina.Compliance.NIS2;

/// <summary>
/// Pipeline behavior that enforces NIS2 compliance checks on requests decorated with
/// <see cref="NIS2CriticalAttribute"/>, <see cref="RequireMFAAttribute"/>, and/or
/// <see cref="NIS2SupplyChainCheckAttribute"/>.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// <para>
/// This behavior performs <strong>pre-execution</strong> checks — all NIS2 compliance validations
/// run before the request handler is invoked. If a check fails in
/// <see cref="NIS2EnforcementMode.Block"/> mode, the handler is never called and an error is
/// returned immediately.
/// </para>
/// <para>
/// <b>Attribute resolution:</b> Each closed generic type resolves its attribute info exactly once
/// via a <c>static readonly</c> field. This ensures zero reflection overhead on subsequent calls
/// for the same <typeparamref name="TRequest"/>/<typeparamref name="TResponse"/> pair.
/// </para>
/// <para>
/// <b>Audit trail:</b> When <see cref="IAuditStore"/> is registered, compliance check decisions
/// are recorded as fire-and-forget audit entries. Audit failures never block the request pipeline.
/// </para>
/// <para>
/// <b>Observability:</b> Emits OpenTelemetry activity spans via <c>Encina.Compliance.NIS2</c>
/// <see cref="ActivitySource"/>, counters via <see cref="System.Diagnostics.Metrics.Meter"/>,
/// and structured log messages via <c>[LoggerMessage]</c> source generator (EventIds 9200-9209).
/// </para>
/// <para>
/// Checks performed (in order):
/// <list type="number">
/// <item><description><c>[RequireMFA]</c> → <see cref="IMFAEnforcer.RequireMFAAsync{TRequest}"/></description></item>
/// <item><description><c>[NIS2SupplyChainCheck]</c> → <see cref="ISupplyChainSecurityValidator.ValidateSupplierForOperationAsync"/> for each supplier</description></item>
/// <item><description><c>[NIS2Critical]</c> → activity span and metrics recording</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class NIS2CompliancePipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Static per-generic-type attribute info. Each closed generic type resolves its own
    /// attribute info exactly once via the CLR's static field guarantee.
    /// </summary>
    private static readonly NIS2AttributeInfo CachedAttributeInfo = NIS2AttributeInfo.FromType(typeof(TRequest));

    private readonly IMFAEnforcer _mfaEnforcer;
    private readonly ISupplyChainSecurityValidator _supplyChainValidator;
    private readonly NIS2Options _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NIS2CompliancePipelineBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NIS2CompliancePipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    public NIS2CompliancePipelineBehavior(
        IMFAEnforcer mfaEnforcer,
        ISupplyChainSecurityValidator supplyChainValidator,
        IOptions<NIS2Options> options,
        IServiceProvider serviceProvider,
        ILogger<NIS2CompliancePipelineBehavior<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(mfaEnforcer);
        ArgumentNullException.ThrowIfNull(supplyChainValidator);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _mfaEnforcer = mfaEnforcer;
        _supplyChainValidator = supplyChainValidator;
        _options = options.Value;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        var requestTypeName = typeof(TRequest).Name;
        var enforcementModeName = _options.EnforcementMode.ToString();

        // Step 1: Disabled mode — no-op
        if (_options.EnforcementMode == NIS2EnforcementMode.Disabled)
        {
            _logger.NIS2PipelineDisabled(requestTypeName);
            return await nextStep().ConfigureAwait(false);
        }

        // Step 2: No NIS2 attributes — skip
        if (!CachedAttributeInfo.HasAnyAttribute)
        {
            _logger.NIS2PipelineNoAttributes(requestTypeName);
            return await nextStep().ConfigureAwait(false);
        }

        // Start activity span and stopwatch for duration tracking
        using var activity = NIS2Diagnostics.StartPipelineExecution(requestTypeName, enforcementModeName);
        var startTimestamp = Stopwatch.GetTimestamp();

        _logger.NIS2PipelineStarted(requestTypeName, enforcementModeName);

        // Track checks performed for audit trail
        var checksPerformed = new List<string>();
        var checksFailed = new List<string>();
        string? actionTaken = null;

        // Step 3: Pre-execution compliance checks
        try
        {
            // 3a: MFA enforcement
            if (CachedAttributeInfo.RequiresMFA && _options.EnforceMFA)
            {
                checksPerformed.Add("MFA");
                NIS2Diagnostics.MFAChecksTotal.Add(1,
                    new KeyValuePair<string, object?>(NIS2Diagnostics.TagRequestType, requestTypeName));

                var mfaResult = await _mfaEnforcer.RequireMFAAsync(request, context, cancellationToken)
                    .ConfigureAwait(false);

                if (mfaResult.IsLeft)
                {
                    var error = (EncinaError)mfaResult;
                    checksFailed.Add("MFA");

                    if (_options.EnforcementMode == NIS2EnforcementMode.Block)
                    {
                        actionTaken = "Blocked: MFA check failed";
                        _logger.NIS2PipelineBlocked(requestTypeName, "MFA", error.Message);
                        NIS2Diagnostics.RecordBlocked(activity, "MFA check failed");
                        RecordPipelineMetrics(startTimestamp, "blocked", enforcementModeName);

                        // Fire-and-forget audit
                        _ = RecordAuditAsync(context, requestTypeName, checksPerformed, checksFailed, actionTaken);

                        return Left<EncinaError, TResponse>(NIS2Errors.MFARequired(requestTypeName));
                    }

                    _logger.NIS2PipelineWarning(requestTypeName, "MFA", error.Message);
                }
            }

            // 3b: Supply chain checks
            foreach (var supplierId in CachedAttributeInfo.SupplyChainChecks)
            {
                checksPerformed.Add($"SupplyChain:{supplierId}");
                NIS2Diagnostics.SupplyChainChecksTotal.Add(1,
                    new KeyValuePair<string, object?>(NIS2Diagnostics.TagSupplierId, supplierId),
                    new KeyValuePair<string, object?>(NIS2Diagnostics.TagRequestType, requestTypeName));

                var validationResult = await _supplyChainValidator
                    .ValidateSupplierForOperationAsync(supplierId, cancellationToken)
                    .ConfigureAwait(false);

                var isAcceptable = validationResult.Match(
                    Right: ok => ok,
                    Left: _ => false);

                if (!isAcceptable)
                {
                    checksFailed.Add($"SupplyChain:{supplierId}");

                    if (_options.EnforcementMode == NIS2EnforcementMode.Block)
                    {
                        actionTaken = $"Blocked: Supply chain check failed for supplier '{supplierId}'";
                        _logger.NIS2PipelineBlocked(requestTypeName, "SupplyChain",
                            $"Supplier '{supplierId}' failed validation");
                        NIS2Diagnostics.RecordBlocked(activity, $"Supply chain: {supplierId}");
                        RecordPipelineMetrics(startTimestamp, "blocked", enforcementModeName);

                        // Fire-and-forget audit
                        _ = RecordAuditAsync(context, requestTypeName, checksPerformed, checksFailed, actionTaken);

                        return Left<EncinaError, TResponse>(
                            NIS2Errors.PipelineBlocked(requestTypeName,
                                $"Supply chain check failed for supplier '{supplierId}'."));
                    }

                    _logger.NIS2PipelineWarning(requestTypeName, "SupplyChain",
                        $"Supplier '{supplierId}' failed validation");
                }
            }

            // 3c: NIS2 Critical — enhanced observability
            if (CachedAttributeInfo.IsNIS2Critical)
            {
                checksPerformed.Add("NIS2Critical");
                _logger.NIS2PipelineCriticalOperation(requestTypeName,
                    CachedAttributeInfo.CriticalDescription ?? "N/A");
                activity?.SetTag(NIS2Diagnostics.TagCheckType, "critical");
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.NIS2PipelineError(requestTypeName, ex);
            NIS2Diagnostics.RecordFailed(activity, ex.Message);

            if (_options.EnforcementMode == NIS2EnforcementMode.Block)
            {
                actionTaken = $"Blocked: Compliance check exception: {ex.Message}";
                RecordPipelineMetrics(startTimestamp, "blocked", enforcementModeName);
                _ = RecordAuditAsync(context, requestTypeName, checksPerformed, checksFailed, actionTaken);

                return Left<EncinaError, TResponse>(
                    NIS2Errors.PipelineBlocked(requestTypeName,
                        $"Compliance check failed with exception: {ex.Message}"));
            }
        }

        // Step 4: Execute the handler
        var result = await nextStep().ConfigureAwait(false);

        // Step 5: Record metrics and audit
        var outcome = checksFailed.Count > 0 ? "warned" : "passed";
        actionTaken ??= checksFailed.Count > 0 ? "Warned: checks failed but allowed" : "Passed";

        if (checksFailed.Count > 0)
        {
            NIS2Diagnostics.RecordWarned(activity, string.Join(", ", checksFailed));
        }
        else
        {
            NIS2Diagnostics.RecordCompleted(activity);
        }

        RecordPipelineMetrics(startTimestamp, outcome, enforcementModeName);
        _logger.NIS2PipelineCompleted(requestTypeName, checksPerformed.Count);

        // Fire-and-forget audit
        _ = RecordAuditAsync(context, requestTypeName, checksPerformed, checksFailed, actionTaken);

        return result;
    }

    /// <summary>
    /// Records pipeline execution metrics (counter + duration histogram).
    /// </summary>
    private static void RecordPipelineMetrics(long startTimestamp, string outcome, string enforcementMode)
    {
        var elapsedMs = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;

        var tags = new TagList
        {
            { NIS2Diagnostics.TagOutcome, outcome },
            { NIS2Diagnostics.TagEnforcementMode, enforcementMode }
        };

        NIS2Diagnostics.PipelineExecutionsTotal.Add(1, tags);
        NIS2Diagnostics.PipelineDuration.Record(elapsedMs, tags);
    }

    /// <summary>
    /// Fire-and-forget audit recording. Never blocks the pipeline. Never throws.
    /// </summary>
    private async Task RecordAuditAsync(
        IRequestContext context,
        string requestTypeName,
        List<string> checksPerformed,
        List<string> checksFailed,
        string actionTaken)
    {
        try
        {
            var auditStore = _serviceProvider.GetService<IAuditStore>();
            if (auditStore is null)
            {
                return;
            }

            var now = DateTimeOffset.UtcNow;
            var entry = new AuditEntry
            {
                Id = Guid.NewGuid(),
                CorrelationId = context.CorrelationId ?? Guid.NewGuid().ToString("N"),
                UserId = context.UserId,
                TenantId = context.TenantId,
                Action = "NIS2ComplianceCheck",
                EntityType = requestTypeName,
                Outcome = checksFailed.Count == 0 ? AuditOutcome.Success : AuditOutcome.Failure,
                ErrorMessage = checksFailed.Count > 0
                    ? $"Failed checks: {string.Join(", ", checksFailed)}"
                    : null,
                TimestampUtc = now.UtcDateTime,
                StartedAtUtc = now,
                CompletedAtUtc = now,
                Metadata = new Dictionary<string, object?>
                {
                    ["nis2.enforcement_mode"] = _options.EnforcementMode.ToString(),
                    ["nis2.checks_performed"] = string.Join(", ", checksPerformed),
                    ["nis2.checks_failed"] = string.Join(", ", checksFailed),
                    ["nis2.action_taken"] = actionTaken,
                    ["nis2.is_critical"] = CachedAttributeInfo.IsNIS2Critical,
                    ["nis2.requires_mfa"] = CachedAttributeInfo.RequiresMFA,
                    ["nis2.supply_chain_suppliers"] = string.Join(", ", CachedAttributeInfo.SupplyChainChecks)
                }
            };

            var result = await auditStore.RecordAsync(entry).ConfigureAwait(false);

            _ = result.Match(
                Right: _ => LanguageExt.Unit.Default,
                Left: error =>
                {
                    _logger.NIS2PipelineAuditFailed(requestTypeName, error.Message);
                    return LanguageExt.Unit.Default;
                });
        }
        catch (Exception ex)
        {
            // Never fail the request pipeline due to audit failures
            _logger.NIS2PipelineAuditException(requestTypeName, ex);
        }
    }
}

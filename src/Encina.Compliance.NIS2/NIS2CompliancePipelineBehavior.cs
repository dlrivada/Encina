using System.Diagnostics;

using Encina.Compliance.NIS2.Abstractions;
using Encina.Compliance.NIS2.Diagnostics;
using Encina.Compliance.NIS2.Model;
using Encina.Diagnostics;
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
        var run = new PipelineRun(context, requestTypeName, enforcementModeName, activity, startTimestamp);

        // Step 3: Pre-execution compliance checks
        try
        {
            var blockError = await RunChecksAsync(request, run, cancellationToken).ConfigureAwait(false);
            if (blockError is { } blocked)
            {
                return Left<EncinaError, TResponse>(blocked);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var blockError = HandleCheckException(ex, run);
            if (blockError is { } blocked)
            {
                return Left<EncinaError, TResponse>(blocked);
            }
        }

        // Step 4: Execute the handler
        var result = await nextStep().ConfigureAwait(false);

        // Step 5: Record metrics and audit
        CompleteRun(run);

        return result;
    }

    /// <summary>
    /// Mutable state of one pipeline execution: what was checked, what failed and the action taken.
    /// </summary>
    private sealed class PipelineRun(
        IRequestContext context,
        string requestTypeName,
        string enforcementModeName,
        Activity? activity,
        long startTimestamp)
    {
        public IRequestContext Context { get; } = context;

        public string RequestTypeName { get; } = requestTypeName;

        public string EnforcementModeName { get; } = enforcementModeName;

        public Activity? Activity { get; } = activity;

        public long StartTimestamp { get; } = startTimestamp;

        public List<string> ChecksPerformed { get; } = [];

        public List<string> ChecksFailed { get; } = [];

        public string? ActionTaken { get; set; }
    }

    /// <summary>
    /// Runs the pre-execution checks in order (MFA, supply chain, critical). Returns the error
    /// that blocks the request, or <c>null</c> when processing may continue.
    /// </summary>
    private async ValueTask<EncinaError?> RunChecksAsync(
        TRequest request,
        PipelineRun run,
        CancellationToken cancellationToken)
    {
        // 3a: MFA enforcement
        var mfaBlock = await RunMfaCheckAsync(request, run, cancellationToken).ConfigureAwait(false);
        if (mfaBlock is not null)
        {
            return mfaBlock;
        }

        // 3b: Supply chain checks
        var supplyChainBlock = await RunSupplyChainChecksAsync(run, cancellationToken).ConfigureAwait(false);
        if (supplyChainBlock is not null)
        {
            return supplyChainBlock;
        }

        // 3c: NIS2 Critical — enhanced observability
        RecordCriticalOperation(run);
        return null;
    }

    private async ValueTask<EncinaError?> RunMfaCheckAsync(
        TRequest request,
        PipelineRun run,
        CancellationToken cancellationToken)
    {
        if (!CachedAttributeInfo.RequiresMFA || !_options.EnforceMFA)
        {
            return null;
        }

        run.ChecksPerformed.Add("MFA");
        NIS2Diagnostics.MFAChecksTotal.Add(1,
            new KeyValuePair<string, object?>(NIS2Diagnostics.TagRequestType, run.RequestTypeName));

        var mfaResult = await _mfaEnforcer.RequireMFAAsync(request, run.Context, cancellationToken)
            .ConfigureAwait(false);

        if (!mfaResult.IsLeft)
        {
            return null;
        }

        var error = (EncinaError)mfaResult;
        run.ChecksFailed.Add("MFA");

        if (_options.EnforcementMode == NIS2EnforcementMode.Block)
        {
            run.ActionTaken = "Blocked: MFA check failed";
            _logger.NIS2PipelineBlocked(run.RequestTypeName, "MFA", error.GetCode().IfNone("encina.unknown"));
            FinishBlocked(run, "MFA check failed");

            return NIS2Errors.MFARequired(run.RequestTypeName);
        }

        _logger.NIS2PipelineWarning(run.RequestTypeName, "MFA", error.GetCode().IfNone("encina.unknown"));
        return null;
    }

    private async ValueTask<EncinaError?> RunSupplyChainChecksAsync(
        PipelineRun run,
        CancellationToken cancellationToken)
    {
        foreach (var supplierId in CachedAttributeInfo.SupplyChainChecks)
        {
            var block = await CheckSupplierAsync(supplierId, run, cancellationToken).ConfigureAwait(false);
            if (block is not null)
            {
                return block;
            }
        }

        return null;
    }

    private async ValueTask<EncinaError?> CheckSupplierAsync(
        string supplierId,
        PipelineRun run,
        CancellationToken cancellationToken)
    {
        run.ChecksPerformed.Add($"SupplyChain:{supplierId}");
        NIS2Diagnostics.SupplyChainChecksTotal.Add(1,
            new KeyValuePair<string, object?>(NIS2Diagnostics.TagSupplierId, supplierId),
            new KeyValuePair<string, object?>(NIS2Diagnostics.TagRequestType, run.RequestTypeName));

        var validationResult = await _supplyChainValidator
            .ValidateSupplierForOperationAsync(supplierId, cancellationToken)
            .ConfigureAwait(false);

        var isAcceptable = validationResult.Match(
            Right: ok => ok,
            Left: _ => false);

        if (isAcceptable)
        {
            return null;
        }

        run.ChecksFailed.Add($"SupplyChain:{supplierId}");

        if (_options.EnforcementMode == NIS2EnforcementMode.Block)
        {
            run.ActionTaken = $"Blocked: Supply chain check failed for supplier '{supplierId}'";
            _logger.NIS2PipelineBlocked(run.RequestTypeName, "SupplyChain",
                $"Supplier '{supplierId}' failed validation");
            FinishBlocked(run, $"Supply chain: {supplierId}");

            return NIS2Errors.PipelineBlocked(run.RequestTypeName,
                $"Supply chain check failed for supplier '{supplierId}'.");
        }

        _logger.NIS2PipelineWarning(run.RequestTypeName, "SupplyChain",
            $"Supplier '{supplierId}' failed validation");
        return null;
    }

    private void RecordCriticalOperation(PipelineRun run)
    {
        if (!CachedAttributeInfo.IsNIS2Critical)
        {
            return;
        }

        run.ChecksPerformed.Add("NIS2Critical");
        _logger.NIS2PipelineCriticalOperation(run.RequestTypeName,
            CachedAttributeInfo.CriticalDescription ?? "N/A");
        run.Activity?.SetTag(NIS2Diagnostics.TagCheckType, "critical");
    }

    /// <summary>
    /// Records the blocked outcome (activity, metrics) and queues the fire-and-forget audit.
    /// </summary>
    private void FinishBlocked(PipelineRun run, string activityReason)
    {
        NIS2Diagnostics.RecordBlocked(run.Activity, activityReason);
        RecordPipelineMetrics(run.StartTimestamp, "blocked", run.EnforcementModeName);

        // Fire-and-forget audit
        _ = RecordAuditAsync(
            run.Context, run.RequestTypeName, run.ChecksPerformed, run.ChecksFailed, run.ActionTaken!);
    }

    /// <summary>
    /// Handles an unexpected exception raised by a compliance check. Returns the blocking error in
    /// <see cref="NIS2EnforcementMode.Block"/> mode, or <c>null</c> in Warn mode.
    /// </summary>
    private EncinaError? HandleCheckException(Exception ex, PipelineRun run)
    {
        _logger.NIS2PipelineError(run.RequestTypeName, ex.ForLogging());
        NIS2Diagnostics.RecordFailed(run.Activity, ex.GetType().Name);

        if (_options.EnforcementMode != NIS2EnforcementMode.Block)
        {
            return null;
        }

        run.ActionTaken = $"Blocked: Compliance check exception: {ex.GetType().Name}";
        RecordPipelineMetrics(run.StartTimestamp, "blocked", run.EnforcementModeName);
        _ = RecordAuditAsync(
            run.Context, run.RequestTypeName, run.ChecksPerformed, run.ChecksFailed, run.ActionTaken);

        return NIS2Errors.PipelineBlocked(run.RequestTypeName,
            $"Compliance check failed with exception: {ex.GetType().Name}");
    }

    private void CompleteRun(PipelineRun run)
    {
        var anyFailed = run.ChecksFailed.Count > 0;
        var outcome = anyFailed ? "warned" : "passed";
        run.ActionTaken ??= anyFailed ? "Warned: checks failed but allowed" : "Passed";

        if (anyFailed)
        {
            NIS2Diagnostics.RecordWarned(run.Activity, string.Join(", ", run.ChecksFailed));
        }
        else
        {
            NIS2Diagnostics.RecordCompleted(run.Activity);
        }

        RecordPipelineMetrics(run.StartTimestamp, outcome, run.EnforcementModeName);
        _logger.NIS2PipelineCompleted(run.RequestTypeName, run.ChecksPerformed.Count);

        // Fire-and-forget audit
        _ = RecordAuditAsync(
            run.Context, run.RequestTypeName, run.ChecksPerformed, run.ChecksFailed, run.ActionTaken);
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

    private AuditEntry BuildAuditEntry(
        IRequestContext context,
        string requestTypeName,
        List<string> checksPerformed,
        List<string> checksFailed,
        string actionTaken)
    {
        var now = DateTimeOffset.UtcNow;
        return new AuditEntry
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

            var entry = BuildAuditEntry(context, requestTypeName, checksPerformed, checksFailed, actionTaken);

            var result = await auditStore.RecordAsync(entry).ConfigureAwait(false);

            _ = result.Match(
                Right: _ => LanguageExt.Unit.Default,
                Left: error =>
                {
                    _logger.NIS2PipelineAuditFailed(requestTypeName, error.GetCode().IfNone("encina.unknown"));
                    return LanguageExt.Unit.Default;
                });
        }
        catch (Exception ex)
        {
            // Never fail the request pipeline due to audit failures
            _logger.NIS2PipelineAuditException(requestTypeName, ex.ForLogging());
        }
    }
}

using Encina.Security.Audit;
using Encina.Security.BreakTheGlass.Diagnostics;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.Security.BreakTheGlass;

/// <summary>
/// The <see cref="IAccessElevationGate"/> that lets a denied request continue under an active
/// break-the-glass elevation.
/// </summary>
/// <remarks>
/// <para>Order of operations:</para>
/// <list type="number">
/// <item><description>the feature is enabled;</description></item>
/// <item><description>the denial is overridable (<see cref="AccessElevationDenials.IsOverridable"/>, defence in depth);</description></item>
/// <item><description>the caller is a <see cref="IdentityKind.User"/> (never a service or an anonymous caller);</description></item>
/// <item><description>the request type is marked <see cref="BreakTheGlassEligibleAttribute"/> and yields a non-blank subject id;</description></item>
/// <item><description>an earlier gate of the same request already holds a grant for this subject: reuse it, write nothing;</description></item>
/// <item><description>when <see cref="BreakTheGlassOptions.RequireTenant"/> is on and the request carries no tenant, the denial stays before any store call (fail closed);</description></item>
/// <item><description>an elevation is effective for tenant, user, subject and scope at this instant (a failed lookup keeps the denial: it is logged and counted, never turned into another error);</description></item>
/// <item><description>the write-ahead access audit entry is stored (a failure returns <c>break_glass.audit_failed</c> and the request stays denied);</description></item>
/// <item><description>the read-audit purpose becomes <c>break-the-glass</c> and the elevation id joins the read-audit metadata.</description></item>
/// </list>
/// <para>Nothing personal reaches the audit metadata: the entry holds the request type name, the
/// denying stage and the denial code.</para>
/// </remarks>
internal sealed class BreakTheGlassAccessGate : IAccessElevationGate
{
    private readonly IBreakTheGlassStore _store;
    private readonly IOperationAuditStore _auditStore;
    private readonly IReadAuditContext _readAuditContext;
    private readonly BreakTheGlassRequestGrants _grants;
    private readonly IOptions<BreakTheGlassOptions> _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<BreakTheGlassAccessGate> _logger;

    public BreakTheGlassAccessGate(
        IBreakTheGlassStore store,
        IOperationAuditStore auditStore,
        IReadAuditContext readAuditContext,
        BreakTheGlassRequestGrants grants,
        IOptions<BreakTheGlassOptions> options,
        TimeProvider timeProvider,
        ILogger<BreakTheGlassAccessGate> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(auditStore);
        ArgumentNullException.ThrowIfNull(readAuditContext);
        ArgumentNullException.ThrowIfNull(grants);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _store = store;
        _auditStore = auditStore;
        _readAuditContext = readAuditContext;
        _grants = grants;
        _options = options;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<AccessElevation>>> TryElevateAsync<TRequest>(
        TRequest request,
        IRequestContext context,
        EncinaError denial,
        CancellationToken cancellationToken)
        where TRequest : notnull
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        // The identity can change between reads (its scope may end): read it once so every decision agrees.
        var identity = context.Identity;
        if (!TryResolve(request, identity, denial, out var eligibility, out var subjectId, out var userId))
        {
            return Option<AccessElevation>.None;
        }

        if (TryReuse(context.CorrelationId, typeof(TRequest), subjectId, out var held))
        {
            return Some(held);
        }

        // Fail closed: when a tenant is required, a tenant-less request never reaches the store.
        if (_options.Value.RequireTenant && string.IsNullOrWhiteSpace(context.TenantId))
        {
            BreakTheGlassLogMessages.AccessTenantMissing(_logger);
            return Option<AccessElevation>.None;
        }

        var found = await _store.FindEffectiveAsync(context.TenantId, userId, subjectId, eligibility.Scope, _timeProvider.GetUtcNow(), cancellationToken).ConfigureAwait(false);
        return await found.Match(
            Right: option => GrantAsync(option, typeof(TRequest), subjectId, userId, context, denial, cancellationToken),
            Left: LookupFailed).ConfigureAwait(false);
    }

    // A failed lookup keeps the original denial (the request stays denied; nobody gets in): the outage is
    // loud through the log, the counter and the health check, not through a changed response. The audit
    // write of an existing elevation is different and still returns a typed Left (see GrantAsync).
    private ValueTask<Either<EncinaError, Option<AccessElevation>>> LookupFailed(EncinaError error)
    {
        BreakTheGlassLogMessages.AccessLookupFailed(_logger, error.GetCode().IfNone("encina.unknown"));
        BreakTheGlassDiagnostics.RecordStoreFailure(BreakTheGlassDiagnostics.OperationAccessLookup);
        return ValueTask.FromResult<Either<EncinaError, Option<AccessElevation>>>(Option<AccessElevation>.None);
    }

    // A grant held by an earlier gate of the same request is reused only while it is still in force.
    private bool TryReuse(string correlationId, Type requestType, string subjectId, out AccessElevation held) =>
        _grants.TryGet(correlationId, requestType, subjectId, out held) && held.ExpiresAtUtc > _timeProvider.GetUtcNow();

    // Steps 1 to 4 and the subject: false when the denial stays.
    private bool TryResolve<TRequest>(
        TRequest request,
        RequestIdentity identity,
        EncinaError denial,
        out BreakTheGlassEligibilityInfo<TRequest> eligibility,
        out string subjectId,
        out string userId)
        where TRequest : notnull
    {
        var candidate = Candidate<TRequest>(identity, denial);
        var subject = candidate?.GetSubjectId(request);
        eligibility = candidate!;
        subjectId = subject!;
        userId = identity.UserId!;
        return candidate is not null && !string.IsNullOrWhiteSpace(subject);
    }

    // Steps 1 to 4: null when the denial cannot be offered or the type or caller is not eligible.
    private BreakTheGlassEligibilityInfo<TRequest>? Candidate<TRequest>(RequestIdentity identity, EncinaError denial)
        where TRequest : notnull =>
        _options.Value.Enabled
            && AccessElevationDenials.IsOverridable(denial)
            && identity is { Kind: IdentityKind.User, UserId: not null }
            ? BreakTheGlassEligibility<TRequest>.Info
            : null;

    private async ValueTask<Either<EncinaError, Option<AccessElevation>>> GrantAsync(
        Option<BreakTheGlassElevation> found,
        Type requestType,
        string subjectId,
        string userId,
        IRequestContext context,
        EncinaError denial,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var elevation = found.IfNoneUnsafe((BreakTheGlassElevation?)null);
        if (elevation is null || !elevation.IsEffectiveAt(now))
        {
            return Option<AccessElevation>.None;
        }

        var entry = BreakTheGlassAudit.Create(
            BreakTheGlassAudit.ActionAccess,
            AuditOutcome.Success,
            elevation.ElevationId,
            context.CorrelationId,
            userId,
            context.TenantId,
            now,
            AccessMetadata(requestType, denial));

        var audited = await BreakTheGlassAudit.WriteAsync(_auditStore, entry, cancellationToken).ConfigureAwait(false);
        return audited.Match<Either<EncinaError, Option<AccessElevation>>>(
            Right: _ =>
            {
                BreakTheGlassDiagnostics.RecordAccess(elevation.Scope, StageOf(denial));
                return Some(Elevate(elevation, requestType, subjectId, context.CorrelationId));
            },
            Left: error => error);
    }

    // The stage that denied the request ("authorization", "abac", ...), or "unknown".
    private static string StageOf(EncinaError denial) =>
        denial.GetDetails().TryGetValue("stage", out var stage) && stage is string name ? name : "unknown";

    private AccessElevation Elevate(BreakTheGlassElevation elevation, Type requestType, string subjectId, string correlationId)
    {
        if (_readAuditContext.Purpose is { } declared)
        {
            _readAuditContext.WithMetadata(BreakTheGlassReadAudit.DeclaredPurposeMetadataKey, declared);
        }

        _readAuditContext.WithPurpose(BreakTheGlassReadAudit.Purpose);
        _readAuditContext.WithMetadata(BreakTheGlassReadAudit.ElevationIdMetadataKey, elevation.ElevationId.ToString("D"));

        var grant = new AccessElevation(elevation.ElevationId, elevation.Scope, elevation.ExpiresAtUtc);
        _grants.Set(correlationId, requestType, subjectId, grant);
        return grant;
    }

    private static Dictionary<string, object?> AccessMetadata(Type requestType, EncinaError denial)
    {
        return new Dictionary<string, object?>
        {
            ["request_type"] = requestType.Name,
            ["gate"] = StageOf(denial),
            ["denial_code"] = denial.GetCode().IfNone("unknown")
        };
    }
}

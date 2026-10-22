using Encina.Security.Audit;
using LanguageExt;
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
/// <item><description>an elevation is effective for tenant, user, subject and scope at this instant;</description></item>
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

    public BreakTheGlassAccessGate(
        IBreakTheGlassStore store,
        IOperationAuditStore auditStore,
        IReadAuditContext readAuditContext,
        BreakTheGlassRequestGrants grants,
        IOptions<BreakTheGlassOptions> options,
        TimeProvider timeProvider)
    {
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

        var eligibility = Candidate<TRequest>(context, denial);
        var subjectId = eligibility?.GetSubjectId(request);
        if (eligibility is null || string.IsNullOrWhiteSpace(subjectId))
        {
            return Option<AccessElevation>.None;
        }

        var userId = context.Identity.UserId!;
        if (TryReuse(context.CorrelationId, typeof(TRequest), subjectId, out var held))
        {
            return Some(held);
        }

        var found = await _store.FindEffectiveAsync(context.TenantId, userId, subjectId, eligibility.Scope, _timeProvider.GetUtcNow(), cancellationToken).ConfigureAwait(false);
        return await found.Match(
            Right: option => GrantAsync(option, typeof(TRequest), subjectId, context, denial, cancellationToken),
            Left: _ => ValueTask.FromResult<Either<EncinaError, Option<AccessElevation>>>(BreakTheGlassErrors.StoreFailed())).ConfigureAwait(false);
    }

    // A grant held by an earlier gate of the same request is reused only while it is still in force.
    private bool TryReuse(string correlationId, Type requestType, string subjectId, out AccessElevation held) =>
        _grants.TryGet(correlationId, requestType, subjectId, out held) && held.ExpiresAtUtc > _timeProvider.GetUtcNow();

    // Steps 1 to 4: null when the denial cannot be offered or the type or caller is not eligible.
    private BreakTheGlassEligibilityInfo<TRequest>? Candidate<TRequest>(IRequestContext context, EncinaError denial)
        where TRequest : notnull =>
        _options.Value.Enabled
            && AccessElevationDenials.IsOverridable(denial)
            && context.Identity is { Kind: IdentityKind.User, UserId: not null }
            ? BreakTheGlassEligibility<TRequest>.Info
            : null;

    private async ValueTask<Either<EncinaError, Option<AccessElevation>>> GrantAsync(
        Option<BreakTheGlassElevation> found,
        Type requestType,
        string subjectId,
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
            context.Identity.UserId,
            context.TenantId,
            now,
            AccessMetadata(requestType, denial));

        var audited = await BreakTheGlassAudit.WriteAsync(_auditStore, entry, cancellationToken).ConfigureAwait(false);
        return audited.Match<Either<EncinaError, Option<AccessElevation>>>(
            Right: _ => Some(Elevate(elevation, requestType, subjectId, context.CorrelationId)),
            Left: error => error);
    }

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
        var details = denial.GetDetails();
        return new Dictionary<string, object?>
        {
            ["request_type"] = requestType.Name,
            ["gate"] = details.TryGetValue("stage", out var stage) && stage is string name ? name : "unknown",
            ["denial_code"] = denial.GetCode().IfNone("unknown")
        };
    }
}

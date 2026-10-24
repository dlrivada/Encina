using Encina.Security.Audit;
using LanguageExt;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.Security.BreakTheGlass;

/// <summary>
/// The default <see cref="IBreakTheGlassService"/>.
/// </summary>
/// <remarks>
/// <para>
/// Order of <see cref="OpenAsync(BreakTheGlassRequest, CancellationToken)"/>: authenticated caller,
/// authorizer, validator, tenant rule, no active elevation for the same tenant, user, subject and
/// scope, then the write-ahead <c>break_glass.opened</c> audit entry, the store row (a store failure
/// writes <c>break_glass.open_failed</c> and fails) and finally the notification.
/// </para>
/// <para>
/// Every audit write is awaited and fail-closed. No subject id, user id, justification, review note
/// or error message is placed in audit metadata or notifications. A failing notification publish is
/// returned to the caller, because an error is never reported as success; the elevation is then
/// already open and audited and can be read and revoked by its id.
/// </para>
/// </remarks>
public sealed class DefaultBreakTheGlassService : IBreakTheGlassService
{
    private readonly IBreakTheGlassStore _store;
    private readonly IOperationAuditStore _auditStore;
    private readonly IRequestContextAccessor _contextAccessor;
    private readonly IBreakTheGlassOpenAuthorizer _authorizer;
    private readonly BreakTheGlassRequestValidator _validator;
    private readonly IEncina _encina;
    private readonly IOptions<BreakTheGlassOptions> _options;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultBreakTheGlassService"/> class.
    /// </summary>
    /// <param name="store">The elevation store.</param>
    /// <param name="auditStore">The operation audit sink.</param>
    /// <param name="contextAccessor">The ambient request context.</param>
    /// <param name="authorizer">Decides who may open and review.</param>
    /// <param name="validator">Validates open requests.</param>
    /// <param name="encina">Publishes the notifications.</param>
    /// <param name="options">The options.</param>
    /// <param name="timeProvider">The clock.</param>
    public DefaultBreakTheGlassService(
        IBreakTheGlassStore store,
        IOperationAuditStore auditStore,
        IRequestContextAccessor contextAccessor,
        IBreakTheGlassOpenAuthorizer authorizer,
        BreakTheGlassRequestValidator validator,
        IEncina encina,
        IOptions<BreakTheGlassOptions> options,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(auditStore);
        ArgumentNullException.ThrowIfNull(contextAccessor);
        ArgumentNullException.ThrowIfNull(authorizer);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(encina);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _store = store;
        _auditStore = auditStore;
        _contextAccessor = contextAccessor;
        _authorizer = authorizer;
        _validator = validator;
        _encina = encina;
        _options = options;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, BreakTheGlassElevation>> OpenAsync(
        string subjectId, string scope, string justification, TimeSpan duration, CancellationToken cancellationToken = default) =>
        OpenAsync(new BreakTheGlassRequest(subjectId, scope, justification, duration), cancellationToken);

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, BreakTheGlassElevation>> OpenAsync(BreakTheGlassRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryGetCaller(out var caller, out var unauthenticated))
        {
            return unauthenticated;
        }

        var prepared = await PrepareOpenAsync(caller, request, cancellationToken).ConfigureAwait(false);
        if (!prepared.TryUnwrap(out var valid, out var preparationError))
        {
            return preparationError;
        }

        var now = _timeProvider.GetUtcNow();
        var elevation = new BreakTheGlassElevation
        {
            ElevationId = Guid.CreateVersion7(now),
            TenantId = caller.Context.TenantId,
            UserId = caller.UserId,
            SubjectId = valid.SubjectId,
            Scope = valid.Scope,
            Justification = valid.Justification,
            OpenedAtUtc = now,
            ExpiresAtUtc = now + valid.Duration
        };

        var persisted = await PersistOpenAsync(caller, elevation, cancellationToken).ConfigureAwait(false);
        if (!persisted.TryUnwrap(out _, out var persistError))
        {
            return persistError;
        }

        var published = await PublishOpenedAsync(elevation, cancellationToken).ConfigureAwait(false);
        return published.Map(_ => elevation);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RevokeAsync(Guid elevationId, string reason, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reason);

        if (!TryGetCaller(out var caller, out var unauthenticated))
        {
            return unauthenticated;
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return BreakTheGlassErrors.ValidationFailed("reason_required");
        }

        var loaded = await LoadAsync(caller, elevationId, cancellationToken).ConfigureAwait(false);
        if (!loaded.TryUnwrap(out var elevation, out var loadError))
        {
            return loadError;
        }

        if (elevation.Status != BreakTheGlassStatus.Active)
        {
            return BreakTheGlassErrors.NotActive();
        }

        return await CloseAsync(caller, elevation, reason.Trim(), BreakTheGlassAudit.ActionRevoked, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> ReviewAsync(
        Guid elevationId, BreakTheGlassReviewOutcome outcome, string? note, CancellationToken cancellationToken = default)
    {
        if (!TryGetCaller(out var caller, out var unauthenticated))
        {
            return unauthenticated;
        }

        var loaded = await LoadForReviewAsync(caller, elevationId, cancellationToken).ConfigureAwait(false);
        if (!loaded.TryUnwrap(out var elevation, out var loadError))
        {
            return loadError;
        }

        var recorded = await RecordReviewAsync(caller, elevation, outcome, note, cancellationToken).ConfigureAwait(false);
        if (!recorded.TryUnwrap(out _, out var reviewError))
        {
            return reviewError;
        }

        var revoked = await RevokeUnjustifiedAsync(caller, elevation, outcome, cancellationToken).ConfigureAwait(false);
        if (!revoked.TryUnwrap(out _, out var revokeError))
        {
            return revokeError;
        }

        return await PublishReviewedAsync(elevation, outcome, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, PagedResult<BreakTheGlassElevation>>> GetReviewQueueAsync(
        BreakTheGlassReviewQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!TryGetCaller(out var caller, out var unauthenticated))
        {
            return unauthenticated;
        }

        return IsReviewer(caller.Identity)
            ? await _store.QueryReviewQueueAsync(caller.Context.TenantId, query, cancellationToken).ConfigureAwait(false)
            : BreakTheGlassErrors.ReviewerNotEligible();
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, BreakTheGlassElevation>> GetAsync(Guid elevationId, CancellationToken cancellationToken = default)
    {
        if (!TryGetCaller(out var caller, out var unauthenticated))
        {
            return unauthenticated;
        }

        return await LoadAsync(caller, elevationId, cancellationToken).ConfigureAwait(false);
    }

    // Resolves the caller for every operation; refuses an unauthenticated caller and, when RequireTenant is on, a missing tenant.
    private bool TryGetCaller(out Caller caller, out EncinaError refusal)
    {
        var context = _contextAccessor.RequestContext;
        caller = default;
        if (context?.Identity is not { IsAuthenticated: true, UserId: { } userId } identity)
        {
            refusal = EncinaErrors.Unauthenticated();
            return false;
        }

        if (IsTenantMissing(context))
        {
            refusal = BreakTheGlassErrors.TenantRequired();
            return false;
        }

        caller = new Caller(context, identity, userId);
        refusal = default;
        return true;
    }

    private bool IsTenantMissing(IRequestContext context) =>
        _options.Value.RequireTenant && string.IsNullOrWhiteSpace(context.TenantId);

    private bool IsReviewer(RequestIdentity identity) =>
        identity.Kind == IdentityKind.User && _options.Value.ReviewerRoles.Overlaps(identity.Roles);

    private static bool TryGetElevation(
        Either<EncinaError, Option<BreakTheGlassElevation>> found, out BreakTheGlassElevation elevation, out EncinaError error)
    {
        elevation = null!;
        if (!found.TryUnwrap(out var option, out error))
        {
            return false;
        }

        var value = option.IfNoneUnsafe((BreakTheGlassElevation?)null);
        if (value is null)
        {
            error = BreakTheGlassErrors.NotFound();
            return false;
        }

        elevation = value;
        return true;
    }

    // Only the opener and a reviewer may see or act on an elevation; anyone else is refused.
    private async ValueTask<Either<EncinaError, BreakTheGlassElevation>> LoadAsync(Caller caller, Guid elevationId, CancellationToken cancellationToken)
    {
        var found = await _store.GetByIdAsync(caller.Context.TenantId, elevationId, cancellationToken).ConfigureAwait(false);
        if (!TryGetElevation(found, out var elevation, out var error))
        {
            return error;
        }

        return string.Equals(elevation.UserId, caller.UserId, StringComparison.Ordinal) || IsReviewer(caller.Identity)
            ? elevation
            : BreakTheGlassErrors.NotEligible();
    }

    private async ValueTask<Either<EncinaError, BreakTheGlassElevation>> LoadForReviewAsync(Caller caller, Guid elevationId, CancellationToken cancellationToken)
    {
        var found = await _store.GetByIdAsync(caller.Context.TenantId, elevationId, cancellationToken).ConfigureAwait(false);
        if (!TryGetElevation(found, out var elevation, out var findError))
        {
            return findError;
        }

        var authorized = await _authorizer.AuthorizeReviewAsync(caller.Identity, elevation, cancellationToken).ConfigureAwait(false);
        return authorized.Map(_ => elevation);
    }

    private async ValueTask<Either<EncinaError, BreakTheGlassRequest>> PrepareOpenAsync(
        Caller caller, BreakTheGlassRequest request, CancellationToken cancellationToken)
    {
        var authorized = await _authorizer.AuthorizeOpenAsync(caller.Identity, request.Scope?.Trim() ?? string.Empty, cancellationToken).ConfigureAwait(false);
        if (!authorized.TryUnwrap(out _, out var authorizationError))
        {
            return authorizationError;
        }

        if (!_validator.Validate(request).TryUnwrap(out var valid, out var validationError))
        {
            return validationError;
        }

        var conflict = await FindConflictAsync(caller, valid, cancellationToken).ConfigureAwait(false);
        return conflict.Match<Either<EncinaError, BreakTheGlassRequest>>(Some: error => error, None: () => valid);
    }

    // Some(error) when an effective elevation exists or the lookup fails.
    private async ValueTask<Option<EncinaError>> FindConflictAsync(Caller caller, BreakTheGlassRequest valid, CancellationToken cancellationToken)
    {
        var active = await _store.FindEffectiveAsync(
            caller.Context.TenantId, caller.UserId, valid.SubjectId, valid.Scope, _timeProvider.GetUtcNow(), cancellationToken).ConfigureAwait(false);
        if (!active.TryUnwrap(out var existing, out _))
        {
            return BreakTheGlassErrors.StoreFailed();
        }

        return existing.IsSome ? Some(BreakTheGlassErrors.AlreadyActive()) : Option<EncinaError>.None;
    }

    // Write-ahead: the opened entry first, then the row; a failed row adds an open_failed entry and fails.
    private async ValueTask<Either<EncinaError, Unit>> PersistOpenAsync(Caller caller, BreakTheGlassElevation elevation, CancellationToken cancellationToken)
    {
        var metadata = new Dictionary<string, object?>
        {
            ["scope"] = elevation.Scope,
            ["expires_at_utc"] = elevation.ExpiresAtUtc.ToString("O", System.Globalization.CultureInfo.InvariantCulture)
        };

        var audited = await WriteAuditAsync(BreakTheGlassAudit.ActionOpened, AuditOutcome.Success, caller, elevation, metadata, cancellationToken).ConfigureAwait(false);
        if (!audited.TryUnwrap(out _, out var auditError))
        {
            return auditError;
        }

        var added = await _store.AddAsync(elevation, cancellationToken).ConfigureAwait(false);
        if (added.TryUnwrap(out _, out var addError))
        {
            return unit;
        }

        // The store's conditional add is authoritative: a lost race is already_active, anything else a store failure.
        var refusal = IsAlreadyActive(addError) ? BreakTheGlassErrors.AlreadyActive() : BreakTheGlassErrors.StoreFailed();
        metadata["reason"] = IsAlreadyActive(addError) ? "already_active" : "store_failed";

        // Best effort: the operation already fails with the refusal.
        _ = await WriteAuditAsync(BreakTheGlassAudit.ActionOpenFailed, AuditOutcome.Error, caller, elevation, metadata, cancellationToken).ConfigureAwait(false);
        return refusal;
    }

    private static bool IsAlreadyActive(EncinaError error) =>
        error.GetCode().Match(Some: code => code == BreakTheGlassErrors.AlreadyActiveCode, None: () => false);

    private async ValueTask<Either<EncinaError, Unit>> CloseAsync(
        Caller caller, BreakTheGlassElevation elevation, string reason, string action, CancellationToken cancellationToken)
    {
        var metadata = new Dictionary<string, object?> { ["scope"] = elevation.Scope };
        var audited = await WriteAuditAsync(action, AuditOutcome.Success, caller, elevation, metadata, cancellationToken).ConfigureAwait(false);
        if (!audited.TryUnwrap(out _, out var auditError))
        {
            return auditError;
        }

        var closed = await _store.TryCloseAsync(
            elevation.TenantId, elevation.ElevationId, BreakTheGlassStatus.Revoked, reason, _timeProvider.GetUtcNow(), cancellationToken).ConfigureAwait(false);
        if (!closed.TryUnwrap(out var applied, out _))
        {
            return BreakTheGlassErrors.StoreFailed();
        }

        return applied ? unit : BreakTheGlassErrors.NotActive();
    }

    private async ValueTask<Either<EncinaError, Unit>> RecordReviewAsync(
        Caller caller, BreakTheGlassElevation elevation, BreakTheGlassReviewOutcome outcome, string? note, CancellationToken cancellationToken)
    {
        if (elevation.ReviewOutcome is not null)
        {
            // Retry of an Unjustified ruling whose revoke or notification failed: the ruling stands, finish the rest.
            return IsInterruptedUnjustifiedRuling(elevation, outcome) ? unit : BreakTheGlassErrors.AlreadyReviewed();
        }

        var metadata = new Dictionary<string, object?> { ["scope"] = elevation.Scope, ["outcome"] = outcome.ToString() };
        var audited = await WriteAuditAsync(BreakTheGlassAudit.ActionReviewed, AuditOutcome.Success, caller, elevation, metadata, cancellationToken).ConfigureAwait(false);
        if (!audited.TryUnwrap(out _, out var auditError))
        {
            return auditError;
        }

        return await StoreReviewAsync(caller, elevation, outcome, note, cancellationToken).ConfigureAwait(false);
    }

    private static bool IsInterruptedUnjustifiedRuling(BreakTheGlassElevation elevation, BreakTheGlassReviewOutcome outcome) =>
        outcome == BreakTheGlassReviewOutcome.Unjustified
        && elevation.ReviewOutcome == BreakTheGlassReviewOutcome.Unjustified
        && elevation.Status == BreakTheGlassStatus.Active;

    private async ValueTask<Either<EncinaError, Unit>> StoreReviewAsync(
        Caller caller, BreakTheGlassElevation elevation, BreakTheGlassReviewOutcome outcome, string? note, CancellationToken cancellationToken)
    {
        var recorded = await _store.TryRecordReviewAsync(
            elevation.TenantId, elevation.ElevationId, outcome, caller.UserId, note?.Trim(), _timeProvider.GetUtcNow(), cancellationToken).ConfigureAwait(false);
        if (!recorded.TryUnwrap(out var applied, out _))
        {
            return BreakTheGlassErrors.StoreFailed();
        }

        return applied ? unit : BreakTheGlassErrors.AlreadyReviewed();
    }

    // An unjustified ruling revokes an elevation that is still active; one already closed stays as it is.
    private async ValueTask<Either<EncinaError, Unit>> RevokeUnjustifiedAsync(
        Caller caller, BreakTheGlassElevation elevation, BreakTheGlassReviewOutcome outcome, CancellationToken cancellationToken)
    {
        if (outcome != BreakTheGlassReviewOutcome.Unjustified || elevation.Status != BreakTheGlassStatus.Active)
        {
            return unit;
        }

        var closed = await CloseAsync(caller, elevation, "review_unjustified", BreakTheGlassAudit.ActionRevoked, cancellationToken).ConfigureAwait(false);
        // Already closed by someone else in the meantime: the intent (no access) holds.
        return closed.Match(Right: _ => Right<EncinaError, Unit>(unit), Left: e => IsNotActive(e) ? Right<EncinaError, Unit>(unit) : Left(e));
    }

    private static bool IsNotActive(EncinaError error) =>
        error.GetCode().Match(Some: code => code == BreakTheGlassErrors.NotActiveCode, None: () => false);

    private ValueTask<Either<EncinaError, Unit>> WriteAuditAsync(
        string action, AuditOutcome outcome, Caller caller, BreakTheGlassElevation elevation, IReadOnlyDictionary<string, object?> metadata, CancellationToken cancellationToken)
    {
        var entry = BreakTheGlassAudit.Create(
            action, outcome, elevation.ElevationId, caller.Context.CorrelationId, caller.UserId, caller.Context.TenantId, _timeProvider.GetUtcNow(), metadata);
        return BreakTheGlassAudit.WriteAsync(_auditStore, entry, cancellationToken);
    }

    private ValueTask<Either<EncinaError, Unit>> PublishOpenedAsync(BreakTheGlassElevation elevation, CancellationToken cancellationToken) =>
        _options.Value.PublishNotifications
            ? _encina.Publish(
                new BreakTheGlassOpenedNotification(elevation.ElevationId, elevation.TenantId, elevation.Scope, elevation.OpenedAtUtc, elevation.ExpiresAtUtc),
                cancellationToken)
            : ValueTask.FromResult<Either<EncinaError, Unit>>(unit);

    private ValueTask<Either<EncinaError, Unit>> PublishReviewedAsync(
        BreakTheGlassElevation elevation, BreakTheGlassReviewOutcome outcome, CancellationToken cancellationToken) =>
        _options.Value.PublishNotifications
            ? _encina.Publish(
                new BreakTheGlassReviewedNotification(elevation.ElevationId, elevation.TenantId, elevation.Scope, outcome, _timeProvider.GetUtcNow()),
                cancellationToken)
            : ValueTask.FromResult<Either<EncinaError, Unit>>(unit);

    private readonly record struct Caller(IRequestContext Context, RequestIdentity Identity, string UserId);
}

/// <summary>Unwraps an <c>Either</c> into an out value and an out error.</summary>
internal static class BreakTheGlassEitherExtensions
{
    public static bool TryUnwrap<T>(this Either<EncinaError, T> either, out T value, out EncinaError error)
    {
        if (either.IsRight)
        {
            value = either.IfLeft(default(T)!);
            error = default;
            return true;
        }

        value = default!;
        error = either.Match(Right: _ => default(EncinaError), Left: e => e);
        return false;
    }
}

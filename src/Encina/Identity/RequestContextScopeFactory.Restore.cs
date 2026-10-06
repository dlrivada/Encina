using System.Security.Claims;
using LanguageExt;

namespace Encina;

// RunRestoredAsync: rebuilds the originating actor of a deferred message (SPEC-002 REQ-015).
internal sealed partial class RequestContextScopeFactory
{
    /// <summary>
    /// The authentication type of the synthetic principal of a restored user.
    /// </summary>
    internal const string RestoredAuthenticationType = "encina-restored";

    /// <inheritdoc />
    public Task<Either<EncinaError, T>> RunRestoredAsync<T>(
        PersistedRequestIdentity persisted,
        PersistedIdentitySource source,
        Func<IRequestContext, CancellationToken, Task<Either<EncinaError, T>>> work,
        string? configuredTenantId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(persisted);
        ArgumentNullException.ThrowIfNull(work);

        var refusal = Precheck(cancellationToken);
        refusal ??= ValidatePersisted(persisted, source, configuredTenantId);
        refusal ??= CheckChain(persisted.Kind, inboundOptIn: false, out _);

        return refusal is not null
            ? Refuse<T>(refusal, persisted.Kind)
            : OpenRestored(persisted, source, configuredTenantId, work, cancellationToken);
    }

    private Task<Either<EncinaError, T>> OpenRestored<T>(
        PersistedRequestIdentity persisted,
        PersistedIdentitySource source,
        string? configuredTenantId,
        Func<IRequestContext, CancellationToken, Task<Either<EncinaError, T>>> work,
        CancellationToken cancellationToken)
    {
        // External (D3, E2): the row crossed the trust boundary, so neither its actor nor its tenant
        // is trusted; the message runs anonymous, marked inbound, with the dispatcher's trusted tenant.
        var external = source == PersistedIdentitySource.External;
        var ambient = _accessor.RequestContext;
        var context = NewContext(
                external ? RequestIdentity.Anonymous : RestoredIdentity(persisted),
                persisted.CorrelationId,
                external ? configuredTenantId : persisted.TenantId,
                external ? RequestOrigin.Inbound : RequestOrigin.Restored)
            .WithCausationId(persisted.CausationId);

        LogTenantChange(nameof(RunRestoredAsync), ambient, context);
        RequestIdentityLog.IdentityRestored(_logger, context.Identity.Kind, context.CorrelationId);
        return Open(context, work, cancellationToken);
    }

    // A restored user carries no roles and no permissions (a row is never a source of authority);
    // a restored service takes its authority from the catalog. Validation has already passed.
    // crap-exempt: single-question switch — the identity of each persisted kind.
    private RequestIdentity RestoredIdentity(PersistedRequestIdentity persisted) => persisted.Kind switch
    {
        IdentityKind.User => RequestIdentity.ForUser(
            persisted.ActorId!,
            new ClaimsPrincipal(new ClaimsIdentity([new Claim(_options.UserIdClaimTypes[0], persisted.ActorId!)], RestoredAuthenticationType)),
            issuer: new IdentityIssuer(),
            perTokenClaimTypes: _perTokenClaimTypes),
        IdentityKind.Service => ServiceIdentity(RestoredService(persisted.ActorId!)),
        _ => RequestIdentity.Anonymous
    };

    private ServiceIdentityDefinition RestoredService(string name) =>
        _catalog.TryGet(name, out var definition)
            ? definition
            : throw new InvalidOperationException("The persisted service identity was validated but is no longer declared.");

    private EncinaError? ValidatePersisted(PersistedRequestIdentity persisted, PersistedIdentitySource source, string? configuredTenantId)
    {
        var reason = SourceProblem(source, configuredTenantId)
            ?? IdsProblem(persisted)
            ?? (source == PersistedIdentitySource.External ? null : ActorProblem(persisted));

        return reason is null ? (EncinaError?)null : RequestIdentityErrors.InvalidPersistedIdentity(reason);
    }

    private static string? SourceProblem(PersistedIdentitySource source, string? configuredTenantId)
    {
        if (!Enum.IsDefined(source))
        {
            return "source";
        }

        return source == PersistedIdentitySource.External && configuredTenantId is not null && InboundRequestNormalizer.Id(configuredTenantId) is null
            ? "configuredTenantId"
            : null;
    }

    private static string? IdsProblem(PersistedRequestIdentity persisted)
    {
        if (InboundRequestNormalizer.Id(persisted.CorrelationId) is null)
        {
            return "correlationId";
        }

        return persisted.CausationId is not null && InboundRequestNormalizer.Id(persisted.CausationId) is null
            ? "causationId"
            : null;
    }

    // Internal sources only: the actor must be valid for its kind, and the tenant a bounded id.
    private string? ActorProblem(PersistedRequestIdentity persisted)
    {
        if (persisted.TenantId is not null && InboundRequestNormalizer.Id(persisted.TenantId) is null)
        {
            return "tenantId";
        }

        return IsValidActor(persisted) ? null : "actor";
    }

    // crap-exempt: single-question switch — whether the actor is valid for each persisted kind.
    private bool IsValidActor(PersistedRequestIdentity persisted) => persisted.Kind switch
    {
        IdentityKind.User => RequestIdentity.IsValidUserId(persisted.ActorId),
        IdentityKind.Service => persisted.ActorId is not null
            && _catalog.TryGet(persisted.ActorId, out var definition)
            && !definition.IsBuiltIn,
        IdentityKind.Anonymous => persisted.ActorId is null,
        _ => false
    };
}

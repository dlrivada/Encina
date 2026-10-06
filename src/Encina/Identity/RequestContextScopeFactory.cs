using System.Collections.Frozen;
using System.Diagnostics;
using System.Security.Claims;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina;

/// <summary>
/// The default <see cref="IRequestContextScopeFactory"/> and <see cref="IInternalRequestContextScopeFactory"/>:
/// the only production code that binds an identity to a unit of work.
/// </summary>
/// <remarks>
/// <para>
/// Every member checks, in order: the accessor is the default one; the token is not cancelled; the
/// arguments; the facts of the current holder chain. A refusal returns <c>Left</c> without invoking
/// <c>work</c> and logs EventId 167 (except cancellation). Otherwise one private <c>async</c> method
/// pushes a new scope holder, awaits <c>work</c> and, in its <c>finally</c>, ends the holder (logs
/// 168, and 170 when an enclosing scope had already ended). Ending only invalidates: the caller's
/// holder comes back through the <c>async</c> frame, because the push happens inside that
/// <c>async</c> body and never in a synchronous wrapper.
/// </para>
/// <para>
/// Every scope gets a new <see cref="RequestIdentity"/> stamped with a new <see cref="IdentityIssuer"/>,
/// so an identity copied out of the scope stops reading as a caller once the scope ends.
/// </para>
/// </remarks>
internal sealed partial class RequestContextScopeFactory : IRequestContextScopeFactory, IInternalRequestContextScopeFactory
{
    private static readonly IdentityScopeOptions DefaultOptions = new();

    private readonly IRequestContextAccessor _accessor;
    private readonly IServiceIdentityCatalog _catalog;
    private readonly IRequestIdentityFactory _identityFactory;
    private readonly RequestIdentityOptions _options;
    private readonly FrozenSet<string> _perTokenClaimTypes;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestContextScopeFactory"/> class.
    /// </summary>
    /// <param name="accessor">The ambient accessor; scopes open only with the default <see cref="RequestContextAccessor"/>.</param>
    /// <param name="catalog">The declared service identities.</param>
    /// <param name="identityFactory">The claim mapper.</param>
    /// <param name="options">The claim map.</param>
    /// <param name="timeProvider">The clock that stamps every scope context.</param>
    /// <param name="logger">The logger (EventIds 166-175); none by default.</param>
    public RequestContextScopeFactory(
        IRequestContextAccessor accessor,
        IServiceIdentityCatalog catalog,
        IRequestIdentityFactory identityFactory,
        IOptions<RequestIdentityOptions> options,
        TimeProvider timeProvider,
        ILogger<RequestContextScopeFactory>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(accessor);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(identityFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _accessor = accessor;
        _catalog = catalog;
        _identityFactory = identityFactory;
        _options = options.Value;
        _perTokenClaimTypes = _options.PerTokenClaimTypes
            .Where(static claimType => !string.IsNullOrWhiteSpace(claimType))
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        _timeProvider = timeProvider;
        _logger = logger ?? (ILogger)NullLogger.Instance;
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, T>> RunAsServiceAsync<T>(
        string serviceId,
        Func<IRequestContext, CancellationToken, Task<Either<EncinaError, T>>> work,
        IdentityScopeOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceId);
        ArgumentNullException.ThrowIfNull(work);

        var scopeOptions = options ?? DefaultOptions;
        ServiceIdentityDefinition? definition = null;
        var overInbound = false;
        var refusal = Precheck(cancellationToken);
        refusal ??= FindService(serviceId, builtIn: false, out definition);
        refusal ??= CheckChain(IdentityKind.Service, scopeOptions.AllowOverInbound, out overInbound);

        return refusal is not null
            ? Refuse<T>(refusal, IdentityKind.Service)
            : OpenService(nameof(RunAsServiceAsync), definition!, scopeOptions.TenantId, overInbound, work, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, T>> RunAsBuiltInAsync<T>(
        string builtInName,
        Func<IRequestContext, CancellationToken, Task<Either<EncinaError, T>>> work,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(builtInName);
        ArgumentNullException.ThrowIfNull(work);

        ServiceIdentityDefinition? definition = null;
        var refusal = Precheck(cancellationToken);
        refusal ??= FindService(builtInName, builtIn: true, out definition);
        refusal ??= CheckChain(IdentityKind.Service, inboundOptIn: false, out _);

        return refusal is not null
            ? Refuse<T>(refusal, IdentityKind.Service)
            : OpenService(nameof(RunAsBuiltInAsync), definition!, tenantId: null, overInbound: false, work, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, T>> RunAsPrincipalAsync<T>(
        ClaimsPrincipal principal,
        Func<IRequestContext, CancellationToken, Task<Either<EncinaError, T>>> work,
        IdentityScopeOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentNullException.ThrowIfNull(work);

        var scopeOptions = options ?? DefaultOptions;
        var requestedKind = RequestedKindOf(principal);
        var principalTenant = _identityFactory.ResolveTenantId(principal);
        var overInbound = false;
        var refusal = Precheck(cancellationToken);
        refusal ??= CheckPrincipalTenant(principalTenant, scopeOptions.TenantId, requestedKind);
        refusal ??= CheckChain(requestedKind, scopeOptions.AllowOverInbound, out overInbound);

        return refusal is not null
            ? Refuse<T>(refusal, requestedKind)
            : OpenPrincipal(principal, principalTenant ?? scopeOptions.TenantId, overInbound, work, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, T>> RunInboundAsync<T>(
        InboundRequestInfo request,
        Func<IRequestContext, CancellationToken, Task<Either<EncinaError, T>>> work,
        CancellationToken cancellationToken = default) =>
        RunInboundCore(request, work, hostAdapter: false, cancellationToken);

    /// <inheritdoc />
    public Task<Either<EncinaError, T>> RunHostInboundAsync<T>(
        InboundRequestInfo request,
        Func<IRequestContext, CancellationToken, Task<Either<EncinaError, T>>> work,
        CancellationToken cancellationToken = default) =>
        RunInboundCore(request, work, hostAdapter: true, cancellationToken);

    private Task<Either<EncinaError, T>> RunInboundCore<T>(
        InboundRequestInfo request,
        Func<IRequestContext, CancellationToken, Task<Either<EncinaError, T>>> work,
        bool hostAdapter,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(work);

        // Client-controlled members are normalized below, never refused: the only refusals are host
        // or transport conditions.
        var requestedKind = RequestedKindOf(request.Principal);
        var refusal = Precheck(cancellationToken)
            ?? CheckChain(requestedKind, inboundOptIn: false, out _);
        if (refusal is not null)
        {
            return Refuse<T>(refusal, requestedKind);
        }

        var ambient = _accessor.RequestContext;
        var issuer = new IdentityIssuer();
        var identity = _identityFactory.Create(request.Principal, issuer);
        var tenantId = _identityFactory.ResolveTenantId(request.Principal) ?? InboundRequestNormalizer.Id(request.TenantHeaderValue);
        var context = NewContext(identity, InboundRequestNormalizer.CorrelationId(request.CorrelationId), tenantId, RequestOrigin.Inbound,
                InboundRequestNormalizer.IdempotencyKey(request.IdempotencyKey))
            .WithMetadataItems(InboundRequestNormalizer.Metadata(request));

        LogTenantChange(hostAdapter ? nameof(RunHostInboundAsync) : nameof(RunInboundAsync), ambient, context);
        if (hostAdapter)
        {
            RequestIdentityLog.InboundScopeOpened(_logger, identity.Kind, context.CorrelationId);
        }
        else
        {
            RequestIdentityLog.InboundScopeOpenedByApplication(_logger, identity.Kind, context.CorrelationId);
        }

        return Open(context, work, cancellationToken);
    }

    private Task<Either<EncinaError, T>> OpenService<T>(
        string member,
        ServiceIdentityDefinition definition,
        string? tenantId,
        bool overInbound,
        Func<IRequestContext, CancellationToken, Task<Either<EncinaError, T>>> work,
        CancellationToken cancellationToken)
    {
        var ambient = _accessor.RequestContext;
        var context = NewContext(ServiceIdentity(definition), AmbientCorrelationId(ambient), tenantId, RequestOrigin.Scope);

        LogTenantChange(member, ambient, context);
        if (overInbound)
        {
            RequestIdentityLog.ScopeOpenedOverInbound(_logger, member, IdentityKind.Service, definition.Name);
        }
        else
        {
            var level = KindOf(ambient) == IdentityKind.Service ? LogLevel.Warning : LogLevel.Information;
            RequestIdentityLog.ServiceIdentityScopeOpened(_logger, level, definition.Name, tenantId is not null, context.CorrelationId);
        }

        return Open(context, work, cancellationToken);
    }

    private Task<Either<EncinaError, T>> OpenPrincipal<T>(
        ClaimsPrincipal principal,
        string? tenantId,
        bool overInbound,
        Func<IRequestContext, CancellationToken, Task<Either<EncinaError, T>>> work,
        CancellationToken cancellationToken)
    {
        var ambient = _accessor.RequestContext;
        var identity = _identityFactory.Create(principal, new IdentityIssuer());
        var context = NewContext(identity, AmbientCorrelationId(ambient), tenantId, RequestOrigin.Scope);

        LogTenantChange(nameof(RunAsPrincipalAsync), ambient, context);
        if (overInbound)
        {
            RequestIdentityLog.ScopeOpenedOverInbound(_logger, nameof(RunAsPrincipalAsync), identity.Kind, serviceName: null);
        }
        else
        {
            var level = identity.Kind == IdentityKind.User && KindOf(ambient) == IdentityKind.Service ? LogLevel.Warning : LogLevel.Information;
            RequestIdentityLog.PrincipalScopeOpened(_logger, level, identity.Kind, context.CorrelationId);
        }

        return Open(context, work, cancellationToken);
    }

    private Task<Either<EncinaError, T>> Open<T>(
        RequestContext context,
        Func<IRequestContext, CancellationToken, Task<Either<EncinaError, T>>> work,
        CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag(ActivityTagNames.IdentityKind, EncinaDiagnostics.ToTagValue(context.Identity.Kind));
        return RunScopeAsync(context, work, cancellationToken);
    }

    // The only place a scope holder is pushed. It must stay an async method: the AsyncLocal write of
    // Push is then confined to this method's execution context, and the caller's ambient comes back
    // when the method first yields or returns. End only invalidates; it restores nothing.
    private async Task<Either<EncinaError, T>> RunScopeAsync<T>(
        RequestContext context,
        Func<IRequestContext, CancellationToken, Task<Either<EncinaError, T>>> work,
        CancellationToken cancellationToken)
    {
        var holder = RequestContextAccessor.Push(context);
        context.Identity.Issuer?.Bind(holder);
        try
        {
            return await work(context, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            EndScope(holder);
        }
    }

    private void EndScope(RequestContextAccessor.ContextHolder holder)
    {
        if (!RequestContextAccessor.End(holder))
        {
            RequestIdentityLog.IdentityScopeOutlivedParent(_logger, holder.Kind, holder.EndedAncestorKind());
        }

        RequestIdentityLog.IdentityScopeClosed(_logger, holder.Kind);
    }

    private EncinaError? Precheck(CancellationToken cancellationToken)
    {
        if (_accessor is not RequestContextAccessor)
        {
            return RequestIdentityErrors.UnsupportedAccessor();
        }

        return cancellationToken.IsCancellationRequested
            ? EncinaErrors.Create(EncinaErrorCodes.RequestCancelled, "The identity scope was not opened because the operation was cancelled.")
            : (EncinaError?)null;
    }

    private EncinaError? FindService(string name, bool builtIn, out ServiceIdentityDefinition? definition)
    {
        definition = null;
        if (!builtIn && ServiceIdentityCatalogOptionsValidator.IsReservedName(name))
        {
            return RequestIdentityErrors.ReservedServiceIdentity(name);
        }

        if (!_catalog.TryGet(name, out var found) || found.IsBuiltIn != builtIn)
        {
            return RequestIdentityErrors.UnknownServiceIdentity(name);
        }

        definition = found;
        return null;
    }

    private static EncinaError? CheckPrincipalTenant(string? principalTenant, string? requestedTenant, IdentityKind requestedKind) =>
        requestedTenant is not null && principalTenant is not null && !string.Equals(principalTenant, requestedTenant, StringComparison.Ordinal)
            ? RequestIdentityErrors.TenantConflict(requestedKind, requestedKind)
            : (EncinaError?)null;

    // The holder-chain refusals (facts of the current holder, ended holders included): a user
    // anywhere refuses every scope; an inbound request anywhere refuses unless the member allows the
    // per-call opt-in and the caller set it.
    private EncinaError? CheckChain(IdentityKind requestedKind, bool inboundOptIn, out bool overInbound)
    {
        var facts = RequestContextAccessor.CurrentFacts;
        overInbound = false;
        if (facts.HasFlag(ChainFacts.User))
        {
            return RequestIdentityErrors.ScopeConflict(IdentityKind.User, requestedKind);
        }

        if (!facts.HasFlag(ChainFacts.Inbound))
        {
            return null;
        }

        overInbound = inboundOptIn;
        return inboundOptIn ? (EncinaError?)null : RequestIdentityErrors.ScopeConflict(KindOf(_accessor.RequestContext), requestedKind);
    }

    // The callers pass a refusal they have just checked for a value.
    private Task<Either<EncinaError, T>> Refuse<T>(EncinaError? refusal, IdentityKind requestedKind)
    {
        var error = refusal.GetValueOrDefault();
        var code = error.GetCode().IfNone("unknown");
        if (!string.Equals(code, EncinaErrorCodes.RequestCancelled, StringComparison.Ordinal))
        {
            RequestIdentityLog.IdentityScopeRefused(_logger, code, requestedKind);
        }

        return Task.FromResult(Left<EncinaError, T>(error));
    }

    private RequestIdentity ServiceIdentity(ServiceIdentityDefinition definition) =>
        RequestIdentity.ForService(
            definition,
            new IdentityIssuer(),
            _perTokenClaimTypes,
            _options.RoleClaimTypes[0],
            _options.PermissionClaimTypes[0]);

    private RequestContext NewContext(
        RequestIdentity identity,
        string correlationId,
        string? tenantId,
        RequestOrigin origin,
        string? idempotencyKey = null) =>
        RequestContext.CreateAt(_timeProvider.GetUtcNow(), correlationId, identity, tenantId, idempotencyKey).WithOrigin(origin);

    private void LogTenantChange(string member, IRequestContext? ambient, RequestContext context)
    {
        if (ambient?.TenantId is { } ambientTenant && !string.Equals(ambientTenant, context.TenantId, StringComparison.Ordinal))
        {
            RequestIdentityLog.ScopeTenantChanged(_logger, member, context.Identity.Kind);
        }
    }

    // The ambient correlation id when usable, otherwise the current activity id (bounded) or a new GUID.
    private static string AmbientCorrelationId(IRequestContext? ambient) =>
        InboundRequestNormalizer.CorrelationId(ambient?.CorrelationId);

    private static IdentityKind KindOf(IRequestContext? context) => context?.Identity?.Kind ?? IdentityKind.Anonymous;

    private static IdentityKind RequestedKindOf(ClaimsPrincipal? principal) =>
        principal?.Identities.Any(static identity => identity.IsAuthenticated) == true ? IdentityKind.User : IdentityKind.Anonymous;
}

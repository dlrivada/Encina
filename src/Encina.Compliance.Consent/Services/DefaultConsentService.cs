using Encina.Caching;
using Encina.Compliance.Consent.Abstractions;
using Encina.Compliance.Consent.Aggregates;
using Encina.Compliance.Consent.Diagnostics;
using Encina.Compliance.Consent.ReadModels;
using Encina.Marten;
using Encina.Marten.Projections;
using Encina.Tenancy;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.Consent.Services;

/// <summary>
/// Default implementation of <see cref="IConsentService"/> that manages consent lifecycle
/// operations via event-sourced aggregates.
/// </summary>
/// <remarks>
/// <para>
/// Wraps <see cref="IAggregateRepository{TAggregate}"/> for <see cref="ConsentAggregate"/> and
/// <see cref="IReadModelRepository{TReadModel}"/> for <see cref="ConsentReadModel"/> to provide
/// a clean CQRS API for managing consent. All write operations go through the aggregate
/// (command side), while read operations use the projected read model (query side).
/// </para>
/// <para>
/// Cache key patterns:
/// <list type="bullet">
///   <item><description><c>"consent:{id}"</c> — Individual consent lookup by ID</description></item>
///   <item><description><c>"consent:tenant:{tenantId}:subject:{subjectId}:purpose:{purpose}"</c> —
///   lookup by subject + purpose, keyed under the ambient tenant (or the literal <c>"-"</c> when
///   no tenant is present) so that a cached read model from one tenant is never served to
///   another (#1315)</description></item>
/// </list>
/// Cache invalidation is fire-and-forget — cache misses are acceptable.
/// </para>
/// <para>
/// <b>Tenant isolation (#1315):</b> Every query reads the ambient tenant from
/// <see cref="IRequestContextAccessor.RequestContext"/> and filters
/// <see cref="ConsentReadModel.TenantId"/> against it, so a request scoped to one tenant can never
/// observe another tenant's consent record. This uses an explicit query predicate rather than
/// Marten's own conjoined tenancy, consistent with the rest of the Encina.Compliance.* modules,
/// none of which currently use conjoined tenancy either. When
/// <see cref="ConsentOptions.RequireTenantContext"/> controls whether a missing ambient tenant
/// fails closed with <see cref="ConsentErrors.TenantRequiredCode"/> instead of running an unscoped
/// query. Its default (<c>null</c>) auto-detects: it fails closed when <c>Encina.Tenancy</c> is
/// registered (<see cref="ITenantProvider"/> resolvable) — a multi-tenant application — and allows
/// the unscoped query when it is not, so a single-tenant application, which never populates
/// <see cref="IRequestContext.TenantId"/> and never registers <c>Encina.Tenancy</c>, keeps working
/// without configuration (SPEC-002 DEC-006, DEC-009). An explicit <c>false</c> is an opt-out that
/// is logged once so it is never silent.
/// </para>
/// </remarks>
internal sealed class DefaultConsentService : IConsentService
{
    private readonly IAggregateRepository<ConsentAggregate> _repository;
    private readonly IReadModelRepository<ConsentReadModel> _readModelRepository;
    private readonly ICacheProvider _cache;
    private readonly TimeProvider _timeProvider;
    private readonly IRequestContextAccessor _requestContextAccessor;
    private readonly ConsentOptions _options;
    private readonly bool _isMultiTenantApplication;
    private readonly ILogger<DefaultConsentService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultConsentService"/>.
    /// </summary>
    /// <param name="repository">The aggregate repository for consent aggregates.</param>
    /// <param name="readModelRepository">The read model repository for consent projections.</param>
    /// <param name="cache">The cache provider for read model caching.</param>
    /// <param name="timeProvider">The time provider for UTC timestamps.</param>
    /// <param name="requestContextAccessor">Provides the ambient tenant for query scoping (#1315).</param>
    /// <param name="options">Consent configuration options, including tenant enforcement.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="tenantProvider">
    /// Present only when <c>Encina.Tenancy</c> is registered in the application. Used solely to
    /// detect a multi-tenant application for <see cref="ConsentOptions.RequireTenantContext"/>
    /// auto-detection (#1315); consent queries scope by the ambient <see cref="IRequestContext"/>
    /// directly, never through this provider.
    /// </param>
    public DefaultConsentService(
        IAggregateRepository<ConsentAggregate> repository,
        IReadModelRepository<ConsentReadModel> readModelRepository,
        ICacheProvider cache,
        TimeProvider timeProvider,
        IRequestContextAccessor requestContextAccessor,
        IOptions<ConsentOptions> options,
        ILogger<DefaultConsentService> logger,
        ITenantProvider? tenantProvider = null)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(readModelRepository);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(requestContextAccessor);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _repository = repository;
        _readModelRepository = readModelRepository;
        _cache = cache;
        _timeProvider = timeProvider;
        _requestContextAccessor = requestContextAccessor;
        _options = options.Value;
        _logger = logger;
        _isMultiTenantApplication = tenantProvider is not null;

        // An explicit opt-out (RequireTenantContext == false) while the application is
        // multi-tenant is exactly the case SPEC-002 DEC-006 requires to be logged, never silent.
        if (_options.RequireTenantContext == false && _isMultiTenantApplication)
        {
            _logger.ConsentTenantEnforcementOptedOut();
        }
    }

    /// <summary>
    /// Reads the ambient tenant id from the request context, or <c>null</c> when no tenant is
    /// present.
    /// </summary>
    private string? CurrentTenantId => _requestContextAccessor.RequestContext?.TenantId;

    /// <summary>
    /// Resolves whether a missing ambient tenant should fail a query closed: the explicit
    /// <see cref="ConsentOptions.RequireTenantContext"/> value when set, otherwise auto-detected
    /// from whether the application registered <c>Encina.Tenancy</c> (#1315, SPEC-002 DEC-006/DEC-009).
    /// </summary>
    private bool IsTenantContextRequired => _options.RequireTenantContext ?? _isMultiTenantApplication;

    // ========================================================================
    // Command operations
    // ========================================================================

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Guid>> GrantConsentAsync(
        string dataSubjectId,
        string purpose,
        string consentVersionId,
        string source,
        string grantedBy,
        string? ipAddress = null,
        string? proofOfConsent = null,
        IReadOnlyDictionary<string, object?>? metadata = null,
        DateTimeOffset? expiresAtUtc = null,
        string? tenantId = null,
        string? moduleId = null,
        CancellationToken cancellationToken = default)
    {
        // Neither the data subject's own identifier nor the actor (which is frequently the data
        // subject itself for a self-service grant) is logged; correlate via purpose only (#1314).
        _logger.LogDebug("Granting consent for purpose '{Purpose}'", purpose);

        if (!TryResolveWriteTenantScope("GrantConsent", tenantId, out var effectiveTenantId, out var tenantError))
        {
            return tenantError!.Value;
        }

        try
        {
            var id = Guid.NewGuid();
            var occurredAtUtc = _timeProvider.GetUtcNow();
            var effectiveMetadata = metadata ?? new Dictionary<string, object?>();

            var aggregate = ConsentAggregate.Grant(
                id, dataSubjectId, purpose, consentVersionId, source,
                ipAddress, proofOfConsent, effectiveMetadata, expiresAtUtc,
                grantedBy, occurredAtUtc, effectiveTenantId, moduleId);

            var result = await _repository.CreateAsync(aggregate, cancellationToken);

            return result.Match<Either<EncinaError, Guid>>(
                Right: _ =>
                {
                    _logger.ConsentGrantedService(id.ToString(), purpose);
                    ConsentDiagnostics.ConsentGrantedTotal.Add(1);
                    return id;
                },
                Left: error => error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ConsentServiceError("GrantConsent", ex);
            return ConsentErrors.ServiceError("GrantConsent", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> WithdrawConsentAsync(
        Guid consentId,
        string withdrawnBy,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        // The actor is frequently the data subject itself for a self-service withdrawal, so it is
        // never logged; correlate via consent id only (#1314).
        _logger.LogDebug("Withdrawing consent '{ConsentId}'", consentId);

        if (!TryResolveTenantScope("WithdrawConsent", out var tenantId, out var tenantError))
        {
            return tenantError!.Value;
        }

        try
        {
            var loadResult = await _repository.LoadAsync(consentId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    // A consent loaded by id that belongs to a different tenant is reported as
                    // not found, never mutated (#1315).
                    if (!TenantsMatch(aggregate.TenantId, tenantId))
                    {
                        return ConsentErrors.ConsentNotFound(consentId);
                    }

                    var occurredAtUtc = _timeProvider.GetUtcNow();
                    aggregate.Withdraw(withdrawnBy, reason, occurredAtUtc);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.ConsentWithdrawnService(consentId.ToString());
                            ConsentDiagnostics.ConsentWithdrawnTotal.Add(1);
                            InvalidateCache(consentId, aggregate);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => ConsentErrors.ConsentNotFound(consentId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.ConsentInvalidStateTransition(consentId.ToString(), "WithdrawConsent", ex);
            return ConsentErrors.InvalidStateTransition("current", "Withdrawn");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ConsentServiceError("WithdrawConsent", ex);
            return ConsentErrors.ServiceError("WithdrawConsent", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RenewConsentAsync(
        Guid consentId,
        string consentVersionId,
        string renewedBy,
        DateTimeOffset? newExpiresAtUtc = null,
        string? source = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Renewing consent '{ConsentId}' with version '{VersionId}'", consentId, consentVersionId);

        if (!TryResolveTenantScope("RenewConsent", out var tenantId, out var tenantError))
        {
            return tenantError!.Value;
        }

        try
        {
            var loadResult = await _repository.LoadAsync(consentId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    // A consent loaded by id that belongs to a different tenant is reported as
                    // not found, never mutated (#1315).
                    if (!TenantsMatch(aggregate.TenantId, tenantId))
                    {
                        return ConsentErrors.ConsentNotFound(consentId);
                    }

                    var occurredAtUtc = _timeProvider.GetUtcNow();
                    aggregate.Renew(consentVersionId, newExpiresAtUtc, renewedBy, source, occurredAtUtc);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.ConsentRenewedService(consentId.ToString(), consentVersionId);
                            ConsentDiagnostics.ConsentRenewedTotal.Add(1);
                            InvalidateCache(consentId, aggregate);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => ConsentErrors.ConsentNotFound(consentId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.ConsentInvalidStateTransition(consentId.ToString(), "RenewConsent", ex);
            return ConsentErrors.InvalidStateTransition("current", "Active");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ConsentServiceError("RenewConsent", ex);
            return ConsentErrors.ServiceError("RenewConsent", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> ProvideReconsentAsync(
        Guid consentId,
        string newConsentVersionId,
        string source,
        string grantedBy,
        string? ipAddress = null,
        string? proofOfConsent = null,
        IReadOnlyDictionary<string, object?>? metadata = null,
        DateTimeOffset? expiresAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Providing reconsent for '{ConsentId}' with version '{VersionId}'", consentId, newConsentVersionId);

        if (!TryResolveTenantScope("ProvideReconsent", out var tenantId, out var tenantError))
        {
            return tenantError!.Value;
        }

        try
        {
            var loadResult = await _repository.LoadAsync(consentId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    // A consent loaded by id that belongs to a different tenant is reported as
                    // not found, never mutated (#1315).
                    if (!TenantsMatch(aggregate.TenantId, tenantId))
                    {
                        return ConsentErrors.ConsentNotFound(consentId);
                    }

                    var occurredAtUtc = _timeProvider.GetUtcNow();
                    var effectiveMetadata = metadata ?? new Dictionary<string, object?>();

                    aggregate.ProvideReconsent(
                        newConsentVersionId, source, ipAddress, proofOfConsent,
                        effectiveMetadata, expiresAtUtc, grantedBy, occurredAtUtc);

                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.ReconsentProvidedService(consentId.ToString(), newConsentVersionId);
                            ConsentDiagnostics.ConsentReconsentTotal.Add(1);
                            InvalidateCache(consentId, aggregate);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => ConsentErrors.ConsentNotFound(consentId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.ConsentInvalidStateTransition(consentId.ToString(), "ProvideReconsent", ex);
            return ConsentErrors.InvalidStateTransition("current", "Active");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ConsentServiceError("ProvideReconsent", ex);
            return ConsentErrors.ServiceError("ProvideReconsent", ex);
        }
    }

    // ========================================================================
    // Query operations
    // ========================================================================

    /// <summary>
    /// Resolves the ambient tenant for a query, failing closed when tenant isolation is required
    /// (<see cref="ConsentOptions.RequireTenantContext"/>) and no tenant is present (#1315,
    /// SPEC-002 DEC-006).
    /// </summary>
    private bool TryResolveTenantScope(string operation, out string? tenantId, out EncinaError? error)
    {
        tenantId = CurrentTenantId;

        if (string.IsNullOrEmpty(tenantId) && IsTenantContextRequired)
        {
            _logger.ConsentTenantContextMissing(operation);
            error = ConsentErrors.TenantRequired(operation);
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Resolves the tenant a write should be recorded under, combining an optional explicit
    /// <paramref name="explicitTenantId"/> with the ambient tenant (#1315): defaults to the
    /// ambient tenant when the caller does not pass one explicitly, so a write made under an
    /// ambient tenant is never orphaned outside every tenant-scoped read; fails closed when
    /// tenant isolation is required and neither is present; and rejects an explicit tenant that
    /// does not match the ambient one, so a request scoped to one tenant can never write into
    /// another tenant's data.
    /// </summary>
    private bool TryResolveWriteTenantScope(
        string operation, string? explicitTenantId, out string? tenantId, out EncinaError? error)
    {
        var ambientTenantId = CurrentTenantId;

        if (explicitTenantId is not null
            && !string.IsNullOrEmpty(ambientTenantId)
            && !TenantsMatch(explicitTenantId, ambientTenantId))
        {
            _logger.ConsentTenantContextMissing(operation);
            tenantId = null;
            error = ConsentErrors.TenantRequired(operation);
            return false;
        }

        tenantId = explicitTenantId ?? ambientTenantId;

        if (string.IsNullOrEmpty(tenantId) && IsTenantContextRequired)
        {
            _logger.ConsentTenantContextMissing(operation);
            error = ConsentErrors.TenantRequired(operation);
            return false;
        }

        error = null;
        return true;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, ConsentReadModel>> GetConsentAsync(
        Guid consentId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting consent '{ConsentId}'", consentId);

        if (!TryResolveTenantScope("GetConsent", out var tenantId, out var tenantError))
        {
            return tenantError!.Value;
        }

        var cacheKey = $"consent:{consentId}";

        try
        {
            var cached = await _cache.GetAsync<ConsentReadModel>(cacheKey, cancellationToken);
            // A tenant-mismatched cache entry is treated as a miss and re-fetched from the
            // read model, which applies the tenant filter below (#1315).
            if (cached is not null && TenantsMatch(cached.TenantId, tenantId))
            {
                _logger.ConsentCacheHit(cacheKey, "Consent");
                return cached;
            }

            var result = await _readModelRepository.GetByIdAsync(consentId, cancellationToken);

            return await result.MatchAsync<Either<EncinaError, ConsentReadModel>>(
                RightAsync: async readModel =>
                {
                    // A consent read by id that belongs to a different tenant is reported as
                    // not found, never returned (#1315).
                    if (!TenantsMatch(readModel.TenantId, tenantId))
                    {
                        return ConsentErrors.ConsentNotFound(consentId);
                    }

                    await _cache.SetAsync(cacheKey, readModel, TimeSpan.FromMinutes(5), cancellationToken);
                    return readModel;
                },
                Left: _ => ConsentErrors.ConsentNotFound(consentId));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ConsentServiceError("GetConsent", ex);
            return ConsentErrors.ServiceError("GetConsent", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<ConsentReadModel>>> GetConsentBySubjectAndPurposeAsync(
        string dataSubjectId,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting consent for purpose '{Purpose}'", purpose);

        if (!TryResolveTenantScope("GetConsentBySubjectAndPurpose", out var tenantId, out var tenantError))
        {
            return tenantError!.Value;
        }

        // The cache key is scoped by tenant so a cached read model from one tenant is never
        // served to another (#1315).
        var cacheKey = $"consent:tenant:{TenantCacheSegment(tenantId)}:subject:{dataSubjectId}:purpose:{purpose}";

        try
        {
            var cached = await _cache.GetAsync<ConsentReadModel>(cacheKey, cancellationToken);
            // Re-checked defensively even though the cache key already encodes the tenant, so a
            // hit can never surface a different tenant's record (#1315).
            if (cached is not null && TenantsMatch(cached.TenantId, tenantId))
            {
                _logger.ConsentCacheHit("consent:subject:purpose", "Consent");
                return Option<ConsentReadModel>.Some(cached);
            }

            var result = await _readModelRepository.QueryAsync(
                q => q.Where(c => c.DataSubjectId == dataSubjectId && c.Purpose == purpose && c.TenantId == tenantId),
                cancellationToken);

            return await result.MatchAsync<Either<EncinaError, Option<ConsentReadModel>>>(
                RightAsync: async readModels =>
                {
                    if (readModels.Count > 0)
                    {
                        var match = readModels[0];
                        await _cache.SetAsync(cacheKey, match, TimeSpan.FromMinutes(5), cancellationToken);
                        return Option<ConsentReadModel>.Some(match);
                    }

                    return Option<ConsentReadModel>.None;
                },
                Left: error => error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ConsentServiceError("GetConsentBySubjectAndPurpose", ex);
            return ConsentErrors.ServiceError("GetConsentBySubjectAndPurpose", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<ConsentReadModel>>> GetAllConsentsAsync(
        string dataSubjectId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all consents for subject.");

        if (!TryResolveTenantScope("GetAllConsents", out var tenantId, out var tenantError))
        {
            return tenantError!.Value;
        }

        try
        {
            return await _readModelRepository.QueryAsync(
                q => q.Where(c => c.DataSubjectId == dataSubjectId && c.TenantId == tenantId),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ConsentServiceError("GetAllConsents", ex);
            return ConsentErrors.ServiceError("GetAllConsents", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, bool>> HasValidConsentAsync(
        string dataSubjectId,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking valid consent for purpose '{Purpose}'", purpose);

        if (!TryResolveTenantScope("HasValidConsent", out var tenantId, out var tenantError))
        {
            return tenantError!.Value;
        }

        try
        {
            var now = _timeProvider.GetUtcNow();

            var result = await _readModelRepository.QueryAsync(
                q => q.Where(c =>
                    c.DataSubjectId == dataSubjectId
                    && c.Purpose == purpose
                    && c.TenantId == tenantId
                    && c.Status == ConsentStatus.Active),
                cancellationToken);

            return result.Match<Either<EncinaError, bool>>(
                Right: readModels =>
                {
                    if (readModels.Count == 0)
                    {
                        return false;
                    }

                    var match = readModels[0];

                    // Check runtime expiration
                    if (match.ExpiresAtUtc.HasValue && now >= match.ExpiresAtUtc.Value)
                    {
                        return false;
                    }

                    return true;
                },
                Left: error => error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ConsentServiceError("HasValidConsent", ex);
            return ConsentErrors.ServiceError("HasValidConsent", ex);
        }
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<object>>> GetConsentHistoryAsync(
        Guid consentId,
        CancellationToken cancellationToken = default)
    {
        // Event history retrieval requires direct Marten event stream access,
        // which is not available through the generic IAggregateRepository.
        // This will be implemented when Marten-specific integration is configured (Phase 4+).
        _logger.LogDebug("Event history requested for consent '{ConsentId}' (not yet available)", consentId);
        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<object>>>(
            ConsentErrors.EventHistoryUnavailable(consentId));
    }

    // ========================================================================
    // Private helpers
    // ========================================================================

    private void InvalidateCache(Guid consentId, ConsentAggregate aggregate)
    {
        var subjectPurposeKey =
            $"consent:tenant:{TenantCacheSegment(aggregate.TenantId)}:subject:{aggregate.DataSubjectId}:purpose:{aggregate.Purpose}";

        // Fire-and-forget cache invalidation — cache misses are acceptable
        _ = _cache.RemoveAsync($"consent:{consentId}", CancellationToken.None);
        _ = _cache.RemoveAsync(subjectPurposeKey, CancellationToken.None);
    }

    /// <summary>
    /// Normalizes a tenant id for use in a cache key segment: the literal <c>"-"</c> when no
    /// tenant is present (whether <c>null</c> or empty), so cache keys are stable regardless of
    /// how the absence of a tenant is represented (#1315).
    /// </summary>
    private static string TenantCacheSegment(string? tenantId) =>
        string.IsNullOrEmpty(tenantId) ? "-" : tenantId;

    /// <summary>
    /// Compares two tenant ids treating <c>null</c> and empty string as the same "no tenant"
    /// value, so a read model whose tenant was never populated is not spuriously rejected against
    /// an ambient ("no tenant") context represented differently (#1315).
    /// </summary>
    private static bool TenantsMatch(string? left, string? right) =>
        string.Equals(TenantCacheSegment(left), TenantCacheSegment(right), StringComparison.Ordinal);
}

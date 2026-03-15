using Encina.Caching;
using Encina.Compliance.Consent.Abstractions;
using Encina.Compliance.Consent.Aggregates;
using Encina.Compliance.Consent.Diagnostics;
using Encina.Compliance.Consent.ReadModels;
using Encina.Marten;
using Encina.Marten.Projections;
using LanguageExt;
using Microsoft.Extensions.Logging;

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
///   <item><description><c>"consent:subject:{subjectId}:purpose:{purpose}"</c> — Lookup by subject + purpose</description></item>
/// </list>
/// Cache invalidation is fire-and-forget — cache misses are acceptable.
/// </para>
/// </remarks>
internal sealed class DefaultConsentService : IConsentService
{
    private readonly IAggregateRepository<ConsentAggregate> _repository;
    private readonly IReadModelRepository<ConsentReadModel> _readModelRepository;
    private readonly ICacheProvider _cache;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DefaultConsentService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultConsentService"/>.
    /// </summary>
    /// <param name="repository">The aggregate repository for consent aggregates.</param>
    /// <param name="readModelRepository">The read model repository for consent projections.</param>
    /// <param name="cache">The cache provider for read model caching.</param>
    /// <param name="timeProvider">The time provider for UTC timestamps.</param>
    /// <param name="logger">The logger instance.</param>
    public DefaultConsentService(
        IAggregateRepository<ConsentAggregate> repository,
        IReadModelRepository<ConsentReadModel> readModelRepository,
        ICacheProvider cache,
        TimeProvider timeProvider,
        ILogger<DefaultConsentService> logger)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(readModelRepository);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _repository = repository;
        _readModelRepository = readModelRepository;
        _cache = cache;
        _timeProvider = timeProvider;
        _logger = logger;
    }

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
        _logger.LogDebug(
            "Granting consent for subject '{SubjectId}', purpose '{Purpose}', by '{GrantedBy}'",
            dataSubjectId, purpose, grantedBy);

        try
        {
            var id = Guid.NewGuid();
            var occurredAtUtc = _timeProvider.GetUtcNow();
            var effectiveMetadata = metadata ?? new Dictionary<string, object?>();

            var aggregate = ConsentAggregate.Grant(
                id, dataSubjectId, purpose, consentVersionId, source,
                ipAddress, proofOfConsent, effectiveMetadata, expiresAtUtc,
                grantedBy, occurredAtUtc, tenantId, moduleId);

            var result = await _repository.CreateAsync(aggregate, cancellationToken);

            return result.Match<Either<EncinaError, Guid>>(
                Right: _ =>
                {
                    _logger.ConsentGrantedService(id.ToString(), dataSubjectId, purpose);
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
        _logger.LogDebug("Withdrawing consent '{ConsentId}' by '{WithdrawnBy}'", consentId, withdrawnBy);

        try
        {
            var loadResult = await _repository.LoadAsync(consentId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
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

        try
        {
            var loadResult = await _repository.LoadAsync(consentId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
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

        try
        {
            var loadResult = await _repository.LoadAsync(consentId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
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

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, ConsentReadModel>> GetConsentAsync(
        Guid consentId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting consent '{ConsentId}'", consentId);

        var cacheKey = $"consent:{consentId}";

        try
        {
            var cached = await _cache.GetAsync<ConsentReadModel>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                _logger.ConsentCacheHit(cacheKey, "Consent");
                return cached;
            }

            var result = await _readModelRepository.GetByIdAsync(consentId, cancellationToken);

            return await result.MatchAsync<Either<EncinaError, ConsentReadModel>>(
                RightAsync: async readModel =>
                {
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
        _logger.LogDebug("Getting consent for subject '{SubjectId}', purpose '{Purpose}'", dataSubjectId, purpose);

        var cacheKey = $"consent:subject:{dataSubjectId}:purpose:{purpose}";

        try
        {
            var cached = await _cache.GetAsync<ConsentReadModel>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                _logger.ConsentCacheHit(cacheKey, "Consent");
                return Option<ConsentReadModel>.Some(cached);
            }

            var result = await _readModelRepository.QueryAsync(
                q => q.Where(c => c.DataSubjectId == dataSubjectId && c.Purpose == purpose),
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
        _logger.LogDebug("Getting all consents for subject '{SubjectId}'", dataSubjectId);

        try
        {
            return await _readModelRepository.QueryAsync(
                q => q.Where(c => c.DataSubjectId == dataSubjectId),
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
        _logger.LogDebug("Checking valid consent for subject '{SubjectId}', purpose '{Purpose}'", dataSubjectId, purpose);

        try
        {
            var now = _timeProvider.GetUtcNow();

            var result = await _readModelRepository.QueryAsync(
                q => q.Where(c =>
                    c.DataSubjectId == dataSubjectId
                    && c.Purpose == purpose
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
        var subjectPurposeKey = $"consent:subject:{aggregate.DataSubjectId}:purpose:{aggregate.Purpose}";

        // Fire-and-forget cache invalidation — cache misses are acceptable
        _ = _cache.RemoveAsync($"consent:{consentId}", CancellationToken.None);
        _ = _cache.RemoveAsync(subjectPurposeKey, CancellationToken.None);
    }
}

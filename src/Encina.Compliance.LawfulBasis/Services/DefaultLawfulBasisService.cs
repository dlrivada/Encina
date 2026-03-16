using Encina.Caching;
using Encina.Compliance.GDPR;
using Encina.Compliance.LawfulBasis.Abstractions;
using Encina.Compliance.LawfulBasis.Aggregates;
using Encina.Compliance.LawfulBasis.Errors;
using Encina.Compliance.LawfulBasis.ReadModels;
using Encina.Marten;
using Encina.Marten.Projections;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Compliance.LawfulBasis.Services;

/// <summary>
/// Default implementation of <see cref="ILawfulBasisService"/> that manages lawful basis
/// registration and LIA lifecycle operations via event-sourced aggregates.
/// </summary>
/// <remarks>
/// <para>
/// Wraps <see cref="IAggregateRepository{TAggregate}"/> for both <see cref="LawfulBasisAggregate"/>
/// and <see cref="LIAAggregate"/> to provide a clean API for registration, assessment, and
/// querying of GDPR Article 6(1) lawful basis determinations.
/// </para>
/// <para>
/// Query operations use <see cref="IReadModelRepository{TReadModel}"/> with a cache-aside pattern
/// via <see cref="ICacheProvider"/>. Write operations invalidate cached entries fire-and-forget.
/// </para>
/// <para>
/// Cache key patterns:
/// <list type="bullet">
///   <item><c>"lb:reg:{id}"</c> — Individual registration lookup</item>
///   <item><c>"lb:reg:type:{requestTypeName}"</c> — Request type name lookup</item>
///   <item><c>"lb:lia:{id}"</c> — Individual LIA lookup</item>
///   <item><c>"lb:lia:ref:{reference}"</c> — LIA reference lookup</item>
/// </list>
/// </para>
/// </remarks>
internal sealed class DefaultLawfulBasisService : ILawfulBasisService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly IAggregateRepository<LawfulBasisAggregate> _registrationRepository;
    private readonly IAggregateRepository<LIAAggregate> _liaRepository;
    private readonly IReadModelRepository<LawfulBasisReadModel> _registrationReadModels;
    private readonly IReadModelRepository<LIAReadModel> _liaReadModels;
    private readonly ICacheProvider _cache;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DefaultLawfulBasisService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultLawfulBasisService"/>.
    /// </summary>
    /// <param name="registrationRepository">The aggregate repository for lawful basis aggregates.</param>
    /// <param name="liaRepository">The aggregate repository for LIA aggregates.</param>
    /// <param name="registrationReadModels">The read model repository for registration projections.</param>
    /// <param name="liaReadModels">The read model repository for LIA projections.</param>
    /// <param name="cache">The cache provider for read model caching.</param>
    /// <param name="timeProvider">The time provider for UTC timestamps.</param>
    /// <param name="logger">The logger instance.</param>
    public DefaultLawfulBasisService(
        IAggregateRepository<LawfulBasisAggregate> registrationRepository,
        IAggregateRepository<LIAAggregate> liaRepository,
        IReadModelRepository<LawfulBasisReadModel> registrationReadModels,
        IReadModelRepository<LIAReadModel> liaReadModels,
        ICacheProvider cache,
        TimeProvider timeProvider,
        ILogger<DefaultLawfulBasisService> logger)
    {
        ArgumentNullException.ThrowIfNull(registrationRepository);
        ArgumentNullException.ThrowIfNull(liaRepository);
        ArgumentNullException.ThrowIfNull(registrationReadModels);
        ArgumentNullException.ThrowIfNull(liaReadModels);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _registrationRepository = registrationRepository;
        _liaRepository = liaRepository;
        _registrationReadModels = registrationReadModels;
        _liaReadModels = liaReadModels;
        _cache = cache;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    // ========================================================================
    // Registration Commands
    // ========================================================================

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Guid>> RegisterAsync(
        Guid id,
        string requestTypeName,
        GDPR.LawfulBasis basis,
        string? purpose,
        string? liaReference,
        string? legalReference,
        string? contractReference,
        string? tenantId = null,
        string? moduleId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Registering lawful basis for request type '{RequestTypeName}' with basis {Basis}",
            requestTypeName, basis);

        try
        {
            var nowUtc = _timeProvider.GetUtcNow();
            var aggregate = LawfulBasisAggregate.Register(
                id, requestTypeName, basis, purpose,
                liaReference, legalReference, contractReference,
                nowUtc, tenantId, moduleId);

            var result = await _registrationRepository.CreateAsync(aggregate, cancellationToken);

            return result.Match<Either<EncinaError, Guid>>(
                Right: _ =>
                {
                    _logger.LogInformation(
                        "Lawful basis registered. RegistrationId={RegistrationId}, RequestType={RequestType}, Basis={Basis}",
                        id, requestTypeName, basis);
                    InvalidateRegistrationCache(id, requestTypeName, cancellationToken);
                    return id;
                },
                Left: error => error);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument during RegisterAsync: {Message}", ex.Message);
            return LawfulBasisErrors.InvalidStateTransition("Register", ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Store operation failed during RegisterAsync");
            return LawfulBasisErrors.StoreError("RegisterAsync", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> ChangeBasisAsync(
        Guid registrationId,
        GDPR.LawfulBasis newBasis,
        string? purpose,
        string? liaReference,
        string? legalReference,
        string? contractReference,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Changing lawful basis for registration '{RegistrationId}' to {NewBasis}",
            registrationId, newBasis);

        try
        {
            var loadResult = await _registrationRepository.LoadAsync(registrationId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var nowUtc = _timeProvider.GetUtcNow();
                    aggregate.ChangeBasis(newBasis, purpose, liaReference, legalReference, contractReference, nowUtc);
                    var saveResult = await _registrationRepository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.LogInformation(
                                "Lawful basis changed. RegistrationId={RegistrationId}, NewBasis={NewBasis}",
                                registrationId, newBasis);
                            InvalidateRegistrationCache(registrationId, aggregate.RequestTypeName, cancellationToken);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => LawfulBasisErrors.RegistrationNotFound(registrationId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid state transition during ChangeBasisAsync for '{RegistrationId}'", registrationId);
            return LawfulBasisErrors.RegistrationAlreadyRevoked(registrationId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Store operation failed during ChangeBasisAsync");
            return LawfulBasisErrors.StoreError("ChangeBasisAsync", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RevokeAsync(
        Guid registrationId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Revoking lawful basis registration '{RegistrationId}'", registrationId);

        try
        {
            var loadResult = await _registrationRepository.LoadAsync(registrationId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var nowUtc = _timeProvider.GetUtcNow();
                    aggregate.Revoke(reason, nowUtc);
                    var saveResult = await _registrationRepository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.LogInformation(
                                "Lawful basis revoked. RegistrationId={RegistrationId}, Reason={Reason}",
                                registrationId, reason);
                            InvalidateRegistrationCache(registrationId, aggregate.RequestTypeName, cancellationToken);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => LawfulBasisErrors.RegistrationNotFound(registrationId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid state transition during RevokeAsync for '{RegistrationId}'", registrationId);
            return LawfulBasisErrors.RegistrationAlreadyRevoked(registrationId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Store operation failed during RevokeAsync");
            return LawfulBasisErrors.StoreError("RevokeAsync", ex);
        }
    }

    // ========================================================================
    // LIA Commands
    // ========================================================================

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Guid>> CreateLIAAsync(
        Guid id,
        string reference,
        string name,
        string purpose,
        string legitimateInterest,
        string benefits,
        string consequencesIfNotProcessed,
        string necessityJustification,
        IReadOnlyList<string> alternativesConsidered,
        string dataMinimisationNotes,
        string natureOfData,
        string reasonableExpectations,
        string impactAssessment,
        IReadOnlyList<string> safeguards,
        string assessedBy,
        bool dpoInvolvement,
        string? conditions = null,
        string? tenantId = null,
        string? moduleId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating LIA with reference '{Reference}'", reference);

        try
        {
            var nowUtc = _timeProvider.GetUtcNow();
            var aggregate = LIAAggregate.Create(
                id, reference, name, purpose,
                legitimateInterest, benefits, consequencesIfNotProcessed,
                necessityJustification, alternativesConsidered, dataMinimisationNotes,
                natureOfData, reasonableExpectations, impactAssessment, safeguards,
                assessedBy, dpoInvolvement, nowUtc,
                conditions, tenantId, moduleId);

            var result = await _liaRepository.CreateAsync(aggregate, cancellationToken);

            return result.Match<Either<EncinaError, Guid>>(
                Right: _ =>
                {
                    _logger.LogInformation(
                        "LIA created. LIAId={LIAId}, Reference={Reference}, Name={Name}",
                        id, reference, name);
                    InvalidateLIACache(id, reference, cancellationToken);
                    return id;
                },
                Left: error => error);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument during CreateLIAAsync: {Message}", ex.Message);
            return LawfulBasisErrors.InvalidStateTransition("CreateLIA", ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Store operation failed during CreateLIAAsync");
            return LawfulBasisErrors.StoreError("CreateLIAAsync", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> ApproveLIAAsync(
        Guid liaId,
        string conclusion,
        string approvedBy,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Approving LIA '{LIAId}' by '{ApprovedBy}'", liaId, approvedBy);

        try
        {
            var loadResult = await _liaRepository.LoadAsync(liaId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var nowUtc = _timeProvider.GetUtcNow();
                    aggregate.Approve(conclusion, approvedBy, nowUtc);
                    var saveResult = await _liaRepository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.LogInformation(
                                "LIA approved. LIAId={LIAId}, ApprovedBy={ApprovedBy}",
                                liaId, approvedBy);
                            InvalidateLIACache(liaId, aggregate.Reference, cancellationToken);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => LawfulBasisErrors.LIANotFound(liaId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid state transition during ApproveLIAAsync for '{LIAId}'", liaId);
            return LawfulBasisErrors.LIAAlreadyDecided(liaId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Store operation failed during ApproveLIAAsync");
            return LawfulBasisErrors.StoreError("ApproveLIAAsync", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RejectLIAAsync(
        Guid liaId,
        string conclusion,
        string rejectedBy,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Rejecting LIA '{LIAId}' by '{RejectedBy}'", liaId, rejectedBy);

        try
        {
            var loadResult = await _liaRepository.LoadAsync(liaId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var nowUtc = _timeProvider.GetUtcNow();
                    aggregate.Reject(conclusion, rejectedBy, nowUtc);
                    var saveResult = await _liaRepository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.LogInformation(
                                "LIA rejected. LIAId={LIAId}, RejectedBy={RejectedBy}",
                                liaId, rejectedBy);
                            InvalidateLIACache(liaId, aggregate.Reference, cancellationToken);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => LawfulBasisErrors.LIANotFound(liaId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid state transition during RejectLIAAsync for '{LIAId}'", liaId);
            return LawfulBasisErrors.LIAAlreadyDecided(liaId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Store operation failed during RejectLIAAsync");
            return LawfulBasisErrors.StoreError("RejectLIAAsync", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> ScheduleLIAReviewAsync(
        Guid liaId,
        DateTimeOffset nextReviewAtUtc,
        string scheduledBy,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Scheduling LIA review for '{LIAId}' at {NextReviewAtUtc} by '{ScheduledBy}'",
            liaId, nextReviewAtUtc, scheduledBy);

        try
        {
            var loadResult = await _liaRepository.LoadAsync(liaId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var nowUtc = _timeProvider.GetUtcNow();
                    aggregate.ScheduleReview(nextReviewAtUtc, scheduledBy, nowUtc);
                    var saveResult = await _liaRepository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.LogInformation(
                                "LIA review scheduled. LIAId={LIAId}, NextReviewAtUtc={NextReviewAtUtc}",
                                liaId, nextReviewAtUtc);
                            InvalidateLIACache(liaId, aggregate.Reference, cancellationToken);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => LawfulBasisErrors.LIANotFound(liaId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid state transition during ScheduleLIAReviewAsync for '{LIAId}'", liaId);
            return LawfulBasisErrors.InvalidStateTransition("ScheduleLIAReview", ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Store operation failed during ScheduleLIAReviewAsync");
            return LawfulBasisErrors.StoreError("ScheduleLIAReviewAsync", ex);
        }
    }

    // ========================================================================
    // Queries
    // ========================================================================

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, LawfulBasisReadModel>> GetRegistrationAsync(
        Guid registrationId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"lb:reg:{registrationId}";

        try
        {
            var cached = await _cache.GetAsync<LawfulBasisReadModel>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                _logger.LogDebug("Cache hit for registration '{CacheKey}'", cacheKey);
                return cached;
            }

            var result = await _registrationReadModels.GetByIdAsync(registrationId, cancellationToken);

            return await result.MatchAsync<Either<EncinaError, LawfulBasisReadModel>>(
                RightAsync: async readModel =>
                {
                    await _cache.SetAsync(cacheKey, readModel, CacheTtl, cancellationToken);
                    return readModel;
                },
                Left: _ => LawfulBasisErrors.RegistrationNotFound(registrationId));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Store operation failed during GetRegistrationAsync");
            return LawfulBasisErrors.StoreError("GetRegistrationAsync", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<LawfulBasisReadModel>>> GetRegistrationByRequestTypeAsync(
        string requestTypeName,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"lb:reg:type:{requestTypeName}";

        try
        {
            var cached = await _cache.GetAsync<LawfulBasisReadModel>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                _logger.LogDebug("Cache hit for registration by type '{CacheKey}'", cacheKey);
                return Option<LawfulBasisReadModel>.Some(cached);
            }

            var result = await _registrationReadModels.QueryAsync(
                q => q.Where(r => r.RequestTypeName == requestTypeName && !r.IsRevoked),
                cancellationToken);

            return await result.MatchAsync<Either<EncinaError, Option<LawfulBasisReadModel>>>(
                RightAsync: async readModels =>
                {
                    if (readModels.Count > 0)
                    {
                        var match = readModels[0];
                        await _cache.SetAsync(cacheKey, match, CacheTtl, cancellationToken);
                        return Option<LawfulBasisReadModel>.Some(match);
                    }

                    return Option<LawfulBasisReadModel>.None;
                },
                Left: error => error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Store operation failed during GetRegistrationByRequestTypeAsync");
            return LawfulBasisErrors.StoreError("GetRegistrationByRequestTypeAsync", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<LawfulBasisReadModel>>> GetAllRegistrationsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _registrationReadModels.QueryAsync(
                q => q.Where(r => !r.IsRevoked),
                cancellationToken);

            return result.Match<Either<EncinaError, IReadOnlyList<LawfulBasisReadModel>>>(
                Right: readModels => Either<EncinaError, IReadOnlyList<LawfulBasisReadModel>>.Right(readModels),
                Left: error => error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Store operation failed during GetAllRegistrationsAsync");
            return LawfulBasisErrors.StoreError("GetAllRegistrationsAsync", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, LIAReadModel>> GetLIAAsync(
        Guid liaId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"lb:lia:{liaId}";

        try
        {
            var cached = await _cache.GetAsync<LIAReadModel>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                _logger.LogDebug("Cache hit for LIA '{CacheKey}'", cacheKey);
                return cached;
            }

            var result = await _liaReadModels.GetByIdAsync(liaId, cancellationToken);

            return await result.MatchAsync<Either<EncinaError, LIAReadModel>>(
                RightAsync: async readModel =>
                {
                    await _cache.SetAsync(cacheKey, readModel, CacheTtl, cancellationToken);
                    return readModel;
                },
                Left: _ => LawfulBasisErrors.LIANotFound(liaId));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Store operation failed during GetLIAAsync");
            return LawfulBasisErrors.StoreError("GetLIAAsync", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<LIAReadModel>>> GetLIAByReferenceAsync(
        string liaReference,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"lb:lia:ref:{liaReference}";

        try
        {
            var cached = await _cache.GetAsync<LIAReadModel>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                _logger.LogDebug("Cache hit for LIA by reference '{CacheKey}'", cacheKey);
                return Option<LIAReadModel>.Some(cached);
            }

            var result = await _liaReadModels.QueryAsync(
                q => q.Where(l => l.Reference == liaReference),
                cancellationToken);

            return await result.MatchAsync<Either<EncinaError, Option<LIAReadModel>>>(
                RightAsync: async readModels =>
                {
                    if (readModels.Count > 0)
                    {
                        var match = readModels[0];
                        await _cache.SetAsync(cacheKey, match, CacheTtl, cancellationToken);
                        return Option<LIAReadModel>.Some(match);
                    }

                    return Option<LIAReadModel>.None;
                },
                Left: error => error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Store operation failed during GetLIAByReferenceAsync");
            return LawfulBasisErrors.StoreError("GetLIAByReferenceAsync", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<LIAReadModel>>> GetPendingLIAReviewsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _liaReadModels.QueryAsync(
                q => q.Where(l => l.Outcome == LIAOutcome.RequiresReview),
                cancellationToken);

            return result.Match<Either<EncinaError, IReadOnlyList<LIAReadModel>>>(
                Right: readModels => Either<EncinaError, IReadOnlyList<LIAReadModel>>.Right(readModels),
                Left: error => error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Store operation failed during GetPendingLIAReviewsAsync");
            return LawfulBasisErrors.StoreError("GetPendingLIAReviewsAsync", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, bool>> HasApprovedLIAAsync(
        string liaReference,
        CancellationToken cancellationToken = default)
    {
        var result = await GetLIAByReferenceAsync(liaReference, cancellationToken);

        return result.Match<Either<EncinaError, bool>>(
            Right: option => option.Match<Either<EncinaError, bool>>(
                Some: lia => lia.Outcome == LIAOutcome.Approved,
                None: () => false),
            Left: error => error);
    }

    // ========================================================================
    // Cache Invalidation (fire-and-forget)
    // ========================================================================

    private void InvalidateRegistrationCache(Guid registrationId, string requestTypeName, CancellationToken cancellationToken)
    {
        _ = _cache.RemoveAsync($"lb:reg:{registrationId}", cancellationToken);
        _ = _cache.RemoveAsync($"lb:reg:type:{requestTypeName}", cancellationToken);
    }

    private void InvalidateLIACache(Guid liaId, string reference, CancellationToken cancellationToken)
    {
        _ = _cache.RemoveAsync($"lb:lia:{liaId}", cancellationToken);
        _ = _cache.RemoveAsync($"lb:lia:ref:{reference}", cancellationToken);
    }
}

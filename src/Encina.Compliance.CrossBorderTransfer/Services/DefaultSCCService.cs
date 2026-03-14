using Encina.Caching;
using Encina.Compliance.CrossBorderTransfer.Abstractions;
using Encina.Compliance.CrossBorderTransfer.Aggregates;
using Encina.Compliance.CrossBorderTransfer.Diagnostics;
using Encina.Compliance.CrossBorderTransfer.Errors;
using Encina.Compliance.CrossBorderTransfer.Model;
using Encina.Compliance.CrossBorderTransfer.ReadModels;
using Encina.Marten;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Compliance.CrossBorderTransfer.Services;

/// <summary>
/// Default implementation of <see cref="ISCCService"/> that manages Standard Contractual Clauses
/// agreement lifecycle operations via event-sourced aggregates.
/// </summary>
/// <remarks>
/// <para>
/// Wraps <see cref="IAggregateRepository{TAggregate}"/> for <see cref="SCCAgreementAggregate"/> to provide
/// a clean API for registering, managing, and validating SCC agreements. All write operations
/// invalidate cached read models via <see cref="ICacheProvider"/>.
/// </para>
/// <para>
/// Cache key pattern: <c>"cbt:scc:{id}"</c> for individual SCC agreement lookups.
/// </para>
/// </remarks>
internal sealed class DefaultSCCService : ISCCService
{
    private readonly IAggregateRepository<SCCAgreementAggregate> _repository;
    private readonly ICacheProvider _cache;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DefaultSCCService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultSCCService"/>.
    /// </summary>
    /// <param name="repository">The aggregate repository for SCC agreement aggregates.</param>
    /// <param name="cache">The cache provider for read model caching.</param>
    /// <param name="timeProvider">The time provider for UTC timestamps.</param>
    /// <param name="logger">The logger instance.</param>
    public DefaultSCCService(
        IAggregateRepository<SCCAgreementAggregate> repository,
        ICacheProvider cache,
        TimeProvider timeProvider,
        ILogger<DefaultSCCService> logger)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _repository = repository;
        _cache = cache;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Guid>> RegisterAgreementAsync(
        string processorId,
        SCCModule sccModule,
        string version,
        DateTimeOffset executedAtUtc,
        DateTimeOffset? expiresAtUtc = null,
        string? tenantId = null,
        string? moduleId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Registering SCC agreement for processor '{ProcessorId}', module: {Module}, version: '{Version}'",
            processorId, sccModule, version);

        try
        {
            var id = Guid.NewGuid();
            var aggregate = SCCAgreementAggregate.Register(id, processorId, sccModule, version, executedAtUtc, expiresAtUtc, tenantId, moduleId);

            var result = await _repository.CreateAsync(aggregate, cancellationToken);

            return result.Match<Either<EncinaError, Guid>>(
                Right: _ =>
                {
                    _logger.SCCAgreementRegistered(id.ToString(), processorId, sccModule.ToString());
                    CrossBorderTransferDiagnostics.SCCRegistered.Add(1);
                    return id;
                },
                Left: error => error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.SCCStoreError("RegisterAgreement", ex);
            return CrossBorderTransferErrors.StoreError("RegisterAgreement", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> AddSupplementaryMeasureAsync(
        Guid agreementId,
        SupplementaryMeasureType type,
        string description,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adding supplementary measure to SCC agreement '{AgreementId}', type: {MeasureType}", agreementId, type);

        try
        {
            var loadResult = await _repository.LoadAsync(agreementId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var measureId = Guid.NewGuid();
                    aggregate.AddSupplementaryMeasure(measureId, type, description);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: unit =>
                        {
                            _logger.SCCSupplementaryMeasureAdded(agreementId.ToString(), measureId.ToString(), type.ToString());
                            _ = _cache.RemoveAsync($"cbt:scc:{agreementId}", cancellationToken);
                            return unit;
                        },
                        Left: error => error);
                },
                Left: _ => CrossBorderTransferErrors.SCCAgreementNotFound(agreementId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot add supplementary measure to SCC agreement '{AgreementId}'", agreementId);
            return CrossBorderTransferErrors.SCCAgreementAlreadyRevoked(agreementId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.SCCStoreError("AddSupplementaryMeasure", ex);
            return CrossBorderTransferErrors.StoreError("AddSupplementaryMeasure", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RevokeAgreementAsync(
        Guid agreementId,
        string reason,
        string revokedBy,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Revoking SCC agreement '{AgreementId}' by '{RevokedBy}'", agreementId, revokedBy);

        try
        {
            var loadResult = await _repository.LoadAsync(agreementId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    aggregate.Revoke(reason, revokedBy);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: unit =>
                        {
                            _logger.SCCAgreementRevoked(agreementId.ToString(), revokedBy, reason);
                            CrossBorderTransferDiagnostics.SCCRevoked.Add(1);
                            _ = _cache.RemoveAsync($"cbt:scc:{agreementId}", cancellationToken);
                            return unit;
                        },
                        Left: error => error);
                },
                Left: _ => CrossBorderTransferErrors.SCCAgreementNotFound(agreementId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot revoke SCC agreement '{AgreementId}' — already revoked", agreementId);
            return CrossBorderTransferErrors.SCCAgreementAlreadyRevoked(agreementId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.SCCStoreError("RevokeAgreement", ex);
            return CrossBorderTransferErrors.StoreError("RevokeAgreement", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, SCCAgreementReadModel>> GetAgreementAsync(
        Guid agreementId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting SCC agreement '{AgreementId}'", agreementId);

        var cacheKey = $"cbt:scc:{agreementId}";

        try
        {
            var cached = await _cache.GetAsync<SCCAgreementReadModel>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                _logger.CacheHit(cacheKey, "SCC");
                return cached;
            }

            var loadResult = await _repository.LoadAsync(agreementId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, SCCAgreementReadModel>>(
                RightAsync: async aggregate =>
                {
                    var readModel = ProjectToReadModel(aggregate);
                    await _cache.SetAsync(cacheKey, readModel, TimeSpan.FromMinutes(5), cancellationToken);
                    return readModel;
                },
                Left: _ => CrossBorderTransferErrors.SCCAgreementNotFound(agreementId));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.SCCStoreError("GetAgreement", ex);
            return CrossBorderTransferErrors.StoreError("GetAgreement", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, SCCValidationResult>> ValidateAgreementAsync(
        string processorId,
        SCCModule sccModule,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Validating SCC agreement for processor '{ProcessorId}', module: {Module}", processorId, sccModule);

        // SCC validation by processor and module requires a projection or query view.
        // Without projection support, this method returns a "not found" validation result.
        // In production, a Marten inline projection would provide indexed queries.
        _logger.LogDebug(
            "SCC validation for processor '{ProcessorId}' requires projection support (not yet available)",
            processorId);

        return await ValueTask.FromResult<Either<EncinaError, SCCValidationResult>>(
            new SCCValidationResult
            {
                IsValid = false,
                AgreementId = null,
                Module = null,
                Version = null,
                MissingMeasures = [],
                Issues = [$"No SCC agreement found for processor '{processorId}' with module '{sccModule}'."]
            });
    }

    private static SCCAgreementReadModel ProjectToReadModel(SCCAgreementAggregate aggregate) =>
        new()
        {
            Id = aggregate.Id,
            ProcessorId = aggregate.ProcessorId,
            Module = aggregate.Module,
            Version = aggregate.SCCVersion,
            ExecutedAtUtc = aggregate.ExecutedAtUtc,
            ExpiresAtUtc = aggregate.ExpiresAtUtc,
            IsRevoked = aggregate.IsRevoked,
            RevokedAtUtc = aggregate.RevokedAtUtc,
            SupplementaryMeasures = aggregate.SupplementaryMeasures,
            TenantId = aggregate.TenantId,
            ModuleId = aggregate.ModuleId
        };
}

using Encina.Caching;
using Encina.Compliance.ProcessorAgreements.Abstractions;
using Encina.Compliance.ProcessorAgreements.Aggregates;
using Encina.Compliance.ProcessorAgreements.Diagnostics;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Compliance.ProcessorAgreements.ReadModels;
using Encina.Marten;
using Encina.Marten.Projections;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.ProcessorAgreements.Services;

/// <summary>
/// Default implementation of <see cref="IDPAService"/> that manages DPA lifecycle
/// operations via event-sourced aggregates.
/// </summary>
/// <remarks>
/// <para>
/// Wraps <see cref="IAggregateRepository{TAggregate}"/> for <see cref="DPAAggregate"/> (command side)
/// and <see cref="IReadModelRepository{TReadModel}"/> for <see cref="DPAReadModel"/> (query side)
/// to provide a clean CQRS API for managing Data Processing Agreements.
/// </para>
/// <para>
/// Cache key pattern: <c>"pa:dpa:{id}"</c> for individual DPA lookup by ID.
/// <c>"pa:dpa:active:{processorId}"</c> for active DPA lookup by processor.
/// Cache invalidation is fire-and-forget — cache misses are acceptable.
/// </para>
/// </remarks>
internal sealed class DefaultDPAService : IDPAService
{
    private static readonly TimeSpan DefaultCacheTtl = TimeSpan.FromMinutes(5);

    private readonly IAggregateRepository<DPAAggregate> _repository;
    private readonly IReadModelRepository<DPAReadModel> _readModelRepository;
    private readonly ICacheProvider _cache;
    private readonly TimeProvider _timeProvider;
    private readonly IOptions<ProcessorAgreementOptions> _options;
    private readonly ILogger<DefaultDPAService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultDPAService"/>.
    /// </summary>
    /// <param name="repository">The aggregate repository for DPA aggregates.</param>
    /// <param name="readModelRepository">The read model repository for DPA projections.</param>
    /// <param name="cache">The cache provider for read model caching.</param>
    /// <param name="timeProvider">The time provider for UTC timestamps.</param>
    /// <param name="options">The processor agreement options.</param>
    /// <param name="logger">The logger instance.</param>
    public DefaultDPAService(
        IAggregateRepository<DPAAggregate> repository,
        IReadModelRepository<DPAReadModel> readModelRepository,
        ICacheProvider cache,
        TimeProvider timeProvider,
        IOptions<ProcessorAgreementOptions> options,
        ILogger<DefaultDPAService> logger)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(readModelRepository);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _repository = repository;
        _readModelRepository = readModelRepository;
        _cache = cache;
        _timeProvider = timeProvider;
        _options = options;
        _logger = logger;
    }

    // ========================================================================
    // Command operations
    // ========================================================================

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Guid>> ExecuteDPAAsync(
        Guid processorId,
        DPAMandatoryTerms mandatoryTerms,
        bool hasSCCs,
        IReadOnlyList<string> processingPurposes,
        DateTimeOffset signedAtUtc,
        DateTimeOffset? expiresAtUtc,
        string? tenantId = null,
        string? moduleId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing DPA for processor '{ProcessorId}'", processorId);

        try
        {
            var id = Guid.NewGuid();
            var occurredAtUtc = _timeProvider.GetUtcNow();

            var aggregate = DPAAggregate.Execute(
                id, processorId, mandatoryTerms, hasSCCs, processingPurposes,
                signedAtUtc, expiresAtUtc, occurredAtUtc, tenantId, moduleId);

            var result = await _repository.CreateAsync(aggregate, cancellationToken);

            return result.Match<Either<EncinaError, Guid>>(
                Right: _ =>
                {
                    _logger.DPAAdded(id.ToString(), processorId.ToString(), DPAStatus.Active.ToString());
                    ProcessorAgreementDiagnostics.DPAOperationTotal.Add(1);
                    InvalidateActiveDPACache(processorId);
                    return id;
                },
                Left: error => error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.DPAAdditionFailed(processorId.ToString(), ex.Message);
            return ProcessorAgreementErrors.StoreError("ExecuteDPA", ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> AmendDPAAsync(
        Guid dpaId,
        DPAMandatoryTerms updatedTerms,
        bool hasSCCs,
        IReadOnlyList<string> processingPurposes,
        string amendmentReason,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Amending DPA '{DPAId}'", dpaId);

        try
        {
            var loadResult = await _repository.LoadAsync(dpaId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var occurredAtUtc = _timeProvider.GetUtcNow();
                    aggregate.Amend(updatedTerms, hasSCCs, processingPurposes, amendmentReason, occurredAtUtc);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.DPAUpdated(dpaId.ToString(), aggregate.Status.ToString());
                            ProcessorAgreementDiagnostics.DPAOperationTotal.Add(1);
                            InvalidateDPACache(dpaId, aggregate.ProcessorId);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => ProcessorAgreementErrors.DPANotFound(dpaId.ToString()));
        }
        catch (InvalidOperationException ex)
        {
            _logger.DPAUpdateFailed(dpaId.ToString(), ex.Message);
            return ProcessorAgreementErrors.ValidationFailed(dpaId.ToString(), ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.DPAUpdateFailed(dpaId.ToString(), ex.Message);
            return ProcessorAgreementErrors.StoreError("AmendDPA", ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> AuditDPAAsync(
        Guid dpaId,
        string auditorId,
        string auditFindings,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Auditing DPA '{DPAId}' by '{AuditorId}'", dpaId, auditorId);

        try
        {
            var loadResult = await _repository.LoadAsync(dpaId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var occurredAtUtc = _timeProvider.GetUtcNow();
                    aggregate.Audit(auditorId, auditFindings, occurredAtUtc);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.DPAUpdated(dpaId.ToString(), aggregate.Status.ToString());
                            ProcessorAgreementDiagnostics.DPAOperationTotal.Add(1);
                            ProcessorAgreementDiagnostics.AuditEntryTotal.Add(1);
                            InvalidateDPACache(dpaId, aggregate.ProcessorId);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => ProcessorAgreementErrors.DPANotFound(dpaId.ToString()));
        }
        catch (InvalidOperationException ex)
        {
            _logger.DPAUpdateFailed(dpaId.ToString(), ex.Message);
            return ProcessorAgreementErrors.ValidationFailed(dpaId.ToString(), ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.DPAUpdateFailed(dpaId.ToString(), ex.Message);
            return ProcessorAgreementErrors.StoreError("AuditDPA", ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RenewDPAAsync(
        Guid dpaId,
        DateTimeOffset newExpiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Renewing DPA '{DPAId}' with new expiration {ExpiresAtUtc}", dpaId, newExpiresAtUtc);

        try
        {
            var loadResult = await _repository.LoadAsync(dpaId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var occurredAtUtc = _timeProvider.GetUtcNow();
                    aggregate.Renew(newExpiresAtUtc, occurredAtUtc);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.DPAUpdated(dpaId.ToString(), DPAStatus.Active.ToString());
                            ProcessorAgreementDiagnostics.DPAOperationTotal.Add(1);
                            InvalidateDPACache(dpaId, aggregate.ProcessorId);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => ProcessorAgreementErrors.DPANotFound(dpaId.ToString()));
        }
        catch (InvalidOperationException ex)
        {
            _logger.DPAUpdateFailed(dpaId.ToString(), ex.Message);
            return ProcessorAgreementErrors.ValidationFailed(dpaId.ToString(), ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.DPAUpdateFailed(dpaId.ToString(), ex.Message);
            return ProcessorAgreementErrors.StoreError("RenewDPA", ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> TerminateDPAAsync(
        Guid dpaId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Terminating DPA '{DPAId}'", dpaId);

        try
        {
            var loadResult = await _repository.LoadAsync(dpaId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    var occurredAtUtc = _timeProvider.GetUtcNow();
                    aggregate.Terminate(reason, occurredAtUtc);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.DPAUpdated(dpaId.ToString(), DPAStatus.Terminated.ToString());
                            ProcessorAgreementDiagnostics.DPAOperationTotal.Add(1);
                            InvalidateDPACache(dpaId, aggregate.ProcessorId);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => ProcessorAgreementErrors.DPANotFound(dpaId.ToString()));
        }
        catch (InvalidOperationException ex)
        {
            _logger.DPAUpdateFailed(dpaId.ToString(), ex.Message);
            return ProcessorAgreementErrors.ValidationFailed(dpaId.ToString(), ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.DPAUpdateFailed(dpaId.ToString(), ex.Message);
            return ProcessorAgreementErrors.StoreError("TerminateDPA", ex.Message, ex);
        }
    }

    // ========================================================================
    // Query operations
    // ========================================================================

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, DPAReadModel>> GetDPAAsync(
        Guid dpaId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting DPA '{DPAId}'", dpaId);

        var cacheKey = $"pa:dpa:{dpaId}";

        try
        {
            var cached = await _cache.GetAsync<DPAReadModel>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                return cached;
            }

            var result = await _readModelRepository.GetByIdAsync(dpaId, cancellationToken);

            return await result.MatchAsync<Either<EncinaError, DPAReadModel>>(
                RightAsync: async readModel =>
                {
                    await _cache.SetAsync(cacheKey, readModel, DefaultCacheTtl, cancellationToken);
                    return readModel;
                },
                Left: _ => ProcessorAgreementErrors.DPANotFound(dpaId.ToString()));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProcessorAgreementErrors.StoreError("GetDPA", ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DPAReadModel>>> GetDPAsByProcessorIdAsync(
        Guid processorId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting DPAs for processor '{ProcessorId}'", processorId);

        try
        {
            return await _readModelRepository.QueryAsync(
                q => q.Where(d => d.ProcessorId == processorId),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProcessorAgreementErrors.StoreError("GetDPAsByProcessorId", ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, DPAReadModel>> GetActiveDPAByProcessorIdAsync(
        Guid processorId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting active DPA for processor '{ProcessorId}'", processorId);

        var cacheKey = $"pa:dpa:active:{processorId}";

        try
        {
            var cached = await _cache.GetAsync<DPAReadModel>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                _logger.ActiveDPARetrieved(processorId.ToString(), true);
                return cached;
            }

            var now = _timeProvider.GetUtcNow();
            var result = await _readModelRepository.QueryAsync(
                q => q.Where(d =>
                    d.ProcessorId == processorId
                    && d.Status == DPAStatus.Active
                    && (d.ExpiresAtUtc == null || d.ExpiresAtUtc > now)),
                cancellationToken);

            return await result.MatchAsync<Either<EncinaError, DPAReadModel>>(
                RightAsync: async dpas =>
                {
                    var activeDpa = dpas.Count > 0 ? dpas[0] : null;
                    if (activeDpa is null)
                    {
                        _logger.ActiveDPARetrieved(processorId.ToString(), false);
                        return ProcessorAgreementErrors.DPAMissing(processorId.ToString());
                    }

                    await _cache.SetAsync(cacheKey, activeDpa, DefaultCacheTtl, cancellationToken);
                    _logger.ActiveDPARetrieved(processorId.ToString(), true);
                    return activeDpa;
                },
                Left: error => error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProcessorAgreementErrors.StoreError("GetActiveDPAByProcessorId", ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DPAReadModel>>> GetDPAsByStatusAsync(
        DPAStatus status,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting DPAs by status '{Status}'", status);

        try
        {
            return await _readModelRepository.QueryAsync(
                q => q.Where(d => d.Status == status),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProcessorAgreementErrors.StoreError("GetDPAsByStatus", ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DPAReadModel>>> GetExpiringDPAsAsync(
        CancellationToken cancellationToken = default)
    {
        var warningDays = _options.Value.ExpirationWarningDays;
        var now = _timeProvider.GetUtcNow();
        var threshold = now.AddDays(warningDays);

        _logger.ExpiringDPAsRetrieved(0, threshold);

        try
        {
            var result = await _readModelRepository.QueryAsync(
                q => q.Where(d =>
                    d.Status == DPAStatus.Active
                    && d.ExpiresAtUtc != null
                    && d.ExpiresAtUtc <= threshold
                    && d.ExpiresAtUtc > now),
                cancellationToken);

            return result.Match<Either<EncinaError, IReadOnlyList<DPAReadModel>>>(
                Right: dpas =>
                {
                    _logger.ExpiringDPAsRetrieved(dpas.Count, threshold);
                    return Either<EncinaError, IReadOnlyList<DPAReadModel>>.Right(dpas);
                },
                Left: error => error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProcessorAgreementErrors.StoreError("GetExpiringDPAs", ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, bool>> HasValidDPAAsync(
        Guid processorId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking valid DPA for processor '{ProcessorId}'", processorId);

        try
        {
            var activeDpaResult = await GetActiveDPAByProcessorIdAsync(processorId, cancellationToken);

            return activeDpaResult.Match<Either<EncinaError, bool>>(
                Right: dpa => dpa.MandatoryTerms.IsFullyCompliant,
                Left: _ => false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ValidationError(processorId.ToString(), ex);
            return ProcessorAgreementErrors.StoreError("HasValidDPA", ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, DPAValidationResult>> ValidateDPAAsync(
        Guid processorId,
        CancellationToken cancellationToken = default)
    {
        _logger.ValidationStarted(processorId.ToString());

        try
        {
            var now = _timeProvider.GetUtcNow();
            var activeDpaResult = await GetActiveDPAByProcessorIdAsync(processorId, cancellationToken);

            return activeDpaResult.Match<Either<EncinaError, DPAValidationResult>>(
                Right: dpa =>
                {
                    var warnings = new List<string>();
                    int? daysUntilExpiration = null;

                    if (dpa.ExpiresAtUtc is not null)
                    {
                        var days = (int)(dpa.ExpiresAtUtc.Value - now).TotalDays;
                        daysUntilExpiration = days;

                        if (days <= _options.Value.ExpirationWarningDays)
                        {
                            warnings.Add($"Agreement expires in {days} days (threshold: {_options.Value.ExpirationWarningDays} days).");
                        }
                    }

                    if (!dpa.HasSCCs)
                    {
                        warnings.Add("Standard Contractual Clauses (SCCs) are not included. Required for non-EEA transfers per Articles 46(2)(c)-(d).");
                    }

                    var isValid = dpa.MandatoryTerms.IsFullyCompliant;
                    var missingTerms = dpa.MandatoryTerms.MissingTerms;

                    if (isValid)
                    {
                        _logger.ValidationPassed(processorId.ToString());
                    }
                    else
                    {
                        _logger.ValidationFailed(processorId.ToString(), $"Missing {missingTerms.Count} mandatory terms");
                    }

                    return new DPAValidationResult
                    {
                        ProcessorId = processorId.ToString(),
                        DPAId = dpa.Id.ToString(),
                        IsValid = isValid,
                        Status = dpa.Status,
                        MissingTerms = missingTerms,
                        Warnings = warnings,
                        DaysUntilExpiration = daysUntilExpiration,
                        ValidatedAtUtc = now
                    };
                },
                Left: _ =>
                {
                    _logger.ValidationFailed(processorId.ToString(), "No active DPA found");
                    return new DPAValidationResult
                    {
                        ProcessorId = processorId.ToString(),
                        IsValid = false,
                        MissingTerms = [],
                        Warnings = ["No active Data Processing Agreement exists for this processor per Article 28(3)."],
                        ValidatedAtUtc = now
                    };
                });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.ValidationError(processorId.ToString(), ex);
            return ProcessorAgreementErrors.StoreError("ValidateDPA", ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<object>>> GetDPAHistoryAsync(
        Guid dpaId,
        CancellationToken cancellationToken = default)
    {
        // Event history retrieval requires direct Marten event stream access,
        // which is not available through the generic IAggregateRepository.
        // This will be implemented when Marten-specific integration is configured (Phase 4+).
        _logger.LogDebug("Event history requested for DPA '{DPAId}' (not yet available)", dpaId);
        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<object>>>(
            ProcessorAgreementErrors.StoreError("GetDPAHistory", "Event history retrieval is not yet available via the generic aggregate repository."));
    }

    // ========================================================================
    // Private helpers
    // ========================================================================

    private void InvalidateDPACache(Guid dpaId, Guid processorId)
    {
        // Fire-and-forget cache invalidation — cache misses are acceptable
        _ = _cache.RemoveAsync($"pa:dpa:{dpaId}", CancellationToken.None);
        _ = _cache.RemoveAsync($"pa:dpa:active:{processorId}", CancellationToken.None);
    }

    private void InvalidateActiveDPACache(Guid processorId)
    {
        // Fire-and-forget cache invalidation — cache misses are acceptable
        _ = _cache.RemoveAsync($"pa:dpa:active:{processorId}", CancellationToken.None);
    }
}

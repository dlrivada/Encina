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
/// Default implementation of <see cref="IApprovedTransferService"/> that manages approved
/// international data transfer lifecycle operations via event-sourced aggregates.
/// </summary>
/// <remarks>
/// <para>
/// Wraps <see cref="IAggregateRepository{TAggregate}"/> for <see cref="ApprovedTransferAggregate"/>
/// to provide a clean API for approving, revoking, renewing, and querying approved transfers.
/// All write operations invalidate cached read models via <see cref="ICacheProvider"/>.
/// </para>
/// <para>
/// Cache key pattern: <c>"cbt:transfer:{id}"</c> for individual lookups,
/// <c>"cbt:transfer:route:{source}:{destination}:{dataCategory}"</c> for route-based lookups.
/// </para>
/// </remarks>
internal sealed class DefaultApprovedTransferService : IApprovedTransferService
{
    private readonly IAggregateRepository<ApprovedTransferAggregate> _repository;
    private readonly ICacheProvider _cache;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DefaultApprovedTransferService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultApprovedTransferService"/>.
    /// </summary>
    /// <param name="repository">The aggregate repository for approved transfer aggregates.</param>
    /// <param name="cache">The cache provider for read model caching.</param>
    /// <param name="timeProvider">The time provider for UTC timestamps.</param>
    /// <param name="logger">The logger instance.</param>
    public DefaultApprovedTransferService(
        IAggregateRepository<ApprovedTransferAggregate> repository,
        ICacheProvider cache,
        TimeProvider timeProvider,
        ILogger<DefaultApprovedTransferService> logger)
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
    public async ValueTask<Either<EncinaError, Guid>> ApproveTransferAsync(
        string sourceCountryCode,
        string destinationCountryCode,
        string dataCategory,
        TransferBasis basis,
        Guid? sccAgreementId = null,
        Guid? tiaId = null,
        string approvedBy = "",
        DateTimeOffset? expiresAtUtc = null,
        string? tenantId = null,
        string? moduleId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Approving transfer for route {Source} → {Destination}, category '{Category}', basis: {Basis}",
            sourceCountryCode, destinationCountryCode, dataCategory, basis);

        try
        {
            var id = Guid.NewGuid();
            var aggregate = ApprovedTransferAggregate.Approve(
                id, sourceCountryCode, destinationCountryCode, dataCategory,
                basis, sccAgreementId, tiaId, approvedBy, expiresAtUtc, tenantId, moduleId);

            var result = await _repository.CreateAsync(aggregate, cancellationToken);

            return result.Match<Either<EncinaError, Guid>>(
                Right: _ =>
                {
                    _logger.ApprovedTransferCreated(id.ToString(), sourceCountryCode, destinationCountryCode, basis.ToString());
                    CrossBorderTransferDiagnostics.TransferApproved.Add(1);
                    InvalidateRouteCache(sourceCountryCode, destinationCountryCode, dataCategory, cancellationToken);
                    return id;
                },
                Left: error => error);
        }
        catch (ArgumentException ex)
        {
            _logger.TransferStoreError("ApproveTransfer", ex);
            return CrossBorderTransferErrors.TransferBlocked(ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.TransferStoreError("ApproveTransfer", ex);
            return CrossBorderTransferErrors.StoreError("ApproveTransfer", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RevokeTransferAsync(
        Guid transferId,
        string reason,
        string revokedBy,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Revoking transfer '{TransferId}' by '{RevokedBy}'", transferId, revokedBy);

        try
        {
            var loadResult = await _repository.LoadAsync(transferId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    aggregate.Revoke(reason, revokedBy);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.ApprovedTransferRevoked(transferId.ToString(), revokedBy, reason);
                            CrossBorderTransferDiagnostics.TransferRevoked.Add(1);
                            InvalidateTransferCache(transferId, aggregate, cancellationToken);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => CrossBorderTransferErrors.TransferNotFound(transferId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.TransferStoreError("RevokeTransfer", ex);
            return CrossBorderTransferErrors.TransferAlreadyRevoked(transferId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.TransferStoreError("RevokeTransfer", ex);
            return CrossBorderTransferErrors.StoreError("RevokeTransfer", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RenewTransferAsync(
        Guid transferId,
        DateTimeOffset newExpiresAtUtc,
        string renewedBy,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Renewing transfer '{TransferId}' with new expiry {ExpiresAtUtc} by '{RenewedBy}'", transferId, newExpiresAtUtc, renewedBy);

        try
        {
            var loadResult = await _repository.LoadAsync(transferId, cancellationToken);

            return await loadResult.MatchAsync<Either<EncinaError, Unit>>(
                RightAsync: async aggregate =>
                {
                    aggregate.Renew(newExpiresAtUtc, renewedBy);
                    var saveResult = await _repository.SaveAsync(aggregate, cancellationToken);

                    return saveResult.Match<Either<EncinaError, Unit>>(
                        Right: _ =>
                        {
                            _logger.ApprovedTransferRenewed(transferId.ToString(), newExpiresAtUtc.ToString("O"), renewedBy);
                            InvalidateTransferCache(transferId, aggregate, cancellationToken);
                            return Unit.Default;
                        },
                        Left: error => error);
                },
                Left: _ => CrossBorderTransferErrors.TransferNotFound(transferId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.TransferStoreError("RenewTransfer", ex);
            return CrossBorderTransferErrors.TransferAlreadyRevoked(transferId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.TransferStoreError("RenewTransfer", ex);
            return CrossBorderTransferErrors.StoreError("RenewTransfer", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, ApprovedTransferReadModel>> GetApprovedTransferAsync(
        string sourceCountryCode,
        string destinationCountryCode,
        string dataCategory,
        CancellationToken cancellationToken = default)
    {
        var routeKey = $"{sourceCountryCode}:{destinationCountryCode}:{dataCategory}";
        var cacheKey = $"cbt:transfer:route:{routeKey}";

        _logger.LogDebug("Getting approved transfer for route {RouteKey}", routeKey);

        try
        {
            var cached = await _cache.GetAsync<ApprovedTransferReadModel>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                _logger.CacheHit(cacheKey, "ApprovedTransfer");
                return cached;
            }

            // Route-based lookup requires a projection or query view.
            // Without projection support, this method returns a not-found error.
            // In production, a Marten inline projection would provide indexed queries.
            _logger.LogDebug(
                "Approved transfer route lookup for {RouteKey} requires projection support (not yet available)",
                routeKey);
            return CrossBorderTransferErrors.TransferNotFoundByRoute(sourceCountryCode, destinationCountryCode, dataCategory);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.TransferStoreError("GetApprovedTransfer", ex);
            return CrossBorderTransferErrors.StoreError("GetApprovedTransfer", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, bool>> IsTransferApprovedAsync(
        string sourceCountryCode,
        string destinationCountryCode,
        string dataCategory,
        CancellationToken cancellationToken = default)
    {
        var result = await GetApprovedTransferAsync(sourceCountryCode, destinationCountryCode, dataCategory, cancellationToken);

        return result.Match<Either<EncinaError, bool>>(
            Right: readModel =>
            {
                var nowUtc = _timeProvider.GetUtcNow();
                return readModel.IsValid(nowUtc);
            },
            Left: error =>
            {
                // Not-found is not an infrastructure error for IsTransferApproved — it means "not approved"
                var code = error.GetCode();
                if (code.IsSome && code == CrossBorderTransferErrors.TransferNotFoundCode)
                {
                    return false;
                }

                return error;
            });
    }

    private static ApprovedTransferReadModel ProjectToReadModel(ApprovedTransferAggregate aggregate) =>
        new()
        {
            Id = aggregate.Id,
            SourceCountryCode = aggregate.SourceCountryCode,
            DestinationCountryCode = aggregate.DestinationCountryCode,
            DataCategory = aggregate.DataCategory,
            Basis = aggregate.Basis,
            SCCAgreementId = aggregate.SCCAgreementId,
            TIAId = aggregate.TIAId,
            ApprovedBy = aggregate.ApprovedBy,
            ExpiresAtUtc = aggregate.ExpiresAtUtc,
            IsRevoked = aggregate.IsRevoked,
            RevokedAtUtc = aggregate.RevokedAtUtc,
            TenantId = aggregate.TenantId,
            ModuleId = aggregate.ModuleId
        };

    private void InvalidateTransferCache(Guid transferId, ApprovedTransferAggregate aggregate, CancellationToken cancellationToken)
    {
        _ = _cache.RemoveAsync($"cbt:transfer:{transferId}", cancellationToken);
        InvalidateRouteCache(aggregate.SourceCountryCode, aggregate.DestinationCountryCode, aggregate.DataCategory, cancellationToken);
    }

    private void InvalidateRouteCache(string source, string destination, string dataCategory, CancellationToken cancellationToken)
    {
        var routeKey = $"{source}:{destination}:{dataCategory}";
        _ = _cache.RemoveAsync($"cbt:transfer:route:{routeKey}", cancellationToken);
    }
}

using Encina.Compliance.DataSubjectRights;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using static LanguageExt.Prelude;

namespace Encina.MongoDB.DataSubjectRights;

/// <summary>
/// MongoDB implementation of <see cref="IDSRRequestStore"/>.
/// </summary>
public sealed class DSRRequestStoreMongoDB : IDSRRequestStore
{
    private readonly IMongoCollection<DSRRequestDocument> _collection;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DSRRequestStoreMongoDB> _logger;

    /// <inheritdoc />
    public DSRRequestStoreMongoDB(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> options,
        ILogger<DSRRequestStoreMongoDB> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var config = options.Value;
        var database = mongoClient.GetDatabase(config.DatabaseName);
        _collection = database.GetCollection<DSRRequestDocument>(config.Collections.DSRRequests);
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> CreateAsync(
        DSRRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            var document = DSRRequestDocument.FromDomain(request);
            var filter = Builders<DSRRequestDocument>.Filter.Eq(d => d.Id, request.Id);
            await _collection.ReplaceOneAsync(filter, document,
                new ReplaceOptions { IsUpsert = true }, cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("DSR request '{RequestId}' persisted to MongoDB", request.Id);
            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(DSRErrors.StoreError("Create", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<DSRRequest>>> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        try
        {
            var filter = Builders<DSRRequestDocument>.Filter.Eq(d => d.Id, id);
            var document = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            if (document is null)
                return Right<EncinaError, Option<DSRRequest>>(None);

            var domain = document.ToDomain();
            return domain is not null
                ? Right<EncinaError, Option<DSRRequest>>(Some(domain))
                : Right<EncinaError, Option<DSRRequest>>(None);
        }
        catch (Exception ex)
        {
            return Left(DSRErrors.StoreError("GetById", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DSRRequest>>> GetBySubjectIdAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);
        try
        {
            var filter = Builders<DSRRequestDocument>.Filter.Eq(d => d.SubjectId, subjectId);
            var documents = await _collection.Find(filter).ToListAsync(cancellationToken).ConfigureAwait(false);
            return Right<EncinaError, IReadOnlyList<DSRRequest>>(MapDocuments(documents));
        }
        catch (Exception ex)
        {
            return Left(DSRErrors.StoreError("GetBySubjectId", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> UpdateStatusAsync(
        string id,
        DSRRequestStatus newStatus,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        try
        {
            var filter = Builders<DSRRequestDocument>.Filter.Eq(d => d.Id, id);
            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

            UpdateDefinition<DSRRequestDocument> update;

            switch (newStatus)
            {
                case DSRRequestStatus.Completed:
                    update = Builders<DSRRequestDocument>.Update
                        .Set(d => d.StatusValue, (int)newStatus)
                        .Set(d => d.CompletedAtUtc, nowUtc);
                    break;

                case DSRRequestStatus.Rejected:
                    update = Builders<DSRRequestDocument>.Update
                        .Set(d => d.StatusValue, (int)newStatus)
                        .Set(d => d.RejectionReason, reason)
                        .Set(d => d.CompletedAtUtc, nowUtc);
                    break;

                case DSRRequestStatus.Extended:
                    {
                        var document = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                        if (document is null)
                            return Left(DSRErrors.RequestNotFound(id));

                        var extendedDeadline = new DateTimeOffset(document.DeadlineAtUtc, TimeSpan.Zero).AddMonths(2).UtcDateTime;
                        update = Builders<DSRRequestDocument>.Update
                            .Set(d => d.StatusValue, (int)newStatus)
                            .Set(d => d.ExtensionReason, reason)
                            .Set(d => d.ExtendedDeadlineAtUtc, extendedDeadline);
                        break;
                    }

                case DSRRequestStatus.IdentityVerified:
                    update = Builders<DSRRequestDocument>.Update
                        .Set(d => d.StatusValue, (int)newStatus)
                        .Set(d => d.VerifiedAtUtc, nowUtc);
                    break;

                default:
                    update = Builders<DSRRequestDocument>.Update
                        .Set(d => d.StatusValue, (int)newStatus);
                    break;
            }

            var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (result.MatchedCount == 0)
                return Left(DSRErrors.RequestNotFound(id));

            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(DSRErrors.StoreError("UpdateStatus", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DSRRequest>>> GetPendingRequestsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var pendingStatuses = new[]
            {
                (int)DSRRequestStatus.Received,
                (int)DSRRequestStatus.IdentityVerified,
                (int)DSRRequestStatus.InProgress,
                (int)DSRRequestStatus.Extended
            };

            var filter = Builders<DSRRequestDocument>.Filter.In(d => d.StatusValue, pendingStatuses);
            var documents = await _collection.Find(filter).ToListAsync(cancellationToken).ConfigureAwait(false);
            return Right<EncinaError, IReadOnlyList<DSRRequest>>(MapDocuments(documents));
        }
        catch (Exception ex)
        {
            return Left(DSRErrors.StoreError("GetPendingRequests", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DSRRequest>>> GetOverdueRequestsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var pendingStatuses = new[]
            {
                (int)DSRRequestStatus.Received,
                (int)DSRRequestStatus.IdentityVerified,
                (int)DSRRequestStatus.InProgress,
                (int)DSRRequestStatus.Extended
            };

            // Get all pending, then filter in memory for COALESCE logic
            var filter = Builders<DSRRequestDocument>.Filter.In(d => d.StatusValue, pendingStatuses);
            var documents = await _collection.Find(filter).ToListAsync(cancellationToken).ConfigureAwait(false);

            var overdue = documents
                .Where(d => (d.ExtendedDeadlineAtUtc ?? d.DeadlineAtUtc) < nowUtc)
                .ToList();

            return Right<EncinaError, IReadOnlyList<DSRRequest>>(MapDocuments(overdue));
        }
        catch (Exception ex)
        {
            return Left(DSRErrors.StoreError("GetOverdueRequests", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, bool>> HasActiveRestrictionAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);
        try
        {
            var restrictionValue = (int)DataSubjectRight.Restriction;
            var pendingStatuses = new[]
            {
                (int)DSRRequestStatus.Received,
                (int)DSRRequestStatus.IdentityVerified,
                (int)DSRRequestStatus.InProgress,
                (int)DSRRequestStatus.Extended
            };

            var filter = Builders<DSRRequestDocument>.Filter.And(
                Builders<DSRRequestDocument>.Filter.Eq(d => d.SubjectId, subjectId),
                Builders<DSRRequestDocument>.Filter.Eq(d => d.RightTypeValue, restrictionValue),
                Builders<DSRRequestDocument>.Filter.In(d => d.StatusValue, pendingStatuses));

            var count = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken).ConfigureAwait(false);
            return Right<EncinaError, bool>(count > 0);
        }
        catch (Exception ex)
        {
            return Left(DSRErrors.StoreError("HasActiveRestriction", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DSRRequest>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var documents = await _collection.Find(Builders<DSRRequestDocument>.Filter.Empty)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            return Right<EncinaError, IReadOnlyList<DSRRequest>>(MapDocuments(documents));
        }
        catch (Exception ex)
        {
            return Left(DSRErrors.StoreError("GetAll", ex.Message));
        }
    }

    private static List<DSRRequest> MapDocuments(List<DSRRequestDocument> documents)
    {
        var results = new List<DSRRequest>();
        foreach (var doc in documents)
        {
            var domain = doc.ToDomain();
            if (domain is not null)
                results.Add(domain);
        }
        return results;
    }
}

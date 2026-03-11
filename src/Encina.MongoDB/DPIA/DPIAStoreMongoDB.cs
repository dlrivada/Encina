using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;
using Encina.Messaging;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using static LanguageExt.Prelude;

namespace Encina.MongoDB.DPIA;

/// <summary>
/// MongoDB implementation of <see cref="IDPIAStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Manages <see cref="DPIAAssessment"/> instances throughout the assessment lifecycle:
/// from draft creation through approval, periodic review, and eventual expiration.
/// </para>
/// <para>
/// Per GDPR Article 35(11), assessments must be reviewed periodically. The
/// <see cref="GetExpiredAssessmentsAsync"/> method supports this by identifying assessments
/// past their review date.
/// </para>
/// <para>
/// Uses <see cref="DPIAAssessmentDocument"/> for MongoDB-native BSON serialization
/// and <c>ReplaceOne</c> with upsert for save operations.
/// </para>
/// </remarks>
public sealed class DPIAStoreMongoDB : IDPIAStore
{
    private readonly IMongoCollection<DPIAAssessmentDocument> _collection;
    private readonly ILogger<DPIAStoreMongoDB> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DPIAStoreMongoDB"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="options">The MongoDB configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    public DPIAStoreMongoDB(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> options,
        ILogger<DPIAStoreMongoDB> logger)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var config = options.Value;
        var database = mongoClient.GetDatabase(config.DatabaseName);
        _collection = database.GetCollection<DPIAAssessmentDocument>(config.Collections.DPIAAssessments);
        _logger = logger;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> SaveAssessmentAsync(
        DPIAAssessment assessment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assessment);

        try
        {
            var document = DPIAAssessmentDocument.FromAssessment(assessment);
            var filter = Builders<DPIAAssessmentDocument>.Filter.Eq(d => d.Id, document.Id);
            await _collection.ReplaceOneAsync(
                filter,
                document,
                new ReplaceOptions { IsUpsert = true },
                cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("Saved DPIA assessment '{AssessmentId}' for request type '{RequestTypeName}'",
                assessment.Id, assessment.RequestTypeName);
            return Right(unit);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "dpia.store_error",
                message: $"Failed to save DPIA assessment: {ex.Message}",
                details: new Dictionary<string, object?> { ["assessmentId"] = assessment.Id }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Option<DPIAAssessment>>> GetAssessmentAsync(
        string requestTypeName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestTypeName);

        try
        {
            var filter = Builders<DPIAAssessmentDocument>.Filter.Eq(d => d.RequestTypeName, requestTypeName);
            var document = await _collection.Find(filter)
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            if (document is null)
                return Right<EncinaError, Option<DPIAAssessment>>(None);

            return Right<EncinaError, Option<DPIAAssessment>>(Some(document.ToAssessment()!));
        }
        catch (Exception ex)
        {
            return Left<EncinaError, Option<DPIAAssessment>>(EncinaErrors.Create(
                code: "dpia.store_error",
                message: $"Failed to retrieve DPIA assessment: {ex.Message}",
                details: new Dictionary<string, object?> { ["requestTypeName"] = requestTypeName }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Option<DPIAAssessment>>> GetAssessmentByIdAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<DPIAAssessmentDocument>.Filter.Eq(d => d.Id, assessmentId.ToString("D"));
            var document = await _collection.Find(filter)
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            if (document is null)
                return Right<EncinaError, Option<DPIAAssessment>>(None);

            return Right<EncinaError, Option<DPIAAssessment>>(Some(document.ToAssessment()!));
        }
        catch (Exception ex)
        {
            return Left<EncinaError, Option<DPIAAssessment>>(EncinaErrors.Create(
                code: "dpia.store_error",
                message: $"Failed to retrieve DPIA assessment by ID: {ex.Message}",
                details: new Dictionary<string, object?> { ["assessmentId"] = assessmentId }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<DPIAAssessment>>> GetExpiredAssessmentsAsync(
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var nowDateTime = nowUtc.UtcDateTime;
            var approvedStatus = (int)DPIAAssessmentStatus.Approved;
            var filterBuilder = Builders<DPIAAssessmentDocument>.Filter;
            var filter = filterBuilder.Ne(d => d.NextReviewAtUtc, null)
                & filterBuilder.Lt(d => d.NextReviewAtUtc, nowDateTime)
                & filterBuilder.Eq(d => d.StatusValue, approvedStatus);

            var documents = await _collection.Find(filter)
                .SortBy(d => d.NextReviewAtUtc)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var assessments = documents
                .Select(d => d.ToAssessment())
                .Where(a => a is not null)
                .Cast<DPIAAssessment>()
                .ToList();

            _logger.LogDebug("Retrieved {Count} expired DPIA assessments", assessments.Count);
            return Right<EncinaError, IReadOnlyList<DPIAAssessment>>(assessments);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<DPIAAssessment>>(EncinaErrors.Create(
                code: "dpia.store_error",
                message: $"Failed to retrieve expired DPIA assessments: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<DPIAAssessment>>> GetAllAssessmentsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var documents = await _collection.Find(FilterDefinition<DPIAAssessmentDocument>.Empty)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var assessments = documents
                .Select(d => d.ToAssessment())
                .Where(a => a is not null)
                .Cast<DPIAAssessment>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<DPIAAssessment>>(assessments);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<DPIAAssessment>>(EncinaErrors.Create(
                code: "dpia.store_error",
                message: $"Failed to retrieve DPIA assessments: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> DeleteAssessmentAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<DPIAAssessmentDocument>.Filter.Eq(d => d.Id, assessmentId.ToString("D"));
            var result = await _collection.DeleteOneAsync(filter, cancellationToken).ConfigureAwait(false);

            if (result.DeletedCount == 0)
            {
                return Left(EncinaErrors.Create(
                    code: "dpia.not_found",
                    message: $"DPIA assessment '{assessmentId}' not found",
                    details: new Dictionary<string, object?> { ["assessmentId"] = assessmentId }));
            }

            _logger.LogDebug("Deleted DPIA assessment '{AssessmentId}'", assessmentId);
            return Right(unit);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "dpia.store_error",
                message: $"Failed to delete DPIA assessment: {ex.Message}",
                details: new Dictionary<string, object?> { ["assessmentId"] = assessmentId }));
        }
    }
}

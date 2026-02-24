using System.Diagnostics.CodeAnalysis;
using Encina.Compliance.GDPR;
using Encina.Compliance.GDPR.Diagnostics;
using LanguageExt;
using MongoDB.Driver;
using static LanguageExt.Prelude;

namespace Encina.MongoDB.ProcessingActivity;

/// <summary>
/// MongoDB implementation of <see cref="IProcessingActivityRegistry"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation creates its own MongoClient from the connection string,
/// making it safe for singleton registration. Uses INSERT-only semantics for
/// <see cref="RegisterActivityAsync"/> (fails on duplicate <c>request_type_name</c>)
/// and a separate replace for <see cref="UpdateActivityAsync"/>.
/// </para>
/// <para>
/// A unique index on <c>request_type_name</c> ensures at most one registration per request type.
/// Register via <c>AddEncinaProcessingActivityMongoDB(connectionString, databaseName)</c>.
/// </para>
/// </remarks>
public sealed class ProcessingActivityRegistryMongoDB : IProcessingActivityRegistry
{
    private readonly IMongoCollection<ProcessingActivityDocument> _collection;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessingActivityRegistryMongoDB"/> class.
    /// </summary>
    /// <param name="connectionString">The MongoDB connection string.</param>
    /// <param name="databaseName">The database name.</param>
    /// <param name="collectionName">The collection name (default: processing_activities).</param>
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "MongoClient is a long-lived singleton connection pool, disposed when the application shuts down")]
    public ProcessingActivityRegistryMongoDB(
        string connectionString,
        string databaseName,
        string collectionName = "processing_activities")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        _collection = database.GetCollection<ProcessingActivityDocument>(collectionName);

        // Ensure unique index on request_type_name (idempotent — no-op if already exists)
        _collection.Indexes.CreateOne(
            new CreateIndexModel<ProcessingActivityDocument>(
                Builders<ProcessingActivityDocument>.IndexKeys.Ascending(d => d.RequestTypeName),
                new CreateIndexOptions { Name = "IX_ProcessingActivity_RequestTypeName", Unique = true }));
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RegisterActivityAsync(
        Compliance.GDPR.ProcessingActivity activity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);

        using var trace = ProcessingActivityDiagnostics.StartRegistration(activity.RequestType);

        try
        {
            var entity = ProcessingActivityMapper.ToEntity(activity);
            var document = ProcessingActivityDocument.FromEntity(entity);

            await _collection.InsertOneAsync(document, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            ProcessingActivityDiagnostics.RecordSuccess(trace, "register");
            return Right(Unit.Default);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            ProcessingActivityDiagnostics.RecordFailure(trace, "register", "duplicate");
            return Left(GDPRErrors.ProcessingActivityDuplicate(activity.RequestType.AssemblyQualifiedName!));
        }
        catch (Exception ex)
        {
            ProcessingActivityDiagnostics.RecordFailure(trace, "register", ex.Message);
            return Left(GDPRErrors.ProcessingActivityStoreError("RegisterActivity", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<Compliance.GDPR.ProcessingActivity>>> GetAllActivitiesAsync(
        CancellationToken cancellationToken = default)
    {
        using var trace = ProcessingActivityDiagnostics.StartGetAll();

        try
        {
            var documents = await _collection.Find(Builders<ProcessingActivityDocument>.Filter.Empty)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var results = new List<Compliance.GDPR.ProcessingActivity>();
            foreach (var document in documents)
            {
                var entity = document.ToEntity();
                var domain = ProcessingActivityMapper.ToDomain(entity);
                if (domain is not null)
                {
                    results.Add(domain);
                }
            }

            ProcessingActivityDiagnostics.RecordSuccess(trace, results.Count, "get_all");
            return Right<EncinaError, IReadOnlyList<Compliance.GDPR.ProcessingActivity>>(results);
        }
        catch (Exception ex)
        {
            ProcessingActivityDiagnostics.RecordFailure(trace, "get_all", ex.Message);
            return Left(GDPRErrors.ProcessingActivityStoreError("GetAllActivities", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<Compliance.GDPR.ProcessingActivity>>> GetActivityByRequestTypeAsync(
        Type requestType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestType);

        using var trace = ProcessingActivityDiagnostics.StartGetByRequestType(requestType);

        try
        {
            var filter = Builders<ProcessingActivityDocument>.Filter.Eq(
                d => d.RequestTypeName, requestType.AssemblyQualifiedName!);

            var document = await _collection.Find(filter)
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            if (document is not null)
            {
                var entity = document.ToEntity();
                var domain = ProcessingActivityMapper.ToDomain(entity);
                ProcessingActivityDiagnostics.RecordSuccess(trace, "get_by_request_type");
                return domain is not null
                    ? Right<EncinaError, Option<Compliance.GDPR.ProcessingActivity>>(Some(domain))
                    : Right<EncinaError, Option<Compliance.GDPR.ProcessingActivity>>(None);
            }

            ProcessingActivityDiagnostics.RecordSuccess(trace, "get_by_request_type");
            return Right<EncinaError, Option<Compliance.GDPR.ProcessingActivity>>(None);
        }
        catch (Exception ex)
        {
            ProcessingActivityDiagnostics.RecordFailure(trace, "get_by_request_type", ex.Message);
            return Left(GDPRErrors.ProcessingActivityStoreError("GetActivityByRequestType", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> UpdateActivityAsync(
        Compliance.GDPR.ProcessingActivity activity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);

        using var trace = ProcessingActivityDiagnostics.StartUpdate(activity.RequestType);

        try
        {
            var entity = ProcessingActivityMapper.ToEntity(activity);
            var document = ProcessingActivityDocument.FromEntity(entity);

            var filter = Builders<ProcessingActivityDocument>.Filter.Eq(
                d => d.RequestTypeName, document.RequestTypeName);

            var result = await _collection.ReplaceOneAsync(
                filter,
                document,
                new ReplaceOptions { IsUpsert = false },
                cancellationToken).ConfigureAwait(false);

            if (result.MatchedCount > 0)
            {
                ProcessingActivityDiagnostics.RecordSuccess(trace, "update");
                return Right(Unit.Default);
            }

            ProcessingActivityDiagnostics.RecordFailure(trace, "update", "not_found");
            return Left(GDPRErrors.ProcessingActivityNotFound(activity.RequestType.AssemblyQualifiedName!));
        }
        catch (Exception ex)
        {
            ProcessingActivityDiagnostics.RecordFailure(trace, "update", ex.Message);
            return Left(GDPRErrors.ProcessingActivityStoreError("UpdateActivity", ex.Message));
        }
    }
}

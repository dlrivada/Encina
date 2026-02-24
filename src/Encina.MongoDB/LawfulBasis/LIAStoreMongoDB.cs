using System.Diagnostics.CodeAnalysis;
using Encina.Compliance.GDPR;
using LanguageExt;
using MongoDB.Driver;
using static LanguageExt.Prelude;

namespace Encina.MongoDB.LawfulBasis;

/// <summary>
/// MongoDB implementation of <see cref="ILIAStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation creates its own MongoClient from the connection string,
/// making it safe for singleton registration. Uses ReplaceOne with upsert for idempotent
/// storage based on document <c>Id</c>.
/// </para>
/// <para>
/// An index on <c>outcome_value</c> optimizes <see cref="GetPendingReviewAsync"/> queries.
/// Register via <c>AddEncinaLawfulBasisMongoDB(connectionString, databaseName)</c>.
/// </para>
/// </remarks>
public sealed class LIAStoreMongoDB : ILIAStore
{
    private readonly IMongoCollection<LIARecordDocument> _collection;

    /// <summary>
    /// Initializes a new instance of the <see cref="LIAStoreMongoDB"/> class.
    /// </summary>
    /// <param name="connectionString">The MongoDB connection string.</param>
    /// <param name="databaseName">The database name.</param>
    /// <param name="collectionName">The collection name (default: lia_records).</param>
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "MongoClient is a long-lived singleton connection pool, disposed when the application shuts down")]
    public LIAStoreMongoDB(
        string connectionString,
        string databaseName,
        string collectionName = "lia_records")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        _collection = database.GetCollection<LIARecordDocument>(collectionName);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> StoreAsync(
        LIARecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        try
        {
            var entity = LIARecordMapper.ToEntity(record);
            var document = LIARecordDocument.FromEntity(entity);

            var filter = Builders<LIARecordDocument>.Filter.Eq(d => d.Id, document.Id);

            await _collection.ReplaceOneAsync(
                filter,
                document,
                new ReplaceOptions { IsUpsert = true },
                cancellationToken).ConfigureAwait(false);

            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(GDPRErrors.LIAStoreError("Store", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<LIARecord>>> GetByReferenceAsync(
        string liaReference,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(liaReference);

        try
        {
            var filter = Builders<LIARecordDocument>.Filter.Eq(d => d.Id, liaReference);

            var document = await _collection.Find(filter)
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            if (document is not null)
            {
                var entity = document.ToEntity();
                var domain = LIARecordMapper.ToDomain(entity);
                return Right<EncinaError, Option<LIARecord>>(Some(domain));
            }

            return Right<EncinaError, Option<LIARecord>>(None);
        }
        catch (Exception ex)
        {
            return Left(GDPRErrors.LIAStoreError("GetByReference", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<LIARecord>>> GetPendingReviewAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<LIARecordDocument>.Filter.Eq(
                d => d.OutcomeValue, (int)LIAOutcome.RequiresReview);

            var documents = await _collection.Find(filter)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var results = documents
                .Select(d => LIARecordMapper.ToDomain(d.ToEntity()))
                .ToList();

            return Right<EncinaError, IReadOnlyList<LIARecord>>(results);
        }
        catch (Exception ex)
        {
            return Left(GDPRErrors.LIAStoreError("GetPendingReview", ex.Message));
        }
    }
}

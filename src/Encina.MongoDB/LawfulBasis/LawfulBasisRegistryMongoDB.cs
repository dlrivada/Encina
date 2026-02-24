using System.Diagnostics.CodeAnalysis;
using Encina.Compliance.GDPR;
using LanguageExt;
using MongoDB.Driver;
using static LanguageExt.Prelude;

namespace Encina.MongoDB.LawfulBasis;

/// <summary>
/// MongoDB implementation of <see cref="ILawfulBasisRegistry"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation creates its own MongoClient from the connection string,
/// making it safe for singleton registration. Uses ReplaceOne with upsert for idempotent
/// registration based on <c>RequestTypeName</c>.
/// </para>
/// <para>
/// A unique index on <c>request_type_name</c> ensures at most one registration per request type.
/// Register via <c>AddEncinaLawfulBasisMongoDB(connectionString, databaseName)</c>.
/// </para>
/// </remarks>
public sealed class LawfulBasisRegistryMongoDB : ILawfulBasisRegistry
{
    private readonly IMongoCollection<LawfulBasisRegistrationDocument> _collection;

    /// <summary>
    /// Initializes a new instance of the <see cref="LawfulBasisRegistryMongoDB"/> class.
    /// </summary>
    /// <param name="connectionString">The MongoDB connection string.</param>
    /// <param name="databaseName">The database name.</param>
    /// <param name="collectionName">The collection name (default: lawful_basis_registrations).</param>
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "MongoClient is a long-lived singleton connection pool, disposed when the application shuts down")]
    public LawfulBasisRegistryMongoDB(
        string connectionString,
        string databaseName,
        string collectionName = "lawful_basis_registrations")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        _collection = database.GetCollection<LawfulBasisRegistrationDocument>(collectionName);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RegisterAsync(
        LawfulBasisRegistration registration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registration);

        try
        {
            var entity = LawfulBasisRegistrationMapper.ToEntity(registration);
            var document = LawfulBasisRegistrationDocument.FromEntity(entity);

            var filter = Builders<LawfulBasisRegistrationDocument>.Filter.Eq(
                d => d.RequestTypeName, document.RequestTypeName);

            await _collection.ReplaceOneAsync(
                filter,
                document,
                new ReplaceOptions { IsUpsert = true },
                cancellationToken).ConfigureAwait(false);

            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(GDPRErrors.LawfulBasisStoreError("Register", ex.Message));
        }
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Option<LawfulBasisRegistration>>> GetByRequestTypeAsync(
        Type requestType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestType);
        return GetByRequestTypeNameAsync(requestType.AssemblyQualifiedName!, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<LawfulBasisRegistration>>> GetByRequestTypeNameAsync(
        string requestTypeName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestTypeName);

        try
        {
            var filter = Builders<LawfulBasisRegistrationDocument>.Filter.Eq(
                d => d.RequestTypeName, requestTypeName);

            var document = await _collection.Find(filter)
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            if (document is not null)
            {
                var entity = document.ToEntity();
                var domain = LawfulBasisRegistrationMapper.ToDomain(entity);
                return domain is not null
                    ? Right<EncinaError, Option<LawfulBasisRegistration>>(Some(domain))
                    : Right<EncinaError, Option<LawfulBasisRegistration>>(None);
            }

            return Right<EncinaError, Option<LawfulBasisRegistration>>(None);
        }
        catch (Exception ex)
        {
            return Left(GDPRErrors.LawfulBasisStoreError("GetByRequestTypeName", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<LawfulBasisRegistration>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var documents = await _collection.Find(Builders<LawfulBasisRegistrationDocument>.Filter.Empty)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var results = new List<LawfulBasisRegistration>();
            foreach (var document in documents)
            {
                var entity = document.ToEntity();
                var domain = LawfulBasisRegistrationMapper.ToDomain(entity);
                if (domain is not null)
                {
                    results.Add(domain);
                }
            }

            return Right<EncinaError, IReadOnlyList<LawfulBasisRegistration>>(results);
        }
        catch (Exception ex)
        {
            return Left(GDPRErrors.LawfulBasisStoreError("GetAll", ex.Message));
        }
    }
}

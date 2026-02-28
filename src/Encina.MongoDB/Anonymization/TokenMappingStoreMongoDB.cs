using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.Model;
using Encina.Messaging;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using static LanguageExt.Prelude;

namespace Encina.MongoDB.Anonymization;

/// <summary>
/// MongoDB implementation of <see cref="ITokenMappingStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Uses <see cref="IMongoClient"/> and <see cref="IMongoCollection{TDocument}"/> for
/// MongoDB-native operations. Documents are stored using <see cref="TokenMappingDocument"/>
/// with BSON attributes for serialization.
/// </para>
/// <para>
/// The token field should have a UNIQUE index and the original_value_hash field should
/// have an INDEX for deduplication queries. These can be configured via
/// <see cref="EncinaMongoDbOptions.CreateIndexes"/>.
/// </para>
/// </remarks>
public sealed class TokenMappingStoreMongoDB : ITokenMappingStore
{
    private readonly IMongoCollection<TokenMappingDocument> _collection;
    private readonly ILogger<TokenMappingStoreMongoDB> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenMappingStoreMongoDB"/> class.
    /// </summary>
    /// <param name="client">The MongoDB client.</param>
    /// <param name="options">The MongoDB configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    public TokenMappingStoreMongoDB(
        IMongoClient client,
        IOptions<EncinaMongoDbOptions> options,
        ILogger<TokenMappingStoreMongoDB> logger)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var opts = options.Value;
        var database = client.GetDatabase(opts.DatabaseName);
        _collection = database.GetCollection<TokenMappingDocument>(
            opts.Collections.TokenMappings);
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> StoreAsync(
        TokenMapping mapping,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mapping);

        try
        {
            var document = TokenMappingDocument.FromDomain(mapping);

            await _collection.ReplaceOneAsync(
                Builders<TokenMappingDocument>.Filter.Eq(d => d.Id, document.Id),
                document,
                new ReplaceOptions { IsUpsert = true },
                cancellationToken).ConfigureAwait(false);

            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(AnonymizationErrors.StoreError("Store", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<TokenMapping>>> GetByTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        try
        {
            var filter = Builders<TokenMappingDocument>.Filter.Eq(d => d.Token, token);
            var document = await _collection.Find(filter)
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            if (document is null)
                return Right<EncinaError, Option<TokenMapping>>(None);

            return Right<EncinaError, Option<TokenMapping>>(Some(document.ToDomain()));
        }
        catch (Exception ex)
        {
            return Left(AnonymizationErrors.StoreError("GetByToken", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<TokenMapping>>> GetByOriginalValueHashAsync(
        string hash,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);

        try
        {
            var filter = Builders<TokenMappingDocument>.Filter.Eq(d => d.OriginalValueHash, hash);
            var document = await _collection.Find(filter)
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            if (document is null)
                return Right<EncinaError, Option<TokenMapping>>(None);

            return Right<EncinaError, Option<TokenMapping>>(Some(document.ToDomain()));
        }
        catch (Exception ex)
        {
            return Left(AnonymizationErrors.StoreError("GetByOriginalValueHash", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> DeleteByKeyIdAsync(
        string keyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);

        try
        {
            var filter = Builders<TokenMappingDocument>.Filter.Eq(d => d.KeyId, keyId);
            await _collection.DeleteManyAsync(filter, cancellationToken).ConfigureAwait(false);
            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(AnonymizationErrors.StoreError("DeleteByKeyId", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<TokenMapping>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var documents = await _collection.Find(Builders<TokenMappingDocument>.Filter.Empty)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var results = documents
                .Select(d => d.ToDomain())
                .ToList();

            return Right<EncinaError, IReadOnlyList<TokenMapping>>(results);
        }
        catch (Exception ex)
        {
            return Left(AnonymizationErrors.StoreError("GetAll", ex.Message));
        }
    }
}

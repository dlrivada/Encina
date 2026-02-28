using System.Data;
using Dapper;
using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.Model;
using Encina.Messaging;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.PostgreSQL.Anonymization;

/// <summary>
/// Dapper implementation of <see cref="ITokenMappingStore"/> for PostgreSQL.
/// </summary>
/// <remarks>
/// <para>
/// All SQL column names use lowercase identifiers to match PostgreSQL conventions.
/// DateTimeOffset values are stored as <c>timestamptz</c> using <see cref="DateTimeOffset.UtcDateTime"/>.
/// Uses Dapper's <c>QueryAsync</c> and <c>ExecuteAsync</c> for lightweight data access.
/// </para>
/// <para>
/// The token column should have a UNIQUE index for fast lookups.
/// The originalvaluehash column should have an INDEX for deduplication queries.
/// </para>
/// </remarks>
public sealed class TokenMappingStoreDapper : ITokenMappingStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenMappingStoreDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The token mappings table name (default: tokenmappings).</param>
    public TokenMappingStoreDapper(
        IDbConnection connection,
        string tableName = "tokenmappings")
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> StoreAsync(
        TokenMapping mapping,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mapping);

        try
        {
            var entity = TokenMappingMapper.ToEntity(mapping);

            var sql = $@"
                INSERT INTO {_tableName}
                (id, token, originalvaluehash, encryptedoriginalvalue, keyid, createdatutc, expiresatutc)
                VALUES
                (@Id, @Token, @OriginalValueHash, @EncryptedOriginalValue, @KeyId, @CreatedAtUtc, @ExpiresAtUtc)";

            await _connection.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.Token,
                entity.OriginalValueHash,
                entity.EncryptedOriginalValue,
                entity.KeyId,
                CreatedAtUtc = entity.CreatedAtUtc.UtcDateTime,
                ExpiresAtUtc = entity.ExpiresAtUtc?.UtcDateTime
            });

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
            var sql = $@"
                SELECT id, token, originalvaluehash, encryptedoriginalvalue, keyid, createdatutc, expiresatutc
                FROM {_tableName}
                WHERE token = @Token";

            var rows = await _connection.QueryAsync<dynamic>(sql, new { Token = token });
            var row = rows.FirstOrDefault();

            if (row is null)
                return Right<EncinaError, Option<TokenMapping>>(None);

            var domain = MapToDomain(row);
            return Right<EncinaError, Option<TokenMapping>>(Some(domain));
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
            var sql = $@"
                SELECT id, token, originalvaluehash, encryptedoriginalvalue, keyid, createdatutc, expiresatutc
                FROM {_tableName}
                WHERE originalvaluehash = @Hash";

            var rows = await _connection.QueryAsync<dynamic>(sql, new { Hash = hash });
            var row = rows.FirstOrDefault();

            if (row is null)
                return Right<EncinaError, Option<TokenMapping>>(None);

            var domain = MapToDomain(row);
            return Right<EncinaError, Option<TokenMapping>>(Some(domain));
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
            var sql = $"DELETE FROM {_tableName} WHERE keyid = @KeyId";
            await _connection.ExecuteAsync(sql, new { KeyId = keyId });
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
            var sql = $@"
                SELECT id, token, originalvaluehash, encryptedoriginalvalue, keyid, createdatutc, expiresatutc
                FROM {_tableName}";

            var rows = await _connection.QueryAsync<dynamic>(sql);
            var results = rows
                .Select(MapToDomain)
                .ToList();

            return Right<EncinaError, IReadOnlyList<TokenMapping>>(results);
        }
        catch (Exception ex)
        {
            return Left(AnonymizationErrors.StoreError("GetAll", ex.Message));
        }
    }

    private static TokenMapping MapToDomain(dynamic row)
    {
        var entity = new TokenMappingEntity
        {
            Id = (string)row.id,
            Token = (string)row.token,
            OriginalValueHash = (string)row.originalvaluehash,
            EncryptedOriginalValue = (byte[])row.encryptedoriginalvalue,
            KeyId = (string)row.keyid,
            CreatedAtUtc = new DateTimeOffset((DateTime)row.createdatutc, TimeSpan.Zero),
            ExpiresAtUtc = row.expiresatutc is null or DBNull
                ? null
                : new DateTimeOffset((DateTime)row.expiresatutc, TimeSpan.Zero)
        };

        return TokenMappingMapper.ToDomain(entity);
    }
}

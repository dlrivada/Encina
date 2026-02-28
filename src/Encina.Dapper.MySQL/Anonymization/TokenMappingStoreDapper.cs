using System.Data;
using Dapper;
using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.Model;
using Encina.Messaging;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.MySQL.Anonymization;

/// <summary>
/// Dapper implementation of <see cref="ITokenMappingStore"/> for MySQL.
/// </summary>
/// <remarks>
/// <para>
/// All SQL column names use PascalCase identifiers to match MySQL conventions.
/// DateTimeOffset values are stored as <c>DATETIME(6)</c> using <see cref="DateTimeOffset.UtcDateTime"/>.
/// Uses Dapper's <c>QueryAsync</c> and <c>ExecuteAsync</c> for lightweight data access.
/// </para>
/// <para>
/// The Token column should have a UNIQUE index for fast lookups.
/// The OriginalValueHash column should have an INDEX for deduplication queries.
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
    /// <param name="tableName">The token mappings table name (default: TokenMappings).</param>
    public TokenMappingStoreDapper(
        IDbConnection connection,
        string tableName = "TokenMappings")
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
                (Id, Token, OriginalValueHash, EncryptedOriginalValue, KeyId, CreatedAtUtc, ExpiresAtUtc)
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
                SELECT Id, Token, OriginalValueHash, EncryptedOriginalValue, KeyId, CreatedAtUtc, ExpiresAtUtc
                FROM {_tableName}
                WHERE Token = @Token";

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
                SELECT Id, Token, OriginalValueHash, EncryptedOriginalValue, KeyId, CreatedAtUtc, ExpiresAtUtc
                FROM {_tableName}
                WHERE OriginalValueHash = @Hash";

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
            var sql = $"DELETE FROM {_tableName} WHERE KeyId = @KeyId";
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
                SELECT Id, Token, OriginalValueHash, EncryptedOriginalValue, KeyId, CreatedAtUtc, ExpiresAtUtc
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
            Id = (string)row.Id,
            Token = (string)row.Token,
            OriginalValueHash = (string)row.OriginalValueHash,
            EncryptedOriginalValue = (byte[])row.EncryptedOriginalValue,
            KeyId = (string)row.KeyId,
            CreatedAtUtc = new DateTimeOffset((DateTime)row.CreatedAtUtc, TimeSpan.Zero),
            ExpiresAtUtc = row.ExpiresAtUtc is null or DBNull
                ? null
                : new DateTimeOffset((DateTime)row.ExpiresAtUtc, TimeSpan.Zero)
        };

        return TokenMappingMapper.ToDomain(entity);
    }
}

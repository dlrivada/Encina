using System.Data;
using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.Model;
using Encina.Messaging;
using LanguageExt;
using MySqlConnector;
using static LanguageExt.Prelude;

namespace Encina.ADO.MySQL.Anonymization;

/// <summary>
/// ADO.NET implementation of <see cref="ITokenMappingStore"/> for MySQL.
/// </summary>
/// <remarks>
/// <para>
/// Uses raw <see cref="MySqlCommand"/> and <see cref="MySqlDataReader"/> for maximum performance.
/// All SQL column names use PascalCase identifiers to match MySQL conventions.
/// DateTimeOffset values are stored as <c>DATETIME(6)</c> using <see cref="DateTimeOffset.UtcDateTime"/>.
/// </para>
/// <para>
/// The Token column should have a UNIQUE index for fast lookups.
/// The OriginalValueHash column should have an INDEX for deduplication queries.
/// </para>
/// </remarks>
public sealed class TokenMappingStoreADO : ITokenMappingStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenMappingStoreADO"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The token mappings table name (default: TokenMappings).</param>
    public TokenMappingStoreADO(
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

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddEntityParameters(command, entity);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
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

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Token", token);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            if (await ReadAsync(reader, cancellationToken))
            {
                var domain = MapToDomain(reader);
                return Right<EncinaError, Option<TokenMapping>>(Some(domain));
            }

            return Right<EncinaError, Option<TokenMapping>>(None);
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

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Hash", hash);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            if (await ReadAsync(reader, cancellationToken))
            {
                var domain = MapToDomain(reader);
                return Right<EncinaError, Option<TokenMapping>>(Some(domain));
            }

            return Right<EncinaError, Option<TokenMapping>>(None);
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

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@KeyId", keyId);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
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

            using var command = _connection.CreateCommand();
            command.CommandText = sql;

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var results = new List<TokenMapping>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                results.Add(MapToDomain(reader));
            }

            return Right<EncinaError, IReadOnlyList<TokenMapping>>(results);
        }
        catch (Exception ex)
        {
            return Left(AnonymizationErrors.StoreError("GetAll", ex.Message));
        }
    }

    private static void AddEntityParameters(IDbCommand command, TokenMappingEntity entity)
    {
        AddParameter(command, "@Id", entity.Id);
        AddParameter(command, "@Token", entity.Token);
        AddParameter(command, "@OriginalValueHash", entity.OriginalValueHash);
        AddParameter(command, "@EncryptedOriginalValue", entity.EncryptedOriginalValue);
        AddParameter(command, "@KeyId", entity.KeyId);
        AddParameter(command, "@CreatedAtUtc", entity.CreatedAtUtc.UtcDateTime);
        AddParameter(command, "@ExpiresAtUtc", entity.ExpiresAtUtc.HasValue ? entity.ExpiresAtUtc.Value.UtcDateTime : DBNull.Value);
    }

    private static TokenMapping MapToDomain(IDataReader reader)
    {
        var entity = new TokenMappingEntity
        {
            Id = reader.GetString(reader.GetOrdinal("Id")),
            Token = reader.GetString(reader.GetOrdinal("Token")),
            OriginalValueHash = reader.GetString(reader.GetOrdinal("OriginalValueHash")),
            EncryptedOriginalValue = (byte[])reader.GetValue(reader.GetOrdinal("EncryptedOriginalValue")),
            KeyId = reader.GetString(reader.GetOrdinal("KeyId")),
            CreatedAtUtc = new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("CreatedAtUtc")), TimeSpan.Zero),
            ExpiresAtUtc = reader.IsDBNull(reader.GetOrdinal("ExpiresAtUtc"))
                ? null
                : new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("ExpiresAtUtc")), TimeSpan.Zero)
        };

        return TokenMappingMapper.ToDomain(entity);
    }

    private static void AddParameter(IDbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private async Task OpenConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is MySqlConnection mySqlConnection)
        {
            await mySqlConnection.OpenAsync(cancellationToken);
            return;
        }

        _connection.Open();
    }

    private static async Task<IDataReader> ExecuteReaderAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is MySqlCommand mySqlCommand)
            return await mySqlCommand.ExecuteReaderAsync(cancellationToken);

        return await Task.Run(command.ExecuteReader, cancellationToken);
    }

    private static async Task<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is MySqlCommand mySqlCommand)
            return await mySqlCommand.ExecuteNonQueryAsync(cancellationToken);

        return await Task.Run(command.ExecuteNonQuery, cancellationToken);
    }

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is MySqlDataReader mySqlReader)
            return await mySqlReader.ReadAsync(cancellationToken);

        return await Task.Run(reader.Read, cancellationToken);
    }
}

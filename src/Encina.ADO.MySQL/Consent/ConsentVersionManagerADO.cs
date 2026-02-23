using System.Data;
using Encina.Compliance.Consent;
using Encina.Messaging;
using LanguageExt;
using MySqlConnector;
using static LanguageExt.Prelude;

namespace Encina.ADO.MySQL.Consent;

/// <summary>
/// ADO.NET implementation of <see cref="IConsentVersionManager"/> for MySQL.
/// Manages consent term versions and reconsent requirements.
/// </summary>
public sealed class ConsentVersionManagerADO : IConsentVersionManager
{
    private readonly IDbConnection _connection;
    private readonly string _versionsTableName;
    private readonly string _consentTableName;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsentVersionManagerADO"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="versionsTableName">The versions table name (default: ConsentVersions).</param>
    /// <param name="consentTableName">The consent records table name (default: ConsentRecords).</param>
    /// <param name="timeProvider">The time provider (unused, kept for constructor parity across providers).</param>
    public ConsentVersionManagerADO(
        IDbConnection connection,
        string versionsTableName = "ConsentVersions",
        string consentTableName = "ConsentRecords",
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _versionsTableName = SqlIdentifierValidator.ValidateTableName(versionsTableName);
        _consentTableName = SqlIdentifierValidator.ValidateTableName(consentTableName);
        _ = timeProvider; // Kept for constructor parity across providers
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, ConsentVersion>> GetCurrentVersionAsync(
        string purpose,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        try
        {
            var sql = $@"
                SELECT VersionId, Purpose, EffectiveFromUtc, Description, RequiresExplicitReconsent
                FROM {_versionsTableName}
                WHERE Purpose = @Purpose
                ORDER BY EffectiveFromUtc DESC
                LIMIT 1";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Purpose", purpose);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            if (await ReadAsync(reader, cancellationToken))
            {
                return Right(MapToConsentVersion(reader));
            }

            return Left(EncinaErrors.Create(
                code: "consent.version_not_found",
                message: $"No consent version found for purpose '{purpose}'.",
                details: new Dictionary<string, object?> { ["purpose"] = purpose }));
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "consent.version_store_error",
                message: $"Failed to get current version: {ex.Message}",
                details: new Dictionary<string, object?> { ["purpose"] = purpose }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> PublishNewVersionAsync(
        ConsentVersion version,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(version);

        try
        {
            var insertSql = $@"
                INSERT INTO {_versionsTableName}
                (VersionId, Purpose, EffectiveFromUtc, Description, RequiresExplicitReconsent)
                VALUES
                (@VersionId, @Purpose, @EffectiveFromUtc, @Description, @RequiresExplicitReconsent)";

            using var insertCommand = _connection.CreateCommand();
            insertCommand.CommandText = insertSql;
            AddParameter(insertCommand, "@VersionId", version.VersionId);
            AddParameter(insertCommand, "@Purpose", version.Purpose);
            AddParameter(insertCommand, "@EffectiveFromUtc", version.EffectiveFromUtc.UtcDateTime);
            AddParameter(insertCommand, "@Description", version.Description);
            AddParameter(insertCommand, "@RequiresExplicitReconsent", version.RequiresExplicitReconsent);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(insertCommand, cancellationToken);

            // If the new version requires explicit reconsent, update existing active consents
            if (version.RequiresExplicitReconsent)
            {
                var updateSql = $@"
                    UPDATE {_consentTableName}
                    SET Status = @RequiresReconsentStatus
                    WHERE Purpose = @Purpose
                      AND Status = @ActiveStatus
                      AND ConsentVersionId != @NewVersionId";

                using var updateCommand = _connection.CreateCommand();
                updateCommand.CommandText = updateSql;
                AddParameter(updateCommand, "@RequiresReconsentStatus", (int)ConsentStatus.RequiresReconsent);
                AddParameter(updateCommand, "@Purpose", version.Purpose);
                AddParameter(updateCommand, "@ActiveStatus", (int)ConsentStatus.Active);
                AddParameter(updateCommand, "@NewVersionId", version.VersionId);

                await ExecuteNonQueryAsync(updateCommand, cancellationToken);
            }

            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "consent.version_store_error",
                message: $"Failed to publish new version: {ex.Message}",
                details: new Dictionary<string, object?> { ["versionId"] = version.VersionId, ["purpose"] = version.Purpose }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, bool>> RequiresReconsentAsync(
        string subjectId,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        try
        {
            // Get the consent record
            var consentSql = $@"
                SELECT ConsentVersionId
                FROM {_consentTableName}
                WHERE SubjectId = @SubjectId
                  AND Purpose = @Purpose
                  AND Status = @ActiveStatus";

            using var consentCommand = _connection.CreateCommand();
            consentCommand.CommandText = consentSql;
            AddParameter(consentCommand, "@SubjectId", subjectId);
            AddParameter(consentCommand, "@Purpose", purpose);
            AddParameter(consentCommand, "@ActiveStatus", (int)ConsentStatus.Active);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            string? consentedVersionId;
            using (var consentReader = await ExecuteReaderAsync(consentCommand, cancellationToken))
            {
                if (!await ReadAsync(consentReader, cancellationToken))
                {
                    // No active consent found - reconsent is required
                    return Right(true);
                }

                consentedVersionId = consentReader.GetString(consentReader.GetOrdinal("ConsentVersionId"));
            }

            // Get the current version
            var versionSql = $@"
                SELECT VersionId, RequiresExplicitReconsent
                FROM {_versionsTableName}
                WHERE Purpose = @Purpose
                ORDER BY EffectiveFromUtc DESC
                LIMIT 1";

            using var versionCommand = _connection.CreateCommand();
            versionCommand.CommandText = versionSql;
            AddParameter(versionCommand, "@Purpose", purpose);

            using var versionReader = await ExecuteReaderAsync(versionCommand, cancellationToken);
            if (!await ReadAsync(versionReader, cancellationToken))
            {
                // No version found - no reconsent needed
                return Right(false);
            }

            var currentVersionId = versionReader.GetString(versionReader.GetOrdinal("VersionId"));
            var requiresReconsent = versionReader.GetBoolean(versionReader.GetOrdinal("RequiresExplicitReconsent"));

            // Reconsent needed if version differs and new version requires it
            return Right(currentVersionId != consentedVersionId && requiresReconsent);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "consent.version_store_error",
                message: $"Failed to check reconsent requirement: {ex.Message}",
                details: new Dictionary<string, object?> { ["subjectId"] = subjectId, ["purpose"] = purpose }));
        }
    }

    private static ConsentVersion MapToConsentVersion(IDataReader reader) => new()
    {
        VersionId = reader.GetString(reader.GetOrdinal("VersionId")),
        Purpose = reader.GetString(reader.GetOrdinal("Purpose")),
        EffectiveFromUtc = new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("EffectiveFromUtc")), TimeSpan.Zero),
        Description = reader.GetString(reader.GetOrdinal("Description")),
        RequiresExplicitReconsent = reader.GetBoolean(reader.GetOrdinal("RequiresExplicitReconsent"))
    };

    private static void AddParameter(IDbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static Task OpenConnectionAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    private static async Task<IDataReader> ExecuteReaderAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is MySqlCommand mysqlCommand)
            return await mysqlCommand.ExecuteReaderAsync(cancellationToken);

        return await Task.Run(command.ExecuteReader, cancellationToken);
    }

    private static async Task<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is MySqlCommand mysqlCommand)
            return await mysqlCommand.ExecuteNonQueryAsync(cancellationToken);

        return await Task.Run(command.ExecuteNonQuery, cancellationToken);
    }

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is MySqlDataReader mysqlReader)
            return await mysqlReader.ReadAsync(cancellationToken);

        return await Task.Run(reader.Read, cancellationToken);
    }
}

using System.Data;
using System.Text.Json;
using Encina.Compliance.Consent;
using Encina.Messaging;
using LanguageExt;
using Microsoft.Data.SqlClient;
using static LanguageExt.Prelude;

namespace Encina.ADO.SqlServer.Consent;

/// <summary>
/// ADO.NET implementation of <see cref="IConsentStore"/> for SQL Server.
/// Uses raw SqlCommand and SqlDataReader for maximum performance.
/// </summary>
public sealed class ConsentStoreADO : IConsentStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly string _versionsTableName;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsentStoreADO"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The consent records table name (default: ConsentRecords).</param>
    /// <param name="versionsTableName">The consent versions table name (default: ConsentVersions).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    public ConsentStoreADO(
        IDbConnection connection,
        string tableName = "ConsentRecords",
        string versionsTableName = "ConsentVersions",
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
        _versionsTableName = SqlIdentifierValidator.ValidateTableName(versionsTableName);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RecordConsentAsync(
        ConsentRecord consent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consent);

        try
        {
            var sql = $@"
                INSERT INTO {_tableName}
                (Id, SubjectId, Purpose, Status, ConsentVersionId, GivenAtUtc, WithdrawnAtUtc, ExpiresAtUtc, Source, IpAddress, ProofOfConsent, Metadata)
                VALUES
                (@Id, @SubjectId, @Purpose, @Status, @ConsentVersionId, @GivenAtUtc, @WithdrawnAtUtc, @ExpiresAtUtc, @Source, @IpAddress, @ProofOfConsent, @Metadata)";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddConsentRecordParameters(command, consent);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "consent.store_error",
                message: $"Failed to record consent: {ex.Message}",
                details: new Dictionary<string, object?> { ["subjectId"] = consent.SubjectId, ["purpose"] = consent.Purpose }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<ConsentRecord>>> GetConsentAsync(
        string subjectId,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        try
        {
            var sql = $@"
                SELECT Id, SubjectId, Purpose, Status, ConsentVersionId, GivenAtUtc, WithdrawnAtUtc, ExpiresAtUtc, Source, IpAddress, ProofOfConsent, Metadata
                FROM {_tableName}
                WHERE SubjectId = @SubjectId AND Purpose = @Purpose";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@SubjectId", subjectId);
            AddParameter(command, "@Purpose", purpose);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            if (await ReadAsync(reader, cancellationToken))
            {
                return Right<EncinaError, Option<ConsentRecord>>(Some(MapToConsentRecord(reader)));
            }

            return Right<EncinaError, Option<ConsentRecord>>(None);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "consent.store_error",
                message: $"Failed to get consent: {ex.Message}",
                details: new Dictionary<string, object?> { ["subjectId"] = subjectId, ["purpose"] = purpose }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<ConsentRecord>>> GetAllConsentsAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);

        try
        {
            var sql = $@"
                SELECT Id, SubjectId, Purpose, Status, ConsentVersionId, GivenAtUtc, WithdrawnAtUtc, ExpiresAtUtc, Source, IpAddress, ProofOfConsent, Metadata
                FROM {_tableName}
                WHERE SubjectId = @SubjectId";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@SubjectId", subjectId);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var records = new List<ConsentRecord>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                records.Add(MapToConsentRecord(reader));
            }

            return Right<EncinaError, IReadOnlyList<ConsentRecord>>(records);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "consent.store_error",
                message: $"Failed to get all consents: {ex.Message}",
                details: new Dictionary<string, object?> { ["subjectId"] = subjectId }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> WithdrawConsentAsync(
        string subjectId,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        try
        {
            var nowUtc = _timeProvider.GetUtcNow();
            var sql = $@"
                UPDATE {_tableName}
                SET Status = @WithdrawnStatus,
                    WithdrawnAtUtc = @NowUtc
                WHERE SubjectId = @SubjectId
                  AND Purpose = @Purpose
                  AND Status = @ActiveStatus";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@SubjectId", subjectId);
            AddParameter(command, "@Purpose", purpose);
            AddParameter(command, "@WithdrawnStatus", (int)ConsentStatus.Withdrawn);
            AddParameter(command, "@ActiveStatus", (int)ConsentStatus.Active);
            AddParameter(command, "@NowUtc", nowUtc);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var rowsAffected = await ExecuteNonQueryAsync(command, cancellationToken);
            if (rowsAffected == 0)
            {
                return Left(ConsentErrors.MissingConsent(subjectId, purpose));
            }

            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "consent.store_error",
                message: $"Failed to withdraw consent: {ex.Message}",
                details: new Dictionary<string, object?> { ["subjectId"] = subjectId, ["purpose"] = purpose }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, bool>> HasValidConsentAsync(
        string subjectId,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        try
        {
            var nowUtc = _timeProvider.GetUtcNow();
            var sql = $@"
                SELECT COUNT(1)
                FROM {_tableName}
                WHERE SubjectId = @SubjectId
                  AND Purpose = @Purpose
                  AND Status = @ActiveStatus
                  AND (ExpiresAtUtc IS NULL OR ExpiresAtUtc > @NowUtc)";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@SubjectId", subjectId);
            AddParameter(command, "@Purpose", purpose);
            AddParameter(command, "@ActiveStatus", (int)ConsentStatus.Active);
            AddParameter(command, "@NowUtc", nowUtc);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            if (await ReadAsync(reader, cancellationToken))
            {
                var count = reader.GetInt32(0);
                return Right(count > 0);
            }

            return Right(false);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "consent.store_error",
                message: $"Failed to check consent validity: {ex.Message}",
                details: new Dictionary<string, object?> { ["subjectId"] = subjectId, ["purpose"] = purpose }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, BulkOperationResult>> BulkRecordConsentAsync(
        IEnumerable<ConsentRecord> consents,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consents);

        var consentList = consents.ToList();
        var successCount = 0;
        var errors = new List<BulkOperationError>();

        foreach (var consent in consentList)
        {
            var result = await RecordConsentAsync(consent, cancellationToken);
            result.Match(
                Right: _ => successCount++,
                Left: error => errors.Add(new BulkOperationError(
                    $"{consent.SubjectId}:{consent.Purpose}", error)));
        }

        return Right<EncinaError, BulkOperationResult>(errors.Count == 0
            ? BulkOperationResult.Success(successCount)
            : BulkOperationResult.Partial(successCount, errors));
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, BulkOperationResult>> BulkWithdrawConsentAsync(
        string subjectId,
        IEnumerable<string> purposes,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);
        ArgumentNullException.ThrowIfNull(purposes);

        var purposeList = purposes.ToList();
        var successCount = 0;
        var errors = new List<BulkOperationError>();

        foreach (var purpose in purposeList)
        {
            var result = await WithdrawConsentAsync(subjectId, purpose, cancellationToken);
            result.Match(
                Right: _ => successCount++,
                Left: error => errors.Add(new BulkOperationError(
                    $"{subjectId}:{purpose}", error)));
        }

        return Right<EncinaError, BulkOperationResult>(errors.Count == 0
            ? BulkOperationResult.Success(successCount)
            : BulkOperationResult.Partial(successCount, errors));
    }

    private static void AddConsentRecordParameters(IDbCommand command, ConsentRecord consent)
    {
        AddParameter(command, "@Id", consent.Id);
        AddParameter(command, "@SubjectId", consent.SubjectId);
        AddParameter(command, "@Purpose", consent.Purpose);
        AddParameter(command, "@Status", (int)consent.Status);
        AddParameter(command, "@ConsentVersionId", consent.ConsentVersionId);
        AddParameter(command, "@GivenAtUtc", consent.GivenAtUtc);
        AddParameter(command, "@WithdrawnAtUtc", consent.WithdrawnAtUtc);
        AddParameter(command, "@ExpiresAtUtc", consent.ExpiresAtUtc);
        AddParameter(command, "@Source", consent.Source);
        AddParameter(command, "@IpAddress", consent.IpAddress);
        AddParameter(command, "@ProofOfConsent", consent.ProofOfConsent);
        AddParameter(command, "@Metadata", JsonSerializer.Serialize(consent.Metadata));
    }

    private static ConsentRecord MapToConsentRecord(IDataReader reader)
    {
        var metadataJson = reader.GetString(reader.GetOrdinal("Metadata"));
        var metadata = JsonSerializer.Deserialize<Dictionary<string, object?>>(metadataJson)
            ?? new Dictionary<string, object?>();

        return new ConsentRecord
        {
            Id = reader.GetGuid(reader.GetOrdinal("Id")),
            SubjectId = reader.GetString(reader.GetOrdinal("SubjectId")),
            Purpose = reader.GetString(reader.GetOrdinal("Purpose")),
            Status = (ConsentStatus)reader.GetInt32(reader.GetOrdinal("Status")),
            ConsentVersionId = reader.GetString(reader.GetOrdinal("ConsentVersionId")),
            GivenAtUtc = (DateTimeOffset)reader.GetValue(reader.GetOrdinal("GivenAtUtc")),
            WithdrawnAtUtc = reader.IsDBNull(reader.GetOrdinal("WithdrawnAtUtc"))
                ? null
                : (DateTimeOffset)reader.GetValue(reader.GetOrdinal("WithdrawnAtUtc")),
            ExpiresAtUtc = reader.IsDBNull(reader.GetOrdinal("ExpiresAtUtc"))
                ? null
                : (DateTimeOffset)reader.GetValue(reader.GetOrdinal("ExpiresAtUtc")),
            Source = reader.GetString(reader.GetOrdinal("Source")),
            IpAddress = reader.IsDBNull(reader.GetOrdinal("IpAddress"))
                ? null
                : reader.GetString(reader.GetOrdinal("IpAddress")),
            ProofOfConsent = reader.IsDBNull(reader.GetOrdinal("ProofOfConsent"))
                ? null
                : reader.GetString(reader.GetOrdinal("ProofOfConsent")),
            Metadata = metadata
        };
    }

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
        if (command is SqlCommand sqlCommand)
            return await sqlCommand.ExecuteReaderAsync(cancellationToken);

        return await Task.Run(command.ExecuteReader, cancellationToken);
    }

    private static async Task<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is SqlCommand sqlCommand)
            return await sqlCommand.ExecuteNonQueryAsync(cancellationToken);

        return await Task.Run(command.ExecuteNonQuery, cancellationToken);
    }

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is SqlDataReader sqlReader)
            return await sqlReader.ReadAsync(cancellationToken);

        return await Task.Run(reader.Read, cancellationToken);
    }
}

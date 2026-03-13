using System.Data;
using System.Globalization;
using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Messaging;
using LanguageExt;
using Microsoft.Data.Sqlite;
using static LanguageExt.Prelude;

namespace Encina.ADO.Sqlite.ProcessorAgreements;

/// <summary>
/// ADO.NET implementation of <see cref="IDPAStore"/> for SQLite.
/// Manages Data Processing Agreement persistence per GDPR Article 28(3).
/// </summary>
public sealed class DPAStoreADO : IDPAStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DPAStoreADO"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The agreements table name (default: DataProcessingAgreements).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    public DPAStoreADO(
        IDbConnection connection,
        string tableName = "DataProcessingAgreements",
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> AddAsync(
        DataProcessingAgreement agreement,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agreement);

        try
        {
            // Check if agreement already exists
            var existsSql = $"SELECT COUNT(1) FROM {_tableName} WHERE Id = @Id";
            using var existsCommand = _connection.CreateCommand();
            existsCommand.CommandText = existsSql;
            AddParameter(existsCommand, "@Id", agreement.Id);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var existsResult = await ExecuteScalarAsync(existsCommand, cancellationToken);
            var count = Convert.ToInt32(existsResult, CultureInfo.InvariantCulture);
            if (count > 0)
            {
                return Left(EncinaErrors.Create(
                    code: "processor_agreements.already_exists",
                    message: $"Data Processing Agreement with ID '{agreement.Id}' already exists.",
                    details: new Dictionary<string, object?> { ["dpaId"] = agreement.Id }));
            }

            var entity = DataProcessingAgreementMapper.ToEntity(agreement);
            var sql = $@"
                INSERT INTO {_tableName}
                (Id, ProcessorId, StatusValue, SignedAtUtc, ExpiresAtUtc, HasSCCs, ProcessingPurposesJson,
                 ProcessOnDocumentedInstructions, ConfidentialityObligations, SecurityMeasures,
                 SubProcessorRequirements, DataSubjectRightsAssistance, ComplianceAssistance,
                 DataDeletionOrReturn, AuditRights, TenantId, ModuleId, CreatedAtUtc, LastUpdatedAtUtc)
                VALUES
                (@Id, @ProcessorId, @StatusValue, @SignedAtUtc, @ExpiresAtUtc, @HasSCCs, @ProcessingPurposesJson,
                 @ProcessOnDocumentedInstructions, @ConfidentialityObligations, @SecurityMeasures,
                 @SubProcessorRequirements, @DataSubjectRightsAssistance, @ComplianceAssistance,
                 @DataDeletionOrReturn, @AuditRights, @TenantId, @ModuleId, @CreatedAtUtc, @LastUpdatedAtUtc)";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddDPAParameters(command, entity);

            await ExecuteNonQueryAsync(command, cancellationToken);
            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "processor_agreements.store_error",
                message: $"Failed to add Data Processing Agreement: {ex.Message}",
                details: new Dictionary<string, object?> { ["dpaId"] = agreement.Id }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<DataProcessingAgreement>>> GetByIdAsync(
        string dpaId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dpaId);

        try
        {
            var sql = $@"
                SELECT Id, ProcessorId, StatusValue, SignedAtUtc, ExpiresAtUtc, HasSCCs, ProcessingPurposesJson,
                       ProcessOnDocumentedInstructions, ConfidentialityObligations, SecurityMeasures,
                       SubProcessorRequirements, DataSubjectRightsAssistance, ComplianceAssistance,
                       DataDeletionOrReturn, AuditRights, TenantId, ModuleId, CreatedAtUtc, LastUpdatedAtUtc
                FROM {_tableName}
                WHERE Id = @Id";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", dpaId);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            if (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToDPAEntity(reader);
                var domain = DataProcessingAgreementMapper.ToDomain(entity);
                return domain is not null
                    ? Right<EncinaError, Option<DataProcessingAgreement>>(Some(domain))
                    : Right<EncinaError, Option<DataProcessingAgreement>>(None);
            }

            return Right<EncinaError, Option<DataProcessingAgreement>>(None);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "processor_agreements.store_error",
                message: $"Failed to get Data Processing Agreement: {ex.Message}",
                details: new Dictionary<string, object?> { ["dpaId"] = dpaId }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DataProcessingAgreement>>> GetByProcessorIdAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processorId);

        try
        {
            var sql = $@"
                SELECT Id, ProcessorId, StatusValue, SignedAtUtc, ExpiresAtUtc, HasSCCs, ProcessingPurposesJson,
                       ProcessOnDocumentedInstructions, ConfidentialityObligations, SecurityMeasures,
                       SubProcessorRequirements, DataSubjectRightsAssistance, ComplianceAssistance,
                       DataDeletionOrReturn, AuditRights, TenantId, ModuleId, CreatedAtUtc, LastUpdatedAtUtc
                FROM {_tableName}
                WHERE ProcessorId = @ProcessorId
                ORDER BY CreatedAtUtc";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@ProcessorId", processorId);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var results = new List<DataProcessingAgreement>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToDPAEntity(reader);
                var domain = DataProcessingAgreementMapper.ToDomain(entity);
                if (domain is not null)
                    results.Add(domain);
            }

            return Right<EncinaError, IReadOnlyList<DataProcessingAgreement>>(results);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "processor_agreements.store_error",
                message: $"Failed to get Data Processing Agreements by processor: {ex.Message}",
                details: new Dictionary<string, object?> { ["processorId"] = processorId }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<DataProcessingAgreement>>> GetActiveByProcessorIdAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processorId);

        try
        {
            var sql = $@"
                SELECT Id, ProcessorId, StatusValue, SignedAtUtc, ExpiresAtUtc, HasSCCs, ProcessingPurposesJson,
                       ProcessOnDocumentedInstructions, ConfidentialityObligations, SecurityMeasures,
                       SubProcessorRequirements, DataSubjectRightsAssistance, ComplianceAssistance,
                       DataDeletionOrReturn, AuditRights, TenantId, ModuleId, CreatedAtUtc, LastUpdatedAtUtc
                FROM {_tableName}
                WHERE ProcessorId = @ProcessorId AND StatusValue = @ActiveStatus";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@ProcessorId", processorId);
            AddParameter(command, "@ActiveStatus", (int)DPAStatus.Active);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            if (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToDPAEntity(reader);
                var domain = DataProcessingAgreementMapper.ToDomain(entity);
                return domain is not null
                    ? Right<EncinaError, Option<DataProcessingAgreement>>(Some(domain))
                    : Right<EncinaError, Option<DataProcessingAgreement>>(None);
            }

            return Right<EncinaError, Option<DataProcessingAgreement>>(None);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "processor_agreements.store_error",
                message: $"Failed to get active Data Processing Agreement: {ex.Message}",
                details: new Dictionary<string, object?> { ["processorId"] = processorId }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> UpdateAsync(
        DataProcessingAgreement agreement,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agreement);

        try
        {
            // Check if agreement exists
            var existsSql = $"SELECT COUNT(1) FROM {_tableName} WHERE Id = @Id";
            using var existsCommand = _connection.CreateCommand();
            existsCommand.CommandText = existsSql;
            AddParameter(existsCommand, "@Id", agreement.Id);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var existsResult = await ExecuteScalarAsync(existsCommand, cancellationToken);
            var count = Convert.ToInt32(existsResult, CultureInfo.InvariantCulture);
            if (count == 0)
            {
                return Left(EncinaErrors.Create(
                    code: "processor_agreements.not_found",
                    message: $"Data Processing Agreement with ID '{agreement.Id}' not found.",
                    details: new Dictionary<string, object?> { ["dpaId"] = agreement.Id }));
            }

            var entity = DataProcessingAgreementMapper.ToEntity(agreement);
            var sql = $@"
                UPDATE {_tableName}
                SET ProcessorId = @ProcessorId,
                    StatusValue = @StatusValue,
                    SignedAtUtc = @SignedAtUtc,
                    ExpiresAtUtc = @ExpiresAtUtc,
                    HasSCCs = @HasSCCs,
                    ProcessingPurposesJson = @ProcessingPurposesJson,
                    ProcessOnDocumentedInstructions = @ProcessOnDocumentedInstructions,
                    ConfidentialityObligations = @ConfidentialityObligations,
                    SecurityMeasures = @SecurityMeasures,
                    SubProcessorRequirements = @SubProcessorRequirements,
                    DataSubjectRightsAssistance = @DataSubjectRightsAssistance,
                    ComplianceAssistance = @ComplianceAssistance,
                    DataDeletionOrReturn = @DataDeletionOrReturn,
                    AuditRights = @AuditRights,
                    TenantId = @TenantId,
                    ModuleId = @ModuleId,
                    LastUpdatedAtUtc = @LastUpdatedAtUtc
                WHERE Id = @Id";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddDPAParameters(command, entity);

            await ExecuteNonQueryAsync(command, cancellationToken);
            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "processor_agreements.store_error",
                message: $"Failed to update Data Processing Agreement: {ex.Message}",
                details: new Dictionary<string, object?> { ["dpaId"] = agreement.Id }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DataProcessingAgreement>>> GetByStatusAsync(
        DPAStatus status,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $@"
                SELECT Id, ProcessorId, StatusValue, SignedAtUtc, ExpiresAtUtc, HasSCCs, ProcessingPurposesJson,
                       ProcessOnDocumentedInstructions, ConfidentialityObligations, SecurityMeasures,
                       SubProcessorRequirements, DataSubjectRightsAssistance, ComplianceAssistance,
                       DataDeletionOrReturn, AuditRights, TenantId, ModuleId, CreatedAtUtc, LastUpdatedAtUtc
                FROM {_tableName}
                WHERE StatusValue = @StatusValue
                ORDER BY CreatedAtUtc";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@StatusValue", (int)status);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var results = new List<DataProcessingAgreement>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToDPAEntity(reader);
                var domain = DataProcessingAgreementMapper.ToDomain(entity);
                if (domain is not null)
                    results.Add(domain);
            }

            return Right<EncinaError, IReadOnlyList<DataProcessingAgreement>>(results);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "processor_agreements.store_error",
                message: $"Failed to get Data Processing Agreements by status: {ex.Message}",
                details: new Dictionary<string, object?> { ["status"] = status.ToString() }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DataProcessingAgreement>>> GetExpiringAsync(
        DateTimeOffset threshold,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $@"
                SELECT Id, ProcessorId, StatusValue, SignedAtUtc, ExpiresAtUtc, HasSCCs, ProcessingPurposesJson,
                       ProcessOnDocumentedInstructions, ConfidentialityObligations, SecurityMeasures,
                       SubProcessorRequirements, DataSubjectRightsAssistance, ComplianceAssistance,
                       DataDeletionOrReturn, AuditRights, TenantId, ModuleId, CreatedAtUtc, LastUpdatedAtUtc
                FROM {_tableName}
                WHERE StatusValue = @ActiveStatus AND ExpiresAtUtc IS NOT NULL AND ExpiresAtUtc <= @Threshold
                ORDER BY ExpiresAtUtc";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@ActiveStatus", (int)DPAStatus.Active);
            AddParameter(command, "@Threshold", threshold.ToString("O"));

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var results = new List<DataProcessingAgreement>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToDPAEntity(reader);
                var domain = DataProcessingAgreementMapper.ToDomain(entity);
                if (domain is not null)
                    results.Add(domain);
            }

            return Right<EncinaError, IReadOnlyList<DataProcessingAgreement>>(results);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "processor_agreements.store_error",
                message: $"Failed to get expiring Data Processing Agreements: {ex.Message}",
                details: new Dictionary<string, object?>()));
        }
    }

    private static void AddDPAParameters(IDbCommand command, DataProcessingAgreementEntity entity)
    {
        AddParameter(command, "@Id", entity.Id);
        AddParameter(command, "@ProcessorId", entity.ProcessorId);
        AddParameter(command, "@StatusValue", entity.StatusValue);
        AddParameter(command, "@SignedAtUtc", entity.SignedAtUtc.ToString("O"));
        AddParameter(command, "@ExpiresAtUtc", entity.ExpiresAtUtc?.ToString("O"));
        AddParameter(command, "@HasSCCs", entity.HasSCCs ? 1 : 0);
        AddParameter(command, "@ProcessingPurposesJson", entity.ProcessingPurposesJson);
        AddParameter(command, "@ProcessOnDocumentedInstructions", entity.ProcessOnDocumentedInstructions ? 1 : 0);
        AddParameter(command, "@ConfidentialityObligations", entity.ConfidentialityObligations ? 1 : 0);
        AddParameter(command, "@SecurityMeasures", entity.SecurityMeasures ? 1 : 0);
        AddParameter(command, "@SubProcessorRequirements", entity.SubProcessorRequirements ? 1 : 0);
        AddParameter(command, "@DataSubjectRightsAssistance", entity.DataSubjectRightsAssistance ? 1 : 0);
        AddParameter(command, "@ComplianceAssistance", entity.ComplianceAssistance ? 1 : 0);
        AddParameter(command, "@DataDeletionOrReturn", entity.DataDeletionOrReturn ? 1 : 0);
        AddParameter(command, "@AuditRights", entity.AuditRights ? 1 : 0);
        AddParameter(command, "@TenantId", entity.TenantId);
        AddParameter(command, "@ModuleId", entity.ModuleId);
        AddParameter(command, "@CreatedAtUtc", entity.CreatedAtUtc.ToString("O"));
        AddParameter(command, "@LastUpdatedAtUtc", entity.LastUpdatedAtUtc.ToString("O"));
    }

    private static DataProcessingAgreementEntity MapToDPAEntity(IDataReader reader)
    {
        var expiresAtUtcOrd = reader.GetOrdinal("ExpiresAtUtc");
        var tenantIdOrd = reader.GetOrdinal("TenantId");
        var moduleIdOrd = reader.GetOrdinal("ModuleId");

        return new DataProcessingAgreementEntity
        {
            Id = reader.GetString(reader.GetOrdinal("Id")),
            ProcessorId = reader.GetString(reader.GetOrdinal("ProcessorId")),
            StatusValue = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("StatusValue")), CultureInfo.InvariantCulture),
            SignedAtUtc = DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("SignedAtUtc")), null, DateTimeStyles.RoundtripKind),
            ExpiresAtUtc = reader.IsDBNull(expiresAtUtcOrd)
                ? null
                : DateTimeOffset.Parse(reader.GetString(expiresAtUtcOrd), null, DateTimeStyles.RoundtripKind),
            HasSCCs = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("HasSCCs")), CultureInfo.InvariantCulture) != 0,
            ProcessingPurposesJson = reader.GetString(reader.GetOrdinal("ProcessingPurposesJson")),
            ProcessOnDocumentedInstructions = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("ProcessOnDocumentedInstructions")), CultureInfo.InvariantCulture) != 0,
            ConfidentialityObligations = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("ConfidentialityObligations")), CultureInfo.InvariantCulture) != 0,
            SecurityMeasures = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("SecurityMeasures")), CultureInfo.InvariantCulture) != 0,
            SubProcessorRequirements = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("SubProcessorRequirements")), CultureInfo.InvariantCulture) != 0,
            DataSubjectRightsAssistance = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("DataSubjectRightsAssistance")), CultureInfo.InvariantCulture) != 0,
            ComplianceAssistance = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("ComplianceAssistance")), CultureInfo.InvariantCulture) != 0,
            DataDeletionOrReturn = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("DataDeletionOrReturn")), CultureInfo.InvariantCulture) != 0,
            AuditRights = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("AuditRights")), CultureInfo.InvariantCulture) != 0,
            TenantId = reader.IsDBNull(tenantIdOrd) ? null : reader.GetString(tenantIdOrd),
            ModuleId = reader.IsDBNull(moduleIdOrd) ? null : reader.GetString(moduleIdOrd),
            CreatedAtUtc = DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("CreatedAtUtc")), null, DateTimeStyles.RoundtripKind),
            LastUpdatedAtUtc = DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("LastUpdatedAtUtc")), null, DateTimeStyles.RoundtripKind)
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
        if (command is SqliteCommand sqlCommand)
            return await sqlCommand.ExecuteReaderAsync(cancellationToken);

        return await Task.Run(command.ExecuteReader, cancellationToken);
    }

    private static async Task<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is SqliteCommand sqlCommand)
            return await sqlCommand.ExecuteNonQueryAsync(cancellationToken);

        return await Task.Run(command.ExecuteNonQuery, cancellationToken);
    }

    private static async Task<object?> ExecuteScalarAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is SqliteCommand sqlCommand)
            return await sqlCommand.ExecuteScalarAsync(cancellationToken);

        return await Task.Run(command.ExecuteScalar, cancellationToken);
    }

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is SqliteDataReader sqlReader)
            return await sqlReader.ReadAsync(cancellationToken);

        return await Task.Run(reader.Read, cancellationToken);
    }
}

using System.Data;

using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Messaging;

using LanguageExt;

using Microsoft.Data.SqlClient;

using static LanguageExt.Prelude;

namespace Encina.ADO.SqlServer.ProcessorAgreements;

/// <summary>
/// ADO.NET implementation of <see cref="IDPAStore"/> for SQL Server.
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
            // Check if already exists.
            var existsSql = $"SELECT COUNT(1) FROM {_tableName} WHERE Id = @Id";
            using (var existsCmd = _connection.CreateCommand())
            {
                existsCmd.CommandText = existsSql;
                AddParameter(existsCmd, "@Id", agreement.Id);

                if (_connection.State != ConnectionState.Open)
                    await OpenConnectionAsync(cancellationToken);

                var count = await ExecuteScalarAsync(existsCmd, cancellationToken);
                if (count is not null && Convert.ToInt32(count, System.Globalization.CultureInfo.InvariantCulture) > 0)
                {
                    return Left(EncinaErrors.Create(
                        code: "processor_agreements.store_error",
                        message: $"Agreement '{agreement.Id}' already exists.",
                        details: new Dictionary<string, object?> { ["dpaId"] = agreement.Id }));
                }
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
            AddParameter(command, "@Id", entity.Id);
            AddParameter(command, "@ProcessorId", entity.ProcessorId);
            AddParameter(command, "@StatusValue", entity.StatusValue);
            AddParameter(command, "@SignedAtUtc", entity.SignedAtUtc.UtcDateTime);
            AddParameter(command, "@ExpiresAtUtc", entity.ExpiresAtUtc?.UtcDateTime);
            AddParameter(command, "@HasSCCs", entity.HasSCCs);
            AddParameter(command, "@ProcessingPurposesJson", entity.ProcessingPurposesJson);
            AddParameter(command, "@ProcessOnDocumentedInstructions", entity.ProcessOnDocumentedInstructions);
            AddParameter(command, "@ConfidentialityObligations", entity.ConfidentialityObligations);
            AddParameter(command, "@SecurityMeasures", entity.SecurityMeasures);
            AddParameter(command, "@SubProcessorRequirements", entity.SubProcessorRequirements);
            AddParameter(command, "@DataSubjectRightsAssistance", entity.DataSubjectRightsAssistance);
            AddParameter(command, "@ComplianceAssistance", entity.ComplianceAssistance);
            AddParameter(command, "@DataDeletionOrReturn", entity.DataDeletionOrReturn);
            AddParameter(command, "@AuditRights", entity.AuditRights);
            AddParameter(command, "@TenantId", entity.TenantId);
            AddParameter(command, "@ModuleId", entity.ModuleId);
            AddParameter(command, "@CreatedAtUtc", entity.CreatedAtUtc.UtcDateTime);
            AddParameter(command, "@LastUpdatedAtUtc", entity.LastUpdatedAtUtc.UtcDateTime);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "processor_agreements.store_error",
                message: $"Failed to add DPA: {ex.Message}",
                details: new Dictionary<string, object?> { ["dpaId"] = agreement.Id }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<DataProcessingAgreement>>> GetByIdAsync(
        string dpaId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dpaId);

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
                var entity = MapToEntity(reader);
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
                message: $"Failed to get DPA by ID: {ex.Message}",
                details: new Dictionary<string, object?> { ["dpaId"] = dpaId }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DataProcessingAgreement>>> GetByProcessorIdAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processorId);

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
                var entity = MapToEntity(reader);
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
                message: $"Failed to get DPAs by processor ID: {ex.Message}",
                details: new Dictionary<string, object?> { ["processorId"] = processorId }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<DataProcessingAgreement>>> GetActiveByProcessorIdAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processorId);

        try
        {
            var sql = $@"
                SELECT Id, ProcessorId, StatusValue, SignedAtUtc, ExpiresAtUtc, HasSCCs, ProcessingPurposesJson,
                       ProcessOnDocumentedInstructions, ConfidentialityObligations, SecurityMeasures,
                       SubProcessorRequirements, DataSubjectRightsAssistance, ComplianceAssistance,
                       DataDeletionOrReturn, AuditRights, TenantId, ModuleId, CreatedAtUtc, LastUpdatedAtUtc
                FROM {_tableName}
                WHERE ProcessorId = @ProcessorId AND StatusValue = @StatusValue";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@ProcessorId", processorId);
            AddParameter(command, "@StatusValue", (int)DPAStatus.Active);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            if (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToEntity(reader);
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
                message: $"Failed to get active DPA by processor ID: {ex.Message}",
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
            // Check if exists.
            var existsSql = $"SELECT COUNT(1) FROM {_tableName} WHERE Id = @Id";
            using (var existsCmd = _connection.CreateCommand())
            {
                existsCmd.CommandText = existsSql;
                AddParameter(existsCmd, "@Id", agreement.Id);

                if (_connection.State != ConnectionState.Open)
                    await OpenConnectionAsync(cancellationToken);

                var count = await ExecuteScalarAsync(existsCmd, cancellationToken);
                if (count is null || Convert.ToInt32(count, System.Globalization.CultureInfo.InvariantCulture) == 0)
                {
                    return ProcessorAgreementErrors.DPANotFound(agreement.Id);
                }
            }

            var entity = DataProcessingAgreementMapper.ToEntity(agreement);
            var sql = $@"
                UPDATE {_tableName}
                SET ProcessorId = @ProcessorId, StatusValue = @StatusValue,
                    SignedAtUtc = @SignedAtUtc, ExpiresAtUtc = @ExpiresAtUtc,
                    HasSCCs = @HasSCCs, ProcessingPurposesJson = @ProcessingPurposesJson,
                    ProcessOnDocumentedInstructions = @ProcessOnDocumentedInstructions,
                    ConfidentialityObligations = @ConfidentialityObligations,
                    SecurityMeasures = @SecurityMeasures,
                    SubProcessorRequirements = @SubProcessorRequirements,
                    DataSubjectRightsAssistance = @DataSubjectRightsAssistance,
                    ComplianceAssistance = @ComplianceAssistance,
                    DataDeletionOrReturn = @DataDeletionOrReturn,
                    AuditRights = @AuditRights,
                    TenantId = @TenantId, ModuleId = @ModuleId,
                    CreatedAtUtc = @CreatedAtUtc, LastUpdatedAtUtc = @LastUpdatedAtUtc
                WHERE Id = @Id";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", entity.Id);
            AddParameter(command, "@ProcessorId", entity.ProcessorId);
            AddParameter(command, "@StatusValue", entity.StatusValue);
            AddParameter(command, "@SignedAtUtc", entity.SignedAtUtc.UtcDateTime);
            AddParameter(command, "@ExpiresAtUtc", entity.ExpiresAtUtc?.UtcDateTime);
            AddParameter(command, "@HasSCCs", entity.HasSCCs);
            AddParameter(command, "@ProcessingPurposesJson", entity.ProcessingPurposesJson);
            AddParameter(command, "@ProcessOnDocumentedInstructions", entity.ProcessOnDocumentedInstructions);
            AddParameter(command, "@ConfidentialityObligations", entity.ConfidentialityObligations);
            AddParameter(command, "@SecurityMeasures", entity.SecurityMeasures);
            AddParameter(command, "@SubProcessorRequirements", entity.SubProcessorRequirements);
            AddParameter(command, "@DataSubjectRightsAssistance", entity.DataSubjectRightsAssistance);
            AddParameter(command, "@ComplianceAssistance", entity.ComplianceAssistance);
            AddParameter(command, "@DataDeletionOrReturn", entity.DataDeletionOrReturn);
            AddParameter(command, "@AuditRights", entity.AuditRights);
            AddParameter(command, "@TenantId", entity.TenantId);
            AddParameter(command, "@ModuleId", entity.ModuleId);
            AddParameter(command, "@CreatedAtUtc", entity.CreatedAtUtc.UtcDateTime);
            AddParameter(command, "@LastUpdatedAtUtc", entity.LastUpdatedAtUtc.UtcDateTime);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "processor_agreements.store_error",
                message: $"Failed to update DPA: {ex.Message}",
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
                var entity = MapToEntity(reader);
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
                message: $"Failed to get DPAs by status: {ex.Message}",
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
                WHERE StatusValue = @StatusValue AND ExpiresAtUtc IS NOT NULL AND ExpiresAtUtc <= @Threshold
                ORDER BY ExpiresAtUtc";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@StatusValue", (int)DPAStatus.Active);
            AddParameter(command, "@Threshold", threshold.UtcDateTime);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var results = new List<DataProcessingAgreement>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToEntity(reader);
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
                message: $"Failed to get expiring DPAs: {ex.Message}",
                details: new Dictionary<string, object?>()));
        }
    }

    private static DataProcessingAgreementEntity MapToEntity(IDataReader reader)
    {
        var expiresAtUtcOrd = reader.GetOrdinal("ExpiresAtUtc");
        var tenantIdOrd = reader.GetOrdinal("TenantId");
        var moduleIdOrd = reader.GetOrdinal("ModuleId");

        return new DataProcessingAgreementEntity
        {
            Id = reader.GetString(reader.GetOrdinal("Id")),
            ProcessorId = reader.GetString(reader.GetOrdinal("ProcessorId")),
            StatusValue = reader.GetInt32(reader.GetOrdinal("StatusValue")),
            SignedAtUtc = new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("SignedAtUtc")), TimeSpan.Zero),
            ExpiresAtUtc = reader.IsDBNull(expiresAtUtcOrd)
                ? null
                : new DateTimeOffset(reader.GetDateTime(expiresAtUtcOrd), TimeSpan.Zero),
            HasSCCs = reader.GetBoolean(reader.GetOrdinal("HasSCCs")),
            ProcessingPurposesJson = reader.GetString(reader.GetOrdinal("ProcessingPurposesJson")),
            ProcessOnDocumentedInstructions = reader.GetBoolean(reader.GetOrdinal("ProcessOnDocumentedInstructions")),
            ConfidentialityObligations = reader.GetBoolean(reader.GetOrdinal("ConfidentialityObligations")),
            SecurityMeasures = reader.GetBoolean(reader.GetOrdinal("SecurityMeasures")),
            SubProcessorRequirements = reader.GetBoolean(reader.GetOrdinal("SubProcessorRequirements")),
            DataSubjectRightsAssistance = reader.GetBoolean(reader.GetOrdinal("DataSubjectRightsAssistance")),
            ComplianceAssistance = reader.GetBoolean(reader.GetOrdinal("ComplianceAssistance")),
            DataDeletionOrReturn = reader.GetBoolean(reader.GetOrdinal("DataDeletionOrReturn")),
            AuditRights = reader.GetBoolean(reader.GetOrdinal("AuditRights")),
            TenantId = reader.IsDBNull(tenantIdOrd) ? null : reader.GetString(tenantIdOrd),
            ModuleId = reader.IsDBNull(moduleIdOrd) ? null : reader.GetString(moduleIdOrd),
            CreatedAtUtc = new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("CreatedAtUtc")), TimeSpan.Zero),
            LastUpdatedAtUtc = new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("LastUpdatedAtUtc")), TimeSpan.Zero)
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

    private static async Task<object?> ExecuteScalarAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is SqlCommand sqlCommand)
            return await sqlCommand.ExecuteScalarAsync(cancellationToken);

        return await Task.Run(command.ExecuteScalar, cancellationToken);
    }

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is SqlDataReader sqlReader)
            return await sqlReader.ReadAsync(cancellationToken);

        return await Task.Run(reader.Read, cancellationToken);
    }
}

using System.Data;
using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Messaging;
using LanguageExt;
using Npgsql;
using static LanguageExt.Prelude;

namespace Encina.ADO.PostgreSQL.ProcessorAgreements;

/// <summary>
/// ADO.NET implementation of <see cref="IDPAStore"/> for PostgreSQL.
/// Manages Data Processing Agreement persistence per GDPR Article 28(3).
/// </summary>
/// <remarks>
/// <para>
/// PostgreSQL-specific considerations:
/// <list type="bullet">
/// <item><description>DateTimeOffset values are stored as TIMESTAMPTZ via <c>.UtcDateTime</c>.</description></item>
/// <item><description>DateTimeOffset values are read back using <c>new DateTimeOffset(reader.GetDateTime(ord), TimeSpan.Zero)</c>.</description></item>
/// <item><description>Boolean values are read using <c>reader.GetBoolean(ord)</c> (PostgreSQL native BOOLEAN type).</description></item>
/// <item><description>Processing purposes are stored as TEXT (JSON-serialized list).</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class DPAStoreADO : IDPAStore
{
    private const string AllColumns =
        "id, processorid, statusvalue, signedatutc, expiresatutc, hassccs, processingpurposesjson, " +
        "processondocumentedinstructions, confidentialityobligations, securitymeasures, " +
        "subprocessorrequirements, datasubjectrightsassistance, complianceassistance, " +
        "datadeletionorreturn, auditrights, tenantid, moduleid, createdatutc, lastupdatedatutc";

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
            var entity = DataProcessingAgreementMapper.ToEntity(agreement);
            var sql = $@"
                INSERT INTO {_tableName}
                ({AllColumns})
                VALUES
                (@Id, @ProcessorId, @StatusValue, @SignedAtUtc, @ExpiresAtUtc, @HasSCCs, @ProcessingPurposesJson,
                 @ProcessOnDocumentedInstructions, @ConfidentialityObligations, @SecurityMeasures,
                 @SubProcessorRequirements, @DataSubjectRightsAssistance, @ComplianceAssistance,
                 @DataDeletionOrReturn, @AuditRights, @TenantId, @ModuleId, @CreatedAtUtc, @LastUpdatedAtUtc)";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddAllParameters(command, entity);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(ProcessorAgreementErrors.StoreError(
                "AddDPA", ex.Message, ex));
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
                SELECT {AllColumns}
                FROM {_tableName}
                WHERE id = @Id";

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
            return Left(ProcessorAgreementErrors.StoreError(
                "GetDPAById", ex.Message, ex));
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
                SELECT {AllColumns}
                FROM {_tableName}
                WHERE processorid = @ProcessorId
                ORDER BY createdatutc DESC";

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
            return Left(ProcessorAgreementErrors.StoreError(
                "GetDPAByProcessorId", ex.Message, ex));
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
                SELECT {AllColumns}
                FROM {_tableName}
                WHERE processorid = @ProcessorId AND statusvalue = @ActiveStatus
                LIMIT 1";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@ProcessorId", processorId);
            AddParameter(command, "@ActiveStatus", (int)DPAStatus.Active);

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
            return Left(ProcessorAgreementErrors.StoreError(
                "GetActiveDPAByProcessorId", ex.Message, ex));
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
            var entity = DataProcessingAgreementMapper.ToEntity(agreement);
            var sql = $@"
                UPDATE {_tableName}
                SET processorid = @ProcessorId, statusvalue = @StatusValue, signedatutc = @SignedAtUtc,
                    expiresatutc = @ExpiresAtUtc, hassccs = @HasSCCs, processingpurposesjson = @ProcessingPurposesJson,
                    processondocumentedinstructions = @ProcessOnDocumentedInstructions,
                    confidentialityobligations = @ConfidentialityObligations,
                    securitymeasures = @SecurityMeasures,
                    subprocessorrequirements = @SubProcessorRequirements,
                    datasubjectrightsassistance = @DataSubjectRightsAssistance,
                    complianceassistance = @ComplianceAssistance,
                    datadeletionorreturn = @DataDeletionOrReturn,
                    auditrights = @AuditRights,
                    tenantid = @TenantId, moduleid = @ModuleId,
                    lastupdatedatutc = @LastUpdatedAtUtc
                WHERE id = @Id";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddAllParameters(command, entity);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var affected = await ExecuteNonQueryAsync(command, cancellationToken);
            return affected > 0
                ? Right(Unit.Default)
                : Left(ProcessorAgreementErrors.DPANotFound(agreement.Id));
        }
        catch (Exception ex)
        {
            return Left(ProcessorAgreementErrors.StoreError(
                "UpdateDPA", ex.Message, ex));
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
                SELECT {AllColumns}
                FROM {_tableName}
                WHERE statusvalue = @StatusValue
                ORDER BY createdatutc DESC";

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
            return Left(ProcessorAgreementErrors.StoreError(
                "GetDPAByStatus", ex.Message, ex));
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
                SELECT {AllColumns}
                FROM {_tableName}
                WHERE statusvalue = @ActiveStatus
                  AND expiresatutc IS NOT NULL
                  AND expiresatutc <= @Threshold
                ORDER BY expiresatutc ASC";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@ActiveStatus", (int)DPAStatus.Active);
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
            return Left(ProcessorAgreementErrors.StoreError(
                "GetExpiringDPAs", ex.Message, ex));
        }
    }

    private static DataProcessingAgreementEntity MapToEntity(IDataReader reader)
    {
        var expiresAtUtcOrd = reader.GetOrdinal("expiresatutc");
        var tenantIdOrd = reader.GetOrdinal("tenantid");
        var moduleIdOrd = reader.GetOrdinal("moduleid");

        return new DataProcessingAgreementEntity
        {
            Id = reader.GetString(reader.GetOrdinal("id")),
            ProcessorId = reader.GetString(reader.GetOrdinal("processorid")),
            StatusValue = reader.GetInt32(reader.GetOrdinal("statusvalue")),
            SignedAtUtc = new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("signedatutc")), TimeSpan.Zero),
            ExpiresAtUtc = reader.IsDBNull(expiresAtUtcOrd)
                ? null
                : new DateTimeOffset(reader.GetDateTime(expiresAtUtcOrd), TimeSpan.Zero),
            HasSCCs = reader.GetBoolean(reader.GetOrdinal("hassccs")),
            ProcessingPurposesJson = reader.GetString(reader.GetOrdinal("processingpurposesjson")),
            ProcessOnDocumentedInstructions = reader.GetBoolean(reader.GetOrdinal("processondocumentedinstructions")),
            ConfidentialityObligations = reader.GetBoolean(reader.GetOrdinal("confidentialityobligations")),
            SecurityMeasures = reader.GetBoolean(reader.GetOrdinal("securitymeasures")),
            SubProcessorRequirements = reader.GetBoolean(reader.GetOrdinal("subprocessorrequirements")),
            DataSubjectRightsAssistance = reader.GetBoolean(reader.GetOrdinal("datasubjectrightsassistance")),
            ComplianceAssistance = reader.GetBoolean(reader.GetOrdinal("complianceassistance")),
            DataDeletionOrReturn = reader.GetBoolean(reader.GetOrdinal("datadeletionorreturn")),
            AuditRights = reader.GetBoolean(reader.GetOrdinal("auditrights")),
            TenantId = reader.IsDBNull(tenantIdOrd) ? null : reader.GetString(tenantIdOrd),
            ModuleId = reader.IsDBNull(moduleIdOrd) ? null : reader.GetString(moduleIdOrd),
            CreatedAtUtc = new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("createdatutc")), TimeSpan.Zero),
            LastUpdatedAtUtc = new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("lastupdatedatutc")), TimeSpan.Zero)
        };
    }

    private static void AddAllParameters(IDbCommand command, DataProcessingAgreementEntity entity)
    {
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
        if (command is NpgsqlCommand npgsqlCommand)
            return await npgsqlCommand.ExecuteReaderAsync(cancellationToken);

        return await Task.Run(command.ExecuteReader, cancellationToken);
    }

    private static async Task<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is NpgsqlCommand npgsqlCommand)
            return await npgsqlCommand.ExecuteNonQueryAsync(cancellationToken);

        return await Task.Run(command.ExecuteNonQuery, cancellationToken);
    }

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is NpgsqlDataReader npgsqlReader)
            return await npgsqlReader.ReadAsync(cancellationToken);

        return await Task.Run(reader.Read, cancellationToken);
    }
}

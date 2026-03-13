using System.Data;
using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Messaging;
using LanguageExt;
using Npgsql;
using static LanguageExt.Prelude;

namespace Encina.ADO.PostgreSQL.ProcessorAgreements;

/// <summary>
/// ADO.NET implementation of <see cref="IProcessorRegistry"/> for PostgreSQL.
/// Manages processor identity and hierarchical relationships per GDPR Article 28.
/// </summary>
/// <remarks>
/// <para>
/// PostgreSQL-specific considerations:
/// <list type="bullet">
/// <item><description>DateTimeOffset values are stored as TIMESTAMPTZ via <c>.UtcDateTime</c>.</description></item>
/// <item><description>DateTimeOffset values are read back using <c>new DateTimeOffset(reader.GetDateTime(ord), TimeSpan.Zero)</c>.</description></item>
/// <item><description>Boolean values are read using <c>reader.GetBoolean(ord)</c> (PostgreSQL native BOOLEAN type).</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class ProcessorRegistryADO : IProcessorRegistry
{
    private const int MaxSubProcessorDepth = 5;

    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessorRegistryADO"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The processors table name (default: Processors).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    public ProcessorRegistryADO(
        IDbConnection connection,
        string tableName = "Processors",
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RegisterProcessorAsync(
        Processor processor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processor);

        try
        {
            var entity = ProcessorMapper.ToEntity(processor);
            var sql = $@"
                INSERT INTO {_tableName}
                (id, name, country, contactemail, parentprocessorid, depth, subprocessorauthorizationtypevalue, tenantid, moduleid, createdatutc, lastupdatedatutc)
                VALUES
                (@Id, @Name, @Country, @ContactEmail, @ParentProcessorId, @Depth, @SubProcessorAuthorizationTypeValue, @TenantId, @ModuleId, @CreatedAtUtc, @LastUpdatedAtUtc)";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", entity.Id);
            AddParameter(command, "@Name", entity.Name);
            AddParameter(command, "@Country", entity.Country);
            AddParameter(command, "@ContactEmail", entity.ContactEmail);
            AddParameter(command, "@ParentProcessorId", entity.ParentProcessorId);
            AddParameter(command, "@Depth", entity.Depth);
            AddParameter(command, "@SubProcessorAuthorizationTypeValue", entity.SubProcessorAuthorizationTypeValue);
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
            return Left(ProcessorAgreementErrors.StoreError(
                "RegisterProcessor", ex.Message, ex));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<Processor>>> GetProcessorAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processorId);

        try
        {
            var sql = $@"
                SELECT id, name, country, contactemail, parentprocessorid, depth, subprocessorauthorizationtypevalue, tenantid, moduleid, createdatutc, lastupdatedatutc
                FROM {_tableName}
                WHERE id = @Id";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", processorId);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            if (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToEntity(reader);
                var domain = ProcessorMapper.ToDomain(entity);
                return domain is not null
                    ? Right<EncinaError, Option<Processor>>(Some(domain))
                    : Right<EncinaError, Option<Processor>>(None);
            }

            return Right<EncinaError, Option<Processor>>(None);
        }
        catch (Exception ex)
        {
            return Left(ProcessorAgreementErrors.StoreError(
                "GetProcessor", ex.Message, ex));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<Processor>>> GetAllProcessorsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $@"
                SELECT id, name, country, contactemail, parentprocessorid, depth, subprocessorauthorizationtypevalue, tenantid, moduleid, createdatutc, lastupdatedatutc
                FROM {_tableName}";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var results = new List<Processor>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToEntity(reader);
                var domain = ProcessorMapper.ToDomain(entity);
                if (domain is not null)
                    results.Add(domain);
            }

            return Right<EncinaError, IReadOnlyList<Processor>>(results);
        }
        catch (Exception ex)
        {
            return Left(ProcessorAgreementErrors.StoreError(
                "GetAllProcessors", ex.Message, ex));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> UpdateProcessorAsync(
        Processor processor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processor);

        try
        {
            var entity = ProcessorMapper.ToEntity(processor);
            var sql = $@"
                UPDATE {_tableName}
                SET name = @Name, country = @Country, contactemail = @ContactEmail,
                    parentprocessorid = @ParentProcessorId, depth = @Depth,
                    subprocessorauthorizationtypevalue = @SubProcessorAuthorizationTypeValue,
                    tenantid = @TenantId, moduleid = @ModuleId,
                    lastupdatedatutc = @LastUpdatedAtUtc
                WHERE id = @Id";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", entity.Id);
            AddParameter(command, "@Name", entity.Name);
            AddParameter(command, "@Country", entity.Country);
            AddParameter(command, "@ContactEmail", entity.ContactEmail);
            AddParameter(command, "@ParentProcessorId", entity.ParentProcessorId);
            AddParameter(command, "@Depth", entity.Depth);
            AddParameter(command, "@SubProcessorAuthorizationTypeValue", entity.SubProcessorAuthorizationTypeValue);
            AddParameter(command, "@TenantId", entity.TenantId);
            AddParameter(command, "@ModuleId", entity.ModuleId);
            AddParameter(command, "@LastUpdatedAtUtc", entity.LastUpdatedAtUtc.UtcDateTime);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var affected = await ExecuteNonQueryAsync(command, cancellationToken);
            return affected > 0
                ? Right(Unit.Default)
                : Left(ProcessorAgreementErrors.NotFound(processor.Id));
        }
        catch (Exception ex)
        {
            return Left(ProcessorAgreementErrors.StoreError(
                "UpdateProcessor", ex.Message, ex));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RemoveProcessorAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processorId);

        try
        {
            var sql = $@"DELETE FROM {_tableName} WHERE id = @Id";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", processorId);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var affected = await ExecuteNonQueryAsync(command, cancellationToken);
            return affected > 0
                ? Right(Unit.Default)
                : Left(ProcessorAgreementErrors.NotFound(processorId));
        }
        catch (Exception ex)
        {
            return Left(ProcessorAgreementErrors.StoreError(
                "RemoveProcessor", ex.Message, ex));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<Processor>>> GetSubProcessorsAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processorId);

        try
        {
            var sql = $@"
                SELECT id, name, country, contactemail, parentprocessorid, depth, subprocessorauthorizationtypevalue, tenantid, moduleid, createdatutc, lastupdatedatutc
                FROM {_tableName}
                WHERE parentprocessorid = @ParentProcessorId";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@ParentProcessorId", processorId);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var results = new List<Processor>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToEntity(reader);
                var domain = ProcessorMapper.ToDomain(entity);
                if (domain is not null)
                    results.Add(domain);
            }

            return Right<EncinaError, IReadOnlyList<Processor>>(results);
        }
        catch (Exception ex)
        {
            return Left(ProcessorAgreementErrors.StoreError(
                "GetSubProcessors", ex.Message, ex));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<Processor>>> GetFullSubProcessorChainAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processorId);

        try
        {
            // BFS traversal in memory bounded by MaxSubProcessorDepth
            var allDescendants = new List<Processor>();
            var queue = new Queue<string>();
            queue.Enqueue(processorId);
            var visited = new System.Collections.Generic.HashSet<string> { processorId };
            var currentDepth = 0;

            while (queue.Count > 0 && currentDepth < MaxSubProcessorDepth)
            {
                var levelSize = queue.Count;
                for (var i = 0; i < levelSize; i++)
                {
                    var parentId = queue.Dequeue();
                    var childrenResult = await GetSubProcessorsAsync(parentId, cancellationToken);

                    var children = childrenResult.Match(
                        Right: list => list,
                        Left: _ => (IReadOnlyList<Processor>)[]);

                    foreach (var child in children)
                    {
                        if (visited.Add(child.Id))
                        {
                            allDescendants.Add(child);
                            queue.Enqueue(child.Id);
                        }
                    }
                }

                currentDepth++;
            }

            return Right<EncinaError, IReadOnlyList<Processor>>(allDescendants);
        }
        catch (Exception ex)
        {
            return Left(ProcessorAgreementErrors.StoreError(
                "GetFullSubProcessorChain", ex.Message, ex));
        }
    }

    private static ProcessorEntity MapToEntity(IDataReader reader)
    {
        var contactEmailOrd = reader.GetOrdinal("contactemail");
        var parentProcessorIdOrd = reader.GetOrdinal("parentprocessorid");
        var tenantIdOrd = reader.GetOrdinal("tenantid");
        var moduleIdOrd = reader.GetOrdinal("moduleid");

        return new ProcessorEntity
        {
            Id = reader.GetString(reader.GetOrdinal("id")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Country = reader.GetString(reader.GetOrdinal("country")),
            ContactEmail = reader.IsDBNull(contactEmailOrd) ? null : reader.GetString(contactEmailOrd),
            ParentProcessorId = reader.IsDBNull(parentProcessorIdOrd) ? null : reader.GetString(parentProcessorIdOrd),
            Depth = reader.GetInt32(reader.GetOrdinal("depth")),
            SubProcessorAuthorizationTypeValue = reader.GetInt32(reader.GetOrdinal("subprocessorauthorizationtypevalue")),
            TenantId = reader.IsDBNull(tenantIdOrd) ? null : reader.GetString(tenantIdOrd),
            ModuleId = reader.IsDBNull(moduleIdOrd) ? null : reader.GetString(moduleIdOrd),
            CreatedAtUtc = new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("createdatutc")), TimeSpan.Zero),
            LastUpdatedAtUtc = new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("lastupdatedatutc")), TimeSpan.Zero)
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

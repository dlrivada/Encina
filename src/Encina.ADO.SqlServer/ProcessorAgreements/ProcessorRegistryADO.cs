using System.Data;

using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Messaging;

using LanguageExt;

using Microsoft.Data.SqlClient;

using static LanguageExt.Prelude;

namespace Encina.ADO.SqlServer.ProcessorAgreements;

/// <summary>
/// ADO.NET implementation of <see cref="IProcessorRegistry"/> for SQL Server.
/// Manages processor identity and hierarchy per GDPR Article 28.
/// </summary>
public sealed class ProcessorRegistryADO : IProcessorRegistry
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Maximum allowed depth for sub-processor chains.
    /// </summary>
    internal const int MaxSubProcessorDepth = 5;

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
            // Validate depth constraint.
            if (processor.Depth > MaxSubProcessorDepth)
            {
                return ProcessorAgreementErrors.SubProcessorDepthExceeded(
                    processor.Id, processor.Depth, MaxSubProcessorDepth);
            }

            // Check if already exists.
            var existsSql = $"SELECT COUNT(1) FROM {_tableName} WHERE Id = @Id";
            using (var existsCmd = _connection.CreateCommand())
            {
                existsCmd.CommandText = existsSql;
                AddParameter(existsCmd, "@Id", processor.Id);

                if (_connection.State != ConnectionState.Open)
                    await OpenConnectionAsync(cancellationToken);

                var count = await ExecuteScalarAsync(existsCmd, cancellationToken);
                if (count is not null && Convert.ToInt32(count, System.Globalization.CultureInfo.InvariantCulture) > 0)
                {
                    return ProcessorAgreementErrors.AlreadyExists(processor.Id);
                }
            }

            var entity = ProcessorMapper.ToEntity(processor);
            var sql = $@"
                INSERT INTO {_tableName}
                (Id, Name, Country, ContactEmail, ParentProcessorId, Depth, SubProcessorAuthorizationTypeValue, TenantId, ModuleId, CreatedAtUtc, LastUpdatedAtUtc)
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
            return Left(EncinaErrors.Create(
                code: "processor_agreements.store_error",
                message: $"Failed to register processor: {ex.Message}",
                details: new Dictionary<string, object?> { ["processorId"] = processor.Id }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<Processor>>> GetProcessorAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processorId);

        try
        {
            var sql = $@"
                SELECT Id, Name, Country, ContactEmail, ParentProcessorId, Depth, SubProcessorAuthorizationTypeValue, TenantId, ModuleId, CreatedAtUtc, LastUpdatedAtUtc
                FROM {_tableName}
                WHERE Id = @Id";

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
            return Left(EncinaErrors.Create(
                code: "processor_agreements.store_error",
                message: $"Failed to get processor: {ex.Message}",
                details: new Dictionary<string, object?> { ["processorId"] = processorId }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<Processor>>> GetAllProcessorsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $@"
                SELECT Id, Name, Country, ContactEmail, ParentProcessorId, Depth, SubProcessorAuthorizationTypeValue, TenantId, ModuleId, CreatedAtUtc, LastUpdatedAtUtc
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
            return Left(EncinaErrors.Create(
                code: "processor_agreements.store_error",
                message: $"Failed to get all processors: {ex.Message}",
                details: new Dictionary<string, object?>()));
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
            // Check if exists.
            var existsSql = $"SELECT COUNT(1) FROM {_tableName} WHERE Id = @Id";
            using (var existsCmd = _connection.CreateCommand())
            {
                existsCmd.CommandText = existsSql;
                AddParameter(existsCmd, "@Id", processor.Id);

                if (_connection.State != ConnectionState.Open)
                    await OpenConnectionAsync(cancellationToken);

                var count = await ExecuteScalarAsync(existsCmd, cancellationToken);
                if (count is null || Convert.ToInt32(count, System.Globalization.CultureInfo.InvariantCulture) == 0)
                {
                    return ProcessorAgreementErrors.NotFound(processor.Id);
                }
            }

            var entity = ProcessorMapper.ToEntity(processor);
            var sql = $@"
                UPDATE {_tableName}
                SET Name = @Name, Country = @Country, ContactEmail = @ContactEmail,
                    ParentProcessorId = @ParentProcessorId, Depth = @Depth,
                    SubProcessorAuthorizationTypeValue = @SubProcessorAuthorizationTypeValue,
                    TenantId = @TenantId, ModuleId = @ModuleId,
                    CreatedAtUtc = @CreatedAtUtc, LastUpdatedAtUtc = @LastUpdatedAtUtc
                WHERE Id = @Id";

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
            return Left(EncinaErrors.Create(
                code: "processor_agreements.store_error",
                message: $"Failed to update processor: {ex.Message}",
                details: new Dictionary<string, object?> { ["processorId"] = processor.Id }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RemoveProcessorAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processorId);

        try
        {
            // Check if exists.
            var existsSql = $"SELECT COUNT(1) FROM {_tableName} WHERE Id = @Id";
            using (var existsCmd = _connection.CreateCommand())
            {
                existsCmd.CommandText = existsSql;
                AddParameter(existsCmd, "@Id", processorId);

                if (_connection.State != ConnectionState.Open)
                    await OpenConnectionAsync(cancellationToken);

                var count = await ExecuteScalarAsync(existsCmd, cancellationToken);
                if (count is null || Convert.ToInt32(count, System.Globalization.CultureInfo.InvariantCulture) == 0)
                {
                    return ProcessorAgreementErrors.NotFound(processorId);
                }
            }

            var sql = $"DELETE FROM {_tableName} WHERE Id = @Id";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", processorId);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "processor_agreements.store_error",
                message: $"Failed to remove processor: {ex.Message}",
                details: new Dictionary<string, object?> { ["processorId"] = processorId }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<Processor>>> GetSubProcessorsAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processorId);

        try
        {
            var sql = $@"
                SELECT Id, Name, Country, ContactEmail, ParentProcessorId, Depth, SubProcessorAuthorizationTypeValue, TenantId, ModuleId, CreatedAtUtc, LastUpdatedAtUtc
                FROM {_tableName}
                WHERE ParentProcessorId = @ProcessorId";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@ProcessorId", processorId);

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
            return Left(EncinaErrors.Create(
                code: "processor_agreements.store_error",
                message: $"Failed to get sub-processors: {ex.Message}",
                details: new Dictionary<string, object?> { ["processorId"] = processorId }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<Processor>>> GetFullSubProcessorChainAsync(
        string processorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processorId);

        try
        {
            // Load all processors and perform BFS in memory (same pattern as EF Core).
            var sql = $@"
                SELECT Id, Name, Country, ContactEmail, ParentProcessorId, Depth, SubProcessorAuthorizationTypeValue, TenantId, ModuleId, CreatedAtUtc, LastUpdatedAtUtc
                FROM {_tableName}";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var allProcessors = new List<Processor>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToEntity(reader);
                var domain = ProcessorMapper.ToDomain(entity);
                if (domain is not null)
                    allProcessors.Add(domain);
            }

            // BFS traversal bounded by MaxSubProcessorDepth.
            var result = new List<Processor>();
            var queue = new Queue<string>();
            queue.Enqueue(processorId);

            while (queue.Count > 0)
            {
                var currentId = queue.Dequeue();
                var children = allProcessors
                    .Where(p => p.ParentProcessorId == currentId && p.Depth <= MaxSubProcessorDepth);

                foreach (var child in children)
                {
                    result.Add(child);
                    queue.Enqueue(child.Id);
                }
            }

            return Right<EncinaError, IReadOnlyList<Processor>>(result);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "processor_agreements.store_error",
                message: $"Failed to get full sub-processor chain: {ex.Message}",
                details: new Dictionary<string, object?> { ["processorId"] = processorId }));
        }
    }

    private static ProcessorEntity MapToEntity(IDataReader reader)
    {
        var contactEmailOrd = reader.GetOrdinal("ContactEmail");
        var parentProcessorIdOrd = reader.GetOrdinal("ParentProcessorId");
        var tenantIdOrd = reader.GetOrdinal("TenantId");
        var moduleIdOrd = reader.GetOrdinal("ModuleId");

        return new ProcessorEntity
        {
            Id = reader.GetString(reader.GetOrdinal("Id")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            Country = reader.GetString(reader.GetOrdinal("Country")),
            ContactEmail = reader.IsDBNull(contactEmailOrd) ? null : reader.GetString(contactEmailOrd),
            ParentProcessorId = reader.IsDBNull(parentProcessorIdOrd) ? null : reader.GetString(parentProcessorIdOrd),
            Depth = reader.GetInt32(reader.GetOrdinal("Depth")),
            SubProcessorAuthorizationTypeValue = reader.GetInt32(reader.GetOrdinal("SubProcessorAuthorizationTypeValue")),
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

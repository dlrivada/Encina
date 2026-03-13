using System.Data;

using Dapper;

using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Messaging;

using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Dapper.PostgreSQL.ProcessorAgreements;

/// <summary>
/// Dapper implementation of <see cref="IProcessorRegistry"/> for PostgreSQL.
/// Manages processor identity and hierarchy per GDPR Article 28.
/// </summary>
/// <remarks>
/// <para>
/// PostgreSQL-specific considerations:
/// <list type="bullet">
/// <item><description>DateTimeOffset values are passed directly (native TIMESTAMPTZ support).</description></item>
/// <item><description>Integer types are cast directly from dynamic rows.</description></item>
/// <item><description>Column names are lowercase; Dapper returns lowercase property names.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class ProcessorRegistryDapper : IProcessorRegistry
{
    internal const int MaxSubProcessorDepth = 5;

    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessorRegistryDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The processors table name (default: Processors).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    public ProcessorRegistryDapper(
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
            // Check if processor already exists.
            var existsSql = $"SELECT COUNT(1) FROM {_tableName} WHERE id = @Id";
            var exists = await _connection.ExecuteScalarAsync<long>(existsSql, new { Id = processor.Id });

            if (exists > 0)
            {
                return ProcessorAgreementErrors.AlreadyExists(processor.Id);
            }

            // Validate depth constraint.
            if (processor.Depth > MaxSubProcessorDepth)
            {
                return ProcessorAgreementErrors.SubProcessorDepthExceeded(
                    processor.Id, processor.Depth, MaxSubProcessorDepth);
            }

            // Validate parent exists if specified.
            if (processor.ParentProcessorId is not null)
            {
                var parentSql = $"SELECT COUNT(1) FROM {_tableName} WHERE id = @Id";
                var parentExists = await _connection.ExecuteScalarAsync<long>(parentSql, new { Id = processor.ParentProcessorId });

                if (parentExists == 0)
                {
                    return ProcessorAgreementErrors.NotFound(processor.ParentProcessorId);
                }
            }

            var entity = ProcessorMapper.ToEntity(processor);
            var sql = $@"
                INSERT INTO {_tableName}
                (id, name, country, contactemail, parentprocessorid, depth, subprocessorauthorizationtypevalue, tenantid, moduleid, createdatutc, lastupdatedatutc)
                VALUES
                (@Id, @Name, @Country, @ContactEmail, @ParentProcessorId, @Depth, @SubProcessorAuthorizationTypeValue, @TenantId, @ModuleId, @CreatedAtUtc, @LastUpdatedAtUtc)";

            await _connection.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.Name,
                entity.Country,
                entity.ContactEmail,
                entity.ParentProcessorId,
                entity.Depth,
                entity.SubProcessorAuthorizationTypeValue,
                entity.TenantId,
                entity.ModuleId,
                entity.CreatedAtUtc,
                entity.LastUpdatedAtUtc
            });

            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return ProcessorAgreementErrors.StoreError("RegisterProcessor", ex.Message, ex);
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
                SELECT id, name, country, contactemail, parentprocessorid, depth, subprocessorauthorizationtypevalue, tenantid, moduleid, createdatutc, lastupdatedatutc
                FROM {_tableName}
                WHERE id = @Id";

            var rows = await _connection.QueryAsync(sql, new { Id = processorId });
            var row = rows.FirstOrDefault();

            if (row is null)
            {
                return Right<EncinaError, Option<Processor>>(Option<Processor>.None);
            }

            var processor = MapToProcessor(row);

            return processor is not null
                ? Right<EncinaError, Option<Processor>>(Some(processor))
                : Right<EncinaError, Option<Processor>>(Option<Processor>.None);
        }
        catch (Exception ex)
        {
            return ProcessorAgreementErrors.StoreError("GetProcessor", ex.Message, ex);
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

            var rows = await _connection.QueryAsync(sql);
            var processors = rows
                .Select(MapToProcessor)
                .Where(p => p is not null)
                .Cast<Processor>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<Processor>>(processors);
        }
        catch (Exception ex)
        {
            return ProcessorAgreementErrors.StoreError("GetAllProcessors", ex.Message, ex);
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
            var existsSql = $"SELECT COUNT(1) FROM {_tableName} WHERE id = @Id";
            var exists = await _connection.ExecuteScalarAsync<long>(existsSql, new { Id = processor.Id });

            if (exists == 0)
            {
                return ProcessorAgreementErrors.NotFound(processor.Id);
            }

            var entity = ProcessorMapper.ToEntity(processor);
            var sql = $@"
                UPDATE {_tableName}
                SET name = @Name, country = @Country, contactemail = @ContactEmail,
                    parentprocessorid = @ParentProcessorId, depth = @Depth,
                    subprocessorauthorizationtypevalue = @SubProcessorAuthorizationTypeValue,
                    tenantid = @TenantId, moduleid = @ModuleId, lastupdatedatutc = @LastUpdatedAtUtc
                WHERE id = @Id";

            await _connection.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.Name,
                entity.Country,
                entity.ContactEmail,
                entity.ParentProcessorId,
                entity.Depth,
                entity.SubProcessorAuthorizationTypeValue,
                entity.TenantId,
                entity.ModuleId,
                entity.LastUpdatedAtUtc
            });

            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return ProcessorAgreementErrors.StoreError("UpdateProcessor", ex.Message, ex);
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
            var sql = $"DELETE FROM {_tableName} WHERE id = @Id";
            var affected = await _connection.ExecuteAsync(sql, new { Id = processorId });

            if (affected == 0)
            {
                return ProcessorAgreementErrors.NotFound(processorId);
            }

            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return ProcessorAgreementErrors.StoreError("RemoveProcessor", ex.Message, ex);
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
                SELECT id, name, country, contactemail, parentprocessorid, depth, subprocessorauthorizationtypevalue, tenantid, moduleid, createdatutc, lastupdatedatutc
                FROM {_tableName}
                WHERE parentprocessorid = @ParentProcessorId";

            var rows = await _connection.QueryAsync(sql, new { ParentProcessorId = processorId });
            var processors = rows
                .Select(MapToProcessor)
                .Where(p => p is not null)
                .Cast<Processor>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<Processor>>(processors);
        }
        catch (Exception ex)
        {
            return ProcessorAgreementErrors.StoreError("GetSubProcessors", ex.Message, ex);
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
            // BFS traversal bounded by MaxSubProcessorDepth.
            var result = new List<Processor>();
            var queue = new Queue<string>();
            queue.Enqueue(processorId);

            while (queue.Count > 0)
            {
                var currentId = queue.Dequeue();
                var sql = $@"
                    SELECT id, name, country, contactemail, parentprocessorid, depth, subprocessorauthorizationtypevalue, tenantid, moduleid, createdatutc, lastupdatedatutc
                    FROM {_tableName}
                    WHERE parentprocessorid = @ParentProcessorId AND depth <= @MaxDepth";

                var rows = await _connection.QueryAsync(sql, new { ParentProcessorId = currentId, MaxDepth = MaxSubProcessorDepth });

                foreach (var row in rows)
                {
                    var processor = MapToProcessor(row);
                    if (processor is not null)
                    {
                        result.Add(processor);
                        queue.Enqueue(processor.Id);
                    }
                }
            }

            return Right<EncinaError, IReadOnlyList<Processor>>(result);
        }
        catch (Exception ex)
        {
            return ProcessorAgreementErrors.StoreError("GetFullSubProcessorChain", ex.Message, ex);
        }
    }

    /// <summary>
    /// Maps a dynamic row from Dapper to a <see cref="Processor"/>.
    /// DateTimeOffset values are cast directly (native PostgreSQL TIMESTAMPTZ support).
    /// Property names are lowercase because PostgreSQL returns lowercase column names.
    /// </summary>
    /// <param name="row">The dynamic row returned by Dapper.</param>
    /// <returns>A populated processor, or <c>null</c> if the entity contains invalid values.</returns>
    private static Processor? MapToProcessor(dynamic row)
    {
        var entity = new ProcessorEntity
        {
            Id = (string)row.id,
            Name = (string)row.name,
            Country = row.country is null or DBNull ? null! : (string)row.country,
            ContactEmail = row.contactemail is null or DBNull ? null : (string)row.contactemail,
            ParentProcessorId = row.parentprocessorid is null or DBNull ? null : (string)row.parentprocessorid,
            Depth = (int)row.depth,
            SubProcessorAuthorizationTypeValue = (int)row.subprocessorauthorizationtypevalue,
            TenantId = row.tenantid is null or DBNull ? null : (string)row.tenantid,
            ModuleId = row.moduleid is null or DBNull ? null : (string)row.moduleid,
            CreatedAtUtc = (DateTimeOffset)row.createdatutc,
            LastUpdatedAtUtc = (DateTimeOffset)row.lastupdatedatutc
        };

        return ProcessorMapper.ToDomain(entity);
    }
}

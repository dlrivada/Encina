using System.Data;
using System.Globalization;
using Dapper;
using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Messaging;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.Sqlite.ProcessorAgreements;

/// <summary>
/// Dapper implementation of <see cref="IProcessorRegistry"/> for SQLite.
/// Manages processor identity and hierarchy per GDPR Article 28.
/// </summary>
/// <remarks>
/// <para>
/// SQLite-specific considerations:
/// <list type="bullet">
/// <item><description>DateTimeOffset values are stored as ISO 8601 text via <c>.ToString("O")</c>.</description></item>
/// <item><description>Integer values require <c>Convert.ToInt32()</c> for safe casting from SQLite's dynamic types.</description></item>
/// <item><description>Never uses <c>datetime('now')</c>; always uses parameterized values.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class ProcessorRegistryDapper : IProcessorRegistry
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Maximum allowed depth for the sub-processor hierarchy.
    /// </summary>
    internal const int MaxSubProcessorDepth = 5;

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
            if (processor.Depth > MaxSubProcessorDepth)
            {
                return Left(ProcessorAgreementErrors.SubProcessorDepthExceeded(
                    processor.Id, processor.Depth, MaxSubProcessorDepth));
            }

            var existsSql = $"SELECT COUNT(1) FROM {_tableName} WHERE Id = @Id";
            var exists = await _connection.ExecuteScalarAsync<long>(existsSql, new { processor.Id });
            if (exists > 0)
            {
                return Left(ProcessorAgreementErrors.AlreadyExists(processor.Id));
            }

            var entity = ProcessorMapper.ToEntity(processor);
            var sql = $@"
                INSERT INTO {_tableName}
                (Id, Name, Country, ContactEmail, ParentProcessorId, Depth, SubProcessorAuthorizationTypeValue, TenantId, ModuleId, CreatedAtUtc, LastUpdatedAtUtc)
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
                CreatedAtUtc = entity.CreatedAtUtc.ToString("O"),
                LastUpdatedAtUtc = entity.LastUpdatedAtUtc.ToString("O")
            });

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
                SELECT Id, Name, Country, ContactEmail, ParentProcessorId, Depth, SubProcessorAuthorizationTypeValue, TenantId, ModuleId, CreatedAtUtc, LastUpdatedAtUtc
                FROM {_tableName}
                WHERE Id = @Id";

            var rows = await _connection.QueryAsync(sql, new { Id = processorId });
            var row = rows.FirstOrDefault();

            if (row is null)
                return Right<EncinaError, Option<Processor>>(None);

            var processor = MapToProcessor(row);
            return processor is not null
                ? Right<EncinaError, Option<Processor>>(Some(processor))
                : Right<EncinaError, Option<Processor>>(None);
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
                SELECT Id, Name, Country, ContactEmail, ParentProcessorId, Depth, SubProcessorAuthorizationTypeValue, TenantId, ModuleId, CreatedAtUtc, LastUpdatedAtUtc
                FROM {_tableName}";

            var rows = await _connection.QueryAsync(sql);
            var results = rows
                .Select(MapToProcessor)
                .Where(p => p is not null)
                .Cast<Processor>()
                .ToList();

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
            var existsSql = $"SELECT COUNT(1) FROM {_tableName} WHERE Id = @Id";
            var exists = await _connection.ExecuteScalarAsync<long>(existsSql, new { processor.Id });
            if (exists == 0)
            {
                return Left(ProcessorAgreementErrors.NotFound(processor.Id));
            }

            var entity = ProcessorMapper.ToEntity(processor);
            var sql = $@"
                UPDATE {_tableName}
                SET Name = @Name,
                    Country = @Country,
                    ContactEmail = @ContactEmail,
                    ParentProcessorId = @ParentProcessorId,
                    Depth = @Depth,
                    SubProcessorAuthorizationTypeValue = @SubProcessorAuthorizationTypeValue,
                    TenantId = @TenantId,
                    ModuleId = @ModuleId,
                    LastUpdatedAtUtc = @LastUpdatedAtUtc
                WHERE Id = @Id";

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
                LastUpdatedAtUtc = entity.LastUpdatedAtUtc.ToString("O")
            });

            return Right(Unit.Default);
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
            var existsSql = $"SELECT COUNT(1) FROM {_tableName} WHERE Id = @Id";
            var exists = await _connection.ExecuteScalarAsync<long>(existsSql, new { Id = processorId });
            if (exists == 0)
            {
                return Left(ProcessorAgreementErrors.NotFound(processorId));
            }

            var sql = $"DELETE FROM {_tableName} WHERE Id = @Id";
            await _connection.ExecuteAsync(sql, new { Id = processorId });

            return Right(Unit.Default);
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
                SELECT Id, Name, Country, ContactEmail, ParentProcessorId, Depth, SubProcessorAuthorizationTypeValue, TenantId, ModuleId, CreatedAtUtc, LastUpdatedAtUtc
                FROM {_tableName}
                WHERE ParentProcessorId = @ParentProcessorId";

            var rows = await _connection.QueryAsync(sql, new { ParentProcessorId = processorId });
            var results = rows
                .Select(MapToProcessor)
                .Where(p => p is not null)
                .Cast<Processor>()
                .ToList();

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
            // Load all processors and traverse via BFS
            var sql = $@"
                SELECT Id, Name, Country, ContactEmail, ParentProcessorId, Depth, SubProcessorAuthorizationTypeValue, TenantId, ModuleId, CreatedAtUtc, LastUpdatedAtUtc
                FROM {_tableName}";

            var rows = await _connection.QueryAsync(sql);
            var allProcessors = rows
                .Select(MapToProcessor)
                .Where(p => p is not null)
                .Cast<Processor>()
                .ToList();

            var byParent = allProcessors
                .Where(p => p.ParentProcessorId is not null)
                .GroupBy(p => p.ParentProcessorId!)
                .ToDictionary(g => g.Key, g => g.ToList());

            var result = new List<Processor>();
            var queue = new Queue<string>();
            queue.Enqueue(processorId);

            while (queue.Count > 0)
            {
                var currentId = queue.Dequeue();
                if (!byParent.TryGetValue(currentId, out var children))
                    continue;

                foreach (var child in children)
                {
                    result.Add(child);
                    if (child.Depth < MaxSubProcessorDepth)
                    {
                        queue.Enqueue(child.Id);
                    }
                }
            }

            return Right<EncinaError, IReadOnlyList<Processor>>(result);
        }
        catch (Exception ex)
        {
            return Left(ProcessorAgreementErrors.StoreError(
                "GetFullSubProcessorChain", ex.Message, ex));
        }
    }

    private static Processor? MapToProcessor(dynamic row)
    {
        var entity = new ProcessorEntity
        {
            Id = (string)row.Id,
            Name = (string)row.Name,
            Country = row.Country is null or DBNull ? string.Empty : (string)row.Country,
            ContactEmail = row.ContactEmail is null or DBNull ? null : (string)row.ContactEmail,
            ParentProcessorId = row.ParentProcessorId is null or DBNull ? null : (string)row.ParentProcessorId,
            Depth = Convert.ToInt32(row.Depth),
            SubProcessorAuthorizationTypeValue = Convert.ToInt32(row.SubProcessorAuthorizationTypeValue),
            TenantId = row.TenantId is null or DBNull ? null : (string)row.TenantId,
            ModuleId = row.ModuleId is null or DBNull ? null : (string)row.ModuleId,
            CreatedAtUtc = DateTimeOffset.Parse((string)row.CreatedAtUtc, null, DateTimeStyles.RoundtripKind),
            LastUpdatedAtUtc = DateTimeOffset.Parse((string)row.LastUpdatedAtUtc, null, DateTimeStyles.RoundtripKind)
        };
        return ProcessorMapper.ToDomain(entity);
    }
}

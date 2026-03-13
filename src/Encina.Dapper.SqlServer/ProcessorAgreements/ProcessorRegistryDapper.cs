using System.Data;
using Dapper;
using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Messaging;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.SqlServer.ProcessorAgreements;

/// <summary>
/// Dapper implementation of <see cref="IProcessorRegistry"/> for SQL Server.
/// Manages processor identity and sub-processor hierarchy per GDPR Article 28.
/// </summary>
/// <remarks>
/// <para>
/// SQL Server-specific considerations:
/// <list type="bullet">
/// <item><description>DateTimeOffset values are passed as <c>.UtcDateTime</c> for native parameter handling.</description></item>
/// <item><description>Uses PascalCase column names matching the entity properties.</description></item>
/// <item><description>Integer and DateTimeOffset values can be cast directly without conversion helpers.</description></item>
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

            var existsCount = await _connection.ExecuteScalarAsync<int>(
                $"SELECT COUNT(1) FROM {_tableName} WHERE Id = @Id",
                new { Id = processor.Id });

            if (existsCount > 0)
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
                CreatedAtUtc = entity.CreatedAtUtc.UtcDateTime,
                LastUpdatedAtUtc = entity.LastUpdatedAtUtc.UtcDateTime
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

            var domain = MapToProcessor(row);
            return domain is null
                ? Right<EncinaError, Option<Processor>>(None)
                : Right<EncinaError, Option<Processor>>(Some(domain));
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
                .Select(row => MapToProcessor(row))
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
            var existsCount = await _connection.ExecuteScalarAsync<int>(
                $"SELECT COUNT(1) FROM {_tableName} WHERE Id = @Id",
                new { Id = processor.Id });

            if (existsCount == 0)
            {
                return Left(ProcessorAgreementErrors.NotFound(processor.Id));
            }

            var entity = ProcessorMapper.ToEntity(processor);

            var sql = $@"
                UPDATE {_tableName}
                SET Name = @Name, Country = @Country, ContactEmail = @ContactEmail,
                    ParentProcessorId = @ParentProcessorId, Depth = @Depth,
                    SubProcessorAuthorizationTypeValue = @SubProcessorAuthorizationTypeValue,
                    TenantId = @TenantId, ModuleId = @ModuleId,
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
                LastUpdatedAtUtc = entity.LastUpdatedAtUtc.UtcDateTime
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
            var existsCount = await _connection.ExecuteScalarAsync<int>(
                $"SELECT COUNT(1) FROM {_tableName} WHERE Id = @Id",
                new { Id = processorId });

            if (existsCount == 0)
            {
                return Left(ProcessorAgreementErrors.NotFound(processorId));
            }

            await _connection.ExecuteAsync(
                $"DELETE FROM {_tableName} WHERE Id = @Id",
                new { Id = processorId });

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
                .Select(row => MapToProcessor(row))
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
            // Load all processors and build the chain in-memory using BFS
            var sql = $@"
                SELECT Id, Name, Country, ContactEmail, ParentProcessorId, Depth, SubProcessorAuthorizationTypeValue, TenantId, ModuleId, CreatedAtUtc, LastUpdatedAtUtc
                FROM {_tableName}";

            var rows = await _connection.QueryAsync(sql);
            var allProcessors = rows
                .Select(row => MapToProcessor(row))
                .Where(p => p is not null)
                .Cast<Processor>()
                .ToList();

            // Build lookup by ParentProcessorId
            var childrenByParent = new Dictionary<string, List<Processor>>(StringComparer.Ordinal);
            foreach (var p in allProcessors)
            {
                if (p.ParentProcessorId is not null)
                {
                    if (!childrenByParent.TryGetValue(p.ParentProcessorId, out var children))
                    {
                        children = [];
                        childrenByParent[p.ParentProcessorId] = children;
                    }
                    children.Add(p);
                }
            }

            // BFS traversal bounded by MaxSubProcessorDepth
            var result = new List<Processor>();
            var queue = new Queue<string>();
            queue.Enqueue(processorId);
            var depth = 0;

            while (queue.Count > 0 && depth < MaxSubProcessorDepth)
            {
                var levelCount = queue.Count;
                for (var i = 0; i < levelCount; i++)
                {
                    var parentId = queue.Dequeue();
                    if (childrenByParent.TryGetValue(parentId, out var children))
                    {
                        foreach (var child in children)
                        {
                            result.Add(child);
                            queue.Enqueue(child.Id);
                        }
                    }
                }
                depth++;
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
            Country = row.Country is null or DBNull ? null! : (string)row.Country,
            ContactEmail = row.ContactEmail is null or DBNull ? null : (string)row.ContactEmail,
            ParentProcessorId = row.ParentProcessorId is null or DBNull ? null : (string)row.ParentProcessorId,
            Depth = (int)row.Depth,
            SubProcessorAuthorizationTypeValue = (int)row.SubProcessorAuthorizationTypeValue,
            TenantId = row.TenantId is null or DBNull ? null : (string)row.TenantId,
            ModuleId = row.ModuleId is null or DBNull ? null : (string)row.ModuleId,
            CreatedAtUtc = (DateTimeOffset)row.CreatedAtUtc,
            LastUpdatedAtUtc = (DateTimeOffset)row.LastUpdatedAtUtc
        };
        return ProcessorMapper.ToDomain(entity);
    }
}

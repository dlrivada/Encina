using System.Data;
using Dapper;
using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Messaging;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.MySQL.ProcessorAgreements;

/// <summary>
/// Dapper implementation of <see cref="IProcessorRegistry"/> for MySQL.
/// Manages processor identity and sub-processor hierarchy per GDPR Article 28.
/// </summary>
/// <remarks>
/// <para>
/// MySQL-specific considerations:
/// <list type="bullet">
/// <item><description>DateTimeOffset values are written via <c>.UtcDateTime</c>.</description></item>
/// <item><description>DateTimeOffset values are read via direct cast <c>(DateTimeOffset)row.Prop</c>.</description></item>
/// <item><description>Integer types are cast directly from dynamic rows.</description></item>
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
            if (processor.Depth > MaxSubProcessorDepth)
            {
                return Left(EncinaErrors.Create(
                    code: "processor.sub_processor_depth_exceeded",
                    message: $"Sub-processor depth {processor.Depth} exceeds maximum allowed depth of {MaxSubProcessorDepth}.",
                    details: new Dictionary<string, object?> { ["processorId"] = processor.Id, ["depth"] = processor.Depth }));
            }

            var entity = ProcessorMapper.ToEntity(processor);

            // Check if processor already exists
            var checkSql = $"SELECT COUNT(*) FROM {_tableName} WHERE Id = @Id";
            var exists = await _connection.ExecuteScalarAsync<int>(checkSql, new { entity.Id });
            if (exists > 0)
            {
                return Left(EncinaErrors.Create(
                    code: "processor.already_exists",
                    message: $"Processor '{processor.Id}' already exists.",
                    details: new Dictionary<string, object?> { ["processorId"] = processor.Id }));
            }

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
            return Left(EncinaErrors.Create(
                code: "processor.store_error",
                message: $"Failed to register processor: {ex.Message}",
                details: new Dictionary<string, object?> { ["processorId"] = processor.Id }));
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
            return Left(EncinaErrors.Create(
                code: "processor.store_error",
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
            return Left(EncinaErrors.Create(
                code: "processor.store_error",
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
            var entity = ProcessorMapper.ToEntity(processor);
            var sql = $@"
                UPDATE {_tableName}
                SET Name = @Name, Country = @Country, ContactEmail = @ContactEmail,
                    ParentProcessorId = @ParentProcessorId, Depth = @Depth,
                    SubProcessorAuthorizationTypeValue = @SubProcessorAuthorizationTypeValue,
                    TenantId = @TenantId, ModuleId = @ModuleId, LastUpdatedAtUtc = @LastUpdatedAtUtc
                WHERE Id = @Id";

            var rowsAffected = await _connection.ExecuteAsync(sql, new
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

            if (rowsAffected == 0)
            {
                return Left(EncinaErrors.Create(
                    code: "processor.not_found",
                    message: $"Processor '{processor.Id}' not found.",
                    details: new Dictionary<string, object?> { ["processorId"] = processor.Id }));
            }

            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "processor.store_error",
                message: $"Failed to update processor: {ex.Message}",
                details: new Dictionary<string, object?> { ["processorId"] = processor.Id }));
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
            var sql = $@"DELETE FROM {_tableName} WHERE Id = @Id";
            var rowsAffected = await _connection.ExecuteAsync(sql, new { Id = processorId });

            if (rowsAffected == 0)
            {
                return Left(EncinaErrors.Create(
                    code: "processor.not_found",
                    message: $"Processor '{processorId}' not found.",
                    details: new Dictionary<string, object?> { ["processorId"] = processorId }));
            }

            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "processor.store_error",
                message: $"Failed to remove processor: {ex.Message}",
                details: new Dictionary<string, object?> { ["processorId"] = processorId }));
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
            return Left(EncinaErrors.Create(
                code: "processor.store_error",
                message: $"Failed to get sub-processors: {ex.Message}",
                details: new Dictionary<string, object?> { ["processorId"] = processorId }));
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
            // Load all processors and perform BFS in memory to traverse the hierarchy
            var allResult = await GetAllProcessorsAsync(cancellationToken);
            return allResult.Match<Either<EncinaError, IReadOnlyList<Processor>>>(
                Right: allProcessors =>
                {
                    var lookup = allProcessors
                        .Where(p => p.ParentProcessorId is not null)
                        .GroupBy(p => p.ParentProcessorId!)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    var chain = new List<Processor>();
                    var queue = new Queue<string>();
                    queue.Enqueue(processorId);

                    var depth = 0;
                    while (queue.Count > 0 && depth < MaxSubProcessorDepth)
                    {
                        var levelSize = queue.Count;
                        for (var i = 0; i < levelSize; i++)
                        {
                            var currentId = queue.Dequeue();
                            if (lookup.TryGetValue(currentId, out var children))
                            {
                                foreach (var child in children)
                                {
                                    chain.Add(child);
                                    queue.Enqueue(child.Id);
                                }
                            }
                        }

                        depth++;
                    }

                    return Right<EncinaError, IReadOnlyList<Processor>>(chain);
                },
                Left: error => Left(error));
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "processor.store_error",
                message: $"Failed to get full sub-processor chain: {ex.Message}",
                details: new Dictionary<string, object?> { ["processorId"] = processorId }));
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

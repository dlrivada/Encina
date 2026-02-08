using System.Data;
using System.Data.Common;
using System.Text.Json;

using Encina.DomainModeling.Pagination;
using Encina.Messaging;

namespace Encina.ADO.PostgreSQL.Pagination;

/// <summary>
/// Helper class for executing cursor-paginated queries using ADO.NET with PostgreSQL.
/// </summary>
/// <remarks>
/// <para>
/// This helper builds efficient keyset pagination queries that provide O(1) performance
/// regardless of page position, unlike offset-based pagination which degrades linearly.
/// </para>
/// <para>
/// <b>PostgreSQL-specific syntax:</b>
/// <list type="bullet">
/// <item><description>Uses double-quotes for identifiers: "ColumnName"</description></item>
/// <item><description>Uses LIMIT for row limiting</description></item>
/// <item><description>Uses standard parameter syntax: @param</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var helper = new CursorPaginationHelper&lt;Order&gt;(connection, encoder);
/// var result = await helper.ExecuteAsync(
///     tableName: "Orders",
///     keyColumn: "CreatedAtUtc",
///     cursor: request.Cursor,
///     pageSize: 20,
///     isDescending: true,
///     direction: CursorDirection.Forward,
///     whereClause: "CustomerId = @CustomerId",
///     configureParameters: cmd => cmd.Parameters.AddWithValue("@CustomerId", customerId),
///     cancellationToken);
/// </code>
/// </example>
public sealed class CursorPaginationHelper<TEntity>
    where TEntity : class
{
    private readonly DbConnection _connection;
    private readonly ICursorEncoder _cursorEncoder;
    private readonly Func<IDataReader, TEntity> _entityMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="CursorPaginationHelper{TEntity}"/> class.
    /// </summary>
    /// <param name="connection">The database connection to use.</param>
    /// <param name="cursorEncoder">The encoder for cursor serialization.</param>
    /// <param name="entityMapper">Function to map a data reader row to an entity.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="connection"/>, <paramref name="cursorEncoder"/>, or <paramref name="entityMapper"/> is null.
    /// </exception>
    public CursorPaginationHelper(
        DbConnection connection,
        ICursorEncoder cursorEncoder,
        Func<IDataReader, TEntity> entityMapper)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(cursorEncoder);
        ArgumentNullException.ThrowIfNull(entityMapper);

        _connection = connection;
        _cursorEncoder = cursorEncoder;
        _entityMapper = entityMapper;
    }

    /// <summary>
    /// Executes a cursor-paginated query with a single key column.
    /// </summary>
    /// <typeparam name="TKey">The type of the cursor key.</typeparam>
    /// <param name="tableName">The table name to query.</param>
    /// <param name="keyColumn">The column name used for cursor pagination (must match ORDER BY).</param>
    /// <param name="cursor">The opaque cursor string from a previous result, or null for the first page.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="isDescending">Whether the key column is ordered descending.</param>
    /// <param name="direction">The direction of pagination (forward or backward).</param>
    /// <param name="whereClause">Optional additional WHERE clause (without the WHERE keyword).</param>
    /// <param name="configureParameters">Optional action to configure additional parameters.</param>
    /// <param name="columns">Optional column list to select (null for all columns).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="CursorPaginatedResult{TEntity}"/> containing the paginated items.</returns>
    public async Task<CursorPaginatedResult<TEntity>> ExecuteAsync<TKey>(
        string tableName,
        string keyColumn,
        string? cursor,
        int pageSize,
        bool isDescending,
        CursorDirection direction = CursorDirection.Forward,
        string? whereClause = null,
        Action<DbCommand>? configureParameters = null,
        string? columns = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(keyColumn);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, CursorPaginationOptions.MaxPageSize);

        // Decode cursor
        var hasCursor = !string.IsNullOrEmpty(cursor);
        var cursorKeyValue = hasCursor
            ? _cursorEncoder.Decode<TKey>(cursor)
            : default;

        var isBackward = direction == CursorDirection.Backward;

        // Build SQL - pass hasCursor flag to correctly handle value types
        var sql = BuildSql(
            tableName,
            keyColumn,
            hasCursor ? (object?)cursorKeyValue : null,
            pageSize,
            isDescending,
            isBackward,
            whereClause,
            columns);

        // Execute query
        await using var command = _connection.CreateCommand();
        command.CommandText = sql;

        // Add cursor parameter if present
        if (hasCursor)
        {
            var cursorParam = command.CreateParameter();
            cursorParam.ParameterName = "@cursorKey";
            cursorParam.Value = cursorKeyValue ?? (object)DBNull.Value;
            command.Parameters.Add(cursorParam);
        }

        // Apply additional parameters
        configureParameters?.Invoke(command);

        // Ensure connection is open
        if (_connection.State != ConnectionState.Open)
        {
            await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        var itemsWithExtra = new List<TEntity>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            itemsWithExtra.Add(_entityMapper(reader));
        }

        // Process results
        return ProcessResults(
            itemsWithExtra,
            pageSize,
            keyColumn,
            cursor,
            isBackward);
    }

    /// <summary>
    /// Executes a cursor-paginated query with composite keys.
    /// </summary>
    /// <param name="tableName">The table name to query.</param>
    /// <param name="keyColumns">The column names used for cursor pagination (must match ORDER BY order).</param>
    /// <param name="cursor">The opaque cursor string from a previous result, or null for the first page.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="keyDescending">Array indicating which key columns are descending.</param>
    /// <param name="direction">The direction of pagination (forward or backward).</param>
    /// <param name="whereClause">Optional additional WHERE clause (without the WHERE keyword).</param>
    /// <param name="configureParameters">Optional action to configure additional parameters.</param>
    /// <param name="columns">Optional column list to select (null for all columns).</param>
    /// <param name="keySelector">Function to extract cursor key values from an entity.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="CursorPaginatedResult{TEntity}"/> containing the paginated items.</returns>
    public async Task<CursorPaginatedResult<TEntity>> ExecuteCompositeAsync(
        string tableName,
        string[] keyColumns,
        string? cursor,
        int pageSize,
        bool[] keyDescending,
        CursorDirection direction = CursorDirection.Forward,
        string? whereClause = null,
        Action<DbCommand>? configureParameters = null,
        string? columns = null,
        Func<TEntity, object>? keySelector = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ArgumentNullException.ThrowIfNull(keyColumns);
        ArgumentNullException.ThrowIfNull(keyDescending);

        if (keyColumns.Length == 0)
        {
            throw new ArgumentException("At least one key column is required.", nameof(keyColumns));
        }

        if (keyColumns.Length != keyDescending.Length)
        {
            throw new ArgumentException(
                $"keyDescending length ({keyDescending.Length}) must match keyColumns length ({keyColumns.Length}).",
                nameof(keyDescending));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, CursorPaginationOptions.MaxPageSize);

        // Decode cursor to dictionary
        var cursorValues = !string.IsNullOrEmpty(cursor)
            ? _cursorEncoder.Decode<Dictionary<string, object?>>(cursor)
            : null;

        var isBackward = direction == CursorDirection.Backward;

        // Build SQL
        var sql = BuildCompositeSql(
            tableName,
            keyColumns,
            cursorValues,
            pageSize,
            keyDescending,
            isBackward,
            whereClause,
            columns);

        // Execute query
        await using var command = _connection.CreateCommand();
        command.CommandText = sql;

        // Add cursor parameters if present
        if (cursorValues is not null)
        {
            foreach (var col in keyColumns)
            {
                if (cursorValues.TryGetValue(col, out var value))
                {
                    var param = command.CreateParameter();
                    param.ParameterName = $"@cursor_{col}";
                    param.Value = ConvertJsonElementToNativeType(value) ?? DBNull.Value;
                    command.Parameters.Add(param);
                }
            }
        }

        // Apply additional parameters
        configureParameters?.Invoke(command);

        // Ensure connection is open
        if (_connection.State != ConnectionState.Open)
        {
            await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        var itemsWithExtra = new List<TEntity>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            itemsWithExtra.Add(_entityMapper(reader));
        }

        // Process results
        return ProcessCompositeResults(
            itemsWithExtra,
            pageSize,
            keyColumns,
            cursor,
            isBackward,
            keySelector);
    }

    #region SQL Building

    private static string BuildSql(
        string tableName,
        string keyColumn,
        object? cursorKeyValue,
        int pageSize,
        bool isDescending,
        bool isBackward,
        string? whereClause,
        string? columns)
    {
        var selectColumns = columns ?? "*";
        var validatedTable = SqlIdentifierValidator.ValidateTableName(tableName);
        var validatedKey = SqlIdentifierValidator.ValidateTableName(keyColumn, nameof(keyColumn));

        // PostgreSQL uses LIMIT at the end
        var sqlParts = new List<string>
        {
            $"SELECT {selectColumns} FROM \"{validatedTable}\""
        };

        // Build WHERE clause
        var whereClauses = new List<string>();

        if (!string.IsNullOrWhiteSpace(whereClause))
        {
            whereClauses.Add($"({whereClause})");
        }

        if (cursorKeyValue is not null)
        {
            var keysetFilter = BuildKeysetFilter(validatedKey, isDescending, isBackward);
            whereClauses.Add(keysetFilter);
        }

        if (whereClauses.Count > 0)
        {
            sqlParts.Add($"WHERE {string.Join(" AND ", whereClauses)}");
        }

        // ORDER BY - flip for backward pagination
        var orderDirection = GetEffectiveOrderDirection(isDescending, isBackward);
        sqlParts.Add($"ORDER BY \"{validatedKey}\" {orderDirection}");

        // PostgreSQL uses LIMIT
        sqlParts.Add($"LIMIT {pageSize + 1}");

        return string.Join(" ", sqlParts);
    }

    private static string BuildCompositeSql(
        string tableName,
        string[] keyColumns,
        Dictionary<string, object?>? cursorValues,
        int pageSize,
        bool[] keyDescending,
        bool isBackward,
        string? whereClause,
        string? columns)
    {
        var selectColumns = columns ?? "*";
        var validatedTable = SqlIdentifierValidator.ValidateTableName(tableName);

        // PostgreSQL uses LIMIT at the end
        var sqlParts = new List<string>
        {
            $"SELECT {selectColumns} FROM \"{validatedTable}\""
        };

        // Build WHERE clause
        var whereClauses = new List<string>();

        if (!string.IsNullOrWhiteSpace(whereClause))
        {
            whereClauses.Add($"({whereClause})");
        }

        if (cursorValues is not null && cursorValues.Count > 0)
        {
            var keysetFilter = BuildCompositeKeysetFilter(keyColumns, keyDescending, isBackward);
            whereClauses.Add(keysetFilter);
        }

        if (whereClauses.Count > 0)
        {
            sqlParts.Add($"WHERE {string.Join(" AND ", whereClauses)}");
        }

        // ORDER BY - flip directions for backward pagination
        var orderClauses = keyColumns.Select((col, i) =>
        {
            var validatedCol = SqlIdentifierValidator.ValidateTableName(col, "column");
            var direction = GetEffectiveOrderDirection(keyDescending[i], isBackward);
            return $"\"{validatedCol}\" {direction}";
        });
        sqlParts.Add($"ORDER BY {string.Join(", ", orderClauses)}");

        // PostgreSQL uses LIMIT
        sqlParts.Add($"LIMIT {pageSize + 1}");

        return string.Join(" ", sqlParts);
    }

    private static string BuildKeysetFilter(string keyColumn, bool isDescending, bool isBackward)
    {
        // For forward + ascending: key > cursorKey
        // For forward + descending: key < cursorKey
        // For backward + ascending: key < cursorKey
        // For backward + descending: key > cursorKey
        var useGreaterThan = isDescending == isBackward;
        var op = useGreaterThan ? ">" : "<";

        return $"\"{keyColumn}\" {op} @cursorKey";
    }

    private static string BuildCompositeKeysetFilter(
        string[] keyColumns,
        bool[] keyDescending,
        bool isBackward)
    {
        // Build compound comparison: (A > @a) OR (A = @a AND B > @b) OR ...
        var clauses = new List<string>();

        for (var i = 0; i < keyColumns.Length; i++)
        {
            var clauseParts = new List<string>();

            // Equality chain for all previous columns
            for (var j = 0; j < i; j++)
            {
                var col = SqlIdentifierValidator.ValidateTableName(keyColumns[j], "column");
                clauseParts.Add($"\"{col}\" = @cursor_{keyColumns[j]}");
            }

            // Comparison for current column
            var currentCol = SqlIdentifierValidator.ValidateTableName(keyColumns[i], "column");
            var useGreaterThan = keyDescending[i] == isBackward;
            var op = useGreaterThan ? ">" : "<";
            clauseParts.Add($"\"{currentCol}\" {op} @cursor_{keyColumns[i]}");

            clauses.Add($"({string.Join(" AND ", clauseParts)})");
        }

        return $"({string.Join(" OR ", clauses)})";
    }

    private static string GetEffectiveOrderDirection(bool isDescending, bool isBackward)
    {
        // For backward pagination, we flip the order direction
        var effectiveDescending = isBackward ? !isDescending : isDescending;
        return effectiveDescending ? "DESC" : "ASC";
    }

    #endregion

    #region Result Processing

    private CursorPaginatedResult<TEntity> ProcessResults(
        List<TEntity> itemsWithExtra,
        int pageSize,
        string keyColumn,
        string? cursor,
        bool isBackward)
    {
        var hasMoreItems = itemsWithExtra.Count > pageSize;
        if (hasMoreItems)
        {
            itemsWithExtra.RemoveAt(itemsWithExtra.Count - 1);
        }

        // For backward pagination, reverse results back to original order
        if (isBackward)
        {
            itemsWithExtra.Reverse();
        }

        if (itemsWithExtra.Count == 0)
        {
            return CursorPaginatedResult<TEntity>.Empty(pageSize);
        }

        // Get property accessor for key column
        var keyProperty = typeof(TEntity).GetProperty(keyColumn)
            ?? throw new InvalidOperationException($"Property '{keyColumn}' not found on type {typeof(TEntity).Name}.");

        // Build cursor items
        var cursorItems = itemsWithExtra
            .Select(item =>
            {
                var keyValue = keyProperty.GetValue(item);
                return new CursorItem<TEntity>(item, _cursorEncoder.Encode(keyValue)!);
            })
            .ToList();

        var pageInfo = new CursorPageInfo(
            HasPreviousPage: isBackward ? hasMoreItems : !string.IsNullOrEmpty(cursor),
            HasNextPage: isBackward ? !string.IsNullOrEmpty(cursor) : hasMoreItems,
            StartCursor: cursorItems[0].Cursor,
            EndCursor: cursorItems[^1].Cursor);

        var pagedData = new CursorPagedData<TEntity>(cursorItems, pageInfo);
        return CursorPaginatedResult<TEntity>.FromPagedData(pagedData);
    }

    private CursorPaginatedResult<TEntity> ProcessCompositeResults(
        List<TEntity> itemsWithExtra,
        int pageSize,
        string[] keyColumns,
        string? cursor,
        bool isBackward,
        Func<TEntity, object>? keySelector)
    {
        var hasMoreItems = itemsWithExtra.Count > pageSize;
        if (hasMoreItems)
        {
            itemsWithExtra.RemoveAt(itemsWithExtra.Count - 1);
        }

        if (isBackward)
        {
            itemsWithExtra.Reverse();
        }

        if (itemsWithExtra.Count == 0)
        {
            return CursorPaginatedResult<TEntity>.Empty(pageSize);
        }

        // Get property accessors for key columns
        var keyProperties = keyColumns
            .Select(col => typeof(TEntity).GetProperty(col)
                ?? throw new InvalidOperationException($"Property '{col}' not found on type {typeof(TEntity).Name}."))
            .ToArray();

        // Build cursor items with composite key
        var cursorItems = itemsWithExtra
            .Select(item =>
            {
                object cursorValue;
                if (keySelector is not null)
                {
                    cursorValue = keySelector(item);
                }
                else
                {
                    // Build dictionary from properties
                    var dict = new Dictionary<string, object?>();
                    for (var i = 0; i < keyColumns.Length; i++)
                    {
                        dict[keyColumns[i]] = keyProperties[i].GetValue(item);
                    }

                    cursorValue = dict;
                }

                return new CursorItem<TEntity>(item, _cursorEncoder.Encode(cursorValue)!);
            })
            .ToList();

        var pageInfo = new CursorPageInfo(
            HasPreviousPage: isBackward ? hasMoreItems : !string.IsNullOrEmpty(cursor),
            HasNextPage: isBackward ? !string.IsNullOrEmpty(cursor) : hasMoreItems,
            StartCursor: cursorItems[0].Cursor,
            EndCursor: cursorItems[^1].Cursor);

        var pagedData = new CursorPagedData<TEntity>(cursorItems, pageInfo);
        return CursorPaginatedResult<TEntity>.FromPagedData(pagedData);
    }

    #endregion

    #region JSON Element Conversion

    /// <summary>
    /// Converts a JsonElement to a native CLR type that can be used as a SQL parameter.
    /// </summary>
    /// <param name="value">The value which may be a JsonElement.</param>
    /// <returns>The converted native type, or the original value if not a JsonElement.</returns>
    private static object? ConvertJsonElementToNativeType(object? value)
    {
        if (value is not JsonElement element)
        {
            return value;
        }

        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetRawText()
        };
    }

    #endregion
}

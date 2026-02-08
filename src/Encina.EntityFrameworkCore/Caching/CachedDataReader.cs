using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Encina.EntityFrameworkCore.Caching;

/// <summary>
/// A <see cref="DbDataReader"/> implementation that serves cached query results
/// to EF Core's entity materializer.
/// </summary>
/// <remarks>
/// <para>
/// This reader wraps a <see cref="CachedQueryResult"/> and presents it as a forward-only,
/// read-only data reader. When the query cache interceptor detects a cache hit, it creates
/// a <see cref="CachedDataReader"/> and returns it via
/// <c>InterceptionResult&lt;DbDataReader&gt;.SuppressWithResult()</c>, allowing EF Core to
/// materialize entities from cached data without executing a database query.
/// </para>
/// <para>
/// The reader supports a single result set and does not support streaming methods
/// (<see cref="GetStream"/>, <see cref="GetTextReader"/>), which are not used by EF Core's
/// standard entity materialization pipeline.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Usage in the query cache interceptor
/// var cached = await cacheProvider.GetAsync&lt;CachedQueryResult&gt;(cacheKey, ct);
/// if (cached is not null)
/// {
///     var reader = new CachedDataReader(cached);
///     return InterceptionResult&lt;DbDataReader&gt;.SuppressWithResult(reader);
/// }
/// </code>
/// </example>
[SuppressMessage("Design", "CA1010:Generic interface should also be implemented",
    Justification = "DbDataReader inherently implements non-generic IEnumerable. " +
                    "Adding IEnumerable<T> is not meaningful for a data reader.")]
public sealed class CachedDataReader : DbDataReader
{
    private CachedQueryResult? _result;
    private readonly Dictionary<string, int> _nameToOrdinal;
    private readonly Type[] _fieldTypes;
    private int _currentRowIndex = -1;
    private bool _isClosed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachedDataReader"/> class.
    /// </summary>
    /// <param name="result">The cached query result to read from.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is <c>null</c>.</exception>
    public CachedDataReader(CachedQueryResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _result = result;

        // Pre-build name-to-ordinal lookup for fast GetOrdinal() calls.
        _nameToOrdinal = new Dictionary<string, int>(
            result.Columns.Count,
            StringComparer.OrdinalIgnoreCase);

        foreach (var column in result.Columns)
        {
            _nameToOrdinal.TryAdd(column.Name, column.Ordinal);
        }

        // Pre-resolve CLR types from the stored type name strings.
        _fieldTypes = new Type[result.Columns.Count];
        for (var i = 0; i < result.Columns.Count; i++)
        {
            _fieldTypes[i] = Type.GetType(result.Columns[i].FieldType) ?? typeof(object);
        }
    }

    // ──────────────────────────────────────────────
    //  Navigation
    // ──────────────────────────────────────────────

    /// <inheritdoc />
    public override bool Read()
    {
        ThrowIfClosed();

        _currentRowIndex++;
        return _currentRowIndex < _result!.Rows.Count;
    }

    /// <inheritdoc />
    public override Task<bool> ReadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Read());
    }

    /// <inheritdoc />
    /// <remarks>Returns <c>false</c>; only a single result set is supported.</remarks>
    public override bool NextResult() => false;

    /// <inheritdoc />
    /// <remarks>Returns <c>false</c>; only a single result set is supported.</remarks>
    public override Task<bool> NextResultAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(false);
    }

    // ──────────────────────────────────────────────
    //  Properties
    // ──────────────────────────────────────────────

    /// <inheritdoc />
    public override int FieldCount => _result?.Columns.Count ?? 0;

    /// <inheritdoc />
    public override bool HasRows => _result is { Rows.Count: > 0 };

    /// <inheritdoc />
    public override bool IsClosed => _isClosed;

    /// <inheritdoc />
    /// <remarks>Always returns <c>-1</c> for query results (SELECT statements do not affect rows).</remarks>
    public override int RecordsAffected => -1;

    /// <inheritdoc />
    public override int Depth => 0;

    // ──────────────────────────────────────────────
    //  Generic value access
    // ──────────────────────────────────────────────

    /// <inheritdoc />
    public override object GetValue(int ordinal)
    {
        ThrowIfClosed();
        ThrowIfNoCurrentRow();
        ValidateOrdinal(ordinal);

        return _result!.Rows[_currentRowIndex][ordinal] ?? DBNull.Value;
    }

    /// <inheritdoc />
    public override int GetValues(object[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        ThrowIfClosed();
        ThrowIfNoCurrentRow();

        var count = Math.Min(values.Length, FieldCount);
        var row = _result!.Rows[_currentRowIndex];

        for (var i = 0; i < count; i++)
        {
            values[i] = row[i] ?? DBNull.Value;
        }

        return count;
    }

    /// <inheritdoc />
    public override bool IsDBNull(int ordinal)
    {
        ThrowIfClosed();
        ThrowIfNoCurrentRow();
        ValidateOrdinal(ordinal);

        return _result!.Rows[_currentRowIndex][ordinal] is null;
    }

    /// <inheritdoc />
    public override Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(IsDBNull(ordinal));
    }

    /// <inheritdoc />
    public override object this[int ordinal] => GetValue(ordinal);

    /// <inheritdoc />
    public override object this[string name] => GetValue(GetOrdinal(name));

    // ──────────────────────────────────────────────
    //  Typed getters
    // ──────────────────────────────────────────────

    /// <inheritdoc />
    public override bool GetBoolean(int ordinal) => GetFieldValue<bool>(ordinal);

    /// <inheritdoc />
    public override byte GetByte(int ordinal) => GetFieldValue<byte>(ordinal);

    /// <inheritdoc />
    public override char GetChar(int ordinal) => GetFieldValue<char>(ordinal);

    /// <inheritdoc />
    public override DateTime GetDateTime(int ordinal) => GetFieldValue<DateTime>(ordinal);

    /// <inheritdoc />
    public override decimal GetDecimal(int ordinal) => GetFieldValue<decimal>(ordinal);

    /// <inheritdoc />
    public override double GetDouble(int ordinal) => GetFieldValue<double>(ordinal);

    /// <inheritdoc />
    public override float GetFloat(int ordinal) => GetFieldValue<float>(ordinal);

    /// <inheritdoc />
    public override Guid GetGuid(int ordinal) => GetFieldValue<Guid>(ordinal);

    /// <inheritdoc />
    public override short GetInt16(int ordinal) => GetFieldValue<short>(ordinal);

    /// <inheritdoc />
    public override int GetInt32(int ordinal) => GetFieldValue<int>(ordinal);

    /// <inheritdoc />
    public override long GetInt64(int ordinal) => GetFieldValue<long>(ordinal);

    /// <inheritdoc />
    public override string GetString(int ordinal) => GetFieldValue<string>(ordinal);

    /// <inheritdoc />
    public override T GetFieldValue<T>(int ordinal)
    {
        ThrowIfClosed();
        ThrowIfNoCurrentRow();
        ValidateOrdinal(ordinal);

        var value = _result!.Rows[_currentRowIndex][ordinal];

        if (value is null)
        {
            // For nullable value types and reference types, return default
            // For non-nullable value types, this will throw at the cast site
            if (default(T) is null)
            {
                return default!;
            }

            throw new InvalidCastException(
                $"Cannot convert NULL value at ordinal {ordinal} to non-nullable type '{typeof(T).Name}'.");
        }

        // System.Text.Json deserializes numbers as JsonElement; handle conversion.
        if (value is System.Text.Json.JsonElement jsonElement)
        {
            return ConvertJsonElement<T>(jsonElement, ordinal);
        }

        if (value is T typed)
        {
            return typed;
        }

        // Attempt conversion for compatible types (e.g., int64 → int32, double → decimal).
        try
        {
            return (T)Convert.ChangeType(value, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
        {
            throw new InvalidCastException(
                $"Cannot convert value of type '{value.GetType().Name}' to '{typeof(T).Name}' at ordinal {ordinal}.",
                ex);
        }
    }

    /// <inheritdoc />
    public override Task<T> GetFieldValueAsync<T>(int ordinal, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(GetFieldValue<T>(ordinal));
    }

    /// <inheritdoc />
    /// <remarks>
    /// Not supported for cached data. EF Core does not use this method for standard entity materialization.
    /// </remarks>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) =>
        throw new NotSupportedException(
            "GetBytes is not supported by CachedDataReader. " +
            "Binary data should be accessed via GetFieldValue<byte[]>().");

    /// <inheritdoc />
    /// <remarks>
    /// Not supported for cached data. EF Core does not use this method for standard entity materialization.
    /// </remarks>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) =>
        throw new NotSupportedException(
            "GetChars is not supported by CachedDataReader. " +
            "String data should be accessed via GetString().");

    /// <inheritdoc />
    /// <remarks>
    /// Not supported for cached data. Streaming is not applicable for in-memory cached results.
    /// </remarks>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public override System.IO.Stream GetStream(int ordinal) =>
        throw new NotSupportedException(
            "GetStream is not supported by CachedDataReader. " +
            "Cached results are served from memory and do not support streaming access.");

    /// <inheritdoc />
    /// <remarks>
    /// Not supported for cached data. Streaming is not applicable for in-memory cached results.
    /// </remarks>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public override System.IO.TextReader GetTextReader(int ordinal) =>
        throw new NotSupportedException(
            "GetTextReader is not supported by CachedDataReader. " +
            "Cached results are served from memory and do not support streaming access.");

    // ──────────────────────────────────────────────
    //  Schema access
    // ──────────────────────────────────────────────

    /// <inheritdoc />
    public override string GetName(int ordinal)
    {
        ThrowIfClosed();
        ValidateOrdinal(ordinal);

        return _result!.Columns[ordinal].Name;
    }

    /// <inheritdoc />
    [SuppressMessage("Usage", "CA2201:Do not raise reserved exception types",
        Justification = "DbDataReader.GetOrdinal() API contract specifies IndexOutOfRangeException " +
                        "when the column name is not found.")]
    public override int GetOrdinal(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        ThrowIfClosed();

        if (_nameToOrdinal.TryGetValue(name, out var ordinal))
        {
            return ordinal;
        }

        throw new IndexOutOfRangeException($"Column '{name}' was not found in the cached result set.");
    }

    /// <inheritdoc />
    public override Type GetFieldType(int ordinal)
    {
        ThrowIfClosed();
        ValidateOrdinal(ordinal);

        return _fieldTypes[ordinal];
    }

    /// <inheritdoc />
    public override string GetDataTypeName(int ordinal)
    {
        ThrowIfClosed();
        ValidateOrdinal(ordinal);

        return _result!.Columns[ordinal].DataTypeName;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Returns a <see cref="DataTable"/> populated from the cached column schema metadata.
    /// </remarks>
    public override DataTable GetSchemaTable()
    {
        ThrowIfClosed();

        var schemaTable = new DataTable("SchemaTable");

        schemaTable.Columns.Add("ColumnName", typeof(string));
        schemaTable.Columns.Add("ColumnOrdinal", typeof(int));
        schemaTable.Columns.Add("DataType", typeof(Type));
        schemaTable.Columns.Add("DataTypeName", typeof(string));
        schemaTable.Columns.Add("AllowDBNull", typeof(bool));

        foreach (var column in _result!.Columns)
        {
            var row = schemaTable.NewRow();
            row["ColumnName"] = column.Name;
            row["ColumnOrdinal"] = column.Ordinal;
            row["DataType"] = Type.GetType(column.FieldType) ?? typeof(object);
            row["DataTypeName"] = column.DataTypeName;
            row["AllowDBNull"] = column.AllowDBNull;
            schemaTable.Rows.Add(row);
        }

        return schemaTable;
    }

    // ──────────────────────────────────────────────
    //  Enumerator
    // ──────────────────────────────────────────────

    /// <inheritdoc />
    public override IEnumerator GetEnumerator() => new DbEnumerator(this, closeReader: false);

    // ──────────────────────────────────────────────
    //  Lifecycle
    // ──────────────────────────────────────────────

    /// <inheritdoc />
    public override void Close()
    {
        _isClosed = true;
        _result = null;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Close();
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        Close();
        await base.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    // ──────────────────────────────────────────────
    //  Private helpers
    // ──────────────────────────────────────────────

    private void ThrowIfClosed()
    {
        if (_isClosed)
        {
            throw new InvalidOperationException("Cannot perform this operation on a closed CachedDataReader.");
        }
    }

    private void ThrowIfNoCurrentRow()
    {
        if (_currentRowIndex < 0 || _currentRowIndex >= _result!.Rows.Count)
        {
            throw new InvalidOperationException(
                "No current row. Call Read() before accessing data, and verify it returns true.");
        }
    }

    [SuppressMessage("Usage", "CA2201:Do not raise reserved exception types",
        Justification = "DbDataReader accessors require IndexOutOfRangeException for invalid ordinals " +
                        "per the ADO.NET data provider contract.")]
    private void ValidateOrdinal(int ordinal)
    {
        // _result is guaranteed non-null here because ThrowIfClosed() is always called
        // before ValidateOrdinal(), and Close() is the only method that sets _result to null.
        var columnCount = _result!.Columns.Count;
        if (ordinal < 0 || ordinal >= columnCount)
        {
            throw new IndexOutOfRangeException(
                $"Column ordinal {ordinal} is out of range. The result set has {columnCount} columns (0-{columnCount - 1}).");
        }
    }

    /// <summary>
    /// Converts a <see cref="System.Text.Json.JsonElement"/> to the requested type.
    /// </summary>
    /// <remarks>
    /// System.Text.Json deserializes <c>object?[]</c> values as <see cref="System.Text.Json.JsonElement"/>
    /// rather than their original CLR types. This method handles the conversion for all types
    /// commonly used in EF Core entity materialization.
    /// </remarks>
    private static T ConvertJsonElement<T>(System.Text.Json.JsonElement element, int ordinal)
    {
        var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

        try
        {
            object? result = targetType switch
            {
                _ when targetType == typeof(bool) => element.GetBoolean(),
                _ when targetType == typeof(byte) => element.GetByte(),
                _ when targetType == typeof(short) => element.GetInt16(),
                _ when targetType == typeof(int) => element.GetInt32(),
                _ when targetType == typeof(long) => element.GetInt64(),
                _ when targetType == typeof(float) => element.GetSingle(),
                _ when targetType == typeof(double) => element.GetDouble(),
                _ when targetType == typeof(decimal) => element.GetDecimal(),
                _ when targetType == typeof(string) => element.GetString(),
                _ when targetType == typeof(DateTime) => element.GetDateTime(),
                _ when targetType == typeof(DateTimeOffset) => element.GetDateTimeOffset(),
                _ when targetType == typeof(Guid) => element.GetGuid(),
                _ when targetType == typeof(byte[]) => element.GetBytesFromBase64(),
                _ when targetType == typeof(char) => ConvertToChar(element),
                _ when targetType == typeof(sbyte) => element.GetSByte(),
                _ when targetType == typeof(ushort) => element.GetUInt16(),
                _ when targetType == typeof(uint) => element.GetUInt32(),
                _ when targetType == typeof(ulong) => element.GetUInt64(),
                _ when targetType == typeof(TimeSpan) => TimeSpan.Parse(
                    element.GetString()!, System.Globalization.CultureInfo.InvariantCulture),
                _ => JsonSerializer.Deserialize<T>(element.GetRawText())
            };

            return (T)result!;
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException or OverflowException)
        {
            throw new InvalidCastException(
                $"Cannot convert JsonElement (kind: {element.ValueKind}) to '{typeof(T).Name}' at ordinal {ordinal}.",
                ex);
        }
    }

    private static char ConvertToChar(System.Text.Json.JsonElement element)
    {
        var str = element.GetString();
        return str is { Length: > 0 } ? str[0] : '\0';
    }
}

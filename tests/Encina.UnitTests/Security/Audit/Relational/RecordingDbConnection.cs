using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace Encina.UnitTests.Security.Audit.Relational;

/// <summary>
/// A <see cref="DbConnection"/> that executes nothing and records, for every command it is asked to run,
/// the SQL text and the value of every bound parameter at execution time.
/// </summary>
/// <remarks>
/// It lets a test observe exactly what an audit store hands to the driver, which is the boundary where the
/// UTC normalisation happens, without depending on the machine's time zone or on a database.
/// </remarks>
internal sealed class RecordingDbConnection : DbConnection
{
    private readonly List<RecordedCommand> _executed = [];

    /// <summary>Gets or sets the rows returned by every reader the connection opens (empty by default).</summary>
    public DataTable ReaderRows { get; set; } = new();

    /// <summary>Gets the commands executed so far, in order.</summary>
    public IReadOnlyList<RecordedCommand> Executed => _executed;

    /// <summary>Gets every parameter bound by every executed command.</summary>
    public IEnumerable<RecordedParameter> BoundParameters => _executed.SelectMany(c => c.Parameters);

    internal void Record(RecordedCommand command) => _executed.Add(command);

    [AllowNull]
    public override string ConnectionString { get; set; } = string.Empty;

    public override string Database => "recording";

    public override string DataSource => "recording";

    public override string ServerVersion => "0";

    public override ConnectionState State => ConnectionState.Open;

    public override void ChangeDatabase(string databaseName)
    {
    }

    public override void Close()
    {
    }

    public override void Open()
    {
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
        throw new NotSupportedException();

    protected override DbCommand CreateDbCommand() => new RecordingDbCommand(this);
}

/// <summary>A command executed by <see cref="RecordingDbConnection"/> with the parameters it carried.</summary>
/// <param name="CommandText">The SQL text.</param>
/// <param name="Parameters">The parameters bound when the command ran.</param>
internal sealed record RecordedCommand(string CommandText, IReadOnlyList<RecordedParameter> Parameters);

/// <summary>A parameter name and value as bound at execution time.</summary>
/// <param name="Name">The parameter name, without the provider prefix.</param>
/// <param name="Value">The bound value, as the driver would receive it.</param>
internal sealed record RecordedParameter(string Name, object? Value);

internal sealed class RecordingDbCommand(RecordingDbConnection owner) : DbCommand
{
    private readonly RecordingDbParameterCollection _parameters = new();

    [AllowNull]
    public override string CommandText { get; set; } = string.Empty;

    public override int CommandTimeout { get; set; }

    public override CommandType CommandType { get; set; } = CommandType.Text;

    public override bool DesignTimeVisible { get; set; }

    public override UpdateRowSource UpdatedRowSource { get; set; }

    protected override DbConnection? DbConnection
    {
        get => owner;
        set { }
    }

    protected override DbParameterCollection DbParameterCollection => _parameters;

    protected override DbTransaction? DbTransaction { get; set; }

    public override void Cancel()
    {
    }

    public override int ExecuteNonQuery()
    {
        Capture();
        return 1;
    }

    public override object? ExecuteScalar()
    {
        Capture();
        return 0;
    }

    public override void Prepare()
    {
    }

    protected override DbParameter CreateDbParameter() => new RecordingDbParameter();

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        Capture();
        return owner.ReaderRows.CreateDataReader();
    }

    private void Capture()
    {
        var snapshot = _parameters.Items
            .Select(p => new RecordedParameter(p.ParameterName.TrimStart('@', ':', '?'), p.Value))
            .ToList();
        owner.Record(new RecordedCommand(CommandText, snapshot));
    }
}

internal sealed class RecordingDbParameter : DbParameter
{
    public override DbType DbType { get; set; }

    public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;

    public override bool IsNullable { get; set; }

    [AllowNull]
    public override string ParameterName { get; set; } = string.Empty;

    public override int Size { get; set; }

    [AllowNull]
    public override string SourceColumn { get; set; } = string.Empty;

    public override bool SourceColumnNullMapping { get; set; }

    public override object? Value { get; set; }

    public override void ResetDbType()
    {
    }
}

internal sealed class RecordingDbParameterCollection : DbParameterCollection
{
    private readonly List<DbParameter> _items = [];

    internal IReadOnlyList<DbParameter> Items => _items;

    public override int Count => _items.Count;

    public override object SyncRoot => ((ICollection)_items).SyncRoot;

    public override int Add(object value)
    {
        _items.Add((DbParameter)value);
        return _items.Count - 1;
    }

    public override void AddRange(Array values)
    {
        foreach (var value in values)
        {
            Add(value);
        }
    }

    public override void Clear() => _items.Clear();

    public override bool Contains(object value) => _items.Contains((DbParameter)value);

    public override bool Contains(string value) => IndexOf(value) >= 0;

    public override void CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);

    public override IEnumerator GetEnumerator() => _items.GetEnumerator();

    public override int IndexOf(object value) => _items.IndexOf((DbParameter)value);

    public override int IndexOf(string parameterName) =>
        _items.FindIndex(p => string.Equals(p.ParameterName, parameterName, StringComparison.Ordinal));

    public override void Insert(int index, object value) => _items.Insert(index, (DbParameter)value);

    public override void Remove(object value) => _items.Remove((DbParameter)value);

    public override void RemoveAt(int index) => _items.RemoveAt(index);

    public override void RemoveAt(string parameterName) => _items.RemoveAt(IndexOf(parameterName));

    protected override DbParameter GetParameter(int index) => _items[index];

    protected override DbParameter GetParameter(string parameterName) => _items[IndexOf(parameterName)];

    protected override void SetParameter(int index, DbParameter value) => _items[index] = value;

    protected override void SetParameter(string parameterName, DbParameter value) => _items[IndexOf(parameterName)] = value;
}

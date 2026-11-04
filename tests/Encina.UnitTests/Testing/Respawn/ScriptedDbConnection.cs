using System.Data;
using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace Encina.UnitTests.Testing.Respawn;

/// <summary>
/// In-process <see cref="DbConnection"/> that answers every query with a result set chosen by a
/// caller-supplied function and records the statements it receives, so that the Respawn
/// discovery and reset code can be exercised without a database server.
/// </summary>
internal sealed class ScriptedDbConnection : DbConnection
{
    private readonly Func<string, DataTable> _resultFor;
    private ConnectionState _state = ConnectionState.Closed;

    public ScriptedDbConnection(Func<string, DataTable> resultFor, List<string> executed)
    {
        _resultFor = resultFor;
        Executed = executed;
    }

    /// <summary>Gets the statements received, in order (queries and non-queries).</summary>
    public List<string> Executed { get; }

    /// <summary>Gets how many times the connection was opened.</summary>
    public int OpenCount { get; private set; }

#pragma warning disable CS8765 // Nullability of parameter doesn't match overridden member
    public override string ConnectionString { get; set; } = string.Empty;
#pragma warning restore CS8765

    public override string Database => "scripted";

    public override string DataSource => "scripted";

    public override string ServerVersion => "1.0";

    public override ConnectionState State => _state;

    public override void ChangeDatabase(string databaseName)
    {
    }

    public override void Close() => _state = ConnectionState.Closed;

    public override void Open()
    {
        OpenCount++;
        _state = ConnectionState.Open;
    }

    public override Task OpenAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Open();
        return Task.CompletedTask;
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
        new ScriptedDbTransaction(this, isolationLevel);

    protected override DbCommand CreateDbCommand() => new ScriptedDbCommand(this);

    internal DataTable Resolve(string commandText) => _resultFor(commandText);

    private sealed class ScriptedDbTransaction : DbTransaction
    {
        private readonly ScriptedDbConnection _connection;

        public ScriptedDbTransaction(ScriptedDbConnection connection, IsolationLevel isolationLevel)
        {
            _connection = connection;
            IsolationLevel = isolationLevel;
        }

        public override IsolationLevel IsolationLevel { get; }

        protected override DbConnection DbConnection => _connection;

        public override void Commit()
        {
        }

        public override void Rollback()
        {
        }
    }

    private sealed class ScriptedDbCommand : DbCommand
    {
        private readonly ScriptedDbConnection _connection;
        private readonly DbParameterCollection _parameters = new SqliteCommand().Parameters;

        public ScriptedDbCommand(ScriptedDbConnection connection) => _connection = connection;

#pragma warning disable CS8765
        public override string CommandText { get; set; } = string.Empty;
#pragma warning restore CS8765

        public override int CommandTimeout { get; set; }

        public override CommandType CommandType { get; set; } = CommandType.Text;

        public override bool DesignTimeVisible { get; set; }

        public override UpdateRowSource UpdatedRowSource { get; set; }

        protected override DbConnection? DbConnection
        {
            get => _connection;
            set { }
        }

        protected override DbParameterCollection DbParameterCollection => _parameters;

        protected override DbTransaction? DbTransaction { get; set; }

        public override void Cancel()
        {
        }

        public override int ExecuteNonQuery()
        {
            _connection.Executed.Add(CommandText);
            return 0;
        }

        public override object? ExecuteScalar()
        {
            _connection.Executed.Add(CommandText);
            return null;
        }

        public override void Prepare()
        {
        }

        protected override DbParameter CreateDbParameter() => new SqliteCommand().CreateParameter();

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            _connection.Executed.Add(CommandText);
            return _connection.Resolve(CommandText).CreateDataReader();
        }
    }
}

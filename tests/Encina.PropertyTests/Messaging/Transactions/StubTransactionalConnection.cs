using System.Data;
using System.Data.Common;

namespace Encina.PropertyTests.Messaging.Transactions;

/// <summary>
/// A database-free <see cref="DbConnection"/> whose transaction counts commits and rollbacks, so the real
/// <c>DbTransactionAccessor</c> can be exercised without a database. Only the asynchronous calls are supported.
/// </summary>
internal sealed class StubTransactionalConnection : DbConnection
{
    private ConnectionState _state = ConnectionState.Closed;

    public int BeginCalls { get; private set; }

    public int CommitCalls { get; private set; }

    public int RollbackCalls { get; private set; }

    /// <summary>Raised when a transaction ends: <see langword="true"/> for a commit, <see langword="false"/> for a rollback.</summary>
    public event Action<bool>? TransactionEnded;

    [System.Diagnostics.CodeAnalysis.AllowNull]
    public override string ConnectionString { get; set; } = string.Empty;

    public override string Database => string.Empty;

    public override string DataSource => string.Empty;

    public override string ServerVersion => string.Empty;

    public override ConnectionState State => _state;

    public override void ChangeDatabase(string databaseName)
    {
    }

    public override void Close() => _state = ConnectionState.Closed;

    public override void Open() => throw new NotSupportedException("Only OpenAsync is supported.");

    public override Task OpenAsync(CancellationToken cancellationToken)
    {
        _state = ConnectionState.Open;
        return Task.CompletedTask;
    }

    protected override DbCommand CreateDbCommand() => throw new NotSupportedException();

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        => throw new NotSupportedException("Only BeginTransactionAsync is supported.");

    protected override ValueTask<DbTransaction> BeginDbTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken)
    {
        BeginCalls++;
        return ValueTask.FromResult<DbTransaction>(new StubTransaction(this, isolationLevel));
    }

    private sealed class StubTransaction(StubTransactionalConnection owner, IsolationLevel level) : DbTransaction
    {
        public override IsolationLevel IsolationLevel => level == IsolationLevel.Unspecified ? IsolationLevel.ReadCommitted : level;

        protected override DbConnection? DbConnection => owner;

        public override void Commit() => throw new NotSupportedException("Only CommitAsync is supported.");

        public override void Rollback() => throw new NotSupportedException("Only RollbackAsync is supported.");

        public override Task CommitAsync(CancellationToken cancellationToken = default)
        {
            owner.CommitCalls++;
            owner.TransactionEnded?.Invoke(true);
            return Task.CompletedTask;
        }

        public override Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            owner.RollbackCalls++;
            owner.TransactionEnded?.Invoke(false);
            return Task.CompletedTask;
        }
    }
}

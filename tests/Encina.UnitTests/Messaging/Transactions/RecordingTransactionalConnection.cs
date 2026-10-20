using System.Data;
using System.Data.Common;

namespace Encina.UnitTests.Messaging.Transactions;

/// <summary>
/// A <see cref="DbConnection"/> that records open, begin, commit and rollback calls (and the tokens they got)
/// and can be told to fail at each step. It lets transaction tests run real accessor code without a database.
/// </summary>
internal sealed class RecordingTransactionalConnection : DbConnection
{
    private ConnectionState _state = ConnectionState.Closed;

    public int OpenAsyncCalls { get; private set; }

    public CancellationToken OpenAsyncToken { get; private set; }

    public int BeginAsyncCalls { get; private set; }

    public CancellationToken BeginAsyncToken { get; private set; }

    public IsolationLevel? BeginIsolationLevel { get; private set; }

    public bool WasDisposed { get; private set; }

    public bool ThrowOnOpen { get; set; }

    public bool ThrowOnBegin { get; set; }

    public RecordingTransaction Transaction { get; } = new();

    [System.Diagnostics.CodeAnalysis.AllowNull]
    public override string ConnectionString { get; set; } = string.Empty;

    public override string Database => string.Empty;

    public override string DataSource => string.Empty;

    public override string ServerVersion => string.Empty;

    public override ConnectionState State => _state;

    public void SetState(ConnectionState state) => _state = state;

    public override void ChangeDatabase(string databaseName)
    {
    }

    public override void Close() => _state = ConnectionState.Closed;

    public override void Open() => throw new InvalidOperationException("The synchronous Open must not be used.");

    public override Task OpenAsync(CancellationToken cancellationToken)
    {
        OpenAsyncCalls++;
        OpenAsyncToken = cancellationToken;
        cancellationToken.ThrowIfCancellationRequested();
        if (ThrowOnOpen)
        {
            throw new InvalidOperationException("open failed");
        }

        _state = ConnectionState.Open;
        return Task.CompletedTask;
    }

    protected override DbCommand CreateDbCommand() => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        WasDisposed = true;
        base.Dispose(disposing);
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        => throw new InvalidOperationException("The synchronous BeginTransaction must not be used.");

    protected override ValueTask<DbTransaction> BeginDbTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken)
    {
        BeginAsyncCalls++;
        BeginAsyncToken = cancellationToken;
        BeginIsolationLevel = isolationLevel;
        if (ThrowOnBegin)
        {
            throw new InvalidOperationException("begin failed");
        }

        Transaction.Connection_ = this;
        Transaction.Level = isolationLevel == IsolationLevel.Unspecified ? IsolationLevel.ReadCommitted : isolationLevel;
        return ValueTask.FromResult<DbTransaction>(Transaction);
    }

    internal sealed class RecordingTransaction : DbTransaction
    {
        public DbConnection? Connection_ { get; set; }

        public IsolationLevel Level { get; set; } = IsolationLevel.ReadCommitted;

        public bool ThrowOnCommit { get; set; }

        public bool ThrowOnRollback { get; set; }

        public int CommitAsyncCalls { get; private set; }

        public CancellationToken CommitAsyncToken { get; private set; }

        public int RollbackAsyncCalls { get; private set; }

        public CancellationToken RollbackAsyncToken { get; private set; }

        public int DisposeAsyncCalls { get; private set; }

        public override IsolationLevel IsolationLevel => Level;

        protected override DbConnection? DbConnection => Connection_;

        public override void Commit() => throw new InvalidOperationException("The synchronous Commit must not be used.");

        public override void Rollback() => throw new InvalidOperationException("The synchronous Rollback must not be used.");

        public override Task CommitAsync(CancellationToken cancellationToken = default)
        {
            CommitAsyncCalls++;
            CommitAsyncToken = cancellationToken;
            return ThrowOnCommit ? Task.FromException(new InvalidOperationException("commit failed")) : Task.CompletedTask;
        }

        public override Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            RollbackAsyncCalls++;
            RollbackAsyncToken = cancellationToken;
            return ThrowOnRollback ? Task.FromException(new InvalidOperationException("rollback failed")) : Task.CompletedTask;
        }

        public override ValueTask DisposeAsync()
        {
            DisposeAsyncCalls++;
            return base.DisposeAsync();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}

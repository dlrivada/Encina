using System.Data;
using System.Data.Common;
using Encina.Messaging;
using LanguageExt;

namespace Encina.UnitTests.Messaging.Behaviors;

/// <summary>
/// Unit tests for TransactionPipelineBehavior.
/// </summary>
// Primary constructor not used due to mock setup order.
public sealed class TransactionPipelineBehaviorTests
{
    private readonly IDbConnection _connection = Substitute.For<IDbConnection>();
    private readonly IDbTransaction _transaction = Substitute.For<IDbTransaction>();
    private readonly TransactionPipelineBehavior<TestRequest, string> _behavior;

    public TransactionPipelineBehaviorTests()
    {
        _connection.BeginTransaction().Returns(_transaction);
        _connection.State.Returns(ConnectionState.Open);
        _behavior = new TransactionPipelineBehavior<TestRequest, string>(_connection);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new TransactionPipelineBehavior<TestRequest, string>(null!);

        // Assert
        var exception = act.ShouldThrow<ArgumentNullException>();
        exception.ParamName.ShouldBe("connection");
    }

    #endregion

    #region Handle Tests - Success Path

    [Fact]
    public async Task Handle_HandlerReturnsSuccess_CommitsTransaction()
    {
        // Arrange
        var request = new TestRequest(Guid.NewGuid());
        var context = CreateTestContext();
        var expectedResult = "Success";

        RequestHandlerCallback<string> nextStep = () =>
            ValueTask.FromResult<Either<EncinaError, string>>(expectedResult);

        // Act
        var result = await _behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: value => value.ShouldBe(expectedResult),
            Left: _ => throw new InvalidOperationException("Expected Right"));

        _transaction.Received(1).Commit();
        _transaction.DidNotReceive().Rollback();
    }

    [Fact]
    public async Task Handle_ConnectionClosed_OpensConnection()
    {
        // Arrange
        var closedConnection = Substitute.For<IDbConnection>();
        closedConnection.State.Returns(ConnectionState.Closed);
        closedConnection.BeginTransaction().Returns(_transaction);

        var behavior = new TransactionPipelineBehavior<TestRequest, string>(closedConnection);
        var request = new TestRequest(Guid.NewGuid());
        var context = CreateTestContext();

        RequestHandlerCallback<string> nextStep = () =>
            ValueTask.FromResult<Either<EncinaError, string>>("Success");

        // Act
        await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        closedConnection.Received(1).Open();
    }

    [Fact]
    public async Task Handle_ConnectionAlreadyOpen_DoesNotOpenAgain()
    {
        // Arrange
        var request = new TestRequest(Guid.NewGuid());
        var context = CreateTestContext();

        RequestHandlerCallback<string> nextStep = () =>
            ValueTask.FromResult<Either<EncinaError, string>>("Success");

        // Act
        await _behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        _connection.DidNotReceive().Open();
    }

    #endregion

    #region Handle Tests - Error Path

    [Fact]
    public async Task Handle_HandlerReturnsError_RollsBackTransaction()
    {
        // Arrange
        var request = new TestRequest(Guid.NewGuid());
        var context = CreateTestContext();
        var error = EncinaErrors.Create("test.error", "Test error");

        RequestHandlerCallback<string> nextStep = () =>
            ValueTask.FromResult<Either<EncinaError, string>>(error);

        // Act
        var result = await _behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: err => err.Message.ShouldBe("Test error"));

        _transaction.Received(1).Rollback();
        _transaction.DidNotReceive().Commit();
    }

    [Fact]
    public async Task Handle_HandlerThrows_RollsBackTransactionAndReturnsLeft()
    {
        // Arrange
        var request = new TestRequest(Guid.NewGuid());
        var context = CreateTestContext();

        RequestHandlerCallback<string> nextStep = () =>
            throw new InvalidOperationException("Handler failed");

        // Act
        var result = await _behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        _transaction.Received(1).Rollback();
        _transaction.DidNotReceive().Commit();
    }

    #endregion

    #region Handle Tests - Guard Clauses

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var context = CreateTestContext();
        RequestHandlerCallback<string> nextStep = () =>
            ValueTask.FromResult<Either<EncinaError, string>>("Success");

        // Act
        var act = async () => await _behavior.Handle(null!, context, nextStep, CancellationToken.None);

        // Assert
        var exception = await act.ShouldThrowAsync<ArgumentNullException>();
        exception.ParamName.ShouldBe("request");
    }

    [Fact]
    public async Task Handle_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var request = new TestRequest(Guid.NewGuid());
        RequestHandlerCallback<string> nextStep = () =>
            ValueTask.FromResult<Either<EncinaError, string>>("Success");

        // Act
        var act = async () => await _behavior.Handle(request, null!, nextStep, CancellationToken.None);

        // Assert
        var exception = await act.ShouldThrowAsync<ArgumentNullException>();
        exception.ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task Handle_NullNextStep_ThrowsArgumentNullException()
    {
        // Arrange
        var request = new TestRequest(Guid.NewGuid());
        var context = CreateTestContext();

        // Act
        var act = async () => await _behavior.Handle(request, context, null!, CancellationToken.None);

        // Assert
        var exception = await act.ShouldThrowAsync<ArgumentNullException>();
        exception.ParamName.ShouldBe("nextStep");
    }

    #endregion

    #region Handle Tests - Transaction Disposal

    [Fact]
    public async Task Handle_WhenCompleted_DisposesTransaction()
    {
        // Arrange
        var request = new TestRequest(Guid.NewGuid());
        var context = CreateTestContext();

        RequestHandlerCallback<string> nextStep = () =>
            ValueTask.FromResult<Either<EncinaError, string>>("Success");

        // Act
        await _behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        _transaction.Received(1).Dispose();
    }

    [Fact]
    public async Task Handle_WhenExceptionThrown_DisposesTransaction()
    {
        // Arrange
        var request = new TestRequest(Guid.NewGuid());
        var context = CreateTestContext();

        RequestHandlerCallback<string> nextStep = () =>
            throw new InvalidOperationException("Handler failed");

        // Act
        var result = await _behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        _transaction.Received(1).Dispose();
    }

    #endregion

    #region Handle Tests - Async Path (DbConnection)

    [Fact]
    public async Task Handle_DbConnectionClosed_OpensConnectionAsync()
    {
        // Arrange
        var fakeConnection = new FakeDbConnection();
        fakeConnection.SetState(ConnectionState.Closed);

        var behavior = new TransactionPipelineBehavior<TestRequest, string>(fakeConnection);
        var request = new TestRequest(Guid.NewGuid());
        var context = CreateTestContext();
        using var cts = new CancellationTokenSource();

        RequestHandlerCallback<string> nextStep = () =>
            ValueTask.FromResult<Either<EncinaError, string>>("Success");

        // Act
        await behavior.Handle(request, context, nextStep, cts.Token);

        // Assert
        fakeConnection.OpenAsyncCalls.ShouldBe(1);
        fakeConnection.OpenCalls.ShouldBe(0);
        fakeConnection.OpenAsyncToken.ShouldBe(cts.Token);
    }

    [Fact]
    public async Task Handle_DbConnection_UsesBeginTransactionAsync_AndCommitAsync()
    {
        // Arrange
        var fakeConnection = new FakeDbConnection();
        fakeConnection.SetState(ConnectionState.Open);

        var behavior = new TransactionPipelineBehavior<TestRequest, string>(fakeConnection);
        var request = new TestRequest(Guid.NewGuid());
        var context = CreateTestContext();
        using var cts = new CancellationTokenSource();

        RequestHandlerCallback<string> nextStep = () =>
            ValueTask.FromResult<Either<EncinaError, string>>("Success");

        // Act
        var result = await behavior.Handle(request, context, nextStep, cts.Token);

        // Assert
        result.IsRight.ShouldBeTrue();
        fakeConnection.BeginTransactionAsyncCalls.ShouldBe(1);
        fakeConnection.BeginTransactionCalls.ShouldBe(0);
        fakeConnection.FakeTransaction.CommitAsyncCalls.ShouldBe(1);
        fakeConnection.FakeTransaction.CommitCalls.ShouldBe(0);
        fakeConnection.FakeTransaction.RollbackAsyncCalls.ShouldBe(0);
        fakeConnection.FakeTransaction.DisposeAsyncCalls.ShouldBe(1);
        fakeConnection.BeginTransactionAsyncToken.ShouldBe(cts.Token);
        fakeConnection.FakeTransaction.CommitAsyncToken.ShouldBe(CancellationToken.None);
    }

    [Fact]
    public async Task Handle_DbConnection_HandlerReturnsError_UsesRollbackAsync()
    {
        // Arrange
        var fakeConnection = new FakeDbConnection();
        fakeConnection.SetState(ConnectionState.Open);

        var behavior = new TransactionPipelineBehavior<TestRequest, string>(fakeConnection);
        var request = new TestRequest(Guid.NewGuid());
        var context = CreateTestContext();
        var error = EncinaErrors.Create("test.error", "Test error");
        using var cts = new CancellationTokenSource();

        RequestHandlerCallback<string> nextStep = () =>
            ValueTask.FromResult<Either<EncinaError, string>>(error);

        // Act
        var result = await behavior.Handle(request, context, nextStep, cts.Token);

        // Assert
        result.IsLeft.ShouldBeTrue();
        fakeConnection.FakeTransaction.RollbackAsyncCalls.ShouldBe(1);
        fakeConnection.FakeTransaction.RollbackCalls.ShouldBe(0);
        fakeConnection.FakeTransaction.CommitAsyncCalls.ShouldBe(0);
        fakeConnection.FakeTransaction.DisposeAsyncCalls.ShouldBe(1);
        fakeConnection.FakeTransaction.RollbackAsyncToken.ShouldBe(CancellationToken.None);
    }

    [Fact]
    public async Task Handle_DbConnection_HandlerThrows_UsesRollbackAsyncWithoutPropagatingCallerToken()
    {
        // Arrange
        var fakeConnection = new FakeDbConnection();
        fakeConnection.SetState(ConnectionState.Open);

        var behavior = new TransactionPipelineBehavior<TestRequest, string>(fakeConnection);
        var request = new TestRequest(Guid.NewGuid());
        var context = CreateTestContext();
        using var cts = new CancellationTokenSource();

        RequestHandlerCallback<string> nextStep = () =>
            throw new InvalidOperationException("Handler failed");

        // Act
        var result = await behavior.Handle(request, context, nextStep, cts.Token);

        // Assert
        result.IsLeft.ShouldBeTrue();
        fakeConnection.FakeTransaction.RollbackAsyncCalls.ShouldBe(1);
        fakeConnection.FakeTransaction.DisposeAsyncCalls.ShouldBe(1);
        fakeConnection.FakeTransaction.RollbackAsyncToken.ShouldBe(CancellationToken.None);
    }

    [Fact]
    public async Task Handle_DbConnection_HandlerThrowsOperationCanceled_RollsBackAndRethrows()
    {
        // Arrange
        var fakeConnection = new FakeDbConnection();
        fakeConnection.SetState(ConnectionState.Open);

        var behavior = new TransactionPipelineBehavior<TestRequest, string>(fakeConnection);
        var request = new TestRequest(Guid.NewGuid());
        var context = CreateTestContext();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        RequestHandlerCallback<string> nextStep = () =>
            throw new OperationCanceledException(cts.Token);

        // Act
        var act = async () => await behavior.Handle(request, context, nextStep, cts.Token);

        // Assert
        await act.ShouldThrowAsync<OperationCanceledException>();
        fakeConnection.FakeTransaction.RollbackAsyncCalls.ShouldBe(1);
        fakeConnection.FakeTransaction.DisposeAsyncCalls.ShouldBe(1);
        fakeConnection.FakeTransaction.RollbackAsyncToken.ShouldBe(CancellationToken.None);
    }

    #endregion

    #region Helpers

    private static IRequestContext CreateTestContext()
    {
        var context = Substitute.For<IRequestContext>();
        context.CorrelationId.Returns(Guid.NewGuid().ToString());
        context.UserId.Returns("test-user");
        context.Timestamp.Returns(DateTimeOffset.UtcNow);
        return context;
    }

    private sealed class FakeDbTransaction : DbTransaction
    {
        public int CommitCalls { get; private set; }
        public int CommitAsyncCalls { get; private set; }
        public int RollbackCalls { get; private set; }
        public int RollbackAsyncCalls { get; private set; }
        public int DisposeCalls { get; private set; }
        public int DisposeAsyncCalls { get; private set; }
        public CancellationToken CommitAsyncToken { get; private set; }
        public CancellationToken RollbackAsyncToken { get; private set; }

        public override IsolationLevel IsolationLevel => IsolationLevel.Unspecified;
        protected override DbConnection? DbConnection => null;

        public override void Commit() => CommitCalls++;

        public override Task CommitAsync(CancellationToken cancellationToken = default)
        {
            CommitAsyncCalls++;
            CommitAsyncToken = cancellationToken;
            return Task.CompletedTask;
        }

        public override void Rollback() => RollbackCalls++;

        public override Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            RollbackAsyncCalls++;
            RollbackAsyncToken = cancellationToken;
            return Task.CompletedTask;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                DisposeCalls++;
            base.Dispose(disposing);
        }

        public override ValueTask DisposeAsync()
        {
            DisposeAsyncCalls++;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeDbConnection : DbConnection
    {
        private ConnectionState _state = ConnectionState.Closed;

        public int OpenCalls { get; private set; }
        public int OpenAsyncCalls { get; private set; }
        public int BeginTransactionCalls { get; private set; }
        public int BeginTransactionAsyncCalls { get; private set; }
        public CancellationToken OpenAsyncToken { get; private set; }
        public CancellationToken BeginTransactionAsyncToken { get; private set; }
        public FakeDbTransaction FakeTransaction { get; } = new();

        [System.Diagnostics.CodeAnalysis.AllowNull]
        public override string ConnectionString { get; set; } = string.Empty;
        public override string Database => string.Empty;
        public override string DataSource => string.Empty;
        public override string ServerVersion => string.Empty;
        public override ConnectionState State => _state;

        public void SetState(ConnectionState state) => _state = state;

        public override void ChangeDatabase(string databaseName) { }

        public override void Close() => _state = ConnectionState.Closed;

        public override void Open()
        {
            OpenCalls++;
            _state = ConnectionState.Open;
        }

        public override Task OpenAsync(CancellationToken cancellationToken)
        {
            OpenAsyncCalls++;
            OpenAsyncToken = cancellationToken;
            _state = ConnectionState.Open;
            return Task.CompletedTask;
        }

        protected override DbCommand CreateDbCommand() => throw new NotImplementedException();

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            BeginTransactionCalls++;
            return FakeTransaction;
        }

        protected override ValueTask<DbTransaction> BeginDbTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken)
        {
            BeginTransactionAsyncCalls++;
            BeginTransactionAsyncToken = cancellationToken;
            return ValueTask.FromResult<DbTransaction>(FakeTransaction);
        }
    }

    #endregion
}

/// <summary>
/// Test request for behavior tests.
/// </summary>
internal sealed record TestRequest(Guid Id) : IRequest<string>;

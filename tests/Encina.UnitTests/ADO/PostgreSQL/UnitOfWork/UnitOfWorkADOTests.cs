using System.Data;
using System.Data.Common;
using Encina;
using Encina.ADO.PostgreSQL.Repository;
using Encina.ADO.PostgreSQL.UnitOfWork;
using Encina.DomainModeling;
using Encina.Testing.Shouldly;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.ADO.PostgreSQL.UnitOfWork;

/// <summary>
/// Unit tests for <see cref="UnitOfWorkADO"/>.
/// </summary>
[Trait("Category", "Unit")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA1001:Types that own disposable fields should be disposable", Justification = "Disposal handled by IAsyncLifetime.DisposeAsync")]
public class UnitOfWorkADOTests : IAsyncLifetime
{
    private readonly IDbConnection _mockConnection;
    private readonly IServiceProvider _mockServiceProvider;
    private readonly UnitOfWorkADO _unitOfWork;

    public UnitOfWorkADOTests()
    {
        _mockConnection = Substitute.For<IDbConnection>();
        _mockServiceProvider = Substitute.For<IServiceProvider>();
        _unitOfWork = new UnitOfWorkADO(_mockConnection, _mockServiceProvider);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await _unitOfWork.DisposeAsync();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new UnitOfWorkADO(null!, _mockServiceProvider));
    }

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new UnitOfWorkADO(_mockConnection, null!));
    }

    #endregion

    #region Repository Tests

    [Fact]
    public void Repository_WithoutMapping_ThrowsInvalidOperationException()
    {
        _mockServiceProvider.GetService(typeof(IEntityMapping<TestADOEntity, Guid>)).Returns((object?)null);

        Should.Throw<InvalidOperationException>(() =>
            _unitOfWork.Repository<TestADOEntity, Guid>());
    }

    [Fact]
    public void Repository_WithMapping_ReturnsRepositoryInstance()
    {
        var mapping = CreateTestMapping();
        _mockServiceProvider.GetService(typeof(IEntityMapping<TestADOEntity, Guid>)).Returns(mapping);

        var repository = _unitOfWork.Repository<TestADOEntity, Guid>();

        repository.ShouldNotBeNull();
        repository.ShouldBeAssignableTo<IFunctionalRepository<TestADOEntity, Guid>>();
    }

    #endregion

    #region SaveChangesAsync Tests

    [Fact]
    public async Task SaveChangesAsync_ReturnsZero()
    {
        var result = await _unitOfWork.SaveChangesAsync();

        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(0));
    }

    #endregion

    #region BeginTransactionAsync / CommitAsync / RollbackAsync - IDbConnection fallback path

    [Fact]
    public async Task BeginTransactionAsync_NonDbConnection_UsesSynchronousFallback()
    {
        _mockConnection.State.Returns(ConnectionState.Open);
        var mockTransaction = Substitute.For<IDbTransaction>();
        _mockConnection.BeginTransaction().Returns(mockTransaction);

        var result = await _unitOfWork.BeginTransactionAsync();

        result.IsRight.ShouldBeTrue();
        _unitOfWork.HasActiveTransaction.ShouldBeTrue();
        _mockConnection.Received(1).BeginTransaction();
    }

    [Fact]
    public async Task BeginTransactionAsync_TransactionAlreadyActive_ReturnsError()
    {
        _mockConnection.State.Returns(ConnectionState.Open);
        var mockTransaction = Substitute.For<IDbTransaction>();
        _mockConnection.BeginTransaction().Returns(mockTransaction);

        await _unitOfWork.BeginTransactionAsync();

        var result = await _unitOfWork.BeginTransactionAsync();

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            var code = error.GetCode();
            code.IsSome.ShouldBeTrue();
            code.IfSome(c => c.ShouldBe(UnitOfWorkErrors.TransactionAlreadyActiveErrorCode));
        });
    }

    [Fact]
    public async Task BeginTransactionAsync_ConnectionThrows_ReturnsError()
    {
        _mockConnection.State.Returns(ConnectionState.Open);
        _mockConnection.BeginTransaction().Returns(x => throw new InvalidOperationException("Cannot start transaction"));

        var result = await _unitOfWork.BeginTransactionAsync();

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            var code = error.GetCode();
            code.IsSome.ShouldBeTrue();
            code.IfSome(c => c.ShouldBe(UnitOfWorkErrors.TransactionStartFailedErrorCode));
        });
    }

    [Fact]
    public async Task CommitAsync_NoActiveTransaction_ReturnsError()
    {
        var result = await _unitOfWork.CommitAsync();

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            var code = error.GetCode();
            code.IsSome.ShouldBeTrue();
            code.IfSome(c => c.ShouldBe(UnitOfWorkErrors.NoActiveTransactionErrorCode));
        });
    }

    [Fact]
    public async Task CommitAsync_NonDbTransaction_CommitsAndClearsTransaction()
    {
        _mockConnection.State.Returns(ConnectionState.Open);
        var mockTransaction = Substitute.For<IDbTransaction>();
        _mockConnection.BeginTransaction().Returns(mockTransaction);

        await _unitOfWork.BeginTransactionAsync();

        var result = await _unitOfWork.CommitAsync();

        result.IsRight.ShouldBeTrue();
        _unitOfWork.HasActiveTransaction.ShouldBeFalse();
        mockTransaction.Received(1).Commit();
    }

    [Fact]
    public async Task CommitAsync_CommitThrows_RollsBackAndReturnsError()
    {
        _mockConnection.State.Returns(ConnectionState.Open);
        var mockTransaction = Substitute.For<IDbTransaction>();
        _mockConnection.BeginTransaction().Returns(mockTransaction);
        mockTransaction.When(t => t.Commit()).Do(x => throw new InvalidOperationException("Commit failed"));

        await _unitOfWork.BeginTransactionAsync();

        var result = await _unitOfWork.CommitAsync();

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            var code = error.GetCode();
            code.IsSome.ShouldBeTrue();
            code.IfSome(c => c.ShouldBe(UnitOfWorkErrors.CommitFailedErrorCode));
        });
        mockTransaction.Received(1).Rollback();
    }

    [Fact]
    public async Task RollbackAsync_NoActiveTransaction_DoesNotThrow()
    {
        await _unitOfWork.RollbackAsync();
    }

    [Fact]
    public async Task RollbackAsync_NonDbTransaction_RollsBackAndClearsTransaction()
    {
        _mockConnection.State.Returns(ConnectionState.Open);
        var mockTransaction = Substitute.For<IDbTransaction>();
        _mockConnection.BeginTransaction().Returns(mockTransaction);

        await _unitOfWork.BeginTransactionAsync();

        await _unitOfWork.RollbackAsync();

        _unitOfWork.HasActiveTransaction.ShouldBeFalse();
        mockTransaction.Received(1).Rollback();
    }

    [Fact]
    public async Task DisposeAsync_WithActiveTransaction_RollsBack()
    {
        _mockConnection.State.Returns(ConnectionState.Open);
        var mockTransaction = Substitute.For<IDbTransaction>();
        _mockConnection.BeginTransaction().Returns(mockTransaction);

        await _unitOfWork.BeginTransactionAsync();

        await _unitOfWork.DisposeAsync();

        mockTransaction.Received(1).Rollback();
    }

    #endregion

    #region BeginTransactionAsync / CommitAsync / RollbackAsync - DbConnection async path

    [Fact]
    public async Task BeginTransactionAsync_DbConnection_UsesConnectionOpenAsyncAndBeginTransactionAsync()
    {
        var fakeTransaction = new FakeDbTransaction();
        using var fakeConnection = new FakeDbConnection { TransactionToReturn = fakeTransaction };
        var unitOfWork = new UnitOfWorkADO(fakeConnection, _mockServiceProvider);

        var result = await unitOfWork.BeginTransactionAsync();

        result.IsRight.ShouldBeTrue();
        fakeConnection.OpenAsyncCallCount.ShouldBe(1);
        fakeConnection.BeginTransactionAsyncCallCount.ShouldBe(1);

        await unitOfWork.DisposeAsync();
    }

    [Fact]
    public async Task CommitAsync_DbTransaction_CallsCommitAsync()
    {
        var fakeTransaction = new FakeDbTransaction();
        using var fakeConnection = new FakeDbConnection { TransactionToReturn = fakeTransaction };
        var unitOfWork = new UnitOfWorkADO(fakeConnection, _mockServiceProvider);
        await unitOfWork.BeginTransactionAsync();

        var result = await unitOfWork.CommitAsync();

        result.IsRight.ShouldBeTrue();
        fakeTransaction.CommitAsyncCallCount.ShouldBe(1);

        await unitOfWork.DisposeAsync();
    }

    [Fact]
    public async Task CommitAsync_DbTransactionCommitAsyncThrows_RollsBackViaRollbackAsync()
    {
        var fakeTransaction = new FakeDbTransaction { ThrowOnCommit = true };
        using var fakeConnection = new FakeDbConnection { TransactionToReturn = fakeTransaction };
        var unitOfWork = new UnitOfWorkADO(fakeConnection, _mockServiceProvider);
        await unitOfWork.BeginTransactionAsync();

        var result = await unitOfWork.CommitAsync();

        result.IsLeft.ShouldBeTrue();
        fakeTransaction.RollbackAsyncCallCount.ShouldBe(1);

        await unitOfWork.DisposeAsync();
    }

    [Fact]
    public async Task RollbackAsync_DbTransaction_CallsRollbackAsync()
    {
        var fakeTransaction = new FakeDbTransaction();
        using var fakeConnection = new FakeDbConnection { TransactionToReturn = fakeTransaction };
        var unitOfWork = new UnitOfWorkADO(fakeConnection, _mockServiceProvider);
        await unitOfWork.BeginTransactionAsync();

        await unitOfWork.RollbackAsync();

        fakeTransaction.RollbackAsyncCallCount.ShouldBe(1);

        await unitOfWork.DisposeAsync();
    }

    #endregion

    #region Helper Methods

    private static IEntityMapping<TestADOEntity, Guid> CreateTestMapping()
    {
        var builder = new EntityMappingBuilder<TestADOEntity, Guid>();
        builder.ToTable("TestEntities")
            .HasId(e => e.Id)
            .MapProperty(e => e.Name);
        return builder.Build().ShouldBeSuccess();
    }

    #endregion
}

#region Test Entity

/// <summary>
/// Test entity for ADO.NET UnitOfWork tests.
/// </summary>
public class TestADOEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

#endregion

#region Fake ADO.NET Types

/// <summary>
/// Minimal <see cref="DbConnection"/> test double that records async call counts.
/// </summary>
internal sealed class FakeDbConnection : DbConnection
{
    private ConnectionState _state = ConnectionState.Closed;

    public int OpenAsyncCallCount { get; private set; }
    public int BeginTransactionAsyncCallCount { get; private set; }
    public FakeDbTransaction? TransactionToReturn { get; set; }

    [System.Diagnostics.CodeAnalysis.AllowNull]
    public override string ConnectionString { get; set; } = string.Empty;
    public override string Database => "fake";
    public override string DataSource => "fake";
    public override string ServerVersion => "1.0";
    public override ConnectionState State => _state;

    public override void ChangeDatabase(string databaseName)
    {
    }

    public override void Close() => _state = ConnectionState.Closed;

    public override void Open() => _state = ConnectionState.Open;

    public override Task OpenAsync(CancellationToken cancellationToken)
    {
        OpenAsyncCallCount++;
        _state = ConnectionState.Open;
        return Task.CompletedTask;
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
        TransactionToReturn ?? throw new InvalidOperationException("No transaction configured");

    protected override ValueTask<DbTransaction> BeginDbTransactionAsync(
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken)
    {
        BeginTransactionAsyncCallCount++;
        return ValueTask.FromResult<DbTransaction>(
            TransactionToReturn ?? throw new InvalidOperationException("No transaction configured"));
    }

    protected override DbCommand CreateDbCommand() => throw new NotSupportedException();
}

/// <summary>
/// Minimal <see cref="DbTransaction"/> test double that records async call counts.
/// </summary>
internal sealed class FakeDbTransaction : DbTransaction
{
    public int CommitAsyncCallCount { get; private set; }
    public int RollbackAsyncCallCount { get; private set; }
    public bool ThrowOnCommit { get; set; }

    protected override DbConnection? DbConnection => null;
    public override IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;

    public override void Commit()
    {
    }

    public override void Rollback()
    {
    }

    public override Task CommitAsync(CancellationToken cancellationToken = default)
    {
        CommitAsyncCallCount++;
        if (ThrowOnCommit)
        {
            throw new InvalidOperationException("Commit failed");
        }

        return Task.CompletedTask;
    }

    public override Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        RollbackAsyncCallCount++;
        return Task.CompletedTask;
    }
}

#endregion

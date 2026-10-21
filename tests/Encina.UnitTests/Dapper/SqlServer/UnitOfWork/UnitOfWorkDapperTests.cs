using Encina;
using Encina.Dapper.SqlServer.Repository;
using Encina.Dapper.SqlServer.UnitOfWork;
using Encina.DomainModeling;
using Encina.Messaging.Transactions;
using Encina.Testing.Shouldly;
using Encina.UnitTests.Messaging.Transactions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.Dapper.SqlServer.UnitOfWork;

/// <summary>
/// Unit tests for <see cref="UnitOfWorkDapper"/> on top of a real <see cref="DbTransactionAccessor"/>: the unit of
/// work takes a lease on the scope's shared transaction, joins one another participant owns, and commits only
/// when it owns it.
/// </summary>
[Trait("Category", "Unit")]
public sealed class UnitOfWorkDapperTests : IAsyncLifetime
{
    private readonly RecordingTransactionalConnection _connection = new();
    private readonly IServiceProvider _mockServiceProvider = Substitute.For<IServiceProvider>();
    private readonly DbTransactionAccessor _accessor;
    private readonly UnitOfWorkDapper _unitOfWork;

    public UnitOfWorkDapperTests()
    {
        _accessor = new DbTransactionAccessor(_connection, NullLogger<DbTransactionAccessor>.Instance);
        _unitOfWork = new UnitOfWorkDapper(_accessor, _mockServiceProvider);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await _unitOfWork.DisposeAsync();
        await _accessor.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private static string CodeOf<T>(LanguageExt.Either<EncinaError, T> result) =>
        ((EncinaError)result).GetCode().IfNone(string.Empty);

    #region Constructor Tests

    [Fact]
    public void Constructor_NullTransactionAccessor_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new UnitOfWorkDapper(null!, _mockServiceProvider));
    }

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new UnitOfWorkDapper(_accessor, null!));
    }

    #endregion

    #region HasActiveTransaction Tests

    [Fact]
    public void HasActiveTransaction_NoTransaction_ReturnsFalse()
    {
        _unitOfWork.HasActiveTransaction.ShouldBeFalse();
    }

    [Fact]
    public async Task HasActiveTransaction_AfterSuccessfulBeginTransaction_ReturnsTrue()
    {
        var result = await _unitOfWork.BeginTransactionAsync();

        result.IsRight.ShouldBeTrue();
        _unitOfWork.HasActiveTransaction.ShouldBeTrue();
    }

    [Fact]
    public async Task HasActiveTransaction_ReflectsOnlyThisUnitOfWork_NotATransactionAnotherParticipantOwns()
    {
        await using var owner = (await _accessor.BeginOrJoinAsync(null)).IfLeft(static _ => throw new InvalidOperationException());

        _unitOfWork.HasActiveTransaction.ShouldBeFalse();
        _unitOfWork.CurrentTransaction.ShouldBeSameAs(_connection.Transaction);
    }

    #endregion

    #region Repository Tests

    [Fact]
    public void Repository_WithoutMapping_ThrowsInvalidOperationException()
    {
        _mockServiceProvider.GetService(typeof(IEntityMapping<TestDapperEntity, Guid>)).Returns((object?)null);

        Should.Throw<InvalidOperationException>(() => _unitOfWork.Repository<TestDapperEntity, Guid>());
    }

    [Fact]
    public void Repository_WithMapping_ReturnsRepositoryInstance()
    {
        _mockServiceProvider.GetService(typeof(IEntityMapping<TestDapperEntity, Guid>)).Returns(CreateTestMapping());

        var repository = _unitOfWork.Repository<TestDapperEntity, Guid>();

        repository.ShouldNotBeNull();
        repository.ShouldBeAssignableTo<IFunctionalRepository<TestDapperEntity, Guid>>();
    }

    [Fact]
    public void Repository_SameEntityType_ReturnsSameInstance()
    {
        _mockServiceProvider.GetService(typeof(IEntityMapping<TestDapperEntity, Guid>)).Returns(CreateTestMapping());

        var repository1 = _unitOfWork.Repository<TestDapperEntity, Guid>();
        var repository2 = _unitOfWork.Repository<TestDapperEntity, Guid>();

        repository1.ShouldBeSameAs(repository2);
    }

    [Fact]
    public async Task Repository_AfterDispose_ThrowsObjectDisposedException()
    {
        await _unitOfWork.DisposeAsync();

        Should.Throw<ObjectDisposedException>(() => _unitOfWork.Repository<TestDapperEntity, Guid>());
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

    [Fact]
    public async Task SaveChangesAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        await _unitOfWork.DisposeAsync();

        await Should.ThrowAsync<ObjectDisposedException>(async () => await _unitOfWork.SaveChangesAsync());
    }

    #endregion

    #region BeginTransactionAsync Tests

    [Fact]
    public async Task BeginTransactionAsync_ClosedConnection_OpensAndBeginsAsynchronouslyWithTheToken()
    {
        using var cts = new CancellationTokenSource();

        var result = await _unitOfWork.BeginTransactionAsync(cts.Token);

        result.IsRight.ShouldBeTrue();
        _unitOfWork.CurrentTransaction.ShouldBeSameAs(_connection.Transaction);
        _connection.OpenAsyncToken.ShouldBe(cts.Token);
        _connection.BeginAsyncToken.ShouldBe(cts.Token);
    }

    [Fact]
    public async Task BeginTransactionAsync_TransactionAlreadyActive_ReturnsError()
    {
        await _unitOfWork.BeginTransactionAsync();

        var result = await _unitOfWork.BeginTransactionAsync();

        CodeOf(result).ShouldBe(UnitOfWorkErrors.TransactionAlreadyActiveErrorCode);
    }

    [Fact]
    public async Task BeginTransactionAsync_AnotherParticipantOwnsTheTransaction_JoinsIt()
    {
        await using var owner = (await _accessor.BeginOrJoinAsync(null)).IfLeft(static _ => throw new InvalidOperationException());

        var result = await _unitOfWork.BeginTransactionAsync();

        result.IsRight.ShouldBeTrue();
        _unitOfWork.HasActiveTransaction.ShouldBeTrue();
        _connection.BeginAsyncCalls.ShouldBe(1);
    }

    [Fact]
    public async Task BeginTransactionAsync_BeginThrows_ReturnsTransactionStartFailed()
    {
        _connection.ThrowOnBegin = true;

        var result = await _unitOfWork.BeginTransactionAsync();

        CodeOf(result).ShouldBe(UnitOfWorkErrors.TransactionStartFailedErrorCode);
        _unitOfWork.HasActiveTransaction.ShouldBeFalse();
    }

    [Fact]
    public async Task BeginTransactionAsync_Cancelled_ReturnsTransactionStartFailed()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await _unitOfWork.BeginTransactionAsync(cts.Token);

        CodeOf(result).ShouldBe(UnitOfWorkErrors.TransactionStartFailedErrorCode);
    }

    [Fact]
    public async Task BeginTransactionAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        await _unitOfWork.DisposeAsync();

        await Should.ThrowAsync<ObjectDisposedException>(async () => await _unitOfWork.BeginTransactionAsync());
    }

    #endregion

    #region CommitAsync Tests

    [Fact]
    public async Task CommitAsync_NoActiveTransaction_ReturnsError()
    {
        var result = await _unitOfWork.CommitAsync();

        CodeOf(result).ShouldBe(UnitOfWorkErrors.NoActiveTransactionErrorCode);
    }

    [Fact]
    public async Task CommitAsync_OwnedTransaction_CommitsAndClearsIt()
    {
        await _unitOfWork.BeginTransactionAsync();

        var result = await _unitOfWork.CommitAsync();

        result.IsRight.ShouldBeTrue();
        _unitOfWork.HasActiveTransaction.ShouldBeFalse();
        _connection.Transaction.CommitAsyncCalls.ShouldBe(1);
        _accessor.HasActiveTransaction.ShouldBeFalse();
    }

    [Fact]
    public async Task CommitAsync_JoinedTransaction_DoesNotCommitAndLeavesItToTheOwner()
    {
        await using var owner = (await _accessor.BeginOrJoinAsync(null)).IfLeft(static _ => throw new InvalidOperationException());
        await _unitOfWork.BeginTransactionAsync();

        var result = await _unitOfWork.CommitAsync();

        result.IsRight.ShouldBeTrue();
        _unitOfWork.HasActiveTransaction.ShouldBeFalse();
        _connection.Transaction.CommitAsyncCalls.ShouldBe(0);
        _accessor.HasActiveTransaction.ShouldBeTrue();
    }

    [Fact]
    public async Task CommitAsync_CommitThrows_RollsBackAndReturnsCommitFailed()
    {
        await _unitOfWork.BeginTransactionAsync();
        _connection.Transaction.ThrowOnCommit = true;

        var result = await _unitOfWork.CommitAsync();

        CodeOf(result).ShouldBe(UnitOfWorkErrors.CommitFailedErrorCode);
        _connection.Transaction.RollbackAsyncCalls.ShouldBe(1);
        _unitOfWork.HasActiveTransaction.ShouldBeFalse();
    }

    [Fact]
    public async Task CommitAsync_AfterAParticipantRolledBack_OwnerRefusesAndReturnsCommitFailed()
    {
        await _unitOfWork.BeginTransactionAsync();
        var joiner = (await _accessor.BeginOrJoinAsync(null)).IfLeft(static _ => throw new InvalidOperationException());
        await joiner.RollbackAsync();

        var result = await _unitOfWork.CommitAsync();

        CodeOf(result).ShouldBe(UnitOfWorkErrors.CommitFailedErrorCode);
        _connection.Transaction.CommitAsyncCalls.ShouldBe(0);
        _connection.Transaction.RollbackAsyncCalls.ShouldBe(1);
        await joiner.DisposeAsync();
    }

    [Fact]
    public async Task CommitAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        await _unitOfWork.DisposeAsync();

        await Should.ThrowAsync<ObjectDisposedException>(async () => await _unitOfWork.CommitAsync());
    }

    #endregion

    #region RollbackAsync Tests

    [Fact]
    public async Task RollbackAsync_NoActiveTransaction_DoesNotThrow()
    {
        await _unitOfWork.RollbackAsync();

        _connection.Transaction.RollbackAsyncCalls.ShouldBe(0);
    }

    [Fact]
    public async Task RollbackAsync_OwnedTransaction_RollsBackAndClearsIt()
    {
        await _unitOfWork.BeginTransactionAsync();

        await _unitOfWork.RollbackAsync();

        _unitOfWork.HasActiveTransaction.ShouldBeFalse();
        _connection.Transaction.RollbackAsyncCalls.ShouldBe(1);
    }

    [Fact]
    public async Task RollbackAsync_JoinedTransaction_MarksTheOwnersTransactionRollbackOnly()
    {
        await using var owner = (await _accessor.BeginOrJoinAsync(null)).IfLeft(static _ => throw new InvalidOperationException());
        await _unitOfWork.BeginTransactionAsync();

        await _unitOfWork.RollbackAsync();
        var committed = await owner.CommitAsync();

        ((EncinaError)committed).GetCode().IfNone(string.Empty).ShouldBe(TransactionErrors.RollbackOnlyCode);
        _connection.Transaction.RollbackAsyncCalls.ShouldBe(1);
    }

    [Fact]
    public async Task RollbackAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        await _unitOfWork.DisposeAsync();

        await Should.ThrowAsync<ObjectDisposedException>(async () => await _unitOfWork.RollbackAsync());
    }

    #endregion

    #region DisposeAsync Tests

    [Fact]
    public async Task DisposeAsync_CalledTwice_DoesNotThrow()
    {
        await _unitOfWork.DisposeAsync();
        await _unitOfWork.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_WithActiveTransaction_RollsBack()
    {
        await _unitOfWork.BeginTransactionAsync();

        await _unitOfWork.DisposeAsync();

        _connection.Transaction.RollbackAsyncCalls.ShouldBe(1);
        _connection.Transaction.DisposeAsyncCalls.ShouldBe(1);
    }

    #endregion

    #region UpdateImmutable Tests

    [Fact]
    public async Task UpdateImmutable_IsNotSupported()
    {
        var entity = new TestDapperEntity();

        _unitOfWork.UpdateImmutable(entity).IsLeft.ShouldBeTrue();
        (await _unitOfWork.UpdateImmutableAsync(entity)).IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Helper Methods

    private static IEntityMapping<TestDapperEntity, Guid> CreateTestMapping()
    {
        var builder = new EntityMappingBuilder<TestDapperEntity, Guid>();
        builder.ToTable("TestEntities")
            .HasId(e => e.Id)
            .MapProperty(e => e.Name);
        return builder.Build().ShouldBeSuccess();
    }

    #endregion
}

#region Test Entity

/// <summary>
/// Test entity for Dapper UnitOfWork tests.
/// </summary>
public class TestDapperEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

#endregion

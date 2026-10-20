using System.Data;
using Encina.Messaging;
using Encina.Messaging.Transactions;
using Encina.UnitTests.Messaging.Transactions;

namespace Encina.UnitTests.Messaging;

/// <summary>
/// Unit tests for <see cref="DbLease"/> (ADR-048): enlisted writes join the active business transaction,
/// independent writes leave it by using a clone of the connection, and connections open asynchronously only.
/// </summary>
public sealed class DbLeaseTests
{
    private static (IDbConnection Connection, IDbTransaction Transaction, IDbTransactionAccessor Accessor) ActiveTransaction()
    {
        var connection = Substitute.For<IDbConnection, ICloneable>();
        var transaction = Substitute.For<IDbTransaction>();
        transaction.Connection.Returns(connection);
        return (connection, transaction, AccessorWith(transaction));
    }

    private static IDbTransactionAccessor AccessorWith(IDbTransaction? transaction)
    {
        var accessor = Substitute.For<IDbTransactionAccessor>();
        accessor.Current.Returns(transaction);
        return accessor;
    }

    [Fact]
    public void Enlisted_TransactionOnSameConnection_CommandsJoinIt()
    {
        var (connection, transaction, accessor) = ActiveTransaction();
        var command = Substitute.For<IDbCommand>();
        connection.CreateCommand().Returns(command);

        using var lease = DbLease.Enlisted(connection, accessor);
        var created = lease.CreateCommand();

        lease.Transaction.ShouldBeSameAs(transaction);
        lease.Connection.ShouldBeSameAs(connection);
        created.ShouldBeSameAs(command);
        command.Transaction.ShouldBeSameAs(transaction);
    }

    [Fact]
    public void Enlisted_NoAccessor_HasNoTransaction()
    {
        var connection = Substitute.For<IDbConnection>();

        using var lease = DbLease.Enlisted(connection, accessor: null);

        lease.Transaction.ShouldBeNull();
        lease.Connection.ShouldBeSameAs(connection);
    }

    [Fact]
    public void Enlisted_TransactionOnAnotherConnection_IsIgnored()
    {
        var (_, _, accessor) = ActiveTransaction();
        var other = Substitute.For<IDbConnection>();

        using var lease = DbLease.Enlisted(other, accessor);

        lease.Transaction.ShouldBeNull();
    }

    [Fact]
    public async Task Independent_NoActiveTransaction_UsesSharedConnectionWithoutCloning()
    {
        var connection = Substitute.For<IDbConnection, ICloneable>();

        using var lease = await DbLease.IndependentAsync(connection, AccessorWith(null), CancellationToken.None);

        lease.Connection.ShouldBeSameAs(connection);
        lease.Transaction.ShouldBeNull();
        ((ICloneable)connection).DidNotReceive().Clone();
    }

    [Fact]
    public async Task Independent_ActiveTransaction_UsesAsynchronouslyOpenedCloneAndDisposesIt()
    {
        var (connection, _, accessor) = ActiveTransaction();
        var clone = new RecordingTransactionalConnection();
        ((ICloneable)connection).Clone().Returns(clone);
        using var cts = new CancellationTokenSource();

        var lease = await DbLease.IndependentAsync(connection, accessor, cts.Token);

        lease.Connection.ShouldBeSameAs(clone);
        lease.Transaction.ShouldBeNull();
        clone.OpenAsyncCalls.ShouldBe(1);
        clone.OpenAsyncToken.ShouldBe(cts.Token);

        lease.Dispose();

        clone.WasDisposed.ShouldBeTrue();
        connection.DidNotReceive().Dispose();
    }

    [Fact]
    public async Task Independent_ActiveTransactionOnNonCloneableConnection_Throws()
    {
        var connection = Substitute.For<IDbConnection>();
        var transaction = Substitute.For<IDbTransaction>();
        transaction.Connection.Returns(connection);

        var act = async () => await DbLease.IndependentAsync(connection, AccessorWith(transaction), CancellationToken.None);

        await act.ShouldThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Independent_CloneFailsToOpen_DisposesCloneAndRethrows()
    {
        var (connection, _, accessor) = ActiveTransaction();
        var clone = new RecordingTransactionalConnection { ThrowOnOpen = true };
        ((ICloneable)connection).Clone().Returns(clone);

        var act = async () => await DbLease.IndependentAsync(connection, accessor, CancellationToken.None);

        await act.ShouldThrowAsync<InvalidOperationException>();
        clone.WasDisposed.ShouldBeTrue();
    }

    [Fact]
    public async Task Independent_CloneIsNotADbConnection_FailsClosedWithoutSynchronousOpen()
    {
        var (connection, _, accessor) = ActiveTransaction();
        var clone = Substitute.For<IDbConnection>();
        ((ICloneable)connection).Clone().Returns(clone);

        var act = async () => await DbLease.IndependentAsync(connection, accessor, CancellationToken.None);

        await act.ShouldThrowAsync<InvalidOperationException>();
        clone.DidNotReceive().Open();
        clone.Received(1).Dispose();
    }

    // Public so the mock proxy generator can implement it.
    public interface IWrapper : IDbConnection, IWrappedDbConnection;

    [Fact]
    public void Enlisted_DecoratedConnection_FindsTheTransactionOfTheInnerConnection()
    {
        var (inner, transaction, accessor) = ActiveTransaction();
        var wrapper = Substitute.For<IWrapper>();
        wrapper.InnerConnection.Returns(inner);
        var command = Substitute.For<IDbCommand>();
        wrapper.CreateCommand().Returns(command);

        using var lease = DbLease.Enlisted(wrapper, accessor);
        lease.CreateCommand();

        lease.Transaction.ShouldBeSameAs(transaction);
        command.Transaction.ShouldBeSameAs(transaction);
    }

    [Fact]
    public async Task Independent_DecoratedConnection_ClonesTheInnerConnection()
    {
        var (inner, _, accessor) = ActiveTransaction();
        var wrapper = Substitute.For<IWrapper>();
        wrapper.InnerConnection.Returns(inner);
        var clone = new RecordingTransactionalConnection();
        ((ICloneable)inner).Clone().Returns(clone);

        using var lease = await DbLease.IndependentAsync(wrapper, accessor, CancellationToken.None);

        lease.Connection.ShouldBeSameAs(clone);
        lease.Transaction.ShouldBeNull();
        clone.OpenAsyncCalls.ShouldBe(1);
    }

    [Fact]
    public async Task Independent_DecoratedNonCloneableInnerConnection_Throws()
    {
        var inner = Substitute.For<IDbConnection>();
        var transaction = Substitute.For<IDbTransaction>();
        transaction.Connection.Returns(inner);
        var wrapper = Substitute.For<IWrapper>();
        wrapper.InnerConnection.Returns(inner);

        var act = async () => await DbLease.IndependentAsync(wrapper, AccessorWith(transaction), CancellationToken.None);

        await act.ShouldThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task EnsureOpenAsync_ClosedDbConnection_OpensItAsynchronouslyWithTheToken()
    {
        var connection = new RecordingTransactionalConnection();
        using var cts = new CancellationTokenSource();
        using var lease = DbLease.Enlisted(connection, accessor: null);

        await lease.EnsureOpenAsync(cts.Token);

        connection.OpenAsyncCalls.ShouldBe(1);
        connection.OpenAsyncToken.ShouldBe(cts.Token);
    }

    [Fact]
    public async Task EnsureOpenAsync_AlreadyOpen_DoesNothing()
    {
        var connection = new RecordingTransactionalConnection();
        connection.SetState(ConnectionState.Open);
        using var lease = DbLease.Enlisted(connection, accessor: null);

        await lease.EnsureOpenAsync(CancellationToken.None);

        connection.OpenAsyncCalls.ShouldBe(0);
    }

    [Fact]
    public async Task EnsureOpenAsync_ClosedConnectionThatIsNotADbConnection_FailsClosed()
    {
        var connection = Substitute.For<IDbConnection>();
        connection.State.Returns(ConnectionState.Closed);
        using var lease = DbLease.Enlisted(connection, accessor: null);

        var act = async () => await lease.EnsureOpenAsync(CancellationToken.None);

        await act.ShouldThrowAsync<InvalidOperationException>();
        connection.DidNotReceive().Open();
    }

    [Fact]
    public void Guards_NullConnection_Throw()
    {
        Should.Throw<ArgumentNullException>(() => DbLease.Enlisted(null!, null));
        Should.ThrowAsync<ArgumentNullException>(() => DbLease.IndependentAsync(null!, null, CancellationToken.None));
    }
}

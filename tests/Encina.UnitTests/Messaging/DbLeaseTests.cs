using System.Data;
using Encina.Messaging;

namespace Encina.UnitTests.Messaging;

/// <summary>
/// Unit tests for <see cref="DbLease"/> and <see cref="DbTransactionAccessor"/> (ADR-048): enlisted writes join the
/// active business transaction, independent writes leave it by using a clone of the connection.
/// </summary>
public sealed class DbLeaseTests
{
    private static (IDbConnection Connection, IDbTransaction Transaction, DbTransactionAccessor Accessor) ActiveTransaction()
    {
        var connection = Substitute.For<IDbConnection, ICloneable>();
        var transaction = Substitute.For<IDbTransaction>();
        transaction.Connection.Returns(connection);
        return (connection, transaction, new DbTransactionAccessor { Current = transaction });
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

        using var lease = await DbLease.IndependentAsync(connection, new DbTransactionAccessor(), CancellationToken.None);

        lease.Connection.ShouldBeSameAs(connection);
        lease.Transaction.ShouldBeNull();
        ((ICloneable)connection).DidNotReceive().Clone();
    }

    [Fact]
    public async Task Independent_ActiveTransaction_UsesOpenedCloneAndDisposesIt()
    {
        var (connection, _, accessor) = ActiveTransaction();
        var clone = Substitute.For<IDbConnection>();
        ((ICloneable)connection).Clone().Returns(clone);

        var lease = await DbLease.IndependentAsync(connection, accessor, CancellationToken.None);

        lease.Connection.ShouldBeSameAs(clone);
        lease.Transaction.ShouldBeNull();
        clone.Received(1).Open();

        lease.Dispose();

        clone.Received(1).Dispose();
        connection.DidNotReceive().Dispose();
    }

    [Fact]
    public async Task Independent_ActiveTransactionOnNonCloneableConnection_Throws()
    {
        var connection = Substitute.For<IDbConnection>();
        var transaction = Substitute.For<IDbTransaction>();
        transaction.Connection.Returns(connection);
        var accessor = new DbTransactionAccessor { Current = transaction };

        var act = async () => await DbLease.IndependentAsync(connection, accessor, CancellationToken.None);

        await act.ShouldThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Independent_CloneFailsToOpen_DisposesCloneAndRethrows()
    {
        var (connection, _, accessor) = ActiveTransaction();
        var clone = Substitute.For<IDbConnection>();
        clone.When(c => c.Open()).Do(_ => throw new InvalidOperationException("cannot open"));
        ((ICloneable)connection).Clone().Returns(clone);

        var act = async () => await DbLease.IndependentAsync(connection, accessor, CancellationToken.None);

        await act.ShouldThrowAsync<InvalidOperationException>();
        clone.Received(1).Dispose();
    }

    [Fact]
    public void Guards_NullConnection_Throw()
    {
        Should.Throw<ArgumentNullException>(() => DbLease.Enlisted(null!, null));
        Should.ThrowAsync<ArgumentNullException>(() => DbLease.IndependentAsync(null!, null, CancellationToken.None));
    }
}

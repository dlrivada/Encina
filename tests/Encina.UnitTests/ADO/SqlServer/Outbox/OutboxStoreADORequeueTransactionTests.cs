using System.Data;
using Encina.Messaging.Outbox;

namespace Encina.UnitTests.ADO.SqlServer.Outbox;

/// <summary>
/// Verifies that requeueing exhausted outbox messages by identifier is all-or-nothing on the ADO.NET
/// stores that split the identifiers into several statements (SQL Server and MySQL): every chunk runs
/// in one transaction, which is committed only when every chunk succeeded (#1167 review).
/// </summary>
/// <remarks>
/// The Dapper stores use the same transaction, but Dapper's asynchronous API requires a real
/// <see cref="System.Data.Common.DbConnection"/>, so their behaviour is covered by the integration tests.
/// </remarks>
public sealed class OutboxStoreADORequeueTransactionTests
{
    // One more identifier than a chunk holds, so the requeue needs two statements.
    private const int IdCount = 1001;

    public static TheoryData<string> Providers => ["ADO.SqlServer", "ADO.MySQL"];

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task RequeueExhaustedAsync_SecondChunkFails_RollsBackWithoutCommitting(string provider)
    {
        var harness = new Harness(failOnCommand: 2);
        var store = CreateStore(provider, harness.Connection);

        var result = await store.RequeueExhaustedAsync(3, Ids(), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        harness.Transaction.DidNotReceive().Commit();
        harness.Transaction.Received(1).Dispose();
        harness.Commands.Count.ShouldBe(2);
        harness.Commands.ShouldAllBe(c => c.Transaction == harness.Transaction);
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task RequeueExhaustedAsync_EveryChunkSucceeds_CommitsOnce(string provider)
    {
        var harness = new Harness(failOnCommand: null);
        var store = CreateStore(provider, harness.Connection);

        var result = await store.RequeueExhaustedAsync(3, Ids(), CancellationToken.None);

        result.Match(Right: count => count, Left: error => throw new InvalidOperationException(error.Message))
            .ShouldBe(2);
        harness.Transaction.Received(1).Commit();
        harness.Commands.Count.ShouldBe(2);
        harness.Commands.ShouldAllBe(c => c.Transaction == harness.Transaction);
    }

    private static IOutboxStore CreateStore(string provider, IDbConnection connection) => provider switch
    {
        "ADO.SqlServer" => new global::Encina.ADO.SqlServer.Outbox.OutboxStoreADO(connection),
        "ADO.MySQL" => new global::Encina.ADO.MySQL.Outbox.OutboxStoreADO(connection),
        _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
    };

    private static Guid[] Ids() => Enumerable.Range(0, IdCount).Select(_ => Guid.NewGuid()).ToArray();

    private sealed class Harness
    {
        public Harness(int? failOnCommand)
        {
            Connection.State.Returns(ConnectionState.Open);
            Connection.BeginTransaction().Returns(Transaction);
            Connection.CreateCommand().Returns(_ =>
            {
                var command = Substitute.For<IDbCommand>();
                command.Parameters.Returns(Substitute.For<IDataParameterCollection>());
                command.CreateParameter().Returns(_ => Substitute.For<IDbDataParameter>());
                Commands.Add(command);
                var ordinal = Commands.Count;
                command.ExecuteNonQuery().Returns(_ => ordinal == failOnCommand
                    ? throw new InvalidOperationException("Lock wait timeout exceeded")
                    : 1);
                return command;
            });
        }

        public IDbConnection Connection { get; } = Substitute.For<IDbConnection>();

        public IDbTransaction Transaction { get; } = Substitute.For<IDbTransaction>();

        public List<IDbCommand> Commands { get; } = [];
    }
}

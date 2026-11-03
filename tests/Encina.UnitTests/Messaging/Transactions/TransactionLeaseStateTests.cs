using System.Diagnostics;
using System.Diagnostics.Metrics;
using Encina.Messaging.Diagnostics;
using Encina.Messaging.Transactions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.UnitTests.Messaging.Transactions;

/// <summary>
/// Unit tests for <see cref="TransactionLeaseState"/>: join depth, rollback-only, closing, and the activity,
/// counter and log signals of a transaction. Signals carry codes, counts and <c>db.system</c> only.
/// </summary>
[Trait("Category", "Unit")]
public sealed class TransactionLeaseStateTests
{
    private static TransactionLeaseState NewState() => new(DbSystemName.SqlServer, NullLogger.Instance);

    [Fact]
    public void New_IsOpenWithDepthOneAndNotRollbackOnly()
    {
        var state = NewState();

        state.IsClosed.ShouldBeFalse();
        state.RollbackOnly.ShouldBeFalse();
        state.Depth.ShouldBe(1);
    }

    [Fact]
    public void Join_And_Leave_TrackDepth_AndLeaveNeverGoesBelowTheOwner()
    {
        var state = NewState();

        state.Join();
        state.Join();
        state.Depth.ShouldBe(3);
        state.Leave();
        state.Leave();
        state.Leave();

        state.Depth.ShouldBe(1);
    }

    [Fact]
    public void MarkRollbackOnly_SetsTheFlagOnce()
    {
        var state = NewState();

        state.MarkRollbackOnly();
        state.MarkRollbackOnly();

        state.RollbackOnly.ShouldBeTrue();
    }

    [Fact]
    public void Close_EndsTheTransaction_AndLaterMarksAreIgnored()
    {
        var state = NewState();

        state.Close(TransactionActivitySource.OutcomeCommitted);
        state.Close(TransactionActivitySource.OutcomeRolledBack);
        state.MarkRollbackOnly();

        state.IsClosed.ShouldBeTrue();
        state.Depth.ShouldBe(0);
        state.RollbackOnly.ShouldBeFalse();
    }

    [Fact]
    public void RecordCommitRefused_And_RecordCommitFailed_DoNotThrow()
    {
        var state = NewState();

        Should.NotThrow(() => state.RecordCommitRefused());
        Should.NotThrow(() => state.RecordCommitFailed(new InvalidOperationException("secret")));
    }

    [Theory]
    [InlineData(TransactionActivitySource.OutcomeCommitted, ActivityStatusCode.Ok)]
    [InlineData(TransactionActivitySource.OutcomeRolledBack, ActivityStatusCode.Ok)]
    [InlineData(TransactionActivitySource.OutcomeRollbackOnly, ActivityStatusCode.Error)]
    [InlineData(TransactionActivitySource.OutcomeCommitFailed, ActivityStatusCode.Error)]
    public void Activity_CarriesOwnerOutcomeAndDbSystem_AndNothingElse(string outcome, ActivityStatusCode status)
    {
        var stopped = new System.Collections.Concurrent.ConcurrentQueue<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == TransactionActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = stopped.Enqueue
        };
        ActivitySource.AddActivityListener(listener);

        // A unique db.system isolates this test from transactions that other tests run in parallel.
        var dbSystem = $"test-{Guid.NewGuid():N}";
        var state = new TransactionLeaseState(dbSystem, NullLogger.Instance);
        state.Close(outcome);

        var activity = stopped.ToArray().Single(a => (string?)a.GetTagItem("db.system") == dbSystem);
        activity.OperationName.ShouldBe("encina.transaction");
        activity.GetTagItem(TransactionActivitySource.OwnerTag).ShouldBe(true);
        activity.GetTagItem(TransactionActivitySource.OutcomeTag).ShouldBe(outcome);
        activity.Status.ShouldBe(status);
        activity.TagObjects.Count().ShouldBe(3);
    }

    [Fact]
    public void MarkRollbackOnly_IncrementsTheRollbackOnlyCounter()
    {
        var measurements = new System.Collections.Concurrent.ConcurrentQueue<(long Value, string? DbSystem)>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Name == "encina.transaction.rollback_only")
            {
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
        {
            string? dbSystem = null;
            foreach (var tag in tags)
            {
                if (tag.Key == "db.system")
                {
                    dbSystem = tag.Value as string;
                }
            }

            measurements.Enqueue((value, dbSystem));
        });
        listener.Start();

        var dbSystem = $"test-{Guid.NewGuid():N}";
        var state = new TransactionLeaseState(dbSystem, NullLogger.Instance);
        state.MarkRollbackOnly();
        state.MarkRollbackOnly();

        measurements.ToArray().Where(m => m.DbSystem == dbSystem).ShouldBe([(1L, (string?)dbSystem)]);
    }

    [Theory]
    [InlineData(typeof(Microsoft.Data.SqlClient.SqlConnection), DbSystemName.SqlServer)]
    [InlineData(typeof(Npgsql.NpgsqlConnection), DbSystemName.PostgreSql)]
    [InlineData(typeof(MySqlConnector.MySqlConnection), DbSystemName.MySql)]
    [InlineData(typeof(RecordingTransactionalConnection), DbSystemName.Other)]
    public void DbSystemName_IsDerivedFromTheConnectionType(Type connectionType, string expected)
    {
        var connection = (System.Data.IDbConnection)Activator.CreateInstance(connectionType, nonPublic: true)!;

        DbSystemName.From(connection).ShouldBe(expected);
    }

    public interface IWrapper : System.Data.IDbConnection, global::Encina.Messaging.IWrappedDbConnection;

    [Fact]
    public void DbSystemName_DecoratedConnection_LooksThroughTheDecorator()
    {
        var wrapper = Substitute.For<IWrapper>();
        wrapper.InnerConnection.Returns(new Npgsql.NpgsqlConnection());

        DbSystemName.From(wrapper).ShouldBe(DbSystemName.PostgreSql);
    }

    [Fact]
    public void DbSystemName_NullConnection_IsOther()
    {
        DbSystemName.From((System.Data.IDbConnection?)null).ShouldBe(DbSystemName.Other);
    }
}

using System.Data;
using System.Data.Common;
using Encina.Database;
using Encina.Messaging.Health;

namespace Encina.UnitTests.Messaging.Health;

/// <summary>
/// Tests of <see cref="DatabaseHealthMonitorBase"/> against real <see cref="DbConnection"/> and
/// <see cref="DbCommand"/> subclasses, which take the asynchronous branches (<c>OpenAsync</c>,
/// <c>ExecuteScalarAsync</c>) that <c>IDbConnection</c> mocks cannot reach.
/// </summary>
public sealed class DatabaseHealthMonitorBaseDbConnectionTests
{
    [Fact]
    public async Task CheckHealthAsync_WithDbConnection_OpensAndQueriesAsynchronously()
    {
        var connection = new FakeDbConnection(openForever: false, failOnOpen: false);
        var monitor = new Monitor(() => connection);

        var result = await monitor.CheckHealthAsync();

        result.Status.ShouldBe(DatabaseHealthStatus.Healthy);
        connection.OpenAsyncCalls.ShouldBe(1);
        connection.Command.ScalarCalls.ShouldBe(1);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenOpenAsyncFails_ReportsOnlyTheExceptionType()
    {
        var connection = new FakeDbConnection(openForever: false, failOnOpen: true);
        var monitor = new Monitor(() => connection);

        var result = await monitor.CheckHealthAsync();

        result.Status.ShouldBe(DatabaseHealthStatus.Unhealthy);
        result.Description!.ShouldContain(nameof(InvalidOperationException));
        result.Description!.ShouldNotContain("db-secret-host");
        monitor.IsCircuitOpen.ShouldBeTrue();
    }

    [Fact]
    public async Task CheckHealthAsync_WhenTheProbeTimesOut_ReturnsUnhealthyWithoutOpeningTheCircuit()
    {
        var connection = new FakeDbConnection(openForever: true, failOnOpen: false);
        var monitor = new Monitor(
            () => connection,
            new DatabaseResilienceOptions { HealthCheckInterval = TimeSpan.FromMilliseconds(50) });

        var result = await monitor.CheckHealthAsync();

        result.Status.ShouldBe(DatabaseHealthStatus.Unhealthy);
        result.Description!.ShouldContain("timed out");
        monitor.IsCircuitOpen.ShouldBeFalse();
    }

    private sealed class Monitor(Func<IDbConnection> factory, DatabaseResilienceOptions? options = null)
        : DatabaseHealthMonitorBase("test", factory, options)
    {
        protected override ConnectionPoolStats GetPoolStatisticsCore() => ConnectionPoolStats.CreateEmpty();

        protected override Task ClearPoolCoreAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeDbConnection(bool openForever, bool failOnOpen) : DbConnection
    {
        private ConnectionState _state = ConnectionState.Closed;

        public int OpenAsyncCalls { get; private set; }

        public FakeDbCommand Command { get; } = new();

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

        public override async Task OpenAsync(CancellationToken cancellationToken)
        {
            OpenAsyncCalls++;

            if (failOnOpen)
            {
                throw new InvalidOperationException("cannot reach db-secret-host");
            }

            if (openForever)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }

            _state = ConnectionState.Open;
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
            => throw new NotSupportedException();

        protected override DbCommand CreateDbCommand() => Command;
    }

    private sealed class FakeDbCommand : DbCommand
    {
        public int ScalarCalls { get; private set; }

        [System.Diagnostics.CodeAnalysis.AllowNull]
        public override string CommandText { get; set; } = string.Empty;

        public override int CommandTimeout { get; set; }

        public override CommandType CommandType { get; set; }

        public override bool DesignTimeVisible { get; set; }

        public override UpdateRowSource UpdatedRowSource { get; set; }

        protected override DbConnection? DbConnection { get; set; }

        protected override DbParameterCollection DbParameterCollection => throw new NotSupportedException();

        protected override DbTransaction? DbTransaction { get; set; }

        public override void Cancel()
        {
        }

        public override int ExecuteNonQuery() => 0;

        public override object? ExecuteScalar() => 1;

        public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
        {
            ScalarCalls++;
            return Task.FromResult<object?>(1);
        }

        public override void Prepare()
        {
        }

        protected override DbParameter CreateDbParameter() => throw new NotSupportedException();

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
            => throw new NotSupportedException();
    }
}

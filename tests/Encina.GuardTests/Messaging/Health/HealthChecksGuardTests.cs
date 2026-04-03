using System.Data;
using Encina.Database;
using Encina.Messaging.DeadLetter;
using Encina.Messaging.Health;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.Sharding.ReferenceTables;
using Encina.Sharding.TimeBased;
using FluentAssertions;

namespace Encina.GuardTests.Messaging.Health;

/// <summary>
/// Guard clause tests for all 11 health check classes in Encina.Messaging.Health.
/// </summary>
public class HealthChecksGuardTests
{
    #region EncinaHealthCheck (abstract base)

    [Fact]
    public void EncinaHealthCheck_NullName_ThrowsArgumentException()
    {
        var act = () => new TestableHealthCheck(null!);

        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void EncinaHealthCheck_EmptyName_ThrowsArgumentException()
    {
        var act = () => new TestableHealthCheck("");

        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void EncinaHealthCheck_WhitespaceName_ThrowsArgumentException()
    {
        var act = () => new TestableHealthCheck("   ");

        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    #endregion

    #region DatabasePoolHealthCheck

    [Fact]
    public void DatabasePoolHealthCheck_NullMonitor_ThrowsArgumentNullException()
    {
        var act = () => new DatabasePoolHealthCheck(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("monitor");
    }

    #endregion

    #region DatabaseHealthCheck

    [Fact]
    public void DatabaseHealthCheck_NullConnectionFactory_ThrowsArgumentNullException()
    {
        var act = () => new TestableDatabaseHealthCheck("test", null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("connectionFactory");
    }

    #endregion

    #region DatabaseHealthMonitorBase

    [Fact]
    public void DatabaseHealthMonitorBase_NullProviderName_ThrowsArgumentException()
    {
        var act = () => new TestableDatabaseHealthMonitor(null!, () => Substitute.For<IDbConnection>());

        act.Should().Throw<ArgumentException>().WithParameterName("providerName");
    }

    [Fact]
    public void DatabaseHealthMonitorBase_EmptyProviderName_ThrowsArgumentException()
    {
        var act = () => new TestableDatabaseHealthMonitor("", () => Substitute.For<IDbConnection>());

        act.Should().Throw<ArgumentException>().WithParameterName("providerName");
    }

    [Fact]
    public void DatabaseHealthMonitorBase_WhitespaceProviderName_ThrowsArgumentException()
    {
        var act = () => new TestableDatabaseHealthMonitor("   ", () => Substitute.For<IDbConnection>());

        act.Should().Throw<ArgumentException>().WithParameterName("providerName");
    }

    [Fact]
    public void DatabaseHealthMonitorBase_NullConnectionFactory_ThrowsArgumentNullException()
    {
        var act = () => new TestableDatabaseHealthMonitor("test-provider", null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("connectionFactory");
    }

    #endregion

    #region TierTransitionHealthCheck

    [Fact]
    public void TierTransitionHealthCheck_NullTierStore_ThrowsArgumentNullException()
    {
        var act = () => new TierTransitionHealthCheck(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("tierStore");
    }

    #endregion

    #region ReferenceTableHealthCheck

    [Fact]
    public void ReferenceTableHealthCheck_NullRegistry_ThrowsArgumentNullException()
    {
        var act = () => new ReferenceTableHealthCheck(
            null!,
            Substitute.For<IReferenceTableStateStore>());

        act.Should().Throw<ArgumentNullException>().WithParameterName("registry");
    }

    [Fact]
    public void ReferenceTableHealthCheck_NullStateStore_ThrowsArgumentNullException()
    {
        var act = () => new ReferenceTableHealthCheck(
            Substitute.For<IReferenceTableRegistry>(),
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("stateStore");
    }

    #endregion

    #region DeadLetterHealthCheck

    [Fact]
    public void DeadLetterHealthCheck_NullStore_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterHealthCheck(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("store");
    }

    #endregion

    #region ShardCreationHealthCheck

    [Fact]
    public void ShardCreationHealthCheck_NullTierStore_ThrowsArgumentNullException()
    {
        var act = () => new ShardCreationHealthCheck(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("tierStore");
    }

    #endregion

    #region SchedulingHealthCheck

    [Fact]
    public void SchedulingHealthCheck_NullStore_ThrowsArgumentNullException()
    {
        var act = () => new SchedulingHealthCheck(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("store");
    }

    #endregion

    #region SagaHealthCheck

    [Fact]
    public void SagaHealthCheck_NullStore_ThrowsArgumentNullException()
    {
        var act = () => new SagaHealthCheck(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("store");
    }

    #endregion

    #region OutboxHealthCheck

    [Fact]
    public void OutboxHealthCheck_NullStore_ThrowsArgumentNullException()
    {
        var act = () => new OutboxHealthCheck(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("store");
    }

    #endregion

    #region InboxHealthCheck

    [Fact]
    public void InboxHealthCheck_NullStore_ThrowsArgumentNullException()
    {
        var act = () => new InboxHealthCheck(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("store");
    }

    #endregion

    #region Testable Implementations

    private sealed class TestableHealthCheck : EncinaHealthCheck
    {
        public TestableHealthCheck(string name) : base(name) { }

        protected override Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken cancellationToken)
            => Task.FromResult(HealthCheckResult.Healthy("ok"));
    }

    private sealed class TestableDatabaseHealthCheck : DatabaseHealthCheck
    {
        public TestableDatabaseHealthCheck(string name, Func<IDbConnection> connectionFactory)
            : base(name, connectionFactory) { }
    }

    private sealed class TestableDatabaseHealthMonitor : DatabaseHealthMonitorBase
    {
        public TestableDatabaseHealthMonitor(string providerName, Func<IDbConnection> connectionFactory)
            : base(providerName, connectionFactory) { }

        protected override ConnectionPoolStats GetPoolStatisticsCore()
            => ConnectionPoolStats.CreateEmpty();

        protected override Task ClearPoolCoreAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    #endregion
}

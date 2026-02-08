using System.Data;

using Encina.Database;
using Encina.Messaging.Health;

using Shouldly;

namespace Encina.GuardTests.Database;

/// <summary>
/// Guard clause tests for <see cref="DatabaseHealthMonitorBase"/>.
/// Verifies null and invalid argument validation on the protected constructor.
/// </summary>
public sealed class DatabaseHealthMonitorBaseGuardsTests
{
    [Fact]
    public void Constructor_NullProviderName_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new TestHealthMonitor(null!, () => Substitute.For<IDbConnection>()));
    }

    [Fact]
    public void Constructor_EmptyProviderName_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new TestHealthMonitor("", () => Substitute.For<IDbConnection>()));
    }

    [Fact]
    public void Constructor_WhitespaceProviderName_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new TestHealthMonitor("   ", () => Substitute.For<IDbConnection>()));
    }

    [Fact]
    public void Constructor_NullConnectionFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new TestHealthMonitor("test-provider", null!));
        ex.ParamName.ShouldBe("connectionFactory");
    }

    private sealed class TestHealthMonitor : DatabaseHealthMonitorBase
    {
        public TestHealthMonitor(string providerName, Func<IDbConnection> connectionFactory)
            : base(providerName, connectionFactory)
        {
        }

        protected override ConnectionPoolStats GetPoolStatisticsCore() => ConnectionPoolStats.CreateEmpty();
        protected override Task ClearPoolCoreAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

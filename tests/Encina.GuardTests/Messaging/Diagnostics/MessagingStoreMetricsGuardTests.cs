using Encina.Messaging.Diagnostics;
using FluentAssertions;

namespace Encina.GuardTests.Messaging.Diagnostics;

/// <summary>
/// Guard clause tests for MessagingStoreMetrics and MessagingStoreMetricsCallbacks.
/// </summary>
public class MessagingStoreMetricsGuardTests
{
    #region MessagingStoreMetricsCallbacks Constructor

    [Fact]
    public void MessagingStoreMetricsCallbacks_NullGetOutboxPendingCount_ThrowsArgumentNullException()
    {
        var act = () => new MessagingStoreMetricsCallbacks(null!, () => 0L);

        act.Should().Throw<ArgumentNullException>().WithParameterName("getOutboxPendingCount");
    }

    [Fact]
    public void MessagingStoreMetricsCallbacks_NullGetActiveSagaCount_ThrowsArgumentNullException()
    {
        var act = () => new MessagingStoreMetricsCallbacks(() => 0L, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("getActiveSagaCount");
    }

    #endregion

    #region MessagingStoreMetrics Construction

    [Fact]
    public void MessagingStoreMetrics_NullCallbacks_DoesNotThrow()
    {
        var act = () => new MessagingStoreMetrics(null);

        act.Should().NotThrow();
    }

    [Fact]
    public void MessagingStoreMetrics_DefaultConstructor_DoesNotThrow()
    {
        var act = () => new MessagingStoreMetrics();

        act.Should().NotThrow();
    }

    [Fact]
    public void MessagingStoreMetrics_WithCallbacks_DoesNotThrow()
    {
        var callbacks = new MessagingStoreMetricsCallbacks(() => 0L, () => 0L);

        var act = () => new MessagingStoreMetrics(callbacks);

        act.Should().NotThrow();
    }

    #endregion
}

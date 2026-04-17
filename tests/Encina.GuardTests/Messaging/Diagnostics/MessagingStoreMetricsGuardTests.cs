using Encina.Messaging.Diagnostics;
using Shouldly;

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

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("getOutboxPendingCount");
    }

    [Fact]
    public void MessagingStoreMetricsCallbacks_NullGetActiveSagaCount_ThrowsArgumentNullException()
    {
        var act = () => new MessagingStoreMetricsCallbacks(() => 0L, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("getActiveSagaCount");
    }

    #endregion

    #region MessagingStoreMetrics Construction

    [Fact]
    public void MessagingStoreMetrics_NullCallbacks_DoesNotThrow()
    {
        var act = () => new MessagingStoreMetrics(null);

        Should.NotThrow(act);
    }

    [Fact]
    public void MessagingStoreMetrics_DefaultConstructor_DoesNotThrow()
    {
        var act = () => new MessagingStoreMetrics();

        Should.NotThrow(act);
    }

    [Fact]
    public void MessagingStoreMetrics_WithCallbacks_DoesNotThrow()
    {
        var callbacks = new MessagingStoreMetricsCallbacks(() => 0L, () => 0L);

        var act = () => new MessagingStoreMetrics(callbacks);

        Should.NotThrow(act);
    }

    #endregion
}

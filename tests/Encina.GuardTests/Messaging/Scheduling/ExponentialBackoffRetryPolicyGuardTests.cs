using Encina.Messaging.Scheduling;
using Shouldly;

namespace Encina.GuardTests.Messaging.Scheduling;

/// <summary>
/// Guard clause tests for <see cref="ExponentialBackoffRetryPolicy"/> constructor.
/// </summary>
public sealed class ExponentialBackoffRetryPolicyGuardTests
{
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new ExponentialBackoffRetryPolicy(null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_ValidOptions_Succeeds()
    {
        var act = () => new ExponentialBackoffRetryPolicy(new SchedulingOptions());
        Should.NotThrow(act);
    }
}

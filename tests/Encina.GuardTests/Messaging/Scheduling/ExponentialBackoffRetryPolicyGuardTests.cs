using Encina.Messaging.Scheduling;
using FluentAssertions;

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
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_ValidOptions_Succeeds()
    {
        var act = () => new ExponentialBackoffRetryPolicy(new SchedulingOptions());
        act.Should().NotThrow();
    }
}

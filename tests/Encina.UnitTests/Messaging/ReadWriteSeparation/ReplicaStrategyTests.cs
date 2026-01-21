using Encina.Messaging.ReadWriteSeparation;
using Shouldly;

namespace Encina.UnitTests.Messaging.ReadWriteSeparation;

/// <summary>
/// Unit tests for <see cref="ReplicaStrategy"/> enum.
/// </summary>
public sealed class ReplicaStrategyTests
{
    [Fact]
    public void RoundRobin_HasCorrectValue()
    {
        ((int)ReplicaStrategy.RoundRobin).ShouldBe(0);
    }

    [Fact]
    public void Random_HasCorrectValue()
    {
        ((int)ReplicaStrategy.Random).ShouldBe(1);
    }

    [Fact]
    public void LeastConnections_HasCorrectValue()
    {
        ((int)ReplicaStrategy.LeastConnections).ShouldBe(2);
    }

    [Theory]
    [InlineData(ReplicaStrategy.RoundRobin, "RoundRobin")]
    [InlineData(ReplicaStrategy.Random, "Random")]
    [InlineData(ReplicaStrategy.LeastConnections, "LeastConnections")]
    public void ToString_ReturnsExpectedName(ReplicaStrategy strategy, string expectedName)
    {
        strategy.ToString().ShouldBe(expectedName);
    }

    [Fact]
    public void AllValues_AreDefined()
    {
        var values = Enum.GetValues<ReplicaStrategy>();
        values.ShouldContain(ReplicaStrategy.RoundRobin);
        values.ShouldContain(ReplicaStrategy.Random);
        values.ShouldContain(ReplicaStrategy.LeastConnections);
        values.Length.ShouldBe(3);
    }
}

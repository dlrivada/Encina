namespace Encina.UnitTests.Core;

public sealed class NotificationDispatchOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var options = new NotificationDispatchOptions();

        options.Strategy.ShouldBe(NotificationDispatchStrategy.Sequential);
        options.MaxDegreeOfParallelism.ShouldBe(-1);
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var options = new NotificationDispatchOptions
        {
            Strategy = NotificationDispatchStrategy.ParallelWhenAll,
            MaxDegreeOfParallelism = 4
        };

        options.Strategy.ShouldBe(NotificationDispatchStrategy.ParallelWhenAll);
        options.MaxDegreeOfParallelism.ShouldBe(4);
    }

    [Theory]
    [InlineData(NotificationDispatchStrategy.Sequential)]
    [InlineData(NotificationDispatchStrategy.Parallel)]
    [InlineData(NotificationDispatchStrategy.ParallelWhenAll)]
    public void Strategy_AcceptsAllValidValues(NotificationDispatchStrategy strategy)
    {
        var options = new NotificationDispatchOptions { Strategy = strategy };
        options.Strategy.ShouldBe(strategy);
    }
}

using Encina.Messaging.ScatterGather;
using Shouldly;

namespace Encina.UnitTests.Messaging.ScatterGather;

/// <summary>
/// Unit tests for <see cref="ScatterGatherOptions"/>.
/// </summary>
public sealed class ScatterGatherOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new ScatterGatherOptions();

        // Assert
        options.DefaultTimeout.ShouldBe(TimeSpan.FromSeconds(30));
        options.ExecuteScattersInParallel.ShouldBeTrue();
        options.MaxDegreeOfParallelism.ShouldBe(Environment.ProcessorCount);
        options.DefaultGatherStrategy.ShouldBe(GatherStrategy.WaitForAll);
        options.DefaultQuorumCount.ShouldBeNull();
        options.IncludeFailedResultsInGather.ShouldBeFalse();
        options.CancelRemainingOnStrategyComplete.ShouldBeTrue();
    }

    [Fact]
    public void CanSetDefaultTimeout()
    {
        // Arrange & Act
        var options = new ScatterGatherOptions { DefaultTimeout = TimeSpan.FromMinutes(1) };

        // Assert
        options.DefaultTimeout.ShouldBe(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void CanSetExecuteScattersInParallel()
    {
        // Arrange & Act
        var options = new ScatterGatherOptions { ExecuteScattersInParallel = false };

        // Assert
        options.ExecuteScattersInParallel.ShouldBeFalse();
    }

    [Fact]
    public void CanSetMaxDegreeOfParallelism()
    {
        // Arrange & Act
        var options = new ScatterGatherOptions { MaxDegreeOfParallelism = 4 };

        // Assert
        options.MaxDegreeOfParallelism.ShouldBe(4);
    }

    [Fact]
    public void CanSetDefaultGatherStrategy_WaitForFirst()
    {
        // Arrange & Act
        var options = new ScatterGatherOptions { DefaultGatherStrategy = GatherStrategy.WaitForFirst };

        // Assert
        options.DefaultGatherStrategy.ShouldBe(GatherStrategy.WaitForFirst);
    }

    [Fact]
    public void CanSetDefaultGatherStrategy_WaitForQuorum()
    {
        // Arrange & Act
        var options = new ScatterGatherOptions { DefaultGatherStrategy = GatherStrategy.WaitForQuorum };

        // Assert
        options.DefaultGatherStrategy.ShouldBe(GatherStrategy.WaitForQuorum);
    }

    [Fact]
    public void CanSetDefaultQuorumCount()
    {
        // Arrange & Act
        var options = new ScatterGatherOptions { DefaultQuorumCount = 3 };

        // Assert
        options.DefaultQuorumCount.ShouldBe(3);
    }

    [Fact]
    public void CanSetIncludeFailedResultsInGather()
    {
        // Arrange & Act
        var options = new ScatterGatherOptions { IncludeFailedResultsInGather = true };

        // Assert
        options.IncludeFailedResultsInGather.ShouldBeTrue();
    }

    [Fact]
    public void CanSetCancelRemainingOnStrategyComplete()
    {
        // Arrange & Act
        var options = new ScatterGatherOptions { CancelRemainingOnStrategyComplete = false };

        // Assert
        options.CancelRemainingOnStrategyComplete.ShouldBeFalse();
    }

    [Fact]
    public void CanSetAllProperties()
    {
        // Arrange & Act
        var options = new ScatterGatherOptions
        {
            DefaultTimeout = TimeSpan.FromSeconds(60),
            ExecuteScattersInParallel = false,
            MaxDegreeOfParallelism = 2,
            DefaultGatherStrategy = GatherStrategy.WaitForQuorum,
            DefaultQuorumCount = 5,
            IncludeFailedResultsInGather = true,
            CancelRemainingOnStrategyComplete = false
        };

        // Assert
        options.DefaultTimeout.ShouldBe(TimeSpan.FromSeconds(60));
        options.ExecuteScattersInParallel.ShouldBeFalse();
        options.MaxDegreeOfParallelism.ShouldBe(2);
        options.DefaultGatherStrategy.ShouldBe(GatherStrategy.WaitForQuorum);
        options.DefaultQuorumCount.ShouldBe(5);
        options.IncludeFailedResultsInGather.ShouldBeTrue();
        options.CancelRemainingOnStrategyComplete.ShouldBeFalse();
    }
}

/// <summary>
/// Unit tests for <see cref="GatherStrategy"/>.
/// </summary>
public sealed class GatherStrategyTests
{
    [Fact]
    public void GatherStrategy_Values_AreCorrect()
    {
        // Assert
        ((int)GatherStrategy.WaitForAll).ShouldBe(0);
        ((int)GatherStrategy.WaitForFirst).ShouldBe(1);
        ((int)GatherStrategy.WaitForQuorum).ShouldBe(2);
    }
}

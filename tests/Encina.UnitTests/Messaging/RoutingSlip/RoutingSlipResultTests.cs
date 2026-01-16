using Encina.Messaging.RoutingSlip;
using Shouldly;

namespace Encina.UnitTests.Messaging.RoutingSlip;

/// <summary>
/// Unit tests for <see cref="RoutingSlipResult{TData}"/>.
/// </summary>
public sealed class RoutingSlipResultTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        // Arrange
        var routingSlipId = Guid.NewGuid();
        var finalData = new TestData { Value = "final" };
        var stepsExecuted = 3;
        var stepsAdded = 1;
        var stepsRemoved = 1;
        var duration = TimeSpan.FromSeconds(5);
        var activityLog = new List<RoutingSlipActivityEntry<TestData>>
        {
            new("Step1", new TestData { Value = "step1" }, null, DateTime.UtcNow)
        };

        // Act
        var result = new RoutingSlipResult<TestData>(
            routingSlipId,
            finalData,
            stepsExecuted,
            stepsAdded,
            stepsRemoved,
            duration,
            activityLog);

        // Assert
        result.RoutingSlipId.ShouldBe(routingSlipId);
        result.FinalData.ShouldBe(finalData);
        result.StepsExecuted.ShouldBe(stepsExecuted);
        result.StepsAdded.ShouldBe(stepsAdded);
        result.StepsRemoved.ShouldBe(stepsRemoved);
        result.Duration.ShouldBe(duration);
        result.ActivityLog.ShouldBe(activityLog);
    }

    [Fact]
    public void ActivityLog_CanBeEmpty()
    {
        // Arrange
        var result = new RoutingSlipResult<TestData>(
            Guid.NewGuid(),
            new TestData { Value = "final" },
            0,
            0,
            0,
            TimeSpan.Zero,
            []);

        // Assert
        result.ActivityLog.ShouldBeEmpty();
    }

    [Fact]
    public void FinalData_PreservesData()
    {
        // Arrange
        var data = new TestData { Value = "preserved", Number = 42 };

        // Act
        var result = new RoutingSlipResult<TestData>(
            Guid.NewGuid(),
            data,
            1,
            0,
            0,
            TimeSpan.FromMilliseconds(100),
            []);

        // Assert
        result.FinalData.Value.ShouldBe("preserved");
        result.FinalData.Number.ShouldBe(42);
    }

    [Fact]
    public void Duration_CanBeZero()
    {
        // Arrange & Act
        var result = new RoutingSlipResult<TestData>(
            Guid.NewGuid(),
            new TestData(),
            0,
            0,
            0,
            TimeSpan.Zero,
            []);

        // Assert
        result.Duration.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void Counters_CanAllBeZero()
    {
        // Arrange & Act
        var result = new RoutingSlipResult<TestData>(
            Guid.NewGuid(),
            new TestData(),
            0,
            0,
            0,
            TimeSpan.Zero,
            []);

        // Assert
        result.StepsExecuted.ShouldBe(0);
        result.StepsAdded.ShouldBe(0);
        result.StepsRemoved.ShouldBe(0);
    }

    private sealed class TestData
    {
        public string Value { get; set; } = string.Empty;
        public int Number { get; set; }
    }
}

/// <summary>
/// Unit tests for <see cref="RoutingSlipActivityEntry{TData}"/>.
/// </summary>
public sealed class RoutingSlipActivityEntryTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        // Arrange
        var stepName = "ProcessOrder";
        var data = new TestData { Value = "processed" };
        Func<TestData, RoutingSlipContext<TestData>, CancellationToken, Task>? compensate =
            (_, _, _) => Task.CompletedTask;
        var executedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var metadata = new Dictionary<string, object?> { ["key"] = "value" };

        // Act
        var entry = new RoutingSlipActivityEntry<TestData>(
            stepName, data, compensate, executedAt, metadata);

        // Assert
        entry.StepName.ShouldBe(stepName);
        entry.DataAfterExecution.ShouldBe(data);
        entry.Compensate.ShouldBe(compensate);
        entry.ExecutedAtUtc.ShouldBe(executedAt);
        entry.Metadata.ShouldContainKey("key");
        entry.Metadata["key"].ShouldBe("value");
    }

    [Fact]
    public void Constructor_NullStepName_ThrowsArgumentException()
    {
        // Act
        var act = () => new RoutingSlipActivityEntry<TestData>(
            null!, new TestData(), null, DateTime.UtcNow);

        // Assert
        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void Constructor_EmptyStepName_ThrowsArgumentException()
    {
        // Act
        var act = () => new RoutingSlipActivityEntry<TestData>(
            string.Empty, new TestData(), null, DateTime.UtcNow);

        // Assert
        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void Constructor_WhitespaceStepName_ThrowsArgumentException()
    {
        // Act
        var act = () => new RoutingSlipActivityEntry<TestData>(
            "   ", new TestData(), null, DateTime.UtcNow);

        // Assert
        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void Constructor_NullData_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new RoutingSlipActivityEntry<TestData>(
            "Step", null!, null, DateTime.UtcNow);

        // Assert
        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullCompensate_IsAllowed()
    {
        // Act
        var entry = new RoutingSlipActivityEntry<TestData>(
            "Step", new TestData(), null, DateTime.UtcNow);

        // Assert
        entry.Compensate.ShouldBeNull();
    }

    [Fact]
    public void Constructor_NullMetadata_UsesEmptyDictionary()
    {
        // Act
        var entry = new RoutingSlipActivityEntry<TestData>(
            "Step", new TestData(), null, DateTime.UtcNow, null);

        // Assert
        entry.Metadata.ShouldNotBeNull();
        entry.Metadata.ShouldBeEmpty();
    }

    private sealed class TestData
    {
        public string Value { get; set; } = string.Empty;
    }
}

/// <summary>
/// Unit tests for <see cref="RoutingSlipStatus"/>.
/// </summary>
public sealed class RoutingSlipStatusTests
{
    [Fact]
    public void Running_HasCorrectValue()
    {
        RoutingSlipStatus.Running.ShouldBe("Running");
    }

    [Fact]
    public void Completed_HasCorrectValue()
    {
        RoutingSlipStatus.Completed.ShouldBe("Completed");
    }

    [Fact]
    public void Compensating_HasCorrectValue()
    {
        RoutingSlipStatus.Compensating.ShouldBe("Compensating");
    }

    [Fact]
    public void Compensated_HasCorrectValue()
    {
        RoutingSlipStatus.Compensated.ShouldBe("Compensated");
    }

    [Fact]
    public void Failed_HasCorrectValue()
    {
        RoutingSlipStatus.Failed.ShouldBe("Failed");
    }

    [Fact]
    public void TimedOut_HasCorrectValue()
    {
        RoutingSlipStatus.TimedOut.ShouldBe("TimedOut");
    }
}

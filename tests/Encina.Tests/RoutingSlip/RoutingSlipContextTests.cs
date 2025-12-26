using Encina.Messaging.RoutingSlip;
using LanguageExt;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.Tests.RoutingSlip;

public sealed class RoutingSlipContextTests
{
    [Fact]
    public void AddStep_AddsStepToEndOfItinerary()
    {
        // Arrange
        var remainingSteps = new List<RoutingSlipStepDefinition<TestData>>
        {
            CreateStep("Step1"),
            CreateStep("Step2")
        };

        var context = CreateContext(remainingSteps);
        var newStep = CreateStep("Step3");

        // Act
        context.AddStep(newStep);

        // Assert
        remainingSteps.Count.ShouldBe(3);
        remainingSteps[2].Name.ShouldBe("Step3");
    }

    [Fact]
    public void AddStep_WithNullStep_ThrowsArgumentNullException()
    {
        // Arrange
        var context = CreateContext([]);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => context.AddStep(null!));
    }

    [Fact]
    public void AddStepNext_InsertsStepAtBeginning()
    {
        // Arrange
        var remainingSteps = new List<RoutingSlipStepDefinition<TestData>>
        {
            CreateStep("Step2"),
            CreateStep("Step3")
        };

        var context = CreateContext(remainingSteps);
        var newStep = CreateStep("Step1");

        // Act
        context.AddStepNext(newStep);

        // Assert
        remainingSteps.Count.ShouldBe(3);
        remainingSteps[0].Name.ShouldBe("Step1");
        remainingSteps[1].Name.ShouldBe("Step2");
        remainingSteps[2].Name.ShouldBe("Step3");
    }

    [Fact]
    public void AddStepNext_WithNullStep_ThrowsArgumentNullException()
    {
        // Arrange
        var context = CreateContext([]);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => context.AddStepNext(null!));
    }

    [Fact]
    public void InsertStep_InsertsStepAtSpecifiedIndex()
    {
        // Arrange
        var remainingSteps = new List<RoutingSlipStepDefinition<TestData>>
        {
            CreateStep("Step1"),
            CreateStep("Step3")
        };

        var context = CreateContext(remainingSteps);
        var newStep = CreateStep("Step2");

        // Act
        context.InsertStep(1, newStep);

        // Assert
        remainingSteps.Count.ShouldBe(3);
        remainingSteps[0].Name.ShouldBe("Step1");
        remainingSteps[1].Name.ShouldBe("Step2");
        remainingSteps[2].Name.ShouldBe("Step3");
    }

    [Fact]
    public void InsertStep_WithNullStep_ThrowsArgumentNullException()
    {
        // Arrange
        var context = CreateContext([]);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => context.InsertStep(0, null!));
    }

    [Fact]
    public void InsertStep_WithNegativeIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var context = CreateContext([CreateStep("Step1")]);

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() =>
            context.InsertStep(-1, CreateStep("Step2")));
    }

    [Fact]
    public void InsertStep_WithIndexGreaterThanCount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var context = CreateContext([CreateStep("Step1")]);

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() =>
            context.InsertStep(5, CreateStep("Step2")));
    }

    [Fact]
    public void RemoveStepAt_RemovesStepAtSpecifiedIndex()
    {
        // Arrange
        var remainingSteps = new List<RoutingSlipStepDefinition<TestData>>
        {
            CreateStep("Step1"),
            CreateStep("Step2"),
            CreateStep("Step3")
        };

        var context = CreateContext(remainingSteps);

        // Act
        var result = context.RemoveStepAt(1);

        // Assert
        result.ShouldBeTrue();
        remainingSteps.Count.ShouldBe(2);
        remainingSteps[0].Name.ShouldBe("Step1");
        remainingSteps[1].Name.ShouldBe("Step3");
    }

    [Fact]
    public void RemoveStepAt_WithNegativeIndex_ReturnsFalse()
    {
        // Arrange
        var context = CreateContext([CreateStep("Step1")]);

        // Act
        var result = context.RemoveStepAt(-1);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void RemoveStepAt_WithIndexOutOfRange_ReturnsFalse()
    {
        // Arrange
        var context = CreateContext([CreateStep("Step1")]);

        // Act
        var result = context.RemoveStepAt(5);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void ClearRemainingSteps_RemovesAllSteps()
    {
        // Arrange
        var remainingSteps = new List<RoutingSlipStepDefinition<TestData>>
        {
            CreateStep("Step1"),
            CreateStep("Step2"),
            CreateStep("Step3")
        };

        var context = CreateContext(remainingSteps);

        // Act
        context.ClearRemainingSteps();

        // Assert
        remainingSteps.Count.ShouldBe(0);
    }

    [Fact]
    public void GetRemainingStepNames_ReturnsStepNamesInOrder()
    {
        // Arrange
        var remainingSteps = new List<RoutingSlipStepDefinition<TestData>>
        {
            CreateStep("Step1"),
            CreateStep("Step2"),
            CreateStep("Step3")
        };

        var context = CreateContext(remainingSteps);

        // Act
        var names = context.GetRemainingStepNames();

        // Assert
        names.ShouldBe(["Step1", "Step2", "Step3"]);
    }

    [Fact]
    public void RemainingStepCount_ReturnsCorrectCount()
    {
        // Arrange
        var remainingSteps = new List<RoutingSlipStepDefinition<TestData>>
        {
            CreateStep("Step1"),
            CreateStep("Step2")
        };

        var context = CreateContext(remainingSteps);

        // Act & Assert
        context.RemainingStepCount.ShouldBe(2);
    }

    [Fact]
    public void CurrentStepIndex_ReturnsActivityLogCount()
    {
        // Arrange
        var activityLog = new List<RoutingSlipActivityEntry<TestData>>
        {
            new("Step1", new TestData(), null, DateTime.UtcNow),
            new("Step2", new TestData(), null, DateTime.UtcNow)
        };

        var context = CreateContext([], activityLog);

        // Act & Assert
        context.CurrentStepIndex.ShouldBe(2);
    }

    [Fact]
    public void RoutingSlipId_ReturnsCorrectId()
    {
        // Arrange
        var id = Guid.NewGuid();
        var context = CreateContext([], [], id);

        // Act & Assert
        context.RoutingSlipId.ShouldBe(id);
    }

    [Fact]
    public void SlipType_ReturnsCorrectType()
    {
        // Arrange
        var context = CreateContext([], [], slipType: "TestSlip");

        // Act & Assert
        context.SlipType.ShouldBe("TestSlip");
    }

    [Fact]
    public void RequestContext_ReturnsCorrectContext()
    {
        // Arrange
        var requestContext = RequestContext.Create();
        var context = CreateContext([], [], requestContext: requestContext);

        // Act & Assert
        context.RequestContext.ShouldBe(requestContext);
    }

    [Fact]
    public void ActivityLog_ReturnsReadOnlyView()
    {
        // Arrange
        var activityLog = new List<RoutingSlipActivityEntry<TestData>>
        {
            new("Step1", new TestData(), null, DateTime.UtcNow)
        };

        var context = CreateContext([], activityLog);

        // Act & Assert
        context.ActivityLog.Count.ShouldBe(1);
        context.ActivityLog[0].StepName.ShouldBe("Step1");
    }

    private static RoutingSlipContext<TestData> CreateContext(
        List<RoutingSlipStepDefinition<TestData>> remainingSteps,
        List<RoutingSlipActivityEntry<TestData>>? activityLog = null,
        Guid? routingSlipId = null,
        string slipType = "TestSlip",
        IRequestContext? requestContext = null)
    {
        // Use reflection to access internal constructor
        var ctorParams = new object[]
        {
            routingSlipId ?? Guid.NewGuid(),
            slipType,
            requestContext ?? RequestContext.Create(),
            remainingSteps,
            activityLog ?? []
        };

        var contextType = typeof(RoutingSlipContext<TestData>);
        var ctor = contextType.GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            [typeof(Guid), typeof(string), typeof(IRequestContext), typeof(List<RoutingSlipStepDefinition<TestData>>), typeof(List<RoutingSlipActivityEntry<TestData>>)],
            null);

        return (RoutingSlipContext<TestData>)ctor!.Invoke(ctorParams);
    }

    private static RoutingSlipStepDefinition<TestData> CreateStep(string name)
    {
        return new RoutingSlipStepDefinition<TestData>(
            name,
            (data, ctx, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)));
    }

    private sealed class TestData
    {
        public Guid Id { get; set; }
    }
}

using Encina.Messaging.RoutingSlip;

using LanguageExt;

using NSubstitute;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.Messaging.Tests.RoutingSlip;

/// <summary>
/// Unit tests for <see cref="RoutingSlipContext{TData}"/>.
/// </summary>
public sealed class RoutingSlipContextTests
{
    private sealed record TestData
    {
        public int Value { get; init; }
    }

    #region Constructor and Properties

    [Fact]
    public void Constructor_SetsAllProperties()
    {
        // Arrange
        var routingSlipId = Guid.NewGuid();
        const string slipType = "TestSlip";
        var requestContext = Substitute.For<IRequestContext>();
        var remainingSteps = new List<RoutingSlipStepDefinition<TestData>>();
        var activityLog = new List<RoutingSlipActivityEntry<TestData>>();

        // Act
        var context = CreateContext(routingSlipId, slipType, requestContext, remainingSteps, activityLog);

        // Assert
        context.RoutingSlipId.ShouldBe(routingSlipId);
        context.SlipType.ShouldBe(slipType);
        context.RequestContext.ShouldBe(requestContext);
    }

    [Fact]
    public void CurrentStepIndex_ReturnsActivityLogCount()
    {
        // Arrange
        var activityLog = new List<RoutingSlipActivityEntry<TestData>>
        {
            CreateActivityEntry("Step1"),
            CreateActivityEntry("Step2")
        };
        var context = CreateContext(activityLog: activityLog);

        // Act & Assert
        context.CurrentStepIndex.ShouldBe(2);
    }

    [Fact]
    public void RemainingStepCount_ReturnsRemainingStepsCount()
    {
        // Arrange
        var remainingSteps = new List<RoutingSlipStepDefinition<TestData>>
        {
            CreateStepDefinition("Step1"),
            CreateStepDefinition("Step2"),
            CreateStepDefinition("Step3")
        };
        var context = CreateContext(remainingSteps: remainingSteps);

        // Act & Assert
        context.RemainingStepCount.ShouldBe(3);
    }

    [Fact]
    public void ActivityLog_ReturnsReadOnlyListOfEntries()
    {
        // Arrange
        var entry1 = CreateActivityEntry("Step1");
        var entry2 = CreateActivityEntry("Step2");
        var activityLog = new List<RoutingSlipActivityEntry<TestData>> { entry1, entry2 };
        var context = CreateContext(activityLog: activityLog);

        // Act
        var result = context.ActivityLog;

        // Assert
        result.ShouldBe(activityLog);
        result.Count.ShouldBe(2);
    }

    #endregion

    #region AddStep

    [Fact]
    public void AddStep_AddsStepToEndOfItinerary()
    {
        // Arrange
        var step1 = CreateStepDefinition("Step1");
        var step2 = CreateStepDefinition("Step2");
        var remainingSteps = new List<RoutingSlipStepDefinition<TestData>> { step1 };
        var context = CreateContext(remainingSteps: remainingSteps);

        // Act
        context.AddStep(step2);

        // Assert
        context.RemainingStepCount.ShouldBe(2);
        AssertNames(context.GetRemainingStepNames(), "Step1", "Step2");
    }

    [Fact]
    public void AddStep_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        var context = CreateContext();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => context.AddStep(null!));
    }

    #endregion

    #region AddStepNext

    [Fact]
    public void AddStepNext_InsertsStepAtBeginning()
    {
        // Arrange
        var step1 = CreateStepDefinition("Step1");
        var step2 = CreateStepDefinition("Step2");
        var remainingSteps = new List<RoutingSlipStepDefinition<TestData>> { step1 };
        var context = CreateContext(remainingSteps: remainingSteps);

        // Act
        context.AddStepNext(step2);

        // Assert
        context.RemainingStepCount.ShouldBe(2);
        AssertNames(context.GetRemainingStepNames(), "Step2", "Step1");
    }

    [Fact]
    public void AddStepNext_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        var context = CreateContext();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => context.AddStepNext(null!));
    }

    #endregion

    #region InsertStep

    [Fact]
    public void InsertStep_AtIndex0_InsertsAtBeginning()
    {
        // Arrange
        var step1 = CreateStepDefinition("Step1");
        var step2 = CreateStepDefinition("Step2");
        var remainingSteps = new List<RoutingSlipStepDefinition<TestData>> { step1 };
        var context = CreateContext(remainingSteps: remainingSteps);

        // Act
        context.InsertStep(0, step2);

        // Assert
        AssertNames(context.GetRemainingStepNames(), "Step2", "Step1");
    }

    [Fact]
    public void InsertStep_AtMiddle_InsertsAtCorrectPosition()
    {
        // Arrange
        var step1 = CreateStepDefinition("Step1");
        var step2 = CreateStepDefinition("Step2");
        var step3 = CreateStepDefinition("Step3");
        var remainingSteps = new List<RoutingSlipStepDefinition<TestData>> { step1, step3 };
        var context = CreateContext(remainingSteps: remainingSteps);

        // Act
        context.InsertStep(1, step2);

        // Assert
        AssertNames(context.GetRemainingStepNames(), "Step1", "Step2", "Step3");
    }

    [Fact]
    public void InsertStep_AtEnd_AppendsStep()
    {
        // Arrange
        var step1 = CreateStepDefinition("Step1");
        var step2 = CreateStepDefinition("Step2");
        var remainingSteps = new List<RoutingSlipStepDefinition<TestData>> { step1 };
        var context = CreateContext(remainingSteps: remainingSteps);

        // Act
        context.InsertStep(1, step2);

        // Assert
        AssertNames(context.GetRemainingStepNames(), "Step1", "Step2");
    }

    [Fact]
    public void InsertStep_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        var context = CreateContext();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => context.InsertStep(0, null!));
    }

    [Fact]
    public void InsertStep_WithNegativeIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var step = CreateStepDefinition("Step");
        var context = CreateContext();

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => context.InsertStep(-1, step));
    }

    [Fact]
    public void InsertStep_WithIndexGreaterThanCount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var step = CreateStepDefinition("Step");
        var context = CreateContext();

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => context.InsertStep(1, step));
    }

    #endregion

    #region RemoveStepAt

    [Fact]
    public void RemoveStepAt_ValidIndex_RemovesStepAndReturnsTrue()
    {
        // Arrange
        var step1 = CreateStepDefinition("Step1");
        var step2 = CreateStepDefinition("Step2");
        var step3 = CreateStepDefinition("Step3");
        var remainingSteps = new List<RoutingSlipStepDefinition<TestData>> { step1, step2, step3 };
        var context = CreateContext(remainingSteps: remainingSteps);

        // Act
        var result = context.RemoveStepAt(1);

        // Assert
        result.ShouldBeTrue();
        context.RemainingStepCount.ShouldBe(2);
        AssertNames(context.GetRemainingStepNames(), "Step1", "Step3");
    }

    [Fact]
    public void RemoveStepAt_FirstIndex_RemovesFirstStep()
    {
        // Arrange
        var step1 = CreateStepDefinition("Step1");
        var step2 = CreateStepDefinition("Step2");
        var remainingSteps = new List<RoutingSlipStepDefinition<TestData>> { step1, step2 };
        var context = CreateContext(remainingSteps: remainingSteps);

        // Act
        var result = context.RemoveStepAt(0);

        // Assert
        result.ShouldBeTrue();
        AssertNames(context.GetRemainingStepNames(), "Step2");
    }

    [Fact]
    public void RemoveStepAt_LastIndex_RemovesLastStep()
    {
        // Arrange
        var step1 = CreateStepDefinition("Step1");
        var step2 = CreateStepDefinition("Step2");
        var remainingSteps = new List<RoutingSlipStepDefinition<TestData>> { step1, step2 };
        var context = CreateContext(remainingSteps: remainingSteps);

        // Act
        var result = context.RemoveStepAt(1);

        // Assert
        result.ShouldBeTrue();
        AssertNames(context.GetRemainingStepNames(), "Step1");
    }

    [Fact]
    public void RemoveStepAt_NegativeIndex_ReturnsFalse()
    {
        // Arrange
        var context = CreateContext();

        // Act
        var result = context.RemoveStepAt(-1);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void RemoveStepAt_IndexEqualToCount_ReturnsFalse()
    {
        // Arrange
        var step = CreateStepDefinition("Step");
        var remainingSteps = new List<RoutingSlipStepDefinition<TestData>> { step };
        var context = CreateContext(remainingSteps: remainingSteps);

        // Act
        var result = context.RemoveStepAt(1);

        // Assert
        result.ShouldBeFalse();
        context.RemainingStepCount.ShouldBe(1);
    }

    [Fact]
    public void RemoveStepAt_IndexGreaterThanCount_ReturnsFalse()
    {
        // Arrange
        var context = CreateContext();

        // Act
        var result = context.RemoveStepAt(10);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region ClearRemainingSteps

    [Fact]
    public void ClearRemainingSteps_RemovesAllSteps()
    {
        // Arrange
        var step1 = CreateStepDefinition("Step1");
        var step2 = CreateStepDefinition("Step2");
        var remainingSteps = new List<RoutingSlipStepDefinition<TestData>> { step1, step2 };
        var context = CreateContext(remainingSteps: remainingSteps);

        // Act
        context.ClearRemainingSteps();

        // Assert
        context.RemainingStepCount.ShouldBe(0);
        context.GetRemainingStepNames().ShouldBeEmpty();
    }

    [Fact]
    public void ClearRemainingSteps_WhenEmpty_DoesNothing()
    {
        // Arrange
        var context = CreateContext();

        // Act
        context.ClearRemainingSteps();

        // Assert
        context.RemainingStepCount.ShouldBe(0);
    }

    #endregion

    #region GetRemainingStepNames

    [Fact]
    public void GetRemainingStepNames_ReturnsStepNamesInOrder()
    {
        // Arrange
        var step1 = CreateStepDefinition("Alpha");
        var step2 = CreateStepDefinition("Beta");
        var step3 = CreateStepDefinition("Gamma");
        var remainingSteps = new List<RoutingSlipStepDefinition<TestData>> { step1, step2, step3 };
        var context = CreateContext(remainingSteps: remainingSteps);

        // Act
        var names = context.GetRemainingStepNames();

        // Assert
        AssertNames(names, "Alpha", "Beta", "Gamma");
    }

    [Fact]
    public void GetRemainingStepNames_WhenEmpty_ReturnsEmptyList()
    {
        // Arrange
        var context = CreateContext();

        // Act
        var names = context.GetRemainingStepNames();

        // Assert
        names.ShouldBeEmpty();
    }

    #endregion

    #region Helper Methods

    private static void AssertNames(IReadOnlyList<string> actual, params string[] expected)
    {
        actual.Count.ShouldBe(expected.Length);
        for (var i = 0; i < expected.Length; i++)
        {
            actual[i].ShouldBe(expected[i]);
        }
    }

    private static RoutingSlipContext<TestData> CreateContext(
        Guid? routingSlipId = null,
        string slipType = "TestSlip",
        IRequestContext? requestContext = null,
        List<RoutingSlipStepDefinition<TestData>>? remainingSteps = null,
        List<RoutingSlipActivityEntry<TestData>>? activityLog = null)
    {
        // Use reflection to create instance since constructor is internal
        var type = typeof(RoutingSlipContext<TestData>);
        var constructor = type.GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            [typeof(Guid), typeof(string), typeof(IRequestContext), typeof(List<RoutingSlipStepDefinition<TestData>>), typeof(List<RoutingSlipActivityEntry<TestData>>)],
            null);

        return (RoutingSlipContext<TestData>)constructor!.Invoke(
        [
            routingSlipId ?? Guid.NewGuid(),
            slipType,
            requestContext ?? Substitute.For<IRequestContext>(),
            remainingSteps ?? [],
            activityLog ?? []
        ]);
    }

    private static RoutingSlipStepDefinition<TestData> CreateStepDefinition(string name)
    {
        return new RoutingSlipStepDefinition<TestData>(
            name,
            execute: (data, _, _) => ValueTask.FromResult(Right<EncinaError, TestData>(data)),
            compensate: null,
            metadata: null);
    }

    private static RoutingSlipActivityEntry<TestData> CreateActivityEntry(string stepName)
    {
        return new RoutingSlipActivityEntry<TestData>(
            stepName,
            dataAfterExecution: new TestData { Value = 1 },
            compensate: null,
            executedAtUtc: DateTime.UtcNow,
            metadata: null);
    }

    #endregion
}

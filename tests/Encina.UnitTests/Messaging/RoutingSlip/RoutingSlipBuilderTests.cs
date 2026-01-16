using Encina.Messaging.RoutingSlip;
using LanguageExt;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Messaging.RoutingSlip;

/// <summary>
/// Unit tests for <see cref="RoutingSlipBuilder"/> and related types.
/// </summary>
public sealed class RoutingSlipBuilderTests
{
    #region RoutingSlipBuilder Static Factory Tests

    [Fact]
    public void Create_WithValidSlipType_ReturnsBuilder()
    {
        // Act
        var builder = RoutingSlipBuilder.Create<TestData>("ProcessOrder");

        // Assert
        builder.ShouldNotBeNull();
        builder.ShouldBeOfType<RoutingSlipBuilder<TestData>>();
        builder.SlipType.ShouldBe("ProcessOrder");
    }

    [Fact]
    public void Create_WithNullSlipType_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => RoutingSlipBuilder.Create<TestData>(null!));
    }

    [Fact]
    public void Create_WithEmptySlipType_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => RoutingSlipBuilder.Create<TestData>(string.Empty));
    }

    [Fact]
    public void Create_WithWhitespaceSlipType_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => RoutingSlipBuilder.Create<TestData>("   "));
    }

    #endregion

    #region RoutingSlipBuilder<TData> Step Tests

    [Fact]
    public void Step_WithName_ReturnsStepBuilder()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("Test");

        // Act
        var stepBuilder = builder.Step("ValidateOrder");

        // Assert
        stepBuilder.ShouldNotBeNull();
        stepBuilder.ShouldBeOfType<RoutingSlipStepBuilder<TestData>>();
    }

    [Fact]
    public void Step_WithoutName_GeneratesName()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("Test");

        // Act - Build with auto-generated name
        var definition = builder
            .Step()
            .Execute(data => Right<EncinaError, TestData>(data))
            .Build();

        // Assert
        definition.Steps[0].Name.ShouldBe("Step 1");
    }

    [Fact]
    public void MultipleSteps_HaveSequentialDefaultNames()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("Test");

        // Act
        var definition = builder
            .Step().Execute(data => Right<EncinaError, TestData>(data))
            .Step().Execute(data => Right<EncinaError, TestData>(data))
            .Step().Execute(data => Right<EncinaError, TestData>(data))
            .Build();

        // Assert
        definition.Steps[0].Name.ShouldBe("Step 1");
        definition.Steps[1].Name.ShouldBe("Step 2");
        definition.Steps[2].Name.ShouldBe("Step 3");
    }

    #endregion

    #region RoutingSlipBuilder<TData> OnCompletion Tests

    [Fact]
    public void OnCompletion_WithAsyncHandler_SetsHandler()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("Test");

        // Act
        var definition = builder
            .Step("Step1").Execute(data => Right<EncinaError, TestData>(data))
            .OnCompletion(async (data, ctx, ct) =>
            {
                await Task.Delay(1, ct);
            })
            .Build();

        // Assert
        definition.OnCompletion.ShouldNotBeNull();
    }

    [Fact]
    public void OnCompletion_WithSimplifiedHandler_SetsHandler()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("Test");

        // Act - Step builder's OnCompletion delegates to parent but uses full signature
        var definition = builder
            .Step("Step1")
            .Execute(data => Right<EncinaError, TestData>(data))
            .OnCompletion(async (data, ctx, ct) =>
            {
                await Task.Delay(1, ct);
            })
            .Build();

        // Assert
        definition.OnCompletion.ShouldNotBeNull();
    }

    [Fact]
    public void OnCompletion_SimplifiedOverloadOnParent_SetsHandler()
    {
        // Arrange - Test the simplified (data, ct) overload directly on parent builder
        var builder = RoutingSlipBuilder.Create<TestData>("Test");

        // Add steps - Step2 triggers Complete() on Step1, Build triggers Complete() on Step2
        var definition = builder
            .Step("Step1")
            .Execute(data => Right<EncinaError, TestData>(data))
            .Step("Step2")  // This triggers Complete() on Step1, adding it to parent
            .Execute(data => Right<EncinaError, TestData>(data))
            .Build();  // This triggers Complete() on Step2

        // Assert
        definition.Steps.Count.ShouldBe(2);
    }

    [Fact]
    public void OnCompletion_SimplifiedOverloadDirectlyOnParent_Works()
    {
        // Arrange - Test the simplified (data, ct) overload available on RoutingSlipBuilder
        var builder = RoutingSlipBuilder.Create<TestData>("Test");

        // Build first step through step chain that returns parent
        var builderAfterStep = builder
            .Step("Step1")
            .Execute(data => Right<EncinaError, TestData>(data))
            .WithTimeout(TimeSpan.FromMinutes(1));  // Returns parent builder

        // Now use simplified OnCompletion on parent
        var definition = builderAfterStep
            .OnCompletion(async (data, ct) =>
            {
                await Task.Delay(1, ct);
            })
            .Build();

        // Assert
        definition.OnCompletion.ShouldNotBeNull();
        definition.Steps.Count.ShouldBe(1);
    }

    [Fact]
    public void OnCompletion_WithNullAsyncHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("Test");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            builder.OnCompletion((Func<TestData, RoutingSlipContext<TestData>, CancellationToken, Task>)null!));
    }

    [Fact]
    public void OnCompletion_WithNullSimplifiedHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("Test");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            builder.OnCompletion((Func<TestData, CancellationToken, Task>)null!));
    }

    #endregion

    #region RoutingSlipBuilder<TData> WithTimeout Tests

    [Fact]
    public void WithTimeout_WithValidTimeout_SetsTimeout()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("Test");
        var timeout = TimeSpan.FromMinutes(5);

        // Act
        var definition = builder
            .Step("Step1").Execute(data => Right<EncinaError, TestData>(data))
            .WithTimeout(timeout)
            .Build();

        // Assert
        definition.Timeout.ShouldBe(timeout);
    }

    [Fact]
    public void WithTimeout_WithZeroTimeout_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("Test");

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => builder.WithTimeout(TimeSpan.Zero));
    }

    [Fact]
    public void WithTimeout_WithNegativeTimeout_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("Test");

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => builder.WithTimeout(TimeSpan.FromMinutes(-1)));
    }

    #endregion

    #region RoutingSlipBuilder<TData> Build Tests

    [Fact]
    public void Build_WithNoSteps_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("Test");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.Build())
            .Message.ShouldContain("At least one step");
    }

    [Fact]
    public void Build_WithSteps_ReturnsDefinition()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("Test");

        // Act
        var definition = builder
            .Step("Step1").Execute(data => Right<EncinaError, TestData>(data))
            .Step("Step2").Execute(data => Right<EncinaError, TestData>(data))
            .Build();

        // Assert
        definition.ShouldNotBeNull();
        definition.SlipType.ShouldBe("Test");
        definition.Steps.Count.ShouldBe(2);
        definition.InitialStepCount.ShouldBe(2);
    }

    [Fact]
    public void Build_WithoutOnCompletion_HasNullOnCompletion()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("Test");

        // Act
        var definition = builder
            .Step("Step1").Execute(data => Right<EncinaError, TestData>(data))
            .Build();

        // Assert
        definition.OnCompletion.ShouldBeNull();
    }

    [Fact]
    public void Build_WithoutTimeout_HasNullTimeout()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("Test");

        // Act
        var definition = builder
            .Step("Step1").Execute(data => Right<EncinaError, TestData>(data))
            .Build();

        // Assert
        definition.Timeout.ShouldBeNull();
    }

    #endregion

    #region RoutingSlipStepBuilder Tests

    [Fact]
    public void Execute_WithFullAsyncHandler_SetsHandler()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("Test");

        // Act
        var definition = builder
            .Step("Step1")
            .Execute(async (data, ctx, ct) =>
            {
                await Task.Delay(1, ct);
                return Right<EncinaError, TestData>(data);
            })
            .Build();

        // Assert
        definition.Steps[0].Execute.ShouldNotBeNull();
    }

    [Fact]
    public void Execute_WithSimplifiedAsyncHandler_SetsHandler()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("Test");

        // Act
        var definition = builder
            .Step("Step1")
            .Execute(async (data, ct) =>
            {
                await Task.Delay(1, ct);
                return Right<EncinaError, TestData>(data);
            })
            .Build();

        // Assert
        definition.Steps[0].Execute.ShouldNotBeNull();
    }

    [Fact]
    public void Execute_WithSyncHandler_SetsHandler()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("Test");

        // Act
        var definition = builder
            .Step("Step1")
            .Execute(data => Right<EncinaError, TestData>(data))
            .Build();

        // Assert
        definition.Steps[0].Execute.ShouldNotBeNull();
    }

    [Fact]
    public void Execute_WithNullFullAsyncHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("Test");
        var stepBuilder = builder.Step("Step1");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            stepBuilder.Execute((Func<TestData, RoutingSlipContext<TestData>, CancellationToken, ValueTask<Either<EncinaError, TestData>>>)null!));
    }

    [Fact]
    public void Execute_WithNullSimplifiedAsyncHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("Test");
        var stepBuilder = builder.Step("Step1");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            stepBuilder.Execute((Func<TestData, CancellationToken, ValueTask<Either<EncinaError, TestData>>>)null!));
    }

    [Fact]
    public void Execute_WithNullSyncHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("Test");
        var stepBuilder = builder.Step("Step1");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            stepBuilder.Execute((Func<TestData, Either<EncinaError, TestData>>)null!));
    }

    [Fact]
    public void Compensate_WithFullAsyncHandler_SetsHandler()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("Test");

        // Act
        var definition = builder
            .Step("Step1")
            .Execute(data => Right<EncinaError, TestData>(data))
            .Compensate(async (data, ctx, ct) =>
            {
                await Task.Delay(1, ct);
            })
            .Build();

        // Assert
        definition.Steps[0].Compensate.ShouldNotBeNull();
    }

    [Fact]
    public void Compensate_WithSimplifiedAsyncHandler_SetsHandler()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("Test");

        // Act
        var definition = builder
            .Step("Step1")
            .Execute(data => Right<EncinaError, TestData>(data))
            .Compensate(async (data, ct) =>
            {
                await Task.Delay(1, ct);
            })
            .Build();

        // Assert
        definition.Steps[0].Compensate.ShouldNotBeNull();
    }

    [Fact]
    public void Compensate_WithSyncAction_SetsHandler()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("Test");

        // Act
        var definition = builder
            .Step("Step1")
            .Execute(data => Right<EncinaError, TestData>(data))
            .Compensate(data => { /* compensation */ })
            .Build();

        // Assert
        definition.Steps[0].Compensate.ShouldNotBeNull();
    }

    [Fact]
    public void Compensate_WithNullFullAsyncHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("Test");
        var stepBuilder = builder.Step("Step1");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            stepBuilder.Compensate((Func<TestData, RoutingSlipContext<TestData>, CancellationToken, Task>)null!));
    }

    [Fact]
    public void Compensate_WithNullSimplifiedAsyncHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("Test");
        var stepBuilder = builder.Step("Step1");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            stepBuilder.Compensate((Func<TestData, CancellationToken, Task>)null!));
    }

    [Fact]
    public void Compensate_WithNullSyncAction_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("Test");
        var stepBuilder = builder.Step("Step1");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            stepBuilder.Compensate((Action<TestData>)null!));
    }

    [Fact]
    public void WithMetadata_AddsMetadataToStep()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("Test");

        // Act
        var definition = builder
            .Step("Step1")
            .Execute(data => Right<EncinaError, TestData>(data))
            .WithMetadata("key1", "value1")
            .WithMetadata("key2", 42)
            .Build();

        // Assert
        var step = definition.Steps[0];
        step.Metadata.ShouldNotBeNull();
        step.Metadata["key1"].ShouldBe("value1");
        step.Metadata["key2"].ShouldBe(42);
    }

    [Fact]
    public void WithMetadata_NullKey_ThrowsArgumentException()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("Test");
        var stepBuilder = builder.Step("Step1");

        // Act & Assert
        Should.Throw<ArgumentException>(() => stepBuilder.WithMetadata(null!, "value"));
    }

    [Fact]
    public void WithMetadata_EmptyKey_ThrowsArgumentException()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("Test");
        var stepBuilder = builder.Step("Step1");

        // Act & Assert
        Should.Throw<ArgumentException>(() => stepBuilder.WithMetadata(string.Empty, "value"));
    }

    [Fact]
    public void WithMetadata_WhitespaceKey_ThrowsArgumentException()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("Test");
        var stepBuilder = builder.Step("Step1");

        // Act & Assert
        Should.Throw<ArgumentException>(() => stepBuilder.WithMetadata("  ", "value"));
    }

    [Fact]
    public void Step_FromStepBuilder_AddsCurrentStepAndStartsNew()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("Test");

        // Act
        var definition = builder
            .Step("Step1")
            .Execute(data => Right<EncinaError, TestData>(data))
            .Step("Step2")
            .Execute(data => Right<EncinaError, TestData>(data))
            .Build();

        // Assert
        definition.Steps.Count.ShouldBe(2);
        definition.Steps[0].Name.ShouldBe("Step1");
        definition.Steps[1].Name.ShouldBe("Step2");
    }

    [Fact]
    public void OnCompletion_FromStepBuilder_AddsStepAndSetsCompletion()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("Test");

        // Act
        var definition = builder
            .Step("Step1")
            .Execute(data => Right<EncinaError, TestData>(data))
            .OnCompletion(async (data, ctx, ct) => await Task.Delay(1, ct))
            .Build();

        // Assert
        definition.Steps.Count.ShouldBe(1);
        definition.OnCompletion.ShouldNotBeNull();
    }

    [Fact]
    public void WithTimeout_FromStepBuilder_AddsStepAndSetsTimeout()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("Test");
        var timeout = TimeSpan.FromMinutes(10);

        // Act
        var definition = builder
            .Step("Step1")
            .Execute(data => Right<EncinaError, TestData>(data))
            .WithTimeout(timeout)
            .Build();

        // Assert
        definition.Steps.Count.ShouldBe(1);
        definition.Timeout.ShouldBe(timeout);
    }

    [Fact]
    public void Build_FromStepBuilder_WithoutExecute_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("Test");
        var stepBuilder = builder.Step("Step1");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => stepBuilder.Build())
            .Message.ShouldContain("must have an Execute function");
    }

    #endregion

    #region BuiltRoutingSlipDefinition Tests

    [Fact]
    public void BuiltRoutingSlipDefinition_Properties_AreCorrect()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("OrderProcessing");
        var timeout = TimeSpan.FromMinutes(15);

        // Act
        var definition = builder
            .Step("Validate").Execute(data => Right<EncinaError, TestData>(data))
            .Step("Process").Execute(data => Right<EncinaError, TestData>(data))
            .OnCompletion(async (d, c, ct) => await Task.Delay(1, ct))
            .WithTimeout(timeout)
            .Build();

        // Assert
        definition.SlipType.ShouldBe("OrderProcessing");
        definition.Steps.Count.ShouldBe(2);
        definition.InitialStepCount.ShouldBe(2);
        definition.Steps[0].Name.ShouldBe("Validate");
        definition.Steps[1].Name.ShouldBe("Process");
        definition.OnCompletion.ShouldNotBeNull();
        definition.Timeout.ShouldBe(timeout);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void CompleteFluentChain_WithAllOptions_Succeeds()
    {
        // Arrange & Act
        var definition = RoutingSlipBuilder.Create<TestData>("FullTest")
            .Step("Step1")
                .Execute(async (data, ctx, ct) =>
                {
                    await Task.Delay(1, ct);
                    return Right<EncinaError, TestData>(data with { Value = data.Value + 1 });
                })
                .Compensate(async (data, ctx, ct) => await Task.Delay(1, ct))
                .WithMetadata("description", "First step")
            .Step("Step2")
                .Execute(data => Right<EncinaError, TestData>(data with { Value = data.Value + 10 }))
                .Compensate(data => { /* sync compensation */ })
                .WithMetadata("retryable", true)
            .Step("Step3")
                .Execute(async (data, ct) =>
                {
                    await Task.Delay(1, ct);
                    return Right<EncinaError, TestData>(data);
                })
            .OnCompletion(async (data, ctx, ct) => await Task.Delay(1, ct))
            .WithTimeout(TimeSpan.FromHours(1))
            .Build();

        // Assert
        definition.SlipType.ShouldBe("FullTest");
        definition.Steps.Count.ShouldBe(3);
        definition.InitialStepCount.ShouldBe(3);
        definition.OnCompletion.ShouldNotBeNull();
        definition.Timeout.ShouldBe(TimeSpan.FromHours(1));

        // Verify step metadata
        definition.Steps[0].Metadata.ShouldNotBeNull();
        definition.Steps[0].Metadata["description"].ShouldBe("First step");
        definition.Steps[1].Metadata.ShouldNotBeNull();
        definition.Steps[1].Metadata["retryable"].ShouldBe(true);
        definition.Steps[2].Metadata.Count.ShouldBe(0);

        // Verify step names
        definition.Steps[0].Name.ShouldBe("Step1");
        definition.Steps[1].Name.ShouldBe("Step2");
        definition.Steps[2].Name.ShouldBe("Step3");

        // Verify compensations
        definition.Steps[0].Compensate.ShouldNotBeNull();
        definition.Steps[1].Compensate.ShouldNotBeNull();
        definition.Steps[2].Compensate.ShouldBeNull();
    }

    #endregion

    private sealed record TestData
    {
        public int Value { get; init; }
    }
}

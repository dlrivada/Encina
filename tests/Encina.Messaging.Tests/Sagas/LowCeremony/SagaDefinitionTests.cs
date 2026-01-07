using Encina.Messaging.Sagas.LowCeremony;
using LanguageExt;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.Messaging.Tests.Sagas.LowCeremony;

/// <summary>
/// Unit tests for <see cref="SagaDefinition"/> and related types.
/// </summary>
public sealed class SagaDefinitionTests
{
    #region SagaDefinition Static Factory Tests

    [Fact]
    public void Create_WithValidSagaType_ReturnsBuilder()
    {
        // Act
        var definition = SagaDefinition.Create<TestSagaData>("OrderProcessing");

        // Assert
        definition.ShouldNotBeNull();
        definition.ShouldBeOfType<SagaDefinition<TestSagaData>>();
        definition.SagaType.ShouldBe("OrderProcessing");
    }

    [Fact]
    public void Create_WithNullSagaType_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => SagaDefinition.Create<TestSagaData>(null!));
    }

    [Fact]
    public void Create_WithEmptySagaType_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => SagaDefinition.Create<TestSagaData>(string.Empty));
    }

    [Fact]
    public void Create_WithWhitespaceSagaType_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => SagaDefinition.Create<TestSagaData>("   "));
    }

    #endregion

    #region SagaDefinition<TData> Step Tests

    [Fact]
    public void Step_WithName_ReturnsStepBuilder()
    {
        // Arrange
        var definition = SagaDefinition.Create<TestSagaData>("Test");

        // Act
        var stepBuilder = definition.Step("ReserveInventory");

        // Assert
        stepBuilder.ShouldNotBeNull();
        stepBuilder.ShouldBeOfType<SagaStepBuilder<TestSagaData>>();
    }

    [Fact]
    public void Step_WithoutName_GeneratesName()
    {
        // Arrange
        var definition = SagaDefinition.Create<TestSagaData>("Test");

        // Act
        var built = definition
            .Step()
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(data)))
            .Build();

        // Assert
        built.Steps[0].Name.ShouldBe("Step 1");
    }

    [Fact]
    public void MultipleSteps_HaveSequentialDefaultNames()
    {
        // Arrange
        var definition = SagaDefinition.Create<TestSagaData>("Test");

        // Act
        var built = definition
            .Step().Execute((d, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(d)))
            .Step().Execute((d, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(d)))
            .Step().Execute((d, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(d)))
            .Build();

        // Assert
        built.Steps[0].Name.ShouldBe("Step 1");
        built.Steps[1].Name.ShouldBe("Step 2");
        built.Steps[2].Name.ShouldBe("Step 3");
    }

    #endregion

    #region SagaDefinition<TData> WithTimeout Tests

    [Fact]
    public void WithTimeout_WithValidTimeout_SetsTimeout()
    {
        // Arrange
        var definition = SagaDefinition.Create<TestSagaData>("Test");
        var timeout = TimeSpan.FromMinutes(5);

        // Act
        var built = definition
            .Step("Step1").Execute((d, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(d)))
            .WithTimeout(timeout)
            .Build();

        // Assert
        built.Timeout.ShouldBe(timeout);
    }

    [Fact]
    public void WithTimeout_WithZeroTimeout_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var definition = SagaDefinition.Create<TestSagaData>("Test");

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => definition.WithTimeout(TimeSpan.Zero));
    }

    [Fact]
    public void WithTimeout_WithNegativeTimeout_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var definition = SagaDefinition.Create<TestSagaData>("Test");

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => definition.WithTimeout(TimeSpan.FromSeconds(-10)));
    }

    #endregion

    #region SagaDefinition<TData> Build Tests

    [Fact]
    public void Build_WithNoSteps_ThrowsInvalidOperationException()
    {
        // Arrange
        var definition = SagaDefinition.Create<TestSagaData>("Test");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => definition.Build())
            .Message.ShouldContain("At least one step");
    }

    [Fact]
    public void Build_WithSteps_ReturnsBuiltDefinition()
    {
        // Arrange
        var definition = SagaDefinition.Create<TestSagaData>("Test");

        // Act
        var built = definition
            .Step("Step1").Execute((d, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(d)))
            .Step("Step2").Execute((d, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(d)))
            .Build();

        // Assert
        built.ShouldNotBeNull();
        built.SagaType.ShouldBe("Test");
        built.Steps.Count.ShouldBe(2);
        built.StepCount.ShouldBe(2);
    }

    [Fact]
    public void Build_WithoutTimeout_HasNullTimeout()
    {
        // Arrange
        var definition = SagaDefinition.Create<TestSagaData>("Test");

        // Act
        var built = definition
            .Step("Step1").Execute((d, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(d)))
            .Build();

        // Assert
        built.Timeout.ShouldBeNull();
    }

    #endregion

    #region SagaStepBuilder Tests

    [Fact]
    public void Execute_WithFullAsyncHandler_SetsHandler()
    {
        // Arrange
        var definition = SagaDefinition.Create<TestSagaData>("Test");

        // Act
        var built = definition
            .Step("Step1")
            .Execute(async (data, ctx, ct) =>
            {
                await Task.Delay(1, ct);
                return Right<EncinaError, TestSagaData>(data);
            })
            .Build();

        // Assert
        built.Steps[0].Execute.ShouldNotBeNull();
    }

    [Fact]
    public void Execute_WithSimplifiedAsyncHandler_SetsHandler()
    {
        // Arrange
        var definition = SagaDefinition.Create<TestSagaData>("Test");

        // Act
        var built = definition
            .Step("Step1")
            .Execute(async (data, ct) =>
            {
                await Task.Delay(1, ct);
                return Right<EncinaError, TestSagaData>(data);
            })
            .Build();

        // Assert
        built.Steps[0].Execute.ShouldNotBeNull();
    }

    [Fact]
    public void Execute_WithNullFullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var definition = SagaDefinition.Create<TestSagaData>("Test");
        var stepBuilder = definition.Step("Step1");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            stepBuilder.Execute((Func<TestSagaData, IRequestContext, CancellationToken, ValueTask<Either<EncinaError, TestSagaData>>>)null!));
    }

    [Fact]
    public void Execute_WithNullSimplifiedHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var definition = SagaDefinition.Create<TestSagaData>("Test");
        var stepBuilder = definition.Step("Step1");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            stepBuilder.Execute((Func<TestSagaData, CancellationToken, ValueTask<Either<EncinaError, TestSagaData>>>)null!));
    }

    [Fact]
    public void Compensate_WithFullAsyncHandler_SetsHandlerAndReturnsParent()
    {
        // Arrange
        var definition = SagaDefinition.Create<TestSagaData>("Test");

        // Act
        var built = definition
            .Step("Step1")
            .Execute((d, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(d)))
            .Compensate(async (data, ctx, ct) =>
            {
                await Task.Delay(1, ct);
            })
            .Build();

        // Assert
        built.Steps[0].Compensate.ShouldNotBeNull();
    }

    [Fact]
    public void Compensate_WithSimplifiedAsyncHandler_SetsHandlerAndReturnsParent()
    {
        // Arrange
        var definition = SagaDefinition.Create<TestSagaData>("Test");

        // Act
        var built = definition
            .Step("Step1")
            .Execute((d, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(d)))
            .Compensate(async (data, ct) =>
            {
                await Task.Delay(1, ct);
            })
            .Build();

        // Assert
        built.Steps[0].Compensate.ShouldNotBeNull();
    }

    [Fact]
    public void Compensate_WithNullFullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var definition = SagaDefinition.Create<TestSagaData>("Test");
        var stepBuilder = definition.Step("Step1")
            .Execute((d, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(d)));

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            stepBuilder.Compensate((Func<TestSagaData, IRequestContext, CancellationToken, Task>)null!));
    }

    [Fact]
    public void Compensate_WithNullSimplifiedHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var definition = SagaDefinition.Create<TestSagaData>("Test");
        var stepBuilder = definition.Step("Step1")
            .Execute((d, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(d)));

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            stepBuilder.Compensate((Func<TestSagaData, CancellationToken, Task>)null!));
    }

    [Fact]
    public void Step_FromStepBuilder_AddsCurrentStepAndStartsNew()
    {
        // Arrange
        var definition = SagaDefinition.Create<TestSagaData>("Test");

        // Act
        var built = definition
            .Step("Step1")
            .Execute((d, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(d)))
            .Step("Step2")
            .Execute((d, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(d)))
            .Build();

        // Assert
        built.Steps.Count.ShouldBe(2);
        built.Steps[0].Name.ShouldBe("Step1");
        built.Steps[1].Name.ShouldBe("Step2");
    }

    [Fact]
    public void WithTimeout_FromStepBuilder_AddsStepAndSetsTimeout()
    {
        // Arrange
        var definition = SagaDefinition.Create<TestSagaData>("Test");
        var timeout = TimeSpan.FromMinutes(10);

        // Act
        var built = definition
            .Step("Step1")
            .Execute((d, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(d)))
            .WithTimeout(timeout)
            .Build();

        // Assert
        built.Steps.Count.ShouldBe(1);
        built.Timeout.ShouldBe(timeout);
    }

    [Fact]
    public void Build_FromStepBuilder_WithoutExecute_ThrowsInvalidOperationException()
    {
        // Arrange
        var definition = SagaDefinition.Create<TestSagaData>("Test");
        var stepBuilder = definition.Step("Step1");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => stepBuilder.Build())
            .Message.ShouldContain("must have an Execute action");
    }

    [Fact]
    public void Build_FromStepBuilder_WithExecute_Succeeds()
    {
        // Arrange
        var definition = SagaDefinition.Create<TestSagaData>("Test");

        // Act
        var built = definition
            .Step("Step1")
            .Execute((d, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(d)))
            .Build();

        // Assert
        built.ShouldNotBeNull();
        built.Steps.Count.ShouldBe(1);
    }

    #endregion

    #region BuiltSagaDefinition Tests

    [Fact]
    public void BuiltSagaDefinition_Properties_AreCorrect()
    {
        // Arrange
        var timeout = TimeSpan.FromHours(1);

        // Act
        var built = SagaDefinition.Create<TestSagaData>("OrderSaga")
            .Step("Reserve").Execute((d, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(d)))
            .Step("Process").Execute((d, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(d)))
            .WithTimeout(timeout)
            .Build();

        // Assert
        built.SagaType.ShouldBe("OrderSaga");
        built.Steps.Count.ShouldBe(2);
        built.StepCount.ShouldBe(2);
        built.Timeout.ShouldBe(timeout);
        built.Steps[0].Name.ShouldBe("Reserve");
        built.Steps[1].Name.ShouldBe("Process");
    }

    #endregion

    #region SagaStepDefinition Tests

    [Fact]
    public void SagaStepDefinition_WithCompensation_HasCompensate()
    {
        // Arrange & Act
        var built = SagaDefinition.Create<TestSagaData>("Test")
            .Step("Step1")
            .Execute((d, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(d)))
            .Compensate(async (d, ct) => await Task.Delay(1, ct))
            .Build();

        // Assert
        built.Steps[0].Compensate.ShouldNotBeNull();
    }

    [Fact]
    public void SagaStepDefinition_WithoutCompensation_HasNullCompensate()
    {
        // Arrange & Act
        var built = SagaDefinition.Create<TestSagaData>("Test")
            .Step("Step1")
            .Execute((d, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(d)))
            .Build();

        // Assert
        built.Steps[0].Compensate.ShouldBeNull();
    }

    #endregion

    #region Handler Execution Tests

    [Fact]
    public async Task Execute_FullHandler_ReceivesContextAndToken()
    {
        // Arrange
        var context = Substitute.For<IRequestContext>();
        var data = new TestSagaData { OrderId = 123 };
        var executedWithContext = false;

        var built = SagaDefinition.Create<TestSagaData>("Test")
            .Step("Step1")
            .Execute(async (d, ctx, ct) =>
            {
                executedWithContext = ctx == context;
                await Task.Delay(1, ct);
                return Right<EncinaError, TestSagaData>(d with { OrderId = d.OrderId + 1 });
            })
            .Build();

        // Act
        var result = await built.Steps[0].Execute(data, context, CancellationToken.None);

        // Assert
        executedWithContext.ShouldBeTrue();
        result.Match(
            Right: d => d.OrderId.ShouldBe(124),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task Execute_SimplifiedHandler_WorksWithoutContext()
    {
        // Arrange
        var context = Substitute.For<IRequestContext>();
        var data = new TestSagaData { OrderId = 100 };

        var built = SagaDefinition.Create<TestSagaData>("Test")
            .Step("Step1")
            .Execute(async (d, ct) =>
            {
                await Task.Delay(1, ct);
                return Right<EncinaError, TestSagaData>(d with { OrderId = d.OrderId * 2 });
            })
            .Build();

        // Act
        var result = await built.Steps[0].Execute(data, context, CancellationToken.None);

        // Assert
        result.Match(
            Right: d => d.OrderId.ShouldBe(200),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task Compensate_FullHandler_ReceivesContextAndToken()
    {
        // Arrange
        var context = Substitute.For<IRequestContext>();
        var data = new TestSagaData { OrderId = 123 };
        var compensatedWithContext = false;

        var built = SagaDefinition.Create<TestSagaData>("Test")
            .Step("Step1")
            .Execute((d, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(d)))
            .Compensate(async (d, ctx, ct) =>
            {
                compensatedWithContext = ctx == context;
                await Task.Delay(1, ct);
            })
            .Build();

        // Act
        await built.Steps[0].Compensate!(data, context, CancellationToken.None);

        // Assert
        compensatedWithContext.ShouldBeTrue();
    }

    [Fact]
    public async Task Compensate_SimplifiedHandler_WorksWithoutContext()
    {
        // Arrange
        var context = Substitute.For<IRequestContext>();
        var data = new TestSagaData { OrderId = 123 };
        var compensated = false;

        var built = SagaDefinition.Create<TestSagaData>("Test")
            .Step("Step1")
            .Execute((d, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(d)))
            .Compensate(async (d, ct) =>
            {
                compensated = true;
                await Task.Delay(1, ct);
            })
            .Build();

        // Act
        await built.Steps[0].Compensate!(data, context, CancellationToken.None);

        // Assert
        compensated.ShouldBeTrue();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void CompleteFluentChain_WithAllOptions_Succeeds()
    {
        // Arrange & Act
        var built = SagaDefinition.Create<TestSagaData>("FullOrderSaga")
            .Step("ReserveInventory")
                .Execute(async (data, ctx, ct) =>
                {
                    await Task.Delay(1, ct);
                    return Right<EncinaError, TestSagaData>(data with { ReservationId = Guid.NewGuid() });
                })
                .Compensate(async (data, ctx, ct) =>
                {
                    await Task.Delay(1, ct);
                })
            .Step("ProcessPayment")
                .Execute(async (data, ct) =>
                {
                    await Task.Delay(1, ct);
                    return Right<EncinaError, TestSagaData>(data with { PaymentId = Guid.NewGuid() });
                })
                .Compensate(async (data, ct) =>
                {
                    await Task.Delay(1, ct);
                })
            .Step("ShipOrder")
                .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(data with { ShippingId = Guid.NewGuid() })))
            .WithTimeout(TimeSpan.FromMinutes(30))
            .Build();

        // Assert
        built.SagaType.ShouldBe("FullOrderSaga");
        built.Steps.Count.ShouldBe(3);
        built.StepCount.ShouldBe(3);
        built.Timeout.ShouldBe(TimeSpan.FromMinutes(30));

        // Verify step names
        built.Steps[0].Name.ShouldBe("ReserveInventory");
        built.Steps[1].Name.ShouldBe("ProcessPayment");
        built.Steps[2].Name.ShouldBe("ShipOrder");

        // Verify compensation
        built.Steps[0].Compensate.ShouldNotBeNull();
        built.Steps[1].Compensate.ShouldNotBeNull();
        built.Steps[2].Compensate.ShouldBeNull();
    }

    #endregion

    private sealed record TestSagaData
    {
        public int OrderId { get; init; }
        public Guid? ReservationId { get; init; }
        public Guid? PaymentId { get; init; }
        public Guid? ShippingId { get; init; }
    }
}

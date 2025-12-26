using Encina.Messaging.RoutingSlip;
using LanguageExt;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.Tests.RoutingSlip;

public sealed class RoutingSlipStepBuilderTests
{
    [Fact]
    public void Execute_WithFullContextSignature_SetsExecuteFunction()
    {
        // Arrange & Act
        var definition = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step1")
            .Execute((data, ctx, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)))
            .Build();

        // Assert
        definition.Steps[0].Execute.ShouldNotBeNull();
    }

    [Fact]
    public void Execute_WithSimplifiedSignature_SetsExecuteFunction()
    {
        // Arrange & Act
        var definition = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step1")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)))
            .Build();

        // Assert
        definition.Steps[0].Execute.ShouldNotBeNull();
    }

    [Fact]
    public void Execute_WithSynchronousSignature_SetsExecuteFunction()
    {
        // Arrange & Act
        var definition = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step1")
            .Execute(data => Right<EncinaError, TestData>(data))
            .Build();

        // Assert
        definition.Steps[0].Execute.ShouldNotBeNull();
    }

    [Fact]
    public void Execute_WithNullFunction_ThrowsArgumentNullException()
    {
        // Arrange
        var stepBuilder = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step1");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            stepBuilder.Execute((Func<TestData, RoutingSlipContext<TestData>, CancellationToken, ValueTask<Either<EncinaError, TestData>>>)null!));
    }

    [Fact]
    public void Compensate_WithFullContextSignature_SetsCompensateFunction()
    {
        // Arrange & Act
        var definition = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step1")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)))
            .Compensate((data, ctx, ct) => Task.CompletedTask)
            .Build();

        // Assert
        definition.Steps[0].Compensate.ShouldNotBeNull();
    }

    [Fact]
    public void Compensate_WithSimplifiedSignature_SetsCompensateFunction()
    {
        // Arrange & Act
        var definition = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step1")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)))
            .Compensate((data, ct) => Task.CompletedTask)
            .Build();

        // Assert
        definition.Steps[0].Compensate.ShouldNotBeNull();
    }

    [Fact]
    public void Compensate_WithActionSignature_SetsCompensateFunction()
    {
        // Arrange & Act
        var definition = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step1")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)))
            .Compensate(data => { /* synchronous compensation */ })
            .Build();

        // Assert
        definition.Steps[0].Compensate.ShouldNotBeNull();
    }

    [Fact]
    public void Compensate_WithNullFunction_ThrowsArgumentNullException()
    {
        // Arrange
        var stepBuilder = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step1")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)));

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            stepBuilder.Compensate((Func<TestData, RoutingSlipContext<TestData>, CancellationToken, Task>)null!));
    }

    [Fact]
    public void WithMetadata_AddsMetadataToStep()
    {
        // Arrange & Act
        var definition = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step1")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)))
            .WithMetadata("key1", "value1")
            .WithMetadata("key2", 42)
            .Build();

        // Assert
        definition.Steps[0].Metadata.ShouldContainKey("key1");
        definition.Steps[0].Metadata["key1"].ShouldBe("value1");
        definition.Steps[0].Metadata.ShouldContainKey("key2");
        definition.Steps[0].Metadata["key2"].ShouldBe(42);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WithMetadata_WithInvalidKey_ThrowsArgumentException(string? key)
    {
        // Arrange
        var stepBuilder = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step1")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)));

        // Act & Assert
        Should.Throw<ArgumentException>(() => stepBuilder.WithMetadata(key!, "value"));
    }

    [Fact]
    public void Step_FromStepBuilder_ChainsToNextStep()
    {
        // Arrange & Act
        var definition = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step1")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)))
            .Step("Step2")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)))
            .Build();

        // Assert
        definition.Steps.Count.ShouldBe(2);
        definition.Steps[0].Name.ShouldBe("Step1");
        definition.Steps[1].Name.ShouldBe("Step2");
    }

    [Fact]
    public void Build_FromStepBuilder_ReturnsDefinition()
    {
        // Arrange & Act
        var definition = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step1")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)))
            .Build();

        // Assert
        definition.ShouldNotBeNull();
    }

    [Fact]
    public void Build_WithoutExecute_ThrowsInvalidOperationException()
    {
        // Arrange
        var stepBuilder = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step1");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => stepBuilder.Build());
    }

    [Fact]
    public void WithTimeout_FromStepBuilder_SetsTimeout()
    {
        // Arrange & Act
        var definition = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step1")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)))
            .WithTimeout(TimeSpan.FromMinutes(10))
            .Build();

        // Assert
        definition.Timeout.ShouldBe(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public void OnCompletion_FromStepBuilder_SetsCompletionHandler()
    {
        // Arrange & Act
        var definition = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step1")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)))
            .OnCompletion((data, ctx, ct) => Task.CompletedTask)
            .Build();

        // Assert
        definition.OnCompletion.ShouldNotBeNull();
    }

    private sealed class TestData
    {
        public Guid Id { get; set; }
        public string? Value { get; set; }
    }
}

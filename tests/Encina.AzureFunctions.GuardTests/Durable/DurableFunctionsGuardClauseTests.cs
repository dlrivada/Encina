using Encina.AzureFunctions.Durable;
using Shouldly;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Encina.AzureFunctions.GuardTests.Durable;

/// <summary>
/// Tests for guard clauses in Durable Functions APIs.
/// </summary>
public sealed class DurableFunctionsGuardClauseTests
{
    #region DurableServiceCollectionExtensions

    [Fact]
    public void AddEncinaDurableFunctions_ThrowsOnNullServices()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var action = () => services.AddEncinaDurableFunctions();

        // Assert
        var ex = Should.Throw<ArgumentNullException>(action);
        ex.ParamName.ShouldBe("services");
    }

    #endregion

    #region DurableFunctionsHealthCheck

    [Fact]
    public void DurableFunctionsHealthCheck_Constructor_ThrowsOnNullOptions()
    {
        // Act
        var action = () => new DurableFunctionsHealthCheck(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(action);
        ex.ParamName.ShouldBe("options");
    }

    #endregion

    #region DurableSagaBuilder

    [Fact]
    public void DurableSagaBuilder_Step_ThrowsOnNullStepName()
    {
        // Arrange
        var builder = DurableSagaBuilder.Create<TestSagaData>();

        // Act
        var action = () => builder.Step(null!);

        // Assert
        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("stepName");
    }

    [Fact]
    public void DurableSagaBuilder_Step_ThrowsOnEmptyStepName()
    {
        // Arrange
        var builder = DurableSagaBuilder.Create<TestSagaData>();

        // Act
        var action = () => builder.Step(string.Empty);

        // Assert
        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("stepName");
    }

    [Fact]
    public void DurableSagaStepBuilder_Execute_ThrowsOnNullActivityName()
    {
        // Arrange
        var stepBuilder = DurableSagaBuilder.Create<TestSagaData>().Step("TestStep");

        // Act
        var action = () => stepBuilder.Execute(null!);

        // Assert
        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("activityName");
    }

    [Fact]
    public void DurableSagaStepBuilder_Execute_ThrowsOnEmptyActivityName()
    {
        // Arrange
        var stepBuilder = DurableSagaBuilder.Create<TestSagaData>().Step("TestStep");

        // Act
        var action = () => stepBuilder.Execute(string.Empty);

        // Assert
        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("activityName");
    }

    [Fact]
    public void DurableSagaStepBuilder_Compensate_ThrowsOnNullActivityName()
    {
        // Arrange
        var stepBuilder = DurableSagaBuilder.Create<TestSagaData>()
            .Step("TestStep")
            .Execute("TestActivity");

        // Act
        var action = () => stepBuilder.Compensate(null!);

        // Assert
        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("activityName");
    }

    [Fact]
    public void DurableSagaStepBuilder_Compensate_ThrowsOnEmptyActivityName()
    {
        // Arrange
        var stepBuilder = DurableSagaBuilder.Create<TestSagaData>()
            .Step("TestStep")
            .Execute("TestActivity");

        // Act
        var action = () => stepBuilder.Compensate(string.Empty);

        // Assert
        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("activityName");
    }

    [Fact]
    public void DurableSagaBuilder_WithTimeout_ThrowsOnZeroTimeout()
    {
        // Arrange
        var builder = DurableSagaBuilder.Create<TestSagaData>();

        // Act
        var action = () => builder.WithTimeout(TimeSpan.Zero);

        // Assert
        Should.Throw<ArgumentOutOfRangeException>(action).ParamName.ShouldBe("timeout");
    }

    [Fact]
    public void DurableSagaBuilder_WithTimeout_ThrowsOnNegativeTimeout()
    {
        // Arrange
        var builder = DurableSagaBuilder.Create<TestSagaData>();

        // Act
        var action = () => builder.WithTimeout(TimeSpan.FromSeconds(-1));

        // Assert
        Should.Throw<ArgumentOutOfRangeException>(action).ParamName.ShouldBe("timeout");
    }

    [Fact]
    public void DurableSagaBuilder_Build_ThrowsWhenNoSteps()
    {
        // Arrange
        var builder = DurableSagaBuilder.Create<TestSagaData>();

        // Act
        var action = () => builder.Build();

        // Assert
        var ex = Should.Throw<InvalidOperationException>(action);
        ex.Message.ShouldContain("at least one step");
    }

    [Fact]
    public void DurableSagaStepBuilder_Build_ThrowsWhenNoExecute()
    {
        // Arrange
        var stepBuilder = DurableSagaBuilder.Create<TestSagaData>()
            .Step("TestStep");

        // Act
        var action = () => stepBuilder.Build();

        // Assert
        var ex = Should.Throw<InvalidOperationException>(action);
        ex.Message.ShouldContain("must have an Execute");
    }

    #endregion

    private sealed record TestSagaData
    {
        public string Value { get; init; } = string.Empty;
    }
}

using Encina.Messaging.RoutingSlip;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.Tests.Guards;

/// <summary>
/// Guard clause tests for Routing Slip pattern classes.
/// Tests ensure proper null validation and argument checking.
/// </summary>
public sealed class RoutingSlipGuardsTests
{
    #region RoutingSlipBuilder Guards

    [Fact]
    public void RoutingSlipBuilder_Create_WithNullSlipType_ThrowsArgumentException()
    {
        // Act & Assert
        FluentActions.Invoking(() => RoutingSlipBuilder.Create<TestData>(null!))
            .Should().Throw<ArgumentException>()
            .WithParameterName("slipType");
    }

    [Fact]
    public void RoutingSlipBuilder_Create_WithEmptySlipType_ThrowsArgumentException()
    {
        // Act & Assert
        FluentActions.Invoking(() => RoutingSlipBuilder.Create<TestData>(""))
            .Should().Throw<ArgumentException>()
            .WithParameterName("slipType");
    }

    [Fact]
    public void RoutingSlipBuilder_Create_WithWhitespaceSlipType_ThrowsArgumentException()
    {
        // Act & Assert
        FluentActions.Invoking(() => RoutingSlipBuilder.Create<TestData>("   "))
            .Should().Throw<ArgumentException>()
            .WithParameterName("slipType");
    }

    [Fact]
    public void RoutingSlipBuilder_OnCompletion_WithNullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("TestSlip");

        // Act & Assert
        FluentActions.Invoking(() =>
            builder.OnCompletion((Func<TestData, RoutingSlipContext<TestData>, CancellationToken, Task>)null!))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("onCompletion");
    }

    [Fact]
    public void RoutingSlipBuilder_OnCompletion_Simplified_WithNullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("TestSlip");

        // Act & Assert
        FluentActions.Invoking(() =>
            builder.OnCompletion((Func<TestData, CancellationToken, Task>)null!))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("onCompletion");
    }

    [Fact]
    public void RoutingSlipBuilder_WithTimeout_WithZero_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("TestSlip");

        // Act & Assert
        FluentActions.Invoking(() => builder.WithTimeout(TimeSpan.Zero))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("timeout");
    }

    [Fact]
    public void RoutingSlipBuilder_WithTimeout_WithNegative_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("TestSlip");

        // Act & Assert
        FluentActions.Invoking(() => builder.WithTimeout(TimeSpan.FromMinutes(-1)))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("timeout");
    }

    [Fact]
    public void RoutingSlipBuilder_Build_WithNoSteps_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("TestSlip");

        // Act & Assert
        FluentActions.Invoking(() => builder.Build())
            .Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region RoutingSlipStepBuilder Guards

    [Fact]
    public void RoutingSlipStepBuilder_Execute_WithNullFunction_ThrowsArgumentNullException()
    {
        // Arrange
        var stepBuilder = RoutingSlipBuilder.Create<TestData>("TestSlip").Step("Step1");

        // Act & Assert
        FluentActions.Invoking(() =>
            stepBuilder.Execute((Func<TestData, RoutingSlipContext<TestData>, CancellationToken, ValueTask<Either<EncinaError, TestData>>>)null!))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("execute");
    }

    [Fact]
    public void RoutingSlipStepBuilder_Execute_Simplified_WithNullFunction_ThrowsArgumentNullException()
    {
        // Arrange
        var stepBuilder = RoutingSlipBuilder.Create<TestData>("TestSlip").Step("Step1");

        // Act & Assert
        FluentActions.Invoking(() =>
            stepBuilder.Execute((Func<TestData, CancellationToken, ValueTask<Either<EncinaError, TestData>>>)null!))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("execute");
    }

    [Fact]
    public void RoutingSlipStepBuilder_Execute_Synchronous_WithNullFunction_ThrowsArgumentNullException()
    {
        // Arrange
        var stepBuilder = RoutingSlipBuilder.Create<TestData>("TestSlip").Step("Step1");

        // Act & Assert
        FluentActions.Invoking(() =>
            stepBuilder.Execute((Func<TestData, Either<EncinaError, TestData>>)null!))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("execute");
    }

    [Fact]
    public void RoutingSlipStepBuilder_Compensate_WithNullFunction_ThrowsArgumentNullException()
    {
        // Arrange
        var stepBuilder = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step1")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)));

        // Act & Assert
        FluentActions.Invoking(() =>
            stepBuilder.Compensate((Func<TestData, RoutingSlipContext<TestData>, CancellationToken, Task>)null!))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("compensate");
    }

    [Fact]
    public void RoutingSlipStepBuilder_Compensate_Simplified_WithNullFunction_ThrowsArgumentNullException()
    {
        // Arrange
        var stepBuilder = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step1")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)));

        // Act & Assert
        FluentActions.Invoking(() =>
            stepBuilder.Compensate((Func<TestData, CancellationToken, Task>)null!))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("compensate");
    }

    [Fact]
    public void RoutingSlipStepBuilder_Compensate_Action_WithNullFunction_ThrowsArgumentNullException()
    {
        // Arrange
        var stepBuilder = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step1")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)));

        // Act & Assert
        FluentActions.Invoking(() =>
            stepBuilder.Compensate((Action<TestData>)null!))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("compensate");
    }

    [Fact]
    public void RoutingSlipStepBuilder_WithMetadata_WithNullKey_ThrowsArgumentException()
    {
        // Arrange
        var stepBuilder = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step1")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)));

        // Act & Assert
        FluentActions.Invoking(() => stepBuilder.WithMetadata(null!, "value"))
            .Should().Throw<ArgumentException>()
            .WithParameterName("key");
    }

    [Fact]
    public void RoutingSlipStepBuilder_WithMetadata_WithEmptyKey_ThrowsArgumentException()
    {
        // Arrange
        var stepBuilder = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step1")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)));

        // Act & Assert
        FluentActions.Invoking(() => stepBuilder.WithMetadata("", "value"))
            .Should().Throw<ArgumentException>()
            .WithParameterName("key");
    }

    [Fact]
    public void RoutingSlipStepBuilder_Build_WithoutExecute_ThrowsInvalidOperationException()
    {
        // Arrange
        var stepBuilder = RoutingSlipBuilder.Create<TestData>("TestSlip").Step("Step1");

        // Act & Assert
        FluentActions.Invoking(() => stepBuilder.Build())
            .Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region RoutingSlipRunner Guards

    [Fact]
    public void RoutingSlipRunner_Constructor_WithNullRequestContext_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new RoutingSlipOptions();
        var logger = Substitute.For<ILogger<RoutingSlipRunner>>();

        // Act & Assert
        FluentActions.Invoking(() => new RoutingSlipRunner(null!, options, logger))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("requestContext");
    }

    [Fact]
    public void RoutingSlipRunner_Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var requestContext = RequestContext.Create();
        var logger = Substitute.For<ILogger<RoutingSlipRunner>>();

        // Act & Assert
        FluentActions.Invoking(() => new RoutingSlipRunner(requestContext, null!, logger))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void RoutingSlipRunner_Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var requestContext = RequestContext.Create();
        var options = new RoutingSlipOptions();

        // Act & Assert
        FluentActions.Invoking(() => new RoutingSlipRunner(requestContext, options, null!))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task RoutingSlipRunner_RunAsync_WithNullDefinition_ThrowsArgumentNullException()
    {
        // Arrange
        var requestContext = RequestContext.Create();
        var options = new RoutingSlipOptions();
        var logger = Substitute.For<ILogger<RoutingSlipRunner>>();
        var runner = new RoutingSlipRunner(requestContext, options, logger);

        // Act & Assert
        await FluentActions.Invoking(async () =>
            await runner.RunAsync<TestData>(null!))
            .Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("definition");
    }

    [Fact]
    public async Task RoutingSlipRunner_RunAsync_WithNullInitialData_ThrowsArgumentNullException()
    {
        // Arrange
        var requestContext = RequestContext.Create();
        var options = new RoutingSlipOptions();
        var logger = Substitute.For<ILogger<RoutingSlipRunner>>();
        var runner = new RoutingSlipRunner(requestContext, options, logger);

        var definition = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step1")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)))
            .Build();

        // Act & Assert
        await FluentActions.Invoking(async () =>
            await runner.RunAsync(definition, null!))
            .Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("initialData");
    }

    #endregion

    #region RoutingSlipStepDefinition Guards

    [Fact]
    public void RoutingSlipStepDefinition_Constructor_WithNullName_ThrowsArgumentException()
    {
        // Act & Assert
        FluentActions.Invoking(() =>
            new RoutingSlipStepDefinition<TestData>(
                null!,
                (data, ctx, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data))))
            .Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void RoutingSlipStepDefinition_Constructor_WithEmptyName_ThrowsArgumentException()
    {
        // Act & Assert
        FluentActions.Invoking(() =>
            new RoutingSlipStepDefinition<TestData>(
                "",
                (data, ctx, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data))))
            .Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void RoutingSlipStepDefinition_Constructor_WithNullExecute_ThrowsArgumentNullException()
    {
        // Act & Assert
        FluentActions.Invoking(() =>
            new RoutingSlipStepDefinition<TestData>("Step1", null!))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("execute");
    }

    #endregion

    #region RoutingSlipActivityEntry Guards

    [Fact]
    public void RoutingSlipActivityEntry_Constructor_WithNullStepName_ThrowsArgumentException()
    {
        // Act & Assert
        FluentActions.Invoking(() =>
            new RoutingSlipActivityEntry<TestData>(null!, new TestData(), null, DateTime.UtcNow))
            .Should().Throw<ArgumentException>()
            .WithParameterName("stepName");
    }

    [Fact]
    public void RoutingSlipActivityEntry_Constructor_WithEmptyStepName_ThrowsArgumentException()
    {
        // Act & Assert
        FluentActions.Invoking(() =>
            new RoutingSlipActivityEntry<TestData>("", new TestData(), null, DateTime.UtcNow))
            .Should().Throw<ArgumentException>()
            .WithParameterName("stepName");
    }

    [Fact]
    public void RoutingSlipActivityEntry_Constructor_WithNullData_ThrowsArgumentNullException()
    {
        // Act & Assert
        FluentActions.Invoking(() =>
            new RoutingSlipActivityEntry<TestData>("Step1", null!, null, DateTime.UtcNow))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("dataAfterExecution");
    }

    #endregion

    #region Test Types

    private sealed class TestData
    {
        public Guid Id { get; set; }
    }

    #endregion
}

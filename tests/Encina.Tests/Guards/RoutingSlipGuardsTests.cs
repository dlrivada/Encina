using Encina.Messaging.RoutingSlip;
using Shouldly;
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
        var ex = Should.Throw<ArgumentException>(() => RoutingSlipBuilder.Create<TestData>(null!));
        ex.ParamName.ShouldBe("slipType");
    }

    [Fact]
    public void RoutingSlipBuilder_Create_WithEmptySlipType_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentException>(() => RoutingSlipBuilder.Create<TestData>(""));
        ex.ParamName.ShouldBe("slipType");
    }

    [Fact]
    public void RoutingSlipBuilder_Create_WithWhitespaceSlipType_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentException>(() => RoutingSlipBuilder.Create<TestData>("   "));
        ex.ParamName.ShouldBe("slipType");
    }

    [Fact]
    public void RoutingSlipBuilder_OnCompletion_WithNullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("TestSlip");

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            builder.OnCompletion((Func<TestData, RoutingSlipContext<TestData>, CancellationToken, Task>)null!));
        ex.ParamName.ShouldBe("onCompletion");
    }

    [Fact]
    public void RoutingSlipBuilder_OnCompletion_Simplified_WithNullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("TestSlip");

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            builder.OnCompletion((Func<TestData, CancellationToken, Task>)null!));
        ex.ParamName.ShouldBe("onCompletion");
    }

    [Fact]
    public void RoutingSlipBuilder_WithTimeout_WithZero_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("TestSlip");

        // Act & Assert
        var ex = Should.Throw<ArgumentOutOfRangeException>(() => builder.WithTimeout(TimeSpan.Zero));
        ex.ParamName.ShouldBe("timeout");
    }

    [Fact]
    public void RoutingSlipBuilder_WithTimeout_WithNegative_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("TestSlip");

        // Act & Assert
        var ex = Should.Throw<ArgumentOutOfRangeException>(() => builder.WithTimeout(TimeSpan.FromMinutes(-1)));
        ex.ParamName.ShouldBe("timeout");
    }

    [Fact]
    public void RoutingSlipBuilder_Build_WithNoSteps_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("TestSlip");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.Build());
    }

    #endregion

    #region RoutingSlipStepBuilder Guards

    [Fact]
    public void RoutingSlipStepBuilder_Execute_WithNullFunction_ThrowsArgumentNullException()
    {
        // Arrange
        var stepBuilder = RoutingSlipBuilder.Create<TestData>("TestSlip").Step("Step1");

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            stepBuilder.Execute((Func<TestData, RoutingSlipContext<TestData>, CancellationToken, ValueTask<Either<EncinaError, TestData>>>)null!));
        ex.ParamName.ShouldBe("execute");
    }

    [Fact]
    public void RoutingSlipStepBuilder_Execute_Simplified_WithNullFunction_ThrowsArgumentNullException()
    {
        // Arrange
        var stepBuilder = RoutingSlipBuilder.Create<TestData>("TestSlip").Step("Step1");

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            stepBuilder.Execute((Func<TestData, CancellationToken, ValueTask<Either<EncinaError, TestData>>>)null!));
        ex.ParamName.ShouldBe("execute");
    }

    [Fact]
    public void RoutingSlipStepBuilder_Execute_Synchronous_WithNullFunction_ThrowsArgumentNullException()
    {
        // Arrange
        var stepBuilder = RoutingSlipBuilder.Create<TestData>("TestSlip").Step("Step1");

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            stepBuilder.Execute((Func<TestData, Either<EncinaError, TestData>>)null!));
        ex.ParamName.ShouldBe("execute");
    }

    [Fact]
    public void RoutingSlipStepBuilder_Compensate_WithNullFunction_ThrowsArgumentNullException()
    {
        // Arrange
        var stepBuilder = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step1")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)));

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            stepBuilder.Compensate((Func<TestData, RoutingSlipContext<TestData>, CancellationToken, Task>)null!));
        ex.ParamName.ShouldBe("compensate");
    }

    [Fact]
    public void RoutingSlipStepBuilder_Compensate_Simplified_WithNullFunction_ThrowsArgumentNullException()
    {
        // Arrange
        var stepBuilder = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step1")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)));

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            stepBuilder.Compensate((Func<TestData, CancellationToken, Task>)null!));
        ex.ParamName.ShouldBe("compensate");
    }

    [Fact]
    public void RoutingSlipStepBuilder_Compensate_Action_WithNullFunction_ThrowsArgumentNullException()
    {
        // Arrange
        var stepBuilder = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step1")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)));

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            stepBuilder.Compensate((Action<TestData>)null!));
        ex.ParamName.ShouldBe("compensate");
    }

    [Fact]
    public void RoutingSlipStepBuilder_WithMetadata_WithNullKey_ThrowsArgumentException()
    {
        // Arrange
        var stepBuilder = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step1")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)));

        // Act & Assert
        var ex = Should.Throw<ArgumentException>(() => stepBuilder.WithMetadata(null!, "value"));
        ex.ParamName.ShouldBe("key");
    }

    [Fact]
    public void RoutingSlipStepBuilder_WithMetadata_WithEmptyKey_ThrowsArgumentException()
    {
        // Arrange
        var stepBuilder = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step1")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)));

        // Act & Assert
        var ex = Should.Throw<ArgumentException>(() => stepBuilder.WithMetadata("", "value"));
        ex.ParamName.ShouldBe("key");
    }

    [Fact]
    public void RoutingSlipStepBuilder_Build_WithoutExecute_ThrowsInvalidOperationException()
    {
        // Arrange
        var stepBuilder = RoutingSlipBuilder.Create<TestData>("TestSlip").Step("Step1");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => stepBuilder.Build());
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
        var ex = Should.Throw<ArgumentNullException>(() => new RoutingSlipRunner(null!, options, logger));
        ex.ParamName.ShouldBe("requestContext");
    }

    [Fact]
    public void RoutingSlipRunner_Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var requestContext = RequestContext.Create();
        var logger = Substitute.For<ILogger<RoutingSlipRunner>>();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => new RoutingSlipRunner(requestContext, null!, logger));
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void RoutingSlipRunner_Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var requestContext = RequestContext.Create();
        var options = new RoutingSlipOptions();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => new RoutingSlipRunner(requestContext, options, null!));
        ex.ParamName.ShouldBe("logger");
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
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () => await runner.RunAsync<TestData>(null!));
        ex.ParamName.ShouldBe("definition");
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
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () => await runner.RunAsync(definition, null!));
        ex.ParamName.ShouldBe("initialData");
    }

    #endregion

    #region RoutingSlipStepDefinition Guards

    [Fact]
    public void RoutingSlipStepDefinition_Constructor_WithNullName_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentException>(() =>
            new RoutingSlipStepDefinition<TestData>(
                null!,
                (data, ctx, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data))));
        ex.ParamName.ShouldBe("name");
    }

    [Fact]
    public void RoutingSlipStepDefinition_Constructor_WithEmptyName_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentException>(() =>
            new RoutingSlipStepDefinition<TestData>(
                "",
                (data, ctx, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data))));
        ex.ParamName.ShouldBe("name");
    }

    [Fact]
    public void RoutingSlipStepDefinition_Constructor_WithNullExecute_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new RoutingSlipStepDefinition<TestData>("Step1", null!));
        ex.ParamName.ShouldBe("execute");
    }

    #endregion

    #region RoutingSlipActivityEntry Guards

    [Fact]
    public void RoutingSlipActivityEntry_Constructor_WithNullStepName_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentException>(() =>
            new RoutingSlipActivityEntry<TestData>(null!, new TestData(), null, DateTime.UtcNow));
        ex.ParamName.ShouldBe("stepName");
    }

    [Fact]
    public void RoutingSlipActivityEntry_Constructor_WithEmptyStepName_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentException>(() =>
            new RoutingSlipActivityEntry<TestData>("", new TestData(), null, DateTime.UtcNow));
        ex.ParamName.ShouldBe("stepName");
    }

    [Fact]
    public void RoutingSlipActivityEntry_Constructor_WithNullData_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new RoutingSlipActivityEntry<TestData>("Step1", null!, null, DateTime.UtcNow));
        ex.ParamName.ShouldBe("dataAfterExecution");
    }

    #endregion

    #region Test Types

    private sealed class TestData
    {
        public Guid Id { get; set; }
    }

    #endregion
}

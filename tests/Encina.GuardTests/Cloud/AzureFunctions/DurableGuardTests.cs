using Encina.AzureFunctions.Durable;
using Microsoft.DurableTask;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

using FanOut = Encina.AzureFunctions.Durable.FanOutFanInExtensions;

namespace Encina.GuardTests.Cloud.AzureFunctions;

/// <summary>
/// Guard clause tests for the Durable Functions integration of <c>Encina.AzureFunctions</c>:
/// each test calls the real implementation with an invalid argument and asserts the exception
/// type and parameter name, plus the minimum valid call that reaches the check.
/// </summary>
public class DurableGuardTests
{
    private const string Activity = "Activity";

    // ---------------------------------------------------------------- DurableSagaBuilder

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void SagaBuilder_Step_ThrowsOnNullOrEmptyName(string? stepName)
    {
        var builder = DurableSagaBuilder.Create<string>();

        var action = () => builder.Step(stepName!);

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("stepName");
    }

    [Fact]
    public void SagaBuilder_Step_AcceptsValidName()
    {
        var builder = DurableSagaBuilder.Create<string>();

        builder.Step("reserve").ShouldNotBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SagaBuilder_WithTimeout_ThrowsOnNonPositiveTimeout(int seconds)
    {
        var builder = DurableSagaBuilder.Create<string>();

        var action = () => builder.WithTimeout(TimeSpan.FromSeconds(seconds));

        Should.Throw<ArgumentOutOfRangeException>(action).ParamName.ShouldBe("timeout");
    }

    [Fact]
    public void SagaBuilder_WithTimeout_AcceptsPositiveTimeout()
    {
        var builder = DurableSagaBuilder.Create<string>();

        builder.WithTimeout(TimeSpan.FromSeconds(1)).ShouldBeSameAs(builder);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void SagaStepBuilder_Execute_ThrowsOnNullOrEmptyActivityName(string? activityName)
    {
        var stepBuilder = DurableSagaBuilder.Create<string>().Step("reserve");

        var action = () => stepBuilder.Execute(activityName!);

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("activityName");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void SagaStepBuilder_Compensate_ThrowsOnNullOrEmptyActivityName(string? activityName)
    {
        var stepBuilder = DurableSagaBuilder.Create<string>().Step("reserve");

        var action = () => stepBuilder.Compensate(activityName!);

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("activityName");
    }

    [Fact]
    public void SagaStepBuilder_Step_ThrowsWhenCurrentStepHasNoExecuteActivity()
    {
        var stepBuilder = DurableSagaBuilder.Create<string>().Step("reserve");

        var action = () => stepBuilder.Step("charge");

        Should.Throw<InvalidOperationException>(action);
    }

    [Fact]
    public void SagaStepBuilder_Build_ReturnsLeftWhenStepHasNoExecuteActivity()
    {
        var stepBuilder = DurableSagaBuilder.Create<string>().Step("reserve");

        stepBuilder.Build().IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void Saga_ExecuteAsync_ThrowsOnNullContext()
    {
        var saga = BuildSingleStepSaga();

        var action = () => saga.ExecuteAsync(null!, "data");

        Should.Throw<ArgumentNullException>(action).ParamName.ShouldBe("context");
    }

    // ---------------------------------------------------------------- ActivityResult / DI / health check

    [Fact]
    public void ActivityResult_ToActivityResult_MapsRightToSuccess()
    {
        var either = LanguageExt.Prelude.Right<EncinaError, string>("value");

        var result = either.ToActivityResult();

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("value");
    }

    [Fact]
    public void DurableServiceCollectionExtensions_AddEncinaDurableFunctions_ThrowsOnNullServices()
    {
        IServiceCollection services = null!;

        var action = () => services.AddEncinaDurableFunctions();

        Should.Throw<ArgumentNullException>(action).ParamName.ShouldBe("services");
    }

    [Fact]
    public void DurableFunctionsHealthCheck_Constructor_ThrowsOnNullOptions()
    {
        var action = () => new DurableFunctionsHealthCheck(null!);

        Should.Throw<ArgumentNullException>(action).ParamName.ShouldBe("options");
    }

    [Fact]
    public void DurableFunctionsHealthCheck_Constructor_AcceptsOptions()
    {
        var check = new DurableFunctionsHealthCheck(Options.Create(new DurableFunctionsOptions()));

        check.Name.ShouldBe("encina-durable-functions");
    }

    // ---------------------------------------------------------------- OrchestrationContextExtensions

    [Fact]
    public async Task CallEncinaActivityAsync_ThrowsOnNullContext()
    {
        TaskOrchestrationContext context = null!;

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => context.CallEncinaActivityAsync<string, string>(Activity, "in"));

        ex.ParamName.ShouldBe("context");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task CallEncinaActivityAsync_ThrowsOnNullOrEmptyActivityName(string? activityName)
    {
        var context = Substitute.For<TaskOrchestrationContext>();

        var ex = await Should.ThrowAsync<ArgumentException>(
            () => context.CallEncinaActivityAsync<string, string>(activityName!, "in"));

        ex.ParamName.ShouldBe("activityName");
    }

    [Fact]
    public async Task CallEncinaActivityWithResultAsync_ThrowsOnNullContext()
    {
        TaskOrchestrationContext context = null!;

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => context.CallEncinaActivityWithResultAsync<string, string>(Activity, "in"));

        ex.ParamName.ShouldBe("context");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task CallEncinaActivityWithResultAsync_ThrowsOnNullOrEmptyActivityName(string? activityName)
    {
        var context = Substitute.For<TaskOrchestrationContext>();

        var ex = await Should.ThrowAsync<ArgumentException>(
            () => context.CallEncinaActivityWithResultAsync<string, string>(activityName!, "in"));

        ex.ParamName.ShouldBe("activityName");
    }

    [Fact]
    public async Task CallEncinaSubOrchestratorAsync_ThrowsOnNullContext()
    {
        TaskOrchestrationContext context = null!;

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => context.CallEncinaSubOrchestratorAsync<string, string>("Orchestrator", "in"));

        ex.ParamName.ShouldBe("context");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task CallEncinaSubOrchestratorAsync_ThrowsOnNullOrEmptyName(string? name)
    {
        var context = Substitute.For<TaskOrchestrationContext>();

        var ex = await Should.ThrowAsync<ArgumentException>(
            () => context.CallEncinaSubOrchestratorAsync<string, string>(name!, "in"));

        ex.ParamName.ShouldBe("orchestratorName");
    }

    [Fact]
    public async Task CallEncinaSubOrchestratorWithResultAsync_ThrowsOnNullContext()
    {
        TaskOrchestrationContext context = null!;

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => context.CallEncinaSubOrchestratorWithResultAsync<string, string>("Orchestrator", "in"));

        ex.ParamName.ShouldBe("context");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task CallEncinaSubOrchestratorWithResultAsync_ThrowsOnNullOrEmptyName(string? name)
    {
        var context = Substitute.For<TaskOrchestrationContext>();

        var ex = await Should.ThrowAsync<ArgumentException>(
            () => context.CallEncinaSubOrchestratorWithResultAsync<string, string>(name!, "in"));

        ex.ParamName.ShouldBe("orchestratorName");
    }

    [Fact]
    public async Task WaitForEncinaExternalEventAsync_ThrowsOnNullContext()
    {
        TaskOrchestrationContext context = null!;

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => context.WaitForEncinaExternalEventAsync<string>("event"));

        ex.ParamName.ShouldBe("context");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task WaitForEncinaExternalEventAsync_ThrowsOnNullOrEmptyEventName(string? eventName)
    {
        var context = Substitute.For<TaskOrchestrationContext>();

        var ex = await Should.ThrowAsync<ArgumentException>(
            () => context.WaitForEncinaExternalEventAsync<string>(eventName!));

        ex.ParamName.ShouldBe("eventName");
    }

    [Fact]
    public void CreateRetryOptions_ThrowsOnNegativeMaxRetries()
    {
        var action = () => OrchestrationContextExtensions.CreateRetryOptions(-1, TimeSpan.FromSeconds(1));

        Should.Throw<ArgumentOutOfRangeException>(action).ParamName.ShouldBe("maxRetries");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateRetryOptions_ThrowsOnNonPositiveFirstRetryInterval(int seconds)
    {
        var action = () => OrchestrationContextExtensions.CreateRetryOptions(1, TimeSpan.FromSeconds(seconds));

        Should.Throw<ArgumentOutOfRangeException>(action).ParamName.ShouldBe("firstRetryInterval");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateRetryOptions_ThrowsOnNonPositiveBackoffCoefficient(double coefficient)
    {
        var action = () => OrchestrationContextExtensions.CreateRetryOptions(1, TimeSpan.FromSeconds(1), coefficient);

        Should.Throw<ArgumentOutOfRangeException>(action).ParamName.ShouldBe("backoffCoefficient");
    }

    [Fact]
    public void CreateRetryOptions_AcceptsZeroRetries()
    {
        OrchestrationContextExtensions.CreateRetryOptions(0, TimeSpan.FromSeconds(1)).ShouldNotBeNull();
    }

    [Fact]
    public void GetCorrelationId_ThrowsOnNullContext()
    {
        TaskOrchestrationContext context = null!;

        var action = () => context.GetCorrelationId();

        Should.Throw<ArgumentNullException>(action).ParamName.ShouldBe("context");
    }

    // ---------------------------------------------------------------- FanOutFanInExtensions

    [Fact]
    public async Task FanOutAsync_ThrowsOnNullContext()
    {
        TaskOrchestrationContext context = null!;

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => FanOut.FanOutAsync<int, string>(context, Activity, [1]));

        ex.ParamName.ShouldBe("context");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task FanOutAsync_ThrowsOnNullOrEmptyActivityName(string? activityName)
    {
        var context = Substitute.For<TaskOrchestrationContext>();

        var ex = await Should.ThrowAsync<ArgumentException>(
            () => FanOut.FanOutAsync<int, string>(context, activityName!, [1]));

        ex.ParamName.ShouldBe("activityName");
    }

    [Fact]
    public async Task FanOutAsync_ThrowsOnNullInputs()
    {
        var context = Substitute.For<TaskOrchestrationContext>();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => FanOut.FanOutAsync<int, string>(context, Activity, null!));

        ex.ParamName.ShouldBe("inputs");
    }

    [Fact]
    public async Task FanOutAsync_ReturnsEmptyForNoInputs()
    {
        var context = Substitute.For<TaskOrchestrationContext>();

        var results = await FanOut.FanOutAsync<int, string>(context, Activity, []);

        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task FanOutAllAsync_ThrowsOnNullContext()
    {
        TaskOrchestrationContext context = null!;

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => FanOut.FanOutAllAsync<int, string>(context, Activity, [1]));

        ex.ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task FanOutWithResultAsync_ThrowsOnNullContext()
    {
        TaskOrchestrationContext context = null!;

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => FanOut.FanOutWithResultAsync<int, string>(context, Activity, [1]));

        ex.ParamName.ShouldBe("context");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task FanOutWithResultAsync_ThrowsOnNullOrEmptyActivityName(string? activityName)
    {
        var context = Substitute.For<TaskOrchestrationContext>();

        var ex = await Should.ThrowAsync<ArgumentException>(
            () => FanOut.FanOutWithResultAsync<int, string>(context, activityName!, [1]));

        ex.ParamName.ShouldBe("activityName");
    }

    [Fact]
    public async Task FanOutWithResultAsync_ThrowsOnNullInputs()
    {
        var context = Substitute.For<TaskOrchestrationContext>();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => FanOut.FanOutWithResultAsync<int, string>(context, Activity, null!));

        ex.ParamName.ShouldBe("inputs");
    }

    [Fact]
    public async Task FanOutWithResultAsync_ReturnsEmptyForNoInputs()
    {
        var context = Substitute.For<TaskOrchestrationContext>();

        var results = await FanOut.FanOutWithResultAsync<int, string>(context, Activity, []);

        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task FanOutMultipleAsync_TwoActivities_ThrowsOnNullContext()
    {
        TaskOrchestrationContext context = null!;

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => FanOut.FanOutMultipleAsync<string, int>(context, ("A", new object()), ("B", new object())));

        ex.ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task FanOutMultipleAsync_ThreeActivities_ThrowsOnNullContext()
    {
        TaskOrchestrationContext context = null!;

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => FanOut.FanOutMultipleAsync<string, int, bool>(
                context, ("A", new object()), ("B", new object()), ("C", new object())));

        ex.ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task FanOutFirstSuccessAsync_ThrowsOnNullContext()
    {
        TaskOrchestrationContext context = null!;

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => FanOut.FanOutFirstSuccessAsync<int, string>(context, Activity, [1]));

        ex.ParamName.ShouldBe("context");
    }

    private static DurableSaga<string> BuildSingleStepSaga()
    {
        var built = DurableSagaBuilder.Create<string>()
            .Step("reserve").Execute(Activity)
            .Build();

        return built.Match(Right: saga => saga, Left: error => throw new InvalidOperationException(error.Message));
    }
}

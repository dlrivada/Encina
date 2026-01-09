using Encina.AzureFunctions.Durable;
using Microsoft.DurableTask;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.AzureFunctions.Tests.Durable;

public class OrchestrationContextExtensionsTests
{
    #region GetCorrelationId Tests

    [Fact]
    public void GetCorrelationId_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        TaskOrchestrationContext? context = null;

        // Act & Assert
        var action = () => context!.GetCorrelationId();
        Should.Throw<ArgumentNullException>(action);
    }

    [Fact]
    public void GetCorrelationId_ReturnsInstanceId()
    {
        // Arrange
        var context = Substitute.For<TaskOrchestrationContext>();
        context.InstanceId.Returns("test-instance-123");

        // Act
        var correlationId = context.GetCorrelationId();

        // Assert
        correlationId.ShouldBe("test-instance-123");
    }

    #endregion

    #region CallEncinaActivityAsync Guard Clauses

    [Fact]
    public async Task CallEncinaActivityAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        TaskOrchestrationContext? context = null;

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await context!.CallEncinaActivityAsync<string, int>("ActivityName", "input"));
    }

    [Fact]
    public async Task CallEncinaActivityAsync_WithNullActivityName_ThrowsArgumentException()
    {
        // Arrange
        var context = Substitute.For<TaskOrchestrationContext>();

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await context.CallEncinaActivityAsync<string, int>(null!, "input"));
    }

    [Fact]
    public async Task CallEncinaActivityAsync_WithEmptyActivityName_ThrowsArgumentException()
    {
        // Arrange
        var context = Substitute.For<TaskOrchestrationContext>();

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await context.CallEncinaActivityAsync<string, int>(string.Empty, "input"));
    }

    #endregion

    #region CallEncinaActivityWithResultAsync Guard Clauses

    [Fact]
    public async Task CallEncinaActivityWithResultAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        TaskOrchestrationContext? context = null;

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await context!.CallEncinaActivityWithResultAsync<string, int>("ActivityName", "input"));
    }

    [Fact]
    public async Task CallEncinaActivityWithResultAsync_WithNullActivityName_ThrowsArgumentException()
    {
        // Arrange
        var context = Substitute.For<TaskOrchestrationContext>();

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await context.CallEncinaActivityWithResultAsync<string, int>(null!, "input"));
    }

    [Fact]
    public async Task CallEncinaActivityWithResultAsync_WithEmptyActivityName_ThrowsArgumentException()
    {
        // Arrange
        var context = Substitute.For<TaskOrchestrationContext>();

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await context.CallEncinaActivityWithResultAsync<string, int>(string.Empty, "input"));
    }

    #endregion

    #region CallEncinaSubOrchestratorAsync Guard Clauses

    [Fact]
    public async Task CallEncinaSubOrchestratorAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        TaskOrchestrationContext? context = null;

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await context!.CallEncinaSubOrchestratorAsync<string, int>("OrchestratorName", "input"));
    }

    [Fact]
    public async Task CallEncinaSubOrchestratorAsync_WithNullOrchestratorName_ThrowsArgumentException()
    {
        // Arrange
        var context = Substitute.For<TaskOrchestrationContext>();

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await context.CallEncinaSubOrchestratorAsync<string, int>(null!, "input"));
    }

    [Fact]
    public async Task CallEncinaSubOrchestratorAsync_WithEmptyOrchestratorName_ThrowsArgumentException()
    {
        // Arrange
        var context = Substitute.For<TaskOrchestrationContext>();

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await context.CallEncinaSubOrchestratorAsync<string, int>(string.Empty, "input"));
    }

    #endregion

    #region CallEncinaSubOrchestratorWithResultAsync Guard Clauses

    [Fact]
    public async Task CallEncinaSubOrchestratorWithResultAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        TaskOrchestrationContext? context = null;

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await context!.CallEncinaSubOrchestratorWithResultAsync<string, int>("OrchestratorName", "input"));
    }

    [Fact]
    public async Task CallEncinaSubOrchestratorWithResultAsync_WithNullOrchestratorName_ThrowsArgumentException()
    {
        // Arrange
        var context = Substitute.For<TaskOrchestrationContext>();

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await context.CallEncinaSubOrchestratorWithResultAsync<string, int>(null!, "input"));
    }

    [Fact]
    public async Task CallEncinaSubOrchestratorWithResultAsync_WithEmptyOrchestratorName_ThrowsArgumentException()
    {
        // Arrange
        var context = Substitute.For<TaskOrchestrationContext>();

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await context.CallEncinaSubOrchestratorWithResultAsync<string, int>(string.Empty, "input"));
    }

    #endregion

    #region WaitForEncinaExternalEventAsync Guard Clauses

    [Fact]
    public async Task WaitForEncinaExternalEventAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        TaskOrchestrationContext? context = null;

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await context!.WaitForEncinaExternalEventAsync<string>("EventName"));
    }

    [Fact]
    public async Task WaitForEncinaExternalEventAsync_WithNullEventName_ThrowsArgumentException()
    {
        // Arrange
        var context = Substitute.For<TaskOrchestrationContext>();

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await context.WaitForEncinaExternalEventAsync<string>(null!));
    }

    [Fact]
    public async Task WaitForEncinaExternalEventAsync_WithEmptyEventName_ThrowsArgumentException()
    {
        // Arrange
        var context = Substitute.For<TaskOrchestrationContext>();

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await context.WaitForEncinaExternalEventAsync<string>(string.Empty));
    }

    #endregion

    #region CreateRetryOptions Tests

    [Fact]
    public void CreateRetryOptions_WithValidParameters_ReturnsTaskOptions()
    {
        // Act
        var options = OrchestrationContextExtensions.CreateRetryOptions(
            maxRetries: 3,
            firstRetryInterval: TimeSpan.FromSeconds(5),
            backoffCoefficient: 2.0,
            maxRetryInterval: TimeSpan.FromMinutes(1));

        // Assert
        options.ShouldNotBeNull();
    }

    [Fact]
    public void CreateRetryOptions_WithDefaultBackoffCoefficient_UsesDefault()
    {
        // Act
        var options = OrchestrationContextExtensions.CreateRetryOptions(
            maxRetries: 3,
            firstRetryInterval: TimeSpan.FromSeconds(5));

        // Assert
        options.ShouldNotBeNull();
    }

    [Fact]
    public void CreateRetryOptions_WithZeroRetries_CreatesValidOptions()
    {
        // Act
        var options = OrchestrationContextExtensions.CreateRetryOptions(
            maxRetries: 0,
            firstRetryInterval: TimeSpan.FromSeconds(1));

        // Assert
        options.ShouldNotBeNull();
    }

    [Fact]
    public void CreateRetryOptions_WithNegativeRetries_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        var action = () => OrchestrationContextExtensions.CreateRetryOptions(
            maxRetries: -1,
            firstRetryInterval: TimeSpan.FromSeconds(5));

        Should.Throw<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void CreateRetryOptions_WithZeroFirstRetryInterval_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        var action = () => OrchestrationContextExtensions.CreateRetryOptions(
            maxRetries: 3,
            firstRetryInterval: TimeSpan.Zero);

        Should.Throw<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void CreateRetryOptions_WithNegativeFirstRetryInterval_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        var action = () => OrchestrationContextExtensions.CreateRetryOptions(
            maxRetries: 3,
            firstRetryInterval: TimeSpan.FromSeconds(-5));

        Should.Throw<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void CreateRetryOptions_WithZeroBackoffCoefficient_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        var action = () => OrchestrationContextExtensions.CreateRetryOptions(
            maxRetries: 3,
            firstRetryInterval: TimeSpan.FromSeconds(5),
            backoffCoefficient: 0);

        Should.Throw<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void CreateRetryOptions_WithNegativeBackoffCoefficient_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        var action = () => OrchestrationContextExtensions.CreateRetryOptions(
            maxRetries: 3,
            firstRetryInterval: TimeSpan.FromSeconds(5),
            backoffCoefficient: -1.0);

        Should.Throw<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void CreateRetryOptions_WithHighMaxRetries_CreatesValidOptions()
    {
        // Act
        var options = OrchestrationContextExtensions.CreateRetryOptions(
            maxRetries: 100,
            firstRetryInterval: TimeSpan.FromMilliseconds(100));

        // Assert
        options.ShouldNotBeNull();
    }

    [Fact]
    public void CreateRetryOptions_WithSmallFirstRetryInterval_CreatesValidOptions()
    {
        // Act
        var options = OrchestrationContextExtensions.CreateRetryOptions(
            maxRetries: 5,
            firstRetryInterval: TimeSpan.FromMilliseconds(1));

        // Assert
        options.ShouldNotBeNull();
    }

    [Fact]
    public void CreateRetryOptions_WithLargeMaxRetryInterval_CreatesValidOptions()
    {
        // Act
        var options = OrchestrationContextExtensions.CreateRetryOptions(
            maxRetries: 5,
            firstRetryInterval: TimeSpan.FromSeconds(1),
            maxRetryInterval: TimeSpan.FromHours(24));

        // Assert
        options.ShouldNotBeNull();
    }

    #endregion
}

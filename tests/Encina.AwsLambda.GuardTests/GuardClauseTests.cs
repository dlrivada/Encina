using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.CloudWatchEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Encina.AwsLambda.Health;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Encina.AwsLambda.GuardTests;

/// <summary>
/// Tests for guard clauses (null checks, argument validation) across public APIs.
/// </summary>
public class GuardClauseTests
{
    #region ServiceCollectionExtensions

    [Fact]
    public void ServiceCollectionExtensions_AddEncinaAwsLambda_ThrowsOnNullServices()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var action = () => services.AddEncinaAwsLambda();

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Fact]
    public void ServiceCollectionExtensions_AddEncinaAwsLambda_WithOptions_ThrowsOnNullServices()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var action = () => services.AddEncinaAwsLambda(_ => { });

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Fact]
    public void ServiceCollectionExtensions_AddEncinaAwsLambda_ThrowsOnNullConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var action = () => services.AddEncinaAwsLambda(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("configureOptions");
    }

    #endregion

    #region AwsLambdaHealthCheck

    [Fact]
    public void AwsLambdaHealthCheck_Constructor_ThrowsOnNullOptions()
    {
        // Act
        var action = () => new AwsLambdaHealthCheck(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    #endregion

    #region ApiGatewayResponseExtensions

    [Fact]
    public void ToCreatedResponse_ThrowsOnNullLocationFactory()
    {
        // Arrange
        var result = Either<EncinaError, TestResult>.Right(new TestResult { Id = 1 });

        // Act
        var action = () => result.ToCreatedResponse<TestResult>(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("locationFactory");
    }

    #endregion

    #region SqsMessageHandler

    [Fact]
    public async Task SqsMessageHandler_ProcessBatchAsync_ThrowsOnNullEvent()
    {
        // Act
        var action = () => SqsMessageHandler.ProcessBatchAsync<int>(
            null!,
            _ => Task.FromResult(Either<EncinaError, int>.Right(1)));

        // Assert
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("sqsEvent");
    }

    [Fact]
    public async Task SqsMessageHandler_ProcessBatchAsync_ThrowsOnNullProcessor()
    {
        // Arrange
        var sqsEvent = new SQSEvent { Records = [] };

        // Act
        var action = () => SqsMessageHandler.ProcessBatchAsync<int>(
            sqsEvent,
            null!);

        // Assert
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("processMessage");
    }

    [Fact]
    public async Task SqsMessageHandler_ProcessAllAsync_ThrowsOnNullEvent()
    {
        // Act
        var action = () => SqsMessageHandler.ProcessAllAsync<int>(
            null!,
            _ => Task.FromResult(Either<EncinaError, int>.Right(1)));

        // Assert
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("sqsEvent");
    }

    [Fact]
    public async Task SqsMessageHandler_ProcessAllAsync_ThrowsOnNullProcessor()
    {
        // Arrange
        var sqsEvent = new SQSEvent { Records = [] };

        // Act
        var action = () => SqsMessageHandler.ProcessAllAsync<int>(
            sqsEvent,
            null!);

        // Assert
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("processMessage");
    }

    [Fact]
    public void SqsMessageHandler_DeserializeMessage_ThrowsOnNullRecord()
    {
        // Act
        var action = () => SqsMessageHandler.DeserializeMessage<TestMessage>(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("record");
    }

    #endregion

    #region EventBridgeHandler

    [Fact]
    public async Task EventBridgeHandler_ProcessAsync_ThrowsOnNullEvent()
    {
        // Act
        var action = () => EventBridgeHandler.ProcessAsync<TestEvent, int>(
            null!,
            _ => Task.FromResult(Either<EncinaError, int>.Right(1)));

        // Assert
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("eventBridgeEvent");
    }

    [Fact]
    public async Task EventBridgeHandler_ProcessAsync_ThrowsOnNullProcessor()
    {
        // Arrange
        var eventBridgeEvent = new CloudWatchEvent<TestEvent> { Detail = new TestEvent() };

        // Act
        var action = () => EventBridgeHandler.ProcessAsync<TestEvent, int>(
            eventBridgeEvent,
            null!);

        // Assert
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("processEvent");
    }

    [Fact]
    public async Task EventBridgeHandler_ProcessRawAsync_ThrowsOnNullJson()
    {
        // Act
        var action = () => EventBridgeHandler.ProcessRawAsync<TestEvent, int>(
            null!,
            _ => Task.FromResult(Either<EncinaError, int>.Right(1)));

        // Assert
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("eventJson");
    }

    [Fact]
    public async Task EventBridgeHandler_ProcessRawAsync_ThrowsOnNullProcessor()
    {
        // Act
        var action = () => EventBridgeHandler.ProcessRawAsync<TestEvent, int>(
            "{}",
            null!);

        // Assert
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("processEvent");
    }

    [Fact]
    public void EventBridgeHandler_GetMetadata_ThrowsOnNullEvent()
    {
        // Act
        var action = () => EventBridgeHandler.GetMetadata<TestEvent>(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("eventBridgeEvent");
    }

    #endregion

    #region LambdaContextExtensions

    [Fact]
    public void LambdaContextExtensions_GetCorrelationId_ThrowsOnNullContext()
    {
        // Arrange
        ILambdaContext context = null!;

        // Act
        var action = () => context.GetCorrelationId();

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public void LambdaContextExtensions_GetTenantId_ThrowsOnNullContext()
    {
        // Arrange
        ILambdaContext context = null!;

        // Act
        var action = () => context.GetTenantId();

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public void LambdaContextExtensions_GetUserId_ThrowsOnNullContext()
    {
        // Arrange
        ILambdaContext context = null!;

        // Act
        var action = () => context.GetUserId();

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public void LambdaContextExtensions_GetAwsRequestId_ThrowsOnNullContext()
    {
        // Arrange
        ILambdaContext context = null!;

        // Act
        var action = () => context.GetAwsRequestId();

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public void LambdaContextExtensions_GetFunctionName_ThrowsOnNullContext()
    {
        // Arrange
        ILambdaContext context = null!;

        // Act
        var action = () => context.GetFunctionName();

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public void LambdaContextExtensions_GetRemainingTimeMs_ThrowsOnNullContext()
    {
        // Arrange
        ILambdaContext context = null!;

        // Act
        var action = () => context.GetRemainingTimeMs();

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    #endregion

    #region Test Types

    private sealed class TestResult
    {
        public int Id { get; set; }
    }

    private sealed class TestMessage
    {
        public int Value { get; set; }
    }

    private sealed class TestEvent
    {
        public string Data { get; set; } = string.Empty;
    }

    #endregion
}

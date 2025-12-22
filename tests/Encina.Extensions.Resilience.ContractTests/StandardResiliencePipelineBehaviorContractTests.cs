using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Polly.Registry;
using Polly.Timeout;
using Shouldly;
using Encina.Extensions.Resilience;
using Xunit;

namespace Encina.Extensions.Resilience.ContractTests;

/// <summary>
/// Contract tests for <see cref="StandardResiliencePipelineBehavior{TRequest, TResponse}"/>.
/// Verifies that the behavior implementation adheres to the IPipelineBehavior contract.
/// </summary>
public class StandardResiliencePipelineBehaviorContractTests
{
    [Fact]
    public void Contract_MustImplementIPipelineBehavior()
    {
        // Arrange
        var behaviorType = typeof(StandardResiliencePipelineBehavior<,>);

        // Act
        var implementsInterface = behaviorType.GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>));

        // Assert
        implementsInterface.Should().BeTrue("StandardResiliencePipelineBehavior must implement IPipelineBehavior");
    }

    [Fact]
    public void Contract_Handle_MustReturnEither()
    {
        // Arrange
        var behaviorType = typeof(StandardResiliencePipelineBehavior<TestRequest, TestResponse>);
        var handleMethod = behaviorType.GetMethod("Handle");

        // Assert
        handleMethod.Should().NotBeNull();
        handleMethod!.ReturnType.Should().BeAssignableTo<ValueTask<Either<EncinaError, TestResponse>>>();
    }

    [Fact]
    public void Contract_Handle_MustAcceptCancellationToken()
    {
        // Arrange
        var behaviorType = typeof(StandardResiliencePipelineBehavior<TestRequest, TestResponse>);
        var handleMethod = behaviorType.GetMethod("Handle");

        // Act
        var parameters = handleMethod!.GetParameters();
        var hasCancellationToken = parameters.Any(p => p.ParameterType == typeof(CancellationToken));

        // Assert
        hasCancellationToken.Should().BeTrue("Handle method must accept CancellationToken");
    }

    [Fact]
    public void Contract_Handle_MustBeAsync()
    {
        // Arrange
        var behaviorType = typeof(StandardResiliencePipelineBehavior<TestRequest, TestResponse>);
        var handleMethod = behaviorType.GetMethod("Handle");

        // Assert
        handleMethod.Should().NotBeNull();
        handleMethod!.ReturnType.IsGenericType.Should().BeTrue();
        handleMethod.ReturnType.GetGenericTypeDefinition().Should().Be(typeof(ValueTask<>));
    }

    [Fact]
    public void Contract_Constructor_MustAcceptPipelineProvider()
    {
        // Arrange
        var behaviorType = typeof(StandardResiliencePipelineBehavior<TestRequest, TestResponse>);
        var constructor = behaviorType.GetConstructors().First();

        // Act
        var parameters = constructor.GetParameters();
        var hasPipelineProvider = parameters.Any(p => p.ParameterType == typeof(ResiliencePipelineProvider<string>));

        // Assert
        hasPipelineProvider.Should().BeTrue("Constructor must accept ResiliencePipelineProvider<string>");
    }

    [Fact]
    public void Contract_Constructor_MustAcceptLogger()
    {
        // Arrange
        var behaviorType = typeof(StandardResiliencePipelineBehavior<TestRequest, TestResponse>);
        var constructor = behaviorType.GetConstructors().First();

        // Act
        var parameters = constructor.GetParameters();
        var hasLogger = parameters.Any(p =>
            p.ParameterType.IsGenericType &&
            p.ParameterType.GetGenericTypeDefinition() == typeof(ILogger<>));

        // Assert
        hasLogger.Should().BeTrue("Constructor must accept ILogger<T>");
    }

    [Fact]
    public void Contract_MustBeSealed()
    {
        // Arrange
        var behaviorType = typeof(StandardResiliencePipelineBehavior<,>);

        // Assert
        behaviorType.IsSealed.Should().BeTrue("StandardResiliencePipelineBehavior should be sealed");
    }

    [Fact]
    public void Contract_MustHaveGenericTypeConstraints()
    {
        // Arrange
        var behaviorType = typeof(StandardResiliencePipelineBehavior<,>);
        var genericArguments = behaviorType.GetGenericArguments();

        // Act
        var requestArgument = genericArguments[0];
        var responseArgument = genericArguments[1];

        // Assert
        requestArgument.Name.Should().Be("TRequest");
        responseArgument.Name.Should().Be("TResponse");

        // TRequest must implement IRequest<TResponse>
        var constraints = requestArgument.GetGenericParameterConstraints();
        constraints.Should().ContainSingle(c =>
            c.IsGenericType &&
            c.GetGenericTypeDefinition() == typeof(IRequest<>));
    }

    [Fact]
    public async Task Contract_Handle_WithValidInput_MustReturnEither()
    {
        // Arrange
        var registry = new ResiliencePipelineRegistry<string>();
        registry.TryAddBuilder("TestRequest", (builder, _) =>
        {
            builder.AddTimeout(new TimeoutStrategyOptions { Timeout = TimeSpan.FromSeconds(10) });
        });

        var logger = new LoggerFactory().CreateLogger<StandardResiliencePipelineBehavior<TestRequest, TestResponse>>();
        var behavior = new StandardResiliencePipelineBehavior<TestRequest, TestResponse>(registry, logger);

        var request = new TestRequest();
        var context = RequestContext.Create(Guid.NewGuid().ToString());
        RequestHandlerCallback<TestResponse> nextStep = () => ValueTask.FromResult<Either<EncinaError, TestResponse>>(new TestResponse());

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.Should().BeAssignableTo<Either<EncinaError, TestResponse>>();
    }

    // Test helper classes
    private sealed record TestRequest : IRequest<TestResponse>;
    private sealed record TestResponse;
}

using LanguageExt;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Encina.Refit;
using Shouldly;
using System.Reflection;

namespace Encina.Refit.ContractTests;

/// <summary>
/// Contract tests for <see cref="RestApiRequestHandler{TRequest, TApiClient, TResponse}"/>.
/// Verifies that the handler implementation conforms to the expected contracts.
/// </summary>
public class RestApiRequestHandlerContractTests
{
    [Fact]
    public void Contract_MustImplementIRequestHandler()
    {
        // Arrange
        var handlerType = typeof(RestApiRequestHandler<TestRequest, ITestApiClient, string>);

        // Act
        var interfaces = handlerType.GetInterfaces();

        // Assert
        interfaces.ShouldContain(t =>
            t.IsGenericType &&
            t.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));
    }

    [Fact]
    public void Contract_Handle_MustReturnTaskOfEither()
    {
        // Arrange
        var handlerType = typeof(RestApiRequestHandler<TestRequest, ITestApiClient, string>);
        var handleMethod = handlerType.GetMethod("Handle");

        // Assert
        handleMethod.ShouldNotBeNull();
        handleMethod!.ReturnType.ShouldBe(typeof(Task<Either<EncinaError, string>>));
    }

    [Fact]
    public void Contract_Handle_MustAcceptCancellationToken()
    {
        // Arrange
        var handlerType = typeof(RestApiRequestHandler<TestRequest, ITestApiClient, string>);
        var handleMethod = handlerType.GetMethod("Handle");

        // Act
        var parameters = handleMethod!.GetParameters();

        // Assert
        parameters.Length.ShouldBe(2);
        parameters[0].ParameterType.ShouldBe(typeof(TestRequest));
        parameters[1].ParameterType.ShouldBe(typeof(CancellationToken));
    }

    [Fact]
    public void Contract_TApiClient_MustBeClass()
    {
        // Arrange
        var handlerType = typeof(RestApiRequestHandler<,,>);
        var apiClientTypeParameter = handlerType.GetGenericArguments()[1];

        // Act
        var constraints = apiClientTypeParameter.GetGenericParameterConstraints();
        var attributes = apiClientTypeParameter.GenericParameterAttributes;

        // Assert
        attributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint).ShouldBeTrue();
    }

    [Fact]
    public void Contract_Handler_MustBeSealed()
    {
        // Arrange
        var handlerType = typeof(RestApiRequestHandler<TestRequest, ITestApiClient, string>);

        // Assert
        handlerType.IsSealed.ShouldBeTrue("the handler should be sealed to prevent inheritance");
    }

    [Fact]
    public void Contract_Constructor_MustAcceptApiClientAndLogger()
    {
        // Arrange
        var handlerType = typeof(RestApiRequestHandler<TestRequest, ITestApiClient, string>);

        // Act
        var constructor = handlerType.GetConstructors().FirstOrDefault();

        // Assert
        constructor.ShouldNotBeNull();
        var parameters = constructor!.GetParameters();
        parameters.Count.ShouldBe(2);
        parameters[0].ParameterType.ShouldBe(typeof(ITestApiClient));
        parameters[1].ParameterType.ShouldBe(typeof(ILogger<RestApiRequestHandler<TestRequest, ITestApiClient, string>>));
    }

    [Fact]
    public void Contract_TRequest_MustImplementIRestApiRequest()
    {
        // Arrange
        var handlerType = typeof(RestApiRequestHandler<,,>);
        var requestTypeParameter = handlerType.GetGenericArguments()[0];

        // Act
        var constraints = requestTypeParameter.GetGenericParameterConstraints();

        // Assert
        constraints.ShouldContain(t =>
            t.IsGenericType &&
            t.GetGenericTypeDefinition() == typeof(IRestApiRequest<,>));
    }

    // Test helpers
    public interface ITestApiClient
    {
        Task<string> GetDataAsync();
    }

    public class TestRequest : IRestApiRequest<ITestApiClient, string>
    {
        public Task<string> ExecuteAsync(ITestApiClient apiClient, CancellationToken cancellationToken)
        {
            return Task.FromResult("test");
        }
    }
}

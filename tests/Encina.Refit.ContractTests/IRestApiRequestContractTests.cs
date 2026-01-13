using Encina.Refit;
using System.Reflection;

namespace Encina.Refit.ContractTests;

/// <summary>
/// Contract tests for <see cref="IRestApiRequest{TApiClient, TResponse}"/> interface.
/// Verifies that the interface conforms to the expected contracts.
/// </summary>
public class IRestApiRequestContractTests
{
    [Fact]
    public void Contract_MustInheritFromIRequest()
    {
        // Arrange
        var interfaceType = typeof(IRestApiRequest<,>);

        // Act
        var baseInterfaces = interfaceType.GetInterfaces();

        // Assert
        baseInterfaces.ShouldContain(t =>
            t.IsGenericType &&
            t.GetGenericTypeDefinition() == typeof(IRequest<>));
    }

    [Fact]
    public void Contract_ExecuteAsync_MustReturnTaskOfTResponse()
    {
        // Arrange
        var interfaceType = typeof(IRestApiRequest<ITestApiClient, string>);

        // Act
        var executeMethod = interfaceType.GetMethod("ExecuteAsync");

        // Assert
        executeMethod.ShouldNotBeNull();
        executeMethod!.ReturnType.ShouldBe(typeof(Task<string>));
    }

    [Fact]
    public void Contract_ExecuteAsync_MustAcceptApiClientAndCancellationToken()
    {
        // Arrange
        var interfaceType = typeof(IRestApiRequest<ITestApiClient, string>);

        // Act
        var executeMethod = interfaceType.GetMethod("ExecuteAsync");

        // Assert
        executeMethod.ShouldNotBeNull();
        var parameters = executeMethod!.GetParameters();
        parameters.Length.ShouldBe(2);
        parameters[0].ParameterType.ShouldBe(typeof(ITestApiClient));
        parameters[0].Name.ShouldBe("apiClient");
        parameters[1].ParameterType.ShouldBe(typeof(CancellationToken));
        parameters[1].Name.ShouldBe("cancellationToken");
    }

    [Fact]
    public void Contract_TApiClient_MustHaveClassConstraint()
    {
        // Arrange
        var interfaceType = typeof(IRestApiRequest<,>);
        var apiClientTypeParameter = interfaceType.GetGenericArguments()[0];

        // Act
        var attributes = apiClientTypeParameter.GenericParameterAttributes;

        // Assert
        ((attributes & GenericParameterAttributes.ReferenceTypeConstraint) != GenericParameterAttributes.None)
            .ShouldBeTrue("TApiClient must have a class constraint");
    }

    [Fact]
    public void Contract_Interface_ShouldBePublic()
    {
        // Arrange
        var interfaceType = typeof(IRestApiRequest<,>);

        // Assert
        interfaceType.IsPublic.ShouldBeTrue();
        interfaceType.IsInterface.ShouldBeTrue();
    }

    [Fact]
    public void Contract_GenericArguments_ShouldHaveCorrectNames()
    {
        // Arrange
        var interfaceType = typeof(IRestApiRequest<,>);

        // Act
        var genericArguments = interfaceType.GetGenericArguments();

        // Assert
        genericArguments.Length.ShouldBe(2);
        genericArguments[0].Name.ShouldBe("TApiClient");
        genericArguments[1].Name.ShouldBe("TResponse");
    }

    // Test helper
    public interface ITestApiClient
    {
        Task<string> GetDataAsync();
    }
}

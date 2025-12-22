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
        baseInterfaces.Should().Contain(t =>
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
        executeMethod.Should().NotBeNull();
        executeMethod!.ReturnType.Should().Be(typeof(Task<string>));
    }

    [Fact]
    public void Contract_ExecuteAsync_MustAcceptApiClientAndCancellationToken()
    {
        // Arrange
        var interfaceType = typeof(IRestApiRequest<ITestApiClient, string>);

        // Act
        var executeMethod = interfaceType.GetMethod("ExecuteAsync");
        var parameters = executeMethod!.GetParameters();

        // Assert
        parameters.Should().HaveCount(2);
        parameters[0].ParameterType.Should().Be(typeof(ITestApiClient));
        parameters[0].Name.Should().Be("apiClient");
        parameters[1].ParameterType.Should().Be(typeof(CancellationToken));
        parameters[1].Name.Should().Be("cancellationToken");
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
        (attributes & GenericParameterAttributes.ReferenceTypeConstraint).Should().NotBe(0,
            "TApiClient must have a class constraint");
    }

    [Fact]
    public void Contract_Interface_ShouldBePublic()
    {
        // Arrange
        var interfaceType = typeof(IRestApiRequest<,>);

        // Assert
        interfaceType.IsPublic.Should().BeTrue();
        interfaceType.IsInterface.Should().BeTrue();
    }

    [Fact]
    public void Contract_GenericArguments_ShouldHaveCorrectNames()
    {
        // Arrange
        var interfaceType = typeof(IRestApiRequest<,>);

        // Act
        var genericArguments = interfaceType.GetGenericArguments();

        // Assert
        genericArguments.Should().HaveCount(2);
        genericArguments[0].Name.Should().Be("TApiClient");
        genericArguments[1].Name.Should().Be("TResponse");
    }

    // Test helper
    public interface ITestApiClient
    {
        Task<string> GetDataAsync();
    }
}

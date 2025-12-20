using SimpleMediator.Refit;
using System.Reflection;

namespace SimpleMediator.Refit.Tests;

/// <summary>
/// Unit tests for <see cref="IRestApiRequest{TApiClient, TResponse}"/> interface.
/// </summary>
public class IRestApiRequestTests
{
    [Fact]
    public void Interface_ShouldInheritFromIRequest()
    {
        // Arrange
        var interfaceType = typeof(IRestApiRequest<,>);

        // Act
        var baseInterfaces = interfaceType.GetInterfaces();

        // Assert
        baseInterfaces.Should().Contain(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IRequest<>));
    }

    [Fact]
    public void Interface_ShouldHaveExecuteAsyncMethod()
    {
        // Arrange
        var interfaceType = typeof(IRestApiRequest<ITestApiClient, string>);

        // Act
        var method = interfaceType.GetMethod("ExecuteAsync");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be<Task<string>>();
        method.GetParameters().Should().HaveCount(2);
        method.GetParameters()[0].ParameterType.Should().Be<ITestApiClient>();
        method.GetParameters()[1].ParameterType.Should().Be<CancellationToken>();
    }

    [Fact]
    public void Interface_TApiClient_MustBeClass()
    {
        // Arrange
        var interfaceType = typeof(IRestApiRequest<,>);
        var apiClientTypeParameter = interfaceType.GetGenericArguments()[0];

        // Act
        var constraints = apiClientTypeParameter.GetGenericParameterConstraints();
        var attributes = apiClientTypeParameter.GenericParameterAttributes;

        // Assert
        (attributes & GenericParameterAttributes.ReferenceTypeConstraint).Should().NotBe(0);
    }

    // Test helper
    public interface ITestApiClient
    {
        Task<string> GetDataAsync();
    }
}

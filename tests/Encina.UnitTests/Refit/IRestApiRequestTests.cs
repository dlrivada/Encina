using System.Reflection;
using Encina.Refit;
using Shouldly;

namespace Encina.UnitTests.Refit;

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
        baseInterfaces.ShouldContain(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IRequest<>));
    }

    [Fact]
    public void Interface_ShouldHaveExecuteAsyncMethod()
    {
        // Arrange
        var interfaceType = typeof(IRestApiRequest<ITestApiClient, string>);

        // Act
        var method = interfaceType.GetMethod("ExecuteAsync");

        // Assert
        method.ShouldNotBeNull();
        method!.ReturnType.ShouldBe(typeof(Task<string>));
        method.GetParameters().Length.ShouldBe(2);
        method.GetParameters()[0].ParameterType.ShouldBe(typeof(ITestApiClient));
        method.GetParameters()[1].ParameterType.ShouldBe(typeof(CancellationToken));
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
        attributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint).ShouldBeTrue();
    }

    // Test helper
    public interface ITestApiClient
    {
        Task<string> GetDataAsync();
    }
}

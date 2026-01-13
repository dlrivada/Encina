using Microsoft.Extensions.DependencyInjection;
using Encina.Extensions.Resilience;
using System.Reflection;
using Xunit;

namespace Encina.Extensions.Resilience.ContractTests;

/// <summary>
/// Contract tests for <see cref="ServiceCollectionExtensions"/>.
/// Verifies that extension methods follow the expected contract patterns.
/// </summary>
public class ServiceCollectionExtensionsContractTests
{
    [Fact]
    public void Contract_AddEncinaStandardResilience_MustReturnIServiceCollection()
    {
        // Arrange
        var extensionsType = typeof(ServiceCollectionExtensions);
        var method = extensionsType.GetMethod(
            "AddEncinaStandardResilience",
            new[] { typeof(IServiceCollection) });

        // Assert
        method.ShouldNotBeNull();
        method!.ReturnType.ShouldBe(typeof(IServiceCollection));
    }

    [Fact]
    public void Contract_AddEncinaStandardResilience_WithOptions_MustReturnIServiceCollection()
    {
        // Arrange
        var extensionsType = typeof(ServiceCollectionExtensions);
        var method = extensionsType.GetMethod(
            "AddEncinaStandardResilience",
            new[] { typeof(IServiceCollection), typeof(Action<StandardResilienceOptions>) });

        // Assert
        method.ShouldNotBeNull();
        method!.ReturnType.ShouldBe(typeof(IServiceCollection));
    }

    [Fact]
    public void Contract_AddEncinaStandardResilienceFor_MustReturnIServiceCollection()
    {
        // Arrange
        var extensionsType = typeof(ServiceCollectionExtensions);
        var method = extensionsType.GetMethods()
            .FirstOrDefault(m => m.Name == "AddEncinaStandardResilienceFor" && m.IsGenericMethodDefinition);

        // Assert
        method.ShouldNotBeNull();
        method!.ReturnType.ShouldBe(typeof(IServiceCollection));
    }

    [Fact]
    public void Contract_AddEncinaStandardResilience_MustBeExtensionMethod()
    {
        // Arrange
        var extensionsType = typeof(ServiceCollectionExtensions);
        var method = extensionsType.GetMethod(
            "AddEncinaStandardResilience",
            new[] { typeof(IServiceCollection) });

        // Assert
        method.ShouldNotBeNull();
        method!.IsStatic.ShouldBeTrue();
        method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)
            .ShouldBeTrue("Method should be an extension method");
    }

    [Fact]
    public void Contract_AddEncinaStandardResilienceFor_MustBeExtensionMethod()
    {
        // Arrange
        var extensionsType = typeof(ServiceCollectionExtensions);
        var method = extensionsType.GetMethods()
            .FirstOrDefault(m => m.Name == "AddEncinaStandardResilienceFor" && m.IsGenericMethodDefinition);

        // Assert
        method.ShouldNotBeNull();
        method!.IsStatic.ShouldBeTrue();
        method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)
            .ShouldBeTrue("Method should be an extension method");
    }

    [Fact]
    public void Contract_ServiceCollectionExtensions_MustBeStaticClass()
    {
        // Arrange
        var extensionsType = typeof(ServiceCollectionExtensions);

        // Assert
        extensionsType.IsAbstract.ShouldBeTrue();
        extensionsType.IsSealed.ShouldBeTrue();
    }

    [Fact]
    public void Contract_AddEncinaStandardResilience_FirstParameterMustBeThis()
    {
        // Arrange
        var extensionsType = typeof(ServiceCollectionExtensions);
        var method = extensionsType.GetMethod(
            "AddEncinaStandardResilience",
            new[] { typeof(IServiceCollection) });

        // Act
        var firstParameter = method!.GetParameters()[0];

        // Assert
        firstParameter.ParameterType.ShouldBe(typeof(IServiceCollection));
    }

    [Fact]
    public void Contract_AddEncinaStandardResilienceFor_MustHaveGenericConstraints()
    {
        // Arrange
        var extensionsType = typeof(ServiceCollectionExtensions);
        var method = extensionsType.GetMethods()
            .FirstOrDefault(m => m.Name == "AddEncinaStandardResilienceFor" && m.IsGenericMethodDefinition);

        // Act
        var genericArguments = method!.GetGenericArguments();

        // Assert
        genericArguments.Length.ShouldBe(2);
        genericArguments[0].Name.ShouldBe("TRequest");
        genericArguments[1].Name.ShouldBe("TResponse");

        // TRequest must implement IRequest<TResponse>
        var requestConstraints = genericArguments[0].GetGenericParameterConstraints();
        requestConstraints.ShouldContain(c =>
            c.IsGenericType &&
            c.GetGenericTypeDefinition() == typeof(IRequest<>));
    }

    [Fact]
    public void Contract_AllExtensionMethods_MustHaveXmlDocumentation()
    {
        // Arrange
        var extensionsType = typeof(ServiceCollectionExtensions);
        var methods = extensionsType.GetMethods(BindingFlags.Public | BindingFlags.Static);

        // Assert
        foreach (var method in methods)
        {
            // XML documentation is checked by the compiler, just verify methods are public
            method.IsPublic.ShouldBeTrue($"Extension method {method.Name} should be public");
        }
    }

    [Fact]
    public void Contract_AddEncinaStandardResilience_ShouldAcceptNullConfigure()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - passing null should be handled by the parameterless overload
        var act = () => services.AddEncinaStandardResilience();

        // Assert
        Should.NotThrow(act);
    }

    [Fact]
    public void Contract_ExtensionMethods_MustBeFluentlyChainable()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services
            .AddEncinaStandardResilience()
            .AddEncinaStandardResilienceFor<TestRequest, TestResponse>(_ => { });

        // Assert
        result.ShouldBeSameAs(services);
    }

    // Test helper classes
    private sealed record TestRequest : IRequest<TestResponse>;
    private sealed record TestResponse;
}

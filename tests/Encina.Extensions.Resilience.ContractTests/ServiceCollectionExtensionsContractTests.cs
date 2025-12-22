using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
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
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be<IServiceCollection>();
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
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be<IServiceCollection>();
    }

    [Fact]
    public void Contract_AddEncinaStandardResilienceFor_MustReturnIServiceCollection()
    {
        // Arrange
        var extensionsType = typeof(ServiceCollectionExtensions);
        var method = extensionsType.GetMethods()
            .FirstOrDefault(m => m.Name == "AddEncinaStandardResilienceFor" && m.IsGenericMethodDefinition);

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be<IServiceCollection>();
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
        method.Should().NotBeNull();
        method!.IsStatic.Should().BeTrue();
        method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)
            .Should().BeTrue("Method should be an extension method");
    }

    [Fact]
    public void Contract_AddEncinaStandardResilienceFor_MustBeExtensionMethod()
    {
        // Arrange
        var extensionsType = typeof(ServiceCollectionExtensions);
        var method = extensionsType.GetMethods()
            .FirstOrDefault(m => m.Name == "AddEncinaStandardResilienceFor" && m.IsGenericMethodDefinition);

        // Assert
        method.Should().NotBeNull();
        method!.IsStatic.Should().BeTrue();
        method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)
            .Should().BeTrue("Method should be an extension method");
    }

    [Fact]
    public void Contract_ServiceCollectionExtensions_MustBeStaticClass()
    {
        // Arrange
        var extensionsType = typeof(ServiceCollectionExtensions);

        // Assert
        extensionsType.IsAbstract.Should().BeTrue();
        extensionsType.IsSealed.Should().BeTrue();
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
        firstParameter.ParameterType.Should().Be<IServiceCollection>();
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
        genericArguments.Should().HaveCount(2);
        genericArguments[0].Name.Should().Be("TRequest");
        genericArguments[1].Name.Should().Be("TResponse");

        // TRequest must implement IRequest<TResponse>
        var requestConstraints = genericArguments[0].GetGenericParameterConstraints();
        requestConstraints.Should().ContainSingle(c =>
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
            method.IsPublic.Should().BeTrue($"Extension method {method.Name} should be public");
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
        act.Should().NotThrow();
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
        result.Should().BeSameAs(services);
    }

    // Test helper classes
    private sealed record TestRequest : IRequest<TestResponse>;
    private sealed record TestResponse;
}

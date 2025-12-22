using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Refit;
using Encina.Refit;
using System.Reflection;

namespace Encina.Refit.ContractTests;

/// <summary>
/// Contract tests for <see cref="ServiceCollectionExtensions"/>.
/// Verifies that extension methods conform to expected contracts.
/// </summary>
public class ServiceCollectionExtensionsContractTests
{
    [Fact]
    public void Contract_AddEncinaRefitClient_MustReturnIHttpClientBuilder()
    {
        // Arrange
        var method = typeof(ServiceCollectionExtensions).GetMethod(
            "AddEncinaRefitClient",
            new[] { typeof(IServiceCollection), typeof(Action<HttpClient>) });

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(IHttpClientBuilder));
    }

    [Fact]
    public void Contract_AddEncinaRefitClient_WithRefitSettings_MustReturnIHttpClientBuilder()
    {
        // Arrange
        var method = typeof(ServiceCollectionExtensions).GetMethod(
            "AddEncinaRefitClient",
            new[] { typeof(IServiceCollection), typeof(RefitSettings), typeof(Action<HttpClient>) });

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(IHttpClientBuilder));
    }

    [Fact]
    public void Contract_AddEncinaRefitClient_WithSettingsProvider_MustReturnIHttpClientBuilder()
    {
        // Arrange
        var method = typeof(ServiceCollectionExtensions).GetMethod(
            "AddEncinaRefitClient",
            new[] { typeof(IServiceCollection), typeof(Func<IServiceProvider, RefitSettings>), typeof(Action<HttpClient>) });

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(IHttpClientBuilder));
    }

    [Fact]
    public void Contract_AllOverloads_MustBeExtensionMethods()
    {
        // Arrange
        var methods = typeof(ServiceCollectionExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == "AddEncinaRefitClient");

        // Assert
        foreach (var method in methods)
        {
            method.IsStatic.Should().BeTrue();
            method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false).Should().BeTrue();
            var parameters = method.GetParameters();
            parameters[0].ParameterType.Should().Be(typeof(IServiceCollection));
        }
    }

    [Fact]
    public void Contract_AllOverloads_MustHaveGenericTypeParameter()
    {
        // Arrange
        var methods = typeof(ServiceCollectionExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == "AddEncinaRefitClient");

        // Assert
        foreach (var method in methods)
        {
            method.IsGenericMethod.Should().BeTrue();
            var genericArguments = method.GetGenericArguments();
            genericArguments.Should().HaveCount(1);
            genericArguments[0].Name.Should().Be("TApiClient");
        }
    }

    [Fact]
    public void Contract_TApiClient_MustHaveClassConstraint()
    {
        // Arrange
        var method = typeof(ServiceCollectionExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == "AddEncinaRefitClient");
        var genericParameter = method.GetGenericArguments()[0];

        // Act
        var attributes = genericParameter.GenericParameterAttributes;

        // Assert
        (attributes & GenericParameterAttributes.ReferenceTypeConstraint).Should().NotBe(0);
    }

    [Fact]
    public void Contract_ConfigureParameter_MustBeOptional()
    {
        // Arrange
        var method = typeof(ServiceCollectionExtensions).GetMethod(
            "AddEncinaRefitClient",
            new[] { typeof(IServiceCollection), typeof(Action<HttpClient>) });

        // Act
        var configureParameter = method!.GetParameters()[1];

        // Assert
        configureParameter.IsOptional.Should().BeTrue();
        configureParameter.DefaultValue.Should().BeNull();
    }

    [Fact]
    public void Contract_AllMethods_MustBePublic()
    {
        // Arrange
        var methods = typeof(ServiceCollectionExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == "AddEncinaRefitClient");

        // Assert
        methods.Should().NotBeEmpty();
        foreach (var method in methods)
        {
            method.IsPublic.Should().BeTrue();
        }
    }

    [Fact]
    public void Contract_ExtensionsClass_MustBeStaticAndPublic()
    {
        // Arrange
        var type = typeof(ServiceCollectionExtensions);

        // Assert
        type.IsPublic.Should().BeTrue();
        type.IsAbstract.Should().BeTrue();
        type.IsSealed.Should().BeTrue();
    }

    [Fact]
    public void Contract_AllOverloads_ShouldExist()
    {
        // Arrange
        var methods = typeof(ServiceCollectionExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == "AddEncinaRefitClient")
            .ToList();

        // Assert
        methods.Should().HaveCount(3, "there should be 3 overloads");

        // Overload 1: Basic
        methods.Should().Contain(m =>
            m.GetParameters().Length == 2 &&
            m.GetParameters()[1].ParameterType == typeof(Action<HttpClient>));

        // Overload 2: With RefitSettings
        methods.Should().Contain(m =>
            m.GetParameters().Length == 3 &&
            m.GetParameters()[1].ParameterType == typeof(RefitSettings));

        // Overload 3: With settings provider
        methods.Should().Contain(m =>
            m.GetParameters().Length == 3 &&
            m.GetParameters()[1].ParameterType == typeof(Func<IServiceProvider, RefitSettings>));
    }
}

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
        method.ShouldNotBeNull();
        method!.ReturnType.ShouldBe(typeof(IHttpClientBuilder));
    }

    [Fact]
    public void Contract_AddEncinaRefitClient_WithRefitSettings_MustReturnIHttpClientBuilder()
    {
        // Arrange
        var method = typeof(ServiceCollectionExtensions).GetMethod(
            "AddEncinaRefitClient",
            new[] { typeof(IServiceCollection), typeof(RefitSettings), typeof(Action<HttpClient>) });

        // Assert
        method.ShouldNotBeNull();
        method!.ReturnType.ShouldBe(typeof(IHttpClientBuilder));
    }

    [Fact]
    public void Contract_AddEncinaRefitClient_WithSettingsProvider_MustReturnIHttpClientBuilder()
    {
        // Arrange
        var method = typeof(ServiceCollectionExtensions).GetMethod(
            "AddEncinaRefitClient",
            new[] { typeof(IServiceCollection), typeof(Func<IServiceProvider, RefitSettings>), typeof(Action<HttpClient>) });

        // Assert
        method.ShouldNotBeNull();
        method!.ReturnType.ShouldBe(typeof(IHttpClientBuilder));
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
            method.IsStatic.ShouldBeTrue();
            method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false).ShouldBeTrue();
            var parameters = method.GetParameters();
            parameters[0].ParameterType.ShouldBe(typeof(IServiceCollection));
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
            method.IsGenericMethod.ShouldBeTrue();
            var genericArguments = method.GetGenericArguments();
            genericArguments.Length.ShouldBe(1);
            genericArguments[0].Name.ShouldBe("TApiClient");
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
        ((attributes & GenericParameterAttributes.ReferenceTypeConstraint) != GenericParameterAttributes.None)
            .ShouldBeTrue();
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
        configureParameter.IsOptional.ShouldBeTrue();
        configureParameter.DefaultValue.ShouldBeNull();
    }

    [Fact]
    public void Contract_AllMethods_MustBePublic()
    {
        // Arrange
        var methods = typeof(ServiceCollectionExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == "AddEncinaRefitClient");

        // Assert
        methods.ShouldNotBeEmpty();
        foreach (var method in methods)
        {
            method.IsPublic.ShouldBeTrue();
        }
    }

    [Fact]
    public void Contract_ExtensionsClass_MustBeStaticAndPublic()
    {
        // Arrange
        var type = typeof(ServiceCollectionExtensions);

        // Assert
        type.IsPublic.ShouldBeTrue();
        type.IsAbstract.ShouldBeTrue();
        type.IsSealed.ShouldBeTrue();
    }

    [Fact]
    public void Contract_AllOverloads_ShouldExist()
    {
        // Arrange
        var methods = typeof(ServiceCollectionExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == "AddEncinaRefitClient")
            .ToList();

        // Assert
        methods.Count.ShouldBe(3, "there should be 3 overloads");

        // Overload 1: Basic
        methods.ShouldContain(m =>
            m.GetParameters().Length == 2 &&
            m.GetParameters()[1].ParameterType == typeof(Action<HttpClient>));

        // Overload 2: With RefitSettings
        methods.ShouldContain(m =>
            m.GetParameters().Length == 3 &&
            m.GetParameters()[1].ParameterType == typeof(RefitSettings));

        // Overload 3: With settings provider
        methods.ShouldContain(m =>
            m.GetParameters().Length == 3 &&
            m.GetParameters()[1].ParameterType == typeof(Func<IServiceProvider, RefitSettings>));
    }
}

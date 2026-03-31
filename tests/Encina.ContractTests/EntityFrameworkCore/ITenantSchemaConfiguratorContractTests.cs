using System.Reflection;
using Encina.EntityFrameworkCore.Tenancy;
using Encina.Tenancy;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Encina.ContractTests.EntityFrameworkCore;

/// <summary>
/// Contract tests verifying that <see cref="ITenantSchemaConfigurator"/> interface
/// and its default implementation are correctly defined.
/// </summary>
[Trait("Category", "Contract")]
[Trait("Feature", "Tenancy")]
public sealed class ITenantSchemaConfiguratorContractTests
{
    private static readonly Type ConfiguratorInterface = typeof(ITenantSchemaConfigurator);
    private static readonly Type DefaultImplementation = typeof(DefaultTenantSchemaConfigurator);

    #region Interface Shape

    [Fact]
    public void ITenantSchemaConfigurator_ShouldBeAnInterface()
    {
        ConfiguratorInterface.IsInterface.ShouldBeTrue(
            "ITenantSchemaConfigurator should be an interface");
    }

    [Fact]
    public void ITenantSchemaConfigurator_ShouldHaveConfigureSchemaMethod()
    {
        var method = ConfiguratorInterface.GetMethod("ConfigureSchema");

        method.ShouldNotBeNull(
            "ITenantSchemaConfigurator should have a ConfigureSchema method");
    }

    [Fact]
    public void ConfigureSchema_ShouldReturnVoid()
    {
        var method = ConfiguratorInterface.GetMethod("ConfigureSchema");

        method.ShouldNotBeNull();
        method!.ReturnType.ShouldBe(typeof(void),
            "ConfigureSchema should return void");
    }

    [Fact]
    public void ConfigureSchema_ShouldHaveTwoParameters()
    {
        var method = ConfiguratorInterface.GetMethod("ConfigureSchema");

        method.ShouldNotBeNull();
        var parameters = method!.GetParameters();

        parameters.Length.ShouldBe(2,
            "ConfigureSchema should have 2 parameters: ModelBuilder, TenantInfo?");
    }

    [Fact]
    public void ConfigureSchema_FirstParameter_ShouldBeModelBuilder()
    {
        var method = ConfiguratorInterface.GetMethod("ConfigureSchema");

        method.ShouldNotBeNull();
        var parameters = method!.GetParameters();

        parameters[0].ParameterType.ShouldBe(typeof(ModelBuilder),
            "First parameter should be ModelBuilder");
        parameters[0].Name.ShouldBe("modelBuilder");
    }

    [Fact]
    public void ConfigureSchema_SecondParameter_ShouldBeTenantInfo()
    {
        var method = ConfiguratorInterface.GetMethod("ConfigureSchema");

        method.ShouldNotBeNull();
        var parameters = method!.GetParameters();

        parameters[1].ParameterType.ShouldBe(typeof(TenantInfo),
            "Second parameter should be TenantInfo");
        parameters[1].Name.ShouldBe("tenantInfo");
    }

    [Fact]
    public void ITenantSchemaConfigurator_ShouldHaveExactlyOneMember()
    {
        var methods = ConfiguratorInterface.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        methods.Length.ShouldBe(1,
            "ITenantSchemaConfigurator should have exactly 1 method (ConfigureSchema)");
    }

    #endregion

    #region DefaultTenantSchemaConfigurator

    [Fact]
    public void DefaultTenantSchemaConfigurator_ShouldImplementInterface()
    {
        ConfiguratorInterface.IsAssignableFrom(DefaultImplementation).ShouldBeTrue(
            "DefaultTenantSchemaConfigurator should implement ITenantSchemaConfigurator");
    }

    [Fact]
    public void DefaultTenantSchemaConfigurator_ShouldBeSealed()
    {
        DefaultImplementation.IsSealed.ShouldBeTrue(
            "DefaultTenantSchemaConfigurator should be sealed");
    }

    [Fact]
    public void DefaultTenantSchemaConfigurator_ShouldNotBeAbstract()
    {
        DefaultImplementation.IsAbstract.ShouldBeFalse(
            "DefaultTenantSchemaConfigurator should not be abstract");
    }

    [Fact]
    public void DefaultTenantSchemaConfigurator_ShouldHaveSingleConstructor()
    {
        var constructors = DefaultImplementation.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        constructors.Length.ShouldBe(1,
            "DefaultTenantSchemaConfigurator should have exactly one public constructor");
    }

    [Fact]
    public void DefaultTenantSchemaConfigurator_Constructor_ShouldTakeIOptionsTenancyOptions()
    {
        var constructor = DefaultImplementation.GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0];
        var parameters = constructor.GetParameters();

        parameters.Length.ShouldBe(1,
            "Constructor should have exactly one parameter");

        parameters[0].ParameterType.IsGenericType.ShouldBeTrue(
            "Constructor parameter should be a generic type (IOptions<TenancyOptions>)");
        parameters[0].ParameterType.GetGenericTypeDefinition().ShouldBe(
            typeof(Microsoft.Extensions.Options.IOptions<>),
            "Constructor parameter should be IOptions<>");
        parameters[0].ParameterType.GetGenericArguments()[0].ShouldBe(typeof(TenancyOptions),
            "Constructor parameter generic argument should be TenancyOptions");
    }

    #endregion
}

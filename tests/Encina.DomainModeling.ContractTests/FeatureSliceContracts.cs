using System.Reflection;
using Encina.DomainModeling;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.DomainModeling.ContractTests;

/// <summary>
/// Contract tests verifying the public API of feature slice patterns.
/// </summary>
public class FeatureSliceContracts
{
    // === FeatureSlice Class ===

    [Fact]
    public void FeatureSlice_IsAbstractClass()
    {
        Assert.True(typeof(FeatureSlice).IsAbstract);
        Assert.True(typeof(FeatureSlice).IsClass);
    }

    [Fact]
    public void FeatureSlice_HasFeatureNameProperty()
    {
        var property = typeof(FeatureSlice).GetProperty("FeatureName");
        Assert.NotNull(property);
        Assert.True(property.GetMethod?.IsAbstract ?? false);
    }

    [Fact]
    public void FeatureSlice_HasDescriptionProperty()
    {
        var property = typeof(FeatureSlice).GetProperty("Description");
        Assert.NotNull(property);
        Assert.True(property.GetMethod?.IsVirtual ?? false);
    }

    [Fact]
    public void FeatureSlice_HasRoutePrefixProperty()
    {
        var property = typeof(FeatureSlice).GetProperty("RoutePrefix");
        Assert.NotNull(property);
        Assert.True(property.GetMethod?.IsVirtual ?? false);
    }

    [Fact]
    public void FeatureSlice_HasConfigureServicesMethod()
    {
        var method = typeof(FeatureSlice).GetMethod("ConfigureServices");
        Assert.NotNull(method);
        Assert.True(method.IsAbstract);
    }

    // === IFeatureSliceWithEndpoints Interface ===

    [Fact]
    public void IFeatureSliceWithEndpoints_IsInterface()
    {
        Assert.True(typeof(IFeatureSliceWithEndpoints).IsInterface);
    }

    [Fact]
    public void IFeatureSliceWithEndpoints_HasFeatureNameProperty()
    {
        var property = typeof(IFeatureSliceWithEndpoints).GetProperty("FeatureName");
        Assert.NotNull(property);
    }

    [Fact]
    public void IFeatureSliceWithEndpoints_HasConfigureServicesMethod()
    {
        var method = typeof(IFeatureSliceWithEndpoints).GetMethod("ConfigureServices");
        Assert.NotNull(method);
    }

    [Fact]
    public void IFeatureSliceWithEndpoints_HasEndpointConfiguratorTypesProperty()
    {
        var property = typeof(IFeatureSliceWithEndpoints).GetProperty("EndpointConfiguratorTypes");
        Assert.NotNull(property);
    }

    // === SliceDependency Record ===

    [Fact]
    public void SliceDependency_IsRecord()
    {
        Assert.True(typeof(SliceDependency).IsClass);
        var equalityContract = typeof(SliceDependency).GetProperty("EqualityContract", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(equalityContract);
    }

    [Fact]
    public void SliceDependency_HasSliceNameProperty()
    {
        var property = typeof(SliceDependency).GetProperty("SliceName");
        Assert.NotNull(property);
        Assert.Equal(typeof(string), property.PropertyType);
    }

    [Fact]
    public void SliceDependency_HasIsOptionalProperty()
    {
        var property = typeof(SliceDependency).GetProperty("IsOptional");
        Assert.NotNull(property);
        Assert.Equal(typeof(bool), property.PropertyType);
    }

    // === IFeatureSliceWithDependencies Interface ===

    [Fact]
    public void IFeatureSliceWithDependencies_IsInterface()
    {
        Assert.True(typeof(IFeatureSliceWithDependencies).IsInterface);
    }

    [Fact]
    public void IFeatureSliceWithDependencies_HasDependenciesProperty()
    {
        var property = typeof(IFeatureSliceWithDependencies).GetProperty("Dependencies");
        Assert.NotNull(property);
    }

    // === FeatureSliceError Record ===

    [Fact]
    public void FeatureSliceError_IsRecord()
    {
        Assert.True(typeof(FeatureSliceError).IsClass);
        var equalityContract = typeof(FeatureSliceError).GetProperty("EqualityContract", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(equalityContract);
    }

    [Fact]
    public void FeatureSliceError_HasRequiredProperties()
    {
        var properties = typeof(FeatureSliceError).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        Assert.Contains(properties, p => p.Name == "Message");
        Assert.Contains(properties, p => p.Name == "ErrorCode");
        Assert.Contains(properties, p => p.Name == "SliceName");
    }

    [Fact]
    public void FeatureSliceError_HasMissingDependencyFactory()
    {
        var method = typeof(FeatureSliceError).GetMethod("MissingDependency");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void FeatureSliceError_HasCircularDependencyFactory()
    {
        var method = typeof(FeatureSliceError).GetMethod("CircularDependency");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void FeatureSliceError_HasRegistrationFailedFactory()
    {
        var method = typeof(FeatureSliceError).GetMethod("RegistrationFailed");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    // === FeatureSliceConfiguration Class ===

    [Fact]
    public void FeatureSliceConfiguration_IsClass()
    {
        Assert.True(typeof(FeatureSliceConfiguration).IsClass);
        Assert.True(typeof(FeatureSliceConfiguration).IsSealed);
    }

    [Fact]
    public void FeatureSliceConfiguration_HasSlicesProperty()
    {
        var property = typeof(FeatureSliceConfiguration).GetProperty("Slices");
        Assert.NotNull(property);
    }

    [Fact]
    public void FeatureSliceConfiguration_HasSliceTypesProperty()
    {
        var property = typeof(FeatureSliceConfiguration).GetProperty("SliceTypes");
        Assert.NotNull(property);
    }

    [Fact]
    public void FeatureSliceConfiguration_HasValidateDependenciesProperty()
    {
        var property = typeof(FeatureSliceConfiguration).GetProperty("ValidateDependencies");
        Assert.NotNull(property);
        Assert.Equal(typeof(bool), property.PropertyType);
    }

    [Fact]
    public void FeatureSliceConfiguration_HasAddSliceGenericMethod()
    {
        var methods = typeof(FeatureSliceConfiguration).GetMethods()
            .Where(m => m.Name == "AddSlice" && m.IsGenericMethod);
        Assert.NotEmpty(methods);
    }

    [Fact]
    public void FeatureSliceConfiguration_HasAddSliceInstanceMethod()
    {
        var methods = typeof(FeatureSliceConfiguration).GetMethods()
            .Where(m => m.Name == "AddSlice" && !m.IsGenericMethod);
        Assert.NotEmpty(methods);
    }

    [Fact]
    public void FeatureSliceConfiguration_HasAddSlicesFromAssemblyMethod()
    {
        var method = typeof(FeatureSliceConfiguration).GetMethod("AddSlicesFromAssembly");
        Assert.NotNull(method);
    }

    // === FeatureSliceExtensions ===

    [Fact]
    public void FeatureSliceExtensions_IsStaticClass()
    {
        Assert.True(typeof(FeatureSliceExtensions).IsAbstract);
        Assert.True(typeof(FeatureSliceExtensions).IsSealed);
    }

    [Fact]
    public void FeatureSliceExtensions_HasAddFeatureSlicesMethod()
    {
        var method = typeof(FeatureSliceExtensions).GetMethod("AddFeatureSlices");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void FeatureSliceExtensions_HasAddFeatureSliceGenericMethod()
    {
        var method = typeof(FeatureSliceExtensions).GetMethod("AddFeatureSlice");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
        Assert.True(method.IsGenericMethod);
    }

    [Fact]
    public void FeatureSliceExtensions_HasAddFeatureSlicesFromAssemblyMethod()
    {
        var method = typeof(FeatureSliceExtensions).GetMethod("AddFeatureSlicesFromAssembly");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void FeatureSliceExtensions_HasGetFeatureSlicesMethod()
    {
        var method = typeof(FeatureSliceExtensions).GetMethod("GetFeatureSlices");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void FeatureSliceExtensions_HasGetFeatureSliceMethod()
    {
        var method = typeof(FeatureSliceExtensions).GetMethod("GetFeatureSlice");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void AddFeatureSlices_ReturnsIServiceCollection()
    {
        var method = typeof(FeatureSliceExtensions).GetMethod("AddFeatureSlices");
        Assert.NotNull(method);
        Assert.Equal(typeof(IServiceCollection), method.ReturnType);
    }

    // === IUseCaseHandler Interfaces ===

    [Fact]
    public void IUseCaseHandler_IsInterface()
    {
        Assert.True(typeof(IUseCaseHandler).IsInterface);
    }

    [Fact]
    public void IUseCaseHandler_IsMarkerInterface()
    {
        var members = typeof(IUseCaseHandler).GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Assert.Empty(members);
    }

    [Fact]
    public void IUseCaseHandler_WithInputOutput_IsGenericInterface()
    {
        Assert.True(typeof(IUseCaseHandler<,>).IsInterface);
        Assert.True(typeof(IUseCaseHandler<,>).IsGenericType);
        Assert.Equal(2, typeof(IUseCaseHandler<,>).GetGenericArguments().Length);
    }

    [Fact]
    public void IUseCaseHandler_WithInputOutput_ExtendsIUseCaseHandler()
    {
        var interfaces = typeof(IUseCaseHandler<,>).GetInterfaces();
        Assert.Contains(typeof(IUseCaseHandler), interfaces);
    }

    [Fact]
    public void IUseCaseHandler_WithInputOutput_HasHandleAsyncMethod()
    {
        var method = typeof(IUseCaseHandler<,>).GetMethod("HandleAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void IUseCaseHandler_WithInput_IsGenericInterface()
    {
        Assert.True(typeof(IUseCaseHandler<>).IsInterface);
        Assert.True(typeof(IUseCaseHandler<>).IsGenericType);
        Assert.Single(typeof(IUseCaseHandler<>).GetGenericArguments());
    }

    [Fact]
    public void IUseCaseHandler_WithInput_ExtendsIUseCaseHandler()
    {
        var interfaces = typeof(IUseCaseHandler<>).GetInterfaces();
        Assert.Contains(typeof(IUseCaseHandler), interfaces);
    }

    [Fact]
    public void IUseCaseHandler_WithInput_HasHandleAsyncMethod()
    {
        var method = typeof(IUseCaseHandler<>).GetMethod("HandleAsync");
        Assert.NotNull(method);
    }

    // === UseCaseHandlerExtensions ===

    [Fact]
    public void UseCaseHandlerExtensions_IsStaticClass()
    {
        Assert.True(typeof(UseCaseHandlerExtensions).IsAbstract);
        Assert.True(typeof(UseCaseHandlerExtensions).IsSealed);
    }

    [Fact]
    public void UseCaseHandlerExtensions_HasAddUseCaseHandlersFromAssemblyMethod()
    {
        var method = typeof(UseCaseHandlerExtensions).GetMethod("AddUseCaseHandlersFromAssembly");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void UseCaseHandlerExtensions_HasAddUseCaseHandlerMethod()
    {
        var method = typeof(UseCaseHandlerExtensions).GetMethod("AddUseCaseHandler");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
        Assert.True(method.IsGenericMethod);
    }

    [Fact]
    public void AddUseCaseHandlersFromAssembly_ReturnsIServiceCollection()
    {
        var method = typeof(UseCaseHandlerExtensions).GetMethod("AddUseCaseHandlersFromAssembly");
        Assert.NotNull(method);
        Assert.Equal(typeof(IServiceCollection), method.ReturnType);
    }
}

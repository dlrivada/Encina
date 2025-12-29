using Encina.DomainModeling;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Encina.DomainModeling.ContractTests;

/// <summary>
/// Contract tests verifying the public API of ports and adapters patterns.
/// </summary>
public class PortsAndAdaptersContracts
{
    // === IPort Interface Hierarchy ===

    [Fact]
    public void IPort_IsInterface()
    {
        Assert.True(typeof(IPort).IsInterface);
    }

    [Fact]
    public void IInboundPort_ExtendsIPort()
    {
        Assert.True(typeof(IPort).IsAssignableFrom(typeof(IInboundPort)));
    }

    [Fact]
    public void IOutboundPort_ExtendsIPort()
    {
        Assert.True(typeof(IPort).IsAssignableFrom(typeof(IOutboundPort)));
    }

    [Fact]
    public void IPort_IsMarkerInterface()
    {
        // Marker interfaces have no members
        var members = typeof(IPort).GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Assert.Empty(members);
    }

    [Fact]
    public void IInboundPort_IsMarkerInterface()
    {
        var members = typeof(IInboundPort).GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Assert.Empty(members);
    }

    [Fact]
    public void IOutboundPort_IsMarkerInterface()
    {
        var members = typeof(IOutboundPort).GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Assert.Empty(members);
    }

    // === IAdapter<TPort> Interface ===

    [Fact]
    public void IAdapter_IsGenericInterface()
    {
        Assert.True(typeof(IAdapter<>).IsInterface);
        Assert.True(typeof(IAdapter<>).IsGenericType);
    }

    [Fact]
    public void IAdapter_HasPortConstraint()
    {
        var typeParam = typeof(IAdapter<>).GetGenericArguments()[0];
        var constraints = typeParam.GetGenericParameterConstraints();

        Assert.Contains(typeof(IPort), constraints);
    }

    // === AdapterBase<TPort> Class ===

    [Fact]
    public void AdapterBase_IsAbstractClass()
    {
        Assert.True(typeof(AdapterBase<>).IsAbstract);
        Assert.True(typeof(AdapterBase<>).IsClass);
    }

    [Fact]
    public void AdapterBase_ImplementsIAdapter()
    {
        var interfaces = typeof(AdapterBase<>).GetInterfaces();
        Assert.Contains(interfaces, i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAdapter<>));
    }

    [Fact]
    public void AdapterBase_HasLoggerProperty()
    {
        var property = typeof(AdapterBase<>).GetProperty("Logger", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(property);
    }

    [Fact]
    public void AdapterBase_HasExecuteMethod()
    {
        var method = typeof(AdapterBase<>).GetMethod("Execute", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        Assert.True(method.IsFamily); // protected
    }

    [Fact]
    public void AdapterBase_HasExecuteAsyncMethod()
    {
        var method = typeof(AdapterBase<>).GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        Assert.True(method.IsFamily); // protected
    }

    // === AdapterError Record ===

    [Fact]
    public void AdapterError_IsRecord()
    {
        Assert.True(typeof(AdapterError).IsClass);
        // Records have EqualityContract
        var equalityContract = typeof(AdapterError).GetProperty("EqualityContract", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(equalityContract);
    }

    [Fact]
    public void AdapterError_HasRequiredProperties()
    {
        var properties = typeof(AdapterError).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Assert.Contains(properties, p => p.Name == "Message");
        Assert.Contains(properties, p => p.Name == "ErrorCode");
        Assert.Contains(properties, p => p.Name == "PortType");
        Assert.Contains(properties, p => p.Name == "OperationName");
        Assert.Contains(properties, p => p.Name == "InnerException");
    }

    [Fact]
    public void AdapterError_HasOperationFailedFactory()
    {
        var method = typeof(AdapterError).GetMethod("OperationFailed");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
        Assert.True(method.IsGenericMethod);
    }

    [Fact]
    public void AdapterError_HasCancelledFactory()
    {
        var method = typeof(AdapterError).GetMethod("Cancelled");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
        Assert.True(method.IsGenericMethod);
    }

    [Fact]
    public void AdapterError_HasNotFoundFactory()
    {
        var method = typeof(AdapterError).GetMethod("NotFound");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
        Assert.True(method.IsGenericMethod);
    }

    [Fact]
    public void AdapterError_HasCommunicationFailedFactory()
    {
        var method = typeof(AdapterError).GetMethod("CommunicationFailed");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
        Assert.True(method.IsGenericMethod);
    }

    [Fact]
    public void AdapterError_HasExternalErrorFactory()
    {
        var method = typeof(AdapterError).GetMethod("ExternalError");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
        Assert.True(method.IsGenericMethod);
    }

    // === PortRegistrationExtensions ===

    [Fact]
    public void PortRegistrationExtensions_IsStaticClass()
    {
        Assert.True(typeof(PortRegistrationExtensions).IsAbstract);
        Assert.True(typeof(PortRegistrationExtensions).IsSealed);
    }

    [Fact]
    public void PortRegistrationExtensions_HasAddPortWithTypeParams()
    {
        var methods = typeof(PortRegistrationExtensions).GetMethods()
            .Where(m => m.Name == "AddPort" && m.IsGenericMethod);

        Assert.NotEmpty(methods);
    }

    [Fact]
    public void PortRegistrationExtensions_HasAddPortWithFactory()
    {
        var methods = typeof(PortRegistrationExtensions).GetMethods()
            .Where(m => m.Name == "AddPort" && m.GetParameters().Any(p => p.ParameterType.Name.Contains("Func")));

        Assert.NotEmpty(methods);
    }

    [Fact]
    public void PortRegistrationExtensions_HasAddPortsFromAssembly()
    {
        var method = typeof(PortRegistrationExtensions).GetMethod("AddPortsFromAssembly");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void PortRegistrationExtensions_HasAddPortsFromAssemblies()
    {
        var method = typeof(PortRegistrationExtensions).GetMethod("AddPortsFromAssemblies");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void AddPort_ReturnsIServiceCollection()
    {
        var method = typeof(PortRegistrationExtensions).GetMethods()
            .First(m => m.Name == "AddPort");

        Assert.Equal(typeof(IServiceCollection), method.ReturnType);
    }
}

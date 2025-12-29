using System.Reflection;
using Encina.DomainModeling;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.DomainModeling.ContractTests;

/// <summary>
/// Contract tests verifying the public API of bounded context patterns.
/// </summary>
public class BoundedContextContracts
{
    // === BoundedContextAttribute ===

    [Fact]
    public void BoundedContextAttribute_IsAttribute()
    {
        Assert.True(typeof(BoundedContextAttribute).IsSubclassOf(typeof(Attribute)));
    }

    [Fact]
    public void BoundedContextAttribute_HasContextNameProperty()
    {
        var property = typeof(BoundedContextAttribute).GetProperty("ContextName");
        Assert.NotNull(property);
        Assert.Equal(typeof(string), property.PropertyType);
    }

    [Fact]
    public void BoundedContextAttribute_HasDescriptionProperty()
    {
        var property = typeof(BoundedContextAttribute).GetProperty("Description");
        Assert.NotNull(property);
    }

    [Fact]
    public void BoundedContextAttribute_CanBeAppliedToClasses()
    {
        var usage = typeof(BoundedContextAttribute).GetCustomAttribute<AttributeUsageAttribute>();
        Assert.NotNull(usage);
        Assert.True((usage.ValidOn & AttributeTargets.Class) != 0);
    }

    [Fact]
    public void BoundedContextAttribute_CanBeAppliedToInterfaces()
    {
        var usage = typeof(BoundedContextAttribute).GetCustomAttribute<AttributeUsageAttribute>();
        Assert.NotNull(usage);
        Assert.True((usage.ValidOn & AttributeTargets.Interface) != 0);
    }

    // === ContextRelationship Enum ===

    [Fact]
    public void ContextRelationship_IsEnum()
    {
        Assert.True(typeof(ContextRelationship).IsEnum);
    }

    [Fact]
    public void ContextRelationship_HasExpectedValues()
    {
        var values = Enum.GetNames<ContextRelationship>();
        Assert.Contains("Conformist", values);
        Assert.Contains("AntiCorruptionLayer", values);
        Assert.Contains("SharedKernel", values);
        Assert.Contains("CustomerSupplier", values);
        Assert.Contains("Partnership", values);
        Assert.Contains("PublishedLanguage", values);
        Assert.Contains("SeparateWays", values);
        Assert.Contains("OpenHostService", values);
    }

    // === ContextRelation Record ===

    [Fact]
    public void ContextRelation_IsRecord()
    {
        Assert.True(typeof(ContextRelation).IsClass);
        var equalityContract = typeof(ContextRelation).GetProperty("EqualityContract", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(equalityContract);
    }

    [Fact]
    public void ContextRelation_HasRequiredProperties()
    {
        var properties = typeof(ContextRelation).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        Assert.Contains(properties, p => p.Name == "UpstreamContext");
        Assert.Contains(properties, p => p.Name == "DownstreamContext");
        Assert.Contains(properties, p => p.Name == "Relationship");
        Assert.Contains(properties, p => p.Name == "Description");
    }

    // === ContextMap Class ===

    [Fact]
    public void ContextMap_IsClass()
    {
        Assert.True(typeof(ContextMap).IsClass);
        Assert.True(typeof(ContextMap).IsSealed);
    }

    [Fact]
    public void ContextMap_HasRelationsProperty()
    {
        var property = typeof(ContextMap).GetProperty("Relations");
        Assert.NotNull(property);
    }

    [Fact]
    public void ContextMap_HasAddRelationMethod()
    {
        var method = typeof(ContextMap).GetMethod("AddRelation");
        Assert.NotNull(method);
        Assert.Equal(typeof(ContextMap), method.ReturnType);
    }

    [Fact]
    public void ContextMap_HasAddSharedKernelMethod()
    {
        var method = typeof(ContextMap).GetMethod("AddSharedKernel");
        Assert.NotNull(method);
        Assert.Equal(typeof(ContextMap), method.ReturnType);
    }

    [Fact]
    public void ContextMap_HasAddCustomerSupplierMethod()
    {
        var method = typeof(ContextMap).GetMethod("AddCustomerSupplier");
        Assert.NotNull(method);
        Assert.Equal(typeof(ContextMap), method.ReturnType);
    }

    [Fact]
    public void ContextMap_HasAddPublishedLanguageMethod()
    {
        var method = typeof(ContextMap).GetMethod("AddPublishedLanguage");
        Assert.NotNull(method);
        Assert.Equal(typeof(ContextMap), method.ReturnType);
    }

    [Fact]
    public void ContextMap_HasGetContextNamesMethod()
    {
        var method = typeof(ContextMap).GetMethod("GetContextNames");
        Assert.NotNull(method);
    }

    [Fact]
    public void ContextMap_HasGetRelationsForMethod()
    {
        var method = typeof(ContextMap).GetMethod("GetRelationsFor");
        Assert.NotNull(method);
    }

    [Fact]
    public void ContextMap_HasToMermaidDiagramMethod()
    {
        var method = typeof(ContextMap).GetMethod("ToMermaidDiagram");
        Assert.NotNull(method);
        Assert.Equal(typeof(string), method.ReturnType);
    }

    // === BoundedContextModule Class ===

    [Fact]
    public void BoundedContextModule_IsAbstractClass()
    {
        Assert.True(typeof(BoundedContextModule).IsAbstract);
        Assert.True(typeof(BoundedContextModule).IsClass);
    }

    [Fact]
    public void BoundedContextModule_HasContextNameProperty()
    {
        var property = typeof(BoundedContextModule).GetProperty("ContextName");
        Assert.NotNull(property);
        Assert.True(property.GetMethod?.IsAbstract ?? false);
    }

    [Fact]
    public void BoundedContextModule_HasDescriptionProperty()
    {
        var property = typeof(BoundedContextModule).GetProperty("Description");
        Assert.NotNull(property);
        Assert.True(property.GetMethod?.IsVirtual ?? false);
    }

    [Fact]
    public void BoundedContextModule_HasConfigureMethod()
    {
        var method = typeof(BoundedContextModule).GetMethod("Configure");
        Assert.NotNull(method);
        Assert.True(method.IsAbstract);
    }

    // === IBoundedContextModule Interface ===

    [Fact]
    public void IBoundedContextModule_IsInterface()
    {
        Assert.True(typeof(IBoundedContextModule).IsInterface);
    }

    [Fact]
    public void IBoundedContextModule_HasContextNameProperty()
    {
        var property = typeof(IBoundedContextModule).GetProperty("ContextName");
        Assert.NotNull(property);
    }

    [Fact]
    public void IBoundedContextModule_HasPublishedIntegrationEventsProperty()
    {
        var property = typeof(IBoundedContextModule).GetProperty("PublishedIntegrationEvents");
        Assert.NotNull(property);
    }

    [Fact]
    public void IBoundedContextModule_HasConsumedIntegrationEventsProperty()
    {
        var property = typeof(IBoundedContextModule).GetProperty("ConsumedIntegrationEvents");
        Assert.NotNull(property);
    }

    [Fact]
    public void IBoundedContextModule_HasExposedPortsProperty()
    {
        var property = typeof(IBoundedContextModule).GetProperty("ExposedPorts");
        Assert.NotNull(property);
    }

    [Fact]
    public void IBoundedContextModule_HasConfigureMethod()
    {
        var method = typeof(IBoundedContextModule).GetMethod("Configure");
        Assert.NotNull(method);
    }

    // === BoundedContextError Record ===

    [Fact]
    public void BoundedContextError_IsRecord()
    {
        Assert.True(typeof(BoundedContextError).IsClass);
        var equalityContract = typeof(BoundedContextError).GetProperty("EqualityContract", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(equalityContract);
    }

    [Fact]
    public void BoundedContextError_HasRequiredProperties()
    {
        var properties = typeof(BoundedContextError).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        Assert.Contains(properties, p => p.Name == "Message");
        Assert.Contains(properties, p => p.Name == "ErrorCode");
        Assert.Contains(properties, p => p.Name == "ContextName");
        Assert.Contains(properties, p => p.Name == "Details");
    }

    [Fact]
    public void BoundedContextError_HasOrphanedConsumerFactory()
    {
        var method = typeof(BoundedContextError).GetMethod("OrphanedConsumer");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void BoundedContextError_HasCircularDependencyFactory()
    {
        var method = typeof(BoundedContextError).GetMethod("CircularDependency");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void BoundedContextError_HasValidationFailedFactory()
    {
        var method = typeof(BoundedContextError).GetMethod("ValidationFailed");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    // === BoundedContextValidator Class ===

    [Fact]
    public void BoundedContextValidator_IsClass()
    {
        Assert.True(typeof(BoundedContextValidator).IsClass);
        Assert.True(typeof(BoundedContextValidator).IsSealed);
    }

    [Fact]
    public void BoundedContextValidator_HasAddContextMethod()
    {
        var method = typeof(BoundedContextValidator).GetMethod("AddContext");
        Assert.NotNull(method);
        Assert.Equal(typeof(BoundedContextValidator), method.ReturnType);
    }

    [Fact]
    public void BoundedContextValidator_HasValidateEventContractsMethod()
    {
        var method = typeof(BoundedContextValidator).GetMethod("ValidateEventContracts");
        Assert.NotNull(method);
    }

    [Fact]
    public void BoundedContextValidator_HasGenerateContextMapMethod()
    {
        var method = typeof(BoundedContextValidator).GetMethod("GenerateContextMap");
        Assert.NotNull(method);
        Assert.Equal(typeof(ContextMap), method.ReturnType);
    }

    // === BoundedContextExtensions ===

    [Fact]
    public void BoundedContextExtensions_IsStaticClass()
    {
        Assert.True(typeof(BoundedContextExtensions).IsAbstract);
        Assert.True(typeof(BoundedContextExtensions).IsSealed);
    }

    [Fact]
    public void BoundedContextExtensions_HasAddBoundedContextGenericMethod()
    {
        var methods = typeof(BoundedContextExtensions).GetMethods()
            .Where(m => m.Name == "AddBoundedContext" && m.IsGenericMethod);
        Assert.NotEmpty(methods);
    }

    [Fact]
    public void BoundedContextExtensions_HasAddBoundedContextsFromAssemblyMethod()
    {
        var method = typeof(BoundedContextExtensions).GetMethod("AddBoundedContextsFromAssembly");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void BoundedContextExtensions_HasGetBoundedContextNameMethod()
    {
        var method = typeof(BoundedContextExtensions).GetMethod("GetBoundedContextName");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void BoundedContextExtensions_HasGetTypesInBoundedContextMethod()
    {
        var method = typeof(BoundedContextExtensions).GetMethod("GetTypesInBoundedContext");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void AddBoundedContext_ReturnsIServiceCollection()
    {
        var method = typeof(BoundedContextExtensions).GetMethods()
            .First(m => m.Name == "AddBoundedContext");
        Assert.Equal(typeof(IServiceCollection), method.ReturnType);
    }
}

using System.Reflection;
using Encina.Modules;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.Tests.Modules;

public sealed class ModuleHandlerRegistryTests
{
    [Fact]
    public void Constructor_WithValidDescriptors_BuildsRegistry()
    {
        // Arrange
        var module = new OrderModule();
        var descriptors = new[] { new ModuleDescriptor(module, typeof(OrderModule).Assembly) };

        // Act
        var registry = new ModuleHandlerRegistry(descriptors);

        // Assert
        registry.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithNullDescriptors_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new ModuleHandlerRegistry(null!));
    }

    [Fact]
    public void GetModuleName_ForHandlerInModule_ReturnsModuleName()
    {
        // Arrange
        var module = new TestAssemblyModule();
        var descriptors = new[] { new ModuleDescriptor(module, typeof(TestAssemblyModule).Assembly) };
        var registry = new ModuleHandlerRegistry(descriptors);

        // Act
        var moduleName = registry.GetModuleName(typeof(TestHandler));

        // Assert
        moduleName.ShouldBe("TestAssembly");
    }

    [Fact]
    public void GetModuleName_ForHandlerNotInModule_ReturnsNull()
    {
        // Arrange
        var module = new OrderModule();
        var descriptors = new[] { new ModuleDescriptor(module, typeof(OrderModule).Assembly) };
        var registry = new ModuleHandlerRegistry(descriptors);

        // External type not in module assembly
        var externalType = typeof(string);

        // Act
        var moduleName = registry.GetModuleName(externalType);

        // Assert
        moduleName.ShouldBeNull();
    }

    [Fact]
    public void GetModuleName_WithNullType_ThrowsArgumentNullException()
    {
        // Arrange
        var module = new OrderModule();
        var descriptors = new[] { new ModuleDescriptor(module, typeof(OrderModule).Assembly) };
        var registry = new ModuleHandlerRegistry(descriptors);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => registry.GetModuleName(null!));
    }

    [Fact]
    public void GetModule_ForHandlerServiceInterface_ReturnsModule()
    {
        // Arrange
        var module = new TestAssemblyModule();
        var descriptors = new[] { new ModuleDescriptor(module, typeof(TestAssemblyModule).Assembly) };
        var registry = new ModuleHandlerRegistry(descriptors);

        var handlerServiceType = typeof(IRequestHandler<TestRequest, string>);

        // Act
        var result = registry.GetModule(handlerServiceType);

        // Assert
        result.ShouldBe(module);
    }

    [Fact]
    public void BelongsToModule_ByName_ReturnsTrue_WhenHandlerInModule()
    {
        // Arrange
        var module = new TestAssemblyModule();
        var descriptors = new[] { new ModuleDescriptor(module, typeof(TestAssemblyModule).Assembly) };
        var registry = new ModuleHandlerRegistry(descriptors);

        var handlerServiceType = typeof(IRequestHandler<TestRequest, string>);

        // Act
        var result = registry.BelongsToModule(handlerServiceType, "TestAssembly");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void BelongsToModule_ByName_ReturnsFalse_WhenHandlerNotInModule()
    {
        // Arrange
        var module = new TestAssemblyModule();
        var descriptors = new[] { new ModuleDescriptor(module, typeof(TestAssemblyModule).Assembly) };
        var registry = new ModuleHandlerRegistry(descriptors);

        var handlerServiceType = typeof(IRequestHandler<TestRequest, string>);

        // Act
        var result = registry.BelongsToModule(handlerServiceType, "OtherModule");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void BelongsToModule_ByName_IsCaseInsensitive()
    {
        // Arrange
        var module = new TestAssemblyModule();
        var descriptors = new[] { new ModuleDescriptor(module, typeof(TestAssemblyModule).Assembly) };
        var registry = new ModuleHandlerRegistry(descriptors);

        var handlerServiceType = typeof(IRequestHandler<TestRequest, string>);

        // Act & Assert
        registry.BelongsToModule(handlerServiceType, "testassembly").ShouldBeTrue();
        registry.BelongsToModule(handlerServiceType, "TESTASSEMBLY").ShouldBeTrue();
        registry.BelongsToModule(handlerServiceType, "TestAssembly").ShouldBeTrue();
    }

    [Fact]
    public void BelongsToModule_ByType_ReturnsTrue_WhenCorrectModuleType()
    {
        // Arrange
        var module = new TestAssemblyModule();
        var descriptors = new[] { new ModuleDescriptor(module, typeof(TestAssemblyModule).Assembly) };
        var registry = new ModuleHandlerRegistry(descriptors);

        var handlerServiceType = typeof(IRequestHandler<TestRequest, string>);

        // Act
        var result = registry.BelongsToModule<TestAssemblyModule>(handlerServiceType);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void BelongsToModule_ByType_ReturnsFalse_WhenDifferentModuleType()
    {
        // Arrange
        var module = new TestAssemblyModule();
        var descriptors = new[] { new ModuleDescriptor(module, typeof(TestAssemblyModule).Assembly) };
        var registry = new ModuleHandlerRegistry(descriptors);

        var handlerServiceType = typeof(IRequestHandler<TestRequest, string>);

        // Act
        var result = registry.BelongsToModule<OrderModule>(handlerServiceType);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void BelongsToModule_WithNullHandlerType_ThrowsArgumentNullException()
    {
        // Arrange
        var module = new OrderModule();
        var descriptors = new[] { new ModuleDescriptor(module, typeof(OrderModule).Assembly) };
        var registry = new ModuleHandlerRegistry(descriptors);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => registry.BelongsToModule(null!, "Test"));
    }

    [Fact]
    public void BelongsToModule_WithNullModuleName_ThrowsArgumentException()
    {
        // Arrange
        var module = new OrderModule();
        var descriptors = new[] { new ModuleDescriptor(module, typeof(OrderModule).Assembly) };
        var registry = new ModuleHandlerRegistry(descriptors);

        // Act & Assert
        Should.Throw<ArgumentException>(() => registry.BelongsToModule(typeof(string), null!));
    }

    #region NullModuleHandlerRegistry Tests

    [Fact]
    public void NullRegistry_GetModuleName_AlwaysReturnsNull()
    {
        // Arrange
        var registry = NullModuleHandlerRegistry.Instance;

        // Act
        var result = registry.GetModuleName(typeof(string));

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void NullRegistry_GetModule_AlwaysReturnsNull()
    {
        // Arrange
        var registry = NullModuleHandlerRegistry.Instance;

        // Act
        var result = registry.GetModule(typeof(string));

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void NullRegistry_BelongsToModule_AlwaysReturnsFalse()
    {
        // Arrange
        var registry = NullModuleHandlerRegistry.Instance;

        // Act
        var result = registry.BelongsToModule(typeof(string), "AnyModule");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void NullRegistry_BelongsToModuleGeneric_AlwaysReturnsFalse()
    {
        // Arrange
        var registry = NullModuleHandlerRegistry.Instance;

        // Act
        var result = registry.BelongsToModule<OrderModule>(typeof(string));

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region Test Fixtures

    private sealed class OrderModule : IModule
    {
        public string Name => "Orders";
        public void ConfigureServices(IServiceCollection services) { }
    }

    public sealed class TestAssemblyModule : IModule
    {
        public string Name => "TestAssembly";
        public void ConfigureServices(IServiceCollection services) { }
    }

    #endregion
}

// These must be in the same assembly as the test module for the registry to find them
public sealed record TestRequest : IRequest<string>;

public sealed class TestHandler : IRequestHandler<TestRequest, string>
{
    public Task<Either<EncinaError, string>> Handle(
        TestRequest request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Right<EncinaError, string>("handled"));
    }
}

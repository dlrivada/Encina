using Encina.Modules;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.Tests.Modules;

public sealed class ModuleRegistryTests
{
    [Fact]
    public void Constructor_WithModules_RegistersAllModules()
    {
        // Arrange
        var modules = new IModule[]
        {
            new TestModuleA(),
            new TestModuleB()
        };

        // Act
        var registry = new ModuleRegistry(modules);

        // Assert
        registry.Modules.Count.ShouldBe(2);
        registry.ContainsModule("TestA").ShouldBeTrue();
        registry.ContainsModule("TestB").ShouldBeTrue();
    }

    [Fact]
    public void Constructor_WithNullModules_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new ModuleRegistry(null!));
    }

    [Fact]
    public void Constructor_WithDuplicateNames_ThrowsArgumentException()
    {
        // Arrange
        var modules = new IModule[]
        {
            new TestModuleA(),
            new DuplicateNameModuleA() // Same name as TestModuleA
        };

        // Act & Assert
        var ex = Should.Throw<ArgumentException>(() => new ModuleRegistry(modules));
        ex.Message.ShouldContain("Duplicate module names");
        ex.Message.ShouldContain("TestA");
    }

    [Fact]
    public void GetModule_ByName_ReturnsCorrectModule()
    {
        // Arrange
        var moduleA = new TestModuleA();
        var moduleB = new TestModuleB();
        var registry = new ModuleRegistry([moduleA, moduleB]);

        // Act
        var result = registry.GetModule("TestA");

        // Assert
        result.ShouldBe(moduleA);
    }

    [Fact]
    public void GetModule_ByName_IsCaseInsensitive()
    {
        // Arrange
        var moduleA = new TestModuleA();
        var registry = new ModuleRegistry([moduleA]);

        // Act & Assert
        registry.GetModule("testa").ShouldBe(moduleA);
        registry.GetModule("TESTA").ShouldBe(moduleA);
        registry.GetModule("TestA").ShouldBe(moduleA);
    }

    [Fact]
    public void GetModule_ByName_ReturnsNullForUnknownModule()
    {
        // Arrange
        var registry = new ModuleRegistry([new TestModuleA()]);

        // Act
        var result = registry.GetModule("Unknown");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetModule_ByName_ThrowsForNullName()
    {
        // Arrange
        var registry = new ModuleRegistry([new TestModuleA()]);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => registry.GetModule(null!));
    }

    [Fact]
    public void GetModule_ByType_ReturnsCorrectModule()
    {
        // Arrange
        var moduleA = new TestModuleA();
        var moduleB = new TestModuleB();
        var registry = new ModuleRegistry([moduleA, moduleB]);

        // Act
        var result = registry.GetModule<TestModuleA>();

        // Assert
        result.ShouldBe(moduleA);
    }

    [Fact]
    public void GetModule_ByType_ReturnsNullForUnregisteredType()
    {
        // Arrange
        var registry = new ModuleRegistry([new TestModuleA()]);

        // Act
        var result = registry.GetModule<TestModuleB>();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void ContainsModule_ReturnsTrue_WhenModuleExists()
    {
        // Arrange
        var registry = new ModuleRegistry([new TestModuleA()]);

        // Act & Assert
        registry.ContainsModule("TestA").ShouldBeTrue();
    }

    [Fact]
    public void ContainsModule_ReturnsFalse_WhenModuleDoesNotExist()
    {
        // Arrange
        var registry = new ModuleRegistry([new TestModuleA()]);

        // Act & Assert
        registry.ContainsModule("Unknown").ShouldBeFalse();
    }

    [Fact]
    public void ContainsModule_ThrowsForNullName()
    {
        // Arrange
        var registry = new ModuleRegistry([new TestModuleA()]);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => registry.ContainsModule(null!));
    }

    [Fact]
    public void GetLifecycleModules_ReturnsOnlyLifecycleModules()
    {
        // Arrange
        var lifecycleModule = new TestLifecycleModule();
        var regularModule = new TestModuleA();
        var registry = new ModuleRegistry([lifecycleModule, regularModule]);

        // Act
        var result = registry.GetLifecycleModules();

        // Assert
        result.Count.ShouldBe(1);
        result[0].ShouldBe(lifecycleModule);
    }

    [Fact]
    public void GetLifecycleModules_ReturnsEmptyList_WhenNoLifecycleModules()
    {
        // Arrange
        var registry = new ModuleRegistry([new TestModuleA(), new TestModuleB()]);

        // Act
        var result = registry.GetLifecycleModules();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void Modules_PreservesRegistrationOrder()
    {
        // Arrange
        var moduleA = new TestModuleA();
        var moduleB = new TestModuleB();
        var lifecycleModule = new TestLifecycleModule();

        // Act
        var registry = new ModuleRegistry([moduleA, lifecycleModule, moduleB]);

        // Assert
        registry.Modules[0].ShouldBe(moduleA);
        registry.Modules[1].ShouldBe(lifecycleModule);
        registry.Modules[2].ShouldBe(moduleB);
    }

    #region Test Fixtures

    private sealed class TestModuleA : IModule
    {
        public string Name => "TestA";
        public void ConfigureServices(IServiceCollection services) { }
    }

    private sealed class TestModuleB : IModule
    {
        public string Name => "TestB";
        public void ConfigureServices(IServiceCollection services) { }
    }

    private sealed class DuplicateNameModuleA : IModule
    {
        public string Name => "TestA"; // Same name as TestModuleA
        public void ConfigureServices(IServiceCollection services) { }
    }

    private sealed class TestLifecycleModule : IModuleLifecycle
    {
        public string Name => "Lifecycle";
        public void ConfigureServices(IServiceCollection services) { }
        public Task OnStartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task OnStopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    #endregion
}

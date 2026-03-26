using Encina.Modules;

namespace Encina.UnitTests.Core.Modules;

/// <summary>
/// Unit tests for <see cref="ModuleRegistry"/>.
/// </summary>
public sealed class ModuleRegistryTests
{
    [Fact]
    public void Constructor_WithValidModules_RegistersAll()
    {
        // Arrange
        var module1 = CreateModule("module-a");
        var module2 = CreateModule("module-b");

        // Act
        var registry = new ModuleRegistry([module1, module2]);

        // Assert
        registry.Modules.Count.ShouldBe(2);
    }

    [Fact]
    public void Constructor_WithDuplicateNames_ThrowsArgumentException()
    {
        // Arrange
        var module1 = CreateModule("duplicate");
        var module2 = CreateModule("duplicate");

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new ModuleRegistry([module1, module2]));
        ex.Message.ShouldContain("Duplicate module names");
    }

    [Fact]
    public void Constructor_WithNullModules_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ModuleRegistry(null!));
    }

    [Fact]
    public void Constructor_WithEmpty_CreatesEmptyRegistry()
    {
        // Act
        var registry = new ModuleRegistry([]);

        // Assert
        registry.Modules.Count.ShouldBe(0);
    }

    [Fact]
    public void GetModule_ByName_ReturnsModule()
    {
        // Arrange
        var module = CreateModule("test-module");
        var registry = new ModuleRegistry([module]);

        // Act
        var result = registry.GetModule("test-module");

        // Assert
        result.ShouldNotBeNull();
        result!.Name.ShouldBe("test-module");
    }

    [Fact]
    public void GetModule_ByName_CaseInsensitive()
    {
        // Arrange
        var module = CreateModule("TestModule");
        var registry = new ModuleRegistry([module]);

        // Act
        var result = registry.GetModule("testmodule");

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    public void GetModule_ByName_WhenNotFound_ReturnsNull()
    {
        // Arrange
        var registry = new ModuleRegistry([]);

        // Act
        var result = registry.GetModule("nonexistent");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetModule_ByName_WithNullName_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new ModuleRegistry([]);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => registry.GetModule((string)null!));
    }

    [Fact]
    public void ContainsModule_WhenExists_ReturnsTrue()
    {
        // Arrange
        var module = CreateModule("exists");
        var registry = new ModuleRegistry([module]);

        // Act & Assert
        registry.ContainsModule("exists").ShouldBeTrue();
    }

    [Fact]
    public void ContainsModule_WhenNotExists_ReturnsFalse()
    {
        // Arrange
        var registry = new ModuleRegistry([]);

        // Act & Assert
        registry.ContainsModule("nope").ShouldBeFalse();
    }

    [Fact]
    public void ContainsModule_WithNullName_ThrowsArgumentNullException()
    {
        var registry = new ModuleRegistry([]);
        Assert.Throws<ArgumentNullException>(() => registry.ContainsModule(null!));
    }

    [Fact]
    public void GetLifecycleModules_ReturnsOnlyLifecycleModules()
    {
        // Arrange
        var regularModule = CreateModule("regular");
        var lifecycleModule = CreateLifecycleModule("lifecycle");
        var registry = new ModuleRegistry([regularModule, lifecycleModule]);

        // Act
        var result = registry.GetLifecycleModules();

        // Assert
        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("lifecycle");
    }

    [Fact]
    public void GetModule_ByType_ReturnsCorrectModule()
    {
        // Arrange
        var module = new ConcreteTestModule("typed-module");
        var registry = new ModuleRegistry([module]);

        // Act
        var result = registry.GetModule<ConcreteTestModule>();

        // Assert
        result.ShouldNotBeNull();
        result!.Name.ShouldBe("typed-module");
    }

    [Fact]
    public void GetModule_ByType_WhenNotRegistered_ReturnsNull()
    {
        // Arrange
        var registry = new ModuleRegistry([]);

        // Act
        var result = registry.GetModule<ConcreteTestModule>();

        // Assert
        result.ShouldBeNull();
    }

    private static IModule CreateModule(string name)
    {
        var module = Substitute.For<IModule>();
        module.Name.Returns(name);
        return module;
    }

    private static IModule CreateLifecycleModule(string name)
    {
        var module = Substitute.For<IModule, IModuleLifecycle>();
        module.Name.Returns(name);
        ((IModuleLifecycle)module).Name.Returns(name);
        return module;
    }

    internal sealed class ConcreteTestModule : IModule
    {
        public ConcreteTestModule(string name) => Name = name;
        public string Name { get; }
        public void ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services) { }
    }
}

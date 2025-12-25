using Encina.Modules;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.Tests.Modules;

public sealed class ModuleConfigurationTests
{
    [Fact]
    public void AddModule_ByType_AddsModuleToConfiguration()
    {
        // Arrange
        var config = new ModuleConfiguration();

        // Act
        config.AddModule<TestModuleA>();

        // Assert
        config.ModuleDescriptors.Count.ShouldBe(1);
        config.ModuleDescriptors[0].Module.Name.ShouldBe("TestA");
    }

    [Fact]
    public void AddModule_ByInstance_AddsModuleToConfiguration()
    {
        // Arrange
        var config = new ModuleConfiguration();
        var module = new TestModuleA();

        // Act
        config.AddModule(module);

        // Assert
        config.ModuleDescriptors.Count.ShouldBe(1);
        config.ModuleDescriptors[0].Module.ShouldBe(module);
    }

    [Fact]
    public void AddModule_WithNullInstance_ThrowsArgumentNullException()
    {
        // Arrange
        var config = new ModuleConfiguration();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => config.AddModule(null!));
    }

    [Fact]
    public void AddModule_WithDuplicateName_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = new ModuleConfiguration();
        config.AddModule<TestModuleA>();

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => config.AddModule(new DuplicateNameModuleA()));
        ex.Message.ShouldContain("TestA");
        ex.Message.ShouldContain("already registered");
    }

    [Fact]
    public void AddModule_ChainsCalls()
    {
        // Arrange
        var config = new ModuleConfiguration();

        // Act
        var result = config
            .AddModule<TestModuleA>()
            .AddModule<TestModuleB>();

        // Assert
        result.ShouldBe(config);
        config.ModuleDescriptors.Count.ShouldBe(2);
    }

    [Fact]
    public void AddModule_WithAssembly_UsesProvidedAssembly()
    {
        // Arrange
        var config = new ModuleConfiguration();
        var module = new TestModuleA();
        var assembly = typeof(ModuleConfigurationTests).Assembly;

        // Act
        config.AddModule(module, assembly);

        // Assert
        config.ModuleDescriptors[0].HandlerAssembly.ShouldBe(assembly);
    }

    [Fact]
    public void AddModule_WithNullAssembly_UsesModuleAssembly()
    {
        // Arrange
        var config = new ModuleConfiguration();
        var module = new TestModuleA();

        // Act
        config.AddModule(module, null);

        // Assert
        config.ModuleDescriptors[0].HandlerAssembly.ShouldBe(typeof(TestModuleA).Assembly);
    }

    [Fact]
    public void WithoutHandlerDiscovery_DisablesDiscovery()
    {
        // Arrange
        var config = new ModuleConfiguration();
        config.EnableHandlerDiscovery.ShouldBeTrue(); // Default

        // Act
        var result = config.WithoutHandlerDiscovery();

        // Assert
        result.ShouldBe(config);
        config.EnableHandlerDiscovery.ShouldBeFalse();
    }

    [Fact]
    public void ModuleDescriptors_DefaultsToEmpty()
    {
        // Arrange & Act
        var config = new ModuleConfiguration();

        // Assert
        config.ModuleDescriptors.ShouldBeEmpty();
    }

    [Fact]
    public void EnableHandlerDiscovery_DefaultsToTrue()
    {
        // Arrange & Act
        var config = new ModuleConfiguration();

        // Assert
        config.EnableHandlerDiscovery.ShouldBeTrue();
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

    #endregion
}

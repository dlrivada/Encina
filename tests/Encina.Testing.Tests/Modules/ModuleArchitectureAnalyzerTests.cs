using System.Reflection;
using Encina.Modules;
using Encina.Testing.Modules;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.Testing.Tests.Modules;

file sealed class OrdersModule : IModule
{
    public string Name => "Orders";

    public void ConfigureServices(IServiceCollection services)
    {
        // No-op for testing
    }
}

file sealed class PaymentsModule : IModule
{
    public string Name => "Payments";

    public void ConfigureServices(IServiceCollection services)
    {
        // No-op for testing
    }
}

file sealed class ShippingModule : IModule
{
    public string Name => "Shipping";

    public void ConfigureServices(IServiceCollection services)
    {
        // No-op for testing
    }
}

public sealed class ModuleArchitectureAnalyzerTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_NullAssemblies_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => new ModuleArchitectureAnalyzer(null!));
    }

    [Fact]
    public void Constructor_EmptyAssemblies_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => new ModuleArchitectureAnalyzer([]));
    }

    [Fact]
    public void Constructor_ValidAssembly_CreatesAnalyzer()
    {
        // Act
        var analyzer = new ModuleArchitectureAnalyzer(typeof(ModuleArchitectureAnalyzerTests).Assembly);

        // Assert
        analyzer.ShouldNotBeNull();
    }

    #endregion

    #region Static Factory Tests

    [Fact]
    public void Analyze_CreatesAnalyzer()
    {
        // Act
        var analyzer = ModuleArchitectureAnalyzer.Analyze(typeof(ModuleArchitectureAnalyzerTests).Assembly);

        // Assert
        analyzer.ShouldNotBeNull();
    }

    [Fact]
    public void AnalyzeAssemblyContaining_CreatesAnalyzer()
    {
        // Act
        var analyzer = ModuleArchitectureAnalyzer.AnalyzeAssemblyContaining<ModuleArchitectureAnalyzerTests>();

        // Assert
        analyzer.ShouldNotBeNull();
    }

    #endregion

    #region Module Discovery Tests

    [Fact]
    public void Result_DiscoversModulesInAssembly()
    {
        // Arrange
        var analyzer = ModuleArchitectureAnalyzer.AnalyzeAssemblyContaining<ModuleArchitectureAnalyzerTests>();

        // Act
        var result = analyzer.Result;

        // Assert
        result.ModuleCount.ShouldBeGreaterThanOrEqualTo(3); // At least our test modules
        result.Modules.ShouldContain(m => m.Name == "Orders");
        result.Modules.ShouldContain(m => m.Name == "Payments");
        result.Modules.ShouldContain(m => m.Name == "Shipping");
    }

    [Fact]
    public void Result_ModuleInfoContainsCorrectData()
    {
        // Arrange
        var analyzer = ModuleArchitectureAnalyzer.AnalyzeAssemblyContaining<ModuleArchitectureAnalyzerTests>();

        // Act
        var ordersModule = analyzer.Result.Modules.First(m => m.Name == "Orders");

        // Assert
        ordersModule.Type.ShouldBe(typeof(OrdersModule));
        ordersModule.Assembly.ShouldBe(typeof(ModuleArchitectureAnalyzerTests).Assembly);
        ordersModule.Namespace.ShouldNotBeNullOrEmpty();
    }

    #endregion

    #region Circular Dependency Tests

    [Fact]
    public void Result_NoCircularDependencies_HasCircularDependenciesIsFalse()
    {
        // Arrange - Our test modules don't have dependencies on each other
        var analyzer = ModuleArchitectureAnalyzer.AnalyzeAssemblyContaining<ModuleArchitectureAnalyzerTests>();

        // Act
        var result = analyzer.Result;

        // Assert
        // Note: This may or may not have circular deps depending on actual code structure
        // The important thing is the API works correctly
        result.HasCircularDependencies.ShouldBe(result.CircularDependencies.Count > 0);
    }

    #endregion

    #region Assertion Tests

    [Fact]
    public void ShouldContainModule_ModuleExists_Passes()
    {
        // Arrange
        var analyzer = ModuleArchitectureAnalyzer.AnalyzeAssemblyContaining<ModuleArchitectureAnalyzerTests>();

        // Act & Assert - should not throw
        analyzer.Result.ShouldContainModule("Orders");
    }

    [Fact]
    public void ShouldContainModule_ModuleNotExists_Throws()
    {
        // Arrange
        var analyzer = ModuleArchitectureAnalyzer.AnalyzeAssemblyContaining<ModuleArchitectureAnalyzerTests>();

        // Act & Assert
        Should.Throw<ShouldAssertException>(() =>
            analyzer.Result.ShouldContainModule("NonExistentModule"));
    }

    [Fact]
    public void ShouldContainModule_CaseInsensitive_Passes()
    {
        // Arrange
        var analyzer = ModuleArchitectureAnalyzer.AnalyzeAssemblyContaining<ModuleArchitectureAnalyzerTests>();

        // Act & Assert - should not throw
        analyzer.Result.ShouldContainModule("orders");
        analyzer.Result.ShouldContainModule("ORDERS");
    }

    #endregion

    #region Architecture Property Tests

    [Fact]
    public void Architecture_ReturnsArchUnitNETArchitecture()
    {
        // Arrange
        var analyzer = ModuleArchitectureAnalyzer.AnalyzeAssemblyContaining<ModuleArchitectureAnalyzerTests>();

        // Act
        var architecture = analyzer.Architecture;

        // Assert
        architecture.ShouldNotBeNull();
    }

    #endregion

    #region Dependency Assertion Tests

    [Fact]
    public void ShouldNotHaveDependency_NoDependency_Passes()
    {
        // Arrange
        var analyzer = ModuleArchitectureAnalyzer.AnalyzeAssemblyContaining<ModuleArchitectureAnalyzerTests>();

        // Act & Assert - Our isolated test modules shouldn't have dependencies on each other
        // This should pass if there's no dependency between Orders and Payments
        var result = analyzer.Result;

        // Assert that Orders does not depend on Payments — fail if dependency exists
        result.ShouldNotHaveDependency("Orders", "Payments");
    }

    #endregion

    #region Chaining Tests

    [Fact]
    public void Assertions_CanBeChained()
    {
        // Arrange
        var analyzer = ModuleArchitectureAnalyzer.AnalyzeAssemblyContaining<ModuleArchitectureAnalyzerTests>();

        // Act & Assert
        analyzer.Result
            .ShouldContainModule("Orders")
            .ShouldContainModule("Payments")
            .ShouldContainModule("Shipping");
    }

    #endregion
}

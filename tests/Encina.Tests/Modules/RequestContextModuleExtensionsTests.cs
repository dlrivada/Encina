using Encina.Modules;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.Tests.Modules;

public sealed class RequestContextModuleExtensionsTests
{
    [Fact]
    public void GetModuleName_WhenModuleNameSet_ReturnsName()
    {
        // Arrange
        var context = RequestContext.Create().WithModuleName("Orders");

        // Act
        var result = context.GetModuleName();

        // Assert
        result.ShouldBe("Orders");
    }

    [Fact]
    public void GetModuleName_WhenNoModuleName_ReturnsNull()
    {
        // Arrange
        var context = RequestContext.Create();

        // Act
        var result = context.GetModuleName();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetModuleName_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        IRequestContext context = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => context.GetModuleName());
    }

    [Fact]
    public void WithModuleName_String_SetsModuleName()
    {
        // Arrange
        var context = RequestContext.Create();

        // Act
        var result = context.WithModuleName("Orders");

        // Assert
        result.GetModuleName().ShouldBe("Orders");
    }

    [Fact]
    public void WithModuleName_String_CreatesNewContext()
    {
        // Arrange
        var original = RequestContext.Create();

        // Act
        var modified = original.WithModuleName("Orders");

        // Assert
        original.GetModuleName().ShouldBeNull();
        modified.GetModuleName().ShouldBe("Orders");
    }

    [Fact]
    public void WithModuleName_String_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        IRequestContext context = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => context.WithModuleName("Orders"));
    }

    [Fact]
    public void WithModuleName_String_WithNullName_ThrowsArgumentException()
    {
        // Arrange
        var context = RequestContext.Create();

        // Act & Assert
        Should.Throw<ArgumentException>(() => context.WithModuleName((string)null!));
    }

    [Fact]
    public void WithModuleName_String_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var context = RequestContext.Create();

        // Act & Assert
        Should.Throw<ArgumentException>(() => context.WithModuleName(""));
    }

    [Fact]
    public void WithModuleName_String_WithWhitespaceName_ThrowsArgumentException()
    {
        // Arrange
        var context = RequestContext.Create();

        // Act & Assert
        Should.Throw<ArgumentException>(() => context.WithModuleName("   "));
    }

    [Fact]
    public void WithModuleName_Module_SetsModuleName()
    {
        // Arrange
        var context = RequestContext.Create();
        var module = new TestModule();

        // Act
        var result = context.WithModuleName(module);

        // Assert
        result.GetModuleName().ShouldBe("TestModule");
    }

    [Fact]
    public void WithModuleName_Module_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        IRequestContext context = null!;
        var module = new TestModule();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => context.WithModuleName(module));
    }

    [Fact]
    public void WithModuleName_Module_WithNullModule_ThrowsArgumentNullException()
    {
        // Arrange
        var context = RequestContext.Create();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => context.WithModuleName((IModule)null!));
    }

    [Fact]
    public void IsInModule_String_ReturnsTrue_WhenModuleMatches()
    {
        // Arrange
        var context = RequestContext.Create().WithModuleName("Orders");

        // Act
        var result = context.IsInModule("Orders");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsInModule_String_ReturnsFalse_WhenModuleDoesNotMatch()
    {
        // Arrange
        var context = RequestContext.Create().WithModuleName("Orders");

        // Act
        var result = context.IsInModule("Payments");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsInModule_String_ReturnsFalse_WhenNoModuleSet()
    {
        // Arrange
        var context = RequestContext.Create();

        // Act
        var result = context.IsInModule("Orders");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsInModule_String_IsCaseInsensitive()
    {
        // Arrange
        var context = RequestContext.Create().WithModuleName("Orders");

        // Act & Assert
        context.IsInModule("orders").ShouldBeTrue();
        context.IsInModule("ORDERS").ShouldBeTrue();
        context.IsInModule("Orders").ShouldBeTrue();
    }

    [Fact]
    public void IsInModule_String_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        IRequestContext context = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => context.IsInModule("Orders"));
    }

    [Fact]
    public void IsInModule_String_WithNullModuleName_ThrowsArgumentException()
    {
        // Arrange
        var context = RequestContext.Create();

        // Act & Assert
        Should.Throw<ArgumentException>(() => context.IsInModule((string)null!));
    }

    [Fact]
    public void IsInModule_Module_ReturnsTrue_WhenModuleMatches()
    {
        // Arrange
        var module = new TestModule();
        var context = RequestContext.Create().WithModuleName(module);

        // Act
        var result = context.IsInModule(module);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsInModule_Module_ReturnsFalse_WhenModuleDoesNotMatch()
    {
        // Arrange
        var module = new TestModule();
        var otherModule = new OtherModule();
        var context = RequestContext.Create().WithModuleName(module);

        // Act
        var result = context.IsInModule(otherModule);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsInModule_Module_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        IRequestContext context = null!;
        var module = new TestModule();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => context.IsInModule(module));
    }

    [Fact]
    public void IsInModule_Module_WithNullModule_ThrowsArgumentNullException()
    {
        // Arrange
        var context = RequestContext.Create();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => context.IsInModule<TestModule>(null!));
    }

    #region Test Fixtures

    private sealed class TestModule : IModule
    {
        public string Name => "TestModule";
        public void ConfigureServices(IServiceCollection services) { }
    }

    private sealed class OtherModule : IModule
    {
        public string Name => "OtherModule";
        public void ConfigureServices(IServiceCollection services) { }
    }

    #endregion
}

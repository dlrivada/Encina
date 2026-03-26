using Encina.Modules;

namespace Encina.UnitTests.Core.Modules;

/// <summary>
/// Unit tests for <see cref="NullModuleHandlerRegistry"/>.
/// </summary>
public sealed class NullModuleHandlerRegistryTests
{
    private readonly NullModuleHandlerRegistry _sut = NullModuleHandlerRegistry.Instance;

    [Fact]
    public void Instance_ReturnsSingleton()
    {
        // Assert
        NullModuleHandlerRegistry.Instance.ShouldBeSameAs(_sut);
    }

    [Fact]
    public void GetModuleName_AlwaysReturnsNull()
    {
        // Act
        var result = _sut.GetModuleName(typeof(string));

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetModuleName_WithNullType_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _sut.GetModuleName(null!));
    }

    [Fact]
    public void GetModule_AlwaysReturnsNull()
    {
        // Act
        var result = _sut.GetModule(typeof(string));

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetModule_WithNullType_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _sut.GetModule(null!));
    }

    [Fact]
    public void BelongsToModule_ByName_AlwaysReturnsFalse()
    {
        // Act
        var result = _sut.BelongsToModule(typeof(string), "any-module");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void BelongsToModule_ByName_WithNullType_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _sut.BelongsToModule(null!, "module"));
    }

    [Fact]
    public void BelongsToModule_ByName_WithNullName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            _sut.BelongsToModule(typeof(string), null!));
    }

    [Fact]
    public void BelongsToModule_ByType_AlwaysReturnsFalse()
    {
        // Act
        var result = _sut.BelongsToModule<TestModuleForNull>(typeof(string));

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void BelongsToModule_ByType_WithNullHandlerType_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _sut.BelongsToModule<TestModuleForNull>(null!));
    }

    internal sealed class TestModuleForNull : IModule
    {
        public string Name => "test";
        public void ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services) { }
    }
}

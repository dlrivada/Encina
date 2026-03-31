using System.Reflection;
using Encina.Modules;

namespace Encina.GuardTests.Core.Modules;

/// <summary>
/// Guard tests for <see cref="ModuleHandlerRegistry"/> and <see cref="NullModuleHandlerRegistry"/>
/// to verify null parameter handling across all public methods.
/// </summary>
public class ModuleHandlerRegistryGuardTests
{
    // ---- ModuleHandlerRegistry constructor ----

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when moduleDescriptors is null.
    /// </summary>
    [Fact]
    public void Constructor_NullModuleDescriptors_ThrowsArgumentNullException()
    {
        var act = () => new ModuleHandlerRegistry(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("moduleDescriptors");
    }

    /// <summary>
    /// Verifies that the constructor accepts an empty collection without throwing.
    /// </summary>
    [Fact]
    public void Constructor_EmptyModuleDescriptors_DoesNotThrow()
    {
        var act = () => new ModuleHandlerRegistry(Enumerable.Empty<ModuleDescriptor>());
        Should.NotThrow(act);
    }

    // ---- GetModuleName ----

    /// <summary>
    /// Verifies that GetModuleName throws ArgumentNullException when handlerType is null.
    /// </summary>
    [Fact]
    public void GetModuleName_NullHandlerType_ThrowsArgumentNullException()
    {
        var registry = CreateEmptyRegistry();
        var act = () => registry.GetModuleName(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("handlerType");
    }

    /// <summary>
    /// Verifies that GetModuleName returns null for an unregistered type.
    /// </summary>
    [Fact]
    public void GetModuleName_UnregisteredType_ReturnsNull()
    {
        var registry = CreateEmptyRegistry();
        registry.GetModuleName(typeof(string)).ShouldBeNull();
    }

    // ---- GetModule ----

    /// <summary>
    /// Verifies that GetModule throws ArgumentNullException when handlerType is null.
    /// </summary>
    [Fact]
    public void GetModule_NullHandlerType_ThrowsArgumentNullException()
    {
        var registry = CreateEmptyRegistry();
        var act = () => registry.GetModule(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("handlerType");
    }

    /// <summary>
    /// Verifies that GetModule returns null for an unregistered type.
    /// </summary>
    [Fact]
    public void GetModule_UnregisteredType_ReturnsNull()
    {
        var registry = CreateEmptyRegistry();
        registry.GetModule(typeof(string)).ShouldBeNull();
    }

    // ---- GetModuleForRequest ----

    /// <summary>
    /// Verifies that GetModuleForRequest throws ArgumentNullException when requestType is null.
    /// </summary>
    [Fact]
    public void GetModuleForRequest_NullRequestType_ThrowsArgumentNullException()
    {
        var registry = CreateEmptyRegistry();
        var act = () => registry.GetModuleForRequest(null!, typeof(string));
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("requestType");
    }

    /// <summary>
    /// Verifies that GetModuleForRequest throws ArgumentNullException when responseType is null.
    /// </summary>
    [Fact]
    public void GetModuleForRequest_NullResponseType_ThrowsArgumentNullException()
    {
        var registry = CreateEmptyRegistry();
        var act = () => registry.GetModuleForRequest(typeof(string), null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("responseType");
    }

    /// <summary>
    /// Verifies that GetModuleForRequest returns null for an unregistered request/response pair.
    /// </summary>
    [Fact]
    public void GetModuleForRequest_UnregisteredPair_ReturnsNull()
    {
        var registry = CreateEmptyRegistry();
        registry.GetModuleForRequest(typeof(string), typeof(int)).ShouldBeNull();
    }

    // ---- BelongsToModule (string) ----

    /// <summary>
    /// Verifies that BelongsToModule throws ArgumentNullException when handlerType is null.
    /// </summary>
    [Fact]
    public void BelongsToModule_NullHandlerType_ThrowsArgumentNullException()
    {
        var registry = CreateEmptyRegistry();
        Action act = () => registry.BelongsToModule(null!, "Orders");
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("handlerType");
    }

    /// <summary>
    /// Verifies that BelongsToModule throws ArgumentException when moduleName is null.
    /// </summary>
    [Fact]
    public void BelongsToModule_NullModuleName_ThrowsArgumentException()
    {
        var registry = CreateEmptyRegistry();
        Action act = () => registry.BelongsToModule(typeof(string), null!);
        Should.Throw<ArgumentException>(act);
    }

    /// <summary>
    /// Verifies that BelongsToModule throws ArgumentException when moduleName is whitespace.
    /// </summary>
    [Fact]
    public void BelongsToModule_WhitespaceModuleName_ThrowsArgumentException()
    {
        var registry = CreateEmptyRegistry();
        Action act = () => registry.BelongsToModule(typeof(string), "   ");
        Should.Throw<ArgumentException>(act);
    }

    /// <summary>
    /// Verifies that BelongsToModule returns false for an unregistered handler.
    /// </summary>
    [Fact]
    public void BelongsToModule_UnregisteredHandler_ReturnsFalse()
    {
        var registry = CreateEmptyRegistry();
        registry.BelongsToModule(typeof(string), "Orders").ShouldBeFalse();
    }

    // ---- BelongsToModule<TModule> ----

    /// <summary>
    /// Verifies that BelongsToModule generic throws ArgumentNullException when handlerType is null.
    /// </summary>
    [Fact]
    public void BelongsToModuleGeneric_NullHandlerType_ThrowsArgumentNullException()
    {
        var registry = CreateEmptyRegistry();
        Action act = () => registry.BelongsToModule<TestModule>(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("handlerType");
    }

    // ---- NullModuleHandlerRegistry ----

    /// <summary>
    /// Verifies that NullModuleHandlerRegistry.GetModuleName throws ArgumentNullException for null.
    /// </summary>
    [Fact]
    public void NullRegistry_GetModuleName_NullType_ThrowsArgumentNullException()
    {
        var act = () => NullModuleHandlerRegistry.Instance.GetModuleName(null!);
        Should.Throw<ArgumentNullException>(act);
    }

    /// <summary>
    /// Verifies that NullModuleHandlerRegistry.GetModule throws ArgumentNullException for null.
    /// </summary>
    [Fact]
    public void NullRegistry_GetModule_NullType_ThrowsArgumentNullException()
    {
        var act = () => NullModuleHandlerRegistry.Instance.GetModule(null!);
        Should.Throw<ArgumentNullException>(act);
    }

    /// <summary>
    /// Verifies that NullModuleHandlerRegistry.BelongsToModule throws ArgumentNullException for null handler.
    /// </summary>
    [Fact]
    public void NullRegistry_BelongsToModule_NullType_ThrowsArgumentNullException()
    {
        Action act = () => NullModuleHandlerRegistry.Instance.BelongsToModule(null!, "test");
        Should.Throw<ArgumentNullException>(act);
    }

    /// <summary>
    /// Verifies that NullModuleHandlerRegistry.BelongsToModule throws ArgumentException for null module name.
    /// </summary>
    [Fact]
    public void NullRegistry_BelongsToModule_NullModuleName_ThrowsArgumentException()
    {
        Action act = () => NullModuleHandlerRegistry.Instance.BelongsToModule(typeof(string), null!);
        Should.Throw<ArgumentException>(act);
    }

    /// <summary>
    /// Verifies that NullModuleHandlerRegistry always returns null/false for valid inputs.
    /// </summary>
    [Fact]
    public void NullRegistry_ValidInputs_ReturnsNullOrFalse()
    {
        var instance = NullModuleHandlerRegistry.Instance;
        instance.GetModuleName(typeof(string)).ShouldBeNull();
        instance.GetModule(typeof(string)).ShouldBeNull();
        instance.BelongsToModule(typeof(string), "Orders").ShouldBeFalse();
        instance.BelongsToModule<TestModule>(typeof(string)).ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that NullModuleHandlerRegistry.BelongsToModule generic throws for null.
    /// </summary>
    [Fact]
    public void NullRegistry_BelongsToModuleGeneric_NullType_ThrowsArgumentNullException()
    {
        Action act = () => NullModuleHandlerRegistry.Instance.BelongsToModule<TestModule>(null!);
        Should.Throw<ArgumentNullException>(act);
    }

    private static ModuleHandlerRegistry CreateEmptyRegistry()
    {
        return new ModuleHandlerRegistry(Enumerable.Empty<ModuleDescriptor>());
    }

    private sealed class TestModule : IModule
    {
        public string Name => "Test";
        public void ConfigureServices(IServiceCollection services) { }
    }
}

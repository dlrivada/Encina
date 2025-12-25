namespace Encina.Modules;

/// <summary>
/// Provides runtime access to registered modules.
/// </summary>
/// <remarks>
/// <para>
/// The module registry maintains a collection of all modules registered
/// in the application. It provides methods to query and access modules
/// at runtime.
/// </para>
/// <para>
/// This interface is registered as a singleton and can be injected
/// into services that need to interact with modules dynamically.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class ModuleInfoController : ControllerBase
/// {
///     private readonly IModuleRegistry _registry;
///
///     public ModuleInfoController(IModuleRegistry registry)
///     {
///         _registry = registry;
///     }
///
///     [HttpGet("modules")]
///     public IActionResult GetModules()
///     {
///         var modules = _registry.Modules.Select(m => new { m.Name });
///         return Ok(modules);
///     }
/// }
/// </code>
/// </example>
public interface IModuleRegistry
{
    /// <summary>
    /// Gets all registered modules.
    /// </summary>
    /// <value>A read-only collection of all modules in registration order.</value>
    IReadOnlyList<IModule> Modules { get; }

    /// <summary>
    /// Gets a module by its name.
    /// </summary>
    /// <param name="name">The unique name of the module.</param>
    /// <returns>
    /// The module with the specified name, or <c>null</c> if not found.
    /// </returns>
    IModule? GetModule(string name);

    /// <summary>
    /// Gets a module by its type.
    /// </summary>
    /// <typeparam name="TModule">The type of the module to retrieve.</typeparam>
    /// <returns>
    /// The module of the specified type, or <c>null</c> if not found.
    /// </returns>
    TModule? GetModule<TModule>() where TModule : class, IModule;

    /// <summary>
    /// Determines whether a module with the specified name is registered.
    /// </summary>
    /// <param name="name">The name of the module to check.</param>
    /// <returns>
    /// <c>true</c> if a module with the specified name is registered;
    /// otherwise, <c>false</c>.
    /// </returns>
    bool ContainsModule(string name);

    /// <summary>
    /// Gets all modules that implement lifecycle hooks.
    /// </summary>
    /// <returns>
    /// A read-only collection of modules that implement <see cref="IModuleLifecycle"/>.
    /// </returns>
    IReadOnlyList<IModuleLifecycle> GetLifecycleModules();
}

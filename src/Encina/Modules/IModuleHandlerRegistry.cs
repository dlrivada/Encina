namespace Encina.Modules;

/// <summary>
/// Provides mapping between handlers and their owning modules.
/// </summary>
/// <remarks>
/// <para>
/// This registry tracks which module each handler belongs to, enabling module-scoped
/// behaviors to determine if they should execute for a given handler.
/// </para>
/// <para>
/// The mapping is established during module registration based on the assembly
/// each handler was discovered from.
/// </para>
/// </remarks>
public interface IModuleHandlerRegistry
{
    /// <summary>
    /// Gets the module name that owns the specified handler type.
    /// </summary>
    /// <param name="handlerType">The type of the handler.</param>
    /// <returns>
    /// The name of the module that owns the handler, or <c>null</c> if the handler
    /// is not associated with any module (e.g., registered globally).
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="handlerType"/> is <c>null</c>.
    /// </exception>
    /// <example>
    /// <code>
    /// var moduleName = registry.GetModuleName(typeof(CreateOrderHandler));
    /// // Returns "Orders" if handler is in OrderModule
    /// </code>
    /// </example>
    string? GetModuleName(Type handlerType);

    /// <summary>
    /// Gets the module that owns the specified handler type.
    /// </summary>
    /// <param name="handlerType">The type of the handler.</param>
    /// <returns>
    /// The module that owns the handler, or <c>null</c> if the handler
    /// is not associated with any module.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="handlerType"/> is <c>null</c>.
    /// </exception>
    IModule? GetModule(Type handlerType);

    /// <summary>
    /// Determines whether a handler belongs to a specific module.
    /// </summary>
    /// <param name="handlerType">The type of the handler.</param>
    /// <param name="moduleName">The name of the module to check.</param>
    /// <returns>
    /// <c>true</c> if the handler belongs to the specified module; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="handlerType"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="moduleName"/> is <c>null</c> or whitespace.
    /// </exception>
    bool BelongsToModule(Type handlerType, string moduleName);

    /// <summary>
    /// Determines whether a handler belongs to a specific module.
    /// </summary>
    /// <typeparam name="TModule">The type of the module to check.</typeparam>
    /// <param name="handlerType">The type of the handler.</param>
    /// <returns>
    /// <c>true</c> if the handler belongs to the specified module type; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="handlerType"/> is <c>null</c>.
    /// </exception>
    bool BelongsToModule<TModule>(Type handlerType) where TModule : class, IModule;
}

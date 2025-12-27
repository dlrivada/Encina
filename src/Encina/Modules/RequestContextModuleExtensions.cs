namespace Encina.Modules;

/// <summary>
/// Extension methods for <see cref="IRequestContext"/> to work with module information.
/// </summary>
public static class RequestContextModuleExtensions
{
    /// <summary>
    /// The metadata key used to store the module name in the request context.
    /// </summary>
    private const string ModuleNameKey = "Encina.ModuleName";

    /// <summary>
    /// Gets the name of the module processing the current request.
    /// </summary>
    /// <param name="context">The request context.</param>
    /// <returns>
    /// The module name if set, or <c>null</c> if the request is not associated with a module.
    /// </returns>
    /// <example>
    /// <code>
    /// public ValueTask&lt;Either&lt;EncinaError, TResponse&gt;&gt; Handle(
    ///     TRequest request,
    ///     IRequestContext context,
    ///     RequestHandlerCallback&lt;TResponse&gt; nextStep,
    ///     CancellationToken cancellationToken)
    /// {
    ///     var moduleName = context.GetModuleName();
    ///     if (moduleName == "Orders")
    ///     {
    ///         // Module-specific logic
    ///     }
    ///     return nextStep();
    /// }
    /// </code>
    /// </example>
    public static string? GetModuleName(this IRequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Metadata.TryGetValue(ModuleNameKey, out var value) &&
            value is string moduleName)
        {
            return moduleName;
        }

        return null;
    }

    /// <summary>
    /// Creates a new context with the module name set.
    /// </summary>
    /// <param name="context">The request context.</param>
    /// <param name="moduleName">The name of the module processing the request.</param>
    /// <returns>A new context instance with the module name set.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="context"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="moduleName"/> is <c>null</c> or whitespace.
    /// </exception>
    /// <example>
    /// <code>
    /// // Set module name in context before processing
    /// var enrichedContext = context.WithModuleName("Orders");
    /// </code>
    /// </example>
    public static IRequestContext WithModuleName(this IRequestContext context, string moduleName)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleName);

        return context.WithMetadata(ModuleNameKey, moduleName);
    }

    /// <summary>
    /// Creates a new context with the module name set from a module instance.
    /// </summary>
    /// <param name="context">The request context.</param>
    /// <param name="module">The module instance to get the name from.</param>
    /// <returns>A new context instance with the module name set.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="context"/> or <paramref name="module"/> is <c>null</c>.
    /// </exception>
    /// <example>
    /// <code>
    /// // Set module name from module instance
    /// var enrichedContext = context.WithModuleName(orderModule);
    /// </code>
    /// </example>
    public static IRequestContext WithModuleName(this IRequestContext context, IModule module)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(module);

        return context.WithModuleName(module.Name);
    }

    /// <summary>
    /// Determines whether the request is being processed by a specific module.
    /// </summary>
    /// <param name="context">The request context.</param>
    /// <param name="moduleName">The module name to check.</param>
    /// <returns>
    /// <c>true</c> if the request is being processed by the specified module; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="context"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="moduleName"/> is <c>null</c> or whitespace.
    /// </exception>
    /// <example>
    /// <code>
    /// if (context.IsInModule("Orders"))
    /// {
    ///     // Execute order-specific logic
    /// }
    /// </code>
    /// </example>
    public static bool IsInModule(this IRequestContext context, string moduleName)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleName);

        var currentModule = context.GetModuleName();
        return string.Equals(currentModule, moduleName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the request is being processed by a specific module type.
    /// </summary>
    /// <typeparam name="TModule">The module type to check.</typeparam>
    /// <param name="context">The request context.</param>
    /// <param name="module">The module instance to compare against.</param>
    /// <returns>
    /// <c>true</c> if the request is being processed by the specified module; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="context"/> or <paramref name="module"/> is <c>null</c>.
    /// </exception>
    /// <example>
    /// <code>
    /// if (context.IsInModule(orderModule))
    /// {
    ///     // Execute order-specific logic
    /// }
    /// </code>
    /// </example>
    public static bool IsInModule<TModule>(this IRequestContext context, TModule module)
        where TModule : class, IModule
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(module);

        return context.IsInModule(module.Name);
    }
}

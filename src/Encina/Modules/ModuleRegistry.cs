namespace Encina.Modules;

/// <summary>
/// Default implementation of <see cref="IModuleRegistry"/>.
/// </summary>
/// <remarks>
/// This class maintains an immutable collection of registered modules
/// and provides efficient lookup by name and type.
/// </remarks>
internal sealed class ModuleRegistry : IModuleRegistry
{
    private readonly List<IModule> _modules;
    private readonly List<IModuleLifecycle> _lifecycleModules;
    private readonly Dictionary<string, IModule> _modulesByName;
    private readonly Dictionary<Type, IModule> _modulesByType;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleRegistry"/> class.
    /// </summary>
    /// <param name="modules">The modules to register.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="modules"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when duplicate module names are detected.
    /// </exception>
    public ModuleRegistry(IEnumerable<IModule> modules)
    {
        ArgumentNullException.ThrowIfNull(modules);

        var moduleList = modules.ToList();
        ValidateUniqueNames(moduleList);

        _modules = moduleList;
        _lifecycleModules = moduleList.OfType<IModuleLifecycle>().ToList();
        _modulesByName = new Dictionary<string, IModule>(StringComparer.OrdinalIgnoreCase);
        _modulesByType = [];

        foreach (var module in moduleList)
        {
            _modulesByName[module.Name] = module;
            _modulesByType[module.GetType()] = module;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<IModule> Modules => _modules;

    /// <inheritdoc />
    public IModule? GetModule(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _modulesByName.GetValueOrDefault(name);
    }

    /// <inheritdoc />
    public TModule? GetModule<TModule>() where TModule : class, IModule
    {
        return _modulesByType.GetValueOrDefault(typeof(TModule)) as TModule;
    }

    /// <inheritdoc />
    public bool ContainsModule(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _modulesByName.ContainsKey(name);
    }

    /// <inheritdoc />
    public IReadOnlyList<IModuleLifecycle> GetLifecycleModules() => _lifecycleModules;

    private static void ValidateUniqueNames(IReadOnlyList<IModule> modules)
    {
        var duplicates = modules
            .GroupBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Skip(1).Any())
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Count != 0)
        {
            throw new ArgumentException(
                $"Duplicate module names detected: {string.Join(", ", duplicates)}. " +
                "Each module must have a unique name.",
                nameof(modules));
        }
    }
}

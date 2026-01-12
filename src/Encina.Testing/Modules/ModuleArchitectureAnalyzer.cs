using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using Encina.Modules;
using Microsoft.Extensions.Logging;
using Shouldly;
using ReflectionAssembly = System.Reflection.Assembly;
using ReflectionBindingFlags = System.Reflection.BindingFlags;
using ReflectionType = System.Type;

namespace Encina.Testing.Modules;

/// <summary>
/// Analyzes module architecture for violations such as circular dependencies
/// and improper cross-module access.
/// </summary>
/// <remarks>
/// <para>
/// This analyzer provides comprehensive checks for modular monolith architecture:
/// <list type="bullet">
/// <item><description>Circular dependency detection between modules</description></item>
/// <item><description>Module boundary verification</description></item>
/// <item><description>Public API compliance checking</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = ModuleArchitectureAnalyzer.Analyze(typeof(Program).Assembly);
///
/// result.ShouldHaveNoCircularDependencies();
/// result.ModuleCount.ShouldBe(5);
/// </code>
/// </example>
public sealed class ModuleArchitectureAnalyzer
{
    private readonly ReflectionAssembly[] _assemblies;
    private readonly Lazy<ArchUnitNET.Domain.Architecture> _architecture;
    private readonly Lazy<ModuleAnalysisResult> _analysisResult;
    private readonly ILogger<ModuleArchitectureAnalyzer>? _logger;

    private static readonly Action<ILogger, string, string, Exception?> _failedToLoadTypes =
        LoggerMessage.Define<string, string>(LogLevel.Error,
            new EventId(1001, nameof(ModuleArchitectureAnalyzer) + ".FailedToLoadTypes"),
            "ModuleArchitectureAnalyzer: failed to load types for {SourceType} in assembly {Assembly} - LoaderExceptions");

    private static readonly Action<ILogger, string, string, string, string, string, Exception?> _loaderExceptionLog =
        LoggerMessage.Define<string, string, string, string, string>(LogLevel.Error,
            new EventId(1002, nameof(ModuleArchitectureAnalyzer) + ".LoaderException"),
            "Loader exception in {Assembly} for type {SourceType}: {ExceptionType}: {Message}\n{StackTrace}");

    private static readonly Action<ILogger, string, string, string, string, string, Exception?> _analysisErrorLog =
        LoggerMessage.Define<string, string, string, string, string>(LogLevel.Error,
            new EventId(1003, nameof(ModuleArchitectureAnalyzer) + ".AnalysisError"),
            "ModuleArchitectureAnalyzer: error analyzing type {SourceType} in assembly {Assembly}: {ExceptionType}: {Message}\n{StackTrace}");

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleArchitectureAnalyzer"/> class.
    /// </summary>
    /// <param name="assemblies">The assemblies to analyze.</param>
    public ModuleArchitectureAnalyzer(params ReflectionAssembly[] assemblies)
    {
        if (assemblies is null || assemblies.Length == 0)
        {
            throw new ArgumentException("At least one assembly must be provided.", nameof(assemblies));
        }

        _assemblies = assemblies;
        _architecture = new Lazy<ArchUnitNET.Domain.Architecture>(() =>
            new ArchLoader().LoadAssemblies(_assemblies).Build());
        _analysisResult = new Lazy<ModuleAnalysisResult>(PerformAnalysis);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleArchitectureAnalyzer"/> class with an <see cref="ILogger"/>.
    /// </summary>
    /// <param name="logger">The logger to use for error reporting during analysis.</param>
    /// <param name="assemblies">The assemblies to analyze.</param>
    public ModuleArchitectureAnalyzer(ILogger<ModuleArchitectureAnalyzer> logger, params ReflectionAssembly[] assemblies)
        : this(assemblies)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates an analyzer for the specified assemblies.
    /// </summary>
    /// <param name="assemblies">The assemblies to analyze.</param>
    /// <returns>A new analyzer instance.</returns>
    public static ModuleArchitectureAnalyzer Analyze(params ReflectionAssembly[] assemblies)
    {
        return new ModuleArchitectureAnalyzer(assemblies);
    }

    /// <summary>
    /// Creates an analyzer for assemblies containing the specified types.
    /// </summary>
    /// <typeparam name="T1">A type from the first assembly.</typeparam>
    /// <returns>A new analyzer instance.</returns>
    public static ModuleArchitectureAnalyzer AnalyzeAssemblyContaining<T1>()
    {
        return new ModuleArchitectureAnalyzer(typeof(T1).Assembly);
    }

    /// <summary>
    /// Creates an analyzer for assemblies containing the specified types.
    /// </summary>
    /// <typeparam name="T1">A type from the first assembly.</typeparam>
    /// <typeparam name="T2">A type from the second assembly.</typeparam>
    /// <returns>A new analyzer instance.</returns>
    public static ModuleArchitectureAnalyzer AnalyzeAssemblyContaining<T1, T2>()
    {
        return new ModuleArchitectureAnalyzer(typeof(T1).Assembly, typeof(T2).Assembly);
    }

    /// <summary>
    /// Gets the analysis result.
    /// </summary>
    public ModuleAnalysisResult Result => _analysisResult.Value;

    /// <summary>
    /// Gets the underlying ArchUnitNET architecture.
    /// </summary>
    public ArchUnitNET.Domain.Architecture Architecture => _architecture.Value;

    private ModuleAnalysisResult PerformAnalysis()
    {
        var modules = DiscoverModules();
        var dependencies = AnalyzeDependencies(modules);
        var circularDependencies = DetectCircularDependencies(dependencies);

        return new ModuleAnalysisResult(
            modules.ToList().AsReadOnly(),
            dependencies.ToList().AsReadOnly(),
            circularDependencies.ToList().AsReadOnly());
    }

    private List<ModuleInfo> DiscoverModules()
    {
        var modules = new List<ModuleInfo>();

        foreach (var assembly in _assemblies)
        {
            var moduleTypes = assembly.GetTypes()
                .Where(t => typeof(IModule).IsAssignableFrom(t)
                    && t.IsClass
                    && !t.IsAbstract);

            foreach (var moduleType in moduleTypes)
            {
                try
                {
                    if (Activator.CreateInstance(moduleType) is IModule moduleInstance)
                    {
                        modules.Add(new ModuleInfo(
                            moduleInstance.Name,
                            moduleType,
                            assembly,
                            GetModuleNamespace(moduleType)));
                    }
                }
                catch
                {
                    // Skip modules that can't be instantiated (e.g., require constructor parameters)
                    modules.Add(new ModuleInfo(
                        moduleType.Name.Replace("Module", string.Empty),
                        moduleType,
                        assembly,
                        GetModuleNamespace(moduleType)));
                }
            }
        }

        return modules;
    }

    private static string GetModuleNamespace(ReflectionType moduleType)
    {
        var ns = moduleType.Namespace ?? string.Empty;
        // Return the base module namespace (up to the module folder)
        var parts = ns.Split('.');
        if (parts.Length >= 2)
        {
            // Find the module name in the namespace
            for (var i = parts.Length - 1; i >= 0; i--)
            {
                if (parts[i].EndsWith("Module", StringComparison.Ordinal) ||
                    parts[i].EndsWith('s')) // e.g., "Orders", "Payments"
                {
                    return string.Join(".", parts.Take(i + 1));
                }
            }
        }

        return ns;
    }

    private List<ModuleDependency> AnalyzeDependencies(List<ModuleInfo> modules)
    {
        var dependencies = new List<ModuleDependency>();

        foreach (var sourceModule in modules)
        {
            foreach (var targetModule in modules)
            {
                if (sourceModule.Name == targetModule.Name)
                {
                    continue;
                }

                var hasDependency = CheckDependency(sourceModule, targetModule);
                if (hasDependency)
                {
                    dependencies.Add(new ModuleDependency(
                        sourceModule.Name,
                        targetModule.Name,
                        DependencyType.Direct));
                }
            }
        }

        return dependencies;
    }

    private bool CheckDependency(ModuleInfo source, ModuleInfo target)
    {
        var sourceTypes = source.Assembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith(source.Namespace, StringComparison.Ordinal) == true);

        foreach (var sourceType in sourceTypes)
        {
            if (HasDependencyOnTarget(sourceType, source, target))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasDependencyOnTarget(ReflectionType sourceType, ModuleInfo source, ModuleInfo target)
    {
        try
        {
            var referencedTypes = GetReferencedTypes(sourceType);
            return referencedTypes.Any(t =>
                t.Namespace?.StartsWith(target.Namespace, StringComparison.Ordinal) == true);
        }
        catch (System.Reflection.ReflectionTypeLoadException rtle)
        {
            LogReflectionTypeLoadException(sourceType, source.Assembly, rtle);
            return false;
        }
        catch (Exception ex)
        {
            LogAnalysisException(sourceType, source.Assembly, ex);
            return false;
        }
    }

    private void LogReflectionTypeLoadException(ReflectionType sourceType, ReflectionAssembly assembly, System.Reflection.ReflectionTypeLoadException rtle)
    {
        var asmName = assembly.GetName().Name ?? "<unknown assembly>";
        var typeName = sourceType.FullName ?? sourceType.Name ?? "<unknown type>";

        if (_logger is not null)
        {
            _failedToLoadTypes(_logger, typeName, asmName, rtle);
            LogLoaderExceptions(asmName, typeName, rtle.LoaderExceptions);
        }
        else
        {
            Console.Error.WriteLine($"ModuleArchitectureAnalyzer: failed to load types for {typeName} in assembly {asmName} - LoaderExceptions:");
            WriteLoaderExceptionsToConsole(rtle.LoaderExceptions);
        }
    }

    private void LogLoaderExceptions(string asmName, string typeName, Exception?[] loaderExceptions)
    {
        foreach (var le in loaderExceptions)
        {
            if (le is null) continue;
            _loaderExceptionLog(_logger!,
                asmName,
                typeName,
                le.GetType().Name,
                le.Message ?? string.Empty,
                le.StackTrace ?? string.Empty,
                le);
        }
    }

    private static void WriteLoaderExceptionsToConsole(Exception?[] loaderExceptions)
    {
        foreach (var le in loaderExceptions)
        {
            if (le is null) continue;
            Console.Error.WriteLine($" - {le.GetType().Name}: {le.Message}\n{le.StackTrace}");
        }
    }

    private void LogAnalysisException(ReflectionType sourceType, ReflectionAssembly assembly, Exception ex)
    {
        var asmName = assembly.GetName().Name ?? "<unknown assembly>";
        var typeName = sourceType.FullName ?? sourceType.Name ?? "<unknown type>";

        if (_logger is not null)
        {
            _analysisErrorLog(_logger,
                typeName,
                asmName,
                ex.GetType().Name,
                ex.Message ?? string.Empty,
                ex.StackTrace ?? string.Empty,
                ex);
        }
        else
        {
            Console.Error.WriteLine($"ModuleArchitectureAnalyzer: error analyzing type {typeName} in assembly {asmName}: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private static HashSet<ReflectionType> GetReferencedTypes(ReflectionType type)
    {
        var types = new HashSet<ReflectionType>();

        // Base type
        if (type.BaseType is not null && type.BaseType != typeof(object))
        {
            types.Add(type.BaseType);
        }

        // Interfaces
        foreach (var iface in type.GetInterfaces())
        {
            types.Add(iface);
        }

        // Fields
        foreach (var field in type.GetFields(ReflectionBindingFlags.Instance | ReflectionBindingFlags.Static |
            ReflectionBindingFlags.Public | ReflectionBindingFlags.NonPublic))
        {
            types.Add(field.FieldType);
        }

        // Properties
        foreach (var prop in type.GetProperties(ReflectionBindingFlags.Instance | ReflectionBindingFlags.Static |
            ReflectionBindingFlags.Public | ReflectionBindingFlags.NonPublic))
        {
            types.Add(prop.PropertyType);
        }

        // Method parameters and return types
        foreach (var method in type.GetMethods(ReflectionBindingFlags.Instance | ReflectionBindingFlags.Static |
            ReflectionBindingFlags.Public | ReflectionBindingFlags.NonPublic | ReflectionBindingFlags.DeclaredOnly))
        {
            types.Add(method.ReturnType);
            foreach (var param in method.GetParameters())
            {
                types.Add(param.ParameterType);
            }
        }

        // Constructor parameters
        foreach (var ctor in type.GetConstructors(ReflectionBindingFlags.Instance |
            ReflectionBindingFlags.Public | ReflectionBindingFlags.NonPublic))
        {
            foreach (var param in ctor.GetParameters())
            {
                types.Add(param.ParameterType);
            }
        }

        return types;
    }

    private static List<CircularDependency> DetectCircularDependencies(List<ModuleDependency> dependencies)
    {
        var graph = BuildDependencyGraph(dependencies);
        var circularDeps = new List<CircularDependency>();
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();
        var path = new Stack<string>();

        foreach (var module in graph.Keys)
        {
            if (!visited.Contains(module))
            {
                DetectCyclesDfs(module, graph, visited, recursionStack, path, circularDeps);
            }
        }

        return circularDeps;
    }

    private static Dictionary<string, List<string>> BuildDependencyGraph(List<ModuleDependency> dependencies)
    {
        var graph = new Dictionary<string, List<string>>();

        foreach (var dep in dependencies)
        {
            if (!graph.TryGetValue(dep.SourceModule, out var sourceList))
            {
                sourceList = new List<string>();
                graph[dep.SourceModule] = sourceList;
            }

            sourceList.Add(dep.TargetModule);

            // Ensure target module is in graph even if it has no outgoing dependencies
            if (!graph.ContainsKey(dep.TargetModule))
            {
                graph[dep.TargetModule] = new List<string>();
            }
        }

        return graph;
    }

    private static void DetectCyclesDfs(
        string module,
        Dictionary<string, List<string>> graph,
        HashSet<string> visited,
        HashSet<string> recursionStack,
        Stack<string> path,
        List<CircularDependency> circularDeps)
    {
        visited.Add(module);
        recursionStack.Add(module);
        path.Push(module);

        if (graph.TryGetValue(module, out var neighbors))
        {
            foreach (var neighbor in neighbors)
            {
                if (!visited.Contains(neighbor))
                {
                    DetectCyclesDfs(neighbor, graph, visited, recursionStack, path, circularDeps);
                }
                else if (recursionStack.Contains(neighbor))
                {
                    // Found a cycle
                    var cycle = ExtractCycle(path, neighbor);
                    circularDeps.Add(new CircularDependency(cycle.AsReadOnly()));
                }
            }
        }

        path.Pop();
        recursionStack.Remove(module);
    }

    private static List<string> ExtractCycle(Stack<string> path, string cycleStart)
    {
        var cycle = new List<string>();
        var pathList = path.ToList();
        pathList.Reverse();

        var started = false;
        foreach (var module in pathList)
        {
            if (module == cycleStart)
            {
                started = true;
            }

            if (started)
            {
                cycle.Add(module);
            }
        }

        cycle.Add(cycleStart); // Complete the cycle
        return cycle;
    }
}

/// <summary>
/// Represents the result of module architecture analysis.
/// </summary>
public sealed class ModuleAnalysisResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleAnalysisResult"/> class.
    /// </summary>
    public ModuleAnalysisResult(
        IReadOnlyList<ModuleInfo> modules,
        IReadOnlyList<ModuleDependency> dependencies,
        IReadOnlyList<CircularDependency> circularDependencies)
    {
        Modules = modules;
        Dependencies = dependencies;
        CircularDependencies = circularDependencies;
    }

    /// <summary>
    /// Gets the discovered modules.
    /// </summary>
    public IReadOnlyList<ModuleInfo> Modules { get; }

    /// <summary>
    /// Gets the dependencies between modules.
    /// </summary>
    public IReadOnlyList<ModuleDependency> Dependencies { get; }

    /// <summary>
    /// Gets any detected circular dependencies.
    /// </summary>
    public IReadOnlyList<CircularDependency> CircularDependencies { get; }

    /// <summary>
    /// Gets the number of modules discovered.
    /// </summary>
    public int ModuleCount => Modules.Count;

    /// <summary>
    /// Gets a value indicating whether there are circular dependencies.
    /// </summary>
    public bool HasCircularDependencies => CircularDependencies.Count > 0;

    #region Assertions

    /// <summary>
    /// Asserts that there are no circular dependencies between modules.
    /// </summary>
    /// <returns>This result for chaining.</returns>
    public ModuleAnalysisResult ShouldHaveNoCircularDependencies()
    {
        CircularDependencies.ShouldBeEmpty(
            $"Found {CircularDependencies.Count} circular dependency chain(s):{Environment.NewLine}" +
            string.Join(Environment.NewLine, CircularDependencies.Select(c =>
                $"  - {string.Join(" -> ", c.ModulesInCycle)}")));
        return this;
    }

    /// <summary>
    /// Asserts that the specified module exists.
    /// </summary>
    /// <param name="moduleName">The module name to check.</param>
    /// <returns>This result for chaining.</returns>
    public ModuleAnalysisResult ShouldContainModule(string moduleName)
    {
        Modules.Any(m => m.Name.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
            .ShouldBeTrue($"Module '{moduleName}' not found. Found modules: " +
                $"[{string.Join(", ", Modules.Select(m => m.Name))}]");
        return this;
    }

    /// <summary>
    /// Asserts that a module does not depend on another module.
    /// </summary>
    /// <param name="sourceModule">The source module name.</param>
    /// <param name="targetModule">The target module name that should not be a dependency.</param>
    /// <returns>This result for chaining.</returns>
    public ModuleAnalysisResult ShouldNotHaveDependency(string sourceModule, string targetModule)
    {
        Dependencies.Any(d =>
            d.SourceModule.Equals(sourceModule, StringComparison.OrdinalIgnoreCase) &&
            d.TargetModule.Equals(targetModule, StringComparison.OrdinalIgnoreCase))
            .ShouldBeFalse(
                $"Module '{sourceModule}' should not depend on '{targetModule}' but a dependency was found.");
        return this;
    }

    /// <summary>
    /// Asserts that a module depends on another module.
    /// </summary>
    /// <param name="sourceModule">The source module name.</param>
    /// <param name="targetModule">The expected target module dependency.</param>
    /// <returns>This result for chaining.</returns>
    public ModuleAnalysisResult ShouldHaveDependency(string sourceModule, string targetModule)
    {
        Dependencies.Any(d =>
            d.SourceModule.Equals(sourceModule, StringComparison.OrdinalIgnoreCase) &&
            d.TargetModule.Equals(targetModule, StringComparison.OrdinalIgnoreCase))
            .ShouldBeTrue(
                $"Expected module '{sourceModule}' to depend on '{targetModule}' but no dependency was found.");
        return this;
    }

    #endregion
}

// ModuleInfo, ModuleDependency, CircularDependency and DependencyType
// have been extracted into their own files to improve maintainability.

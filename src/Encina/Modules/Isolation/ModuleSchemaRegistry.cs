using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Encina.Modules.Isolation;

/// <summary>
/// Default implementation of <see cref="IModuleSchemaRegistry"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation maintains a thread-safe registry of module-to-schema mappings.
/// It is typically configured during application startup and then used as a read-only
/// registry during request processing.
/// </para>
/// <para>
/// Schema names are normalized to lowercase for case-insensitive comparison.
/// </para>
/// </remarks>
public sealed class ModuleSchemaRegistry : IModuleSchemaRegistry
{
    private readonly ConcurrentDictionary<string, ModuleSchemaEntry> _modules = new(StringComparer.OrdinalIgnoreCase);
    private readonly ImmutableHashSet<string> _sharedSchemas;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleSchemaRegistry"/> class.
    /// </summary>
    /// <param name="options">The module isolation configuration.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> is <c>null</c>.
    /// </exception>
    public ModuleSchemaRegistry(ModuleIsolationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Store shared schemas (normalized to lowercase)
        _sharedSchemas = options.SharedSchemas
            .Select(s => s.ToLowerInvariant())
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

        // Register each module's schema configuration
        foreach (var moduleSchema in options.ModuleSchemas)
        {
            RegisterModule(moduleSchema);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleSchemaRegistry"/> class with empty configuration.
    /// </summary>
    /// <remarks>
    /// Use this constructor for testing or when building the registry programmatically.
    /// </remarks>
    public ModuleSchemaRegistry()
        : this(new ModuleIsolationOptions())
    {
    }

    /// <inheritdoc />
    public IReadOnlySet<string> GetAllowedSchemas(string moduleName)
    {
        if (string.IsNullOrWhiteSpace(moduleName))
        {
            return ImmutableHashSet<string>.Empty;
        }

        if (_modules.TryGetValue(moduleName, out var entry))
        {
            return entry.AllowedSchemas;
        }

        // Module not registered - return only shared schemas
        return _sharedSchemas;
    }

    /// <inheritdoc />
    public ModuleSchemaOptions? GetModuleOptions(string moduleName)
    {
        if (string.IsNullOrWhiteSpace(moduleName))
        {
            return null;
        }

        return _modules.TryGetValue(moduleName, out var entry) ? entry.Options : null;
    }

    /// <inheritdoc />
    public bool CanAccessSchema(string moduleName, string schemaName)
    {
        if (string.IsNullOrWhiteSpace(schemaName))
        {
            return true; // No schema specified, allow
        }

        var normalizedSchema = schemaName.ToLowerInvariant();

        // Shared schemas are always allowed
        if (_sharedSchemas.Contains(normalizedSchema))
        {
            return true;
        }

        // Check module-specific permissions
        if (!string.IsNullOrWhiteSpace(moduleName) &&
            _modules.TryGetValue(moduleName, out var entry))
        {
            return entry.AllowedSchemas.Contains(normalizedSchema);
        }

        // Module not registered - deny access to non-shared schemas
        return false;
    }

    /// <inheritdoc />
    public IEnumerable<string> GetRegisteredModules()
    {
        return _modules.Keys;
    }

    /// <inheritdoc />
    public IReadOnlySet<string> GetSharedSchemas()
    {
        return _sharedSchemas;
    }

    /// <inheritdoc />
    public bool IsModuleRegistered(string moduleName)
    {
        return !string.IsNullOrWhiteSpace(moduleName) && _modules.ContainsKey(moduleName);
    }

    /// <inheritdoc />
    public SchemaAccessValidationResult ValidateSqlAccess(string moduleName, string sql)
    {
        var allowedSchemas = GetAllowedSchemas(moduleName);
        var accessedSchemas = SqlSchemaExtractor.ExtractSchemas(sql);

        if (accessedSchemas.Count == 0)
        {
            return SchemaAccessValidationResult.Success(accessedSchemas, allowedSchemas);
        }

        var unauthorized = accessedSchemas
            .Where(s => !allowedSchemas.Contains(s))
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

        if (unauthorized.Count == 0)
        {
            return SchemaAccessValidationResult.Success(accessedSchemas, allowedSchemas);
        }

        return SchemaAccessValidationResult.Failure(accessedSchemas, unauthorized, allowedSchemas);
    }

    /// <summary>
    /// Registers a module's schema configuration.
    /// </summary>
    /// <param name="options">The module schema options.</param>
    private void RegisterModule(ModuleSchemaOptions options)
    {
        // Compute the full set of allowed schemas for this module
        var allowedSchemas = ComputeAllowedSchemas(options);

        var entry = new ModuleSchemaEntry(options, allowedSchemas);
        _modules[options.ModuleName] = entry;
    }

    /// <summary>
    /// Computes the complete set of allowed schemas for a module.
    /// </summary>
    private ImmutableHashSet<string> ComputeAllowedSchemas(ModuleSchemaOptions options)
    {
        var builder = ImmutableHashSet.CreateBuilder<string>(StringComparer.OrdinalIgnoreCase);

        // Add the module's own schema
        builder.Add(options.SchemaName.ToLowerInvariant());

        // Add shared schemas
        foreach (var schema in _sharedSchemas)
        {
            builder.Add(schema);
        }

        // Add additional allowed schemas
        foreach (var schema in options.AdditionalAllowedSchemas)
        {
            builder.Add(schema.ToLowerInvariant());
        }

        return builder.ToImmutable();
    }

    /// <summary>
    /// Internal entry for a registered module.
    /// </summary>
    private sealed class ModuleSchemaEntry
    {
        public ModuleSchemaOptions Options { get; }
        public ImmutableHashSet<string> AllowedSchemas { get; }

        public ModuleSchemaEntry(ModuleSchemaOptions options, ImmutableHashSet<string> allowedSchemas)
        {
            Options = options;
            AllowedSchemas = allowedSchemas;
        }
    }
}
